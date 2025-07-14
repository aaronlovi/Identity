
[Games Platform](..\..\..\Games%20Platform.md) > [2025-06](..\..\2025-06.md) > [Identity Context](..\Identity%20Context.md)

# Identity Context Requirements

- [1. Primary Responsibilities (Functional Scope)](#key-1-primary-responsibilities-functional-scope)
- [2. Credential Flows](#key-2-credential-flows)
- [3. Session & Token Model](#key-3-session-token-model)
- [4. Rate-Limiting & Abuse Protection](#key-4-rate-limiting-abuse-protection)
- [5. Events Emitted](#key-5-events-emitted)
- [6. gRPC Surface (Internal Only)](#key-6-grpc-surface-internal-only)
- [7. Non-Functional Requirements (cross-cutting)](#key-7-non-functional-requirements-cross-cutting)
  - [7.A. Non-functional requirements - extra notes](#key-7-a-non-functional-requirements-extra-notes)
- [8. Out-of-Scope for v-0](#key-8-out-of-scope-for-v-0)

# 1. Primary Responsibilities (Functional Scope)

|  #    |  Requirement                                                                                                         |
|:------|:---------------------------------------------------------------------------------------------------------------------|
| R-1   | The service **shall** authenticate players and staff accounts using one or more credentials.                         |
| R-2   | The service **shall** provision new user records and link additional credentials to existing users.                  |
| R-3   | The service **shall** issue, refresh, validate, and revoke access/refresh tokens that downstream services can trust. |
| R-4   | The service **shall** expose identity and token-validation data to other back-end services via an internal API.      |
| R-5   | The service **shall** capture an immutable audit trail of login attempts and session activity.                       |

---

# 2. Credential Flows

|  Flow                                          |  Requirements                                                                                                                                                                                                  |
|:-----------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Email + Password – Registration**            | The service shall: (a) validate password strength; (b) hash with an industry-standard algorithm; (c) create a user record; (d) emit a `UserCreated` event.                                                     |
| **Email + Password – Login**                   | It shall verify the hash, reject on mismatch, create a session on success, and record `LoginSucceeded` / `LoginFailed`.                                                                                        |
| **OAuth – Google, Apple, Facebook, X/Twitter** | The service shall support token verification for each provider’s OpenID-Connect compliant endpoint. It shall auto-provision a user if none exists or link to an existing user when the verified email matches. |
| **Credential Linking**                         | Authenticated users shall add or remove secondary credentials (e.g., set a password after Google signup). Conflicting credentials shall be rejected.                                                           |
| **Password Reset**                             | The service shall generate one-time reset tokens, deliver them via email/SMS through an outbox, and invalidate prior sessions after a successful reset.                                                        |
| **Account Status Handling**                    | For every flow, the service shall check user status (`active`, `banned`, `shadow_banned`) and respond accordingly.                                                                                             |

---

# 3. Session & Token Model

|  Requirement                                                                                                                |
|:----------------------------------------------------------------------------------------------------------------------------|
| The service shall create a **Session** record for each successful login, containing user, device, and IP metadata.          |
| The service shall issue **access tokens** with ≤15-minute TTL and **refresh tokens** with ≤30-day TTL.                      |
| Tokens shall carry user roles and an immutable `jwt_id` claim.                                                              |
| The service shall support *global* logout by marking sessions revoked and ensuring token introspection detects revocation.  |
| Device binding is optional but, if present, the token shall include a `device_id` claim and be rejected from other devices. |

---

# 4. Rate-Limiting & Abuse Protection

|  Requirement                                                                                    |
|:------------------------------------------------------------------------------------------------|
| The service shall track failed login counts per user, credential, and IP over a sliding window. |
| It shall return a “blocked” response with a **Retry-After** hint when thresholds are exceeded.  |
| The gateway shall be able to request the current block status via an internal call.             |
| The service shall expose metrics suitable for alerting on brute-force or bot behavior.          |

---

# 5. Events Emitted

|  Event           |  Mandatory Payload                                     |  Purpose                                    |
|:-----------------|:-------------------------------------------------------|:--------------------------------------------|
| `UserCreated`    | `user_id`, `email`, `timestamp`                        | Downstream wallet auto-setup, analytics.    |
| `LoginSucceeded` | `user_id`, `credential_type`, `device_id`, `timestamp` | Security posture, DAU metrics.              |
| `LoginFailed`    | `credential_identifier`, `reason`, `ip`, `timestamp`   | Fraud detection.                            |
| `SessionRevoked` | `jwt_id`, `user_id`, `revoked_at`                      | Gateway cache invalidation; support audits. |

All events **shall** be published to the platform’s message bus (Kafka topic `identity.events`) within 500 ms of the triggering action.

---

# 6. gRPC Surface (Internal Only)

|  Method               |  Requirement                                                                                                           |
|:----------------------|:-----------------------------------------------------------------------------------------------------------------------|
| `ValidateToken`       | Shall return user context (`user_id`, roles, status) for a supplied token or an error code if invalid/revoked/expired. |
| `GetUserById`         | Shall return the same context when downstream services only have a `user_id`.                                          |
| `RevokeSession`       | Shall mark a session revoked and emit `SessionRevoked`.                                                                |
| `CreateSystemSession` | Shall issue service-to-service tokens for non-player daemons (e.g., Leaderboard).                                      |

All internal calls **shall** complete in < 50 ms at p95 under expected load (10 k CCU).

---

# 7. Non-Functional Requirements (cross-cutting)

- **Security** – Passwords stored with Argon2id (or Bcrypt 12+); OAuth tokens verified against provider public keys; all outbound email via [Mail configuration (SPF/DKIM/DMARC)](Mail%20configuration%20(SPF_DKIM_DMARC).md).
- **Availability** – Identity endpoints must achieve ≥ 99.9 % monthly uptime; token validation must continue during provider outages.
- **Scalability** – Sustain 100 login POST/s peak and 2 000 `ValidateToken` calls/s with linear horizontal scaling.
- **Data Retention** – Login audit records retained ≥ 180 days; older data may be archived.
- **Compliance Ready** – Data model must allow future additions of KYC and 2FA attributes without breaking existing APIs.

## 7.A. Non-functional requirements - extra notes

|  Clause                                                  |  What it really means                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    |  Why it matters                                                                                                                                                                                                                        |
|:---------------------------------------------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **“Passwords stored with Argon2id (or Bcrypt 12+)”**     | When you keep a user’s password in the database, you **never** store it as-is. You first run it through a *slow*, cryptographically strong hash function. • **Argon2id** is the current state-of-the-art algorithm—designed to be expensive in both CPU *and* memory, which blocks modern GPU/brute-force attacks. • **Bcrypt 12+** is an older but still respectable alternative; **“12+”** refers to the cost factor (work factor). The bigger the number, the slower each hash calculation, which makes offline cracking impractical. | If an attacker ever steals your user database, the hashes are so slow to crack that the stolen passwords are effectively useless.                                                                                                      |
| **“OAuth tokens verified against provider public keys”** | When someone logs in with Google / Apple / Facebook, your server receives an **ID-token** (a signed JWT). You must check its signature using the official public keys that the provider publishes via *their OpenID-Connect discovery endpoint*. If the signature and claims (audience, expiry) check out, you can trust the identity.                                                                                                                                                                                                   | Prevents attackers from forging a Google token and logging in as someone else. Verifying against the provider’s keys is the only reliable way to know the token is genuine.                                                            |
| **“All outbound email via SPF/DKIM-compliant domain”**   | Any password-reset or verification email you send **should originate from a domain that has valid SPF and DKIM DNS records**. • **SPF** (Sender Policy Framework) lists which mail servers are allowed to send on behalf of your domain. • **DKIM** (DomainKeys Identified Mail) adds a cryptographic signature that receiving mail servers can verify. See the following internal document link: [Mail configuration (SPF/DKIM/DMARC)](Mail%20configuration%20(SPF_DKIM_DMARC).md)                                                      | Major email providers (Gmail, Outlook, Apple) increasingly refuse or spam-folder messages from domains without SPF/DKIM. Using compliant records ensures your reset emails actually reach users and protects your brand from spoofing. |

---

# 8. Out-of-Scope for v-0

- Email verification loops (we assume verified emails are delivered via OAuth or implicit trust for early testing).
- Multi-factor authentication for player roles (reserved for future but required for `super_admin`).
- Social-graph or friend-list features (handled by a future Social context).
