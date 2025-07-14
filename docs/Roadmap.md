
[Games Platform](..\..\Games%20Platform.md) > [2025-06](..\2025-06.md)

# Roadmap

# Contents

- [Contents](#contents)
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
| **Identity**        | User, LoginCredential, Profile | PostgreSQL table(s)                                    | KYC docs, risk flags            |
| **Wallet**          | Account, Balance, TxEntry      | Postgres *double-entry* tables (`accounts`, `entries`) | FX tables, AML flags            |
| **Game-Session**    | Session, Bet, Outcome          | Event table or Kafka topic; snapshot in Postgres       | Game-state archiving for audits |
| **AdminUI**         | Settings, FeatureFlags         | Postgres                                               | Role-based ACL                  |

*Design artefact*: a context map + ER diagram.  
*Why now*: locks in stable IDs (`user_id`, `account_id`, `session_id`) that every other piece plugs into.

# 2. Wallet and ledger first

- Implement a **double-entry** schema (credit/debit rows).
- Separate “**coin**” (play-money) from “**cash**” currency codes *now* → swapping in PSP rows later is trivial.
- Expose a single idempotent gRPC endpoint:

  `PostTransaction(request) => {success, new_balance}`
- Store every post in Postgres plus stream to a Kafka topic `wallet.entries` for later analytics.

# 3. Identity & auth

- Phoenix + Pow (or your favourite) for email / social login.
- Generate short-lived **access tokens** (JWT with `user_id`, `device_id`) for WebSocket auth.
- Phoenix.Token or Plug attack for **rate-limiting** by `user_id` + `ip` (ETS counter) → blocks client scripts in play-money mode.

# 4. Game adaptor contract

Create an Erlang **behaviour** (or protobuf service) that every game server must implement:

`@callback init_session(user_id, game_id, opts) :: {:ok, session_id}`  
`@callback place_bet(session_id, wager, meta) :: {:ok, tx_ref}`  
`@callback settle(session_id, outcome) :: {:ok, result_map}`

- Slots & poker back-ends live in their own OTP apps; they talk to Wallet via gRPC or internal messages.
- This lets you swap a Java-based slot engine tomorrow without touching Identity or Wallet.

# 5. Real-time infrastructure

- **PubSub**: Phoenix.PubSub cluster for lobby chat, table lists, and jackpot broadcasts.
- **Broadway**: use it to consume Kafka topics such as `wallet.entries` for leaderboards.  
  *For jackpots*: aggregate every 3 s; push the number out to subscribed clients.  
  *For leaderboards*: update top-N cache every 30–60 s (good UX, light DB load).

# 6. Supervisors & disaster recovery

- Every stateful process (e.g., a live poker table) is a **GenServer** under a `DynamicSupervisor`.
- Persist *all* state changes as events (`table.events` Kafka topic) → on crash, replay to restore; on scale-out, a second node can take over.
- Run Postgres in HA (Patroni or CloudSQL) and enable WAL archiving; losing a node ≠ losing balances.

# 7. Admin UI Skeleton

Phoenix LiveView:

- Users & balances list with Free-text search.
- Toggle feature flags (e.g., enable “auto-spin”).
- Real-time stream of the last 100 ledger posts (helps you QA bet flows).

# 8. Deployment topology (10k CCU target)

┌──────────┐ gRPC ┌──────────┐  
│ Games │◀─────▶│ Wallet │  
│(OTP apps)│ │ API │  
└────┬─────┘ └────┬────┘  
 │ WS/Phoenix │ SQL  
┌────▼─────┐ ┌───▼────┐  
│ Ingress │ ↔ clients │ Postgres │  
└──────────┘ └────────┘  
 ↕ Kafka (events) ↕  
┌─────────────┐ ┌──────────┐  
│ Leaderboard │ │ AdminLive │  
└─────────────┘ └──────────┘

- One BEAM node handles ~50 k lightweight processes; two nodes behind Nginx/Envoy cover 10 k users easily.
- Kafka + Postgres give horizontal analytics potential without touching the game loop.

# 9. Later “real money” switches

|  Feature             |  Enable by…                                                                      |  Already prepared                          |
|:---------------------|:---------------------------------------------------------------------------------|:-------------------------------------------|
| PSP / card acquiring | Add `payment_provider` table + webhook consumer; post cleared deposits to Wallet | Separate currency codes, idempotent ledger |
| KYC/AML              | Extend Identity with `kyc_state`; create workflow service                        | User ID centralised                        |
| Regulators’ audit    | Snapshot `table.events` nightly to S3 / BigQuery                                 | Event streams exist                        |
| Risk engine          | Consume `wallet.entries`; run rules in separate Elixir app                       | Kafka topic ready                          |

# Open threads (feel free to answer later)

1. **Randomness / fairness** – RNG on server or certified hardware RNG?
2. **Geo-blocking** – plan to block IPs from real-money-restricted regions?
3. **Data residency** – any region constraints (e.g., EU vs US)?

Nail the wallet schema and the game adaptor next; everything else will fall neatly into place. Enjoy building the BEAM-powered casino!
