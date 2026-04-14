#!/usr/bin/env bash
# =============================================================================
# MumbleManager
# Author:  Gerald Hull, W1VE
# Date:    April 14, 2026
# License: MIT License
#
# Permission is hereby granted, free of charge, to any person obtaining a copy
# of this software and associated documentation files (the "Software"), to deal
# in the Software without restriction.  The above copyright notice and this
# permission notice shall be included in all copies or substantial portions of
# the Software.
# =============================================================================

# =============================================================================
#  deploy.sh — MumbleManager one-shot deployment for a clean Ubuntu VPS
#
#  Usage:
#    chmod +x deploy.sh
#    ./deploy.sh
#
#  What it does:
#    1. Installs Docker Engine + Docker Compose plugin (if missing)
#    2. Copies .env.example → .env if no .env exists
#    3. Builds the Docker images (multi-stage: Node → .NET SDK → aspnet runtime)
#    4. Starts the stack (app + nginx) in the background
#    5. Tails the logs briefly so you can confirm a healthy start
# =============================================================================
set -euo pipefail

REPO_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$REPO_DIR"

RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; NC='\033[0m'
info()  { echo -e "${GREEN}[deploy]${NC} $*"; }
warn()  { echo -e "${YELLOW}[warn]${NC}  $*"; }
error() { echo -e "${RED}[error]${NC} $*" >&2; }

# ── 1. Docker ────────────────────────────────────────────────────────────────
if ! command -v docker &>/dev/null; then
  info "Installing Docker Engine…"
  apt-get update -qq
  apt-get install -y -qq ca-certificates curl gnupg lsb-release

  install -m 0755 -d /etc/apt/keyrings
  curl -fsSL https://download.docker.com/linux/ubuntu/gpg \
    | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
  chmod a+r /etc/apt/keyrings/docker.gpg

  echo \
    "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] \
    https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" \
    | tee /etc/apt/sources.list.d/docker.list > /dev/null

  apt-get update -qq
  apt-get install -y -qq docker-ce docker-ce-cli containerd.io \
                         docker-buildx-plugin docker-compose-plugin
  systemctl enable --now docker
  info "Docker installed."
else
  info "Docker already installed: $(docker --version)"
fi

# ── 2. .env file ─────────────────────────────────────────────────────────────
if [[ ! -f .env ]]; then
  cp .env.example .env
  warn ".env created from .env.example — review it before going to production."
else
  info ".env already exists — leaving it unchanged."
fi

# ── 3. Build ──────────────────────────────────────────────────────────────────
info "Building Docker images (this takes a few minutes on first run)…"
docker compose build --pull

# ── 4. Start ──────────────────────────────────────────────────────────────────
info "Starting services…"
docker compose up -d

# ── 5. Health check ───────────────────────────────────────────────────────────
info "Waiting for app to become healthy…"
MAX=30; COUNT=0
until docker compose exec -T app wget -qO- http://localhost:5000/health &>/dev/null; do
  COUNT=$((COUNT+1))
  if [[ $COUNT -ge $MAX ]]; then
    error "App did not become healthy after ${MAX}s — check logs with: docker compose logs app"
    exit 1
  fi
  sleep 2
done

info "✓ MumbleManager is running."
echo
echo "  Local:   http://localhost"
echo "  Swagger: http://localhost/swagger  (if ASPNETCORE_ENVIRONMENT=Development)"
echo
info "Tailing logs for 10 seconds — press Ctrl-C to stop following:"
sleep 2
timeout 10 docker compose logs -f --tail=40 || true
echo
info "Done. Use 'docker compose logs -f' to follow logs."
