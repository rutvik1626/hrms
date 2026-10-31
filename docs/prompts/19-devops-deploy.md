# 19 — DevOps: containers, AWS, CI/CD, deployment

## Goal
Production-grade images, staging and prod environments on AWS Mumbai via Terraform, GitHub Actions pipelines with approval-gated deploys, secrets, logging/alerting — so every merge reaches staging automatically and prod with one approval.

## Read first
`CLAUDE.md`; requirements → Stage 9 (all), Stage 4 "Migrations", "Backup & recovery", Stage 10 "CI security gates".

Run **early (after 04)**, then re-run the **"Extend"** section whenever a service is added.

## Scope
### Part A — Images & compose
- Multi-stage Dockerfiles for gateway + each service (chiseled runtime, non-root, read-only fs), migrator images (`dotnet ef migrations bundle`), web (node build → nginx-unprivileged with SPA fallback, caching, CSP/HSTS headers).
- `docker-compose.staging.yml`, `docker-compose.prod.app.yml`, `docker-compose.prod.infra.yml` per Stage 9 (limits, restart policy, log rotation, migrator → service ordering).
- `deploy/scripts/`: `deploy.sh` (pull, migrate, up changed services, wait `/health/ready`, smoke), `smoke-test.sh`, `rollback.sh <sha>`, `seed-demo.sh`.

### Part B — Terraform (AWS ap-south-1)
- Modules: network (VPC, public/private subnets, SGs per Stage 9), compute (EC2 app + infra with IAM instance profile, SSM agent, CloudWatch agent, Docker install via user-data), RDS PostgreSQL 16 (encrypted, PITR, Multi-AZ toggle, parameter group, per-service roles created by bootstrap script), S3 (files bucket: private, versioning, SSE, lifecycle incl. selfies 90 days; backups bucket; CRR toggle), ECR repos + lifecycle (keep 10), ALB + target groups + listener rules (path routing) + ACM wildcard cert, Route 53 records, SES domain identity, SSM parameters (placeholders), CloudWatch alarms + billing alarm, GitHub OIDC IAM role (least privilege).
- Environments `staging` (single VM, Postgres + SeaweedFS S3 containers) and `prod`; remote state in S3 + DynamoDB lock.

### Part C — Pipelines
- Complete `ci-backend.yml`, `ci-web.yml`, `ci-mobile.yml`, `clients.yml`, `security.yml` (CodeQL, dependency audit, Trivy, secret scanning, tfsec/Checkov), `terraform.yml`, `deploy.yml` (build changed images → ECR (SHA tag) → SSM Run Command `deploy.sh` on staging automatically → smoke → ZAP baseline on staging → `prod` environment approval → prod deploy → release tag + changelog).
- Mobile: Fastlane lanes (or Codemagic config) for Play internal track / TestFlight, signing from CI secrets.

### Part D — Observability
- Seq on infra VM (volume, retention 30 days prod / 7 staging, API keys per service, signals + alerts → email/Slack for: error spikes, `_error` queues, job failures, login-failure spikes).
- Uptime monitor config notes; CloudWatch dashboards (EC2, RDS); RabbitMQ/Seq accessible only via SSM port-forward (documented).

### Extend (per new service)
Add Dockerfile + migrator, compose entries, ECR repo, ALB/gateway routes, DB + roles, SSM secrets, pipeline path filter.

## Acceptance criteria
- `terraform plan` clean for both envs; `terraform apply` on staging creates a working environment.
- Merge to `main` deploys to staging automatically; smoke tests pass; prod deploy requires approval and succeeds; rollback to previous SHA works.
- Images run as non-root; Trivy shows no HIGH/CRITICAL; no secrets in images or repo.
- Logs from all services visible in Seq with tenant/correlation ids; alert fires on a forced error.
- Restore drill: RDS PITR to a new instance documented in `docs/runbooks/restore.md`.

## Verify
```bash
cd deploy/terraform/envs/staging && terraform init && terraform plan
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.staging.yml config
```

## Ask me first
- AWS account ids, domain name, and who approves prod deploys.
- Instance sizes/budget ceiling; whether RDS Multi-AZ is on from day one.
