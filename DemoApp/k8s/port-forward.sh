#!/bin/bash
# Port-forward all services for local frontend access
# Run: ./port-forward.sh   (Ctrl+C to stop all)

trap 'kill $(jobs -p) 2>/dev/null; exit' INT TERM

echo "Starting port-forwards..."
kubectl port-forward svc/taskapi    5186:80   &
kubectl port-forward svc/analyzer   5297:80   &
kubectl port-forward svc/grafana    3000:3000 &
kubectl port-forward svc/prometheus 9090:9090 &

echo ""
echo "  TaskApi:    http://localhost:5186"
echo "  Analyzer:   http://localhost:5297"
echo "  Grafana:    http://localhost:3000"
echo "  Prometheus: http://localhost:9090"
echo ""
echo "Press Ctrl+C to stop all port-forwards."
wait
