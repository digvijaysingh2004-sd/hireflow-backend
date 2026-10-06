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

---

### 1. Database Configuration
You can run PostgreSQL either through Docker or locally:

#### Option A: Local PostgreSQL (Recommended if already installed)
1. Ensure your local PostgreSQL service is running on port `5432`.
2. Credentials & Connection Strings:
   - **Identity DB**: `hireflow_identity`
   - **Hiring DB**: `hireflow_hiring`
   - **Notification DB**: `hireflow_notification`

#### Option B: Docker Compose
Start PostgreSQL and Mailpit with:
```bash
docker compose up -d postgres mailpit
```

---

### 2. Database Migrations

#### A. Add a New Migration
To create a new migration (replace `InitialCreate` with your migration name), run from the solution root:

**Identity Service:**
```bash
dotnet ef migrations add InitialCreate --project src/Services/Identity/HireFlow.Identity.Infrastructure --startup-project src/Services/Identity/HireFlow.Identity.Api --output-dir Persistence/Migrations
```

**Hiring Service:**
```bash
dotnet ef migrations add InitialCreate --project src/Services/Hiring/HireFlow.Hiring.Infrastructure --startup-project src/Services/Hiring/HireFlow.Hiring.Api --output-dir Persistence/Migrations
```

**Notification Service:**
```bash
dotnet ef migrations add InitialCreate --project src/Services/Notification/HireFlow.Notification.Infrastructure --startup-project src/Services/Notification/HireFlow.Notification.Api
```

##### One-Liner to Add Migrations to All Services:

**PowerShell (Windows):**
```powershell
dotnet ef migrations add InitialCreate --project src/Services/Identity/HireFlow.Identity.Infrastructure --startup-project src/Services/Identity/HireFlow.Identity.Api --output-dir Persistence/Migrations; dotnet ef migrations add InitialCreate --project src/Services/Hiring/HireFlow.Hiring.Infrastructure --startup-project src/Services/Hiring/HireFlow.Hiring.Api --output-dir Persistence/Migrations; dotnet ef migrations add InitialCreate --project src/Services/Notification/HireFlow.Notification.Infrastructure --startup-project src/Services/Notification/HireFlow.Notification.Api
```

**Bash / Git Bash (Linux / macOS):**
```bash
dotnet ef migrations add InitialCreate --project src/Services/Identity/HireFlow.Identity.Infrastructure --startup-project src/Services/Identity/HireFlow.Identity.Api --output-dir Persistence/Migrations && dotnet ef migrations add InitialCreate --project src/Services/Hiring/HireFlow.Hiring.Infrastructure --startup-project src/Services/Hiring/HireFlow.Hiring.Api --output-dir Persistence/Migrations && dotnet ef migrations add InitialCreate --project src/Services/Notification/HireFlow.Notification.Infrastructure --startup-project src/Services/Notification/HireFlow.Notification.Api
```

---

#### B. Apply Database Migrations (All Services)

Run migrations for all microservices (`Identity`, `Hiring`, and `Notification`) from the repository root using any of the following one-liners:

##### PowerShell (Windows):
```powershell
"Identity", "Hiring", "Notification" | ForEach-Object { dotnet ef database update --project "src/Services/$_/HireFlow.$_.Infrastructure" --startup-project "src/Services/$_/HireFlow.$_.Api" }
```

##### Bash / Git Bash (Linux / macOS):
```bash
for s in Identity Hiring Notification; do dotnet ef database update --project "src/Services/$s/HireFlow.$s.Infrastructure" --startup-project "src/Services/$s/HireFlow.$s.Api"; done
```

##### Direct Chained Command (CMD / PowerShell):
```powershell
dotnet ef database update --project src/Services/Identity/HireFlow.Identity.Infrastructure --startup-project src/Services/Identity/HireFlow.Identity.Api; dotnet ef database update --project src/Services/Hiring/HireFlow.Hiring.Infrastructure --startup-project src/Services/Hiring/HireFlow.Hiring.Api; dotnet ef database update --project src/Services/Notification/HireFlow.Notification.Infrastructure --startup-project src/Services/Notification/HireFlow.Notification.Api
```

---

### 3. Run Microservices

#### Run Individual Microservices

**Identity API (Port 5218):**
```bash
dotnet run --project src/Services/Identity/HireFlow.Identity.Api --launch-profile http
```

**Hiring API (Port 5104):**
```bash
dotnet run --project src/Services/Hiring/HireFlow.Hiring.Api --launch-profile http
```

**Notification API (Port 5289):**
```bash
dotnet run --project src/Services/Notification/HireFlow.Notification.Api --launch-profile http
```

#### Run All Services at Once

**Bash / Git Bash:**
```bash
dotnet run --project src/Services/Identity/HireFlow.Identity.Api --launch-profile http & dotnet run --project src/Services/Hiring/HireFlow.Hiring.Api --launch-profile http & dotnet run --project src/Services/Notification/HireFlow.Notification.Api --launch-profile http &
```

**PowerShell (Separate Windows):**
```powershell
Start-Process dotnet -ArgumentList "run --project src/Services/Identity/HireFlow.Identity.Api --launch-profile http" ; Start-Process dotnet -ArgumentList "run --project src/Services/Hiring/HireFlow.Hiring.Api --launch-profile http" ; Start-Process dotnet -ArgumentList "run --project src/Services/Notification/HireFlow.Notification.Api --launch-profile http"
```

**CMD (Separate Windows):**
```cmd
start "" dotnet run --project src/Services/Identity/HireFlow.Identity.Api --launch-profile http & start "" dotnet run --project src/Services/Hiring/HireFlow.Hiring.Api --launch-profile http & start "" dotnet run --project src/Services/Notification/HireFlow.Notification.Api --launch-profile http
```

---

## 🌐 Swagger UI Documentation Links

When running locally, Swagger UI is available at:

- **Identity Service Swagger**: `http://localhost:5218/swagger`
- **Hiring Service Swagger**: `http://localhost:5104/swagger`
- **Notification Service Swagger**: `http://localhost:5289/swagger`

> Note: All Swagger UIs include a top-right **"Select a definition"** dropdown allowing you to switch between microservice documentation on a single page.

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

# Run test suites
dotnet test HireFlow.sln
```
