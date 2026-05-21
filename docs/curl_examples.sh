#!/usr/bin/env sh
set -eu

echo "Load balancing test:"
for _ in 1 2 3 4 5 6; do
  curl -sk -D - https://localhost/ -o /tmp/music-service-response.json | grep -i "x-instance-id"
  cat /tmp/music-service-response.json
  echo
done

echo "Static file test:"
curl -sk https://localhost/static/style.css

echo
echo "Health check through Nginx:"
curl -sk -D - https://localhost/health -o /dev/null

echo
echo "Metrics endpoint through Nginx:"
curl -sk https://localhost/metrics | head -n 20

echo
echo "Prometheus targets:"
curl -s http://localhost:9090/api/v1/targets

echo
echo "Prometheus 5xx rate query:"
curl -s "http://localhost:9090/api/v1/query" \
  --data-urlencode 'query=sum(rate(http_requests_received_total{status=~"5.."}[1m]))'

echo
echo "Grafana health:"
curl -s http://localhost:3000/api/health

echo
echo "Direct access must be closed from host:"
if curl -sS --max-time 3 http://localhost:8080 >/tmp/music-service-direct.out 2>/tmp/music-service-direct.err; then
  echo "Unexpected: backend is reachable directly on localhost:8080"
  cat /tmp/music-service-direct.out
else
  echo "OK: direct backend access is closed"
  cat /tmp/music-service-direct.err
fi

echo
echo "Synthetic load:"
for _ in $(seq 1 20); do
  curl -sk https://localhost/ > /dev/null
done
echo "Load sent"
