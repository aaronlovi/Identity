
[Games Platform](..\..\..\Games%20Platform.md) > [2025-06](..\..\2025-06.md) > [Identity Context](..\Identity%20Context.md)

# Identity Context Design

- [1. Context and Components](#key-1-context-and-components)
  - [1.A. Context and Components - Actors & External Interfaces](#key-1-a-context-and-components-actors-external-interfaces)
  - [1.B. Context and Components - Internal Components](#key-1-b-context-and-components-internal-components)
  - [1.C. Context and Components - Data Stores & Buses](#key-1-c-context-and-components-data-stores-buses)
  - [1.D. Context and Components - High-level Call Flows (text)](#key-1-d-context-and-components-high-level-call-flows-text)
- [2. Identity Data Model](#key-2-identity-data-model)
  - [2.A. Core entities](#key-2-a-core-entities)
  - [2.B. Relationships & cardinalities](#key-2-b-relationships-cardinalities)
  - [2.C. Invariants & constraints (business rules)](#key-2-c-invariants-constraints-business-rules)
  - [2.D. Extension hooks (future-proofing)](#key-2-d-extension-hooks-future-proofing)
  - [2.E. Retention & privacy](#key-2-e-retention-privacy)
- [3. External REST API (through the Gateway)](#key-3-external-rest-api-through-the-gateway)
  - [3.A. External REST API - Conventions](#key-3-a-external-rest-api-conventions)
  - [3.B. External REST API - Endpoint catalogue](#key-3-b-external-rest-api-endpoint-catalogue)
  - [3.C. External REST API - Request/Response sketches](#key-3-c-external-rest-api-request-response-sketches)
    - [3.C.1. Register (POST /v1/auth/register)](#key-3-c-1-register-post-v1-auth-register)
    - [3.C.2. OAuth login (POST /v1/auth/oauth/google)](#key-3-c-2-oauth-login-post-v1-auth-oauth-google)
    - [3.C.3 Forgot password (POST /v1/auth/password/forgot)](#key-3-c-3-forgot-password-post-v1-auth-password-forgot)
  - [3.D. External REST API - Standard error cases](#key-3-d-external-rest-api-standard-error-cases)
  - [3.E. External REST API - Cross-flow notes](#key-3-e-external-rest-api-cross-flow-notes)
  - [3.F. External REST API - Open items for future discussions](#key-3-f-external-rest-api-open-items-for-future-discussions)
- [4. Internal gRPC API & Event Schemas](#key-4-internal-grpc-api-event-schemas)
  - [4.A. Internal gRPC API & Event Schemas - Logical split: two gRPC services](#key-4-a-internal-grpc-api-event-schemas-logical-split-two-grpc-services)
  - [4.B. Internal gRPC API & Event Schemas - AuthService (method catalogue)](#key-4-b-internal-grpc-api-event-schemas-authservice-method-catalogue)
  - [4.C. Internal gRPC API & Event Schemas - IdentityService (method catalogue)](#key-4-c-internal-grpc-api-event-schemas-identityservice-method-catalogue)
  - [4.D. Internal gRPC API & Event Schemas - Message key fields (summary)](#key-4-d-internal-grpc-api-event-schemas-message-key-fields-summary)
  - [4.E. Internal gRPC API & Event Schemas - Event stream design (Kafka topic identity.events)](#key-4-e-internal-grpc-api-event-schemas-event-stream-design-kafka-topic-identity-events)
    - [4.E.1. Internal gRPC API & Event Schemas - Event stream design (Kafka topic identity.events) - Envelope (Avro/Proto suggestion)](#key-4-e-1-internal-grpc-api-event-schemas-event-stream-design-kafka-topic-identity-events-envelope-avro-proto-suggestion)
    - [4.E.2. Internal gRPC API & Event Schemas - Event stream design (Kafka topic identity.events) - Payload messages](#key-4-e-2-internal-grpc-api-event-schemas-event-stream-design-kafka-topic-identity-events-payload-messages)
    - [4.E.3. Internal gRPC API & Event Schemas - Event stream design (Kafka topic identity.events) - Security & transport](#key-4-e-3-internal-grpc-api-event-schemas-event-stream-design-kafka-topic-identity-events-security-transport)
- [5. Runtime & Non-Functional Requirements (Identity Service)](#key-5-runtime-non-functional-requirements-identity-service)
  - [5.A. Runtime & Non-Functional Requirements - Capacity & Performance](#key-5-a-runtime-non-functional-requirements-capacity-performance)
  - [5.B. Runtime & Non-Functional Requirements - Availability & Scaling](#key-5-b-runtime-non-functional-requirements-availability-scaling)
  - [5.C. Runtime & Non-Functional Requirements - Rate-Limiting & Abuse Protection](#key-5-c-runtime-non-functional-requirements-rate-limiting-abuse-protection)
  - [5.D. Runtime & Non-Functional Requirements - Security Hardening](#key-5-d-runtime-non-functional-requirements-security-hardening)
  - [5.E. Runtime & Non-Functional Requirements - Observability](#key-5-e-runtime-non-functional-requirements-observability)
  - [5.F. Runtime & Non-Functional Requirements - Backup & DR](#key-5-f-runtime-non-functional-requirements-backup-dr)
  - [5.G. Runtime & Non-Functional Requirements - Deployment & Release](#key-5-g-runtime-non-functional-requirements-deployment-release)
  - [5.H. Runtime & Non-Functional Requirements - Compliance & Privacy (play-money phase)](#key-5-h-runtime-non-functional-requirements-compliance-privacy-play-money-phase)
  - [5.I. Runtime & Non-Functional Requirements - Real-Money Readiness Hooks](#key-5-i-runtime-non-functional-requirements-real-money-readiness-hooks)
  - [5.J. Runtime & Non-Functional Requirements - Open runtime questions](#key-5-j-runtime-non-functional-requirements-open-runtime-questions)

# 1. Context and Components

## 1.A. Context and Components - Actors & External Interfaces

|  Actor / System                                       |  How it talks to Identity                                                      |
|:------------------------------------------------------|:-------------------------------------------------------------------------------|
| **API Gateway (edge)**                                | gRPC unary calls (`ValidatePassword`, `CreateSession`, `ValidateToken`)        |
| **Email Service / ESP**                               | Asynchronous HTTP webhook *from* Identity outbox                               |
| **OAuth Providers** (Google, Apple, Facebook, X)      | Outbound HTTPS token-info calls                                                |
| **Downstream Services** (Wallet, Game-Session, Admin) | gRPC unary (`ValidateToken`, `GetUserById`) + Kafka events (`identity.events`) |

---

## 1.B. Context and Components - Internal Components

|  Component                            |  Responsibility                                                                   |  Data it owns                      |
|:--------------------------------------|:----------------------------------------------------------------------------------|:-----------------------------------|
| **Identity API** (Phoenix/Plug layer) | Exposes gRPC server, orchestrates flows, publishes events                         | —                                  |
| **Credential Manager**                | Hash/verify passwords, call OAuth introspection endpoints                         | —                                  |
| **Session Manager**                   | Issue / refresh / revoke tokens; maintain in-memory rate-limit buckets            | `sessions` table (logical)         |
| **User Repository**                   | CRUD for User, Profile, Credential records                                        | `users`, `credentials`, `profiles` |
| **Audit Logger**                      | Insert `login_events`; publish `UserCreated`, `Login*`, `SessionRevoked` to Kafka | `login_events`                     |
| **Outbox / Mailer**                   | Stores outbound email payloads; reacts to DB commit, calls ESP                    | `email_outbox`                     |
| **Token Validation Cache**            | LRU/ETS cache of recent JWT → UserContext look-ups                                | —                                  |

---

## 1.C. Context and Components - Data Stores & Buses

|  Store / Bus                     |  Used by                                                             |  Notes                                       |
|:---------------------------------|:---------------------------------------------------------------------|:---------------------------------------------|
| **PostgreSQL (identity schema)** | User Repo, Credential Manager, Session Manager, Audit Logger, Outbox | Single writer; logical partitioning by table |
| **Kafka –** `identity.events`    | Audit Logger (producer); Wallet, Analytics (consumers)               | At-least-once publication within 500 ms      |
| **ETS (in-proc)**                | Session Manager (rate limits), Token Cache                           | Volatile; rebuilt on node restart            |

---

## 1.D. Context and Components - High-level Call Flows (text)

1. **Email + Password Login**

   ```java
   Client ──REST──▶ Gateway ──gRPC──▶ Identity API
                            │             ├─ Credential Manager → hash verify
                            │             ├─ Session Mgr → create session, JWT
                            │             └─ Audit Logger → LoginSucceeded
                            └─gRPC reply with tokens ──▶ Gateway ──▶ REST ──▶ Client 
   ```
2. **Google OAuth Login**

   ```java
   Client → Gateway → Identity
                         └─ Credential Manager ──▶ verify ID-token via Google endpoint
   ```
3. **Downstream token check** (`Game-Session` request)

   ```java
   Game-Session ──gRPC──▶ Identity.ValidateToken → UserContext
   ```

# 2. Identity Data Model

## 2.A. Core entities

|  Entity                |  Purpose                                                                                                                                                                                       |
|:-----------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **User**               | The canonical person record every other service references.                                                                                                                                    |
| **Profile**            | Mutable, non-security details (display name, avatar, locale, timezone).                                                                                                                        |
| **Role**               | A single authority label (`player`, `super_admin`, …) linked to a User; users may hold many roles.                                                                                             |
| **Credential**         | One authentication method for a user (email + hash, Google OAuth sub, Apple sub, etc.).                                                                                                        |
| **Device**             | Logical installation of the client app, tied to a user after first successful login.                                                                                                           |
| **Session**            | One authenticated login instance (identified by `jwt_id`), created on success, revoked on logout/reset.                                                                                        |
| **LoginEvent**         | Immutable audit row for every sign-in attempt (success or failure).                                                                                                                            |
| **PasswordResetToken** | Short-lived, one-time token that allows a user to set a new password. Records how the token was sent. Masked destination (e.g. a\*\*\*@g\*\*\*.com) is displayed in client UI for confirmation |

## 2.B. Relationships & cardinalities

```java
User 1 ──< Profile                     (1-to-1, optional)  
User 1 ──< Role *                      (1-to-many)  
User 1 ──< Credential *                (1-to-many, unique per (type, ext_id))  
User 1 ──< Device *                    (1-to-many)  
User 1 ──< Session *                   (1-to-many, active & historical)  
User 1 ──< LoginEvent *                (1-to-many audit trail)  
Credential 1 ──< PasswordResetToken *  (ephemeral, nullable when unused)
```

*Key points*

- A **User** may hold multiple roles (set, not enum) so future staff roles require no schema change.
- A **Credential**’s `(type, external_id)` pair is globally unique to stop two accounts claiming the same Google sub.
- A **Session** references both `user_id` *and* `device_id` (nullable for web clients) to enable device-targeted logout.
- **LoginEvent** stores a copy of `credential_type` so you can query “failed password logins vs. OAuth logins” without joins.

## 2.C. Invariants & constraints (business rules)

|  ID    |  Rule                                                                                                                             |  Notes                                                 |
|:-------|:----------------------------------------------------------------------------------------------------------------------------------|:-------------------------------------------------------|
| INV-1  | Every active Session **must** reference an existing User that is `status = active OR shadow_banned`.                              | Enforced by foreign key + application check on login.  |
| INV-2  | A Credential of type `email_pwd` **must** have a password-hash; OAuth credentials must **not**.                                   | Keeps columns semantically clean.                      |
| INV-3  | A PasswordResetToken **expires ≤ 24 h** after issuance and is single-use.                                                         | On use, token row is deleted or flagged `consumed_at`. |
| INV-4  | Deleting a User cascades to Roles, Credentials, Devices, Sessions, and Profile but **not** LoginEvents (audit must remain).       | Satisfies audit retention requirements.                |
| INV-5  | A PasswordResetToken **shall** record `delivery_channel` and `masked_destination`; token verification ignores channel.            |                                                        |
| INV-6  | If `user.status = shadow_banned`, `ValidateToken` **shall** return `shadow_banned = true`; downstream services decide visibility. |                                                        |

## 2.D. Extension hooks (future-proofing)

|  Area        |  Placeholder column / table                                       |  Reason                                 |
|:-------------|:------------------------------------------------------------------|:----------------------------------------|
| KYC / AML    | `users.kyc_state`, `users.self_excluded_until`                    | Required when you add real-money.       |
| 2FA          | `credentials.mfa_secret`, `users.mfa_enforced`                    | Already needed for `super_admin` later. |
| Social graph | Separate “Friend” link table referencing `user_id` ↔ `friend_id`. | Keeps core identity clean.              |

## 2.E. Retention & privacy

|  Data set                      |  Minimum retention                          |  Rationale                                                          |
|:-------------------------------|:--------------------------------------------|:--------------------------------------------------------------------|
| **Sessions** (revoked/expired) | 90 days                                     | Debug token theft; GDPR “right to be forgotten” after purge window. |
| **LoginEvents**                | 180 days                                    | Fraud analytics, incident forensics.                                |
| **PasswordResetTokens**        | Delete immediately after success or expiry. | Minimises exposure.                                                 |

# 3. External REST API (through the Gateway)

Everything under `/v1/…` is public-facing HTTPS JSON; all 4xx/5xx errors follow **RFC 7807** problem-details.

## 3.A. External REST API - Conventions

|  Aspect                   |  Decision                                                                                             |
|:--------------------------|:------------------------------------------------------------------------------------------------------|
| **Auth header**           | `Authorization: Bearer <access-token>`                                                                |
| **Device header**         | `X-Device-Id: <uuid>` (sent on every request that can create a new session)                           |
| **Refresh token**         | HttpOnly, Secure cookie `refresh_token`, path `/v1/auth/refresh`, 30-day TTL                          |
| **Rate-limit signalling** | `429 Too Many Requests` + `Retry-After` seconds                                                       |
| **Error body**            | `{ "type": "https://api.example.com/errors/<slug>", "title": "...", "detail": "...", "status": 4xx }` |
| **Time fields**           | ISO-8601 UTC strings                                                                                  |

## 3.B. External REST API - Endpoint catalogue

|    #  |  Verb & Path                                                                       |  Goal                                      |  Auth required    |  Success code    |
|------:|:-----------------------------------------------------------------------------------|:-------------------------------------------|:------------------|:-----------------|
|    1  | **POST** `/v1/auth/register`                                                       | Create user w/ email + password            | none              | **201**          |
|    2  | **POST** `/v1/auth/login`                                                          | Password login                             | none              | **200**          |
|    3  | **POST** `/v1/auth/oauth/{provider}{provider}=google | apple | facebook | twitter` | OAuth token login / auto-signup            | none              | **200 or 201**   |
|    4  | **POST** `/v1/auth/refresh`                                                        | Swap refresh cookie for new access token   | refresh cookie    | **200**          |
|    5  | **POST** `/v1/auth/logout`                                                         | Invalidate current session                 | access token      | **204**          |
|    6  | **POST** `/v1/auth/logout_all`                                                     | Global logout (all devices)                | access token      | **204**          |
|    7  | **POST** `/v1/auth/password/forgot`                                                | Start reset flow (e-mail or SMS)           | none              | **202**          |
|    8  | **POST** `/v1/auth/password/reset`                                                 | Complete reset w/ one-time token           | none              | **204**          |
|    9  | **GET** `/v1/profile/me`                                                           | Fetch own Profile + Roles + Flags          | access token      | **200**          |
|   10  | **PUT** `/v1/profile/me`                                                           | Update display-name, avatar, locale        | access token      | **200**          |
|   11  | **POST** `/v1/credentials/link`                                                    | Link extra credential (e.g., add password) | access token      | **201**          |

## 3.C. External REST API - Request/Response sketches

### 3.C.1. Register (`POST /v1/auth/register`)

Request

```java
{
  "email": "alice@example.com",
  "password": "Str0ng!!",
  "device_id": "0c7f…",
  "locale": "en-US"
}
```

Response 201

```java
{
  "user_id": "9f1d…",
  "access_token": "eyJhbGciOiJSUzI1…",
  "expires_in": 900,
  "roles": ["player"]
}
```

(`refresh_token` cookie set)

### 3.C.2. OAuth login (`POST /v1/auth/oauth/google`)

Request

```java
{
  "id_token": "ya29.a0AR…",
  "device_id": "0c7f…"
}
```

Response 200/201 - same payload as register/login

### 3.C.3 Forgot password (`POST /v1/auth/password/forgot`)

Request

```java
{
  "destination": "alice@example.com"
}
```

**Response 202** – empty body.  
Outbox emits e-mail/SMS containing reset link with one-time token.

## 3.D. External REST API - Standard error cases

|  HTTP    |  `title` (example)    |  When                        |
|:---------|:----------------------|:-----------------------------|
| **400**  | `invalid_request`     | Missing or extraneous field  |
| **401**  | `invalid_credentials` | Bad password or OAuth token  |
| **403**  | `account_disabled`    | `status = banned`            |
| **409**  | `email_exists`        | Duplicate on register        |
| **422**  | `weak_password`       | Fails policy                 |
| **429**  | `rate_limited`        | Gateway or Identity throttle |
| **500**  | `internal_error`      | Unhandled server issue       |

## 3.E. External REST API - Cross-flow notes

- **Shadow-banned users** receive normal 200 responses - they do not know they are shadow-banned
- **Refresh rotation** – every `/refresh` call issues a new refresh cookie and revokes the old session.
- **MFA readiness** – endpoints accept optional `mfa_code` field; ignored until feature flag on.

## 3.F. External REST API - Open items for future discussions

- Exact JWT claim names (`sub`, `sid`, `roles`, `shadow_banned`, `exp`, `aud`…).
- Password policy regex and minimum entropy rule.
- Localization of error `title` / `detail`.

# 4. Internal gRPC API & Event Schemas

*(Design-level; names and fields are definitive, wire formats are illustrative)*

## 4.A. Internal gRPC API & Event Schemas - Logical split: two gRPC services

|  Service            |  Called by                |  Purpose                                                                            |
|:--------------------|:--------------------------|:------------------------------------------------------------------------------------|
| **AuthService**     | **Gateway only**          | Heavy auth flows: login, OAuth, refresh, password reset.                            |
| **IdentityService** | **All back-end services** | Cheap, high-rate token introspection and user lookup; session revocation for Admin. |

Both services share one `.proto` package (`identity.v1`) so message types can be reused.

## 4.B. Internal gRPC API & Event Schemas - AuthService (method catalogue)

|  RPC                    |  Req → Resp                                     |  Notes                                                                   |
|:------------------------|:------------------------------------------------|:-------------------------------------------------------------------------|
| `LoginPassword`         | `LoginPasswordReq` → `SessionTokens`            | For `/auth/login` and `/auth/register` (gateway passes a `create` flag). |
| `LoginOAuth`            | `LoginOAuthReq` → `SessionTokens`               | For Google / Apple / Facebook / X tokens.                                |
| `RefreshSession`        | `RefreshReq` → `SessionTokens`                  | Exchanges refresh-token cookie for new pair.                             |
| `PasswordResetInit`     | `PwdResetInitReq` → `google.protobuf.Empty`     | Writes **PasswordResetToken** + pushes Outbox.                           |
| `PasswordResetComplete` | `PwdResetCompleteReq` → `google.protobuf.Empty` | Verifies token, sets new hash, revokes sessions.                         |

**SessionTokens** *(common response)*

```java
message SessionTokens {
  string access_token   = 1;
  int32  expires_in_sec = 2; // e.g. 900
  string jwt_id         = 3;
  google.protobuf.Timestamp issued_at = 4;
}
```

## 4.C. Internal gRPC API & Event Schemas - IdentityService (method catalogue)

|  RPC                  |  Req → Resp                           |  Performance target                            |
|:----------------------|:--------------------------------------|:-----------------------------------------------|
| `ValidateToken`       | `Token` → `UserContext`               | ≤ 5 ms p95                                     |
| `GetUserById`         | `UserId` → `UserContext`              | ≤ 5 ms p95                                     |
| `RevokeSession`       | `SessionId` → `google.protobuf.Empty` | —                                              |
| `CreateSystemSession` | `SystemSessionReq` → `SessionTokens`  | For daemon-to-daemon calls (Leader­board, ETL). |

**UserContext** *(returned to gateway & services, never to client)*

```java
message UserContext {
  string user_id            = 1;
  repeated string roles      = 2;   // "player", "super_admin"
  bool   shadow_banned       = 3;
  string status              = 4;   // "active" | "banned" | "shadow_banned"
  google.protobuf.Timestamp token_exp = 5;
}
```

For shadow-ban stealth, **access tokens issued to clients DO NOT include** `shadow_banned`**;** the gateway attaches the flag downstream via header or gRPC metadata.

## 4.D. Internal gRPC API & Event Schemas - Message key fields (summary)

|  Type                 |  Required fields                           |
|:----------------------|:-------------------------------------------|
| `LoginPasswordReq`    | `email`, `password`, `device_id`, `locale` |
| `LoginOAuthReq`       | `provider` (enum), `id_token`, `device_id` |
| `RefreshReq`          | `refresh_token`, `device_id`, `ip`         |
| `PwdResetInitReq`     | `destination` (email                       |
| `PwdResetCompleteReq` | `reset_token`, `new_password`              |
| `Token`               | `jwt` (string)                             |
| `SystemSessionReq`    | `service_name` (string), `shared_secret`   |

All requests contain a `request_id` string in gRPC metadata for tracing.

## 4.E. Internal gRPC API & Event Schemas - Event stream design (Kafka topic `identity.events`)

### 4.E.1. Internal gRPC API & Event Schemas - Event stream design (Kafka topic `identity.events`) - Envelope (Avro/Proto suggestion)

```java
message IdentityEvent {
  string event_id   = 1; // UUID
  string type       = 2; // "UserCreated" | "LoginSucceeded" | …
  string version    = 3; // "v1"
  google.protobuf.Timestamp occurred_at = 4;
  bytes  payload    = 5; // type-specific message packed/Any
}
```

### 4.E.2. Internal gRPC API & Event Schemas - Event stream design (Kafka topic `identity.events`) - Payload messages

|  EventType                   |  Key fields                                     |  Emitted when                                     |
|:-----------------------------|:------------------------------------------------|:--------------------------------------------------|
| **UserCreated v1**           | `user_id`, `email`, `locale`                    | First successful register or OAuth auto-provision |
| **LoginSucceeded v1**        | `user_id`, `credential_type`, `device_id`, `ip` | Password or OAuth success                         |
| **LoginFailed v1**           | `credential_identifier`, `reason`, `ip`         | Any auth failure (hashed email for privacy)       |
| **SessionRevoked v1**        | `jwt_id`, `user_id`, `revoked_by` (`user`       | `system`                                          |
| **PasswordResetInit v1**     | `user_id`, `delivery_channel` (email            | sms)                                              |
| **PasswordResetComplete v1** | `user_id`                                       | Token consumed & password set                     |

*Partition key*: `user_id` where present, else hash of `credential_identifier` to keep ordering per user.

*Delivery*: at-least-once; consumers (Wallet, Analytics, Risk) must de-duplicate by `event_id`.

### 4.E.3. Internal gRPC API & Event Schemas - Event stream design (Kafka topic `identity.events`) - Security & transport

|  Path                      |  Transport                                                                   |  Auth                                                    |
|:---------------------------|:-----------------------------------------------------------------------------|:---------------------------------------------------------|
| Gateway ↔ Identity gRPC    | mTLS within cluster; SPIFFE IDs or mutual-TLS certificates                   | Gateway presents its cert, Identity allows only that SAN |
| Services ↔ IdentityService | Same mTLS; token requests authorized via shared secret in `SystemSessionReq` |                                                          |
| Kafka                      | SASL-SSL with client certs; ACL topic write allows only Identity svc         |                                                          |

# 5. Runtime & Non-Functional Requirements (Identity Service)

Scope: **Identity API pod(s)**, backing **PostgreSQL** and **Kafka**, plus the **Gateway** facets required to protect Identity.  
Target load: **10 000 concurrent users**, peaking at **100 login POST/s** and **2 000 ValidateToken calls/s**.

## 5.A. Runtime & Non-Functional Requirements - Capacity & Performance

|  Item                   |  Budget                          |  Rationale                                                    |
|:------------------------|:---------------------------------|:--------------------------------------------------------------|
| **Login password flow** | ≤ 150 ms p99 (end-to-end)        | Includes gateway, OAuth introspection, DB hit, token signing. |
| **ValidateToken RPC**   | ≤ 5 ms p95 (inside cluster)      | High-rate call on every request; cached in gateway for 30 s.  |
| **CPU baseline**        | 1 vCPU ≈ 2 000 ValidateToken/s † | Benchmarked on BEAM 2.16; scale pods linearly.                |
| **Memory baseline**     | 250 MB/pod                       | 125 MB code + ETS caches + BEAM overhead; 2× headroom.        |
| **DB connection pool**  | 30 conns/pod                     | Enough for bursty parallel logins; use pgbouncer in front.    |

† Real numbers should be validated with a K6 or Locust test before GA.

## 5.B. Runtime & Non-Functional Requirements - Availability & Scaling

|  Concern                 |  Approach                                                                                  |
|:-------------------------|:-------------------------------------------------------------------------------------------|
| **SLO**                  | 99.9 % monthly for *all* `/v1/auth` endpoints.                                             |
| **Horizontal scaling**   | Kubernetes HPA on `grpc_request_duration_seconds_p95`; min = 2 pods, max = 8.              |
| **Statelessness**        | All runtime state in Postgres / Kafka / ETS; no sticky sessions.                           |
| **Graceful restart**     | Pods receive SIGTERM → 10 s drain window → finish inflight RPCs, reject new.               |
| **Readiness & liveness** | `GET /healthz/ready` checks DB ping + Kafka producer; liveness only checks BEAM heartbeat. |
| **Multi-AZ**             | Two replicas per AZ; Postgres HA via Patroni or CloudSQL regional.                         |

## 5.C. Runtime & Non-Functional Requirements - Rate-Limiting & Abuse Protection

|  Layer                            |  Mechanism                                   |  Default thresholds                                    |
|:----------------------------------|:---------------------------------------------|:-------------------------------------------------------|
| **Gateway (IP buckets)**          | NGINX/Envoy leaky-bucket                     | 100 GET/min, 10 POST/min; bursts 2×.                   |
| **Gateway (credential buckets)**  | Lua / WASM calling Redis                     | 5 failed logins → 2 min lockout.                       |
| **Identity ETS (device buckets)** | Sliding-window counter                       | 10 failed logins/device/10 min.                        |
| **Captcha gating**                | Cloudflare Turnstile toggled after threshold | Enabled if IP or credential bucket trips twice in 1 h. |

## 5.D. Runtime & Non-Functional Requirements - Security Hardening

|  Area                  |  Control                                                              |
|:-----------------------|:----------------------------------------------------------------------|
| **Transport**          | mTLS inside cluster (Istio SPIFFE IDs) + TLS 1.3 at edge.             |
| **Secrets**            | Kubernetes Secrets + sealed-secret GitOps; rotated quarterly.         |
| **Password hashing**   | Argon2id, ops = 2, mem = 64 MiB, parallel-1.                          |
| **OAuth token cache**  | 5 min cache of Google/Facebook public certs, re-fetched hourly.       |
| **Admin accounts 2FA** | `super_admin` role must enrol in TOTP before acquiring the role flag. |

## 5.E. Runtime & Non-Functional Requirements - Observability

|  Signal             |  Tool                                                                                   |  SLI / Alert                                                                                                                                                                                                                        |
|:--------------------|:----------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Metrics**         | Prometheus + Grafana                                                                    | <ul><li><p><code>identity_login_latency_p99 &gt; 300 ms 5 m</code> → <em>WARN</em>• <code>login_failed_total{reason="invalid_credentials"} ÷ login_total &gt; 0.3 15 m</code> → <em>Possible credential-stuffing</em></p></li></ul> |
| **Tracing**         | OpenTelemetry → Tempo                                                                   | Sample = 5 % of auth flows; full sample of errors.                                                                                                                                                                                  |
| **Structured logs** | Loki (JSON)                                                                             | Correlation key `request_id`, `session_id`.                                                                                                                                                                                         |
| **Audit events**    | Kafka topic `identity.events` + 90 d retention; daily check job validates schema drift. |                                                                                                                                                                                                                                     |

## 5.F. Runtime & Non-Functional Requirements - Backup & DR

|  Asset                         |  Strategy                                                   |  RPO / RTO                 |
|:-------------------------------|:------------------------------------------------------------|:---------------------------|
| **Postgres (identity schema)** | WAL + nightly snapshot to S3 with 14 d PITR                 | RPO ≤ 5 min / RTO ≤ 30 min |
| **Kafka topic**                | Cluster-replicated 3×; off-cluster MirrorMaker to DR site   | RPO ≤ 0 (synchronous)      |
| **Kubernetes manifests**       | GitOps repo is source of truth; restore via `argo app sync` | RTO ≤ 15 min               |

## 5.G. Runtime & Non-Functional Requirements - Deployment & Release

|  Step           |  Detail                                                                  |
|:----------------|:-------------------------------------------------------------------------|
| **Branch & PR** | Feature branches, squash-merge to `main`.                                |
| **CI**          | GitLab CI: compile, run unit tests, Sobelow security scan.               |
| **Image build** | Multi-stage Docker → `ghcr.io/yourorg/identity:<git-sha>`.               |
| **CD**          | Argo CD auto-sync `main` tag to *staging*; manual promotion to *prod*.   |
| **Canary**      | 10 % traffic for 30 min; SLO alerts on latency and errors gates rollout. |

## 5.H. Runtime & Non-Functional Requirements - Compliance & Privacy (play-money phase)

- GDPR: `DELETE /v1/profile/me` triggers asynchronous “User-Erase” workflow — purges Profile, Credentials, Devices, Sessions; keeps hashed email in LoginEvents.
- COPPA: registration flow forbids `birth_year < 2009` (configurable).
- Audit data retained 180 days, encrypted at rest (AES-256 on disk).

## 5.I. Runtime & Non-Functional Requirements - Real-Money Readiness Hooks

- **KYC fields present** (`kyc_state`, `legal_country`) but nullable.
- **Event signature** envelope includes SHA-256 HMAC placeholder so events can be tamper-evident later.
- **2FA tables** already exist; enforcement flag toggled per role.

## 5.J. Runtime & Non-Functional Requirements - Open runtime questions

- **Gateway tech** – Envoy vs. NGINX OSS (affects built-in rate-limit modules).
- **mTLS certificate rotation** cadence (SPIRE auto-rotation vs. manual).
- **Peak login spikes** (e.g., after Apple feature) – size of Argon2id hash pool.

|  Question                                                                                         |  Recommendation                                                                                                                  |  Reasoning & quick action items                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                |
|:--------------------------------------------------------------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| <ol start="1"><li><p><strong>Gateway tech – Envoy vs NGINX OSS</strong></p></li></ol>             | **Pick Envoy** as your main edge/gateway.                                                                                        | *Feature fit*  • Native HTTP/2 **and** gRPC proxy, plus gRPC-Web and WebSocket support in one binary. • Ready-made filters for **rate-limit**, **JWT verification**, **mTLS**, and **ext-auth** (lets you outsource captcha or IP reputation checks). *Configurability*  • Dynamic xDS makes live flag flips and canary routes easy (aligns with your Feature-Flag plans). *Operational cost*  • Single container per replica; no “Plus” licence. Community docs and examples are abundant. *Downside*  • YAML config is verbose; mitigate by templating (Helm, Kustomize) or use Kuma/Contour if you later adopt a service mesh. **Action**: spin up a dev Envoy with the built-in RateLimit filter; wire it to Identity’s staging pod and throw K6 at it to confirm 429 behaviour.                           |
| <ol start="2"><li><p><strong>mTLS certificate rotation cadence</strong></p></li></ol>             | **Automate rotation with SPIRE (SPIFFE IDs)**—let the system issue workload certs that last **24 h**; rotate them transparently. | *Why SPIRE*  • Works well without a full Istio/Linkerd mesh; you just run the SPIRE Agent as a sidecar or daemonset. • Issued SVIDs (X.509) are short-lived, so even if a pod image is compromised, the cert dies in a day. *Root CA hygiene*  • Rotate the cluster CA manually **every 6 – 12 months** (off-peak maintenance). SPIRE regenerates workload cert chains automatically. *Fallback*  • If you prefer zero new components today, script cert-manager to issue pod certs with **30-day TTL** and restart pods via a CronJob—but this adds brief connection churn. **Action**: deploy SPIRE in staging, configure Identity and Gateway workloads to authenticate with SPIFFE IDs like `spiffe://games.local/identity`. Monitor that pods re-establish gRPC connections seamlessly on 24 h rollovers. |
| <ol start="3"><li><p><strong>Peak login spikes – Argon2id hash pool sizing</strong></p></li></ol> | **Dedicated CPU thread-pool × 1 vCPU per ~1 000 password logins/minute** + horizontal pod auto-scaling.                          | *Hash cost baseline*  • Your chosen Argon2id parameters (ops = 2, mem = 64 MiB) run in ≈ 3.5 ms on a modern core. 100 login POST/s ≈ 6 000 hashes/min → need ~6 cores for headroom. *Implementation*  • Use the `argon2_elixir` or `argon2-kdf` NIF with `max_threads = number_of_cores` to avoid BEAM scheduler starvation. • Let the Kubernetes HPA scale Identity pods on CPU ≥ 70 %. *Protection*  • If traffic bursts even higher (viral ad campaign) the gateway’s IP+credential buckets will shed excess attempts with 429s before latency explodes. *Validation*  • Load-test with K6: script 1 500 concurrent login requests; verify p99 < 150 ms and HPA scales additional pods within 1–2 minutes.                                                                                                  |

**Bottom-line checklist**

1. **Prototype Envoy** in dev, enable RateLimit + ext-auth filters.
2. **Deploy SPIRE** (or cert-manager fallback) and verify 24 h cert rollover.
3. **Instrument Argon2 CPU cost**; configure BEAM dirty-CPU NIF threads and set HPA targets.
