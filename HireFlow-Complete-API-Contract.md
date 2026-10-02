# HireFlow Complete API Contract

> Implementation contract for the three databases and three ASP.NET Core services.
>
> **Identity Service** owns authentication and users. **Hiring Service** owns companies, jobs, applications, and interviews. **Notification Service** owns email templates, delivery, and event processing.

## 1. API conventions

### Base URLs

| Environment | Identity | Hiring | Notification |
|---|---|---|---|
| Local | `http://localhost:5001` | `http://localhost:5002` | `http://localhost:5003` |
| Production | `https://hireflow-identity.onrender.com` | `https://hireflow-hiring.onrender.com` | `https://hireflow-notification.onrender.com` |

All public routes use `/api/v1`.

### Required headers

```http
Content-Type: application/json
Accept: application/json
X-Correlation-ID: 9f7f4cbb-3d66-4da9-85b8-66abcc165d38
Authorization: Bearer <access-token>
```

Commands that create side effects should also accept:

```http
Idempotency-Key: 8c5c56d8-55d1-4a26-a9e0-551e4ef2aabb
```

### Success envelope

Single resource:

```json
{
  "data": {},
  "meta": {
    "traceId": "00-abc123..."
  }
}
```

Collection:

```json
{
  "data": [],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalCount": 42,
    "totalPages": 3,
    "hasNextPage": true
  },
  "meta": {
    "traceId": "00-abc123..."
  }
}
```

### Error envelope

Use RFC 9457-style `ProblemDetails` for every error:

```json
{
  "type": "https://api.hireflow.local/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "detail": "One or more fields are invalid.",
  "instance": "/api/v1/jobs",
  "traceId": "00-abc123...",
  "errors": {
    "title": ["Title is required."],
    "salaryMax": ["Salary maximum must be greater than salary minimum."]
  }
}
```

Standard statuses:

| Status | Meaning |
|---:|---|
| 200 | Successful read or update |
| 201 | Resource created |
| 202 | Accepted for asynchronous processing |
| 204 | Successful operation with no response body |
| 400 | Invalid request |
| 401 | Missing or invalid authentication |
| 403 | Authenticated but not authorized |
| 404 | Resource not found |
| 409 | Duplicate or state conflict |
| 422 | Semantically invalid business request |
| 429 | Rate limit exceeded |
| 500 | Unexpected server error |
| 503 | Dependency unavailable |

### Pagination and sorting

```text
?page=1&pageSize=20&sort=-createdAt&search=dotnet
```

Rules:

- Default `page=1`.
- Default `pageSize=20`.
- Maximum `pageSize=100`.
- Prefix sort with `-` for descending order.
- Never return unbounded collections.

---

# 2. Identity Service API

Base path: `/api/v1`

## 2.1 Authentication endpoints

### `POST /auth/register`

Permission: Anonymous. Public registration always creates a Candidate; never accept an Admin role from this endpoint.

Request:

```json
{
  "email": "candidate@example.com",
  "password": "Use-a-strong-password-123!",
  "firstName": "Asha",
  "lastName": "Sharma"
}
```

Response `201 Created`:

```json
{
  "data": {
    "userId": "8e1dfc1c-84db-4a9e-a33c-4ce8c3e4c44f",
    "email": "candidate@example.com",
    "emailVerificationRequired": true,
    "otpExpiresAtUtc": "2026-10-02T12:45:00Z"
  },
  "meta": { "traceId": "00-abc123..." }
}
```

Do not return the OTP.

### `POST /auth/verify-email`

Permission: Anonymous.

Request:

```json
{
  "email": "candidate@example.com",
  "otp": "482913"
}
```

Response `200 OK`:

```json
{
  "data": {
    "email": "candidate@example.com",
    "isEmailVerified": true,
    "message": "Email verified successfully."
  },
  "meta": { "traceId": "00-abc123..." }
}
```

### `POST /auth/resend-otp`

Permission: Anonymous. Rate limited by email hash and IP.

Request:

```json
{ "email": "candidate@example.com", "purpose": "EmailVerification" }
```

Response `202 Accepted`:

```json
{
  "data": { "message": "If the account is eligible, a new code has been sent." },
  "meta": { "traceId": "00-abc123..." }
}
```

Use the same generic response for unknown accounts.

### `POST /auth/login`

Permission: Anonymous. Rate limited by IP and account identifier.

Request:

```json
{
  "email": "candidate@example.com",
  "password": "Use-a-strong-password-123!",
  "rememberMe": true
}
```

Response `200 OK`:

```json
{
  "data": {
    "accessToken": "eyJhbGciOi...",
    "expiresIn": 900,
    "refreshToken": "opaque-random-refresh-token",
    "user": {
      "id": "8e1dfc1c-84db-4a9e-a33c-4ce8c3e4c44f",
      "email": "candidate@example.com",
      "firstName": "Asha",
      "lastName": "Sharma",
      "roles": ["Candidate"]
    }
  },
  "meta": { "traceId": "00-abc123..." }
}
```

### `POST /auth/refresh`

Permission: Anonymous with a valid refresh token.

Request:

```json
{ "refreshToken": "opaque-random-refresh-token" }
```

Response `200 OK`:

```json
{
  "data": {
    "accessToken": "eyJhbGciOi...new-token...",
    "expiresIn": 900,
    "refreshToken": "new-rotated-refresh-token"
  },
  "meta": { "traceId": "00-abc123..." }
}
```

Reuse of a revoked token returns `401` and revokes the token family.

### `POST /auth/logout`

Permission: Authenticated.

Request:

```json
{ "refreshToken": "opaque-random-refresh-token" }
```

Response `204 No Content`.

### `POST /auth/forgot-password`

Permission: Anonymous. Always return a generic response.

Request:

```json
{ "email": "candidate@example.com" }
```

Response `202 Accepted`:

```json
{
  "data": { "message": "If the account exists, password-reset instructions have been sent." },
  "meta": { "traceId": "00-abc123..." }
}
```

### `POST /auth/reset-password`

Permission: Anonymous with a valid reset OTP/token.

Request:

```json
{
  "email": "candidate@example.com",
  "otp": "193842",
  "newPassword": "Another-strong-password-456!"
}
```

Response `204 No Content`.

### `GET /auth/me`

Permission: Authenticated.

Request body: none.

Response `200 OK`:

```json
{
  "data": {
    "id": "8e1dfc1c-84db-4a9e-a33c-4ce8c3e4c44f",
    "email": "candidate@example.com",
    "firstName": "Asha",
    "lastName": "Sharma",
    "isEmailVerified": true,
    "roles": ["Candidate"]
  },
  "meta": { "traceId": "00-abc123..." }
}
```

## 2.2 Session and account endpoints

### `GET /users/me/sessions`

Permission: Authenticated. Request body: none.

Response `200 OK`:

```json
{
  "data": [
    {
      "id": "f2d69405-23e8-4d7a-9006-7f2c0f1d1123",
      "createdAtUtc": "2026-10-01T10:00:00Z",
      "lastUsedAtUtc": "2026-10-02T12:00:00Z",
      "expiresAtUtc": "2026-10-31T10:00:00Z",
      "createdByIp": "203.0.113.10",
      "current": true
    }
  ],
  "meta": { "traceId": "00-abc123..." }
}
```

### `DELETE /users/me/sessions/{sessionId}`

Permission: Authenticated. Request body: none.

Response `204 No Content`.

### `DELETE /users/me/sessions`

Permission: Authenticated. Revokes every refresh-token session.

Response `204 No Content`.

### `GET /users/me/profile`

Permission: Authenticated. Request body: none.

Response `200 OK`:

```json
{
  "data": {
    "id": "8e1dfc1c-84db-4a9e-a33c-4ce8c3e4c44f",
    "email": "candidate@example.com",
    "firstName": "Asha",
    "lastName": "Sharma",
    "phone": "+91-9000000000",
    "timezone": "Asia/Kolkata",
    "avatarUrl": null
  },
  "meta": { "traceId": "00-abc123..." }
}
```

### `PUT /users/me/profile`

Permission: Authenticated.

Request:

```json
{
  "firstName": "Asha",
  "lastName": "Sharma",
  "phone": "+91-9000000000",
  "timezone": "Asia/Kolkata"
}
```

Response `200 OK`: same profile object as `GET /users/me/profile`.

### `PUT /users/me/password`

Permission: Authenticated.

Request:

```json
{
  "currentPassword": "Use-a-strong-password-123!",
  "newPassword": "New-strong-password-789!"
}
```

Response `204 No Content`. Revoke other sessions after a successful password change.

## 2.3 Admin user and role endpoints

All endpoints in this section require `Admin`.

### `GET /users`

Query: `?page=1&pageSize=20&search=asha&role=Candidate&status=Active`

Request body: none.

Response `200 OK`:

```json
{
  "data": [
    {
      "id": "8e1dfc1c-84db-4a9e-a33c-4ce8c3e4c44f",
      "email": "candidate@example.com",
      "name": "Asha Sharma",
      "roles": ["Candidate"],
      "isActive": true,
      "isEmailVerified": true,
      "createdAtUtc": "2026-10-02T12:00:00Z"
    }
  ],
  "pagination": { "page": 1, "pageSize": 20, "totalCount": 1, "totalPages": 1, "hasNextPage": false },
  "meta": { "traceId": "00-abc123..." }
}
```

### `GET /users/{userId}`

Request body: none. Response `200 OK`: one user summary object.

### `PATCH /users/{userId}/status`

Request:

```json
{ "isActive": false, "reason": "Account disabled by administrator" }
```

Response `200 OK`:

```json
{ "data": { "userId": "uuid", "isActive": false }, "meta": { "traceId": "00-abc123..." } }
```

### `PUT /users/{userId}/roles`

Request:

```json
{ "roles": ["Recruiter", "HiringManager"] }
```

Response `200 OK`:

```json
{ "data": { "userId": "uuid", "roles": ["Recruiter", "HiringManager"] }, "meta": { "traceId": "00-abc123..." } }
```

### `GET /roles`

Request body: none. Response `200 OK`:

```json
{
  "data": [
    { "id": "uuid", "name": "Admin", "description": "System administrator" },
    { "id": "uuid", "name": "Recruiter", "description": "Manages jobs and candidates" },
    { "id": "uuid", "name": "HiringManager", "description": "Reviews candidates and interviews" },
    { "id": "uuid", "name": "Candidate", "description": "Applies for jobs" }
  ],
  "meta": { "traceId": "00-abc123..." }
}
```

---

# 3. Hiring Service API

Base path: `/api/v1`

## 3.1 Company endpoints

### `POST /companies`

Permission: Admin or Recruiter.

Request:

```json
{
  "name": "HireFlow Technologies",
  "slug": "hireflow-technologies",
  "websiteUrl": "https://hireflow.example.com",
  "description": "Cloud software company",
  "industry": "Software",
  "location": "Bengaluru, India"
}
```

Response `201 Created`:

```json
{
  "data": {
    "id": "ab1d1ff0-4e29-4d4b-b9a1-7a385e9ecb62",
    "name": "HireFlow Technologies",
    "slug": "hireflow-technologies",
    "websiteUrl": "https://hireflow.example.com",
    "createdAtUtc": "2026-10-02T12:00:00Z"
  },
  "meta": { "traceId": "00-abc123..." }
}
```

### `GET /companies`

Query: `?page=1&pageSize=20&search=hireflow`

Request body: none. Response: paginated company summaries.

### `GET /companies/{companyId}`

Request body: none. Response: one company object.

### `PUT /companies/{companyId}`

Request:

```json
{
  "name": "HireFlow Technologies Pvt Ltd",
  "websiteUrl": "https://hireflow.example.com",
  "description": "Updated company description",
  "industry": "Software",
  "location": "Bengaluru, India"
}
```

Response `200 OK`: updated company object.

### `DELETE /companies/{companyId}`

Permission: Admin. Response `204 No Content`, or `409` when active jobs exist.

## 3.2 Job endpoints

### `POST /jobs`

Permission: Recruiter, HiringManager.

Request:

```json
{
  "companyId": "ab1d1ff0-4e29-4d4b-b9a1-7a385e9ecb62",
  "title": "Senior .NET Backend Engineer",
  "description": "Build secure distributed services using .NET and PostgreSQL.",
  "location": "Bengaluru or Remote",
  "employmentType": "FullTime",
  "workMode": "Hybrid",
  "experienceMinYears": 4,
  "experienceMaxYears": 8,
  "salaryMin": 1800000,
  "salaryMax": 3000000,
  "currency": "INR",
  "skills": ["C#", ".NET", "PostgreSQL", "Redis", "Docker"],
  "closingAtUtc": "2026-12-31T18:30:00Z"
}
```

Response `201 Created`:

```json
{
  "data": {
    "id": "e9b1e36d-8b49-49fd-a8fd-9edb8ad2f68e",
    "companyId": "ab1d1ff0-4e29-4d4b-b9a1-7a385e9ecb62",
    "title": "Senior .NET Backend Engineer",
    "slug": "senior-dotnet-backend-engineer-e9b1e36d",
    "status": "Draft",
    "createdByUserId": "uuid",
    "createdAtUtc": "2026-10-02T12:00:00Z",
    "updatedAtUtc": "2026-10-02T12:00:00Z"
  },
  "meta": { "traceId": "00-abc123..." }
}
```

### `GET /jobs`

Permission: Anonymous for published jobs; authenticated users can use private filters.

Query:

```text
/jobs?search=dotnet&status=Published&location=Remote&workMode=Remote&employmentType=FullTime&minExperience=3&page=1&pageSize=20&sort=-publishedAt
```

Request body: none. Response: paginated job cards:

```json
{
  "data": [
    {
      "id": "e9b1e36d-8b49-49fd-a8fd-9edb8ad2f68e",
      "title": "Senior .NET Backend Engineer",
      "company": { "id": "uuid", "name": "HireFlow Technologies", "slug": "hireflow-technologies" },
      "location": "Bengaluru or Remote",
      "workMode": "Hybrid",
      "employmentType": "FullTime",
      "skills": ["C#", ".NET", "PostgreSQL", "Redis", "Docker"],
      "status": "Published",
      "publishedAtUtc": "2026-10-02T12:30:00Z"
    }
  ],
  "pagination": { "page": 1, "pageSize": 20, "totalCount": 1, "totalPages": 1, "hasNextPage": false },
  "meta": { "traceId": "00-abc123...", "cache": "HIT" }
}
```

### `GET /jobs/{jobId}`

Request body: none. Response `200 OK`: full job detail including description, company, skills, status, dates, and `canApply` for the authenticated candidate.

### `PUT /jobs/{jobId}`

Permission: Job owner, Recruiter, or Admin. Request body: same fields as create, excluding immutable fields. Response: updated job.

### `PATCH /jobs/{jobId}/publish`

Permission: Recruiter, HiringManager, or Admin.

Request:

```json
{ "publish": true }
```

Response `200 OK`:

```json
{
  "data": { "jobId": "uuid", "status": "Published", "publishedAtUtc": "2026-10-02T12:30:00Z" },
  "meta": { "traceId": "00-abc123..." }
}
```

### `PATCH /jobs/{jobId}/close`

Permission: Recruiter, HiringManager, or Admin.

Request:

```json
{ "reason": "Position filled" }
```

Response `200 OK`:

```json
{ "data": { "jobId": "uuid", "status": "Closed", "closedAtUtc": "2026-10-10T10:00:00Z" }, "meta": { "traceId": "00-abc123..." } }
```

### `DELETE /jobs/{jobId}`

Permission: Owner or Admin. Prefer soft deletion. Response `204 No Content`.

### `GET /jobs/{jobId}/statistics`

Permission: Recruiter, HiringManager, or Admin.

Request body: none.

Response `200 OK`:

```json
{
  "data": {
    "jobId": "uuid",
    "viewCount": 1240,
    "applicationCount": 87,
    "submittedCount": 42,
    "shortlistedCount": 18,
    "interviewCount": 9,
    "offeredCount": 2,
    "hiredCount": 1
  },
  "meta": { "traceId": "00-abc123..." }
}
```

## 3.3 Application endpoints

### `POST /jobs/{jobId}/applications`

Permission: Candidate. Requires `Idempotency-Key`.

Request:

```json
{
  "resumeUrl": "https://storage.example.com/resumes/asha-sharma.pdf",
  "coverNote": "I have five years of experience building .NET APIs.",
  "source": "Direct"
}
```

Response `201 Created`:

```json
{
  "data": {
    "id": "2e54c703-d7df-4f47-8c6d-2db2e6c68f55",
    "jobId": "e9b1e36d-8b49-49fd-a8fd-9edb8ad2f68e",
    "candidateUserId": "uuid",
    "status": "Submitted",
    "appliedAtUtc": "2026-10-02T13:00:00Z",
    "timeline": [
      { "status": "Submitted", "changedAtUtc": "2026-10-02T13:00:00Z" }
    ]
  },
  "meta": { "traceId": "00-abc123...", "idempotent": false }
}
```

If the same idempotency key is retried, return the original response with `200 OK` and `idempotent: true`. A second application to the same job returns `409 Conflict`.

### `GET /me/applications`

Permission: Candidate.

Query: `?status=Submitted&page=1&pageSize=20&sort=-appliedAt`

Request body: none. Response: paginated application summaries with job title, company, status, and dates.

### `GET /me/applications/{applicationId}`

Permission: Candidate who owns the application.

Request body: none. Response `200 OK`:

```json
{
  "data": {
    "id": "2e54c703-d7df-4f47-8c6d-2db2e6c68f55",
    "job": { "id": "uuid", "title": "Senior .NET Backend Engineer", "companyName": "HireFlow Technologies" },
    "status": "Shortlisted",
    "coverNote": "I have five years of experience building .NET APIs.",
    "appliedAtUtc": "2026-10-02T13:00:00Z",
    "timeline": [
      { "fromStatus": null, "toStatus": "Submitted", "changedAtUtc": "2026-10-02T13:00:00Z" },
      { "fromStatus": "Submitted", "toStatus": "Shortlisted", "changedAtUtc": "2026-10-03T09:00:00Z" }
    ]
  },
  "meta": { "traceId": "00-abc123..." }
}
```

### `POST /applications/{applicationId}/withdraw`

Permission: Candidate owner.

Request:

```json
{ "reason": "Accepted another offer" }
```

Response `200 OK`:

```json
{ "data": { "applicationId": "uuid", "status": "Withdrawn" }, "meta": { "traceId": "00-abc123..." } }
```

### `GET /jobs/{jobId}/applications`

Permission: Recruiter, HiringManager, or Admin with access to the job.

Query: `?status=Shortlisted&search=asha&page=1&pageSize=20&sort=-appliedAt`

Request body: none. Response: paginated candidate application summaries.

### `GET /applications/{applicationId}`

Permission: Authorized recruiter, hiring manager, admin, or candidate owner. Request body: none. Response: full application detail and timeline.

### `PATCH /applications/{applicationId}/status`

Permission: Recruiter, HiringManager, or Admin.

Request:

```json
{
  "status": "Shortlisted",
  "comment": "Strong distributed-systems experience."
}
```

Response `200 OK`:

```json
{
  "data": {
    "applicationId": "uuid",
    "previousStatus": "UnderReview",
    "status": "Shortlisted",
    "changedByUserId": "uuid",
    "changedAtUtc": "2026-10-03T09:00:00Z"
  },
  "meta": { "traceId": "00-abc123..." }
}
```

Invalid transitions return `409 Conflict`.

### `GET /applications/{applicationId}/history`

Permission: Authorized participant. Request body: none. Response: ordered status-history array.

## 3.4 Interview endpoints

### `POST /applications/{applicationId}/interviews`

Permission: Recruiter or HiringManager. Requires `Idempotency-Key`.

Request:

```json
{
  "startsAtUtc": "2026-10-08T09:30:00Z",
  "endsAtUtc": "2026-10-08T10:30:00Z",
  "timezone": "Asia/Kolkata",
  "meetingUrl": "https://meet.example.com/hireflow-123",
  "interviewType": "Technical",
  "panelistUserIds": ["uuid-1", "uuid-2"],
  "notes": "Focus on API design and reliability."
}
```

Response `201 Created`:

```json
{
  "data": {
    "id": "b72c87f1-eec7-49ce-a47b-65a9c6a5f2a4",
    "applicationId": "uuid",
    "startsAtUtc": "2026-10-08T09:30:00Z",
    "endsAtUtc": "2026-10-08T10:30:00Z",
    "status": "Scheduled",
    "meetingUrl": "https://meet.example.com/hireflow-123"
  },
  "meta": { "traceId": "00-abc123...", "idempotent": false }
}
```

### `GET /me/interviews`

Permission: Candidate. Query: `?fromUtc=2026-10-01T00:00:00Z&toUtc=2026-10-31T23:59:59Z&page=1&pageSize=20`.

Request body: none. Response: paginated interview summaries.

### `GET /interviews`

Permission: Recruiter, HiringManager, or Admin. Query: `?status=Scheduled&fromUtc=...&toUtc=...&page=1&pageSize=20`.

Request body: none. Response: paginated interview summaries.

### `GET /interviews/{interviewId}`

Permission: Authorized participant. Request body: none. Response: full interview detail.

### `PATCH /interviews/{interviewId}`

Permission: Interview organizer.

Request:

```json
{
  "startsAtUtc": "2026-10-08T10:00:00Z",
  "endsAtUtc": "2026-10-08T11:00:00Z",
  "meetingUrl": "https://meet.example.com/hireflow-456",
  "notes": "Updated schedule."
}
```

Response `200 OK`: updated interview object. Publish an `InterviewRescheduled` event.

### `PATCH /interviews/{interviewId}/status`

Request:

```json
{ "status": "Completed", "notes": "Interview completed successfully." }
```

Allowed values: `Scheduled`, `Rescheduled`, `Cancelled`, `Completed`, `NoShow`.

Response `200 OK`:

```json
{ "data": { "interviewId": "uuid", "status": "Completed" }, "meta": { "traceId": "00-abc123..." } }
```

### `DELETE /interviews/{interviewId}`

Permission: Organizer. Prefer status `Cancelled` over physical deletion. Response `204 No Content`.

## 3.5 Hiring dashboard and audit endpoints

### `GET /dashboard/summary`

Permission: Recruiter, HiringManager, or Admin.

Query: `?fromUtc=2026-10-01T00:00:00Z&toUtc=2026-10-31T23:59:59Z`

Request body: none. Response `200 OK`:

```json
{
  "data": {
    "activeJobs": 12,
    "newApplications": 87,
    "shortlistedApplications": 18,
    "scheduledInterviews": 9,
    "offersMade": 2,
    "hiredCandidates": 1,
    "applicationsByStatus": {
      "Submitted": 42,
      "UnderReview": 20,
      "Shortlisted": 18,
      "InterviewScheduled": 5,
      "Rejected": 2
    }
  },
  "meta": { "traceId": "00-abc123..." }
}
```

### `GET /audit-logs`

Permission: Admin; optionally scoped Recruiter access.

Query: `?entityType=Application&action=StatusChanged&actorUserId=uuid&fromUtc=...&toUtc=...&page=1&pageSize=50`.

Request body: none. Response: paginated audit records. Never expose secrets or raw tokens.

### `GET /audit-logs/{auditId}`

Permission: Admin. Request body: none. Response: one audit record with masked old and new values.

---

# 4. Notification Service API

The Notification Service should not be publicly writable from the browser. Prefer internal service authentication or broker events. Expose only safe operational endpoints publicly.

## 4.1 Internal email endpoints

### `POST /internal/notifications/email`

Permission: Service-to-service authentication. Prefer using the outbox/event path for production.

Request:

```json
{
  "eventId": "c4bd6d14-3d25-48aa-a6a6-22c98679bd07",
  "templateKey": "ApplicationSubmitted",
  "recipientEmail": "candidate@example.com",
  "payload": {
    "candidateName": "Asha Sharma",
    "jobTitle": "Senior .NET Backend Engineer",
    "companyName": "HireFlow Technologies"
  },
  "idempotencyKey": "event-c4bd6d14"
}
```

Response `202 Accepted`:

```json
{
  "data": {
    "notificationId": "2c2d3d20-8c20-4f0d-bcb9-4217ca8ce601",
    "status": "Queued"
  },
  "meta": { "traceId": "00-abc123..." }
}
```

### `POST /internal/notifications/otp`

Permission: Identity Service only.

Request:

```json
{
  "eventId": "uuid",
  "recipientEmail": "candidate@example.com",
  "purpose": "EmailVerification",
  "otp": "482913",
  "expiresAtUtc": "2026-10-02T12:45:00Z"
}
```

Response `202 Accepted`:

```json
{ "data": { "notificationId": "uuid", "status": "Queued" }, "meta": { "traceId": "00-abc123..." } }
```

Never log or persist the OTP in plaintext. The notification provider needs the value only long enough to render and send the message.

## 4.2 Template management endpoints

Admin-only.

### `POST /templates`

Request:

```json
{
  "templateKey": "ApplicationSubmitted",
  "subjectTemplate": "Your application for {{jobTitle}} was received",
  "bodyTemplate": "Hello {{candidateName}}, your application was received.",
  "locale": "en-IN"
}
```

Response `201 Created`: template object with ID, key, version, and timestamps.

### `GET /templates`

Query: `?page=1&pageSize=20&search=application&activeOnly=true`. Request body: none. Response: paginated templates.

### `GET /templates/{templateId}`

Request body: none. Response: one template object.

### `PUT /templates/{templateId}`

Request:

```json
{
  "subjectTemplate": "Application received for {{jobTitle}}",
  "bodyTemplate": "Hello {{candidateName}}, we received your application.",
  "isActive": true
}
```

Response `200 OK`: updated template object. Create a version rather than overwriting production history.

### `DELETE /templates/{templateId}`

Permission: Admin. Prefer deactivation. Response `204 No Content`.

## 4.3 Delivery and operations endpoints

### `GET /notifications/{notificationId}`

Permission: Admin or owning service. Request body: none.

Response `200 OK`:

```json
{
  "data": {
    "id": "uuid",
    "templateKey": "ApplicationSubmitted",
    "recipientEmailMasked": "c***@example.com",
    "status": "Sent",
    "attemptCount": 1,
    "createdAtUtc": "2026-10-02T13:00:00Z",
    "sentAtUtc": "2026-10-02T13:00:03Z"
  },
  "meta": { "traceId": "00-abc123..." }
}
```

### `GET /notifications`

Permission: Admin. Query: `?status=Failed&templateKey=ApplicationSubmitted&page=1&pageSize=50`.

Request body: none. Response: paginated delivery summaries with masked email addresses.

### `POST /notifications/{notificationId}/retry`

Permission: Admin or worker.

Request:

```json
{ "reason": "Provider recovered" }
```

Response `202 Accepted`:

```json
{ "data": { "notificationId": "uuid", "status": "Queued" }, "meta": { "traceId": "00-abc123..." } }
```

### `GET /internal/outbox`

Permission: Internal worker only. Query: `?status=Pending&page=1&pageSize=100`. Response: pending event summaries. Do not expose event payloads publicly.

### `POST /internal/outbox/{messageId}/publish`

Permission: Internal worker only. Request body: none. Response `204 No Content` after successful publish.

---

# 5. Cross-service operational endpoints

Implement these in every service. Do not expose detailed dependency information on the public internet.

### `GET /health/live`

No authentication. Returns `200` when the process is running:

```json
{ "status": "Healthy", "service": "Hiring", "version": "1.0.0" }
```

### `GET /health/ready`

No authentication, but expose only a safe summary:

```json
{ "status": "Healthy", "service": "Hiring", "dependencies": { "database": "Healthy", "redis": "Healthy" } }
```

Return `503` when required dependencies are unavailable.

### `GET /version`

No authentication:

```json
{
  "data": {
    "service": "Hiring",
    "version": "1.0.0",
    "commit": "abc1234",
    "buildTimeUtc": "2026-10-02T10:00:00Z"
  }
}
```

### `GET /metrics`

Protect this endpoint with internal authentication or network rules. Expose Prometheus/OpenTelemetry metrics, not business PII.

---

# 6. Distributed-system behavior required by the API

## Rate limits

| Endpoint group | Limit example | Key |
|---|---:|---|
| Login | 5 failures / 15 minutes | IP + account hash |
| Verify OTP | 5 / 10 minutes | User + IP |
| Resend OTP | 3 / 15 minutes | User + IP |
| Forgot password | 3 / hour | Email hash + IP |
| Public jobs | 120 / minute | IP |
| Application submit | 10 / hour | Candidate ID |
| Admin writes | 60 / minute | User ID |

Return `429` with `Retry-After`, `X-RateLimit-Limit`, and `X-RateLimit-Remaining`.

## Idempotency

Require `Idempotency-Key` on:

- `POST /jobs/{jobId}/applications`
- `POST /applications/{applicationId}/interviews`
- Internal notification submission
- Any future payment-like or external side-effect operation

Return the original response for a safe retry. Return `409` when the same key is used with a different request body.

## Events

Publish these domain events through the transactional outbox:

```text
UserRegistered
EmailVerificationRequested
PasswordResetRequested
ApplicationSubmitted
ApplicationStatusChanged
InterviewScheduled
InterviewRescheduled
InterviewCancelled
JobPublished
JobClosed
```

Each event should include:

```json
{
  "eventId": "uuid",
  "eventType": "ApplicationSubmitted",
  "occurredAtUtc": "2026-10-02T13:00:00Z",
  "producer": "Hiring",
  "correlationId": "uuid",
  "causationId": "uuid",
  "version": 1,
  "data": {}
}
```

## Concurrency

Use optimistic concurrency for jobs, applications, and interviews. Accept an `If-Match` ETag or a `rowVersion` value on updates:

```json
{ "rowVersion": 7, "status": "Shortlisted", "comment": "Strong fit" }
```

Return `409 Conflict` when the record changed since it was read.

## Caching

Cache only safe read-heavy data:

- Published job details
- Public job search pages
- Company summaries

Return `ETag` and support:

```http
If-None-Match: "job-e9b1e36d-v4"
```

Return `304 Not Modified` when appropriate. Invalidate cache after job edits, publishing, and closing.

---

# 7. Minimum API implementation order

Build and test in this order:

1. `POST /auth/register`
2. `POST /auth/verify-email`
3. `POST /auth/login`
4. `POST /auth/refresh`
5. `POST /auth/logout`
6. `GET /auth/me`
7. `POST /companies`
8. `POST /jobs`
9. `GET /jobs`
10. `GET /jobs/{jobId}`
11. `PATCH /jobs/{jobId}/publish`
12. `POST /jobs/{jobId}/applications`
13. `GET /me/applications`
14. `GET /jobs/{jobId}/applications`
15. `PATCH /applications/{applicationId}/status`
16. `POST /applications/{applicationId}/interviews`
17. `GET /me/interviews`
18. `POST /internal/notifications/email`
19. `GET /health/live`
20. `GET /health/ready`

Then add password reset, profile/session management, admin APIs, dashboard statistics, audit APIs, event publishing, caching, rate limiting, and observability.

# 8. API acceptance checklist

- [ ] Every endpoint has an authorization rule.
- [ ] Every request has validation.
- [ ] Every response has a documented status code.
- [ ] Every error uses ProblemDetails.
- [ ] Every response includes or can be correlated with a trace ID.
- [ ] Every collection is paginated.
- [ ] Every side-effect command has idempotency or a unique database constraint.
- [ ] Sensitive endpoints have distributed rate limits.
- [ ] Cross-service calls have timeouts and resilience policies.
- [ ] Business transactions publish events through an outbox.
- [ ] Consumers deduplicate events through an inbox.
- [ ] Jobs, applications, and interviews protect against concurrent updates.
- [ ] Health and readiness checks cover required dependencies.
- [ ] OpenAPI accurately describes request and response schemas.
- [ ] Integration tests cover the complete candidate and recruiter journeys.
