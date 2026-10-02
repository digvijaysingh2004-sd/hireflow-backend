# HireFlow — Portfolio-Ready Microservice Project Blueprint

> A small but production-minded recruitment workflow platform built with **.NET 10 LTS, ASP.NET Core, PostgreSQL, React, TypeScript, and Docker**.

This project is intentionally compact enough to build and explain in interviews, but rich enough to demonstrate the engineering practices expected in enterprise teams.

---

## 1. Project Objective

**HireFlow** helps a company manage:

- User registration and email OTP verification

- Secure login with JWT access tokens and rotating refresh tokens

- Role-based access control for Admin, Recruiter, Hiring Manager, and Candidate

- Job creation and publishing

- Candidate applications

- Application status workflow

- Interview scheduling

- Email notifications

- Audit history

- Search, filtering, pagination, sorting, and dashboards

### Suggested resume description

> Built HireFlow, a secure recruitment workflow platform using .NET 10 microservices, PostgreSQL, React, JWT authentication, email OTP verification, refresh-token rotation, RBAC, audit logging, OpenAPI, Docker, automated tests, and CI/CD-ready project structure.

### Why this project stands out

This is not just CRUD. It demonstrates:

- Security fundamentals

- Business workflow modeling

- Clean service boundaries

- Database design and migrations

- API design

- Consistent error handling

- Automated testing

- Observability and health checks

- Containerized local development

- A frontend that consumes the backend professionally

---

## 2. Recommended Technology Decisions

| Area | Choice | Reason |
| --- | --- | --- |
| Backend runtime | **.NET 10 LTS** | Current long-term-support release and strong enterprise adoption |
| API framework | ASP.NET Core Web API | Fast, mature, well-supported REST API framework |
| Language | C# 14 | Modern type-safe language with excellent tooling |
| ORM | Entity Framework Core 10 | Productive PostgreSQL data access and migrations |
| Database | PostgreSQL 17+ | Reliable relational database with strong indexing and JSON support |
| PostgreSQL provider | Npgsql / Npgsql.EntityFrameworkCore.PostgreSQL | Officially popular .NET PostgreSQL integration |
| Authentication | JWT bearer access tokens + refresh-token rotation | Stateless API access with revocation support |
| Password hashing | ASP.NET Core Identity password hasher | Avoids inventing cryptography |
| Email | SMTP in development; provider adapter in production | Keeps email replaceable and testable |
| Validation | FluentValidation | Clear request validation rules |
| API documentation | OpenAPI / Swagger UI | Recruiters and interviewers can try the API quickly |
| Logging | Serilog | Structured logs suitable for production diagnostics |
| Frontend | React + TypeScript | Industry-standard component-based UI |
| Frontend build | Vite | Fast development experience and official React guidance |
| Client data | TanStack Query | Caching, retries, loading states, and server-state management |
| Forms | React Hook Form + Zod | Performant forms and shared validation |
| UI | Tailwind CSS or Material UI | Consistent, responsive enterprise UI |
| Tests | xUnit, FluentAssertions, WebApplicationFactory, Vitest, Playwright | Unit, integration, frontend, and end-to-end coverage |
| Containers | Docker Compose | Repeatable local environment |
| Source control | Git + GitHub | Portfolio visibility and CI/CD integration |

### Version note

Use the latest patch version available for .NET 10 and PostgreSQL when creating the project. Pin versions in `global.json`, Docker images, and package files so the build remains reproducible.

Official references:

- [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)

- [ASP.NET Core JWT bearer configuration](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0)

- [React: build an app from scratch](https://react.dev/learn/build-a-react-app-from-scratch)

- [Vite guide](https://vite.dev/guide/)

---

## 3. High-Level Architecture

```
                         +-------------------+
                         | React Web Client   |
                         | TypeScript + Vite  |
                         +---------+---------+
                                   |
                              HTTPS / JSON
                                   |
                         +---------v---------+
                         | API Gateway/BFF    |
                         | Optional phase 2  |
                         +---------+---------+
                                   |
              +--------------------+--------------------+
              |                    |                    |
      +-------v-------+    +-------v-------+    +-------v-------+
      | IdentitySvc   |    | HiringSvc     |    | Notification  |
      | Auth + users  |    | Jobs + apps   |    | Email + OTP   |
      +-------+-------+    +-------+-------+    +-------+-------+
              |                    |                    |
      +-------v-------+    +-------v-------+    +-------v-------+
      | identity_db   |    | hiring_db    |    | notification_db|
      +---------------+    +--------------+    +----------------+
                                   |
                         +---------v---------+
                         | Message broker    |
                         | Optional phase 2  |
                         +-------------------+
```

### Recommended implementation strategy

Build it in two stages:

1. **Portfolio MVP:** three independently deployable ASP.NET Core services in the backend repository, with separate PostgreSQL databases and synchronous HTTP communication.

1. **Enterprise upgrade:** add a message broker such as RabbitMQ for events like `UserRegistered`, `ApplicationSubmitted`, `InterviewScheduled`, and `OtpRequested`.

This avoids overengineering the first version while still giving you a strong microservice story.

### Service boundaries

#### Identity Service

Owns:

- Users

- Roles

- User-role assignments

- Refresh tokens

- OTP challenges

- Password reset flow

- Login and token issuance

#### Hiring Service

Owns:

- Companies

- Jobs

- Applications

- Application status history

- Interviews

- Recruiter and hiring-manager actions

#### Notification Service

Owns:

- Email templates

- Email delivery attempts

- Notification preferences

- Outbox messages, if asynchronous messaging is enabled

> Each service owns its data. Do not create foreign keys across service databases. Store external IDs such as `CandidateUserId` rather than cross-database relational constraints.

---

## 4. Repository Structure

Create two GitHub repositories with a clear ownership boundary:

1. `hireflow-backend` — ASP.NET Core services, database migrations, Docker files, API documentation, backend tests, and backend CI/CD.

1. `hireflow-frontend` — React/Vite application, UI components, frontend tests, Playwright tests, and frontend CI/CD.

Keep the repositories separate for independent deployment, cleaner pull requests, and a professional portfolio presentation. Keep a short architecture link and the deployed URLs in both repositories' READMEs.

### Backend repository structure: `hireflow-backend`

```
hireflow-backend/
├── README.md
├── LICENSE
├── .editorconfig
├── .gitignore
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── docker-compose.yml
├── docker-compose.override.yml
├── .env.example
├── docs/
│   ├── architecture.md
│   ├── api-contract.md
│   ├── database-design.md
│   ├── threat-model.md
│   └── adr/
│       ├── 001-service-boundaries.md
│       ├── 002-jwt-and-refresh-tokens.md
│       └── 003-sync-http-before-events.md
├── src/
│   ├── BuildingBlocks/
│   │   ├── BuildingBlocks.Contracts/
│   │   ├── BuildingBlocks.Infrastructure/
│   │   └── BuildingBlocks.Observability/
│   ├── Services/
│   │   ├── Identity/
│   │   │   ├── HireFlow.Identity.Api/
│   │   │   ├── HireFlow.Identity.Application/
│   │   │   ├── HireFlow.Identity.Domain/
│   │   │   └── HireFlow.Identity.Infrastructure/
│   │   ├── Hiring/
│   │   │   ├── HireFlow.Hiring.Api/
│   │   │   ├── HireFlow.Hiring.Application/
│   │   │   ├── HireFlow.Hiring.Domain/
│   │   │   └── HireFlow.Hiring.Infrastructure/
│   │   └── Notification/
│   │       ├── HireFlow.Notification.Api/
│   │       ├── HireFlow.Notification.Application/
│   │       ├── HireFlow.Notification.Domain/
│   │       └── HireFlow.Notification.Infrastructure/
│   └── Contracts/
│       └── HireFlow.Contracts/
├── tests/
│   ├── Identity.UnitTests/
│   ├── Identity.IntegrationTests/
│   ├── Hiring.UnitTests/
│   ├── Hiring.IntegrationTests/
│   └── Architecture.Tests/
└── .github/
    └── workflows/
        ├── backend-ci.yml
        └── backend-ci.yml
```

### Frontend repository structure: `hireflow-frontend`

```
hireflow-frontend/
├── README.md
├── package.json
├── vite.config.ts
├── playwright.config.ts
├── .env.example
├── public/
├── src/
└── .github/workflows/frontend-ci.yml
```

### Internal structure of each service

```
HireFlow.Hiring.Api/
├── Endpoints/
│   ├── JobsEndpoints.cs
│   ├── ApplicationsEndpoints.cs
│   └── InterviewsEndpoints.cs
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   └── CorrelationIdMiddleware.cs
├── Extensions/
│   ├── ServiceCollectionExtensions.cs
│   └── ApplicationBuilderExtensions.cs
├── Program.cs
└── appsettings.json

HireFlow.Hiring.Application/
├── Abstractions/
├── Features/
│   ├── Jobs/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   ├── Validators/
│   │   └── Dtos/
│   ├── Applications/
│   └── Interviews/
└── Behaviors/

HireFlow.Hiring.Domain/
├── Entities/
├── Enums/
├── ValueObjects/
├── Events/
└── Exceptions/

HireFlow.Hiring.Infrastructure/
├── Persistence/
│   ├── HiringDbContext.cs
│   ├── Configurations/
│   └── Migrations/
├── Repositories/
├── ExternalServices/
└── DependencyInjection.cs
```

### Why this structure is interview-friendly

- `Api` contains transport concerns only.

- `Application` contains use cases and orchestration.

- `Domain` contains business rules and should not depend on infrastructure.

- `Infrastructure` contains PostgreSQL, email, HTTP clients, and implementation details.

- Feature folders make it easy to find a complete business capability.

---

## 5. Phase 1 — Install and Configure PostgreSQL

### Option A: Docker PostgreSQL — recommended

Install Docker Desktop or Docker Engine. Verify:

```bash
docker --version
docker compose version
```

Create `docker-compose.yml` in the repository root:

```yaml
services:
  postgres:
    image: postgres:17-alpine
    container_name: hireflow-postgres
    restart: unless-stopped
    environment:
      POSTGRES_USER: hireflow_admin
      POSTGRES_PASSWORD: change_me_locally
      POSTGRES_DB: hireflow_identity
    ports:
      - "5432:5432"
    volumes:
      - hireflow-postgres-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U hireflow_admin -d hireflow_identity"]
      interval: 5s
      timeout: 5s
      retries: 10

  mailpit:
    image: axllent/mailpit:latest
    container_name: hireflow-mailpit
    restart: unless-stopped
    ports:
      - "1025:1025"
      - "8025:8025"

volumes:
  hireflow-postgres-data:
```

Start it:

```bash
docker compose up -d postgres mailpit
docker compose ps
```

Mailpit gives you a local inbox at `http://localhost:8025` without sending real email.

### Create service databases

Use separate databases even when PostgreSQL is running in one container:

```bash
docker exec -it hireflow-postgres psql -U hireflow_admin -d hireflow_identity
```

Run:

```sql
CREATE DATABASE hireflow_hiring;
CREATE DATABASE hireflow_notification;
\l
\q
```

For a more production-like setup, create separate database users:

```sql
CREATE USER identity_app WITH PASSWORD 'identity_local_password';
CREATE USER hiring_app WITH PASSWORD 'hiring_local_password';
CREATE USER notification_app WITH PASSWORD 'notification_local_password';

GRANT ALL PRIVILEGES ON DATABASE hireflow_identity TO identity_app;
GRANT ALL PRIVILEGES ON DATABASE hireflow_hiring TO hiring_app;
GRANT ALL PRIVILEGES ON DATABASE hireflow_notification TO notification_app;
```

### PostgreSQL design rules

- Use `uuid` for service-owned entity IDs.

- Use `timestamptz` for timestamps.

- Store timestamps in UTC.

- Add unique constraints at the database level.

- Add indexes based on real query patterns.

- Use check constraints for simple invariants.

- Never store plaintext passwords, OTP codes, refresh tokens, or secrets.

- Never use `Postgres:Password` directly in source code.

- Keep migration scripts in the service that owns the database.

---

## 6. Database Design

### 6.1 Identity database

#### `users`

| Column | Type | Rules |
| --- | --- | --- |
| `id` | uuid | Primary key |
| `email` | citext | Unique, required |
| `password_hash` | text | Required |
| `first_name` | varchar(80 ) | Required |
| `last_name` | varchar(80) | Required |
| `is_email_verified` | boolean | Default false |
| `is_active` | boolean | Default true |
| `failed_login_attempts` | integer | Default 0 |
| `locked_until_utc` | timestamptz | Nullable |
| `created_at_utc` | timestamptz | Required |
| `updated_at_utc` | timestamptz | Required |
| `row_version` | bigint | Optimistic concurrency |

#### `roles`

- `id` uuid primary key

- `name` varchar(50) unique

- `description` varchar(255)

Seed roles:

- `Admin`

- `Recruiter`

- `HiringManager`

- `Candidate`

#### `user_roles`

Composite primary key: `user_id`, `role_id`.

#### `refresh_tokens`

| Column | Type | Rules |
| --- | --- | --- |
| `id` | uuid | Primary key |
| `user_id` | uuid | Indexed |
| `token_hash` | text | Unique; never store raw token |
| `expires_at_utc` | timestamptz | Required |
| `created_at_utc` | timestamptz | Required |
| `revoked_at_utc` | timestamptz | Nullable |
| `replaced_by_token_id` | uuid | Nullable |
| `created_by_ip` | inet | Nullable |
| `revoked_reason` | varchar(200) | Nullable |

#### `otp_challenges`

| Column | Type | Rules |
| --- | --- | --- |
| `id` | uuid | Primary key |
| `user_id` | uuid | Indexed |
| `purpose` | varchar(30) | Registration, PasswordReset, LoginStepUp |
| `code_hash` | text | Required |
| `expires_at_utc` | timestamptz | Required |
| `verified_at_utc` | timestamptz | Nullable |
| `attempt_count` | integer | Default 0 |
| `created_at_utc` | timestamptz | Required |

Recommended indexes:

```sql
CREATE UNIQUE INDEX ux_users_email ON users (lower(email));
CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens (user_id);
CREATE INDEX ix_otp_challenges_user_purpose ON otp_challenges (user_id, purpose);
```

### 6.2 Hiring database

#### `companies`

- `id` uuid primary key

- `name` varchar(150) not null

- `slug` varchar(160) unique not null

- `created_by_user_id` uuid not null

- `created_at_utc` timestamptz not null

#### `jobs`

- `id` uuid primary key

- `company_id` uuid not null

- `title` varchar(160) not null

- `slug` varchar(180) not null

- `description` text not null

- `location` varchar(160)

- `employment_type` varchar(30)

- `experience_min_years` smallint

- `experience_max_years` smallint

- `salary_min` numeric(12,2)

- `salary_max` numeric(12,2)

- `status` varchar(30) not null

- `created_by_user_id` uuid not null

- `published_at_utc` timestamptz

- `closing_at_utc` timestamptz

- `created_at_utc` timestamptz not null

- `updated_at_utc` timestamptz not null

- `row_version` bigint not null

#### `applications`

- `id` uuid primary key

- `job_id` uuid not null

- `candidate_user_id` uuid not null

- `resume_url` text

- `cover_note` text

- `status` varchar(40) not null

- `applied_at_utc` timestamptz not null

- `updated_at_utc` timestamptz not null

- unique `(job_id, candidate_user_id)`

Suggested statuses:

```
Submitted -> UnderReview -> Shortlisted -> InterviewScheduled
InterviewScheduled -> Offered -> Hired
Submitted/UnderReview/Shortlisted -> Rejected
Any active status -> Withdrawn
```

#### `application_status_history`

- `id` uuid primary key

- `application_id` uuid not null

- `from_status` varchar(40)

- `to_status` varchar(40) not null

- `changed_by_user_id` uuid not null

- `comment` varchar(500)

- `changed_at_utc` timestamptz not null

#### `interviews`

- `id` uuid primary key

- `application_id` uuid not null

- `scheduled_by_user_id` uuid not null

- `starts_at_utc` timestamptz not null

- `ends_at_utc` timestamptz not null

- `meeting_url` text

- `status` varchar(30) not null

- `notes` text

- `created_at_utc` timestamptz not null

### 6.3 Notification database

#### `email_templates`

- `id` uuid primary key

- `template_key` varchar(80) unique

- `subject_template` varchar(255)

- `body_template` text

- `is_active` boolean

#### `email_messages`

- `id` uuid primary key

- `recipient_email` citext

- `template_key` varchar(80)

- `payload_json` jsonb

- `status` varchar(30)

- `attempt_count` integer

- `last_error` text

- `sent_at_utc` timestamptz

- `created_at_utc` timestamptz

### 6.4 Audit database or audit table

For the MVP, an audit table in each owning service is enough:

- `id`

- `actor_user_id`

- `action`

- `entity_type`

- `entity_id`

- `old_values_json`

- `new_values_json`

- `correlation_id`

- `ip_address`

- `created_at_utc`

Do not log passwords, OTP values, raw JWTs, refresh tokens, or sensitive resume content.

---

## 7. Phase 2 — Install the Backend Toolchain

### Install .NET 10 SDK

Download the SDK from the official .NET site or use the package manager for your operating system.

Verify:

```bash
dotnet --version
dotnet --info
```

Pin the major SDK version:

```bash
dotnet new globaljson --sdk-version 10.0.100 --roll-forward latestFeature
```

Use the exact installed SDK version in your own `global.json`.

### Install VS Code extensions

Recommended extensions:

- C# Dev Kit

- .NET Install Tool

- C#

- PostgreSQL Explorer or SQLTools

- Docker

- REST Client or Thunder Client

- GitLens

- EditorConfig

- ESLint

- Prettier

- Error Lens

### Create the solution — exact copy-paste sequence

You **should not use the wildcard commands** shown earlier. In Bash, `**` is not always expanded unless a shell option is enabled, and PowerShell uses different wildcard behavior. The explicit commands below are more reliable and show exactly which projects belong to the solution.

Run these commands from the folder where you want the backend repository:

```bash
mkdir hireflow-backend
cd hireflow-backend

# Create the traditional .sln solution file.
dotnet new sln --name HireFlow --format sln

# Create all required folders.
mkdir -p src/Services/Identity
mkdir -p src/Services/Hiring
mkdir -p src/Services/Notification
mkdir -p src/BuildingBlocks
mkdir -p src/Contracts
mkdir -p tests
```

If your installed SDK does not recognize `--format sln`, use this instead:

```bash
dotnet new sln --name HireFlow
```

Then run `ls` and use the solution filename that was created. The commands below assume it is `HireFlow.sln`.

### Create all Identity Service projects

```bash
dotnet new webapi --name HireFlow.Identity.Api --output src/Services/Identity/HireFlow.Identity.Api --use-controllers false
dotnet new classlib --name HireFlow.Identity.Application --output src/Services/Identity/HireFlow.Identity.Application
dotnet new classlib --name HireFlow.Identity.Domain --output src/Services/Identity/HireFlow.Identity.Domain
dotnet new classlib --name HireFlow.Identity.Infrastructure --output src/Services/Identity/HireFlow.Identity.Infrastructure
```

### Create all Hiring Service projects

```bash
dotnet new webapi --name HireFlow.Hiring.Api --output src/Services/Hiring/HireFlow.Hiring.Api --use-controllers false
dotnet new classlib --name HireFlow.Hiring.Application --output src/Services/Hiring/HireFlow.Hiring.Application
dotnet new classlib --name HireFlow.Hiring.Domain --output src/Services/Hiring/HireFlow.Hiring.Domain
dotnet new classlib --name HireFlow.Hiring.Infrastructure --output src/Services/Hiring/HireFlow.Hiring.Infrastructure
```

### Create all Notification Service projects

```bash
dotnet new webapi --name HireFlow.Notification.Api --output src/Services/Notification/HireFlow.Notification.Api --use-controllers false
dotnet new classlib --name HireFlow.Notification.Application --output src/Services/Notification/HireFlow.Notification.Application
dotnet new classlib --name HireFlow.Notification.Domain --output src/Services/Notification/HireFlow.Notification.Domain
dotnet new classlib --name HireFlow.Notification.Infrastructure --output src/Services/Notification/HireFlow.Notification.Infrastructure
```

### Create shared BuildingBlocks and Contracts projects

```bash
dotnet new classlib --name BuildingBlocks.Contracts --output src/BuildingBlocks/BuildingBlocks.Contracts
dotnet new classlib --name BuildingBlocks.Infrastructure --output src/BuildingBlocks/BuildingBlocks.Infrastructure
dotnet new classlib --name BuildingBlocks.Observability --output src/BuildingBlocks/BuildingBlocks.Observability
dotnet new classlib --name HireFlow.Contracts --output src/Contracts/HireFlow.Contracts
```

### Add every project to the solution explicitly

Run this complete list. Do not skip a project:

```bash
# Identity Service
dotnet sln HireFlow.sln add src/Services/Identity/HireFlow.Identity.Api/HireFlow.Identity.Api.csproj
dotnet sln HireFlow.sln add src/Services/Identity/HireFlow.Identity.Application/HireFlow.Identity.Application.csproj
dotnet sln HireFlow.sln add src/Services/Identity/HireFlow.Identity.Domain/HireFlow.Identity.Domain.csproj
dotnet sln HireFlow.sln add src/Services/Identity/HireFlow.Identity.Infrastructure/HireFlow.Identity.Infrastructure.csproj

# Hiring Service
dotnet sln HireFlow.sln add src/Services/Hiring/HireFlow.Hiring.Api/HireFlow.Hiring.Api.csproj
dotnet sln HireFlow.sln add src/Services/Hiring/HireFlow.Hiring.Application/HireFlow.Hiring.Application.csproj
dotnet sln HireFlow.sln add src/Services/Hiring/HireFlow.Hiring.Domain/HireFlow.Hiring.Domain.csproj
dotnet sln HireFlow.sln add src/Services/Hiring/HireFlow.Hiring.Infrastructure/HireFlow.Hiring.Infrastructure.csproj

# Notification Service
dotnet sln HireFlow.sln add src/Services/Notification/HireFlow.Notification.Api/HireFlow.Notification.Api.csproj
dotnet sln HireFlow.sln add src/Services/Notification/HireFlow.Notification.Application/HireFlow.Notification.Application.csproj
dotnet sln HireFlow.sln add src/Services/Notification/HireFlow.Notification.Domain/HireFlow.Notification.Domain.csproj
dotnet sln HireFlow.sln add src/Services/Notification/HireFlow.Notification.Infrastructure/HireFlow.Notification.Infrastructure.csproj

# Shared libraries
dotnet sln HireFlow.sln add src/BuildingBlocks/BuildingBlocks.Contracts/BuildingBlocks.Contracts.csproj
dotnet sln HireFlow.sln add src/BuildingBlocks/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj
dotnet sln HireFlow.sln add src/BuildingBlocks/BuildingBlocks.Observability/BuildingBlocks.Observability.csproj
dotnet sln HireFlow.sln add src/Contracts/HireFlow.Contracts/HireFlow.Contracts.csproj
```

Verify that all 16 projects were added:

```bash
dotnet sln HireFlow.sln list
```

You should see 16 project paths: four Identity projects, four Hiring projects, four Notification projects, three BuildingBlocks projects, and one Contracts project.

### Add project references — exact list

Run the following commands once. These references keep the dependency direction clean:

```text
Api -> Application and Infrastructure
Application -> Domain and shared Contracts
Infrastructure -> Application, Domain, and shared Infrastructure
```

#### Identity Service references

```bash
dotnet add src/Services/Identity/HireFlow.Identity.Api/HireFlow.Identity.Api.csproj reference src/Services/Identity/HireFlow.Identity.Application/HireFlow.Identity.Application.csproj src/Services/Identity/HireFlow.Identity.Infrastructure/HireFlow.Identity.Infrastructure.csproj src/BuildingBlocks/BuildingBlocks.Observability/BuildingBlocks.Observability.csproj
dotnet add src/Services/Identity/HireFlow.Identity.Application/HireFlow.Identity.Application.csproj reference src/Services/Identity/HireFlow.Identity.Domain/HireFlow.Identity.Domain.csproj src/BuildingBlocks/BuildingBlocks.Contracts/BuildingBlocks.Contracts.csproj src/Contracts/HireFlow.Contracts/HireFlow.Contracts.csproj
dotnet add src/Services/Identity/HireFlow.Identity.Infrastructure/HireFlow.Identity.Infrastructure.csproj reference src/Services/Identity/HireFlow.Identity.Application/HireFlow.Identity.Application.csproj src/Services/Identity/HireFlow.Identity.Domain/HireFlow.Identity.Domain.csproj src/BuildingBlocks/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj
```

#### Hiring Service references

```bash
dotnet add src/Services/Hiring/HireFlow.Hiring.Api/HireFlow.Hiring.Api.csproj reference src/Services/Hiring/HireFlow.Hiring.Application/HireFlow.Hiring.Application.csproj src/Services/Hiring/HireFlow.Hiring.Infrastructure/HireFlow.Hiring.Infrastructure.csproj src/BuildingBlocks/BuildingBlocks.Observability/BuildingBlocks.Observability.csproj
dotnet add src/Services/Hiring/HireFlow.Hiring.Application/HireFlow.Hiring.Application.csproj reference src/Services/Hiring/HireFlow.Hiring.Domain/HireFlow.Hiring.Domain.csproj src/BuildingBlocks/BuildingBlocks.Contracts/BuildingBlocks.Contracts.csproj src/Contracts/HireFlow.Contracts/HireFlow.Contracts.csproj
dotnet add src/Services/Hiring/HireFlow.Hiring.Infrastructure/HireFlow.Hiring.Infrastructure.csproj reference src/Services/Hiring/HireFlow.Hiring.Application/HireFlow.Hiring.Application.csproj src/Services/Hiring/HireFlow.Hiring.Domain/HireFlow.Hiring.Domain.csproj src/BuildingBlocks/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj
```

#### Notification Service references

```bash
dotnet add src/Services/Notification/HireFlow.Notification.Api/HireFlow.Notification.Api.csproj reference src/Services/Notification/HireFlow.Notification.Application/HireFlow.Notification.Application.csproj src/Services/Notification/HireFlow.Notification.Infrastructure/HireFlow.Notification.Infrastructure.csproj src/BuildingBlocks/BuildingBlocks.Observability/BuildingBlocks.Observability.csproj
dotnet add src/Services/Notification/HireFlow.Notification.Application/HireFlow.Notification.Application.csproj reference src/Services/Notification/HireFlow.Notification.Domain/HireFlow.Notification.Domain.csproj src/BuildingBlocks/BuildingBlocks.Contracts/BuildingBlocks.Contracts.csproj src/Contracts/HireFlow.Contracts/HireFlow.Contracts.csproj
dotnet add src/Services/Notification/HireFlow.Notification.Infrastructure/HireFlow.Notification.Infrastructure.csproj reference src/Services/Notification/HireFlow.Notification.Application/HireFlow.Notification.Application.csproj src/Services/Notification/HireFlow.Notification.Domain/HireFlow.Notification.Domain.csproj src/BuildingBlocks/BuildingBlocks.Infrastructure/BuildingBlocks.Infrastructure.csproj
```

Do not reference one service's Domain or Infrastructure project from another service. Services communicate through HTTP APIs or events, not direct project references.

### Restore and build verification

Run these commands before adding NuGet packages or writing business code:

```bash
dotnet restore HireFlow.sln
dotnet build HireFlow.sln --no-restore
```

If the build succeeds, the solution structure and project references are correct. If it fails, fix the project structure before continuing.

### Install backend packages — exact list

Run these commands after the solution builds. Package versions should be pinned after the first successful restore.

#### Identity packages

```bash
dotnet add src/Services/Identity/HireFlow.Identity.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/Services/Identity/HireFlow.Identity.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/Services/Identity/HireFlow.Identity.Api package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add src/Services/Identity/HireFlow.Identity.Application package FluentValidation
dotnet add src/Services/Identity/HireFlow.Identity.Api package Serilog.AspNetCore
dotnet add src/Services/Identity/HireFlow.Identity.Api package AspNetCore.HealthChecks.NpgSql
```

#### Hiring packages

```bash
dotnet add src/Services/Hiring/HireFlow.Hiring.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/Services/Hiring/HireFlow.Hiring.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/Services/Hiring/HireFlow.Hiring.Application package FluentValidation
dotnet add src/Services/Hiring/HireFlow.Hiring.Api package Serilog.AspNetCore
dotnet add src/Services/Hiring/HireFlow.Hiring.Api package AspNetCore.HealthChecks.NpgSql
```

#### Notification packages

```bash
dotnet add src/Services/Notification/HireFlow.Notification.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/Services/Notification/HireFlow.Notification.Infrastructure package Microsoft.EntityFrameworkCore.Design
dotnet add src/Services/Notification/HireFlow.Notification.Application package FluentValidation
dotnet add src/Services/Notification/HireFlow.Notification.Api package Serilog.AspNetCore
dotnet add src/Services/Notification/HireFlow.Notification.Api package AspNetCore.HealthChecks.NpgSql
```

#### Distributed-system packages

```bash
dotnet add src/BuildingBlocks/BuildingBlocks.Infrastructure package StackExchange.Redis
dotnet add src/BuildingBlocks/BuildingBlocks.Infrastructure package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet add src/BuildingBlocks/BuildingBlocks.Infrastructure package AspNetCore.HealthChecks.Redis
dotnet add src/BuildingBlocks/BuildingBlocks.Observability package OpenTelemetry.Extensions.Hosting
dotnet add src/BuildingBlocks/BuildingBlocks.Observability package OpenTelemetry.Exporter.OpenTelemetryProtocol
dotnet add src/BuildingBlocks/BuildingBlocks.Observability package OpenTelemetry.Instrumentation.AspNetCore
dotnet add src/BuildingBlocks/BuildingBlocks.Observability package OpenTelemetry.Instrumentation.Http
dotnet add src/BuildingBlocks/BuildingBlocks.Observability package OpenTelemetry.Instrumentation.Runtime
dotnet add src/BuildingBlocks/BuildingBlocks.Observability package Microsoft.Extensions.Http.Resilience
```

Add the distributed packages to each API project that directly calls the shared registration extensions:

```bash
dotnet add src/Services/Identity/HireFlow.Identity.Api package Microsoft.Extensions.Http.Resilience
dotnet add src/Services/Hiring/HireFlow.Hiring.Api package Microsoft.Extensions.Http.Resilience
dotnet add src/Services/Notification/HireFlow.Notification.Api package Microsoft.Extensions.Http.Resilience
```

The web API template already includes the ASP.NET Core OpenAPI packages. Add Swagger UI only if you want the interactive UI:

```bash
dotnet add src/Services/Identity/HireFlow.Identity.Api package Swashbuckle.AspNetCore
dotnet add src/Services/Hiring/HireFlow.Hiring.Api package Swashbuckle.AspNetCore
dotnet add src/Services/Notification/HireFlow.Notification.Api package Swashbuckle.AspNetCore
```

Pin package versions through `Directory.Packages.props` when the project stabilizes. Do not add packages to every project automatically; add a package only to the project that uses it.

---

## 8. Backend Implementation Order

Build in this exact order:

1. Domain entities and enums

1. EF Core `DbContext`

1. Entity configurations

1. Initial migration

1. Database creation/update

1. Application interfaces and use cases

1. JWT token service

1. Registration and OTP verification

1. Login and refresh-token rotation

1. Authorization policies

1. Exception middleware

1. Validation and API endpoints

1. Health checks and OpenAPI

1. Tests

1. Dockerfiles and CI

This keeps the implementation understandable and avoids building the frontend against unstable endpoints.

---

## 9. Identity Service: Essential Features

### 9.1 Entity and domain rules

Create `User`, `Role`, `RefreshToken`, and `OtpChallenge` entities.

Domain rules:

- Email is normalized to lowercase.

- Password must meet a configurable policy.

- A new account cannot log in until email verification succeeds.

- OTP expires after 10 minutes.

- OTP has a maximum attempt count.

- Only one active OTP per user and purpose is allowed.

- Refresh tokens are one-time-use and rotated.

- Reuse of a revoked refresh token revokes the token family.

- Locked accounts cannot authenticate until lockout expires.

### 9.2 EF Core DbContext

Use `IEntityTypeConfiguration<T>` classes instead of putting all configuration inside `OnModelCreating`.

Example connection string in local user secrets:

```json
{
  "ConnectionStrings": {
    "IdentityDatabase": "Host=localhost;Port=5432;Database=hireflow_identity;Username=identity_app;Password=identity_local_password"
  }
}
```

Register the context in Infrastructure and expose an extension such as:

```csharp
public static IServiceCollection AddIdentityInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.AddDbContext<IdentityDbContext>(options =>
        options.UseNpgsql(configuration.GetConnectionString("IdentityDatabase")));

    return services;
}
```

### 9.3 Migrations

Install the EF tool once:

```bash
dotnet tool install --global dotnet-ef
```

Create and apply the migration:

```bash
dotnet ef migrations add InitialIdentity \
  --project src/Services/Identity/HireFlow.Identity.Infrastructure \
  --startup-project src/Services/Identity/HireFlow.Identity.Api \
  --output-dir Persistence/Migrations

dotnet ef database update \
  --project src/Services/Identity/HireFlow.Identity.Infrastructure \
  --startup-project src/Services/Identity/HireFlow.Identity.Api
```

Commit migrations to Git. Do not edit an already-applied migration; create a new migration.

### 9.4 Registration flow

`POST /api/v1/auth/register`

Request:

```json
{
  "email": "candidate@example.com",
  "password": "Use-a-strong-password-123!",
  "firstName": "Asha",
  "lastName": "Sharma",
  "role": "Candidate"
}
```

Flow:

1. Validate request.

1. Normalize email.

1. Check uniqueness.

1. Hash password.

1. Create user with `IsEmailVerified = false`.

1. Assign the Candidate role; do not accept privileged roles from public registration.

1. Generate a cryptographically secure six-digit OTP.

1. Store only the OTP hash.

1. Ask Notification Service to send the OTP.

1. Return a generic success response.

`POST /api/v1/auth/verify-email`

```json
{
  "email": "candidate@example.com",
  "otp": "482913"
}
```

Never return the OTP in the API response or logs.

### 9.5 Login and JWT design

`POST /api/v1/auth/login`

Return:

```json
{
  "accessToken": "eyJ...",
  "expiresIn": 900,
  "refreshToken": "opaque-random-token",
  "user": {
    "id": "uuid",
    "email": "candidate@example.com",
    "roles": ["Candidate"]
  }
}
```

JWT claims should include:

- `sub`: user ID

- `jti`: unique token ID

- `email`

- `role`: one claim per role

- `iss`: issuer

- `aud`: audience

- `iat`

- `exp`

Use a short access-token lifetime, such as 15 minutes. Use a longer refresh-token lifetime, such as 7–30 days, depending on your security policy.

### 9.6 Refresh-token rotation

`POST /api/v1/auth/refresh`

Rules:

1. Hash the submitted refresh token.

1. Find the token record.

1. Reject expired or revoked tokens.

1. Revoke the old token.

1. Create a replacement token.

1. Issue a new access token.

1. Store only the new token hash.

If an already-revoked token is reused, treat it as possible token theft and revoke the entire token family.

### 9.7 Logout

`POST /api/v1/auth/logout`

- Revoke the current refresh token.

- Make the endpoint idempotent.

- Do not rely on logout to invalidate an already-issued JWT immediately; use short access-token lifetimes and server-side refresh-token revocation.

### 9.8 Password reset

Implement:

- `POST /api/v1/auth/forgot-password`

- `POST /api/v1/auth/reset-password`

Always return a generic response from the forgot-password endpoint so attackers cannot enumerate accounts.

---

## 10. JWT Configuration

Use environment variables or user secrets for signing credentials.

Example configuration shape:

```json
{
  "Jwt": {
    "Issuer": "HireFlow.Identity",
    "Audience": "HireFlow.Api",
    "AccessTokenMinutes": 15,
    "SigningKey": "development-only-long-secret-change-this"
  }
}
```

For production, prefer an asymmetric key pair or a managed secret store. Never commit signing keys.

Configure validation:

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["Jwt:Issuer"],
            ValidAudience = configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(configuration["Jwt:SigningKey"]!)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
```

Add authorization policies:

```csharp
services.AddAuthorization(options =>
{
    options.AddPolicy("RecruiterOnly", policy =>
        policy.RequireRole("Recruiter", "Admin"));

    options.AddPolicy("HiringManagerOnly", policy =>
        policy.RequireRole("HiringManager", "Admin"));
});
```

---

## 11. Hiring Service API

### Jobs

| Method | Route | Permission |
| --- | --- | --- |
| POST | `/api/v1/jobs` | Recruiter, HiringManager |
| GET | `/api/v1/jobs` | Authenticated users |
| GET | `/api/v1/jobs/{id}` | Authenticated users |
| PUT | `/api/v1/jobs/{id}` | Owner, Recruiter, Admin |
| PATCH | `/api/v1/jobs/{id}/publish` | Recruiter, HiringManager |
| DELETE | `/api/v1/jobs/{id}` | Owner, Admin |

Support query parameters:

```
GET /api/v1/jobs?search=dotnet&status=Published&page=1&pageSize=20&sort=-createdAt
```

Return a consistent paginated envelope:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3
}
```

### Applications

| Method | Route | Permission |
| --- | --- | --- |
| POST | `/api/v1/jobs/{jobId}/applications` | Candidate |
| GET | `/api/v1/me/applications` | Candidate |
| GET | `/api/v1/jobs/{jobId}/applications` | Recruiter, HiringManager |
| PATCH | `/api/v1/applications/{id}/status` | Recruiter, HiringManager |
| POST | `/api/v1/applications/{id}/withdraw` | Candidate |
| GET | `/api/v1/applications/{id}/history` | Authorized participant |

### Interviews

| Method | Route | Permission |
| --- | --- | --- |
| POST | `/api/v1/applications/{id}/interviews` | Recruiter, HiringManager |
| GET | `/api/v1/me/interviews` | Candidate |
| GET | `/api/v1/interviews` | Recruiter, HiringManager |
| PATCH | `/api/v1/interviews/{id}/status` | Authorized participant |

### API design rules

- Use nouns in routes.

- Use HTTP status codes correctly.

- Use `201 Created` after creation.

- Use `204 No Content` for successful operations with no response body.

- Use `409 Conflict` for duplicates and invalid state transitions.

- Use `422 Unprocessable Entity` for semantically invalid input where appropriate.

- Include a correlation ID in responses.

- Version the API under `/api/v1`.

- Generate OpenAPI documentation for all endpoints.

---

## 12. Consistent Error Handling

Return RFC 9457-style `ProblemDetails`:

```json
{
  "type": "https://api.hireflow.local/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more fields are invalid.",
  "instance": "/api/v1/jobs",
  "traceId": "00-abc123...",
  "errors": {
    "title": ["Title is required."]
  }
}
```

Create middleware that:

- Maps known domain exceptions to safe HTTP responses.

- Logs unexpected exceptions with the correlation ID.

- Never exposes stack traces outside Development.

- Returns a stable error contract to React.

Expected mapping:

| Exception | HTTP status |
| --- | --- |
| ValidationException | 400 |
| UnauthorizedAccessException | 401 |
| ForbiddenException | 403 |
| NotFoundException | 404 |
| ConflictException | 409 |
| Unexpected exception | 500 |

---

## 13. Cross-Cutting Backend Features

### Validation

Validate at the application boundary:

- Email format and length

- Password policy

- Required fields

- Date ranges

- Salary ranges

- Interview end time after start time

- Allowed status transitions

- Pagination limits, with a maximum `pageSize`

### Rate limiting

Protect:

- Login

- Registration

- OTP resend

- Password reset

- Public job search

Use ASP.NET Core rate limiting policies for the local development fallback. In production, enforce limits through a shared Redis-compatible store so the limit is consistent across every Render instance. Return `429 Too Many Requests` with a `Retry-After` header and include a response header such as `X-RateLimit-Remaining` where appropriate.

Use separate policies rather than one global number:

- Login: strict per-IP and per-account limits

- OTP resend: strict per-user and per-IP limits

- Password reset: strict per-email and per-IP limits

- Public job search: higher anonymous limit

- Authenticated reads: user or client-identity limit

- Admin operations: lower-volume role-aware limit

### Health checks

Expose:

- `GET /health/live`: process is running

- `GET /health/ready`: database and required dependencies are available

Do not expose secrets or detailed connection strings in health responses.

### Structured logging

Log fields such as:

- `Timestamp`

- `Level`

- `Message`

- `TraceId`

- `CorrelationId`

- `UserId` when available

- `Service`

- `Environment`

- `Endpoint`

- `ElapsedMilliseconds`

Do not log credentials, raw tokens, OTPs, or sensitive personal documents.

### Correlation IDs

Accept `X-Correlation-ID` from trusted callers or generate one per request. Return it in the response header and include it in every log event.

### CORS

Allow only the configured frontend origin:

```
http://localhost:5173
```

Do not use `AllowAnyOrigin` with credentials in production.

### Idempotency

For operations such as application submission and interview scheduling, use an idempotency key or a unique database constraint to prevent duplicate requests.

---

## 14. Microservice Communication

### Phase 1: synchronous HTTP

Use typed `HttpClient` only where one service must request another service immediately. Configure:

- Base URL through configuration

- Timeout

- Retry only for safe transient failures

- Circuit breaker for repeated failures

- Correlation ID propagation

- Authentication between services

### Phase 2: events

Introduce events:

```
UserRegistered
EmailVerificationRequested
ApplicationSubmitted
ApplicationStatusChanged
InterviewScheduled
PasswordResetRequested
```

Use an outbox pattern:

1. Save the business transaction and an outbox record in the same database transaction.

1. A background worker publishes the outbox message.

1. Mark the message as published.

1. Retry failed messages with backoff.

1. Make consumers idempotent.

This is a strong interview topic because it addresses the dual-write problem.

---

## 15. Phase 3 — Frontend Setup in VS Code

### Install frontend prerequisites

```bash
node --version
npm --version
```

Use the current Node.js LTS release. Create the React app with Vite:

```bash
npm create vite@latest frontend/hireflow-web -- --template react-ts
cd frontend/hireflow-web
npm install
```

Install core libraries:

```bash
npm install react-router-dom @tanstack/react-query axios react-hook-form zod @hookform/resolvers
npm install lucide-react
npm install -D eslint prettier vitest jsdom @testing-library/react @testing-library/jest-dom @testing-library/user-event playwright
```

For styling, choose one consistent approach:

```bash
npm install tailwindcss @tailwindcss/vite
```

### Frontend structure

```
frontend/hireflow-web/src/
├── app/
│   ├── App.tsx
│   ├── router.tsx
│   ├── queryClient.ts
│   └── providers.tsx
├── assets/
├── components/
│   ├── ui/
│   ├── forms/
│   ├── data-table/
│   └── feedback/
├── features/
│   ├── auth/
│   │   ├── api.ts
│   │   ├── types.ts
│   │   ├── hooks.ts
│   │   └── pages/
│   ├── jobs/
│   ├── applications/
│   ├── interviews/
│   └── dashboard/
├── layouts/
│   ├── PublicLayout.tsx
│   └── AppLayout.tsx
├── lib/
│   ├── apiClient.ts
│   ├── authStorage.ts
│   ├── logger.ts
│   └── utils.ts
├── routes/
├── styles/
├── test/
├── main.tsx
└── vite-env.d.ts
```

### Frontend screens

#### Public

- Landing page

- Job listing

- Job details

- Login

- Register

- Verify email OTP

- Forgot password

- Reset password

#### Candidate

- Candidate dashboard

- My applications

- Application details and timeline

- My interviews

- Profile settings

#### Recruiter / Hiring Manager

- Hiring dashboard

- Jobs table

- Create/edit job

- Applicants table

- Applicant detail drawer

- Status workflow actions

- Interview scheduling

#### Admin

- User management

- Role management

- Audit log

- System health summary

### API client

Create one Axios instance:

```
import axios from "axios";

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: { "Content-Type": "application/json" },
} );
```

Add interceptors for:

- Access-token attachment

- Handling `401`

- Refresh-token flow

- Redirecting to login after refresh failure

- Correlation ID generation

Prefer storing the access token in memory. If refresh tokens are held in cookies, use `HttpOnly`, `Secure`, and appropriate `SameSite` settings. Avoid casually storing long-lived tokens in `localStorage`.

### Authentication state

Create an `AuthProvider` or a small auth store that exposes:

```
{
  user,
  isAuthenticated,
  isLoading,
  login,
  logout,
  refreshSession,
}
```

Create route guards:

- `RequireAuth`

- `RequireRole`

- `PublicOnly`

The backend remains the authority. Frontend guards improve user experience but are not security controls.

### TanStack Query conventions

Use query keys such as:

```
["jobs", { search, status, page, pageSize }]
["job", jobId]
["applications", { jobId, status, page }]
["my-applications"]
```

Invalidate related queries after mutations instead of manually duplicating server state.

### Form conventions

For each form:

1. Define a Zod schema.

1. Connect it to React Hook Form.

1. Show field-level errors.

1. Disable submit while pending.

1. Show a success or error notification.

1. Preserve accessible labels and keyboard navigation.

### UI quality checklist

- Responsive layout

- Empty states

- Loading skeletons

- Error states with retry

- Confirmation for destructive actions

- Accessible color contrast

- Focus states

- Keyboard navigation

- Toast notifications

- Pagination and filters

- Mobile-friendly tables

- Human-readable dates and status badges

---

## 16. Environment Configuration

### Backend `.env.example`

```
ASPNETCORE_ENVIRONMENT=Development
IDENTITY_DB_CONNECTION=Host=localhost;Port=5432;Database=hireflow_identity;Username=identity_app;Password=identity_local_password
HIRING_DB_CONNECTION=Host=localhost;Port=5432;Database=hireflow_hiring;Username=hiring_app;Password=hiring_local_password
NOTIFICATION_DB_CONNECTION=Host=localhost;Port=5432;Database=hireflow_notification;Username=notification_app;Password=notification_local_password
JWT_ISSUER=HireFlow.Identity
JWT_AUDIENCE=HireFlow.Api
JWT_SIGNING_KEY=replace-with-a-long-development-secret
SMTP_HOST=localhost
SMTP_PORT=1025
FRONTEND_ORIGIN=http://localhost:5173
```

### Frontend `.env.example`

```
VITE_API_BASE_URL=http://localhost:5001
```

Never commit `.env`, real secrets, signing keys, production credentials, or personal data.

Use .NET User Secrets locally:

```bash
dotnet user-secrets init --project src/Services/Identity/HireFlow.Identity.Api
dotnet user-secrets set "Jwt:SigningKey" "local-only-long-development-secret" --project src/Services/Identity/HireFlow.Identity.Api
```

---

## 17. Local Run Sequence

### Start infrastructure

```bash
docker compose up -d postgres mailpit
```

### Apply migrations

Run the EF database update command for Identity, Hiring, and Notification.

### Start services

Assign ports:

```
Identity API      http://localhost:5001
Hiring API        http://localhost:5002
Notification API  http://localhost:5003
```

Run from separate VS Code terminals:

```bash
dotnet run --project src/Services/Identity/HireFlow.Identity.Api
 dotnet run --project src/Services/Hiring/HireFlow.Hiring.Api
 dotnet run --project src/Services/Notification/HireFlow.Notification.Api
```

### Start React

```bash
cd frontend/hireflow-web
npm run dev
```

Open:

- Frontend: `http://localhost:5173`

- Identity Swagger: `http://localhost:5001/swagger`

- Hiring Swagger: `http://localhost:5002/swagger`

- Mailpit: `http://localhost:8025`

### Recommended VS Code tasks

Create `.vscode/tasks.json` for:

- Start Docker Compose

- Apply migrations

- Run Identity API

- Run Hiring API

- Run Notification API

- Run frontend

- Run all tests

Create `.vscode/launch.json` for debugging all APIs and the frontend.

---

## 18. Testing Strategy

### Backend unit tests

Test:

- Password policy

- OTP expiry and attempt limits

- Refresh-token rotation

- Token reuse detection

- Job publishing rules

- Application status transitions

- Interview date validation

- Authorization policies

### Integration tests

Use `WebApplicationFactory` and a disposable PostgreSQL database.

Test complete API flows:

1. Register.

1. Verify email.

1. Login.

1. Refresh token.

1. Create a job as recruiter.

1. Publish job.

1. Apply as candidate.

1. Move application to shortlist.

1. Schedule interview.

1. Confirm audit history.

### Frontend tests

Test:

- Login form validation

- OTP form behavior

- Protected routes

- Job filters

- Application submission

- Status update modal

- Loading and error states

### End-to-end tests

Use Playwright for the most important user journey:

```
Candidate registers -> verifies OTP in test mode -> logs in -> views job -> applies -> views application timeline
Recruiter logs in -> creates job -> publishes job -> reviews applicant -> schedules interview
```

### Test naming convention

Use behavior-focused names:

```
Register_WhenEmailAlreadyExists_ReturnsConflict
Refresh_WhenTokenWasAlreadyUsed_RevokesTokenFamily
PublishJob_WhenUserIsCandidate_ReturnsForbidden
SubmitApplication_WhenAlreadyApplied_ReturnsConflict
```

---

## 19. Dockerization

Create one Dockerfile per API. Example pattern:

```
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/Services/Identity/HireFlow.Identity.Api/HireFlow.Identity.Api.csproj
RUN dotnet publish src/Services/Identity/HireFlow.Identity.Api/HireFlow.Identity.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HireFlow.Identity.Api.dll"]
```

Use multi-stage builds and run as a non-root user where practical.

For the React app, build static assets and serve them through Nginx or another production web server. Inject environment-specific API configuration safely; do not put secrets in frontend bundles.

---

## 20. CI/CD Checklist

Create GitHub Actions workflows that run on pull requests:

### Backend pipeline

```
git checkout
setup .NET 10
restore
build --no-restore
run unit tests
run integration tests
run format check
run security/dependency scan
build Docker images
```

### Frontend pipeline

```
setup Node LTS
npm ci
npm run lint
npm run typecheck
npm run test -- --run
npm run build
```

### Branch strategy

```
main       -> production-ready
 develop   -> integration branch
 feature/* -> individual changes
```

Use pull requests with:

- Small focused commits

- Clear descriptions

- Screenshots for UI work

- Test evidence

- Database migration notes

- Breaking API change notes

---

## 21. Security Checklist

### Authentication

- [ ] Passwords are hashed with a proven framework implementation.

- [ ] Email must be verified before login.

- [ ] OTP values are hashed at rest.

- [ ] OTPs expire and have attempt limits.

- [ ] Access tokens are short-lived.

- [ ] Refresh tokens are hashed and rotated.

- [ ] Reuse detection revokes the token family.

- [ ] JWT issuer, audience, lifetime, and signature are validated.

- [ ] Signing keys are outside source control.

### Authorization

- [ ] Backend checks roles and ownership.

- [ ] Candidate cannot access another candidate’s application.

- [ ] Public registration cannot assign Admin.

- [ ] Admin endpoints require explicit policy.

- [ ] Frontend guards are treated only as UX.

### API and data

- [ ] Rate limits protect authentication endpoints.

- [ ] CORS is restricted.

- [ ] ProblemDetails does not expose internals.

- [ ] SQL uses EF Core parameterization.

- [ ] Uploaded files use type, size, and malware controls in production.

- [ ] Audit logs exclude secrets.

- [ ] PII is minimized and protected.

- [ ] HTTPS is required outside local development.

---

## 22. Portfolio Documentation That Impresses Reviewers

Your `README.md` should include:

1. One-paragraph project summary

1. Architecture diagram

1. Feature list

1. Technology choices

1. Local setup commands

1. Environment variable instructions

1. Service URLs

1. Database migration commands

1. API documentation links

1. Test commands

1. Screenshots or a short demo GIF

1. Security decisions

1. Trade-offs and future improvements

1. Known limitations

Add Architecture Decision Records:

- Why separate services were chosen

- Why JWT plus refresh tokens was used

- Why the first implementation uses synchronous HTTP

- When and why an outbox/event broker would be introduced

- Why each service owns its own database

### Good interview talking points

- “I used a database per service to preserve ownership and reduce coupling.”

- “I store only hashes of refresh tokens and OTPs, not the raw values.”

- “I rotate refresh tokens and detect reuse to limit token theft impact.”

- “The frontend uses TanStack Query for server state instead of duplicating API data in local state.”

- “I use ProblemDetails and correlation IDs so frontend errors and backend logs can be connected.”

- “I started with synchronous calls to keep the MVP understandable, and designed an outbox path for reliable events.”

- “The API is versioned, documented with OpenAPI, rate-limited, tested, and containerized.”

---

## 23. Eight-Week Build Plan

### Week 1: Foundation

- Repository setup

- Docker Compose

- PostgreSQL databases

- .NET solution and project boundaries

- React/Vite setup

- README skeleton

### Week 2: Identity database and authentication

- Entities and migrations

- Registration

- Password hashing

- Email OTP

- Login

- JWT access tokens

- Refresh-token rotation

### Week 3: Identity hardening

- Roles and policies

- Lockout

- Password reset

- Rate limits

- ProblemDetails

- Health checks

- Identity tests

### Week 4: Hiring database and jobs

- Companies and jobs

- Migrations

- Create/edit/publish job APIs

- Search, filtering, pagination

- Hiring tests

### Week 5: Applications and interviews

- Application workflow

- Status history

- Interview scheduling

- Authorization and ownership checks

- Audit logging

### Week 6: Notification service

- Email templates

- Mailpit integration

- Delivery attempts

- HTTP integration or outbox foundation

### Week 7: React application

- Authentication screens

- Role-based layouts

- Jobs and job details

- Candidate applications

- Recruiter dashboard

- Interview screens

### Week 8: Production polish

- Frontend tests

- End-to-end tests

- Dockerfiles

- CI pipeline

- Screenshots and demo

- Architecture diagrams

- Resume bullets

---

## 24. Definition of Done

The project is ready to show recruiters when:

- [ ] A new developer can run it from the README.

- [ ] `docker compose up -d` starts local infrastructure.

- [ ] Database migrations run cleanly from an empty database.

- [ ] Registration and email OTP verification work through Mailpit.

- [ ] Login issues an access token and refresh token.

- [ ] Refresh-token rotation and logout work.

- [ ] Roles prevent unauthorized actions.

- [ ] A recruiter can create and publish a job.

- [ ] A candidate can apply once and view status history.

- [ ] A recruiter can schedule an interview.

- [ ] All APIs have OpenAPI documentation.

- [ ] Health endpoints work.

- [ ] Errors use a consistent contract.

- [ ] Backend and frontend have automated tests.

- [ ] No secrets are committed.

- [ ] Docker builds succeed.

- [ ] CI passes on a clean clone.

- [ ] README includes screenshots, architecture, setup, and trade-offs.

---

## 25. Final Implementation Principle

Do not try to build every advanced feature on day one. Build one complete, secure vertical slice first:

```
Register -> Verify OTP -> Login -> Create Job -> Publish Job -> Apply -> Change Status -> Schedule Interview
```

Once that slice works end-to-end, add the quality features that distinguish the project:

- Refresh-token rotation

- Rate limiting

- ProblemDetails

- Correlation IDs

- Audit logs

- Health checks

- Tests

- Docker

- CI

- OpenAPI

- Outbox-ready event design

That combination gives you a project that is **small enough to finish, realistic enough to discuss, and technically strong enough to support a serious portfolio or resume conversation**.

---

## 26. GitHub Repository and Cloud Deployment Plan

The portfolio will use two public GitHub repositories and separate deployment targets:

```
GitHub: hireflow-frontend  ──> Vercel ──> React web application
                                      |
                                      | HTTPS API calls
                                      v
GitHub: hireflow-backend   ──> Render ──> Identity API
                                      ├─> Hiring API
                                      └─> Notification API

Render PostgreSQL ────────────────> service-owned production databases
```

### 26.1 Create the two GitHub repositories

Create these repositories:

#### `hireflow-backend`

Contains:

- ASP.NET Core services

- Shared backend building blocks

- EF Core migrations

- Dockerfiles

- OpenAPI documentation

- Unit and integration tests

- Backend GitHub Actions workflow

- Backend README with Render deployment instructions

Initial commands:

```bash
mkdir hireflow-backend
cd hireflow-backend
git init
git branch -M main
git add .
git commit -m "chore: initialize HireFlow backend"
git remote add origin https://github.com/<your-username>/hireflow-backend.git
git push -u origin main
```

#### `hireflow-frontend`

Contains:

- React and TypeScript source

- Vite configuration

- UI components and feature modules

- Frontend tests and Playwright tests

- Vercel configuration, if required

- Frontend GitHub Actions workflow

- Frontend README with the deployed Vercel URL

Initial commands:

```bash
npm create vite@latest hireflow-frontend -- --template react-ts
cd hireflow-frontend
npm install
git init
git branch -M main
git add .
git commit -m "chore: initialize HireFlow frontend"
git remote add origin https://github.com/<your-username>/hireflow-frontend.git
git push -u origin main
```

Do not commit `.env` files. Commit `.env.example` files with placeholder values only.

### 26.2 Render backend deployment architecture

Deploy the backend services independently on Render. Each service should have its own Render Web Service and its own public URL:

```
Identity API       https://hireflow-identity.onrender.com
Hiring API         https://hireflow-hiring.onrender.com
Notification API   https://hireflow-notification.onrender.com
```

You may begin with Identity and Hiring as public web services and keep Notification internal or protected until email delivery is implemented. The important portfolio point is that each service can be built, deployed, health-checked, and scaled independently.

#### Render services to create

| Render resource | Purpose |
| --- | --- |
| Identity Web Service | Registration, OTP, login, JWT, refresh tokens |
| Hiring Web Service | Jobs, applications, interviews, audit history |
| Notification Web Service | Email templates and delivery attempts |
| Render PostgreSQL | Production database infrastructure |
| Optional background worker | Outbox publishing or email retry processing |

#### Recommended Render deployment method

Use Docker deployment for each backend service so local and cloud environments use the same runtime assumptions.

For each service:

1. Open Render and choose **New > Web Service**.

1. Connect the `hireflow-backend` GitHub repository.

1. Select the correct branch, normally `main`.

1. Set the service root directory if using a monorepo-style backend repository.

1. Choose Docker as the runtime.

1. Point Render to the service-specific Dockerfile.

1. Configure the service port to match the container port, for example `8080`.

1. Add service-specific environment variables.

1. Configure the health-check path as `/health/ready`.

1. Deploy and inspect the logs.

If the backend repository contains all three services, configure one Render service per Dockerfile. For example:

```
Dockerfile.identity       -> Identity Web Service
Dockerfile.hiring         -> Hiring Web Service
Dockerfile.notification   -> Notification Web Service
```

### 26.3 Render PostgreSQL setup

Use managed PostgreSQL for the deployed environment. Do not use the local Docker database connection string in Render.

Recommended database options:

- Separate Render PostgreSQL databases for Identity, Hiring, and Notification for strict ownership.

- One Render PostgreSQL instance with separate databases if cost and operational simplicity are more important for the initial MVP.

The application should still preserve service ownership even when databases share an infrastructure instance.

For production-like separation, configure:

```
Identity service       -> hireflow_identity production database
Hiring service         -> hireflow_hiring production database
Notification service   -> hireflow_notification production database
```

Run migrations safely as part of a controlled deployment step. Do not blindly run destructive schema operations during application startup.

### 26.4 Render environment variables

Configure secrets in the Render dashboard or secret-management integration, not in GitHub source files.

#### Identity Web Service

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__IdentityDatabase=<render-identity-connection-string>
Jwt__Issuer=https://hireflow-identity.onrender.com
Jwt__Audience=https://hireflow-hiring.onrender.com
Jwt__SigningKey=<strong-production-secret>
Jwt__AccessTokenMinutes=15
Frontend__Origin=https://<your-vercel-project>.vercel.app
Notification__BaseUrl=https://hireflow-notification.onrender.com
```

#### Hiring Web Service

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__HiringDatabase=<render-hiring-connection-string>
Identity__BaseUrl=https://hireflow-identity.onrender.com
Frontend__Origin=https://<your-vercel-project>.vercel.app
```

#### Notification Web Service

```
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
ConnectionStrings__NotificationDatabase=<render-notification-connection-string>
Smtp__Host=<production-smtp-host>
Smtp__Port=<production-smtp-port>
Smtp__Username=<production-smtp-username>
Smtp__Password=<production-smtp-password>
```

Never put the JWT signing key, SMTP password, database password, or API credentials in `appsettings.json` committed to GitHub.

### 26.5 Vercel frontend deployment

Deploy the React application from the separate `hireflow-frontend` repository:

1. Open Vercel and choose **Add New > Project**.

1. Import `hireflow-frontend` from GitHub.

1. Select the production branch, normally `main`.

1. Confirm the framework is Vite.

1. Set the build command to `npm run build`.

1. Set the output directory to `dist`.

1. Set the install command to `npm ci`.

1. Add the production environment variable:

```
VITE_API_BASE_URL=https://hireflow-hiring.onrender.com
VITE_IDENTITY_API_BASE_URL=https://hireflow-identity.onrender.com
```

1. Deploy the project.

1. Add the Vercel production URL to the backend CORS allowlist.

1. Test authentication and API calls from the deployed Vercel domain.

If the frontend uses a single API gateway later, only `VITE_API_BASE_URL` is needed. Until then, keep service base URLs in one typed configuration file rather than scattering URLs throughout components.

### 26.6 CORS and production URL configuration

Local development and production need separate allowed origins:

```
Local:       http://localhost:5173
Production:  https://<your-vercel-project>.vercel.app
```

Configure CORS in every API that receives browser requests:

- Allow only the exact Vercel origin.

- Allow required methods and headers.

- Allow credentials only when the cookie-based refresh-token design requires it.

- Do not use `AllowAnyOrigin` in production.

- Do not add a trailing slash mismatch to the configured origin.

Example production CORS configuration shape:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://<your-vercel-project>.vercel.app"
    ]
  }
}
```

### 26.7 Vercel SPA routing

Because React Router handles client-side routes, configure a Vercel rewrite so refreshing routes such as `/jobs/123` loads the application instead of returning a platform 404.

Create `vercel.json` in the frontend repository:

```json
{
  "rewrites": [
    {
      "source": "/(.* )",
      "destination": "/index.html"
    }
  ]
}
```

Verify that static assets still load correctly after adding the rewrite.

### 26.8 Production deployment sequence

Use this order for the first deployment:

1. Create Render PostgreSQL resources.

1. Configure backend secrets in Render.

1. Deploy Identity API.

1. Run Identity database migrations.

1. Verify `GET /health/live` and `GET /health/ready`.

1. Deploy Notification API and test email delivery.

1. Deploy Hiring API.

1. Run Hiring database migrations.

1. Add deployed service URLs to the frontend environment configuration.

1. Deploy the frontend to Vercel.

1. Add the Vercel URL to backend CORS settings.

1. Redeploy backend services after the CORS configuration is updated.

1. Complete the end-to-end smoke test.

### 26.9 Deployment smoke test

After deployment, verify:

```
[ ] Vercel homepage loads over HTTPS.
[ ] Vercel job listing can call the Hiring API.
[ ] Registration works.
[ ] OTP email arrives through the configured email provider.
[ ] Email verification works.
[ ] Login issues an access token.
[ ] Refresh-token rotation works.
[ ] Candidate can view a published job.
[ ] Candidate can submit one application.
[ ] Recruiter can view and update the application.
[ ] Interview scheduling works.
[ ] Unauthorized API requests return 401 or 403 correctly.
[ ] Health endpoints report the expected status.
[ ] Render logs contain no secrets or raw tokens.
[ ] Browser console contains no CORS errors.
```

### 26.10 GitHub Actions and deployment protection

Keep CI in both repositories:

#### Backend repository workflow

```
Pull request -> restore -> build -> unit tests -> integration tests -> Docker build
Main branch  -> all checks -> deploy through Render integration or approved deploy hook
```

#### Frontend repository workflow

```
Pull request -> npm ci -> lint -> typecheck -> unit tests -> production build
Main branch  -> all checks -> Vercel deployment
```

Use protected `main` branches and require successful checks before merging. Keep production deployment tied to the `main` branch, while preview deployments can be used for pull requests.

### 26.11 README deployment information

Add the following badges and links to both repositories:

- Build status

- Test status

- Live frontend URL

- API Swagger URL

- Health endpoint URL

- Architecture documentation

- Local setup instructions

- Deployment instructions

Recommended top section for `hireflow-frontend/README.md`:

```
Live application: https://<your-vercel-project>.vercel.app
Backend API: https://hireflow-hiring.onrender.com
```

Recommended top section for `hireflow-backend/README.md`:

```
Frontend: https://<your-vercel-project>.vercel.app
Identity API: https://hireflow-identity.onrender.com
Hiring API: https://hireflow-hiring.onrender.com
Notification API: https://hireflow-notification.onrender.com
```

This deployment arrangement demonstrates a realistic separation of concerns: the React client is delivered through Vercel's frontend workflow, while independently deployable .NET services run on Render with managed PostgreSQL and environment-specific secrets.

---

## 27. Distributed-System Upgrade — What Makes HireFlow Stand Out

Yes, HireFlow can be designed as a genuine distributed system rather than only a set of separate projects. The key is to demonstrate how services behave when networks fail, requests are duplicated, instances scale horizontally, and data becomes eventually consistent.

Do not add distributed technology only for decoration. Every component below should solve a visible engineering problem.

### 27.1 Target distributed architecture

```
                         +-------------------+
                         | Vercel React App  |
                         +---------+---------+
                                   |
                                   v
                         +-------------------+
                         | API Gateway/BFF    |
                         | Optional phase 2   |
                         +---------+---------+
                                   |
       +---------------------------+---------------------------+
       |                           |                           |
+------v------+              +-----v------+              +-----v------+
| Identity    |              | Hiring     |              | Notification|
| API x N     |              | API x N    |              | Worker/API  |
+------+------+              +-----+------+              +-----+------+
       |                           |                           |
       +---------------------------+---------------------------+
                                   |
                         +---------v---------+
                         | Redis-compatible  |
                         | distributed state |
                         +---------+---------+
                                   |
                    +--------------v--------------+
                    | Message broker / event bus  |
                    +--------------+--------------+
                                   |
              +--------------------+--------------------+
              |                    |                    |
       +------v------+      +------v------+      +------v------+
       | Identity DB |      | Hiring DB   |      | Notification |
       | owned data  |      | owned data  |      | owned data   |
       +-------------+      +-------------+      +-------------+
```

### 27.2 What “distributed” means in this project

HireFlow qualifies as a distributed system when:

- Services run as independent processes and deploy independently.

- Each service owns its database and can scale horizontally.

- Requests may cross a network boundary and can fail independently.

- Redis-backed controls are shared across service instances.

- Events are delivered asynchronously and may be delayed or retried.

- Consumers are idempotent because messages can be delivered more than once.

- Observability connects a user request across multiple services.

- The system has explicit behavior for timeouts, retries, partial failure, and recovery.

You do not need Kubernetes to demonstrate these principles. Multiple Render services, managed PostgreSQL, Redis, and a broker are enough for a strong portfolio implementation.

### 27.3 Distributed rate limiting with Redis

A local in-memory limiter is incorrect when the same API is running on multiple instances:

```
Request 1 -> Instance A -> local counter A = 1
Request 2 -> Instance B -> local counter B = 1
```

The user has effectively made two requests even though the intended shared limit was one. Use Redis as the shared counter/state store:

```
Request -> any API instance -> Redis atomic counter -> allow or reject
```

#### Recommended algorithm

Use a token bucket or sliding-window algorithm:

- Token bucket is easy to explain and supports controlled bursts.

- Sliding window is easy to reason about for login and OTP abuse prevention.

- Redis Lua scripts or atomic commands prevent race conditions.

- Key expiry automatically cleans old counters.

Example logical keys:

```
rate-limit:identity:login:ip:{ip}
rate-limit:identity:login:account:{emailHash}
rate-limit:identity:otp:user:{userId}
rate-limit:hiring:search:ip:{ip}
```

Never place raw email addresses or other sensitive values directly into Redis keys. Normalize and hash them first.

#### Policy example

| Operation | Identity key | Example policy |
| --- | --- | --- |
| Login | IP + normalized account hash | 5 failures per 15 minutes |
| OTP verification | User ID + IP | 5 attempts per 10 minutes |
| OTP resend | User ID + IP | 3 requests per 15 minutes |
| Forgot password | Email hash + IP | 3 requests per hour |
| Job search | IP or authenticated user | Higher burst limit |
| Application submit | User ID + job ID | Idempotency plus business limit |

Return:

```
HTTP/1.1 429 Too Many Requests
Retry-After: 120
X-RateLimit-Limit: 5
X-RateLimit-Remaining: 0
```

#### Redis failure policy

Choose and document a policy:

- **Fail closed** for login, OTP, and password-reset abuse controls.

- **Fail open with a conservative local fallback** for low-risk job search, while emitting an alert.

This trade-off is an excellent interview discussion because availability and security have different priorities.

### 27.4 Distributed idempotency

A client can retry because of a network timeout even when the server already completed the operation. Protect side-effecting endpoints with an `Idempotency-Key` header:

```
POST /api/v1/jobs/{jobId}/applications
Idempotency-Key: 2dbf7fd8-91f3-4fb8-b983-9f4d3b970e0a
```

Store:

- User ID

- Route and HTTP method

- Idempotency key

- Request hash

- Status code

- Response body or response reference

- Expiry time

Use a unique constraint on `(user_id, route, idempotency_key )`. Redis can provide a fast lock, but PostgreSQL must remain the source of truth for a completed business transaction.

Behavior:

1. First request creates the key record and performs the operation.

1. Concurrent requests receive `409` or wait briefly for the first request.

1. A retry returns the original response instead of creating a duplicate application.

1. Reusing a key with a different request body returns `409 Conflict`.

### 27.5 Outbox and inbox patterns

The outbox pattern is mandatory for reliable business events:

```
Business transaction + outbox insert
              |
              v
        Same database transaction
              |
              v
     Background publisher -> broker -> consumer
```

Add an `outbox_messages` table to the service that owns the event:

- `id`

- `event_type`

- `aggregate_type`

- `aggregate_id`

- `payload_json`

- `occurred_at_utc`

- `published_at_utc`

- `attempt_count`

- `last_error`

Add an `inbox_messages` table to consumers:

- `message_id` unique

- `consumer_name`

- `received_at_utc`

- `processed_at_utc`

- `status`

- `error`

The inbox makes repeated delivery safe. A consumer should check the message ID before applying the same business action twice.

### 27.6 Eventual consistency and sagas

Do not pretend that a multi-service operation is one ACID transaction. For example:

```
ApplicationSubmitted
  -> Hiring Service commits application
  -> outbox publishes event
  -> Notification Service sends confirmation email
```

If email delivery fails, the application must remain submitted. The notification can retry independently.

For a larger workflow, document a saga:

```
InterviewScheduled
  -> reserve interview record
  -> publish notification event
  -> send candidate email
  -> send recruiter email
```

Compensating actions may include canceling a pending notification or marking delivery as failed. Do not delete the original business record merely because a side effect failed.

### 27.7 Resilience patterns for service-to-service calls

Use typed `HttpClient` with Polly or the current .NET resilience pipeline:

- Timeout: every network call has a bounded timeout.

- Retry: retry only transient failures and only safe or idempotent operations.

- Exponential backoff: prevent synchronized retry storms.

- Jitter: randomize delays between clients.

- Circuit breaker: stop calling an unhealthy dependency temporarily.

- Bulkhead: limit concurrent calls to a dependency.

- Fallback: return a safe degraded response where appropriate.

Example policy intent:

```
Identity lookup:
  timeout 2 seconds
  retry 2 times for 5xx/timeouts
  circuit opens after repeated failures

Notification request:
  do not block application submission
  publish an outbox event instead

Job search:
  short timeout
  cached fallback for a brief period if safe
```

Never blindly retry login, payment-like operations, or any non-idempotent command without an idempotency strategy.

### 27.8 Distributed caching

Use Redis for carefully selected read-heavy data:

- Published job detail by job ID

- Job search result pages for a short TTL

- Role or permission metadata if it is stable

- Feature flags

- Rate-limit counters

- Short-lived distributed locks

Cache rules:

- Cache-aside: read cache, load from database on miss, then set cache.

- Use short TTLs for mutable job data.

- Invalidate after job edits, publishing, or closing.

- Never cache private candidate data without a user-scoped key.

- Never treat cache as the source of truth.

### 27.9 Distributed locking

Use a short-lived Redis lock only for operations that truly need cross-instance coordination, such as:

- One outbox publisher leader per partition

- Preventing duplicate scheduled maintenance work

- Coordinating a rare job that must run once at a time

Every lock needs:

- Unique owner token

- Expiration time

- Safe release only by the owner

- A timeout and recovery path

Prefer database uniqueness and idempotency over distributed locks whenever possible.

### 27.10 Observability across services

Add OpenTelemetry to every backend service:

- Distributed traces for HTTP requests

- Trace propagation through `traceparent`

- Metrics for request count, latency, error rate, and saturation

- Database query duration

- Redis latency and failures

- Outbox backlog size

- Event processing delay

- Rate-limit rejection count

- Circuit-breaker open count

Use a local development collector such as Jaeger or Aspire Dashboard if desired. In deployment, send telemetry to a compatible hosted observability platform.

Every request should be traceable like this:

```
Vercel browser request
  -> Hiring API trace
  -> Redis rate-limit span
  -> PostgreSQL span
  -> outbox publish span
  -> Notification consumer trace
  -> SMTP delivery span
```

### 27.11 Metrics worth showing in the dashboard

Create a small operations dashboard or document these metrics in the README:

| Metric | Why it matters |
| --- | --- |
| Request rate | Traffic volume |
| p50/p95/p99 latency | User experience and tail behavior |
| 4xx/5xx rate | Correctness and availability |
| Rate-limit rejects | Abuse or capacity pressure |
| Database pool usage | Resource saturation |
| Redis latency | Shared-state health |
| Outbox pending count | Event delivery health |
| Consumer retry count | Downstream instability |
| Circuit-breaker state | Dependency health |
| Login failures | Security signal |

### 27.12 Security improvements for a market-ready project

Add these after the core vertical slice:

- Asymmetric JWT signing with key rotation and a JWKS endpoint.

- Secret management through Render environment secrets or a managed vault.

- Refresh-token family revocation.

- Device/session list with remote logout.

- Security headers and strict HTTPS.

- Request body size limits.

- File upload scanning if resumes are uploaded.

- PII masking in logs.

- Audit log integrity and retention rules.

- Dependency vulnerability scanning in GitHub Actions.

- Container image scanning.

- Threat model covering credential stuffing, token theft, replay, IDOR, SSRF, and data leakage.

### 27.13 Load and failure testing

Use k6 or another load-testing tool to demonstrate:

- Login rate limits work under concurrent traffic.

- Multiple API instances share one Redis limit.

- Duplicate application submissions produce one application.

- Notification failures do not lose the application transaction.

- A slow dependency triggers a timeout and circuit breaker.

- The outbox retries and eventually drains after recovery.

Failure scenarios to test manually:

```
[ ] Stop Redis while job search is running.
[ ] Stop Notification API after an application is submitted.
[ ] Introduce database latency.
[ ] Send the same Idempotency-Key concurrently.
[ ] Reuse a revoked refresh token.
[ ] Restart a service during outbox publishing.
[ ] Deploy two service instances and verify shared rate limits.
```

### 27.14 Render deployment additions for distributed features

Add these production dependencies to the deployment plan:

| Dependency | Purpose |
| --- | --- |
| Managed Redis-compatible service | Distributed rate limits, cache, locks |
| Message broker | Events and asynchronous communication |
| Managed telemetry backend | Traces, metrics, and logs |
| Render background worker | Outbox publishing and retry processing |

If a native Render Redis-compatible option is unavailable for your account or region, use a managed Redis-compatible provider such as Upstash and store its connection string only in Render secrets. Keep the provider behind an abstraction so the application is not coupled to one vendor.

Additional environment variables:

```
Redis__ConnectionString=<managed-redis-connection-string>
Messaging__BrokerUrl=<managed-broker-url>
Messaging__Username=<broker-username>
Messaging__Password=<broker-password>
OpenTelemetry__Endpoint=<telemetry-endpoint>
```

### 27.15 Implementation phases for distributed features

Do not implement everything at once:

#### Phase A — secure baseline

- Local ASP.NET rate limiting

- JWT and refresh-token rotation

- ProblemDetails

- Health checks

- Structured logs

- Basic retries and timeouts

#### Phase B — shared state

- Managed Redis

- Distributed rate limiting

- Cache-aside for published jobs

- Idempotency keys

- Distributed correlation and trace IDs

#### Phase C — reliable events

- Outbox table

- Background publisher

- Message broker

- Inbox deduplication

- Notification consumer

- Retry and dead-letter handling

#### Phase D — production evidence

- OpenTelemetry traces

- Metrics dashboard

- Load tests

- Failure tests

- Architecture Decision Records

- Deployment runbook

### 27.16 Strong resume bullets after this upgrade

Use only the bullets that are genuinely implemented:

- Designed and implemented a distributed recruitment platform using independently deployed .NET services, PostgreSQL, Redis, and asynchronous domain events.

- Implemented Redis-backed distributed rate limiting across horizontally scalable API instances with separate abuse policies for login, OTP, password reset, and public search.

- Prevented duplicate business operations using database-backed idempotency keys, unique constraints, and inbox deduplication.

- Implemented the transactional outbox pattern to reliably publish application and interview events without losing messages during partial failures.

- Added timeout, retry, circuit-breaker, bulkhead, and fallback strategies for resilient service-to-service communication.

- Added OpenTelemetry distributed tracing, structured logs, health checks, and operational metrics for cross-service diagnosis.

- Deployed the React frontend to Vercel and independently deployed .NET backend services to Render with managed PostgreSQL, Redis, secrets, and CI/CD.

### 27.17 Distributed-system definition of done

- [ ] At least two backend instances can share one Redis rate limit.

- [ ] Login and OTP abuse controls work consistently across instances.

- [ ] Application submission is idempotent under concurrent retries.

- [ ] Business data and outbox event are committed atomically.

- [ ] Consumers safely process duplicate events.

- [ ] Notification failure does not roll back a successful application.

- [ ] Service-to-service calls have timeout and retry policies.

- [ ] Circuit breakers prevent cascading failures.

- [ ] Cache invalidation works after job updates.

- [ ] Trace IDs connect browser, API, database, Redis, and message operations.

- [ ] Metrics expose latency, errors, rate limits, and outbox backlog.

- [ ] Load and failure tests are documented.

- [ ] Render deployment has Redis, broker, secrets, health checks, and worker configuration documented.

This upgrade changes HireFlow from a normal CRUD portfolio project into a compact demonstration of **distributed systems, security engineering, reliability, observability, and cloud deployment**—the combination that makes the project much more compelling to senior engineers, technical interviewers, and hiring managers.
