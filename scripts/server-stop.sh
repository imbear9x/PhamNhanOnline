#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PID_FILE="$ROOT_DIR/.run/gameserver.pid"

if [[ ! -f "$PID_FILE" ]]; then
  echo "GameServer is not running (no pid file)."
  exit 0
fi

PID="$(cat "$PID_FILE" || true)"
if [[ -z "${PID:-}" ]]; then
  rm -f "$PID_FILE"
  echo "Stale pid file removed."
  exit 0
fi

if kill -0 "$PID" 2>/dev/null; then
  kill "$PID"
  for _ in {1..20}; do
    if kill -0 "$PID" 2>/dev/null; then
      sleep 0.5
    else
      break
    fi
  done

  if kill -0 "$PID" 2>/dev/null; then
    kill -9 "$PID" || true
  fi
  echo "GameServer stopped (pid=$PID)."
else
  echo "Process $PID not found."
fi

rm -f "$PID_FILE"
