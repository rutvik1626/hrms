# 04 — API Gateway (YARP)

## Goal
Single public entry point for web and mobile: routing by resource, JWT validation, rate limiting, CORS, security headers, SignalR proxying, and the `/me/bootstrap` aggregation endpoint.

## Read first
`CLAUDE.md`; requirements → Stage 5 "Gateway route map (YARP)", "Mobile-specific endpoints", "Real-time (SignalR)", Stage 6 "Tenant resolution at login", Stage 8 "Navigation" (bootstrap contents).

## Scope
**In**
- `Hrms.Gateway` with YARP routes/clusters from configuration matching the route map; identity's `/connect/*`, `/account/*` (hosted login) passed through unauthenticated.
- JWT validation at the gateway (fail fast) **and** forwarding the token (services still validate).
- Tenant resolution from subdomain for unauthenticated routes (login branding, careers page); for authenticated routes the `tid` claim wins and must match the subdomain when present.
- Strip client-supplied internal headers; add `X-Correlation-Id`, `X-Forwarded-*`.
- Rate limiting (ASP.NET Core rate limiter + Redis): per user, per tenant, stricter per IP on `/connect/token`, OTP and login endpoints; 429 with `Retry-After`.
- CORS: allow-list from config (tenant subdomains pattern + admin portal).
- Request size limits (default 1 MB; uploads go direct to S3 anyway), response compression, security headers.
- WebSockets proxy for `/hubs/notifications`.
- `/internal/*` never routed.
- `GET /api/v1/me/bootstrap`: parallel fan-out to identity (`/me/permissions`, tenant settings, enabled modules, min app version), corehr (profile summary), time (today's shift, geofences), workflow (pending task count); partial-failure tolerant (returns what succeeded + `warnings`), cached per user for 60 s.
- Aggregated `/api/v1/health` (public, minimal) for uptime monitoring.
- Add gateway + identity to the `full` compose profile.

**Out** — production Dockerfile/CI deploy (19).

## Acceptance criteria
- Every route in the map resolves to the right cluster (integration test with stub services).
- Requests without/with invalid token to protected routes → 401 at gateway.
- Rate limits trigger and reset correctly; `/internal/*` returns 404.
- `/me/bootstrap` returns combined payload; with one downstream down it still returns 200 + warning.
- SignalR handshake works through the gateway (test with a stub hub).

## Verify
```bash
dotnet test tests/Gateway.IntegrationTests
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.override.yml --profile full up -d
curl -i http://demo.localhost:<gw-port>/api/v1/health
```

## Ask me first
- Rate-limit numbers if you have commercial plan tiers in mind (otherwise defaults: 300 req/min/user, 3,000 req/min/tenant, 10 req/min/IP on auth).
