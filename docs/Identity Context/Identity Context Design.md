
[Games Platform](..\..\..\Games%20Platform.md) > [2025-06](..\..\2025-06.md) > [Identity Context](..\Identity%20Context.md)

# Identity Context Design

# Contents

- [1. Context and Components](#key-1-context-and-components)
  - [1.A. Actors & External Interfaces](#key-1-a-actors-external-interfaces)
  - [1.B. Internal Components](#key-1-b-internal-components)
  - [1.C. Data Stores & Buses](#key-1-c-data-stores-buses)
  - [1.D. High-level Call Flows (text)](#key-1-d-high-level-call-flows-text)
    - [1.D.1. Email + Password Registration / Login](#key-1-d-1-email-password-registration-login)
    - [1.D.2. Google / Apple / Facebook OAuth Login](#key-1-d-2-google-apple-facebook-oauth-login)
    - [1.D.3. Down-stream token check on every API call](#key-1-d-3-down-stream-token-check-on-every-api-call)
    - [1.D.4. Admin changes a user’s status / roles](#key-1-d-4-admin-changes-a-user-s-status-roles)
    - [1.D.5. Password reset (token revocation path)](#key-1-d-5-password-reset-token-revocation-path)
- [2. Domain Data Model](#key-2-domain-data-model)
  - [2.A. Core Entities](#key-2-a-core-entities)
  - [2.B. Relationships and cardinalities](#key-2-b-relationships-and-cardinalities)
    - [Key points](#key-points)
  - [2.C. Invariants & constraints (business rules)](#key-2-c-invariants-constraints-business-rules)
  - [2.D. Extension hooks (future-proofing)](#key-2-d-extension-hooks-future-proofing)
  - [2.E. Retention and Privacy](#key-2-e-retention-and-privacy)
- [3. Admin gRPC API and Domain Event Schemas](#key-3-admin-grpc-api-and-domain-event-schemas)
  - [3.A Why this interface exists](#key-3-a-why-this-interface-exists)
  - [3.B. gRPC service – at a glance](#key-3-b-grpc-service-at-a-glance)
  - [3.C. Domain events emitted](#key-3-c-domain-events-emitted)
  - [3.D. Access control overview](#key-3-d-access-control-overview)
- [4. Custom Claim Lifecycle](#key-4-custom-claim-lifecycle)
- [5. Rate‑Limiting & Abuse Protection](#key-5-rate-limiting-abuse-protection)

# 1. Context and Components

## 1.A. Actors & External Interfaces

|  **Actor / System**                                           |  **Role in v-0**                                                                                                                                                                     |  **Key Interaction**                                                                                                       |
|:--------------------------------------------------------------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------|
| **End-user Client (Web, Flutter, etc.)**                      | Initiates sign-up / sign-in via Firebase SDK; calls downstream APIs with an ID-token.                                                                                                | `firebase.auth()` methods; `Authorization: Bearer <ID-token>` on API calls                                                 |
| **Public API Gateway** *(e.g., Cloud Endpoints, Kong, Envoy)* | Single ingress for all business APIs — terminates TLS, validates Firebase ID-token once, forwards the request to the appropriate internal service.                                   | `Authorization: Bearer <ID-token>` header is verified by an *auth-module* or policy plugin configured with Firebase certs. |
| **Firebase Authentication (Google-managed)**                  | Issues ID-/refresh tokens, hosts OAuth & password-reset flows, enforces App Check / reCAPTCHA, revokes tokens on password change. Emits Auth events that trigger our Cloud Function. | HTTPS SDK calls inbound; *Auth event* webhooks outbound                                                                    |
| **Firebase Admin SDK (library)**                              | Used by every back-end service (including Identity) to verify ID-tokens locally, set custom claims, and revoke refresh tokens.                                                       | In-process library call; fetches Google certs once per day                                                                 |
| **Downstream Business Services**                              | Consume the ID-token; use the custom claims (`roles`, `status`) to authorize requests.                                                                                               | Local `verifyIdToken()` in each service                                                                                    |
| **Pub/Sub / Kafka Consumers** Topic `identity.events`         | *Event bus*. **Published by the Identity context** (Cloud Function + Identity Service) and **consumed by other back-end services** that need account lifecycle updates               | At-least-once CloudEvents JSON messages: `UserCreated`, `UserStatusChanged`, `UserRolesUpdated`.                           |

---

## 1.B. Internal Components

|  **Component**                            |  **Responsibility**                                                                                                                                                                                                                                                        |  **Typical caller / Access path**                                                                                                                                            |  **Data it owns**                              |
|:------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:-----------------------------------------------|
| **Identity Admin Service**                | Admin gRPC: `GetUser`, `SetUserStatus`, `UpdateUserRoles`, optional `MintCustomToken`; updates Firebase custom claims; publishes lifecycle events.                                                                                                                         | **Ops dashboard / CLI** and trusted automation over **mTLS gRPC** inside the VPC. *Never called by public clients.*                                                          | —                                              |
| **Custom-Claim Function**                 | On `auth.user().onCreate` → create `user_id`, set initial claims and fire `UserCreated`. On `beforeSignIn` → refresh claims if out-of-date.                                                                                                                                | Triggered **by Firebase**, not by clients. Connects to DB via Cloud SQL Auth Proxy (*or Firestore alternative*).                                                             | `users` table                                  |
| **User Repository**                       | Stores mapping `firebase_uid` ↔ numeric `user_id`; optional `user_devices`.                                                                                                                                                                                                | Read by Admin Service; writes only from Custom-Claim Function.                                                                                                               | `users`, (`user_devices`)                      |
| **Abuse-Protection Fuse**                 | Counts auth-related failures keyed by `(uid ∨ email) × IP`; returns HTTP 429 when > 10 in 10 min.                                                                                                                                                                          | Inline in public web/API layer.                                                                                                                                              | — (cache only)                                 |
| **Event Publisher**                       | Serialises and emits CloudEvents to **Pub/Sub topic** `identity.events`.                                                                                                                                                                                                   | Library code inside Admin Service & Custom-Claim Function.                                                                                                                   | —                                              |
| **Scheduled Job** **Claim-Drift Auditor** | **Nightly (02:00 UTC)**:  1. Scan `users` + `user_roles` tables. 2. Fetch current custom claims via Admin SDK. 3. If claim ≠ DB truth →    • update claim to match DB;   • emit `identity.audit.claim_drift` log entry;   • raise Cloud Monitoring alert on >0 mismatches. | Triggered by **Cloud Scheduler** → **Cloud Run job** (small .NET / Go container) inside VPC. Reads Postgres through the Cloud SQL connector; uses Admin SDK to patch claims. | None (read-only SQL; writes only logs/metrics) |

---

## 1.C. Data Stores & Buses

|  **Store / Bus**                                                                                                               |  **Used by**                                                                                                     |  **Notes**                                                                                                          |
|:-------------------------------------------------------------------------------------------------------------------------------|:-----------------------------------------------------------------------------------------------------------------|:--------------------------------------------------------------------------------------------------------------------|
| **Cloud SQL (PostgreSQL) — schema** `identity`• `users` (id PK, firebase\_uid UNIQUE, created\_at…)• `user_devices` (optional) | **Custom-Claim Function** (write / upsert) **Identity Admin Service** (read)                                     | No credentials, refresh tokens, or sessions stored. Single writer (Function) prevents race conditions.              |
| **Firebase Custom Claims** (per-user payload inside ID-token)                                                                  | **Identity Admin Service** & Custom-Claim Function *(set)* All back-end services *(read via token verification)* | Contains `roles`, `status`, optional flags (`has_password`). Max 1 KB/user; lives in Firebase Auth, not in your DB. |
| **Pub/Sub topic** `identity.events` (fan-out to Kafka/EventBridge permitted)                                                   | **Publishers:** Custom-Claim Function, Identity Admin Service**Consumers:** Wallet, Game, Analytics, etc.        | CloudEvents v1.0 JSON; at-least-once delivery; ordering not guaranteed across partitions.                           |
| **Fuse Cache** (in-process LRU or Redis)                                                                                       | **Abuse-Protection Fuse**                                                                                        | Sliding-window counters `(uid ∨ email) × IP`; entries auto-expire after 10 min; rebuilt on restart.                 |
| **Cloud Logging / BigQuery Auth export**                                                                                       | Firebase Auth (producer) SRE dashboards, SIEM (consumers)                                                        | Capture sign-ins, password resets, token revocations, fuse trips. Retained ≥ 180 days (see NFR §7).                 |

---

## 1.D. High-level Call Flows (text)

### 1.D.1. Email + Password Registration / Login

```java
Client ──SDK──► Firebase Auth
                 │  (hosted page / FlutterFire UI)
                 │
                 ├─► returns ID-token + refresh-token
                 ▼
Client ──HTTPS──► API Gateway
                 │   (sends  ID-token  in  Authorization header)
                 ▼
Gateway ──► verifyIdToken()         ← cached Google certs
                 │                  (no network hop)
                 ▼
Downstream API  ←──────────────────── authorised request
```

*Side-effect:* `auth.user().onCreate` event fires once; **Custom-Claim Function** creates `user_id`, sets initial `roles=["player"]`, `status="active"`, then publishes `UserCreated`.

### 1.D.2. Google / Apple / Facebook OAuth Login

```java
Client ──SDK──► Firebase Auth (OAuth pop-up / redirect)
                 │
                 ├─► ID-token  (provider-linked)
                 ▼
    (same flow as above: ID-token → Gateway → Downstream API)
```

### 1.D.3. Down-stream token check on every API call

```java
Client ──HTTPS──► API Gateway
                 │
Gateway ──► verifyIdToken(check_revoked = true)
                 │
                 ├─ roles, status custom claims
                 ▼
Authorisation:  if status != active → HTTP 403
                 else               → forward to service
```

### 1.D.4. Admin changes a user’s status / roles

```java
Ops Dashboard ──mTLS gRPC──► Identity Admin Service
                              │  SetUserStatus(banned)
                              ▼
                    auth.setCustomUserClaims(uid, {status:"banned"})
                              │
                              ├─ publish UserStatusChanged to Pub/Sub
                              ▼
                    Client’s next call → ID-token revoked
```

### 1.D.5. Password reset (token revocation path)

```java
User clicks link in Firebase-hosted reset page
                 │
Firebase Auth ───┤  automatically revokes all refresh-tokens for uid
                 ▼
Old ID-token used → verifyIdToken(check_revoked = true) → “revoked”  → HTTP 401
                 ▼
Client forced to re-authenticate
```

---

# 2. Domain Data Model

## 2.A. Core Entities

|  **Entity**                             |  **Purpose**                                                                                                                   |  **Where it lives**                              |
|:----------------------------------------|:-------------------------------------------------------------------------------------------------------------------------------|:-------------------------------------------------|
| **User**                                | Canonical record every other service references; maps `firebase_uid` → numeric `user_id`; stores created / updated timestamps. | **PostgreSQL** (`users` table)                   |
| **Role**                                | Enumerated authority labels (`player`, `moderator`, `admin`, …). Stored once so UIs & policies can validate input.             | **Static ENUM** (code) or **lookup table**       |
| **User ↔ Role** *(optional join table)* | Historical audit of which roles a user has held. Custom claim holds the current set; DB keeps changes for reporting.           | **PostgreSQL** (`user_roles`)                    |
| **Device** *(optional)*                 | Logical installation of the client app; helps support staff spot suspicious logins or force device revocation.                 | **PostgreSQL** (`user_devices`) or **Firestore** |
| **Profile** *(optional)*                | Mutable, non-security details (display name, avatar, locale, timezone). Can be moved to a dedicated “Profile” context later.   | **PostgreSQL** (`profiles`) or **Firestore**     |

---

## 2.B. Relationships and cardinalities

```java
User 1 ──▶ Profile           (1-to-1, optional)
User 1 ──▶ Device *          (1-to-many, optional)
User 1 ──▶ UserRole *        (1-to-many, history audit)
Role 1 ──▶ UserRole *        (1-to-many, lookup)
```

#### Key points

- **Roles live in two places**

  - Current set ⇒ stored in the user’s **custom claim** (`roles`).
  - Change history ⇒ kept in **UserRole** audit rows; no foreign keys in the claim itself.
- **Profile** is purely cosmetic data (display name, avatar, locale).  
  If you spin off a dedicated Profile service later, drop this table without touching auth.
- **Device** rows are optional; add only if support needs a “where did this user log in from?” view.  
  Each record is keyed by `installation_id` reported by the client after first successful sign-in.
- **No Credential / Session / PasswordResetToken / LoginEvent tables**  
  Those concerns are fully handled by **Firebase Authentication** and its BigQuery / Cloud-Logging exports.

---

## 2.C. Invariants & constraints (business rules)

|  **ID**    |  **Rule**                                                                                               |  **Notes / Enforcement**                                                                                 |
|:-----------|:--------------------------------------------------------------------------------------------------------|:---------------------------------------------------------------------------------------------------------|
| **INV-1**  | `firebase_uid` **is globally unique** and must map to **exactly one** numeric `user_id`.                | DB constraint (`UNIQUE(firebase_uid)`); written only by Custom-Claim Function.                           |
| **INV-2**  | `user.status` **permitted values:** `active`, `banned`, `shadow_banned`.                                | Domain enum; Identity Admin Service validates before updating custom claim.                              |
| **INV-3**  | The `roles` **custom claim** MUST equal the **current row set in** `user_roles` (order-insensitive).    | Admin Service writes both DB rows *and* custom claim in the same transaction; a nightly job flags drift. |
| **INV-4**  | Setting `status = banned` or `shadow_banned` MUST immediately **revoke refresh tokens** for that `uid`. | Admin Service calls `auth.revokeRefreshTokens(uid)` then emits `UserStatusChanged`.                      |
| **INV-5**  | Role `super_admin` MAY only be added or removed by a caller that **already holds** `super_admin`.       | Verified inside Admin Service via caller mTLS CN → operator role lookup.                                 |
| **INV-6**  | Hard-deleting a User MUST **cascade to UserRole and Device rows** but leave event history intact.       | Foreign keys with `ON DELETE CASCADE`; Pub/Sub event stream is append-only.                              |

---

## 2.D. Extension hooks (future-proofing)

|  **Area**                          |  **Placeholder hook**                                                                                  |  **Why we leave the hook**                                                                                      |
|:-----------------------------------|:-------------------------------------------------------------------------------------------------------|:----------------------------------------------------------------------------------------------------------------|
| **Multi-Factor Auth (MFA)**        | *Flag only*  → add Boolean`users.mfa_enforced` **or** custom-claim `mfa=true`                          | Firebase Auth already supports SMS / TOTP. A toggle lets you roll it out per cohort without migrations.         |
| **KYC / Age Verification**         | Columns in `users` table:`kyc_state ENUM(pending, verified, failed)`,`dob DATE`, `country_iso CHAR(2)` | Required if you ever handle real-money or age-restricted features. Keeping them in SQL avoids claim-size bloat. |
| **Account Deletion / GDPR Export** | Audit table `user_deletion_requests` with `requested_at`, `processed_at`                               | Lets you process “right to be forgotten” manually at first; no schema change later when you automate it.        |
| **Device Trust & Notifications**   | Optional table `user_devices` (`installation_id`, `first_seen`, `last_seen`, `ip`)                     | Enables per-device session dashboards or suspicious-login alerts without touching core auth.                    |
| **Social Graph / Friends**         | Separate table `friend_links` (`user_id`, `friend_id`, `state`)                                        | Keeps social features out of the Identity context; link table can live in a future “Social” service.            |
| **Premium / Subscription Flag**    | Custom claim `subscription="pro"` & mirror column `users.subscription`                                 | Claim gives instant gating; SQL mirror supports analytics joins.                                                |

**Guideline:**  
*Use **custom claims** for small, latency-sensitive flags (≤ 1 KB total).*  
*Keep **larger or slowly changing data** (KYC docs, social links) in Postgres or Firestore so the ID-token size and issue time stay low.*

These placeholders require no code now but avoid disruptive migrations when the business roadmap expands.

---

## 2.E. Retention and Privacy

|  **Data set**                                               |  **Minimum retention**                                                      |  **Rationale / notes**                                                                                            |
|:------------------------------------------------------------|:----------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------|
| `users` **table** ( `user_id`, `firebase_uid`, timestamps ) | **Indefinite** – until an operator executes a GDPR / CCPA deletion request. | Canonical key used by every other service; must remain stable for the lifetime of the account.                    |
| `user_roles` **audit**                                      | **24 months**                                                               | Supports abuse investigations and historical entitlement analytics; older rows may be rolled off to cold storage. |
| `user_devices` (optional)                                   | **90 days**                                                                 | Provides recent login-location context for support; longer retention offers little value and adds PII risk.       |
| **Firebase Auth logs → BigQuery / Cloud Logging**           | **≥ 180 days**                                                              | Fraud analytics and security forensics window; aligns with common SOC & PCI guidance.                             |
| **Fuse-cache counters**                                     | **≤ 10 minutes** (auto-expire)                                              | Holds only transient IP / UID rate-limit buckets; no long-term user data.                                         |
| **Pub/Sub topic** `identity.events`                         | **≥ 12 months** (or replicate to a long-term warehouse)                     | Allows new consumers to replay user-lifecycle history; fulfils audit requirements.                                |
| **Custom claims inside ID-tokens**                          | **≈ 60 minutes** (token TTL)                                                | Lives only in signed JWTs on the client; refreshed automatically, never stored server-side.                       |

---

# 3. Admin gRPC API and Domain Event Schemas

## 3.A Why this interface exists

- Give trusted operators and automated jobs a **controlled way** to read a user record and change **roles** or **status**.
- Emit **lifecycle events** so other bounded contexts can react without polling.
- Everything runs **inside the private VPC**; no public traffic hits this API.

---

## 3.B. gRPC service – at a glance

|  **Capability**                                          |  **Typical caller**                        |  **Notes**                                                                |
|:---------------------------------------------------------|:-------------------------------------------|:--------------------------------------------------------------------------|
| **GetUser** (read-only)                                  | Support dashboard, batch reports           | Fetch by `user_id` or `firebase_uid`; returns roles & status.             |
| **SetUserStatus** (`active <--> banned / shadow_banned`) | Moderation dashboard, automated fraud jobs | Immediately updates the Firebase custom claim and revokes refresh tokens. |
| **UpdateUserRoles** (add / remove)                       | Admin dashboard, entitlement sync          | Writes DB + claim in one transaction to avoid drift.                      |
| **MintCustomToken (Optional)**                           | Internal daemons (e.g., Leaderboard)       | Short-lived token so a backend can act “as the user” for ≤ 15 min.        |

*Transport & auth* – gRPC over mTLS, 50 req/s per caller; caller identity is the client-certificate CN.

---

## 3.C. Domain events emitted

|  **Event**            |  **Fired when**                                       |  **Key data carried**                                                |  **Typical consumers**                                  |
|:----------------------|:------------------------------------------------------|:---------------------------------------------------------------------|:--------------------------------------------------------|
| **UserCreated**       | Firebase “user-create” trigger finishes claim set-up  | `user_id`, `firebase_uid`, roles=[player], status=active, timestamps | Wallet → open account, Profile → create default profile |
| **UserStatusChanged** | Status flips (`active`→`banned`, etc.) via Admin call | `user_id`, previous & new status, who changed, when                  | Game / Social services → kick or hide player            |
| **UserRolesUpdated**  | Roles added / removed via Admin call                  | `user_id`, added / removed sets, new full set                        | Feature gates, Analytics                                |

*Bus* – published to **Pub/Sub topic** `identity.events` (fan-out to Kafka/EventBridge allowed); at-least-once delivery.

---

## 3.D. Access control overview

|  **Caller role**    |  **Allowed methods**    |
|:--------------------|:------------------------|
| `super_admin`       | All                     |
| `moderator`         | GetUser, SetUserStatus  |
| `support`           | GetUser only            |

Roles are checked from the caller’s mTLS certificate metadata.

---

# 4. Custom Claim Lifecycle

- *Trigger wiring* – Cloud Function `setClaimsOnCreate` fires on `auth.user().onCreate`; `refreshClaimsBeforeSignIn` fires on `beforeSignIn`.
- *Write logic* – look up or insert into `users`, then call `auth.setCustomUserClaims(uid, { roles, status })`.
- *Failure path* – if DB write or claim update fails, function logs ERROR and Cloud Monitoring alert *“claim-drift risk”* fires; sign-in proceeds (user gets minimal claims).

# 5. Rate‑Limiting & Abuse Protection

- *Client side* – Firebase SDK enforces reCAPTCHA/App Check for Email/Pass and OAuth flows.
- *Server fuse* – inline LRU cache counts `(uid ∨ email) × IP`; default threshold = 10 failures / 10 min ➜ HTTP 429, `Retry-After: 30`. Auto-expire after 10 min; no persistence.
- *Monitoring* – fuse trips `>= 100 / 5 min` send a PagerDuty *HIGH* alert.
