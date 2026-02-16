#!/bin/bash
set -euo pipefail

# ═══════════════════════════════════════════════════════════
#  Self-Healing System — Run Script
#  Starts port-forwards and frontend dev server
# ═══════════════════════════════════════════════════════════

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m'
BOLD='\033[1m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
KIND_CLUSTER="self-healing"
PIDS=()

# ── Cleanup on exit ──────────────────────────────────────

cleanup() {
    echo ""
    echo -e "${YELLOW}Stopping all services...${NC}"
    for pid in "${PIDS[@]}"; do
        kill "$pid" 2>/dev/null || true
    done
    wait 2>/dev/null
    echo -e "${GREEN}All services stopped.${NC}"
    exit 0
}

trap cleanup INT TERM

# ── Helper functions ─────────────────────────────────────

log_ok()   { echo -e "  ${GREEN}✓${NC} $1"; }
log_info() { echo -e "  ${BLUE}→${NC} $1"; }
log_err()  { echo -e "  ${RED}✗${NC} $1"; }

wait_for_port() {
    local port=$1
    local name=$2
    local max_attempts=30
    local attempt=0

    while ! curl -sf "http://localhost:$port" >/dev/null 2>&1; do
        attempt=$((attempt + 1))
        if [ $attempt -ge $max_attempts ]; then
            log_err "$name not responding on port $port after ${max_attempts}s"
            return 1
        fi
        sleep 1
    done
    log_ok "$name is ready on port $port"
}

echo -e "${BOLD}"
echo "╔═══════════════════════════════════════════════════════╗"
echo "║       Self-Healing System — Starting Services         ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo -e "${NC}"

# ── 1. Verify cluster is running ─────────────────────────

echo -e "\n${CYAN}[1/4] Checking Kubernetes cluster...${NC}"

if ! kind get clusters 2>/dev/null | grep -qx "$KIND_CLUSTER"; then
    log_err "Kind cluster '$KIND_CLUSTER' not found. Run ./install.sh first."
    exit 1
fi

kubectl cluster-info --context "kind-${KIND_CLUSTER}" &>/dev/null
log_ok "Cluster '$KIND_CLUSTER' is running"

# ── 2. Check pod health ─────────────────────────────────

echo -e "\n${CYAN}[2/4] Checking pod status...${NC}"

PODS_READY=$(kubectl get pods --no-headers 2>/dev/null | grep -c "Running" || echo "0")
PODS_TOTAL=$(kubectl get pods --no-headers 2>/dev/null | wc -l || echo "0")

if [ "$PODS_READY" -eq 0 ]; then
    log_err "No running pods found. Run ./install.sh to deploy."
    exit 1
fi

log_ok "$PODS_READY/$PODS_TOTAL pods running"
kubectl get pods --no-headers 2>/dev/null | while read -r line; do
    echo -e "    $line"
done

# ── 3. Start port-forwards ──────────────────────────────

echo -e "\n${CYAN}[3/4] Starting port-forwards...${NC}"

# Kill existing port-forwards on these ports
for port in 5186 5297 3000 9090; do
    fuser -k "${port}/tcp" 2>/dev/null || true
done
sleep 1

kubectl port-forward svc/taskapi    5186:80   &>/dev/null &
PIDS+=($!)
log_info "TaskApi     → http://localhost:5186"

kubectl port-forward svc/analyzer   5297:80   &>/dev/null &
PIDS+=($!)
log_info "Analyzer    → http://localhost:5297"

kubectl port-forward svc/grafana    3000:3000 &>/dev/null &
PIDS+=($!)
log_info "Grafana     → http://localhost:3000"

kubectl port-forward svc/prometheus 9090:9090 &>/dev/null &
PIDS+=($!)
log_info "Prometheus  → http://localhost:9090"

# Wait for services
sleep 3
echo ""
log_info "Waiting for backend services..."
wait_for_port 5186 "TaskApi" || true
wait_for_port 5297 "Analyzer" || true

# ── 4. Start frontend dev server ─────────────────────────

echo -e "\n${CYAN}[4/4] Starting frontend dev server...${NC}"

cd "$SCRIPT_DIR/task-frontend"

# Check if node_modules exists
if [ ! -d "node_modules" ]; then
    log_info "Installing frontend dependencies..."
    npm install
fi

VITE_API_URL="http://localhost:5186" \
VITE_ANALYZER_URL="http://localhost:5297" \
npx vite --host &
PIDS+=($!)

cd "$SCRIPT_DIR"

sleep 3

echo ""
echo -e "${GREEN}${BOLD}"
echo "╔═══════════════════════════════════════════════════════╗"
echo "║           All Services Running!                       ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo -e "${NC}"
echo ""
echo -e "  ${BOLD}Frontend${NC}     ${CYAN}http://localhost:5173${NC}"
echo -e "  ${BOLD}TaskApi${NC}      ${CYAN}http://localhost:5186${NC}"
echo -e "  ${BOLD}Analyzer${NC}     ${CYAN}http://localhost:5297${NC}"
echo -e "  ${BOLD}Grafana${NC}      ${CYAN}http://localhost:3000${NC}  (admin/admin)"
echo -e "  ${BOLD}Prometheus${NC}   ${CYAN}http://localhost:9090${NC}"
echo ""
echo -e "  ${BOLD}Stress Test:${NC} curl http://localhost:5186/stress/memory?mb=100"
echo ""
echo -e "  ${YELLOW}Press Ctrl+C to stop all services${NC}"
echo ""

# Wait for all background processes
wait
