# Task Management API

A backend API where users register, log in and manage their own tasks, built with ASP.NET Core 9, SQL Server and Redis.

It was written for the ".NET Backend Developer Technical Task" and covers every required item plus all six optional bonus items.

| Requirement                                 | Where it is                                                        |
| ------------------------------------------- | ------------------------------------------------------------------ |
| DDD-style project structure                 | Four projects: Domain, Application, Infrastructure, Api            |
| RESTful API with Swagger and JWT support    | `backend/TaskManagement/src/TaskManagement.Api`                    |
| Register, login, profile                    | `AuthController`, `UsersController`                                |
| Seeded admin and admin-only user management | `DatabaseInitializer`, `AdminUsersController`                      |
| Task module with ownership rules            | `TasksController`, `TaskService`                                   |
| Redis cache on Get Task by ID               | `TaskService.GetByIdAsync`, `RedisTaskCache`                       |
| Background processing                       | `TaskProcessingQueue`, `TaskProcessingWorker`, `TaskProcessor`     |
| Business logic (sorting, duplicate rule)    | `TaskRepository.GetByUserAsync`, `TaskService.CreateAsync`         |
| Bonus: global exception handling            | `GlobalExceptionHandler`                                           |
| Bonus: Docker support                       | `Dockerfile`, `docker-compose.yml`                                 |
| Bonus: unit tests                           | `backend/TaskManagement/tests/TaskManagement.UnitTests` (63 tests) |
| Bonus: clean logging                        | Serilog, configured in `appsettings.json`                          |
| Bonus: refresh tokens                       | `AuthService.RefreshAsync`                                         |
| Bonus: soft delete for users                | `User.Delete`, `UserRepository`                                    |

## Seeded admin credentials

| Email               | Password    |
| ------------------- | ----------- |
| `admin@example.com` | `Admin@123` |

The admin is created the first time the application starts. The values come from the `AdminSeed` section of `appsettings.json`.

## Running the project

There are two ways to run it. Option A uses a SQL Server and Redis you already have. Option B needs only Docker.

### Option A: run locally

**Prerequisites**

- .NET 9 SDK
- SQL Server (any edition; the default connection string targets the default local instance with Windows Authentication)
- Redis on `localhost:6379`. The simplest way to get it is Docker:

```bash
docker run -d --name taskmgmt-redis -p 6379:6379 redis:7-alpine
```

**Steps**

1. Clone the repository.

```bash
git clone https://github.com/00Eslam00/TaskManagement.git
```

2. If your SQL Server is not the default local instance, change `ConnectionStrings:DefaultConnection` in `backend/TaskManagement/src/TaskManagement.Api/appsettings.json`. For example, use `Server=.\SQLEXPRESS` for SQL Server Express or `Server=(localdb)\MSSQLLocalDB` for LocalDB.

3. Run the API from the repository root.

```bash
dotnet run --project backend/TaskManagement/src/TaskManagement.Api
```

4. Open `http://localhost:5034/swagger`.

There is no manual database step. On startup the application creates the `TaskManagementDb` database, applies the EF Core migrations and seeds the admin user.

### Running the frontend

The React app lives in `frontend/`. It needs Node.js 20 or later and the API running (Option A).

1. Create the environment file. The default value points to the API started with `dotnet run`.

```bash
cp frontend/.env.example frontend/.env
```

2. Install the packages and start the dev server.

```bash
cd frontend
npm install
npm run dev
```

3. Open `http://localhost:5173`.

The dev server always uses port 5173 because that is the only origin the API allows (`Cors:AllowedOrigins`).

| Variable       | Default                 | Purpose                                           |
| -------------- | ----------------------- | ------------------------------------------------- |
| `VITE_API_URL` | `http://localhost:5034` | Base address of the API, without a trailing slash |

### Option B: run everything in Docker

**Prerequisite:** Docker Desktop.

```bash
docker compose up --build
```

Then open `http://localhost:8080/swagger`.

This starts three containers: the API, SQL Server 2022 and Redis. Things to know:

- The SQL Server image is about 1.5 GB, and the compose file sets `ACCEPT_EULA=Y`, which means you accept Microsoft's SQL Server licence terms.
- The API connects with a SQL login (`sa`), because a container cannot use Windows Authentication. The password is a development-only default; set `MSSQL_SA_PASSWORD` in a `.env` file to use your own.
- SQL Server and Redis do not publish ports to the host, so they cannot clash with instances already running on your machine.

To stop the stack and delete its database volume:

```bash
docker compose down -v
```

### Running the tests

From the repository root:

```bash
dotnet test backend/TaskManagement/tests/TaskManagement.UnitTests
```

## Trying it in Swagger

1. Call `POST /api/auth/login` with the admin credentials above.
2. Copy the `accessToken` from the response, click **Authorize** at the top of the page, and paste it (without the word `Bearer`).
3. Call any protected endpoint. To test as a regular user, register one with `POST /api/auth/register`, log in as that user, and authorize with the new token.

## API endpoints

| Method | Endpoint                 | Access        | Description                                        |
| ------ | ------------------------ | ------------- | -------------------------------------------------- |
| POST   | `/api/auth/register`     | Anonymous     | Create an account (always with the `User` role)    |
| POST   | `/api/auth/login`        | Anonymous     | Returns an access token and a refresh token        |
| POST   | `/api/auth/refresh`      | Anonymous     | Exchanges a refresh token for a new pair of tokens |
| POST   | `/api/auth/logout`       | Authenticated | Revokes all refresh tokens of the caller           |
| GET    | `/api/users/me`          | Authenticated | Profile of the logged-in user                      |
| GET    | `/api/admin/users`       | Admin         | List all active users                              |
| POST   | `/api/admin/users`       | Admin         | Add a user (role is optional, defaults to `User`)  |
| DELETE | `/api/admin/users/{id}`  | Admin         | Soft-delete a user                                 |
| POST   | `/api/tasks`             | Authenticated | Create a task                                      |
| GET    | `/api/tasks`             | Authenticated | List the caller's tasks, sorted                    |
| GET    | `/api/tasks/{id}`        | Authenticated | Get one of the caller's tasks (cached in Redis)    |
| PATCH  | `/api/tasks/{id}/status` | Authenticated | Change a task's status                             |

Errors are returned as RFC 7807 problem details. Status codes used: 400 (validation or business rule), 401 (not authenticated), 403 (not an admin), 404 (not found or not yours), 409 (duplicate).

## Project structure

```
backend/TaskManagement/
  TaskManagement.slnx
  src/
    TaskManagement.Domain           Entities, enums, business rules, repository interfaces
    TaskManagement.Application      Use cases (services), DTOs, interfaces for cache, queue, tokens
    TaskManagement.Infrastructure   EF Core, migrations, seeding, Redis, JWT, BCrypt, background worker
    TaskManagement.Api              Controllers, Swagger, exception handling, startup
  tests/
    TaskManagement.UnitTests        Domain, application and cache tests
```

## Architecture approach

The solution follows a DDD-style layered architecture. Dependencies point inwards only:

```
Api  ->  Application  ->  Domain
 |            ^
 v            |
Infrastructure
```

- **Domain** has no dependencies. The `User` and `TaskItem` entities have private setters and are changed only through methods that enforce the rules, such as `TaskItem.ChangeStatus` or `User.Delete`. A broken rule throws a `DomainException`. `RefreshToken` is a child of the `User` aggregate and is only reachable through it.
- **Application** contains one service per use-case area (`AuthService`, `UserService`, `TaskService`, `TaskProcessor`). It depends on interfaces only (`ITaskCache`, `ITaskProcessingQueue`, `IPasswordHasher`, `IJwtTokenGenerator`, repositories), so it knows nothing about EF Core, Redis or JWT libraries.
- **Infrastructure** implements those interfaces.
- **Api** is thin: controllers read the request, call a service and return the result.

Plain application services were chosen over CQRS/MediatR to keep the solution simple, as the task asks.

## Authentication and authorization

- **Passwords** are hashed with BCrypt and never stored or returned in plain text.
- **Login** returns a JWT access token (valid 60 minutes, signed with HMAC-SHA256) that carries the user id, email, name and role.
- **Authentication:** protected endpoints require `Authorization: Bearer {token}`. On every authenticated request the API also checks that the token's user still exists, so a deleted user is locked out immediately instead of when the token expires.
- **Authorization by role:** the admin endpoints use `[Authorize(Roles = "Admin")]`. A regular user gets 403.
- **Authorization by ownership:** every task operation checks that the task belongs to the caller. Someone else's task is reported as 404 rather than 403, so the API does not confirm which task ids exist.
- **Refresh tokens** are random 64-byte values, valid 7 days, stored only as a SHA-256 hash. Each one works once and is replaced by a new one. Presenting a used or expired refresh token revokes all of that user's refresh tokens, because it may have been stolen.

## Redis caching

`GET /api/tasks/{id}` uses the cache-aside pattern:

1. Look for the key `TaskManagement:task:{id}` in Redis.
2. On a hit, return the cached task. Ownership is still checked, because the cache is shared by all users.
3. On a miss, load the task from the database, store it in Redis for 10 minutes, and return it.

The cached entry is removed when the task's status is updated and when the background worker marks the task as processed, so the next read loads fresh data.

The log shows which path was taken: `served from cache` or `loaded from database and cached`.

If Redis is unavailable the API keeps working from the database. After a failure it skips Redis for 30 seconds so requests do not each wait for a timeout.

## Background processing

1. `POST /api/tasks` saves the task and then puts its id on an in-memory queue (`System.Threading.Channels`). The request returns immediately.
2. `TaskProcessingWorker`, a .NET `BackgroundService`, reads ids from the queue one at a time.
3. For each task it waits 5 seconds to simulate work, then sets the task's `processedAt` timestamp and removes its cache entry.

The worker does not change the task's status, so it never overrides a status set by the user.

Because the queue is in memory, the worker re-queues every task that has no `processedAt` when the application starts. A task created just before a crash or restart is therefore still processed.

## Business logic

- **Sorting:** `GET /api/tasks` returns tasks by priority (High, then Medium, then Low), then by creation date, oldest first.
- **Duplicate rule:** a user cannot create two tasks with the same title on the same day. The comparison ignores case and surrounding spaces. It is checked in `TaskService` and also enforced by a unique database index on (user, day, title), so two simultaneous requests cannot both succeed. The second one gets 409.
- **Status rule:** `Done` is a final state; a task that is `Done` cannot be changed. Setting a task to the status it already has is accepted and does nothing.
- **Admin safety:** an admin cannot delete their own account, so the system can never be left without an admin.

## Database seeding

`DatabaseInitializer` runs once at startup. It applies pending migrations and then creates the default admin only if no admin exists, so restarting the application never creates duplicates.

## Assumptions

- "Same day" in the duplicate rule means the same UTC calendar day.
- The admin follows the same task rules as everyone else and sees only their own tasks.
- Self-registration always creates a regular user. Only an admin can create another admin.
- Deleting a user is a soft delete. The user's tasks are kept, and the email can be registered again.
- "Created date" sorting is oldest first within the same priority.
- All dates are stored and returned in UTC.
- Logout revokes refresh tokens; the current access token remains valid until it expires.

## Configuration

All settings are in `backend/TaskManagement/src/TaskManagement.Api/appsettings.json` and can be overridden with environment variables (for example `Jwt__Key`).

| Section                | Purpose                                                        |
| ---------------------- | -------------------------------------------------------------- |
| `ConnectionStrings`    | SQL Server and Redis addresses                                 |
| `Jwt`                  | Issuer, audience, signing key, token lifetimes                 |
| `Cache`                | Cache expiry and the retry window after a Redis failure        |
| `Cors`                 | Browser origins allowed to call the API (the React dev server) |
| `BackgroundProcessing` | Simulated work time per task                                   |
| `AdminSeed`            | Default admin name, email and password                         |
| `Serilog`              | Log levels and outputs                                         |

The JWT signing key and the admin password in this file are development values, kept there so the project runs without any setup. In a real deployment they must come from environment variables or a secret store.

## Known limitations

- The queue is in memory and the worker runs inside the API process, one task at a time. With several API instances, a restart of one instance could process a task twice; the operation is idempotent, so the result is the same. A production system would use a durable queue.
- The guard against stale cache entries after a Redis outage is kept in memory per API instance. With several instances, an outdated entry could survive until its 10-minute expiry.
- There is no pagination on the list endpoints.

## Changes since Stage 1

| Change                                                                                                                                                                                      | Reason                                                                                                                                                                                  |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Dockerfile` and `.dockerignore` now read the .NET code from `backend/TaskManagement/`, and the README paths were updated to match.                                                         | The Stage 1 commit moved the solution into `backend/` but the Dockerfile still copied from `src/`, so `docker compose up --build` failed.                                               |
| CORS policy `Frontend` added (`CorsExtensions`, `Cors:AllowedOrigins` in `appsettings.json`). Only `http://localhost:5173` is allowed, with only the headers and methods the frontend uses. | The browser blocks calls from the React app to a different origin unless the API allows it. It runs before the exception handler so 401/409 error bodies stay readable to the frontend. |
