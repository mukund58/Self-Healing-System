#!/bin/bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
KIND_CLUSTER="self-healing"

echo "========================================="
echo " Self-Healing System — K8s Deployment"
echo "========================================="

# ── 1. Build Docker images ──
echo ""
echo "[1/5] Building Docker images..."

echo "  → TaskApi..."
docker build -t taskapi:latest -f "$PROJECT_DIR/TaskApi/Dockerfile" "$PROJECT_DIR/TaskApi"

echo "  → Analyzer..."
docker build -t analyzer:latest -f "$PROJECT_DIR/Analyzer/Dockerfile" "$PROJECT_DIR/Analyzer"

# ── 2. Load into kind ──
echo ""
echo "[2/5] Loading images into kind cluster '$KIND_CLUSTER'..."
kind load docker-image taskapi:latest --name "$KIND_CLUSTER"
kind load docker-image analyzer:latest --name "$KIND_CLUSTER"

# ── 3. Apply manifests ──
echo ""
echo "[3/5] Applying Kubernetes manifests..."

kubectl apply -f "$SCRIPT_DIR/postgres.yaml"
kubectl apply -f "$SCRIPT_DIR/prometheus.yaml"
kubectl apply -f "$SCRIPT_DIR/grafana.yaml"
kubectl apply -f "$SCRIPT_DIR/rbac.yaml"
kubectl apply -f "$SCRIPT_DIR/taskapi.yaml"

echo "  Waiting for Postgres to be ready..."
kubectl rollout status deployment/postgres --timeout=120s

# Apply analyzer after postgres + taskapi are wired
kubectl apply -f "$SCRIPT_DIR/analyzer.yaml"

# ── 4. Force rollout (pick up new images) ──
echo ""
echo "[4/5] Rolling out new images..."
kubectl rollout restart deployment/taskapi
kubectl rollout restart deployment/analyzer

echo "  Waiting for rollouts to complete..."
kubectl rollout status deployment/taskapi --timeout=120s
kubectl rollout status deployment/analyzer --timeout=120s

# ── 5. Summary ──
echo ""
echo "[5/5] Deployment complete!"
echo ""
kubectl get pods -o wide
echo ""
echo "========================================="
echo " Access via port-forward:"
echo "   kubectl port-forward svc/taskapi 5186:80"
echo "   kubectl port-forward svc/analyzer 5297:80"
echo "   kubectl port-forward svc/grafana 3000:3000"
echo "   kubectl port-forward svc/prometheus 9090:9090"
echo "========================================="
