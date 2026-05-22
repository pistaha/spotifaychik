#!/usr/bin/env sh
set -eu

echo "Health:"
curl -k https://localhost/health

echo
echo "Metrics:"
curl -k https://localhost/metrics

echo
echo "20 requests through nginx to show different backend instances:"
for i in $(seq 1 20); do
  curl -sk https://localhost/
  echo
done

echo
echo "50 requests of synthetic load:"
for i in $(seq 1 50); do
  curl -sk https://localhost/ > /dev/null
done
echo "Load finished"

echo
echo "Prometheus targets:"
curl -s http://localhost:9090/api/v1/targets

echo
echo "Prometheus up query:"
curl -s "http://localhost:9090/api/v1/query" \
  --data-urlencode 'query=up'

echo
echo "Prometheus 5xx rate query:"
curl -s "http://localhost:9090/api/v1/query" \
  --data-urlencode 'query=sum(rate(http_requests_received_total{status=~"5.."}[1m]))'
