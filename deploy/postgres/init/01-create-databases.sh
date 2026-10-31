#!/bin/sh
# Runs once, on first start of an empty Postgres volume (docker-entrypoint-initdb.d).
# Creates one database per service plus two roles per service:
#   <svc>_owner — owns the database and schema objects; used only to run migrations
#   <svc>_app   — runtime login used by the API; DML only, no DDL, no BYPASSRLS
# Local dev only: every role shares HRMS_DB_PASSWORD. Staging/prod use per-role secrets (prompt 19).
set -eu

: "${HRMS_DB_PASSWORD:?HRMS_DB_PASSWORD must be set}"

SERVICES="identity corehr time payroll workflow expense talent"

psql_su() {
  psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" "$@"
}

create_service_db() {
  svc="$1"
  db="hrms_${svc}"
  owner="${svc}_owner"
  app="${svc}_app"

  echo "Creating database ${db} with roles ${owner} / ${app}"

  psql_su --dbname postgres <<SQL
CREATE ROLE ${owner} LOGIN PASSWORD '${HRMS_DB_PASSWORD}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
CREATE ROLE ${app}   LOGIN PASSWORD '${HRMS_DB_PASSWORD}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
CREATE DATABASE ${db} OWNER ${owner};
REVOKE ALL ON DATABASE ${db} FROM PUBLIC;
GRANT CONNECT, TEMPORARY ON DATABASE ${db} TO ${app};
SQL

  psql_su --dbname "${db}" <<SQL
ALTER SCHEMA public OWNER TO ${owner};
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO ${app};
-- Objects the owner creates later (via migrations) are automatically usable by the app role.
ALTER DEFAULT PRIVILEGES FOR ROLE ${owner} IN SCHEMA public
  GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO ${app};
ALTER DEFAULT PRIVILEGES FOR ROLE ${owner} IN SCHEMA public
  GRANT USAGE, SELECT ON SEQUENCES TO ${app};
ALTER DEFAULT PRIVILEGES FOR ROLE ${owner} IN SCHEMA public
  GRANT EXECUTE ON FUNCTIONS TO ${app};
SQL
}

for svc in $SERVICES; do
  create_service_db "$svc"
done

# Hangfire storage shared by job-running services (each uses its own schema inside it).
echo "Creating database hrms_jobs with role jobs_owner"
psql_su --dbname postgres <<SQL
CREATE ROLE jobs_owner LOGIN PASSWORD '${HRMS_DB_PASSWORD}' NOSUPERUSER NOCREATEDB NOCREATEROLE NOBYPASSRLS;
CREATE DATABASE hrms_jobs OWNER jobs_owner;
REVOKE ALL ON DATABASE hrms_jobs FROM PUBLIC;
SQL
psql_su --dbname hrms_jobs -c "ALTER SCHEMA public OWNER TO jobs_owner;"

echo "HRMS databases ready."
