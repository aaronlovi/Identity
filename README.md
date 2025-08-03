# Identity Service

A comprehensive identity and authentication service for gaming platforms, built with .NET, Orleans, and Firebase Authentication.

## Overview

The Identity Service provides secure user authentication, authorization, and user management capabilities. It leverages Firebase Authentication for credential management while adding custom roles, status management, and event-driven user lifecycle management.

### Key Features

- 🔐 **Firebase Authentication Integration** - Delegated credential management with email/password and OAuth providers
- 👥 **Role-Based Access Control** - Flexible role system with custom claims in JWT tokens
- 🚦 **User Status Management** - Active, banned, and shadow-banned user states
- 📡 **Event-Driven Architecture** - Pub/Sub events for user lifecycle changes
- 🛡️ **Rate Limiting** - Built-in abuse protection with sliding window counters
- 🔧 **Admin API** - Internal gRPC API for user management operations
- 📊 **Audit Trail** - Comprehensive logging and monitoring

## Architecture

### High-Level Components

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Client Apps   │───▶│   API Gateway    │───▶│ Downstream APIs │
│ (Web, Mobile)   │    │ (Token Verify)   │    │  (Game, Wallet) │
└─────────────────┘    └──────────────────┘    └─────────────────┘
         │                       │
         ▼                       ▼
┌─────────────────┐    ┌──────────────────┐
│ Firebase Auth   │    │ Identity Service │
│   (Hosted)      │    │   (Orleans)      │
└─────────────────┘    └──────────────────┘
                                │
                                ▼
                    ┌──────────────────┐    ┌─────────────────┐
                    │   PostgreSQL     │    │   Pub/Sub       │
                    │   (User Data)    │    │   (Events)      │
                    └──────────────────┘    └─────────────────┘
```

### Technology Stack

- **.NET 8** - Runtime and framework
- **Microsoft Orleans** - Distributed computing platform
- **Firebase Authentication** - Identity provider
- **PostgreSQL** - User data storage
- **Google Cloud Pub/Sub** - Event messaging
- **gRPC** - Internal API transport
- **Docker Compose** - Local development

## Quick Start

### Prerequisites

- .NET 8 SDK
- Docker Desktop
- Google Cloud SDK (gcloud)
- Firebase CLI
- Git

### Local Development Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/aaronlovi/Identity.git
   cd Identity
   ```

2. **Start local infrastructure**

   ```bash
   cd infra
   docker compose up -d
   ```

   This starts:
   - PostgreSQL database
   - Pub/Sub emulator
   - Firebase Auth emulator

3. **Initialize Firebase project**

   ```bash
   firebase login
   firebase init functions
   # Follow prompts to set up Firebase project
   ```

4. **Run database migrations**

   ```bash
   cd src
   dotnet run --project Identity.Host -- migrate
   ```

5. **Start the services**

   ```bash
   # Terminal 1: Orleans Host
   dotnet run --project Identity.Host

   # Terminal 2: API Gateway
   dotnet run --project Identity.Gateway
   ```

6. **Verify setup**

   ```bash
   curl http://localhost:5000/health
   # Should return: {"status": "healthy"}
   ```

### Project Structure

```
Identity/
├── src/
│   ├── Identity.sln              # Visual Studio solution
│   ├── Identity.Host/            # Orleans silo (main service)
│   ├── Identity.Gateway/         # API Gateway (public endpoints)
│   ├── Identity.Grains/          # Business logic (Orleans grains)
│   ├── Identity.Protos/          # gRPC contracts
│   ├── Identity.Common/          # Shared libraries
│   └── Identity.Tests/           # Unit and integration tests
├── functions/                    # Firebase Cloud Functions
├── infra/
│   ├── docker-compose.yml        # Local development stack
│   └── migrations/               # Database migrations
├── docs/
│   ├── adrs/                     # Architecture Decision Records
│   └── [design documents]
└── README.md
```

## Configuration

### Environment Variables

#### Local Development

```bash
# Database
ConnectionStrings__Default="Host=localhost;Database=identity_dev;Username=postgres;Password=dev123"

# Firebase
GOOGLE_APPLICATION_CREDENTIALS="path/to/service-account-key.json"
Firebase__ProjectId="your-firebase-project-dev"

# Pub/Sub (Local Emulator)
PUBSUB_EMULATOR_HOST="localhost:8085"

# Orleans
Orleans__ClusteringType="Localhost"
```

#### Production (Cloud Run)

```bash
# Database (Cloud SQL)
ConnectionStrings__Default="Host=/cloudsql/project:region:instance;Database=identity_prod;Username=app;Password=${DB_PASSWORD}"

# Firebase
Firebase__ProjectId="your-firebase-project-prod"

# Pub/Sub
PubSub__ProjectId="your-gcp-project"
PubSub__TopicName="identity.events"

# Orleans
Orleans__ClusteringType="Redis"
Orleans__Redis__ConnectionString="${REDIS_CONNECTION_STRING}"
```

## API Reference

### Public Endpoints (API Gateway)

- `GET /health` - Health check
- `POST /auth/verify` - Verify Firebase ID token (development only)

### Internal gRPC API (Admin Service)

- `GetUser(userId)` - Retrieve user information
- `SetUserStatus(userId, status)` - Update user status (active/banned/shadow_banned)
- `UpdateUserRoles(userId, roles)` - Modify user roles
- `MintCustomToken(userId)` - Generate custom Firebase token (optional)

### Authentication Flow

1. **Client Registration/Login**

   ```javascript
   // Client uses Firebase SDK
   const userCredential = await signInWithEmailAndPassword(auth, email, password);
   const idToken = await userCredential.user.getIdToken();
   ```

2. **API Calls with Token**

   ```bash
   curl -H "Authorization: Bearer ${ID_TOKEN}" \
        https://api.yourgame.com/wallet/balance
   ```

3. **Token Verification (in downstream service)**

   ```csharp
   var decodedToken = await FirebaseAuth.DefaultInstance
       .VerifyIdTokenAsync(idToken, checkRevoked: true);
   
   var userId = decodedToken.Claims["user_id"];
   var roles = decodedToken.Claims["roles"];
   var status = decodedToken.Claims["status"];
   ```

## Events

The service publishes domain events to `identity.events` topic:

### UserCreated

```json
{
  "specversion": "1.0",
  "type": "identity.user.created",
  "source": "identity-service",
  "id": "uuid",
  "time": "2025-08-02T10:30:00Z",
  "data": {
    "user_id": 12345,
    "firebase_uid": "abc123...",
    "roles": ["player"],
    "status": "active",
    "email_verified": true
  }
}
```

### UserStatusChanged

```json
{
  "specversion": "1.0",
  "type": "identity.user.status_changed",
  "source": "identity-service",
  "id": "uuid",
  "time": "2025-08-02T10:30:00Z",
  "data": {
    "user_id": 12345,
    "previous_status": "active",
    "new_status": "banned",
    "changed_by": "admin@example.com",
    "reason": "violation_of_terms"
  }
}
```

## Testing

### Run Unit Tests

```bash
cd src
dotnet test
```

### Run Integration Tests

```bash
# Ensure local infrastructure is running
docker compose up -d

# Run integration tests
dotnet test --filter Category=Integration
```

### End-to-End Testing

```bash
# Run the E2E test script
./scripts/e2e.sh
```

## Deployment

### Google Cloud Run

1. **Build and push container**

   ```bash
   gcloud builds submit --tag gcr.io/PROJECT_ID/identity-service
   ```

2. **Deploy to Cloud Run**

   ```bash
   gcloud run deploy identity-service \
     --image gcr.io/PROJECT_ID/identity-service \
     --platform managed \
     --region us-central1 \
     --set-env-vars="Firebase__ProjectId=your-project" \
     --set-secrets="ConnectionStrings__Default=db-connection:latest"
   ```

3. **Deploy Cloud Functions**

   ```bash
   cd functions
   firebase deploy --only functions
   ```

## Monitoring & Observability

### Health Checks

- Service: `GET /health`
- Dependencies: Database, Pub/Sub, Firebase connectivity

### Metrics

- Authentication success/failure rates
- Token verification latency
- Rate limiting events
- Event publishing metrics

### Logging

- Structured logging with correlation IDs
- Security events (failed auth, rate limits)
- Performance metrics
- Error tracking

### Alerts

- Service health degradation
- High rate limiting activity (>100 events/5min)
- Firebase quota approaching limits
- Database connection issues

## Security

### Best Practices Implemented

- No credential storage (delegated to Firebase)
- Local token verification (no network round-trips)
- Rate limiting with sliding windows
- mTLS for internal communications
- Structured audit logging
- Input validation on all endpoints

### Security Considerations

- Custom claims limited to <1KB
- Sensitive data excluded from logs
- Regular security dependency updates
- Principle of least privilege for service accounts

## Troubleshooting

### Common Issues

**"Firebase Admin SDK not initialized"**

- Ensure `GOOGLE_APPLICATION_CREDENTIALS` points to valid service account key
- Verify Firebase project ID configuration

**"Database connection failed"**

- Check PostgreSQL is running: `docker compose ps`
- Verify connection string format
- Ensure database exists and migrations applied

**"Pub/Sub emulator not found"**

- Start emulator: `docker compose up pubsub-emulator`
- Verify `PUBSUB_EMULATOR_HOST` environment variable

**"Orleans clustering failed"**

- For local development, ensure no other Orleans instances running
- For production, verify Redis connection string

### Getting Help

1. Check the [troubleshooting guide](docs/troubleshooting.md)
2. Review [Architecture Decision Records](docs/adrs/)
3. Search existing [GitHub Issues](https://github.com/aaronlovi/Identity/issues)
4. Create a new issue with:
   - Environment details
   - Error messages
   - Steps to reproduce

## Contributing

### Development Workflow

1. Create feature branch from `master`
2. Make changes with appropriate tests
3. Ensure all tests pass: `dotnet test`
4. Update documentation if needed
5. Create pull request

### Architecture Decisions

For significant changes, create an Architecture Decision Record (ADR):

```bash
# Create new ADR
cp docs/adrs/template.md docs/adrs/ADR-XXXX-your-decision.md
# Edit the ADR with your decision details
```

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## Roadmap

- [ ] Multi-factor authentication support
- [ ] Enhanced admin dashboard
- [ ] GDPR compliance features
- [ ] Performance optimizations
- [ ] Advanced monitoring

For detailed roadmap and implementation plan, see [docs/Roadmap.md](docs/Roadmap.md).
