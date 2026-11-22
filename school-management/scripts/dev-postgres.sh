#!/usr/bin/env bash
# Optional Ubuntu unprivileged isolated cluster. Never used in production.
set -euo pipefail
PG_BIN="$(pg_config --bindir)"
TASK_PG_DATA="${SCHOOL_PG_DATA:-$HOME/.local/share/mission-school-postgres}"
mkdir -p "$TASK_PG_DATA"
if [ ! -f "$TASK_PG_DATA/PG_VERSION" ]; then "$PG_BIN/initdb" -D "$TASK_PG_DATA" -A scram-sha-256 -U mission_app -W; fi
if ! "$PG_BIN/pg_ctl" -D "$TASK_PG_DATA" status >/dev/null 2>&1; then
  "$PG_BIN/pg_ctl" -D "$TASK_PG_DATA" -o "-p 55432 -h 127.0.0.1 -k $TASK_PG_DATA" -l "$TASK_PG_DATA/server.log" start
fi
printf '%s\n' 'Create database: createdb -h 127.0.0.1 -p 55432 -U mission_app mission_school'
