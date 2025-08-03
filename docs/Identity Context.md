
[Games Platform](..\..\Games%20Platform.md) > [2025-06](..\2025-06.md)

# Identity Context

# Contents

- [1. Core purpose (play-money phase)](#key-1-core-purpose-play-money-phase)
- [2. Public API / Events](#key-2-public-api-events)
- [3. Downstream consumers](#key-3-downstream-consumers)
  - [3.1. How a downstream service verifies a Firebase ID‑token](#key-3-1-how-a-downstream-service-verifies-a-firebase-id-token)

# 1. Core purpose (play-money phase)

Handle registration, login, token issuance, password reset, basic rate-limiting.

# 2. Public API / Events

|  **Endpoint / Event**    |  **Verb**                     |  **Notes**                       |
|:-------------------------|:------------------------------|:---------------------------------|
| `POST /register`         | REST                          | email + password → `UserCreated` |
| `POST /auth`             | REST                          | returns JWT                      |
| `POST /refresh`          | REST                          | renews JWT                       |
| **Event** `UserCreated`  | Kafka topic `identity.events` |                                  |

# 3. Downstream consumers

Wallet · Game-Session · Admin UI (read-only)

## 3.1. How a downstream service verifies a Firebase ID‑token

1. **Include the Admin SDK** in the service and initialize it once with a service‑account key.
2. When a request arrives, pull the token from the `Authorization: Bearer …` header.
3. Call the SDK’s verify method (`verifyIdToken()` in Node, Python, Go, etc.).

   - The SDK checks the signature against Google’s public certs, validates `aud`, `iss`, `exp`, and (optionally) whether the token has been revoked.
   - The certs are cached in memory; a fresh network call is needed only when Google rotates keys (roughly once a day). So **there is no round‑trip to Firebase on every request**.
4. The method returns a decoded token object. Your code reads `roles`, `status`, or other custom claims and decides whether to serve the request.
