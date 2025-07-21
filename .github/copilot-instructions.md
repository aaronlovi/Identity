# Identity Service - AI Coding Assistant Instructions

## Project Overview
This is the **Identity Context** microservice for a social gaming platform, designed initially for play-money gaming with future real-money readiness. It handles user authentication, authorization, session management, and identity events using a domain-driven design approach.

## Architecture & Tech Stack
- **.NET 8+ with C#** - Primary development platform
- **Orleans (Virtual Actors)** - Distributed state management and grain-based architecture
- **PostgreSQL** - Primary data store with double-entry ledger patterns
- **Kafka** - Event streaming (`identity.events` topic)
- **gRPC** - Internal service communication (AuthService, IdentityService)
- **REST API** - External client interfaces through gateway
- **JWT tokens** - Short-lived access tokens (≤15min) + refresh tokens (≤30day)

## Project Structure (Planned)
```
src/
├── Identity.Api/          # REST API layer, gateway interface
├── Identity.Domain/       # Core business logic, entities, value objects
├── Identity.GrainInterfaces/ # Orleans grain contracts
├── Identity.Grains/       # Orleans grain implementations
├── Identity.Grpc/         # gRPC service definitions and implementations
├── Identity.Infrastructure/ # Kafka, email, outbox, persistence
└── Identity.Benchmarks/   # Benchmarking project for tuning Argon2 parameters
```

## Key Domain Concepts
- **User**: Core identity with stable `user_id`, supports multiple roles (player, admin)
- **Credentials**: Multiple auth methods per user (email/password, OAuth providers)
- **Sessions**: Server-side auth state tracking, supports global logout
- **Devices**: Logical app installations, enables device-specific rate limiting
- **User Status**: Active, Banned, Shadow-banned (gaming-specific behavior)
- **Rate-limit Buckets**: In-memory counters keyed by user/device/IP
- **Argon2 Tuning**: Password hashing parameters (`Iterations`, `MemoryCost`) are benchmarked and tuned using the `Identity.Benchmarks` project.

## Critical Workflows

### Development Setup
```bash
# Solution structure creation
dotnet new sln -n Identity
dotnet new webapi -n Identity.Api
dotnet new classlib -n Identity.Domain
dotnet new grpc -n Identity.Grpc
# Add Orleans packages to appropriate projects
dotnet add Identity.Grains package Microsoft.Orleans.Server
```

### Key API Patterns
- **Registration**: `POST /v1/auth/register` → UserCreated event → Auto-provision wallet account
- **OAuth Flow**: Verify external tokens, auto-link by email, handle conflicts gracefully  
- **Token Refresh**: Stateless JWT with session validation for revocation checking
- **Rate Limiting**: ETS counters by `user_id + ip`, protects against brute force

### Event-Driven Integration
All identity changes flow through Kafka `identity.events` topic:
- `UserCreated` → Triggers wallet account creation
- `LoginSucceeded/Failed` → Audit trail, security monitoring
- `SessionRevoked` → Downstream session cleanup

## Gaming Platform Conventions
- **User IDs**: Always UUIDs, never expose internal sequences
- **Shadow Banning**: Users can login/play but others don't see their activity
- **Device Binding**: Optional but enables per-device logout and fraud detection
- **Audit Everything**: Immutable login events with IP, device, timestamp
- **Currency Separation**: Ready for coin (play-money) vs cash (real-money) currencies

## Security & Compliance
- **Password Hashing**: Use industry-standard algorithms (bcrypt/Argon2)
  - Argon2 parameters (`Iterations`, `MemoryCost`) are benchmarked and tuned using the `Identity.Benchmarks` project.
- **JWT Security**: Include `jwt_id` for revocation, validate against session state
- **Rate Limiting**: Implement before any authentication logic
- **Email Security**: Use outbox pattern, configure SPF/DKIM/DMARC (see mail config docs)

## Integration Points
- **Wallet Service**: User creation triggers account provisioning
- **Game Session Service**: Validates tokens, consumes user metadata
- **Admin UI**: Read-only access to user data, session management

## Development Notes
- **Start with data layer**: PostgreSQL schemas, migrations, then domain logic
- **Orleans Patterns**: Use grains for stateful operations (sessions, rate limits)
- **Event Sourcing**: Consider for audit requirements and future real-money compliance
- **Container Ready**: Docker support planned for deployment topology
- **Benchmarking**: Run the `Identity.Benchmarks` project to tune Argon2 parameters and update `appsettings.json` accordingly.

## Future Considerations
- **KYC Integration**: Hook points for real-money compliance
- **Multi-factor Auth**: SMS/TOTP support planned
- **Advanced Fraud**: ML-based detection patterns
- **Horizontal Scaling**: Design for 10k+ concurrent users

## Documentation Reference
All actions must refer to the `/docs` folder, especially under the `/docs/Identity Context` folder, for design, requirements, and implementation notes. These documents are paramount for understanding the Identity microservice and must always be included in the context for any task or implementation.

**Important**: Always include the `/docs` folder in the context of any task or implementation. This ensures that all design, requirements, and implementation notes are adhered to. Do not skip or selectively omit this step.

When implementing features, always consider the gaming context (shadow banning, play-money vs real-money readiness) and maintain event-driven patterns for downstream service integration.

## Code Quality Instructions
To maintain consistent and high-quality code, follow these conventions for class and record definitions:

1. **Order of Members**:
   - Constants at the top
   - Private fields
   - Private properties
   - Constructors
   - Public properties
   - Public methods
   - Private methods

2. **Naming Conventions**:
   - Use PascalCase for class, method, and property names.
   - Use camelCase for private fields, prefixed with an underscore (e.g., `_exampleField`).

3. **Documentation**:
   - Add XML comments (`///`) for all public classes, methods, and properties.
   - Include a summary, parameters, and return value descriptions where applicable.

4. **Error Handling**:
   - Constructors should throw exceptions for invalid inputs or unexpected conditions.
   - For other methods, prefer using the `InnoAndLogic.Shared.Result` and `InnoAndLogic.Shared.Result<T>` types to represent success or failure states.
   - The `InnoAndLogic.Shared` library is available on NuGet (https://www.nuget.org/packages/InnoAndLogic.Shared) and its source code can be found at https://github.com/aaronlovi/InnoAndLogic/tree/master/InnoAndLogic.Shared.

5. **Immutability**:
   - Prefer immutability for value objects (e.g., use `record` types).

6. **Enumerations**:
   - Enumerations should generally have as their first member an `Invalid = 0` item to represent an uninitialized or invalid state.

7. **Unit Tests**:
   - Write unit tests for all public methods and ensure 100% coverage for critical business logic.

8. **Documentation Adherence**:
   - Always include the `/docs` folder in the context of any task or implementation. This ensures that all design, requirements, and implementation notes are adhered to. Do not skip or selectively omit this step.

By adhering to these guidelines, we ensure maintainable, readable, and robust code across the project.
