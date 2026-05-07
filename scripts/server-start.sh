#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PID_DIR="$ROOT_DIR/.run"
LOG_DIR="$ROOT_DIR/.logs"
PID_FILE="$PID_DIR/gameserver.pid"
LOG_FILE="$LOG_DIR/gameserver.log"
PROJECT="$ROOT_DIR/GameServer/GameServer.csproj"

mkdir -p "$PID_DIR" "$LOG_DIR"

if [[ -f "$PID_FILE" ]]; then
  OLD_PID="$(cat "$PID_FILE" || true)"
  if [[ -n "${OLD_PID:-}" ]] && kill -0 "$OLD_PID" 2>/dev/null; then
    echo "GameServer is already running (pid=$OLD_PID)."
    exit 0
  fi
  rm -f "$PID_FILE"
fi

nohup dotnet run --project "$PROJECT" --configuration Debug >> "$LOG_FILE" 2>&1 &
NEW_PID=$!
echo "$NEW_PID" > "$PID_FILE"

sleep 1
if kill -0 "$NEW_PID" 2>/dev/null; then
  echo "GameServer started (pid=$NEW_PID). log=$LOG_FILE"
else
  echo "GameServer failed to start. Check log: $LOG_FILE"
  exit 1
fi
