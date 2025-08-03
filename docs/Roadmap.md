
[Games Platform](..\..\Games%20Platform.md) > [2025-06](..\2025-06.md)

# Roadmap

# Contents

- [1. Core service boundaries & data model](#key-1-core-service-boundaries-data-model)
- [2. Wallet and ledger first](#key-2-wallet-and-ledger-first)
- [3. Identity & auth](#key-3-identity-auth)
- [4. Game adaptor contract](#key-4-game-adaptor-contract)
- [5. Real-time infrastructure](#key-5-real-time-infrastructure)
- [6. Supervisors & disaster recovery](#key-6-supervisors-disaster-recovery)
- [7. Admin UI Skeleton](#key-7-admin-ui-skeleton)
- [8. Deployment topology (10k CCU target)](#key-8-deployment-topology-10k-ccu-target)
- [9. Later “real money” switches](#key-9-later-real-money-switches)
- [Open threads (feel free to answer later)](#open-threads-feel-free-to-answer-later)

# 1. Core service boundaries & data model

|  Bounded context    |  Key data                      |  Persistence now                                       |  Adds later (real-money)        |
|:--------------------|:-------------------------------|:-------------------------------------------------------|:--------------------------------|
| **Identity**        | User, LoginCredential, Profile | Database table(s)                                      | KYC docs, risk flags            |
| **Wallet**          | Account, Balance, TxEntry      | Database *double-entry* tables (`accounts`, `entries`) | FX tables, AML flags            |
| **Game-Session**    | Session, Bet, Outcome          | Event table or Pub-sub topic; snapshot in database     | Game-state archiving for audits |
| **AdminUI**         | Settings, FeatureFlags         | Database                                               | Role-based ACL                  |

*Design artefact*: a context map + ER diagram.  
*Why now*: locks in stable IDs (`user_id`, `account_id`, `session_id`) that every other piece plugs into.

# 2. Wallet and ledger first

- Implement a **double-entry** schema (credit/debit rows).
- Separate “**coin**” (play-money) from “**cash**” currency codes *now* → swapping in PSP rows later is trivial.
- Expose a single idempotent endpoint: `PostTransaction(request) => {success, new_balance}`
- Store every post in database plus stream to an event-stream topic `wallet.entries` for later analytics.

# 3. Identity & auth

- Use an OAuth 2.0/OIDC provider for email and social login
- **Delegate all credential flows to Firebase Auth** (Email + Password, Google, Apple, X, etc.)
- Down-stream services verify Firebase **ID-tokens locally via Admin SDK** (stateless)
- Store `firebase_uid → user_id` mapping and **custom claims** (`roles[]`, `status`) in Postgres (≤ 1 KB)
- Local cache-based rate-limit remains, but **only on your own APIs**
- MVP uses **one Firebase project**; split into separate tenants later if custom-claim bloat approaches the 1 KB limit

# 4. Game adaptor contract

Define a service contract (e.g., Protocol Buffers / gRPC or JSON-RPC) with operations:

`InitSession(user_id, game_id, opts) -> session_id`  
`PlaceBet(session_id, wager, meta) -> transaction_ref`  
`Settle(session_id, outcome) -> results`

- Any game engine that implements the contract can plug into Wallet via the chosen RPC transport

# 5. Real-time infrastructure

- Publish/Subscribe layer for lobby chat, table lists, jackpot broadcasts (e.g., Redis Streams, NATS, or a managed pub-sub service).
- Stream-processing pipeline that consumes `wallet.entries` to build leaderboards and jackpot tallies. Runs every 3-60 s depending on freshness requirements
- Evaluate **Orleans Streams** for purely in-cluster fan-out (chat, table lists) and compare Cloud Pub/Sub cost & latency; park decision in an ADR.

# 6. Supervisors & disaster recovery

- Each live table/match is an **Orleans grain**;

  - Events persisted to `table.events`.
  - On failure, replay events to a new instance; on scale-out, another node can assume the workload

# 7. Admin UI Skeleton

Real-time web admin dashboard (any SPA + server push):

- Searchable user and balances list
- Toggle feature flags (e.g., enable “auto-spin”).
- Real-time stream of the last *N* ledger posts (helps you QA bet flows).

# 8. Deployment topology (10k CCU target)

- Scale horizontally by adding instances behind the gateway.
- Message broker + relational database give horizontal analytics potential without touching the game loop.
- **Start on Cloud Run** (cheapest path to production); add a *decision checkpoint* to evaluate GKE once load-testing approaches 10 k CCU
- Orleans clustering via **Redis membership** first; migrate to GKE DNS-based membership if we move to Kubernetes.

# 9. Later “real money” switches

|  Feature             |  Enable by…                                                                      |  Already prepared                          |
|:---------------------|:---------------------------------------------------------------------------------|:-------------------------------------------|
| PSP / card acquiring | Add `payment_provider` table + webhook consumer; post cleared deposits to Wallet | Separate currency codes, idempotent ledger |
| KYC/AML              | Extend Identity with `kyc_state`; create workflow service                        | User ID centralised                        |
| Regulators’ audit    | Snapshot `table.events` nightly to S3 / BigQuery                                 | Event streams exist                        |
| Risk engine          | Consume `wallet.entries`; run rules in separate app                              | Web broker topic ready                     |

# Open threads (feel free to answer later)

1. **Randomness / fairness** – RNG on server or certified hardware RNG?
2. **Geo-blocking** – plan to block IPs from real-money-restricted regions?
3. **Data residency** – any region constraints (e.g., EU vs US)?
4. ⚠ **Custom-claim size** – monitor roles/status list; Firebase limit 1 KB.
5. 💰 **Cloud Run vs GKE TCO** – revisit after load test.

Nail the wallet schema and the game adaptor next; everything else will fall neatly into place.
