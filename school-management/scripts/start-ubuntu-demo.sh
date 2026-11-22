#!/usr/bin/env bash
# Convenience for a previously configured local PostgreSQL cluster.
set -euo pipefail
TASK_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TASK_PG_DATA="${SCHOOL_PG_DATA:-$HOME/.local/share/mission-school-postgres}"
TASK_PG_BIN="$(pg_config --bindir)"
if [ ! -f "$TASK_PG_DATA/PG_VERSION" ]; then
  printf '%s\n' 'Set up PostgreSQL and backend configuration using README.md first.'
  exit 1
fi
if ! "$TASK_PG_BIN/pg_ctl" -D "$TASK_PG_DATA" status >/dev/null 2>&1; then
  "$TASK_PG_BIN/pg_ctl" -D "$TASK_PG_DATA" -o "-p 55432 -h 127.0.0.1 -k $TASK_PG_DATA" -l "$TASK_PG_DATA/server.log" start
fi
cd "$TASK_ROOT/backend"
exec dotnet run
