
[Games Platform](..\..\..\Games%20Platform.md) > [2025-06](..\..\2025-06.md) > [Identity Context](..\Identity%20Context.md)

# Identity Context Requirements

- [1. Primary Responsibilities (Functional Scope)](#key-1-primary-responsibilities-functional-scope)
- [2. Credential Flows](#key-2-credential-flows)
  - [2.1. Notes](#key-2-1-notes)
- [3. Session & Token Model](#key-3-session-token-model)
  - [3.1  Session & Token Model - Principles](#key-3-1-session-token-model-principles)
  - [3.2  Session & Token Model - Token Classes](#key-3-2-session-token-model-token-classes)
  - [3.3  Session & Token Model - Custom Claims](#key-3-3-session-token-model-custom-claims)
  - [3.4  Session & Token Model - Token Verification Contract](#key-3-4-session-token-model-token-verification-contract)
  - [3.5  Session & Token Model - Revocation & Global Logout](#key-3-5-session-token-model-revocation-global-logout)
  - [3.6  Session & Token Model - Audit & Monitoring](#key-3-6-session-token-model-audit-monitoring)
- [4. Rate-Limiting and Abuse Protection](#key-4-rate-limiting-and-abuse-protection)
  - [4.A. Rate-Limiting and Abuse Protection - Overview](#key-4-a-rate-limiting-and-abuse-protection-overview)
  - [4.B. Rate-Limiting and Abuse Protection - Client‑Side Controls](#key-4-b-rate-limiting-and-abuse-protection-client-side-controls)
  - [4.C. Rate-Limiting and Abuse Protection - Server‑Side Fuse](#key-4-c-rate-limiting-and-abuse-protection-server-side-fuse)
  - [4.D. Rate-Limiting and Abuse Protection - Manual Blocks and Escalation](#key-4-d-rate-limiting-and-abuse-protection-manual-blocks-and-escalation)
  - [4.E. Rate-Limiting and Abuse Protection - Monitoring & Alerting](#key-4-e-rate-limiting-and-abuse-protection-monitoring-alerting)
  - [4.F. Rate-Limiting and Abuse Protection - Additional Notes](#key-4-f-rate-limiting-and-abuse-protection-additional-notes)
- [5. Events Emitted](#key-5-events-emitted)
  - [5.1  Events Emitted - Event Bus](#key-5-1-events-emitted-event-bus)
  - [5.2  Events Emitted - Domain Events](#key-5-2-events-emitted-domain-events)
  - [5.3  Events Emitted - Authentication Analytics](#key-5-3-events-emitted-authentication-analytics)
  - [5.4  Events Emitted - Schema Stability](#key-5-4-events-emitted-schema-stability)
- [6. gRPC Surface (Internal Only)](#key-6-grpc-surface-internal-only)
  - [6.1  gRPC Surface (Internal Only) - Purpose](#key-6-1-grpc-surface-internal-only-purpose)
  - [6.2  gRPC Surface (Internal Only) - Service & Methods](#key-6-2-grpc-surface-internal-only-service-methods)
  - [6.3  gRPC Surface (Internal Only) - Transport & Security](#key-6-3-grpc-surface-internal-only-transport-security)
  - [6.4  gRPC Surface (Internal Only) - Schema & Versioning](#key-6-4-grpc-surface-internal-only-schema-versioning)
- [7. Out-of-Scope for v-0](#key-7-out-of-scope-for-v-0)
- [8. Non-Functional Requirements (cross-cutting)](#key-8-non-functional-requirements-cross-cutting)

# 1. Primary Responsibilities (Functional Scope)

|  #    |  Requirement                                                                                                    |
|:------|:----------------------------------------------------------------------------------------------------------------|
| R-1   | Authentication, credential linking and token issuance are delegated to *Firebase Authentication* (client SDK).  |
| R-2   | The service enriches Firebase users with `roles` and `status` custom claims.                                    |
| R-3   | Down‑stream services verify Firebase ID‑tokens locally with the Admin SDK.                                      |
| R-4   | The service **shall** expose identity and token-validation data to other back-end services via an internal API. |
| R-5   | The service **shall** capture an immutable audit trail of login attempts and session activity.                  |

---

# 2. Credential Flows

|  **Flow**                                 |  **Requirements**                                                                                                                                                                            |
|:------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Email + Password — Registration**       | Delegated to *Firebase Authentication* hosted page / SDK. Identity service listens to the **Auth‑user‑create** trigger and sets initial `roles=["player"]`, `status="active"` custom claims. |
| **Email + Password — Login**              | Handled by Firebase. Clients receive a Firebase **ID‑token**; downstream APIs simply verify it. No “Session” DB row is created.                                                              |
| **OAuth (Google / Apple / Facebook / X)** | Login & token verification occur inside Firebase. Identity service is notified via the same trigger as above; if the user is new, map Firebase `uid` → internal `user_id`.                   |
| **Credential Linking**                    | Authenticated clients[^1] call `linkWithCredential()` in the Firebase SDK. On success, a Cloud Function updates custom claims if needed (e.g., set `has_password=true`).                     |
| **Password Reset**                        | Use `sendPasswordResetEmail()` from Firebase; the hosted reset page invalidates old refresh tokens automatically. No outbox email logic remains in the service.                              |
| **Account Status Handling**               | Every request must check `custom_claims.status`: `active` → allow, `banned` → HTTP 403, `shadow_banned` → allow but suppress visibility events.                                              |

All user‑facing flows are implemented by Firebase hosted pages or SDK widgets; Identity service only consumes the resulting ID‑token.

## 2.1. Notes

1. Authenticated clients:

   1. Definition: A client (mobile, web, desktop) that **already holds a valid Firebase ID‑token** for the user. In code that means `firebase.auth().currentUser` (Web) or `FirebaseAuth.instance.currentUser` (Flutter) is **non‑null**.
   2. Credential Link flow:

      1. The signed‑in user selects “Add Google” (or Apple, etc.).
      2. The client obtains a **credential object** for that provider (e.g. `GoogleAuthProvider.credential(…)`).
      3. The same client calls `currentUser.linkWithCredential(credential)` (Web/Android/iOS) or `currentUser.linkWithProvider()` (FlutterFire).
      4. Firebase merges the two sign‑in methods under the same `uid` and issues a fresh ID‑token.
      5. If you need to adjust roles/flags, a Cloud Function listening to the **Auth‑user‑provider‑link** trigger updates custom claims.
2. How does the Identity service notice that tokens are no longer valid after a **Password Reset**?  
   You don’t have to poll or store anything:

|  **Step**                                                 |  **What happens**                                                                                                                           |  **How your code reacts**                                             |
|:----------------------------------------------------------|:--------------------------------------------------------------------------------------------------------------------------------------------|:----------------------------------------------------------------------|
| User clicks the password‑reset link (hosted by Firebase). | Firebase **revokes every existing refresh token** for that `uid` ([Firebase](https://firebase.google.com/docs/auth/admin/manage-sessions)). | –                                                                     |
| Client tries to call your API with an **old ID‑token**.   | The token is still cryptographically valid but `verifyIdToken(..., check_revoked = true)` returns *“token revoked”*.                        | Your API returns **HTTP 401**, prompting the app to force a re‑login. |
| Client tries to refresh with its **old refresh token**.   | Backend rejects it (token was revoked), so the SDK signs the user out automatically.                                                        | –                                                                     |

So the “realization” is implicit: verification fails and you reject the request—no background job, no database row.

3. Do Flutter and Web offer drop‑in UI for these flows?

|  **Platform**    |  **Firebase‑supplied UI**                                                                                                                                |  **Notes**                                                                                                                                            |
|:-----------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Flutter**      | **FlutterFire UI** (`SignInScreen`, `ProfileScreen`, `LinkAccountsScreen`, etc.) ([firebase.flutter.dev](https://firebase.flutter.dev/docs/ui/widgets/)) | Widgets cover Email/Pass, OAuth providers, password‑reset links, and even account‑linking. You can theme or swap them out for your own design.        |
| **Web**          | **FirebaseUI Web** (`firebaseui‑auth`) ([Firebase](https://firebase.google.com/docs/auth/web/firebaseui))                                                | Provides hosted or embeddable screens for sign‑up/in, linking, and reset. You can also use the plain JS SDK and build a fully custom UI if preferred. |

---

# 3. Session & Token Model

## 3.1  Session & Token Model - Principles

1. **Firebase‑Centric** – All authentication credentials are issued and managed by **Firebase Authentication**.
2. **Stateless Back‑End** – The Identity service SHALL NOT create, persist, or introspect proprietary session records or JWTs.
3. **Local Verification** – Every downstream service SHALL verify Firebase ID‑tokens locally via the Firebase Admin SDK; no central “token‑validation” endpoint is provided.

Notes:

- **Stateless** here means “no server‑side session table or token‑introspection endpoint,” **not** “don’t check tokens.”
- Every service—including the Identity service itself—still calls the Admin‑SDK method `verifyIdToken()` (or its C#, Go, etc. equivalent) on each request.

  - The SDK validates the JWT signature locally with Google public keys that it downloads **once every 24 h and then caches in RAM**; normal requests incur **no network round‑trip to Firebase**.
  - After that signature check succeeds, the SDK hands you the decoded claims object; you read `roles`, `status`, etc. and proceed.  
    So: **verification yes, central introspection service no.**

## 3.2  Session & Token Model - Token Classes

|  **Token**        |  **Issuer & Format**     |  **Nominal Lifetime**    |  **Storage Responsibility**                           |
|:------------------|:-------------------------|:-------------------------|:------------------------------------------------------|
| **ID token**      | Firebase‑signed JWT      | ~60 minutes              | Client runtime memory and HTTP `Authorization` header |
| **Refresh token** | Opaque string (Firebase) | Until explicitly revoked | Secure client storage (Keychain / Keystore)           |

- *No other token types are permitted.*
- Secure client storage:

  - On iOS the SDK stores the refresh token in the **Keychain**; on Android it uses the **Keystore / EncryptedSharedPreferences**.
  - In a web SPA the SDK keeps it in an IndexedDB database, encrypted with the browser’s Crypto API.

## 3.3  Session & Token Model - Custom Claims

- The Identity context SHALL attach the following custom claims to each Firebase user:

  - `roles` – array of role identifiers (e.g., `["player"]`).
  - `status` – `active`, `banned`, or `shadow_banned`.
- These claims SHALL propagate automatically in every new ID token and SHALL be the sole source of authorization data for downstream services.

## 3.4  Session & Token Model - Token Verification Contract

- All internal APIs SHALL reject requests lacking a valid, non‑revoked Firebase ID‑token (HTTP 401).
- Authorization decisions (role/status) SHALL be derived exclusively from the custom claims contained in that ID‑token (HTTP 403 when insufficient).

## 3.5  Session & Token Model - Revocation & Global Logout

- The Identity service SHALL be able to revoke a user’s refresh tokens via the Firebase Admin SDK.
- Token revocation SHALL take effect immediately for new token exchanges and within ≤ 60 minutes for already‑issued ID‑tokens.

## 3.6  Session & Token Model - Audit & Monitoring

- The project SHALL capture authentication analytics via either (a) Firebase‑managed exports (BigQuery, Cloud Logging) **or** (b) an in‑house Pub/Sub/Kafka topic providing equivalent data.

Notes:

- **Firebase Auth → BigQuery export** and **Auth events → Cloud Logging** turn on with two clicks and give you structured rows for every sign‑in, sign‑out, reset, provider‑link, etc.—no code, no maintenance
- You certainly *can* stream the same data into your own Kafka/Kinesis topics instead; it just means writing a Cloud Function to listen to the BigQuery notification or the Auth trigger and re‑emit events.

---

# 4. Rate-Limiting and Abuse Protection

**Goal** - Stop automated abuse with the lightest possible footprint for an early‑stage, hobby‑scale service.  
All figures are defaults; tune in production as needed.

## 4.A. Rate-Limiting and Abuse Protection - Overview

|  **Layer**           |  **Purpose**                                                         |  **Technique**                                                                        |
|:---------------------|:---------------------------------------------------------------------|:--------------------------------------------------------------------------------------|
| **Client-side**      | Filter obvious bot traffic *before* it hits Firebase or your service | reCAPTCHA v3 (Web) / SafetyNet / DeviceCheck, plus Firebase Auth built‑in rate limits |
| **Server‑side fuse** | Catch residual abuse against your own gRPC / REST endpoints          | Stateless sliding‑window counter in RAM (or Redis) that returns **HTTP 429**          |

The two layers are independent: if the client fails the first, no request reaches the second.

---

## 4.B. Rate-Limiting and Abuse Protection - Client‑Side Controls

1. **Challenge on every auth flow**  
   Sign‑in, sign‑up, password reset, and credential linking **MUST** include a Firebase‑validated challenge token.
2. **Respect Firebase throttling**  
   When the SDK yields `too‑many‑requests`, show a generic *“Please wait a moment and try again”* without calling your back‑end.
3. **No PII leakage**  
   Do **not** send raw e‑mail / phone data to your back‑end purely for challenge evaluation.

---

## 4.C. Rate-Limiting and Abuse Protection - Server‑Side Fuse

|  **Parameter**    |  **Default**                     |  **Notes**                                |
|:------------------|:---------------------------------|:------------------------------------------|
| Key               | `(authId or providerId) × IP`    | Combine user credential & source IP       |
| Window            | **10 min** sliding               | Adjust once you have real traffic         |
| Limit             | **10 failed attempts**           | Count only requests that end in *4xx/5xx* |
| Response          | **HTTP 429** + `Retry‑After: 30` | Client should back off                    |

Implementation rules:

- Store counters in process memory or Redis **only**; *no* database writes.
- Auto‑expire keys after the window; nothing to clean up.
- The fuse applies to admin or other public endpoints you own – **not** to raw Firebase Auth calls (those are already throttled upstream).

---

## 4.D. Rate-Limiting and Abuse Protection - Manual Blocks and Escalation

- Support staff may write problem IPs or `uid`s to an `abuse_blacklist` (e.g., Firestore).
- Your service checks the list first and returns **HTTP 403** if matched.

---

## 4.E. Rate-Limiting and Abuse Protection - Monitoring & Alerting

- Log every fuse trip, `too‑many‑requests` error, and blacklist hit to **Cloud Logging** (`severity=WARNING`).
- A simple metric `fuse_trips_per_5min` triggers an email if **> 100** events occur in any 5‑min window.

---

## 4.F. Rate-Limiting and Abuse Protection - Additional Notes

- **Scope** – The fuse protects *your* endpoints (e.g., `SetUserStatus`). It cannot see attacks on `firebaseAuth.signInWithEmailPassword`.
- **Client telemetry** – If you want `too‑many‑requests` counts in one place, either (a) rely on BigQuery Auth export, or (b) send a non‑PII beacon from the client.

---

# 5. Events Emitted

## 5.1  Events Emitted - Event Bus

- Project‑wide domain events SHALL be published to **Pub/Sub topic**`identity.events` (fan‑out to Kafka, EventBridge or similar is acceptable).
- All events SHALL use **CloudEvents v1.0 JSON**; ordering is *not* guaranteed, delivery is *at‑least‑once*.

## 5.2  Events Emitted - Domain Events

|  Name                 |  Trigger                                                                                           |  Key payload fields                                                                     |  Purpose                                                                                                             |
|:----------------------|:---------------------------------------------------------------------------------------------------|:----------------------------------------------------------------------------------------|:---------------------------------------------------------------------------------------------------------------------|
| **UserCreated**       | After Firebase reports **user‑create** and the Identity service provisions the internal `user_id`. | `user_id`, `firebase_uid`, `created_at`, `roles`, `status` (`active`), `email_verified` | Notify other contexts (e.g. Wallet, Profile) that a new account exists.                                              |
| **UserStatusChanged** | Whenever `status` transitions (`active` ↔ `banned` / `shadow_banned`).                             | `user_id`, `previous_status`, `new_status`, `changed_by`, `changed_at`                  | Cascade bans/unbans to game & social services.                                                                       |
| **UserRolesUpdated**  | Whenever the `roles` array is modified.                                                            | `user_id`, `added_roles`, `removed_roles`, `roles`, `changed_by`, `changed_at`          | Grant or revoke feature access in downstream services. `roles` is the *complete* post-update array (for idempotency) |

*No other events SHALL be emitted by the Identity context.*

## 5.3  Events Emitted - Authentication Analytics

- **Sign‑in successes, failures, password‑reset requests, and token revocations** SHALL NOT be duplicated on `identity.events`.
- Instead, the project SHALL rely on **Firebase Auth → BigQuery export** and **Auth events → Cloud Logging** for security analytics and alerting.

## 5.4  Events Emitted - Schema Stability

- Event schemas SHALL be versioned with a `specversion` field (`"1.0"` initial).
- Backward‑incompatible changes SHALL be introduced only via a new event name or a bump to `specversion`.

---

# 6. gRPC Surface (Internal Only)

## 6.1  gRPC Surface (Internal Only) - Purpose

The gRPC interface exists **only** for back‑office and automation tasks that cannot be performed through the Firebase client or Admin SDK.  
It **MUST NOT** be called by end‑user devices and **SHALL** be reachable only from the private VPC.

## 6.2  gRPC Surface (Internal Only) - Service & Methods

|  **Method name**             |  **Description**                                                                       |  **Typical caller**                           |  **Key effects**                                                                                                             |
|:-----------------------------|:---------------------------------------------------------------------------------------|:----------------------------------------------|:-----------------------------------------------------------------------------------------------------------------------------|
| `GetUser`                    | Fetch a user record by `user_id` **or** `firebase_uid`.                                | Customer‑support tool, back‑office batch jobs | Returns identifiers, `roles`, `status`, `email_verified`, timestamps.                                                        |
| `SetUserStatus`              | Change account status to `active`, `banned`, or `shadow_banned`.                       | Moderation dashboard                          | Updates Firebase custom claim `status`; emits **UserStatusChanged**.                                                         |
| `UpdateUserRoles`            | Add or remove role identifiers.                                                        | Admin dashboard, entitlement service          | Updates Firebase custom claim `roles`; emits **UserRolesUpdated**.                                                           |
| `MintCustomToken` (optional) | Issue a short‑lived Firebase **Custom Token** so an internal daemon can act as a user. | Trusted back‑end services (e.g., Leaderboard) | Returns a token with ≤ 15 min TTL. Note: the caller must still exchange the custom token for an **ID‑token** before using it |

*No other gRPC methods SHALL be exposed.*

## 6.3  gRPC Surface (Internal Only) - Transport & Security

- gRPC over **mTLS** within the private VPC.
- Caller identity derived from the client certificate’s `CN`; no bearer tokens.
- **Rate‑limit:** 50 requests per second, with a burst of 100 for ≤ 10 s.; higher‑volume operations must use bulk Pub/Sub messages.

## 6.4  gRPC Surface (Internal Only) - Schema & Versioning

- Protobuf package version suffix `v1`.
- Backward‑incompatible changes SHALL be introduced under a new package (`v2`) and the previous version SHALL remain available for at least six months.

---

# 7. Out-of-Scope for v-0

- **Multi‑Factor Authentication (MFA)**

  - Time‑based one‑time passwords (TOTP), SMS, or push‑based approval flows are deferred.
  - Firebase Auth supports SMS / TOTP; enable only after we’ve shipped v‑0 and validated the primary flow.
- **Enterprise Identity (SAML / OIDC Provider‑Hosted)**

  - Single‑sign‑on for corporate directories and SCIM user‑provisioning are excluded.
- **Advanced Role Management UI**

  - A self‑service console for fine‑grained role editing is postponed; roles will be managed through gRPC calls only.
- **Self‑Serve Account Deletion & Data Export**

  - GDPR/CCPA “right to be forgotten” flows will be handled manually until v‑1.
  - Manual deletions will be executed within ≤ 30 days
- **Custom Email Branding**

  - Firebase’s default email templates (verification, password‑reset) will be used without brand styling.
  - Will be revisited when Marketing provides templates.
- **KYC / Age Verification**

  - Government‑ID checks, proof‑of‑age gates, or other regulatory identity steps are not included.
- **Device‑Level Session Dashboard**

  - End‑users will not see a list of their active devices or be able to revoke individual sessions.
- **Rate‑Limit Configuration Dashboard**

  - Abuse‑protection thresholds will be code‑based; an operator UI to tune them is deferred.
- **Custom Sub‑Domain for Auth Pages**

  - Hosted Firebase auth pages will remain on `*.firebaseapp.com`; moving to `auth.<your‑domain>` is slated for a future iteration.
- **Per‑Region Data Residency Controls**

  - All auth data will reside in the default multi‑region location; dedicated EU/US shards are out of scope for v‑0.
  - We’ll revisit when EU multi‑region becomes Generally Available in Firebase.

# 8. Non-Functional Requirements (cross-cutting)

|  **Area**                      |  **Requirement**                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           |
|:-------------------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Security**                   | *Credential storage* SHALL be delegated to **Firebase Authentication** (Google‑managed salted hashes). *Token integrity* SHALL rely on Firebase‑signed ID‑tokens verified with the Admin SDK’s cached public keys.  *Outbound email* (e.g., password reset) SHALL be sent via Firebase‑hosted pages; when custom templates are introduced, the sending domain MUST have valid SPF and DKIM records. Custom claims MUST NOT contain sensitive personal data beyond coarse roles / status” (reinforces GDPR) |
| **Availability**               | Identity API endpoints and supporting Cloud Functions SHALL achieve **≥ 99.95 %** monthly uptime by leveraging Firebase’s SLA. If Auth or Cloud Functions SLAs are revised upward, this target SHALL track the higher SLA.                                                                                                                                                                                                                                                                                 |
| **Performance / Scalability**  | The service SHALL sustain **500 verified ID‑token calls/s** at p95 < 50 ms without horizontal scaling; higher load is expected to be absorbed client‑side because token verification is local to each service. Measured from gRPC ingress to response (excluding network) on x86/ARM baseline                                                                                                                                                                                                              |
| **Data Retention**             | Firebase Auth logs exported to BigQuery MUST be retained **≥ 180 days** (archiving older data is permitted).                                                                                                                                                                                                                                                                                                                                                                                               |
| **Compliance & Extensibility** | The data model SHALL accommodate future additions such as KYC attributes or MFA flags without breaking existing APIs. No personally identifiable information beyond `email` and `display_name` may be stored inside custom claims.                                                                                                                                                                                                                                                                         |
| **Observability**              | All auth‑related warnings and errors (fuse trips, custom‑claim write failures, Admin SDK revocation calls) SHALL be sent to central logging stack with severity ≥ WARNING and exposed via Cloud Monitoring alerts.                                                                                                                                                                                                                                                                                         |
