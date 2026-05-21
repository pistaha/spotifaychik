#!/usr/bin/env sh
set -eu

ROOT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
API_URL="${API_URL:-https://localhost:${NGINX_HTTPS_PORT:-443}}"
EVENTS_LOG="${ROOT_DIR}/.docker/oom-events.log"
HEADERS_FILE="${ROOT_DIR}/.docker/oom-response-headers.txt"
BODY_FILE="${ROOT_DIR}/.docker/oom-response-body.json"

mkdir -p "${ROOT_DIR}/.docker"
rm -f "$EVENTS_LOG"
rm -f "$HEADERS_FILE" "$BODY_FILE"

echo "Triggering API memory stress test at $API_URL/api/diagnostics/memory/oom"
curl -sk -D "$HEADERS_FILE" -o "$BODY_FILE" -X POST "$API_URL/api/diagnostics/memory/oom" || true

INSTANCE_ID=$(awk 'BEGIN{IGNORECASE=1} /^X-Instance-Id:/ {gsub("\r","",$2); print $2}' "$HEADERS_FILE" | tail -n 1)

if [ -z "${INSTANCE_ID:-}" ]; then
  echo "Failed to detect X-Instance-Id from response headers."
  echo "Captured headers:"
  cat "$HEADERS_FILE"
  exit 1
fi

CONTAINER_NAME="${CONTAINER_NAME:-music-service-${INSTANCE_ID}}"
COMPOSE_SERVICE="$INSTANCE_ID"

echo "OOM request was handled by instance: $INSTANCE_ID"
echo "Watching container: $CONTAINER_NAME"

docker events \
  --filter "container=$CONTAINER_NAME" \
  --filter "event=oom" \
  --filter "event=die" \
  --filter "event=start" \
  --filter "event=restart" \
  --format '{{.Time}} {{.Type}} {{.Action}} {{json .Actor.Attributes}}' > "$EVENTS_LOG" &
EVENTS_PID=$!

echo
echo "Waiting for real container OOM/restart sequence..."
sleep 12
kill "$EVENTS_PID" 2>/dev/null || true
wait "$EVENTS_PID" 2>/dev/null || true

echo
echo "Container status after OOM test:"
docker compose --env-file "$ROOT_DIR/.env" ps -a

echo
echo "Docker state:"
docker inspect "$CONTAINER_NAME" --format 'Status={{.State.Status}} OOMKilled={{.State.OOMKilled}} ExitCode={{.State.ExitCode}} Restarting={{.State.Restarting}} RestartCount={{.RestartCount}} StartedAt={{.State.StartedAt}} FinishedAt={{.State.FinishedAt}}'

echo
echo "Docker events:"
if [ -s "$EVENTS_LOG" ]; then
  cat "$EVENTS_LOG"
else
  echo "No matching Docker events captured."
fi

echo
echo "Response headers from OOM trigger:"
cat "$HEADERS_FILE"

echo
echo "Response body from OOM trigger:"
cat "$BODY_FILE"

echo
echo "Last logs for restarted instance:"
docker compose --env-file "$ROOT_DIR/.env" logs --tail=80 "$COMPOSE_SERVICE"
