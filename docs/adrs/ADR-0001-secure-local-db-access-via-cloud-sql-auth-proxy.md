# ADR-0001: Secure Local DB Access via Cloud SQL Auth Proxy

## Status

Accepted - 2025-08-03

## Context

Developers need local access to the managed PostgreSQL dev instance (`identity-postgres-dev`) while keeping the database fire‑walled from the public internet. Two approaches exist:

1. **Public IP + authorized networks** — exposes port 5432 and manages an allow‑list of home / office IPs. Easy, but adds attack surface and breaks when IPs change.

2. **Cloud SQL Auth Proxy** — opens a local tunnel using IAM credentials (Application Default Credentials or service‑account key), leaving the instance on a private backend network.

Given our security baseline and rotating developer locations, option #2 is preferred. As a single-developer project initially, we want to establish good security practices from the start that will scale as the team grows.

## Decision

We will adopt **Cloud SQL Auth Proxy (v2)** as the standard way for:

- Local developer connections (HeidiSQL, psql, application code via `localhost:5432`)
- CI/CD jobs that need DB access (via service‑account key)
- Local integration tests against the dev database

The PostgreSQL instance will **not have a public IP**; instead we rely exclusively on the proxy for all external access.

### Implementation Details

- Use `cloud-sql-proxy` binary or Docker container locally
- Connection string format: `Host=localhost;Port=5432;Database=identity_dev;...`
- IAM-based authentication via Application Default Credentials for interactive use
- Service account keys for CI/CD environments

## Consequences

### Positive

- **No open database port** — reduced external attack surface
- **Uses existing Google IAM** — easy to revoke access centrally, no separate database user management
- **Supports both interactive and headless environments** — `gcloud auth application-default login` for developers, service account keys for CI
- **Scales with team growth** — IAM-based access control ready for multiple developers
- **Consistent with GCP best practices** — aligns with Google's recommended security patterns

### Negative

- **Developers must run the proxy locally** — adds extra setup step to development workflow
- **CI pipelines need additional complexity** — requires service account key management and `cloud-sql-proxy` binary or Docker sidecar
- **Slight latency overhead** — approximately 1–2 ms per connection due to proxy layer
- **Additional dependency** — proxy must be running for database access, potential point of failure in dev workflow

## Alternatives Considered

- **VPN into VPC with Cloud SQL private IP** — Most secure but overkill for current single-developer scale. Requires additional GCP networking setup (VPC, firewall rules, VPN gateway) and more complex developer onboarding.

- **Direct Public IP with SSL-only connections** — Simpler setup with just connection string configuration. However, leaves database port exposed to internet even with SSL encryption, and requires managing authorized IP networks.

- **Bastion host / jump server** — Traditional approach with SSH tunneling through a VM. Adds operational overhead of maintaining and securing an additional compute instance.

## References

- [Cloud SQL Auth Proxy Overview](https://cloud.google.com/sql/docs/postgres/sql-proxy)
- [Cloud SQL Auth Proxy v2 Documentation](https://github.com/GoogleCloudPlatform/cloud-sql-proxy)
- [GCP Security Best Practices for Cloud SQL](https://cloud.google.com/sql/docs/postgres/security-best-practices)
