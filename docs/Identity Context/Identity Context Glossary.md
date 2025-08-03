
[Games Platform](..\..\..\Games%20Platform.md) > [2025-06](..\..\2025-06.md) > [Identity Context](..\Identity%20Context.md)

# Identity Context Glossary

|  **Term**                           |  **Definition / Notes**                                                                                                                   |
|:------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------------|
| **User**                            | One human participant in the ecosystem, identified by a stable internal ID; can act as player and, with extra powers, as an administrator |
| **User status**                     | Account state: **active** (normal), **banned** (log-ins refused), **shadow\_banned** (user may log in but is hidden from others)          |
| **Role**                            | Label expressing powers (e.g., *player*, *super\_admin*, *support*). A user may hold multiple roles; authorization checks inspect roles   |
| **Profile**                         | Friendly attributes a player can edit (display name, avatar, locale); safe to cache/fan-out in lobbies                                    |
| **Credential**                      | Method the user proves identity (email + password, Google, Apple, etc.); decouples “how they sign in” from “who they are”                 |
| **Device**                          | Logical installation of the app on a physical device; identified by client-generated ID and tied to the user after first login            |
| **Session**                         | Back-end record of one authenticated login instance (“server-remembered cookie on steroids”) enabling “log out everywhere”                |
| **Access token (JWT)**              | Short-lived signed blob on every request; carries user-id, roles, device-id, expiry                                                       |
| **Refresh token**                   | Long-lived opaque secret stored once; exchanging it yields new access tokens, allowing rotation without re-auth                           |
| **Rate-limit bucket**               | In-memory counter keyed by user or device tracking recent request volume to thwart brute-force scripts                                    |
| **Login event**                     | Immutable audit record of each sign-in attempt (success/failure, timestamp, IP, device, credential) for fraud analytics                   |
| **Email service provider (ESP)**    | Hosted service that delivers email (SendGrid, Mailgun, SES, etc.) used for password-reset / verification emails                           |
| **Claim**                           | Name → value pair in a JWT; Firebase ID-tokens include Google claims plus up to 1 KB of custom claims such as `roles` and `status`        |
| **Firebase Authentication**         | Google-managed service issuing ID/refresh tokens, hosting sign-up/in & enforcing App Check / reCAPTCHA                                    |
| **Firebase UID**                    | Immutable 28-char string uniquely identifying a user in Firebase Auth (e.g., `O7RSp8Ecz9a6Wj…`)                                           |
| **User ID**                         | Internal `INT64` primary key mapped 1-to-1 to a Firebase UID and referenced by back-end services                                          |
| **ID-token**                        | Firebase-signed JWT (~60 min TTL) presented on every API call; contains Google and custom claims                                          |
| **Custom claim**                    | Server-set name/value (e.g., `roles`, `status`) that rides in every new ID-token; readable by clients, writable only by trusted code      |
| **Identity Admin Service**          | Internal mTLS gRPC service for operators to get users, change roles/status, mint custom tokens & publish events                           |
| **Custom-Claim Function**           | Cloud Function triggered on `onCreate` / `beforeSignIn`; sets or refreshes custom claims and creates the `users` row                      |
| **Claim-Drift Auditor**             | Nightly Cloud Run job reconciling DB truth with current custom claims, logging mismatches                                                 |
| **Abuse-Protection Fuse**           | In-process (or Redis-backed) sliding-window counter blocking `(uid ∨ email) × IP` after 10 failed actions in 10 min; returns **HTTP 429** |
| **API Gateway**                     | Single public ingress that terminates TLS, verifies ID-tokens once, applies per-route quotas, and forwards to services                    |
| **Pub/Sub topic** `identity.events` | At-least-once event bus carrying `UserCreated`, `UserStatusChanged`, `UserRolesUpdated` CloudEvents JSON                                  |
| **UserCreated**                     | Domain event emitted when a Firebase user is first provisioned; payload includes `user_id`, `firebase_uid`, initial roles/status          |
| **UserStatusChanged**               | Event emitted whenever `status` transitions (e.g., active → banned), triggering refresh-token revocation                                  |
| **UserRolesUpdated**                | Event emitted when roles are added/removed; carries full post-update role list                                                            |
| **Cloud SQL**                       | Managed PostgreSQL instance storing `users`, optional `user_roles`, `user_devices` tables                                                 |
| **mTLS**                            | Mutual TLS where both client and server present certs; caller identity derived from certificate CN                                        |
| **App Check / reCAPTCHA**           | Firebase client-side bot-defense mechanisms; first line of rate-limit protection for auth flows                                           |
| **CloudEvents**                     | CNCF JSON envelope standard (`specversion`, `type`, etc.) used for all events on **identity.events**                                      |
