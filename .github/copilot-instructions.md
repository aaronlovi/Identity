# GitHub Copilot Instructions for Identity Service

## Project Overview
This is the Identity context for a gaming platform - a .NET service that provides authentication, authorization, and user management capabilities. The service uses Firebase Authentication as the core identity provider and adds custom roles, status management, and event publishing on top.

## Technology Stack
- **Runtime**: .NET 8
- **Framework**: ASP.NET Core
- **Distributed Computing**: Microsoft Orleans
- **Database**: PostgreSQL (Cloud SQL for production, Docker for local dev)
- **Authentication**: Firebase Authentication
- **Messaging**: Google Cloud Pub/Sub (emulator for local dev)
- **Transport**: gRPC with mTLS for internal APIs
- **Deployment**: Google Cloud Run
- **Local Development**: Docker Compose

## Project Structure
Assume the following solution structure when creating or referencing code:

```
src/
├── Identity.sln
├── Identity.Host/              # Orleans silo host
├── Identity.Gateway/           # API Gateway (public-facing)
├── Identity.Grains/            # Orleans grains (business logic)
├── Identity.Protos/            # gRPC contracts
├── Identity.Common/            # Shared libraries
└── Identity.Tests/             # Unit tests
functions/                      # Firebase Cloud Functions
infra/
├── docker-compose.yml          # Local development stack
└── migrations/                 # Database migrations
docs/
├── adrs/                       # Architecture Decision Records
└── [existing docs]/
```

## Existing Documentation Reference

### Use the comprehensive project documentation:
- **`docs/Identity Context.md`** - High-level overview and public API specification
- **`docs/Roadmap.md`** - Project roadmap and implementation priorities
- **`docs/Identity Context/`** - Detailed design documents:
  - `Identity Context Design.md` - Complete system architecture, data models, and component interactions
  - `Identity Context Requirements.md` - Functional and non-functional requirements
  - `Identity Context Glossary.md` - Domain terminology and definitions
  - `Identity Context Implementation Plan.md` - Step-by-step development plan with time estimates
- **`docs/adrs/`** - Architecture Decision Records for historical context

### When generating code or making recommendations:
1. **Always reference these documents first** to understand the complete context
2. **Follow the established patterns** described in the design documents
3. **Respect the requirements** outlined in the requirements document
4. **Use consistent terminology** from the glossary
5. **Consider the implementation plan** for prioritization and dependencies
6. **Review existing ADRs** to understand previous architectural decisions

## Key Architectural Principles

### Authentication & Authorization
- **Delegate ALL credential management to Firebase Authentication**
- Use Firebase ID tokens for all API calls
- Custom claims in Firebase tokens carry `roles` and `status`
- Downstream services verify tokens locally via Firebase Admin SDK (no central token validation)
- gRPC Admin API for internal user management (roles/status changes)

### Data Model
- Map `firebase_uid` → internal numeric `user_id`
- Store minimal user data in PostgreSQL (`users`, `user_roles` tables)
- Use Firebase custom claims for authorization data (`roles`, `status`)
- Emit domain events (`UserCreated`, `UserStatusChanged`, `UserRolesUpdated`) to Pub/Sub

### Development Approach
- **Keep it simple** - this is a single-developer project
- **Local-first development** with Docker Compose
- **Event-driven architecture** with Pub/Sub messaging
- **Infrastructure as Code** principles but start simple

## Code Generation Guidelines

### When writing Orleans grains:
- Use `IGrainWithIntegerKey` for user-related grains (keyed by `user_id`)
- Use `IGrainWithStringKey` for rate limiting/fuse grains (keyed by IP/identifier)
- Include proper state persistence patterns
- Add comprehensive logging with structured data

### When writing gRPC services:
- Use protobuf-first approach (define .proto files first)
- Include proper error handling and status codes
- Add request/response validation
- Use mTLS for internal APIs

### When writing database code:
- Use migrations for all schema changes (Evolve or EF migrations)
- Write idempotent migration scripts
- Include proper indexing for performance
- Use connection pooling appropriately

### When writing Firebase integration:
- Use Firebase Admin SDK for server-side operations
- Handle Firebase errors gracefully with retry logic
- Cache Firebase public keys appropriately
- Log all Firebase operations for audit

### When writing tests:
- Use Orleans TestCluster for grain testing
- Mock external dependencies (Firebase, database)
- Include integration tests for critical flows
- Test rate limiting and error scenarios

## Development Patterns

### Error Handling
- Use structured logging with Serilog
- Include correlation IDs in all logs
- Log security events (failed auth, rate limiting) at WARNING level
- Use appropriate HTTP status codes

### Configuration
- Use .NET configuration providers
- Store secrets in environment variables or Secret Manager
- Support both local development and cloud deployment configs
- Validate configuration on startup

### Rate Limiting
- Implement sliding window rate limiting in Orleans grains
- Key by `(user_id OR email) × IP_address`
- Return HTTP 429 with Retry-After header
- Log all rate limit violations

### Event Publishing
- Use CloudEvents format for all domain events
- Publish to Google Cloud Pub/Sub
- Include proper error handling and retries
- Log all event publishing for audit

## Local Development Setup

### When creating development infrastructure:
- Use Docker Compose for PostgreSQL, Pub/Sub emulator, Firebase emulator
- Include seed data for testing
- Provide scripts for common development tasks
- Document environment setup clearly

### When writing deployment code:
- Support both local development and GCP deployment
- Use Cloud Run for production deployment
- Include health check endpoints
- Support graceful shutdown

## Architecture Decision Records (ADRs)

When making significant architectural decisions, always generate an ADR document in `docs/adrs/` following this format:

```markdown
# ADR-XXXX: [Title]

## Status
[Proposed | Accepted | Deprecated | Superseded by ADR-YYYY]

## Context
[What is the issue that we're seeing that is motivating this decision or change?]

## Decision
[What is the change that we're proposing or have agreed to implement?]

## Consequences
[What becomes easier or more difficult to do and any risks introduced by this change?]

## Alternatives Considered
[What other options were evaluated?]

## References
[Links to relevant documentation, RFCs, or discussions]
```

### Generate ADRs for decisions like:
- Technology choices (Orleans vs alternatives, PostgreSQL vs other databases)
- Authentication patterns (Firebase vs custom JWT)
- Rate limiting strategies
- Event schema designs
- Deployment architecture choices
- Testing strategies

## Security Considerations

### Always consider:
- Input validation for all external inputs
- SQL injection prevention
- Rate limiting on all public endpoints
- Proper secret management
- mTLS for internal communications
- Audit logging for security events

### Never:
- Store credentials or session tokens in database
- Log sensitive data (passwords, tokens, PII)
- Accept user input without validation
- Skip authentication checks
- Hard-code secrets in source code

## Performance Guidelines

### Optimize for:
- Local token verification (avoid network calls per request)
- Database connection pooling
- Efficient Orleans grain activation
- Minimal custom claim size (< 1KB)
- Fast startup times for Cloud Run

### Monitor:
- Token verification latency
- Database query performance
- Orleans grain activation times
- Memory usage in Cloud Run
- Event publishing lag

## Documentation Standards

### Always document:
- Public API endpoints with OpenAPI/Swagger
- gRPC services with comprehensive comments
- Database schema with comments
- Configuration options
- Deployment procedures
- Local development setup

### Keep docs updated:
- Update README for any setup changes
- Generate ADRs for architectural decisions
- Document breaking changes
- Include troubleshooting guides

Remember: This is a single-developer project focused on simplicity and rapid development while maintaining production readiness.