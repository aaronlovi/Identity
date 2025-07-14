`
[Games Platform](..\..\..\Games%20Platform.md) > [2025-06](..\..\2025-06.md) > [Identity Context](..\Identity%20Context.md)

# Identity Context Implementation Plan

- [1. Milestone 0: Repo & scaffolding (~30 min)](#key-1-milestone-0-repo-scaffolding-30-min)
- [2. Milestone 1: Data layer (~1 h)](#key-2-milestone-1-data-layer-1-h)
- [3. Milestone 2: Core domain functions (~2 h)](#key-3-milestone-2-core-domain-functions-2-h)
- [4. Milestone 3: REST surface (~1 h)](#key-4-milestone-3-rest-surface-1-h)
- [5. Milestone 4: gRPC introspection (~1 h)](#key-5-milestone-4-grpc-introspection-1-h)
- [6. Milestone 5: Events & outbox (~2 h)](#key-6-milestone-5-events-outbox-2-h)
- [7. Milestone 6: Container & dev-ops (~1 h)](#key-7-milestone-6-container-dev-ops-1-h)
- [8. Next passes](#key-8-next-passes)
- [9. How to work day-to-day](#key-9-how-to-work-day-to-day)
- [10. Bite-sized initial task list](#key-10-bite-sized-initial-task-list)
  - [10.1. Initial task list - Repo & Project scaffolding](#key-10-1-initial-task-list-repo-project-scaffolding)
  - [10.2. Initial task list - Data layer & migrations](#key-10-2-initial-task-list-data-layer-migrations)
  - [10.3. Initial task list - Core domain (identity\_core)](#key-10-3-initial-task-list-core-domain-identity-core)
  - [10.4. Initial task list - Public REST API (identity\_api)](#key-10-4-initial-task-list-public-rest-api-identity-api)
  - [10.5. Initial task list - Internal gRPC API (identity\_grpc)](#key-10-5-initial-task-list-internal-grpc-api-identity-grpc)
  - [10.6. Initial task list - Events & messaging (identity\_infra)](#key-10-6-initial-task-list-events-messaging-identity-infra)
  - [10.7. Initial task list - Outbox & mailer](#key-10-7-initial-task-list-outbox-mailer)
  - [10.8. Initial task list - Rate-limiting & abuse protection](#key-10-8-initial-task-list-rate-limiting-abuse-protection)
  - [10.9. Initial task list - Security hardening](#key-10-9-initial-task-list-security-hardening)
  - [10.10. Initial task list - Observability & alerting](#key-10-10-initial-task-list-observability-alerting)
  - [10.11. Initial task list - Backup & disaster recovery](#key-10-11-initial-task-list-backup-disaster-recovery)
  - [10.12. Initial task list - Containerization & dev environment](#key-10-12-initial-task-list-containerization-dev-environment)
  - [10.13. Initial task list - Compliance & retention](#key-10-13-initial-task-list-compliance-retention)
  - [10.14. Initial task list - Future backlog (icebox)](#key-10-14-initial-task-list-future-backlog-icebox)

# 1. Milestone 0: Repo & scaffolding (~30 min)

|  **Task**            |  **Command / pointer**                                                                                                                                                                                                                                                                    |
|:---------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Create solution      | `dotnet new sln -n Identity`                                                                                                                                                                                                                                                              |
| Add projects         | `dotnet new webapi -n Identity.Api` (REST) `dotnet new classlib -n Identity.Domain` `dotnet new classlib -n Identity.GrainInterfaces` `dotnet new classlib -n Identity.Grains` `dotnet new grpc -n Identity.Grpc` `dotnet new classlib -n Identity.Infrastructure` (Kafka, Email, Outbox) |
| Reference projects   | `dotnet sln add **/*.csproj`; then `dotnet add Identity.Api reference Identity.Domain…`                                                                                                                                                                                                   |
| Add Orleans packages | `dotnet add Identity.GrainInterfaces package Microsoft.Orleans.Core`; `dotnet add Identity.Grains package Microsoft.Orleans.Server`                                                                                                                                                       |
| Configure Startup    | In **Identity.Api**, add `builder.UseOrleans()` to host builder; wire middleware pipeline                                                                                                                                                                                                 |

**Structure**

```java
src/
  Identity.Api/
  Identity.Domain/
  Identity.GrainInterfaces/
  Identity.Grains/
  Identity.Grpc/
  Identity.Infastructure/
```

---

# 2. Milestone 1: Data layer (~1 h)

1. Write DDL scripts under **db/migrations/** (PostgreSQL).
2. Expose repository interfaces (IUserRepository, ICredentialRepository, …) in **Identity.Domain**.
3. Provide Npgsql-based implementations in **Identity.Infrastructure**.
4. Add unit tests with Test containers for .NET to spin up Postgres*.*

---

# 3. Milestone 2: Core domain functions (~2 h)

|  Function                             |  Notes                                                                                                   |
|:--------------------------------------|:---------------------------------------------------------------------------------------------------------|
| `CreateUserAsync`                     | Hashes password with `PasswordHasher<T>` (PBKDF2) *or* `Isopoh.Cryptography.Argon2` depending on config. |
| `VerifyPasswordAsync`                 | Compares hash                                                                                            |
| `CreateSessionAsync`                  | Persists session row, returns `TokenPair` (JWT HS256 created via `System.IdentityModel.Tokens.Jwt`).     |
| `RefreshSessionAsync`                 | Rotates refresh token, updates session                                                                   |
| `RevokeSessionAsync / RevokeAllAsync` | Marks session(s) revoked                                                                                 |
| `GoogleOAuthAsync`                    | Validates Google token via `GoogleJsonWebSignature`                                                      |

Sessions & rate-limit counters are hosted in dedicated Orleans grains (`SessionGrain`, `RateLimiterGrain`).

---

# 4. Milestone 3: REST surface (~1 h)

1. Minimal-API routes in **Identity.Api** (`/v1/auth/*`)

   ```c#
   app.MapPost("/v1/auth/login", (LoginDto dto, IAUthService svc) => ...);
   ```
2. Return RFC 7807 error objects via `ProblemDetails` middleware
3. Use `[EnableRateLimiting(“auth”)]` policy backed by an Orleans rate-limit grain

*Hit endpoints with curl/Postman; tokens should parse on* <http://jwt.io> *.*

---

# 5. Milestone 4: gRPC introspection (~1 h)

1. Keep original `identity.proto`; place under **protos/**.  
   Compile with `Grpc.Tools` (`dotnet build` auto-generates C# stubs).
2. Implement services in **Identity.Grpc** forwarding to Orleans calls.

*Spin up a second app as a dummy client; call ValidateTokenAsync and assert user context returns.*

---

# 6. Milestone 5: Events & outbox (~2 h)

|  **Task**    |  **Pointer**                                                                                |
|:-------------|:--------------------------------------------------------------------------------------------|
| Outbox table | `email_outbox` & `domain_event_outbox` tables in Postgres.                                  |
| Outbox pump  | BackgroundService (**Identity.Workers**) reads unsent rows and publishes via Kafka producer |
| Kafka        | Docker service with `identity.events.v1` topic (6 partitions).                              |
| Email        | Use SendGrid SDK; dev uses `Smtp4Dev` docker image.                                         |

---

# 7. Milestone 6: Container & dev-ops (~1 h)

|  **Task**      |  **Command**                                                                                     |
|:---------------|:-------------------------------------------------------------------------------------------------|
| Builder stage  | `FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build … dotnet publish -c Release -o /app/publish`     |
| Runtime stage  | `FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime … ENTRYPOINT [“dotnet”,”Identity.Api.dll”]` |
| docker-compose | Postgres 17, Kafka, identity-api; health-checks on `/healthz/ready`                              |
| K8s            | Deployment (replicas = 2), HPA CPU=80%, *PodDisruptionBudget* min 1.                             |

*Run* `docker compose up`*; curl inside container.*

---

# 8. Next passes

- Add OAuth login
- MFA (TOTP/WebAuthn)
- Admin UI
- SPIFFE/SPIRE mTLS
- Replace curl mailer with real ESP API key.
- Wire into Envoy in your main cluster.

---

# **9. How to work day-to-day**

1. **Branch per milestone** → PR → merge to `main`.
2. Keep all unit tests passing; each merge deploys a fresh container to your staging environment.
3. When milestone 6 is solid, hand endpoints to the mobile client team; keep iterating on rate-limits and observability

---

# 10. Bite-sized initial task list

## 10.1. Initial task list - Repo & Project scaffolding

|  **ID**      |  **One- or Two-liner Task Description (translated to .NET)**                                                                                                                        |    **Est (hrs)**  |
|:-------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------:|
| **SETUP-01** | `dotnet new sln -n Identity` → commit `.gitignore`, `.editorconfig`, `README.md`.                                                                                                   |              0.25 |
| **SETUP-02** | Add projects: `Identity.Api` (web API), `Identity.Domain`, `Identity.GrainInterfaces`, `Identity.Grains`, `Identity.Grpc`, `Identity.Infrastructure`; `dotnet sln add **/*.csproj`. |              0.5  |
| **SETUP-03** | Wire up Orleans host in `Program.cs`: `builder.Host.UseOrleans(silo => silo.UseLocalhostClustering());` + DI registrations for Domain & Infrastructure.                             |              0.5  |
| **SETUP-04** | Write multi-stage `Dockerfile` (`sdk:8.0` build → `aspnet:8.0` runtime) plus `.dockerignore`; verify `docker build .` succeeds.                                                     |              0.75 |
| **SETUP-05** | GitHub Actions workflow: restore, build, unit-test, `docker build` & push to `ghcr.io/…/identity-api:sha`.                                                                          |              0.5  |

|  **ID**    |  **Description**                   |    **Est (h)**  |
|:-----------|:-----------------------------------|----------------:|
| SETUP-01   | `dotnet new sln` + add projects    |            0.25 |
| SETUP-02   | Baseline Orleans host boiler-plate |            0.5  |
| SETUP-03   | Dockerfile multi-stage             |            0.75 |
| SETUP-04   | GitHub CI YAML workflow            |            0.5  |

## 10.2. Initial task list - Data layer & migrations

|  **ID**    |  **One- or Two-liner Description**                                                                                                                                               |    **Estimate (hrs)**  |
|:-----------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------:|
| **DL-01**  | **Baseline migration**: `CREATE SCHEMA identity;` and run an Evolve `Init.sql` that sets `search_path = identity;public;`                                                        |                   0.25 |
| **DL-02**  | Migration **users**: `id UUID PK`, `status text`, `locale text`, `kyc_state text`, `self_excluded_until :utc_datetime`, timestamps.                                              |                   0.5  |
| **DL-03**  | Migration **profiles**: `user_id` 1-to-1 FK, `display_name text`, `avatar text`, `timezone text`, timestamps + unique `(user_id)`.                                               |                   0.5  |
| **DL-04**  | Migration **roles**: `user_id` FK, `role text`, timestamps.                                                                                                                      |                   0.3  |
| **DL-05**  | Migration **credentials**: `user_id` FK, `type text`, `external_id text`, `hash text`, `mfa_secret text`, timestamps + unique `(type, external_id)`.                             |                   0.5  |
| **DL-06**  | Migration **devices**: `user_id` FK, `device_id UUID`, `first_ip inet`, `ua text`, timestamps.                                                                                   |                   0.4  |
| **DL-07**  | Migration **sessions**: `user_id` & `device_id` FK, `jwt_id UUID`, `revoked_at utc_datetime`, `exp utc_datetime`, timestamps + unique `(user_id, device_id, jwt_id)`.            |                   0.5  |
| **DL-08**  | Migration **login\_events**: `user_id` FK, `credential_type text`, `ip inet`, `success boolean`, timestamps.                                                                     |                   0.5  |
| **DL-09**  | Migration **password\_reset\_tokens**: `credential_id` FK, `token UUID`, `expires_at utc_datetime`, `delivery_channel text`, `masked_destination text`, timestamps.              |                   0.4  |
| **DL-10**  | Migration **email\_outbox**: `id UUID PK`, `payload JSONB`, `sent_at utc_datetime`, `fail_count integer default 0`, timestamps.                                                  |                   0.4  |
| **DL-11**  | Enforce all FKs with `ON DELETE CASCADE` where child data makes no sense without parent; add the uniques noted above.                                                            |                   0.4  |
| **DL-12**  | **Seed script**: a small `dotnet run --project Tools.Seeder` that inserts the first `super_admin` (user + credential + role) after migrations succeed.                           |                   0.5  |
| **DL-13**  | **xUnit integration tests**: spin up Postgres with Testcontainers, apply Evolve migrations, insert sample rows in every table, read them back, assert round-trip & FK integrity. |                   1.5  |

## 10.3. Initial task list - Core domain (`identity_core`)

|  **ID**      |  **One- or Two-liner Task Description**                                                                                                                            |    **Est (hrs)**  |
|:-------------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------:|
| **CORE-01a** | Add **Isopoh.Cryptography.Argon2** NuGet package to *Identity.Domain* and run `dotnet restore`.                                                                    |               0.5 |
| **CORE-01b** | Create `Bench/Argon2Bench.cs` using **BenchmarkDotNet** to tune `Iterations` / `MemoryCost`; store chosen values in `appsettings.json`.                            |               1   |
| **CORE-02**  | `CreateUserAsync`: hash password, insert **users + credentials** in one transaction via your DAL, publish **UserCreated** event to a Kafka stream through Orleans. |               1.5 |
| **CORE-03**  | `VerifyPasswordAsync`: fetch credential, `Argon2.Verify()`, return `(User, Success)` or `InvalidCredentialError`.                                                  |               0.5 |
| **CORE-04a** | `CreateSessionAsync`: insert **sessions** row with `JwtId`, `Expires`, `DeviceId` (FKs checked).                                                                   |               1   |
| **CORE-04b** | Sign access & refresh JWTs (HS256) using **Microsoft.IdentityModel.Tokens**; key comes from `JWT_SECRET` env-var.                                                  |               0.5 |
| **CORE-04c** | Return `TokenPair { Access, Refresh, Expires }`; default TTLs = 15 min (access) / 30 days (refresh).                                                               |               0.5 |
| **CORE-05**  | `RefreshSessionAsync`: validate refresh token, rotate pair, mark old session `RevokedAt = now()`.                                                                  |               1   |
| **CORE-06a** | `RevokeSessionAsync`: set `RevokedAt`, add `jti` to **JwtBlacklistGrain** for O(1) checks.                                                                         |               0.5 |
| **CORE-06b** | `RevokeAllAsync`: revoke every active session for a user in a single SQL update and clear grain cache.                                                             |               0.5 |
| **CORE-07a** | `VerifyGoogleTokenAsync`: use `GoogleJsonWebSignature.ValidateAsync`, extract `sub`, `email`, `verified`.                                                          |               1   |
| **CORE-07b** | Auto-provision user with credential `Type = "google_oauth"` when `sub` unseen; else link & log in.                                                                 |               0.5 |
| **CORE-08a** | `LinkCredentialAsync`: add extra credential, enforce unique `(Type, ExternalId)` constraint.                                                                       |               0.5 |
| **CORE-08b** | `UnlinkCredentialAsync`: remove credential if ≥ 1 remain, else raise `LastCredentialError`.                                                                        |               0.5 |
| **CORE-09**  | `GenerateResetTokenAsync`: insert **password\_reset\_tokens** row, choose channel (email), store masked destination.                                               |               0.5 |
| **CORE-10**  | `CompletePasswordResetAsync`: validate token, set new Argon2 hash, **RevokeAllAsync** for user.                                                                    |               1   |
| **CORE-11**  | `CheckShadowBanAsync`: gate auth flows; add `ShadowBanned` bool to user DTO.                                                                                       |               0.5 |
| **CORE-12a** | **JwtBlacklistGrain**: in-memory hash-set of revoked `jti`s with TTL eviction (extends Orleans `Grain` base).                                                      |               0.5 |
| **CORE-12b** | **RateLimiterGrain**: sliding-window counters keyed by `IP` and `CredentialId` (10 failures / 10 min).                                                             |               1   |
| **CORE-13a** | `AuditLogger.LogAsync`: insert **login\_events** row inside same DB transaction.                                                                                   |               0.5 |
| **CORE-13b** | Publish Kafka message to topic `identity.events.v1` (6 partitions, key = `UserId`) via **Confluent.Kafka** producer in `EventPublisher`.                           |               0.5 |

## 10.4. Initial task list - Public REST API (`identity_api`)

|  **ID**     |  **One- or Two-liner Task Description (translated to .NET)**                                                                                                      |    **Estimate (hrs)**  |
|:------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------:|
| **API-01a** | `dotnet new webapi -n Identity.Api`; set listener via `ASPNETCORE_URLS` (default `http://*:8080`).                                                                |                    0.5 |
| **API-01b** | Add **Minimal-API** group: `var v1 = app.MapGroup("/v1");` with `v1.MapPost(...)` etc.                                                                            |                    1   |
| **API-01c** | Configure middleware pipeline: `UseRouting → UseCors → UseAuthentication → UseAuthorization → UseRateLimiter → MapEndpoints`.                                     |                    0.5 |
| **API-01d** | Add CORS policy `AllowAnyOrigin, AllowAnyHeader`; add <http://ASP.NET> Core **Antiforgery** with double-submit cookie for state-changing routes.                  |                    0.5 |
| **API-02**  | `POST /v1/auth/register` → `IAuthService.CreateUserAsync`; set `Secure; HttpOnly; SameSite=None` cookies for **access**+**refresh** JWTs; return **201 Created**. |                    1.5 |
| **API-03**  | `POST /v1/auth/login` → password flow; same cookie logic; **200 OK**.                                                                                             |                    1   |
| **API-04**  | `POST /v1/auth/oauth/google` → `GoogleAuthService.LoginAsync`; auto-provision user; **200 OK**.                                                                   |                    1   |
| **API-05**  | `POST /v1/auth/refresh` → validate refresh cookie, rotate pair, issue new cookies; **200 OK**.                                                                    |                    0.5 |
| **API-06**  | `POST /v1/auth/logout` → `SessionGrain.RevokeAsync`; clear cookies; **204 No Content**.                                                                           |                    0.5 |
| **API-07**  | `POST /v1/auth/logout_all` → `SessionGrain.RevokeAllAsync`; clear cookies; **204 No Content**.                                                                    |                    0.5 |
| **API-08**  | `POST /v1/auth/password/forgot` → `PasswordResetService.GenerateTokenAsync`, enqueue email; **202 Accepted**.                                                     |                    0.5 |
| **API-09**  | `POST /v1/auth/password/reset` → consume token, set new hash; **204 No Content**.                                                                                 |                    0.5 |
| **API-10**  | `GET /v1/profile/me` returns profile DTO; `PUT` updates profile via validator.                                                                                    |                    1   |
| **API-11**  | `POST /v1/credentials/link` add/remove secondary credential; enforce ownership via `[Authorize]`.                                                                 |                    1   |
| **API-12a** | Implement `ProblemDetailsFactory` subclass producing RFC 7807 (`type`, `title`, `status`, `detail`).                                                              |                    0.5 |
| **API-12b** | Global exception/validation middleware returns those `ProblemDetails` objects.                                                                                    |                    0.5 |
| **API-13a** | `RateLimiterMiddleware` queries **RateLimiterGrain**; on throttle returns **429 Too Many Requests** with `Retry-After`.                                           |                    1   |
| **API-13b** | Insert the middleware before `/auth/*` group (`app.UseRateLimiter()` just before `MapGroup`).                                                                     |                    0.5 |
| **API-14a** | xUnit + `WebApplicationFactory` endpoint tests (happy & error paths, CORS pre-flight, CSRF).                                                                      |                    2   |
| **API-14b** | Provide `scripts/auth_demo.ps1` + `auth_demo.sh` curl scripts covering register → login → refresh → logout.                                                       |                    0.5 |

## 10.5. Initial task list - Internal gRPC API (`identity_grpc`)

|  **ID**      |  **One- or Two-liner Task Description (translated to .NET)**                                                                                                                      |    **Est (hrs)**  |
|:-------------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------:|
| **GRPC-01a** | Create `protos/identity/common.proto` with shared messages (`User`, `TokenPair`, `Error`, `Empty`).                                                                               |               0.5 |
| **GRPC-01b** | Create `protos/identity/auth.proto` defining `AuthService` RPCs: `Register`, `Login`, `Refresh`, `PasswordResetInit`, `PasswordResetComplete`.                                    |               0.5 |
| **GRPC-01c** | Create `protos/identity/identity.proto` defining `IdentityService` RPCs: `ValidateToken`, `RevokeSession`, `RevokeAllSessions`, `CreateSystemSession`.                            |               0.5 |
| **GRPC-02a** | Implement `AuthService` handler class in **Identity.Grpc**; inject `IAuthService` from domain and map domain exceptions → `StatusCode.ALREADY_EXISTS`, `UNAUTHENTICATED`.         |               1.5 |
| **GRPC-02b** | Implement password-reset flows in the handler; translate validation errors → `INVALID_ARGUMENT`, missing user / token → `NOT_FOUND`.                                              |               1   |
| **GRPC-03**  | Implement `IdentityService` handler that delegates to Orleans grains (`SessionGrain`, `JwtBlacklistGrain`) for token validation & revocation.                                     |               1.5 |
| **GRPC-04a** | Add NuGet packages to **Identity.Grpc**: `Grpc.AspNetCore`, `Google.Protobuf`, `Grpc.Tools`; enable `<Protobuf Include="protos/**/*.proto" GrpcServices="Server" />` in .csproj.  |               0.5 |
| **GRPC-04b** | Expose gRPC endpoint in **Identity.Api**: `app.MapGrpcService<AuthService>()`; read port from `GRPC_PORT` env-var; enable reflection for Postman/gRPC-UI.                         |               0.5 |
| **GRPC-05a** | Create `Tools/GrpcSmokeTest.cs`: a console client using `Grpc.Net.Client` that exercises register → login → refresh → logout against a local server.                              |               0.5 |
| **GRPC-05b** | Add integration test project (`Identity.Api.Tests`) that spins up server via `WebApplicationFactory`, calls gRPC services, and asserts contract compliance (happy & error paths). |               1   |

## 10.6. Initial task list - Events & messaging (`identity_infra`)

|  **ID**     |  **One- or two-liner task description (translated to .NET)**                                                                                                                                                                       |    **Est (hrs)**  |
|:------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------:|
| **EVT-01a** | Create `protos/identity/event.proto` with `enum EventType { LOGIN_SUCCEEDED = 11; LOGIN_FAILED = 12; PASSWORD_RESET = 21; … }` and wrapper `message IdentityEvent { string user_id = 1; EventType type = 2; bytes payload = 3; }`. |               0.5 |
| **EVT-01b** | Add `Grpc.Tools` to *Identity.Infrastructure*; `<Protobuf Include="protos/**/*.proto" GrpcServices="None" />`; run `dotnet build` to generate C# event DTOs and commit.                                                            |               0.5 |
| **EVT-02**  | Implement `KafkaEventProducer` (wrap **Confluent.Kafka** producer) with `Task PublishAsync<T>(IdentityEvent evt, CancellationToken ct)`. Reads brokers from `KAFKA_BOOTSTRAP`; serialises with Protobuf.                           |               1   |
| **EVT-03**  | Instrument AuthService flows to emit **LoginSucceeded / LoginFailed / PasswordResetRequested** by calling `KafkaEventProducer.PublishAsync` inside the same transaction scope.                                                     |               1   |
| **EVT-04a** | Configure producer in DI at host start-up: `enable.idempotence=true`, `acks=all`, `compression.type=zstd`, topic `identity.events.v1` (6 partitions, key = `UserId`).                                                              |               0.5 |
| **EVT-04b** | Wrap `PublishAsync` in an **OpenTelemetry** `Activity` (`Identity.Event.Publish`) and record histogram `event_publish_latency_ms`; export via Prometheus scraper endpoint.                                                         |               0.5 |
| **EVT-05a** | Extend `docker-compose.yml` with a Redpanda (or Confluent Kafka) service, health-check `/v1/health/ready`; inject env-vars into `identity-api` container.                                                                          |               0.5 |
| **EVT-05b** | xUnit integration test: spin up Kafka via **Testcontainers**, start a consumer, call `KafkaEventProducer.PublishAsync`, assert the message arrives with the correct key/value.                                                     |               1   |

## 10.7. Initial task list - Outbox & mailer

|  **ID**      |  **One- or Two-liner Task Description (translated to .NET)**                                                                                    |    **Est (hrs)**  |
|:-------------|:------------------------------------------------------------------------------------------------------------------------------------------------|------------------:|
| **MAIL-01**  | When `GenerateResetTokenAsync` succeeds, insert a row into **email\_outbox** table: `{template: "password_reset", user_id, destination, vars}`. |               0.5 |
| **MAIL-02a** | Implement `OutboxPoller` **BackgroundService** (hosted in ***Identity.Infrastructure***): every 10 s fetch ≤ 10 rows where `sent_at IS NULL`.   |               1   |
| **MAIL-02b** | For each row, resolve `IEmailSender` (see MAIL-04a); in **dev** use **Smtp4Dev** to write an HTML preview; on success update `sent_at = NOW()`. |               0.5 |
| **MAIL-03**  | On exception, increment `fail_count`; when `fail_count > 5`, set `dead_letter = TRUE` and log `ILogger<OutboxPoller>.LogWarning(payload)`.      |               0.5 |
| **MAIL-04a** | Read `MAIL_ADAPTER` env-var (`local`, `sendgrid`, `mailgun`) and register the corresponding `IEmailSender` implementation in DI at startup.     |               0.5 |
| **MAIL-04b** | Load API keys (`SENDGRID_API_KEY`, `MAILGUN_API_KEY`) from env-vars; list required variables in **README.md / docs/env.md**.                    |               0.5 |

## 10.8. Initial task list - Rate-limiting & abuse protection

|  **ID**    |  **One- or Two-liner Task Description (translated to .NET)**                                                                                                      |    **Est (hrs)**  |
|:-----------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------:|
| **RL-01a** | Add `infra/envoy/ip_rate_limit.yaml`: **100 GET /min**, **10 POST /min** per client IP (leaky-bucket).                                                            |               0.5 |
| **RL-01b** | Reference that file in Envoy bootstrap (`http_filters → envoy.filters.http.ratelimit`) and hot-reload the sidecar.                                                |               0.5 |
| **RL-02a** | Implement **CredentialRateLimiterGrain** (key = `CredentialId`; sliding window 5 failures / 2 min; in-memory state, clears via `RegisterTimer`).                  |               1   |
| **RL-02b** | In `AuthService.LoginAsync`, call `CredentialRateLimiterGrain.IncrementAsync(id)` on failure; when count ≥ 5 return **423 Locked**.                               |               0.5 |
| **RL-03**  | Implement **DeviceRateLimiterGrain** keyed by `DeviceId`; allow 10 failed logins in a 10-min rolling window.                                                      |               1   |
| **RL-04a** | If either limiter trips twice within 1 h, set `NeedsCaptcha` flag on the current **SessionGrain** (and persist to DB).                                            |               0.5 |
| **RL-04b** | In `POST /v1/auth/login`, when `NeedsCaptcha` is true, verify Google reCAPTCHA v3 token via `Google.Apis.RecaptchaEnterprise.v1`; reject with **400** if invalid. |               0.5 |
| **RL-05**  | Increment OpenTelemetry counter `identity_login_failed_total{reason="invalid_password"}` on every failed attempt; expose via Prometheus scrape endpoint.          |               0.5 |

**Assumptions**

- Using Envoy’s built-in *HTTP global rate limit* filter; Redis available at `REDIS_URL` for credential buckets.
- Google reCAPTCHA v3 for CAPTCHA gate (swap easily later).

## 10.9. Initial task list - Security hardening

|  **ID**     |  **One- or Two-liner Task Description (translated to .NET)**                                                                                                                 |    **Est (hrs)**  |
|:------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------:|
| **SEC-01a** | Add `JwtKeyRing` singleton (backed by `IMemoryCache`) holding **current** key + two **previous** 256-bit HS256 secrets; inject `IJwtSigner` to APIs & grains.                |               1   |
| **SEC-01b** | Create `JwtKeyRotationWorker` **IHostedService** (or Orleans **Reminder**) that runs daily at 03:00 UTC: generates new 256-bit secret, pushes to key ring, drops the oldest. |               1   |
| **SEC-01c** | Implement `GET /.well-known/jwks.json` in **Identity.Api** that emits JWK set for the three active HS256 keys (kty = “oct”, kid = `yyyyMMdd`).                               |               0.5 |
| **SEC-02a** | Helm-deploy single-node **SPIRE Server** (`trust_domain = "identity.local"`) and `spire-bundle.yaml` into the cluster.                                                       |               2   |
| **SEC-02b** | Add `spire-agent` side-car to Identity deployment; obtain SVID `spiffe://identity.local/service/identity`; mount cert & key via Unix domain socket.                          |               1   |
| **SEC-02c** | Configure Envoy SDS to pull certs from the agent and enforce mTLS (`require_client_certificate = true`) on **all** incoming HTTP + gRPC listeners.                           |               1   |
| **SEC-03a** | In `RolesService.GrantAsync(user, "super_admin")` refuse unless `user.TotpSecret` is non-null (enforced in DB and grain logic).                                              |               0.5 |
| **SEC-03b** | Evolve migration: `ALTER TABLE users ADD COLUMN totp_secret bytea NULL;` encrypt/decrypt at rest with **NetTopologySuite.DataProtection** or pgcrypto (`pgp_sym_encrypt`).   |               1   |

**Assumptions**

- JWTs still HS256; key rotation handled entirely in-app.
- SPIRE provides 24 h SVIDs; you renew automatically via agent.
- `totp_secret` is envelope-encrypted before storage.

## 10.10. Initial task list - Observability & alerting

|  **ID**     |  **One- or Two-liner Task Description (translated to .NET)**                                                                                                               |    **Est (hrs)**  |
|:------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------:|
| **OBS-01a** | Add NuGet packages **OpenTelemetry.Extensions.Hosting**, **OpenTelemetry.Exporter.Otlp**; configure OTLP exporter to a local collector (`OTEL_EXPORTER_OTLP_ENDPOINT`).    |               0.5 |
| **OBS-01b** | Instrument <http://ASP.NET> Core middleware & `Identity.Domain` auth methods with `ActivitySource` spans; set sampler `ParentBasedTraceIdRatioSampler(0.05)` ≈ 5 % traces. |               1   |
| **OBS-02a** | Add **prometheus-net.AspNetCore**; enable default CLR/ASP metrics and create `Histogram login_latency_ms` (buckets = 50, 100, 200, 300, 500).                              |               0.5 |
| **OBS-02b** | In `LoginEndpoint`, start `using var metric = loginLatency.NewTimer();` and expose `/metrics` via `app.UseMetricServer();`.                                                |               0.5 |
| **OBS-03**  | Commit Grafana/Prometheus alert rule: `histogram_quantile(0.99, rate(login_latency_ms_bucket[5m])) > 0.3` for 5 min (p99 > 300 ms).                                        |               0.5 |
| **OBS-04a** | Switch logging to **Serilog** with `WriteTo.Console(new RenderedCompactJsonFormatter())`; include `timestamp, level, message, requestId, sessionId`.                       |               0.5 |
| **OBS-04b** | Add **loki** + **promtail** services to `docker-compose.yml`; promtail tail `/var/log/containers/*identity*.log` and ship to Loki.                                         |               1   |
| **OBS-04c** | Configure Grafana Loki data-source; create Explore shortcut query `requestId="<guid>"` to correlate traces ↔ logs.                                                         |               0.5 |

## 10.11. Initial task list - Backup & disaster recovery

|  **ID**    |  **One- or Two-liner Task Description (translated to .NET / K8s)**                                                                                                                                      |  **Est (hrs)**    |
|:-----------|:--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------|
| **DR-01a** | Keep **wal-g** for Postgres 16; add `wal_level = replica`, `archive_mode = on`, `archive_command = 'wal-g wal-push %p'` to the *postgres-config* ConfigMap.                                             | 1                 |
| **DR-01b** | Create S3 (or MinIO) bucket **identity-db-backups** with 30-day lifecycle policy; store creds in K8s `Secret/postgres-walg-aws`.                                                                        | 0.5               |
| **DR-01c** | Add `wal-g.yaml` ConfigMap pointing to `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `WALG_S3_PREFIX=s3://identity-db-backups`. Mount into Postgres container at `/etc/wal-g.yaml`.                     | 0.5               |
| **DR-01d** | Kubernetes **CronJob**: `wal-g backup-push /var/lib/postgresql/data` daily at 02:15 UTC; `successfulJobsHistoryLimit: 3`, `failedJobsHistoryLimit: 3`.                                                  | 0.5               |
| **DR-01e** | Write run-book section “PITR restore”: `wal-g backup-fetch /tmp/restore LATEST` → `wal-g wal-fetch --pitr-target='2025-07-10T23:59:00Z'` → start Postgres in recovery; test to *yesterday’s* timestamp. | 1                 |
| **DR-02a** | Deploy **MirrorMaker 2** (Confluent image) as StatefulSet; `replication.factor=1`, `config.storage.replication.factor=1`; bootstrap to primary `redpanda:9092`.                                         | 1                 |
| **DR-02b** | Provision secondary Kafka cluster **redpanda-backup** (single node) in a different zone/namespace; expose `PLAINTEXT://redpanda-backup:9092`.                                                           | 1                 |
| **DR-02c** | MirrorMaker config: `topics = identity.*`, `replication.policy.class = org.apache.kafka.connect.mirror.DefaultRep\*                                                                                     |                   |

## 10.12. Initial task list - Containerization & dev environment

|  **ID**     |  **One- or Two-liner Task Description (C# / Orleans)**                                                                                                |  **Est (hrs)**    |
|:------------|:------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------|
| **DEV-01a** | **Builder stage** in `Dockerfile`: `FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build` → `dotnet publish -c Release -o /app/publish` for *Identity.Api*. | 1                 |
| **DEV-01b** | **Runtime stage**: `FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime`; copy publish folder; `ENTRYPOINT ["dotnet","Identity.Api.dll"]`.            | 0.5               |
| **DEV-01c** | Add `.dockerignore` (`bin/`, `obj/`, `.vs/`, `*.user`, `*.suo`, `**/TestResults/`, `**/*.md`, etc.).                                                  | 0.5               |
| **DEV-02a** | Write `docker-compose.yml` with services: **postgres 16**, **redpanda (Kafka)**, **identity-api** (build `.`); identity depends\_on both.             | 1                 |
| **DEV-02b** | Add identity health-check: `CMD curl -f <http://localhost:8080/healthz/ready>                                                                         |                   |
| **DEV-02c** | Optional **init container** (or `depends_on` entrypoint) running `dotnet Identity.Tools.Migrator.dll` to apply Evolve migrations before app starts.   | 0.5               |
| **DEV-03a** | Create K8s **Deployment** `identity-api` (replicas = 2) with env-vars (`POSTGRES_URL`, `KAFKA_BOOTSTRAP`, `OTEL_EXPORTER_OTLP_ENDPOINT`).             | 1                 |
| **DEV-03b** | Add ClusterIP **Service** exposing port 8080 (`REST`) and 5001 (`gRPC`) to other cluster workloads.                                                   | 0.5               |
| **DEV-03c** | Create **HorizontalPodAutoscaler**: min 2, max 8, target CPU 75 %, scale based on `nginx.ingress.kubernetes.io/metrics`.                              | 0.5               |
| **DEV-03d** | Add **PodDisruptionBudget** with `minAvailable: 1` for the identity Deployment to keep at least one pod during drain/upgrade.                         | 0.5               |

## 10.13. Initial task list - Compliance & retention

|  **ID**      |  **One- or Two-liner Task Description (translated to .NET)**                                                                                                                                 |    **Est (hrs)**  |
|:-------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|------------------:|
| **COMP-01a** | Add `DELETE /v1/profile/me` Minimal-API route; `[Authorize]` → enqueue `UserEraseGrain.StartAsync(userId)`; respond **202 Accepted**.                                                        |               1   |
| **COMP-01b** | **UserEraseGrain** (Saga orchestrator): set `users.status = "pending_erase"`, publish **UserEraseRequested** Kafka event.                                                                    |               1.5 |
| **COMP-01c** | **UserEraseWorker** (BackgroundService/Hangfire job) pulls pending IDs → scrub PII in `profiles`, `credentials`, `devices`, `sessions`; anonymise `login_events`; emit **UserErased** event. |               1.5 |
| **COMP-02a** | Schedule daily 03:00 UTC **RetentionReminder** (Orleans reminder) or Hangfire cron `@daily` that calls `RetentionService.PurgeAsync()`.                                                      |               0.5 |
| **COMP-02b** | `PurgeAsync`: delete expired `password_reset_tokens`, `sessions` > 90 d, `login_events` > 180 d; log row counts.                                                                             |               1   |
| **COMP-03a** | Provision dedicated PV/PVC for `/var/log/identity`; mount with `ReadWriteOnce`; enable **file-system encryption** (e.g., dm-crypt/LUKS) via init container.                                  |               1   |
| **COMP-03b** | For cloud volumes, enforce storage-class encryption with **KMS-backed** keys (AWS EBS CMK / GCP CMEK / Azure Disk KEK) for audit-log PVC.                                                    |               0.5 |

## 10.14. Initial task list - Future backlog (icebox)

- Add Apple/FB/X OAuth providers.
- MFA flows (TOTP + WebAuthn).
- Friend-graph micro-service & follow lists.
- KYC integration and “real-money” flag promotion.
- Tamper-evident HMAC on Kafka envelopes.
