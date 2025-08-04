
[Games Platform](..\..\..\Games%20Platform.md) > [2025-06](..\..\2025-06.md) > [Identity Context](..\Identity%20Context.md)

# Identity Context Implementation Plan

# Contents

- [A. Project & Local Scaffold (≈ 7 h)](#a-project-local-scaffold-7-h)
- [B. Database & Migrations (≈ 3 h)](#b-database-migrations-3-h)
- [C. Custom-Claim Cloud Functions (≈ 4 h)](#c-custom-claim-cloud-functions-4-h)
- [D. Identity Admin gRPC Service (≈ 5 h)](#d-identity-admin-grpc-service-5-h)
- [E. Rate-Limit Fuse (≈ 2 h)](#e-rate-limit-fuse-2-h)
- [F. API Gateway (≈ 3 h)](#f-api-gateway-3-h)
- [G. Claim-Drift Auditor (≈ 2.5 h)](#g-claim-drift-auditor-2-5-h)
- [H · Events & Messaging (≈ 1.5 h)](#h-events-messaging-1-5-h)
- [I. Admin Web UI (≈ 3 h)](#i-admin-web-ui-3-h)
- [J. Observability (≈ 1 h)](#j-observability-1-h)
- [K · Docs & E2E (≈ 2 h)](#k-docs-e2e-2-h)
- [L · Deploy to Cloud Run (≈ 2 h)](#l-deploy-to-cloud-run-2-h)

# A. Project & Local Scaffold (≈ 7 h)

|  **ID**    |  **What (step-by-step in brief)**                                                                                                                                                                                                       |  **Where**     |  **Test / verify**                               |    **Est.**  |
|:-----------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|:---------------|:-------------------------------------------------|-------------:|
| **A-00**   | Initialize Git repo, push to GitHub/Bitbucket. Folders `src/` & `infra/`                                                                                                                                                                | root           | `git clone` to temp dir opens & builds           |          0.3 |
| **A-01**   | Install & auth **gcloud CLI** (`gcloud init`)                                                                                                                                                                                           | —              | `gcloud projects list` works                     |          0.3 |
| **A-02**   | `gcloud projects create identity-ctx-dev` + link billing                                                                                                                                                                                | GCP            | Project appears in Console                       |          0.5 |
| **A-03**   | `gcloud services enable run.googleapis.com pubsub.googleapis.com sqladmin.googleapis.com secretmanager.googleapis.com`                                                                                                                  | GCP            | `gcloud services list --enabled` shows 4         |          0.2 |
| **A-04**   | Create Cloud SQL Postgres 15 instance (db-n1-dev, 10 GB) + public IP                                                                                                                                                                    | infra/         | `psql` connects from laptop                      |          0.7 |
| **A-05**   | Create *two* Firebase projects → **Games-Auth** & **Edgar-Auth** (Console)                                                                                                                                                              | Firebase       | Console shows project, default web app, Auth tab |          1   |
| **A-06**   | In each Firebase project: enable Email/Password, Google, GitHub, X providers                                                                                                                                                            | Firebase       | Manual sign-up in console works                  |          0.7 |
| **A-07**   | `infra/docker-compose.yml` with • postgres:15 • [gcr.io/google.com/cloudsdk/tool:](http://gcr.io/google.com/cloudsdk/tool:) Pub/Sub emulator • `firebase/emulator` (auth only)                                                          | infra/         | `docker compose up` and `nc localhost 8085` open |          1   |
| **A-08**   | Create **VS solution** `Identity.sln` containing empty projects: • Identity.Host (Orleans silo) • Identity.Gateway (API GW) • Identity.Grains (class lib) • Identity.Protos (gRPC contracts) • Identity.Common (shared)• Identity.Tests | src/           | `dotnet build` green                             |          0.5 |
| **A-09**   | Add Orleans packages, basic `UseLocalhostClustering()` host code in Host & Gateway                                                                                                                                                      | Host & Gateway | Both console apps run locally                    |          0.8 |

# B. Database & Migrations (≈ 3 h)

|  **ID**    |  **What**                                                                                           |  **Where**     |  **Test / verify**               |    **Est.**  |
|:-----------|:----------------------------------------------------------------------------------------------------|:---------------|:---------------------------------|-------------:|
| **B-01**   | Write initial **Evolve** migration `V1__baseline.sql` (tables `users`, `user_roles`, `user_status`) | db/migrations  | `dotnet run -- migrate` succeeds |          0.8 |
| **B-02**   | Integrate your persistence lib into **Identity.Common**; wrap connection factory                    | Common         | Simple unit test inserts user    |          1   |
| **B-03**   | Seed script for local Postgres (Docker)                                                             | infra/seed.sql | `psql` shows seed rows           |          0.5 |
| **B-04**   | README section: “Running DB locally & applying migrations”                                          | docs           | Copy-paste commands work         |          0.3 |

# C. Custom-Claim Cloud Functions (≈ 4 h)

|  **ID**    |  **What**                                                                                        |  **Where**    |  **Test / verify**                             |    **Est.**  |
|:-----------|:-------------------------------------------------------------------------------------------------|:--------------|:-----------------------------------------------|-------------:|
| **C-01**   | Install Firebase CLI; `firebase init functions` (choose .NET 6) in `functions/`                  | functions/    | `dotnet run` under emulator                    |          0.5 |
| **C-02**   | Function **OnCreateUser**: • insert row via REST to Gateway (or direct SQL) • set default claims | functions/    | Register via Emulator UI → DB row appears      |          1   |
| **C-03**   | Publish **UserCreated** message to local Pub/Sub emulator (`PubsubClient`)                       | functions/    | `gcloud pubsub topics list` shows msg count ↑  |          0.5 |
| **C-04**   | Add **beforeSignIn** trigger → refresh custom claims if stale                                    | functions/    | Toggle role in DB → next sign-in has new claim |          0.8 |
| **C-05**   | `firebase deploy --only functions` to Games-Auth project                                         | —             | Live signup gives claims                       |          1.2 |

# D. Identity Admin gRPC Service (≈ 5 h)

|  **ID**    |  **What**                                                                                          |  **Where**    |  **Test / verify**                     |    **Est.**  |
|:-----------|:---------------------------------------------------------------------------------------------------|:--------------|:---------------------------------------|-------------:|
| **D-01**   | Draft `identity_admin.proto` (messages & 4 RPCs) in **Identity.Protos**; `Grpc.Tools`              | Protos        | `dotnet build` generates C# stubs      |          0.7 |
| **D-02**   | Add **AdminGrain** interface & class in **Identity.Grains** (per-User grain)                       | Grains        | Unit test activates grain              |          0.8 |
| **D-03**   | Implement read path: grain → persistence lib → DB                                                  | Grains        | gRPC `GetUser` returns row             |          0.8 |
| **D-04**   | Implement `SetUserStatus`, `UpdateUserRoles`: • write DB • call Firebase Admin SDK to patch claims | Grains        | Status flips, claim visible next login |          1   |
| **D-05**   | Emit Pub/Sub **UserStatusChanged** / **UserRolesUpdated** events                                   | Grains        | Emulator topic receives msg            |          0.4 |
| **D-06**   | Configure mTLS: self-signed root; `dotnet dev-certs` import                                        | Host          | `grpcurl -insecure ...` succeeds       |          0.8 |
| **D-07**   | xUnit tests for each RPC using Orleans TestCluster                                                 | Tests         | `dotnet test` green                    |          0.5 |

# E. Rate-Limit Fuse (≈ 2 h)

|  **ID**    |  **What**                                                                     |  **Where**    |  **Test / verify**            |    **Est.**  |
|:-----------|:------------------------------------------------------------------------------|:--------------|:------------------------------|-------------:|
| **E-01**   | `IFuseGrain : IGrainWithStringKey` in Grains; state = sliding window list     | Grains        | Unit test: 10 hits ⇒ `Trip`   |          0.8 |
| **E-02**   | Middleware in **Identity.Gateway**: before routing, `FuseGrain.Increment(ip)` | Gateway       | Postman: 11th bad creds ⇒ 429 |          0.7 |
| **E-03**   | Add metric counter & log when `Trip`                                          | Gateway       | Cloud Logging entry visible   |          0.5 |

# F. API Gateway (≈ 3 h)

|  **ID**    |  **What**                                                         |  **Where**    |  **Test / verify**          |    **Est.**  |
|:-----------|:------------------------------------------------------------------|:--------------|:----------------------------|-------------:|
| **F-01**   | Minimal ASP.NET Core API; verify Firebase ID token with Admin SDK | Gateway       | CURL with valid token ⇒ 200 |          0.8 |
| **F-02**   | Authorize by claim (`roles` contains “player”)                    | Gateway       | Token w/out role ⇒ 403      |          0.3 |
| **F-03**   | Integrate Fuse (E-02)                                             | Gateway       | Repeat failed logins ⇒ 429  |          0.4 |
| **F-04**   | Proxy route `/hello` to placeholder; returns user id              | Gateway       | Browser GET works           |          0.5 |
| **F-05**   | Unit tests for token validation & policy                          | Tests         | `dotnet test`               |          1   |

# G. Claim-Drift Auditor (≈ 2.5 h)

|  **ID**    |  **What**                                                                          |  **Where**     |  **Test / verify**                       |    **Est.**  |
|:-----------|:-----------------------------------------------------------------------------------|:---------------|:-----------------------------------------|-------------:|
| **G-01**   | Console app `drift-auditor/Program.cs`: for each DB user → compare Firebase claims | drift-auditor/ | Run locally against emulator             |          1   |
| **G-02**   | Dockerfile + deploy to Cloud Run Job (`gcloud run jobs create ...`)                | infra/         | `gcloud run jobs execute ...` shows logs |          0.8 |
| **G-03**   | Cloud Scheduler cron 02:00 UTC daily                                               | GCP            | Next run fires job                       |          0.5 |
| **G-04**   | Email or Pub/Sub alert on mismatch count > 0                                       | drift-auditor/ | Inject fake mismatch ⇒ alert             |          0.2 |

# H · Events & Messaging (≈ 1.5 h)

|  **ID**    |  **What**                                                                        |  **Where**    |  **Test / verify**              |    **Est.**  |
|:-----------|:---------------------------------------------------------------------------------|:--------------|:--------------------------------|-------------:|
| **H-01**   | `gcloud pubsub topics create identity.events`                                    | GCP           | `gcloud pubsub topics list`     |          0.2 |
| **H-02**   | `gcloud pubsub subscriptions create identity.events.local --topic ...`           | GCP           | Pull shows msgs                 |          0.2 |
| **H-03**   | IAM: add Service Accounts for Host, Functions; `roles/pubsub.publisher`          | GCP           | Pub/Sub IAM tab shows bindings  |          0.3 |
| **H-04**   | Shared C# helper `EventPublisher` in Common (wraps `TopicName`, `JsonFormatter`) | Common        | Unit test publishes to emulator |          0.5 |
| **H-05**   | Local dev: emulator set `PUBSUB_EMULATOR_HOST` var in compose                    | infra/        | Function publishes locally      |          0.3 |

# I. Admin Web UI (≈ 3 h)

|  **ID**    |  **What**                                                                 |  **Where**    |  **Test / verify**               |    **Est.**  |
|:-----------|:--------------------------------------------------------------------------|:--------------|:---------------------------------|-------------:|
| **I-01**   | `dotnet new blazorserver -n Identity.AdminUi`                             | src/          | App runs on <https://localhost>    |          0.3 |
| **I-02**   | Add gRPC client to Admin UI; inject certs                                 | AdminUi       | Fetch user by ID                 |          0.5 |
| **I-03**   | Page “User Details” – show roles, status, buttons “Ban / Unban / Promote” | AdminUi       | Click button updates DB & claims |          1   |
| **I-04**   | SSE or SignalR “Event Log” – stream from Pub/Sub sub via HTTP handler     | AdminUi       | New event appears within 2 s     |          0.7 |
| **I-05**   | Cypress or Playwright smoke test: login, ban user, unban                  | tests/        | `npm run test` green             |          0.5 |

# J. Observability (≈ 1 h)

|  **ID**    |  **What**                                                   |  **Where**     |  **Test / verify**           |    **Est.**  |
|:-----------|:------------------------------------------------------------|:---------------|:-----------------------------|-------------:|
| **J-01**   | Enrich logs with `TraceId`, `UserId`, `Role` (Serilog-Json) | Host & Gateway | Log entry shows fields       |          0.5 |
| **J-02**   | Cloud Monitoring alert: `fuse_trip_count > 100` / 5 min     | GCP            | Set threshold low, see email |          0.5 |

# K · Docs & E2E (≈ 2 h)

|  **ID**    |  **What**                                                           |  **Where**    |  **Test / verify**    |    **Est.**  |
|:-----------|:--------------------------------------------------------------------|:--------------|:----------------------|-------------:|
| **K-01**   | Markdown: local dev guide, build & deploy cheatsheet                | docs/         | New dev follows guide |          0.8 |
| **K-02**   | Bash script `e2e.sh`: create user → call Gateway → ban → expect 403 | scripts/      | `bash e2e.sh` passes  |          1.2 |

# L · Deploy to Cloud Run (≈ 2 h)

|  **ID**    |  **What**                                                              |  **Where**    |  **Test / verify**         |    **Est.**  |
|:-----------|:-----------------------------------------------------------------------|:--------------|:---------------------------|-------------:|
| **L-01**   | Write multistage Dockerfile for Host & Gateway (copy build artefacts)  | src/          | `docker build` succeeds    |          0.8 |
| **L-02**   | `gcloud run deploy identity-host` & `identity-gateway` (min-scale = 0) | GCP           | HTTPS endpoint returns 200 |          0.7 |
| **L-03**   | Secret Manager: store DB conn string; mount via `--set-secrets`        | GCP           | Pod sees env var           |          0.5 |
