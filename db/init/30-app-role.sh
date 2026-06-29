#!/bin/bash
# Runs once on a fresh Postgres data volume. Creates the least-privilege application role the
# Calendar app connects as (ASVS V13.2.2 — see docs/security/postgres-least-privilege.md).
# It owns the calendar database + public schema so EF MigrateAsync can create its own tables,
# but has no cluster-wide rights and cannot touch the separate 'logto' database.
# CALENDAR_APP_DB_PASSWORD is injected from the db service environment.
set -euo pipefail

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
  CREATE ROLE calendar_app LOGIN NOSUPERUSER NOCREATEROLE NOCREATEDB NOREPLICATION
    PASSWORD '${CALENDAR_APP_DB_PASSWORD}';
  ALTER DATABASE ${POSTGRES_DB} OWNER TO calendar_app;
  ALTER SCHEMA public OWNER TO calendar_app;
  GRANT ALL ON SCHEMA public TO calendar_app;
EOSQL

echo "30-app-role.sh: created the least-privilege 'calendar_app' role owning '${POSTGRES_DB}'."
