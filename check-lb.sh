#!/usr/bin/env sh
set -eu

REQUESTS="${1:-12}"

echo "Resolved app replicas:"
docker compose ps --format '{{.Name}}' app | while read -r name; do
  hostname_value="$(docker inspect --format '{{.Config.Hostname}}' "$name")"
  short_name="$(printf '%s' "$name" | sed 's/^music-service-//')"
  echo "$hostname_value -> $short_name"
done

echo
echo "Load balancing results:"
i=1
while [ "$i" -le "$REQUESTS" ]; do
  instance_id="$(curl -sk -D - https://localhost/ -o /dev/null | awk 'BEGIN{IGNORECASE=1}/^X-Instance-Id:/{print $2}' | tr -d '\r')"
  matched_name="$(docker compose ps --format '{{.Name}}' app | while read -r name; do
    hostname_value="$(docker inspect --format '{{.Config.Hostname}}' "$name")"
    if [ "$hostname_value" = "$instance_id" ]; then
      printf '%s' "$name" | sed 's/^music-service-//'
      break
    fi
  done)"

  if [ -n "$matched_name" ]; then
    echo "$matched_name"
  else
    echo "$instance_id"
  fi

  i=$((i + 1))
done
