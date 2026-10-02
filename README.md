# HireFlow — Portfolio-Ready Recruitment Microservices Platform

HireFlow is a recruitment workflow and hiring management platform built with **.NET 10 LTS, ASP.NET Core, PostgreSQL, React, TypeScript, and Docker**. It models end-to-end recruitment lifecycle operations with clean service boundaries, role-based security, distributed resiliency, and auditability.

---

## 🏛 Architecture Overview

HireFlow follows a clean **database-per-service** microservice architecture:

- **Identity Service (`HireFlow.Identity.*`)**: User authentication, role assignment (`Admin`, `Recruiter`, `HiringManager`, `Candidate`), email OTP verification, JWT access tokens, and rotating refresh tokens.
- **Hiring Service (`HireFlow.Hiring.*`)**: Job postings, publishing lifecycle, candidate applications, status workflow, and interview scheduling.
- **Notification Service (`HireFlow.Notification.*`)**: Email templating, Mailpit integration, and async delivery attempts.
- **Shared Building Blocks (`BuildingBlocks.*` & `HireFlow.Contracts`)**: Common domain abstractions, infrastructure helpers, OpenTelemetry observability, and cross-service domain events.

---

## 🛠 Tech Stack

- **Runtime & Language**: .NET 10 LTS, C# 14
- **Framework**: ASP.NET Core Web API
- **Data Access**: Entity Framework Core 10 with Npgsql (PostgreSQL 17+)
- **Security**: JWT Bearer Tokens, Refresh-Token Rotation, ASP.NET Core Identity Password Hasher
- **Observability**: Serilog structured logging, OpenTelemetry, Health Checks
- **Containers & Local Dev**: Docker Compose, Mailpit (local SMTP mock)
- **Frontend (Upcoming)**: React 19, TypeScript, Vite, TanStack Query, Tailwind CSS

---

## 🚀 Quickstart & Local Setup

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL 17+ (installed locally or via Docker)
- Docker Desktop (optional for containerized environment)

### 1. Database Configuration
You can run PostgreSQL either through Docker or locally:

#### Option A: Local PostgreSQL (Recommended if already installed)
1. Ensure your local PostgreSQL service is running on port `5432`.
2. Configure credentials in `appsettings.Development.json` or `.env`:
   ```json
   "ConnectionStrings": {
     "IdentityDatabase": "Host=localhost;Port=5432;Database=hireflow_identity;Username=postgres;Password=your_password"
   }
   ```
3. Create the microservice databases (`hireflow_identity`, `hireflow_hiring`, `hireflow_notification`).

#### Option B: Docker Compose
Start PostgreSQL and Mailpit with:
```bash
docker compose up -d postgres mailpit
```
> Note: Docker PostgreSQL is mapped to host port `5433` by default to prevent port conflicts with any existing local PostgreSQL installation.

---

### 2. Apply Database Migrations

Apply the initial Identity migration:
```bash
dotnet ef database update --project src/Services/Identity/HireFlow.Identity.Infrastructure --startup-project src/Services/Identity/HireFlow.Identity.Api
```

---

### 3. Run the Identity Service

```bash
dotnet run --project src/Services/Identity/HireFlow.Identity.Api
```

- **API Base URL**: `http://localhost:5000` / `https://localhost:5001`
- **Swagger Documentation**: `http://localhost:5000/swagger`

---

## 🔑 Default Seeded Demo Logins

When the Identity Service starts, it automatically seeds default roles and demo user accounts:

| Email | Password | Role | Status |
| :--- | :--- | :--- | :--- |
| `admin@hireflow.local` | `Admin123!` | `Admin` | Verified & Active |
| `recruiter@hireflow.local` | `Recruiter123!` | `Recruiter` | Verified & Active |
| `manager@hireflow.local` | `Manager123!` | `HiringManager` | Verified & Active |
| `candidate@hireflow.local` | `Candidate123!` | `Candidate` | Verified & Active |

---

## 🧪 Build and Test

```bash
# Build complete solution
dotnet build HireFlow.sln

# Run test suites (when implemented)
dotnet test HireFlow.sln
```
