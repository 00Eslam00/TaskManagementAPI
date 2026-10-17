# Task Management

A task management application: users register, log in and manage their own tasks, and an admin manages users.

- **Stage 1, backend:** an ASP.NET Core 9 Web API with SQL Server, Redis, JWT authentication and a background worker. The code is in [`backend/`](backend/TaskManagement).
- **Stage 2, frontend:** a React 19 + TypeScript single-page app built with Vite, which talks to that API. The code is in [`frontend/`](frontend).

## Seeded admin credentials

| Email               | Password    |
| ------------------- | ----------- |
| `admin@example.com` | `Admin@123` |

The admin is created the first time the API starts. The values come from the `AdminSeed` section of `appsettings.json`.

## Running the backend and the frontend together

**Prerequisites**

- .NET 9 SDK
- Node.js 20 or later (with npm)
- SQL Server, any edition. The default connection string targets the default local instance with Windows Authentication.
- Redis on `localhost:6379`. The simplest way to get it is Docker.

### Option A: everything on your machine

1. Clone the repository.

```bash
git clone https://github.com/00Eslam00/TaskManagementAPI.git
cd TaskManagementAPI
```

On Windows, clone into a short folder such as `C:\src`. Some backend file paths are long, and a deeply nested folder can push them past the 260-character path limit (`git config --global core.longpaths true` also avoids it).

2. Start Redis (skip this if you already run Redis on port 6379).

```bash
docker run -d --name taskmgmt-redis -p 6379:6379 redis:7-alpine
```

1. If your SQL Server is not the default local instance, change `ConnectionStrings:DefaultConnection` in `backend/src/TaskManagement.Api/appsettings.json`. For example, use `Server=.\SQLEXPRESS` for SQL Server Express or `Server=(localdb)\MSSQLLocalDB` for LocalDB.

2. Start the API from the repository root. It listens on `http://localhost:5269`.

```bash
dotnet run --project backend/src/TaskManagement.Api
```

There is no manual database step: on startup the API creates the `TaskManagementDb` database, applies the migrations and seeds the admin user. Swagger is at `http://localhost:5269/swagger`.

5. In a second terminal, from the repository root, create the frontend's environment file. The default value already points to the API from step 4.

```bash
cp frontend/.env.example frontend/.env
```

6. Install the frontend packages and start the dev server.

```bash
cd frontend
npm install
npm run dev
```

7. Open `http://localhost:5173` and log in with the admin credentials above, or register a new user.

The dev server always uses port 5173, because that is the origin the API allows (`Cors:AllowedOrigins`). If the port is taken, Vite stops with an error instead of moving to another port.

### Option B: the API in Docker, the frontend on your machine

**Prerequisite:** Docker Desktop. Nothing else is needed for the API: SQL Server and Redis run as containers too.

1. From the repository root, start the API, SQL Server 2022 and Redis.

```bash
docker compose up --build
```

The API is published on `http://localhost:8080` (Swagger at `http://localhost:8080/swagger`).

2. Point the frontend at port 8080: copy `frontend/.env.example` to `frontend/.env` and set `VITE_API_URL=http://localhost:8080`.

3. Run the frontend as in steps 6 and 7 of Option A.

Things to know about the containers:

- The SQL Server image is about 1.5 GB, and the compose file sets `ACCEPT_EULA=Y`, which means you accept Microsoft's SQL Server licence terms.
- The API connects with a SQL login (`sa`), because a container cannot use Windows Authentication. The password is a development-only default; set `MSSQL_SA_PASSWORD` in a `.env` file next to `docker-compose.yml` to use your own.
- SQL Server and Redis do not publish ports to the host, so they cannot clash with instances already running on your machine.

To stop the stack and delete its database volume:

```bash
docker compose down -v
```

### Environment variables

**Frontend** (`frontend/.env`, template in [`frontend/.env.example`](frontend/.env.example))

| Variable       | Default                 | Purpose                                                                                                                |
| -------------- | ----------------------- | ---------------------------------------------------------------------------------------------------------------------- |
| `VITE_API_URL` | `http://localhost:5269` | Base address of the API, without a trailing slash. Read only in `src/config.ts`; no component contains an API address. |

**Backend.** Every setting lives in `backend/TaskManagement/src/TaskManagement.Api/appsettings.json` and can be overridden with an environment variable, where `__` stands for a nested key. The ones you are most likely to change:

| Variable                                     | Default                                  | Purpose                                                            |
| -------------------------------------------- | ---------------------------------------- | ------------------------------------------------------------------ |
| `ConnectionStrings__DefaultConnection`       | Local SQL Server, Windows Authentication | SQL Server database                                                |
| `ConnectionStrings__Redis`                   | `localhost:6379`                         | Redis address                                                      |
| `Cors__AllowedOrigins__0`                    | `http://localhost:5173`                  | Browser origin allowed to call the API (add `__1`, `__2` for more) |
| `Jwt__Key`                                   | Development key                          | JWT signing key. Must be replaced outside development.             |
| `Jwt__AccessTokenMinutes`                    | `60`                                     | Access token lifetime                                              |
| `BackgroundProcessing__SimulatedWorkSeconds` | `5`                                      | How long the background worker "works" on each new task            |
| `AdminSeed__Email`, `AdminSeed__Password`    | `admin@example.com`, `Admin@123`         | Seeded admin account                                               |

### Checks

```bash
dotnet test backend/tests/TaskManagement.UnitTests
```

From `frontend/`: `npm run lint` (ESLint) and `npm run build` (type check with `tsc` in strict mode, then the production build).

### Trying the API in Swagger

1. Call `POST /api/auth/login` with the admin credentials above.
2. Copy the `accessToken` from the response, click **Authorize** at the top of the page, and paste it (without the word `Bearer`).
3. Call any protected endpoint. To test as a regular user, register one with `POST /api/auth/register`, log in as that user, and authorize with the new token.

## Backend (Stage 1)

### API endpoints

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

### Project structure

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

### Architecture approach

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

### Authentication and authorization

- **Passwords** are hashed with BCrypt and never stored or returned in plain text.
- **Login** returns a JWT access token (valid 60 minutes, signed with HMAC-SHA256) that carries the user id, email, name and role.
- **Authentication:** protected endpoints require `Authorization: Bearer {token}`. On every authenticated request the API also checks that the token's user still exists, so a deleted user is locked out immediately instead of when the token expires.
- **Authorization by role:** the admin endpoints use `[Authorize(Roles = "Admin")]`. A regular user gets 403.
- **Authorization by ownership:** every task operation checks that the task belongs to the caller. Someone else's task is reported as 404 rather than 403, so the API does not confirm which task ids exist.
- **Refresh tokens** are random 64-byte values, valid 7 days, stored only as a SHA-256 hash. Each one works once and is replaced by a new one. Presenting a used or expired refresh token revokes all of that user's refresh tokens, because it may have been stolen.

### Redis caching

`GET /api/tasks/{id}` uses the cache-aside pattern:

1. Look for the key `TaskManagement:task:{id}` in Redis.
2. On a hit, return the cached task. Ownership is still checked, because the cache is shared by all users.
3. On a miss, load the task from the database, store it in Redis for 10 minutes, and return it.

The cached entry is removed when the task's status is updated and when the background worker marks the task as processed, so the next read loads fresh data.

The log shows which path was taken: `served from cache` or `loaded from database and cached`.

If Redis is unavailable the API keeps working from the database. After a failure it skips Redis for 30 seconds so requests do not each wait for a timeout.

### Background processing

1. `POST /api/tasks` saves the task and then puts its id on an in-memory queue (`System.Threading.Channels`). The request returns immediately.
2. `TaskProcessingWorker`, a .NET `BackgroundService`, reads ids from the queue one at a time.
3. For each task it waits 5 seconds to simulate work, then sets the task's `processedAt` timestamp and removes its cache entry. `processedAt` is written with one atomic `UPDATE ... WHERE Id = @id AND ProcessedAt IS NULL` (`TaskRepository.MarkProcessedAsync`), so the task is never loaded and saved back.

The worker does not change the task's status and does not check it, so it never overrides, or conflicts with, a status the user sets at the same moment.

Because the queue is in memory, the worker re-queues every task that has no `processedAt` when the application starts. A task created just before a crash or restart is therefore still processed.

### Business logic

- **Sorting:** `GET /api/tasks` returns tasks by priority (High, then Medium, then Low), then by creation date, oldest first.
- **Duplicate rule:** a user cannot create two tasks with the same title on the same day. The comparison ignores case and surrounding spaces. It is checked in `TaskService` and also enforced by a unique database index on (user, day, title), so two simultaneous requests cannot both succeed. The second one gets 409.
- **Status rule:** `Done` is a final state; a task that is `Done` cannot be changed. Setting a task to the status it already has is accepted and does nothing. `Status` is an EF Core concurrency token, so two status changes that arrive at the same time cannot both pass this check: the second one gets 409 instead of overwriting the first (added in Stage 2, see "Changes since Stage 1").
- **Admin safety:** an admin cannot delete their own account, so the system can never be left without an admin.

### Database seeding

`DatabaseInitializer` runs once at startup. It applies pending migrations and then creates the default admin only if no admin exists, so restarting the application never creates duplicates.

### Configuration

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

### Known limitations

- The queue is in memory and the worker runs inside the API process, one task at a time. With several API instances, two workers could pick up the same task after a restart; both would simulate the work, but only one `UPDATE` matches the row (`ProcessedAt IS NULL`), so `processedAt` is set once. A production system would use a durable queue.
- The guard against stale cache entries after a Redis outage is kept in memory per API instance. With several instances, an outdated entry could survive until its 10-minute expiry.
- There is no pagination on the list endpoints.

## Frontend (Stage 2)

### What it does

| Requirement                                                              | Where it is                                                                                          |
| ------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------- |
| Register, login, logout, current user in the header                      | `pages/RegisterPage`, `pages/LoginPage`, `auth/AuthProvider`, `components/AppLayout`                 |
| Protected routes, stay logged in after a refresh, 401 handling           | `auth/ProtectedRoute`, `auth/session`, `api/client`                                                  |
| Task list with badges, loading, empty and error states                   | `pages/TasksPage`, `components/TaskBadges`, `components/PageStates`                                  |
| Filter by status and search by title                                     | `components/TaskFilters`, `lib/taskFilters`                                                          |
| Create task with validation and readable server errors                   | `components/CreateTaskDialog`, `lib/errors`                                                          |
| Task details at `/tasks/:id`, status update, "Task not found"            | `pages/TaskDetailsPage`                                                                              |
| Background processing visible without a reload                           | `lib/processing`, `ProcessingBadge` in `components/TaskBadges`                                       |
| Admin area: users list, create user, delete with an in-page confirmation | `pages/AdminUsersPage`, `components/CreateUserDialog`, `components/ConfirmDialog`                    |
| Admin-only menu item and "Access denied" for normal users                | `components/AppLayout`, `auth/AdminRoute`, `pages/AccessDeniedPage`                                  |
| "Page not found"                                                         | `pages/NotFoundPage`                                                                                 |
| Desktop and mobile widths                                                | MUI breakpoints: the users table becomes cards, dialogs go full screen, badges and filter chips wrap |

**Stack:** React 19, TypeScript (strict), Vite 6, React Router 7, TanStack Query 5, axios, React Hook Form + Zod, MUI 7.

### Project structure

```
frontend/src/
  api/          One module per API area (auth, users, tasks), the shared axios client,
                TypeScript types for every request and response, and the query cache keys
  auth/         Session storage, AuthProvider + useAuth, and the route guards
                (ProtectedRoute, PublicOnlyRoute, AdminRoute)
  components/   Reusable UI: layout, dialogs, badges, filters, loading/empty/error states
  pages/        One component per route
  lib/          Plain functions with no UI: error parsing, validation rules and limits,
                formatting, filtering, the polling rule
  App.tsx       The route table
  main.tsx      Providers: TanStack Query, MUI theme, router, auth
  config.ts     Reads VITE_API_URL
```

Pages own the data fetching (TanStack Query hooks) and compose components; components get their data through props; nothing outside `api/` knows a URL.

### Routes

| Path                  | Access                                                 | Page                                                            |
| --------------------- | ------------------------------------------------------ | --------------------------------------------------------------- |
| `/login`, `/register` | Logged out only (a logged-in user is sent to `/tasks`) | Login, Register                                                 |
| `/tasks`              | Logged in                                              | Task list (`?status=Done&q=report` keeps the filter in the URL) |
| `/tasks/:id`          | Logged in                                              | Task details                                                    |
| `/admin/users`        | Admin                                                  | Users; a normal user sees "Access denied" at the same URL       |
| anything else         | Logged in                                              | Page not found                                                  |

### How authentication works in the frontend

- **Where the token is kept:** after login, the access token and its expiry are stored in `localStorage` (`auth/session.ts`). On startup the app reads them back and throws away an expired or corrupted value. This is what keeps the user logged in after a refresh. The trade-off: any script running on the page can read `localStorage`, so the app relies on having no XSS holes (React escapes output by default). An httpOnly cookie would avoid that, but needs CSRF protection and backend changes.
- **Current user:** `AuthProvider` loads the user from `GET /api/users/me` whenever a token exists, and the header shows that user's name and role.
- **Protected routes:** every route except Login and Register sits inside `ProtectedRoute`. Without a session it redirects to `/login` and remembers the page that was asked for, so the user lands there after logging in. Admin routes also sit inside `AdminRoute`, which shows "Access denied" to non-admins. The API enforces the same rules (401 and 403), so the guards are for usability, not security.
- **401 handling:** the API client reports a 401 to `AuthProvider` through one handler. A 401 on a request that carried a token means the token expired or the user was deleted, so the session is cleared, the query cache is emptied (the next user never sees the previous user's data) and the login page shows "Your session has expired". A 401 from the login form itself has no token and is shown as "Invalid email or password" instead.
- **Logout** calls `POST /api/auth/logout` (which revokes the refresh tokens), then clears the session locally, even if that call fails.

### API client layer

- `api/client.ts` is the only axios instance. A request interceptor adds `Authorization: Bearer <token>`; a response interceptor handles 401 as described above.
- `api/auth.ts`, `api/users.ts` and `api/tasks.ts` contain one typed function per endpoint, for example `getTask(id): Promise<TaskResponse>`. The types in `api/types.ts` mirror the backend DTOs; enums are string unions because the API sends enum names.
- `lib/errors.ts` turns any failure into `{ status, message, fieldErrors }`: the problem-details `detail` (for example the duplicate-title message), the first validation message for a 400, a fixed sentence for 5xx, or "Cannot reach the server" when there is no response. Forms show `message` in an alert and `fieldErrors` under the matching fields.
- Server state goes through TanStack Query. After a change the cache is either updated from the response (status update, create user, delete user) or invalidated (create task, which has to land in the API's sort order). Because of that, the task list and the users list stay fresh for 30 seconds (`LIST_STALE_TIME_MS` in `queryClient.ts`): going back to them reuses the cache instead of calling the API again. The task list is the exception while a task is still processing, because its cached copy may be out of date; then it is reloaded at once. Task details are always loaded from `GET /api/tasks/{id}` when opened.

### How the background status change is shown

The API's worker picks up every new task, "works" for 5 seconds, then sets `processedAt`. It never changes the status, so the frontend watches `processedAt`, not `status === Pending`.

- The task list and the task details page use TanStack Query's `refetchInterval`. While at least one task has `processedAt === null`, the request is repeated every 2 seconds; once every task is processed the interval returns `false` and polling stops by itself.
- A "Processing…" badge (with a spinner) turns into "Processed" as soon as a poll sees `processedAt`. On the details page the "Processed" field changes from "Not yet" to the time.
- Polling pauses while the browser tab is hidden (TanStack Query's default) and gives up on a task that is still unprocessed 5 minutes after it was created, so a stuck worker does not cause endless requests.
- **Why polling** and not SignalR or server-sent events: it needs no backend change, the wait is short (about 5 seconds), and it only runs while something is processing. A push channel would be more immediate, but adds a hub, connection handling and authentication on the socket for a single event.

### Forms and validation

React Hook Form with Zod schemas. The limits come from `lib/limits.ts`, which mirrors the API's attributes (name 100, email 256, password 8 to 100, title 200, description 2000), and the user field rules are shared by Register and the admin's Create user form (`lib/validation.ts`). Server errors are shown too: a 409 marks the duplicate field and shows the API's message, and 400 validation messages are placed under the matching fields.



## Changes since Stage 1

Backend changes made for the frontend:

| Change                                                                                                                                                                                                                                                                                                                                                                          | Reason                                                                                                                                                                                                                                                                                                                                                                                                                                |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| CORS policy `Frontend` added (`Api/Infrastructure/CorsExtensions.cs`, `Cors:AllowedOrigins` in `appsettings.json`). Only `http://localhost:5173` is allowed, with only the `Authorization` and `Content-Type` headers and the GET, POST, PATCH and DELETE methods.                                                                                                              | The browser blocks calls from the React app (a different origin) unless the API allows them. The middleware runs before the exception handler, so 401 and 409 error responses also carry the CORS headers and the UI can show their messages.                                                                                                                                                                                         |
| `Dockerfile` and `.dockerignore` read the .NET code from `backend/TaskManagement/`, and the README paths were updated to match. `frontend/` is excluded from the API's Docker build.                                                                                                                                                                                            | The Stage 1 code had moved into `backend/`, but the Dockerfile still copied from `src/`, so `docker compose up --build` failed.                                                                                                                                                                                                                                                                                                       |
| `Tasks.Status` is now an EF Core concurrency token (`TaskItemConfiguration`), a concurrency failure is mapped to 409 (`AppDbContext`), and `TaskService.UpdateStatusAsync` returns "This task was changed by another request at the same time. Reload it and try again." Migration `MakeTaskStatusConcurrencyToken` only updates the model snapshot; the table does not change. | Two status changes sent at the same moment (for example Done from one tab and In progress from another) both passed the "Done is final" check, and the last one won, so a Done task could be reopened. In a test of 25 simultaneous Done + In progress pairs, 11 tasks ended up In progress after Done had succeeded; after the fix, 0 of 50.                                                                                         |
| The background worker sets `processedAt` with a single atomic `UPDATE ... WHERE Id = @id AND ProcessedAt IS NULL` (`ITaskRepository.MarkProcessedAsync`, `TaskProcessor`) instead of loading the task, changing it and saving it. `TaskItem.MarkProcessed` was removed, because the database now enforces "processed only once".                                                | With `Status` as a concurrency token, the worker's old save also ran `UPDATE ... WHERE Id = @id AND Status = @original` (confirmed in the SQL log). If the user changed the status between the worker's read and its save, the save failed and the task stayed unprocessed until the next restart. The new UPDATE never reads or checks the status, so the worker and the user cannot conflict, and it uses one query instead of two. |

No status code had to change: the API already returns 400 (validation or business rule), 401 (invalid login or token), 403 (not an admin), 404 (not found, or another user's task) and 409 (duplicate email or task title), each with a readable `detail` or field messages. This was checked for every endpoint the frontend uses.

## Enhancements

Improvements to the existing flows, beyond what the task asks for, and why each was made:

- **Simultaneous status changes cannot reopen a Done task (backend):** the "Done is final" check and the save are now protected by a concurrency token on `Status`, so when two changes race, the second one gets a 409 with a clear message instead of silently overwriting the first. The frontend shows that message and reloads the task. This fixes a real rule violation that was reproduced before the change (see "Changes since Stage 1").
- **The background worker and the user can change the same task at the same time (backend):** the worker marks a task processed with one atomic `UPDATE` that only touches `processedAt`, so a status change made by the user while the worker is finishing can neither be overwritten nor make the worker fail, and a task can only be marked processed once, even with two workers. It also replaces a read and a write with a single query.
- **Session expiry while working:** a 401 from any request (for example while the create-task dialog is open) ends the session with a clear "Your session has expired" message, and after logging in the user goes back to the page they were on, so they do not lose their place.
- **A wrong password is not "session expired":** a 401 from the login form is shown as "Invalid email or password." and never triggers the logout flow, so the two cases cannot be confused.
- **Forms keep their input after a server error,** submit buttons are disabled while their request runs, and a dialog cannot be closed mid-request, so nothing is lost or sent twice.
- **Readable duplicate errors:** a duplicate task title or email (409) shows the API's message and marks the field that caused it.
- **Client rules match the server exactly:** all limits come from one file that mirrors the API, and Register and Create user share the same rules, so the browser never accepts what the API rejects, or the other way round.
- **Done is confirmed:** because `Done` is final in the backend, choosing it asks for confirmation first, and afterwards the status control is locked with an explanation instead of failing on the next click.
- **Status changed elsewhere:** if a status update fails (for example the task was set to Done in another tab), the page shows the API's message and reloads the task to show its real state.
- **No unnecessary requests:** a malformed task id shows "Task not found" without calling the API; status updates, created users and deleted users are written into the cache from the response instead of reloading lists; polling runs only while a task is being processed; and the task and user lists are reused from the cache for 30 seconds instead of being reloaded on every visit (a path of opening two tasks and switching to Users and back went from 7 API calls to 3).
- **Bounded polling:** it pauses in a hidden tab and stops for a task still unprocessed after 5 minutes, so a stuck worker cannot cause endless requests.
- **Filters survive navigation:** the status filter and search are kept in the URL, and "Back to tasks" on a task's details restores them.
- **Separate empty states:** "You have no tasks yet" and "No tasks match your filters" (with a Clear filters button) are different messages, so an empty filter result is not mistaken for lost data.
- **Safer admin actions:** the delete button is not shown on the admin's own row (the API forbids self-delete), the confirmation explains that the user's open sessions end at once, and deleting a user who was already deleted fixes the list instead of failing.
- **No leaking between users:** logging out, or an expired session, clears every cached response, so the next person on the same browser never sees the previous user's tasks.
- **Register logs you in:** after registering, the user is logged in straight away instead of typing the same credentials again.
- **The admin's own password is not autofilled** into the Create user form (`autocomplete="new-password"`).
- **Clear failure messages:** a 5xx response shows one fixed sentence, and an unreachable server shows "Cannot reach the server", instead of raw technical errors.
- **Accessibility:** every input has a label; badges use an icon as well as a color; task rows are single links reachable with Tab and Enter; the status filter chips expose `aria-pressed`; destructive dialogs focus Cancel first; MUI provides visible focus and keyboard support in dialogs and menus.

Optional bonus items included: TanStack Query with caching and invalidation, React Hook Form with Zod, and the accessibility points above.

## Assumptions

**Backend (Stage 1)**

- "Same day" in the duplicate rule means the same UTC calendar day.
- The admin follows the same task rules as everyone else and sees only their own tasks.
- Self-registration always creates a regular user. Only an admin can create another admin.
- Deleting a user is a soft delete. The user's tasks are kept, and the email can be registered again.
- "Created date" sorting is oldest first within the same priority.
- All dates are stored and returned in UTC.
- Logout revokes refresh tokens; the current access token remains valid until it expires.

**Frontend (Stage 2)**

- The access token is kept in `localStorage` (see "How authentication works in the frontend"). The refresh token returned by login is not used by the client: when the 60-minute access token expires, the user logs in again.
- "Processed" means the worker has set `processedAt`. The task's status belongs to the user and is not touched by the worker.
- Times are shown in the browser's local time zone. The duplicate-title rule uses the UTC day (a Stage 1 decision), so close to midnight a title can count as "today" in UTC but not locally; the API's message is shown as is.
- An admin has their own task list like everyone else; the admin area only manages users.
- The task list is filtered in the browser, as the task allows, because the API already returns all of the user's tasks.

## Not done

- Component and unit tests for the frontend (Vitest + React Testing Library).
- A frontend service in Docker Compose; the frontend runs with `npm run dev`.
- Server-side pagination.
- A refresh-token flow in the client. The backend supports it, but it rotates refresh tokens and treats a reused one as theft, so two requests refreshing at the same moment would end all of the user's sessions. Doing it safely needs one shared refresh and a retry queue, and it was left out to keep the session handling simple and reliable.

## How AI tools were used

- **Claude Code (Anthropic)** was used to plan Stage 2 in phases, to generate some of the frontend code phase by phase, to check the error handling against the backend's real responses, adding comments, to run browser checks after each phase, and to draft this README.
- I reviewed every change, tested each phase myself against the real API, made the commits, and can explain and change any part of the code.
