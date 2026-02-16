#!/bin/bash
set -euo pipefail

# ═══════════════════════════════════════════════════════════
#  Self-Healing System — Linux Installation Script
#  Installs all prerequisites and deploys the full stack
# ═══════════════════════════════════════════════════════════

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color
BOLD='\033[1m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
KIND_CLUSTER="self-healing"

# ── Helper functions ──────────────────────────────────────

log_step()    { echo -e "\n${BLUE}${BOLD}[$1/$TOTAL_STEPS]${NC} ${CYAN}$2${NC}"; }
log_ok()      { echo -e "  ${GREEN}✓${NC} $1"; }
log_warn()    { echo -e "  ${YELLOW}⚠${NC} $1"; }
log_err()     { echo -e "  ${RED}✗${NC} $1"; }
log_info()    { echo -e "  ${BLUE}→${NC} $1"; }

command_exists() { command -v "$1" &>/dev/null; }

TOTAL_STEPS=8

echo -e "${BOLD}"
echo "╔═══════════════════════════════════════════════════════╗"
echo "║     Self-Healing System — Installation Script         ║"
echo "║     Kubernetes-based anomaly detection & recovery     ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo -e "${NC}"

# ── Step 1: Check & Install Docker ────────────────────────

log_step 1 "Checking Docker..."

if command_exists docker; then
    DOCKER_VERSION=$(docker --version | head -1)
    log_ok "Docker already installed: $DOCKER_VERSION"
else
    log_info "Installing Docker..."
    curl -fsSL https://get.docker.com | sh
    sudo usermod -aG docker "$USER"
    log_ok "Docker installed. You may need to log out/in for group changes."
fi

if ! docker info &>/dev/null; then
    log_warn "Docker daemon not running. Starting..."
    sudo systemctl start docker 2>/dev/null || sudo service docker start 2>/dev/null || true
fi

# ── Step 2: Check & Install kubectl ──────────────────────

log_step 2 "Checking kubectl..."

if command_exists kubectl; then
    KUBECTL_VERSION=$(kubectl version --client --short 2>/dev/null || kubectl version --client 2>/dev/null | head -1)
    log_ok "kubectl already installed: $KUBECTL_VERSION"
else
    log_info "Installing kubectl..."
    ARCH=$(uname -m)
    case "$ARCH" in
        x86_64)  KUBE_ARCH="amd64" ;;
        aarch64) KUBE_ARCH="arm64" ;;
        *)       log_err "Unsupported architecture: $ARCH"; exit 1 ;;
    esac
    KUBE_VERSION=$(curl -L -s https://dl.k8s.io/release/stable.txt)
    curl -LO "https://dl.k8s.io/release/${KUBE_VERSION}/bin/linux/${KUBE_ARCH}/kubectl"
    chmod +x kubectl
    sudo mv kubectl /usr/local/bin/
    log_ok "kubectl $KUBE_VERSION installed"
fi

# ── Step 3: Check & Install Kind ─────────────────────────

log_step 3 "Checking Kind..."

if command_exists kind; then
    KIND_VERSION=$(kind version 2>/dev/null || echo "installed")
    log_ok "Kind already installed: $KIND_VERSION"
else
    log_info "Installing Kind..."
    ARCH=$(uname -m)
    case "$ARCH" in
        x86_64)  KIND_ARCH="amd64" ;;
        aarch64) KIND_ARCH="arm64" ;;
        *)       log_err "Unsupported architecture: $ARCH"; exit 1 ;;
    esac
    KIND_RELEASE=$(curl -s https://api.github.com/repos/kubernetes-sigs/kind/releases/latest | grep tag_name | cut -d '"' -f 4)
    curl -Lo ./kind "https://kind.sigs.k8s.io/dl/${KIND_RELEASE}/kind-linux-${KIND_ARCH}"
    chmod +x ./kind
    sudo mv ./kind /usr/local/bin/kind
    log_ok "Kind $KIND_RELEASE installed"
fi

# ── Step 4: Check & Install .NET SDKs ────────────────────

log_step 4 "Checking .NET SDKs..."

NEED_DOTNET8=false
NEED_DOTNET9=false

if command_exists dotnet; then
    INSTALLED=$(dotnet --list-sdks 2>/dev/null || echo "")
    if echo "$INSTALLED" | grep -q "^8\."; then
        log_ok ".NET 8 SDK installed"
    else
        NEED_DOTNET8=true
    fi
    if echo "$INSTALLED" | grep -q "^9\."; then
        log_ok ".NET 9 SDK installed"
    else
        NEED_DOTNET9=true
    fi
else
    NEED_DOTNET8=true
    NEED_DOTNET9=true
fi

if $NEED_DOTNET8 || $NEED_DOTNET9; then
    log_info "Installing missing .NET SDKs via dotnet-install script..."
    curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh

    if $NEED_DOTNET8; then
        log_info "Installing .NET 8 SDK..."
        /tmp/dotnet-install.sh --channel 8.0
        log_ok ".NET 8 SDK installed"
    fi

    if $NEED_DOTNET9; then
        log_info "Installing .NET 9 SDK..."
        /tmp/dotnet-install.sh --channel 9.0
        log_ok ".NET 9 SDK installed"
    fi

    # Add to PATH if not present
    if [[ ":$PATH:" != *":$HOME/.dotnet:"* ]]; then
        export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
        echo 'export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"' >> ~/.bashrc
        log_info "Added ~/.dotnet to PATH"
    fi
fi

# ── Step 5: Check & Install Node.js ──────────────────────

log_step 5 "Checking Node.js..."

if command_exists node; then
    NODE_VERSION=$(node --version)
    log_ok "Node.js already installed: $NODE_VERSION"
else
    log_info "Installing Node.js 20 LTS..."
    if command_exists apt-get; then
        curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
        sudo apt-get install -y nodejs
    elif command_exists dnf; then
        curl -fsSL https://rpm.nodesource.com/setup_20.x | sudo bash -
        sudo dnf install -y nodejs
    elif command_exists yum; then
        curl -fsSL https://rpm.nodesource.com/setup_20.x | sudo bash -
        sudo yum install -y nodejs
    else
        log_err "Unsupported package manager. Install Node.js 20+ manually."
        exit 1
    fi
    log_ok "Node.js $(node --version) installed"
fi

if ! command_exists npm; then
    log_err "npm not found. Install npm manually."
    exit 1
fi

# ── Step 6: Create Kind Cluster ──────────────────────────

log_step 6 "Setting up Kind cluster '${KIND_CLUSTER}'..."

if kind get clusters 2>/dev/null | grep -qx "$KIND_CLUSTER"; then
    log_ok "Kind cluster '$KIND_CLUSTER' already exists"
else
    log_info "Creating Kind cluster '$KIND_CLUSTER'..."
    kind create cluster --name "$KIND_CLUSTER"
    log_ok "Kind cluster '$KIND_CLUSTER' created"
fi

kubectl cluster-info --context "kind-${KIND_CLUSTER}" &>/dev/null
log_ok "kubectl context set to kind-${KIND_CLUSTER}"

# ── Step 7: Build & Deploy to Kubernetes ─────────────────

log_step 7 "Building and deploying to Kubernetes..."

log_info "Building TaskApi Docker image..."
docker build -t taskapi:latest -f "$SCRIPT_DIR/TaskApi/Dockerfile" "$SCRIPT_DIR/TaskApi"
log_ok "TaskApi image built"

log_info "Building Analyzer Docker image..."
docker build -t analyzer:latest -f "$SCRIPT_DIR/Analyzer/Dockerfile" "$SCRIPT_DIR/Analyzer"
log_ok "Analyzer image built"

log_info "Loading images into Kind cluster..."
kind load docker-image taskapi:latest --name "$KIND_CLUSTER"
kind load docker-image analyzer:latest --name "$KIND_CLUSTER"
log_ok "Images loaded into Kind"

log_info "Applying Kubernetes manifests..."
kubectl apply -f "$SCRIPT_DIR/k8s/postgres.yaml"
kubectl apply -f "$SCRIPT_DIR/k8s/prometheus.yaml"
kubectl apply -f "$SCRIPT_DIR/k8s/grafana.yaml"
kubectl apply -f "$SCRIPT_DIR/k8s/rbac.yaml"
kubectl apply -f "$SCRIPT_DIR/k8s/taskapi.yaml"

log_info "Waiting for Postgres to be ready..."
kubectl rollout status deployment/postgres --timeout=120s
log_ok "Postgres ready"

kubectl apply -f "$SCRIPT_DIR/k8s/analyzer.yaml"

log_info "Rolling out deployments..."
kubectl rollout restart deployment/taskapi
kubectl rollout restart deployment/analyzer

log_info "Waiting for TaskApi rollout..."
kubectl rollout status deployment/taskapi --timeout=120s
log_ok "TaskApi ready"

log_info "Waiting for Analyzer rollout..."
kubectl rollout status deployment/analyzer --timeout=120s
log_ok "Analyzer ready"

# ── Step 8: Install Frontend Dependencies ────────────────

log_step 8 "Installing frontend dependencies..."

cd "$SCRIPT_DIR/task-frontend"
npm install
log_ok "Frontend dependencies installed"

cd "$SCRIPT_DIR"

# ── Summary ──────────────────────────────────────────────

echo ""
echo -e "${GREEN}${BOLD}"
echo "╔═══════════════════════════════════════════════════════╗"
echo "║         Installation Complete!                        ║"
echo "╚═══════════════════════════════════════════════════════╝"
echo -e "${NC}"
echo ""
kubectl get pods -o wide
echo ""
echo -e "${BOLD}Next Steps:${NC}"
echo -e "  Run the project:  ${CYAN}./run.sh${NC}"
echo ""
echo -e "${BOLD}Services (after running ./run.sh):${NC}"
echo -e "  Frontend:    ${CYAN}http://localhost:5173${NC}"
echo -e "  TaskApi:     ${CYAN}http://localhost:5186${NC}"
echo -e "  Analyzer:    ${CYAN}http://localhost:5297${NC}"
echo -e "  Grafana:     ${CYAN}http://localhost:3000${NC}  (admin/admin)"
echo -e "  Prometheus:  ${CYAN}http://localhost:9090${NC}"
echo ""
