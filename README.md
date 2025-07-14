# Identity Service

## Overview

The Identity Service is a microservice within the social gaming platform, designed to handle user authentication, authorization, session management, and identity events. It is built with a domain-driven design approach and is ready for play-money gaming with future real-money readiness.

## Architecture

- **Platform**: .NET 8+ with C#
- **Distributed State Management**: Orleans (Virtual Actors)
- **Database**: PostgreSQL
- **Event Streaming**: Kafka (`identity.events` topic)
- **Communication**: gRPC (internal) and REST API (external)
- **Tokens**: JWT (short-lived access tokens and refresh tokens)

## Project Structure

```plaintext
src/
├── Identity.Api/          # REST API layer, gateway interface
├── Identity.Domain/       # Core business logic, entities, value objects
├── Identity.GrainInterfaces/ # Orleans grain contracts
├── Identity.Grains/       # Orleans grain implementations
├── Identity.Grpc/         # gRPC service definitions and implementations
└── Identity.Infrastructure/ # Kafka, email, outbox, persistence
```

## Key Features

- **User Management**: Core identity with stable `user_id`, multiple roles (player, admin).
- **Authentication**: Email/password, OAuth providers.
- **Session Management**: Server-side auth state tracking, global logout.
- **Device Binding**: Enables device-specific rate limiting and fraud detection.
- **User Status**: Active, Banned, Shadow-banned.
- **Rate Limiting**: In-memory counters keyed by user/device/IP.

## Development Setup

```powershell
# Create solution and projects
mkdir src
cd src

dotnet new sln -n Identity

dotnet new webapi -n Identity.Api

dotnet new classlib -n Identity.Domain

dotnet new classlib -n Identity.GrainInterfaces

dotnet new classlib -n Identity.Grains

dotnet new grpc -n Identity.Grpc

dotnet new classlib -n Identity.Infrastructure

dotnet sln add **/*.csproj

# Add Orleans packages
cd Identity.GrainInterfaces

dotnet add package Microsoft.Orleans.Core
cd ../Identity.Grains

dotnet add package Microsoft.Orleans.Server
```

## Event-Driven Integration

Identity changes flow through Kafka `identity.events` topic:

- `UserCreated` → Triggers wallet account creation.
- `LoginSucceeded/Failed` → Audit trail, security monitoring.
- `SessionRevoked` → Downstream session cleanup.

## Security

- **Password Hashing**: Industry-standard algorithms (bcrypt/Argon2).
- **JWT Security**: Includes `jwt_id` for revocation, validated against session state.
- **Rate Limiting**: Implemented before authentication logic.
- **Email Security**: Outbox pattern, SPF/DKIM/DMARC configured.

## Future Considerations

- **KYC Integration**: Real-money compliance.
- **Multi-factor Auth**: SMS/TOTP support.
- **Advanced Fraud Detection**: ML-based patterns.
- **Horizontal Scaling**: Designed for 10k+ concurrent users.

## Development Notes

- Start with the data layer: PostgreSQL schemas, migrations, then domain logic.
- Use Orleans grains for stateful operations (sessions, rate limits).
- Consider event sourcing for audit requirements and real-money compliance.
- Docker support planned for deployment topology.
