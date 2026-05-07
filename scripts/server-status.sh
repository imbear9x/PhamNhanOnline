#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PID_FILE="$ROOT_DIR/.run/gameserver.pid"
LOG_FILE="$ROOT_DIR/.logs/gameserver.log"

if [[ -f "$PID_FILE" ]]; then
  PID="$(cat "$PID_FILE" || true)"
  if [[ -n "${PID:-}" ]] && kill -0 "$PID" 2>/dev/null; then
    echo "RUNNING pid=$PID"
    echo "LOG=$LOG_FILE"
    exit 0
  fi
fi

echo "STOPPED"
if [[ -f "$LOG_FILE" ]]; then
  echo "LOG=$LOG_FILE"
fi
