#!/usr/bin/env sh
set -eu

ROOT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)

mkdir -p "$ROOT_DIR/.docker/postgres-data" "$ROOT_DIR/.docker/api-storage"
mkdir -p \
  "$ROOT_DIR/.docker/prometheus-data" \
  "$ROOT_DIR/.docker/loki-data" \
  "$ROOT_DIR/.docker/grafana-data"

if [ ! -f "$ROOT_DIR/nginx/certs/server.crt" ] || [ ! -f "$ROOT_DIR/nginx/certs/server.key" ]; then
  echo "TLS certificate not found. Generate it with:"
  echo "openssl req -x509 -nodes -days 365 -newkey rsa:2048 -keyout nginx/certs/server.key -out nginx/certs/server.crt -subj /CN=localhost"
  exit 1
fi

echo "Starting Music Service stack with Nginx load balancer..."
docker compose --env-file "$ROOT_DIR/.env" up --build -d --scale app=3

echo
echo "Services are starting in background."
echo "HTTPS:    https://localhost:${NGINX_HTTPS_PORT:-443}/"
echo "Swagger:  https://localhost:${NGINX_HTTPS_PORT:-443}/swagger"
echo "Static:   https://localhost:${NGINX_HTTPS_PORT:-443}/static/style.css"
echo "Health:   https://localhost:${NGINX_HTTPS_PORT:-443}/health"
echo "Metrics:  https://localhost:${NGINX_HTTPS_PORT:-443}/metrics"
echo "Grafana:  http://localhost:${GRAFANA_PORT:-3000}/"
echo "Prometheus: http://localhost:${PROMETHEUS_PORT:-9090}/"
echo "OOM test: ./oom-check.sh"
