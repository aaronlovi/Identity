# ADR-0002: Separate Firebase Auth Projects per Tenant

## Status

Accepted - 2025-08-03

## Context

The platform will eventually serve two distinct product lines with different user‑bases and risk profiles:

- **Games** — casual multiplayer games; high sign‑up volume, potential for social‑login churn.
- **Edgar** — finance/EDGAR tooling; lower volume but strict data‑access controls.

Firebase Authentication offers a generous free tier, but each project has a hard **1 KB limit on custom claims per ID token**. We anticipate pushing that limit differently for each product, and we want to:

1. **Trial claim payload size** without impacting the other product.
2. **Experiment with different sign‑in providers** (Games may allow Anonymous; Edgar never will).
3. **Rotate credentials or delete the Games database** without touching Edgar users.

Two Firebase projects—Games-Auth and Edgar-Auth—let us do that cleanly.

## Decision

We will provision **one Firebase project per tenant** starting in dev:

- **Games-Auth** (projectId: `games-auth-dev`)
- **Edgar-Auth** (projectId: `edgar-auth-dev`)

Each project has its own:

- Default web app configuration
- Email/password provider enabled
- Google provider enabled
- Anonymous sign‑in remains disabled for now

The Identity service will be configured to work with both projects through environment-based configuration, allowing the same codebase to serve different tenants.

## Consequences

### Positive

- **Isolation** — custom‑claim payload changes or abuse in one tenant cannot affect the other.
- **Provider flexibility** — we can enable/disable providers per product (e.g., Anonymous for Games only).
- **Cleaner dev → prod migration** — can move a tenant independently without affecting the other.
- **Independent scaling** — each product's authentication load is isolated and monitored separately.
- **Risk mitigation** — security incidents or configuration errors in one tenant don't impact the other.

### Negative

- **Double console work** — every provider toggle, configuration change, or security setting must be repeated across projects.
- **Higher MAU tally** — each project's Monthly Active Users count separately toward Firebase's free quota (still negligible at current scale).
- **Future user migration complexity** — if we ever consolidate, we'll need to export/import Auth users between projects.
- **Increased operational overhead** — monitoring, alerting, and maintenance tasks must be duplicated.
- **Configuration drift risk** — settings may accidentally diverge between projects over time.

## Alternatives Considered

| **Option** | **Pros** | **Cons** |
|------------|----------|----------|
| **Single Firebase project** | Simpler setup; one set of quotas; easier to manage; unified user base | Harder to keep claims below 1 KB; risk of config mistakes leaking between tenants; no easy way to wipe one tenant's users; shared rate limits |
| **Tenants via Auth multi‑tenancy (Beta)** | True isolation under one project; unified management console | Beta feature with stability concerns; quotas still shared; claim limit still global; limited provider flexibility |
| **Custom JWT implementation** | Complete control over claims and providers; no Firebase limits | Significant development overhead; security responsibility; loss of Firebase's built-in features and reliability |

## References

- [Firebase Authentication Limits Documentation](https://firebase.google.com/docs/auth/limits)
- [Firebase Custom Claims Documentation](https://firebase.google.com/docs/auth/admin/custom-claims)
- Setup‑guide §11 documents project creation and provider toggles
- Future ADR on cross‑tenant claim strategy will describe the exact payload schema
