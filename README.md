# PROG6212-POE-Part2 — RaceDay API (Part 2)

## RaceDay System - Part 2 RESTful API

RaceDay is a race event management system for South African road running, walking and cycling events. Organisers create events, categories and results. Participants browse events, enrol by category and track their results.

Part 2 builds the **ASP.NET Core Web API** described in the Part 1 plan. It uses Entity Framework Core (Code-First) on SQL Server, server-side session authentication with role enforcement, Swagger UI for testing, xUnit API tests, and a GitHub Actions workflow.

The Part 1 planning documents (ERD, endpoint plan and SQL script) are in [`docs/`](docs/) on `main`.

---

## User Roles

Every account is either an **Organiser** or a **Participant**. The role is chosen at registration and stored on the account. It is kept in the server-side session after login, and the API checks it on every protected request.

| Role | Can do |
| --- | --- |
| **Organiser** | Create, update and delete their own events. Upload event banners. Manage categories for their events. View enrolments for their events. Confirm or cancel enrolments. Record and correct finish times and positions. |
| **Participant** | Create an account. Browse and filter events, and view categories. Enrol in an event by choosing one of its categories. View their own enrolments and their own race history. Update their own profile. |

Both roles can read their own profile and sign out. Anyone can browse events without signing in.

Role rules are enforced by the `[SessionAuthorize]` attribute in the API, not by the UI.

- No session: **401 Unauthorized**
- Signed in with the wrong role, or an Organiser acting on another Organiser's event: **403 Forbidden**

---

## Setup and Run

### Prerequisites

- **.NET 10 SDK** (`dotnet --version` should print `10.0.x`)
- **SQL Server 2019 or later** (Developer, Express or LocalDB). The default connection uses Windows authentication on a local server. If your setup differs, see step 2.
- *Optional:* Visual Studio with the ASP.NET and web development workload (a version that supports .NET 10), or SQL Server Management Studio (SSMS) to look at the database.
- No other tools are needed. The project does not depend on any file outside the repository, so a fresh clone is enough.

### 1. Clone and restore

```bash
git clone https://github.com/Aritondaho/PROG6212-POE-Part2.git
cd PROG6212-POE-Part2
git checkout part-2-api
dotnet restore RaceDay.slnx
```

`NuGet.config` in the repository points restore at nuget.org, so the build does not depend on any private feed.

### 2. Point the API at your SQL Server (check this first)

The API reads one setting, the connection string `DefaultConnection`. The default in `src/RaceDay.Api/appsettings.json` is:

```json
"DefaultConnection": "Server=localhost;Database=RaceDayPart2;Trusted_Connection=True;TrustServerCertificate=True;"
```

This works when SQL Server runs on the same machine on the default instance and your Windows account can sign in to it. If that is not your setup, **do not edit the repository files.** Set an environment variable in the terminal you run the API from. It overrides `appsettings.json`.

PowerShell (named instance such as SQLEXPRESS):

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost\SQLEXPRESS;Database=RaceDayPart2;Trusted_Connection=True;TrustServerCertificate=True;"
```

PowerShell (SQL Server login instead of Windows login):

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=RaceDayPart2;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
```

The database does not need to exist. On startup the API applies the EF Core migrations and creates `RaceDayPart2` by itself. Your other databases are never touched.

### 3. Run the API

**Visual Studio:** open `RaceDay.slnx`, set `RaceDay.Api` as the startup project, and press **F5** (the `http` profile).

**Command line:**

```bash
dotnet run --project src/RaceDay.Api
```

Then open **http://localhost:5285/swagger** (or the port shown in the console). In Development the API also loads sample data (see below).

**Expected console output** (the messages you should see, in this order, with timestamps omitted):

```
Applying migration '20261013102330_InitialCreate'.      (first run only)
Now listening on: http://localhost:5285
Application started. Press Ctrl+C to shut down.
Hosting environment: Development
```

The warning `Failed to determine the https port for redirect` is expected when you use the `http` profile. It does not affect anything. Opening `http://localhost:5285/` redirects to `/swagger`.

### 4. Try the API in Swagger

1. Open `POST /api/auth/login` and sign in with a demo account.
2. Swagger UI keeps the `RaceDay.Session` cookie, so later requests are signed in automatically.
3. Call any endpoint. Protected endpoints return 401 or 403 when the role does not match.
4. Call `POST /api/auth/logout` to end the session.

### Demo accounts (Development only)

All seeded accounts use the password **`RaceDay2026!`**.

| Role | Email | Notes |
| --- | --- | --- |
| Organiser | `race@pretoriasports.co.za` | Owns the Pretoria events |
| Organiser | `events@durbanactive.com` | Owns the Durban events |
| Participant | `amanda.w@email.com` | Enrolled in several events, has results |
| Participant | `sipho.k@email.com` | Enrolled, no results |

The seed data matches the Part 1 SQL script. The only change is that the passwords are real PBKDF2 hashes instead of placeholder strings.

---

## Running the Tests

The tests run the real API in-process. Each test class gets its own SQL Server database, which is dropped afterwards.

```bash
dotnet test RaceDay.slnx
```

**Expected result:** the last line reads `Passed!  - Failed:     0, Passed:    51, Skipped:     0, Total:    51`, and the command exits with code 0.

The tests use the same default connection as the API (local Windows authentication). If your SQL Server setup differs, set `RACEDAY_TEST_CONNECTION` in the same terminal, using the same format as step 2 above. For example, PowerShell:

```powershell
$env:RACEDAY_TEST_CONNECTION = "Server=localhost;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
dotnet test RaceDay.slnx
```

The test databases are named `RaceDayTests_<random>` and are dropped automatically.

The GitHub Actions workflow sets this for you.

**Test coverage (51 tests):**

| Area | What is tested |
| --- | --- |
| Registration and login (`AuthenticationTests`) | Both roles register, the password is stored as a hash, duplicate email, bad role, missing role fields, short password, wrong or unknown credentials, session cookie set on login, logout ends the session, profile update |
| Event management and banners (`EventManagementTests`) | Create, update and delete by the owning Organiser, 403 for another Organiser, public list and filters, validation, 404, PNG banner stored, text file rejected |
| Role enforcement (`RoleAccessTests`) | Participant calls to Organiser endpoints return 403, Organiser calls to Participant endpoints return 403, anonymous calls return 401 |
| Enrolment and results (`EnrolmentAndResultTests`) | Enrolment is saved with the event and category link, duplicate and cross-event enrolments are rejected, Organisers see only their own enrolments, results are recorded once and show position out of finishers, delete is blocked (409) when enrolments exist |

---

## Project Structure

```
PROG6212-POE-Part2/
├── RaceDay.slnx                      Solution (API + tests)
├── NuGet.config                      Restores from nuget.org
├── docs/                             Part 1 planning documents (ERD, endpoint plan, SQL script)
├── .github/workflows/
│   ├── validate.yml                  Part 1 structure check (runs on main)
│   └── part2-api-ci.yml              Part 2 build and test workflow
├── src/RaceDay.Api/
│   ├── Program.cs                    Services, sessions, Swagger, migrations, seeding
│   ├── Controllers/                  Auth, Users, Events, Categories, Enrolments, Results
│   ├── Models/                       EF Core entities (match the Part 1 ERD)
│   ├── Dtos/                         Request and response contracts
│   ├── Data/                         RaceDayDbContext, DbSeeder, design-time factory
│   ├── Migrations/                   EF Core Code-First migration (InitialCreate)
│   ├── Security/                     SessionAuthorizeAttribute and session keys
│   └── Services/                     IBannerStorage and local file storage
└── tests/RaceDay.Api.Tests/
    ├── Infrastructure/               RaceDayApiFactory (test host) and ApiHelpers
    ├── AuthenticationTests.cs
    ├── EventManagementTests.cs
    ├── RoleAccessTests.cs
    └── EnrolmentAndResultTests.cs
```

---

## Design Notes

**Schema.** The EF Core model is configured to match the Part 1 SQL script: table and column names, primary keys, unique keys (`Users.Email`, one enrolment per participant and event, one result per enrolment), CHECK constraints (roles, event types, statuses), `DATETIME`, `DATE`, `TIME` and `DECIMAL(5,2)` column types, and the `GETDATE()` and `'Pending'` defaults. Deleting an organiser, participant, event (while it has enrolments) or category (while it has enrolments) is blocked. Deleting an event also removes its categories.

**Authentication.** Registration and login use `PasswordHasher<User>` from ASP.NET Core Identity (PBKDF2). The plain-text password is never stored or returned. Login clears any old session before it writes the user ID and role, which prevents session fixation. Wrong email and wrong password return the same message, so accounts cannot be enumerated.

**Sessions.** The session is held on the server (`AddDistributedMemoryCache` with ASP.NET Core session state). The cookie `RaceDay.Session` is HttpOnly and expires after 30 minutes of inactivity. The role is read from the server-side session and never from the request body.

**Authorization.** `[SessionAuthorize]` with no arguments requires any signed-in user. `[SessionAuthorize(UserRoles.Organiser)]` requires that role. Ownership is checked in the controller: an Organiser must own an event before changing it, its categories, its enrolments or its results.

**Errors.** Validation problems return 400. Errors use RFC 7807 Problem Details, so clients never receive raw exception text. Unhandled exceptions return a generic 500.

**Banner images.** Images are validated (JPEG, PNG or WebP, up to 5 MB) and stored under a random GUID file name. Part 2 stores them on the local file system behind `IBannerStorage`. Part 3 replaces that with Azure Blob Storage without changing the controller.

---

## Endpoints

Full descriptions, request bodies and response codes are in Swagger UI. The table below lists every endpoint in the Part 1 plan and how it is implemented.

| Method | Route | Role required | Status codes |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | None | 201, 400 |
| POST | `/api/auth/login` | None | 200, 401 |
| POST | `/api/auth/logout` | Any signed-in user | 200, 401 |
| GET | `/api/users/profile` | Any signed-in user | 200, 401 |
| PUT | `/api/users/profile` | Any signed-in user | 200, 400, 401 |
| GET | `/api/events` | None (filters: `type`, `date`, `location`) | 200, 400 |
| GET | `/api/events/{id}` | None | 200, 404 |
| POST | `/api/events` | Organiser | 201, 400, 403 |
| PUT | `/api/events/{id}` | Organiser (owner) | 200, 400, 403, 404 |
| DELETE | `/api/events/{id}` | Organiser (owner) | 204, 403, 404, 409 |
| POST | `/api/events/{id}/upload-banner` | Organiser (owner) | 200, 400, 403, 404 |
| GET | `/api/events/{eventId}/categories` | None | 200, 404 |
| POST | `/api/events/{eventId}/categories` | Organiser (owner) | 201, 400, 403, 404 |
| PUT | `/api/categories/{id}` | Organiser (owner) | 200, 400, 403, 404 |
| DELETE | `/api/categories/{id}` | Organiser (owner) | 204, 403, 404, 409 |
| GET | `/api/enrolments` | Any signed-in user (own enrolments, or enrolments for own events) | 200, 401 |
| GET | `/api/events/{eventId}/enrolments` | Organiser (owner) | 200, 403, 404 |
| POST | `/api/events/{eventId}/enrol` | Participant | 201, 400, 403, 404 |
| PUT | `/api/enrolments/{id}/status` | Organiser (owner) | 200, 400, 403, 404 |
| GET | `/api/results/me` | Participant | 200, 403 |
| GET | `/api/events/{eventId}/results` | Organiser (owner) | 200, 403, 404 |
| POST | `/api/events/{eventId}/results` | Organiser (owner) | 201, 400, 403, 404, 409 |
| PUT | `/api/results/{id}` | Organiser (owner) | 200, 400, 403, 404 |

Every protected endpoint also returns **401** when no session is present.

---

## Deviations from the Part 1 Plan

The Part 2 API follows the Part 1 endpoint plan. These are the places where it differs, and why.

1. **Registration needs more fields than the plan lists.** The plan made `organisationName` and `dateOfBirth` optional. The Part 1 schema requires them: `Organisers.OrganisationName` and `Organisers.ContactNumber` are NOT NULL, and `Participants.DateOfBirth` is NOT NULL. So Organisers must send `organisationName` and `contactNumber`, and Participants must send `dateOfBirth`. Missing fields return 400.
2. **`contactNumber` was added** to the registration and profile bodies, because the schema needs it for Organisers.
3. **`GET /api/enrolments`** — the plan says "Organisers see all". Organisers see enrolments for events they own. Showing other Organisers' participants would break the ownership rule.
4. **Extra failure codes** the plan did not list: `404` for unknown IDs where it was omitted, `409 Conflict` when deleting an event or category that has enrolments (so participant records are not lost), and `409` when a second result is recorded for the same enrolment. The plan's `400` for a duplicate enrolment is kept as written.
5. **Event update keeps the existing banner** when `bannerImageUrl` is left out, so editing an event does not clear its banner.
6. **Results validation:** finish time must be greater than 00:00:00 and less than 24:00:00, and position must be at least 1.
7. **Result responses** include `totalFinishers`, the number of published results in the event. The Part 3 "47th out of 312" display needs it.
8. **Banner storage** is on the local file system in Part 2 (see Design Notes). The endpoint itself matches the plan.
9. **Seeded passwords** are real hashes. The Part 1 script stored placeholder strings such as `hash_pretoria_789`, which cannot be verified.
10. **Database name** is `RaceDayPart2`, so the API never touches a database that holds the Part 1 script data. The tables, columns and constraints are the same as the Part 1 script.
11. **Tests use SQL Server**, not an in-memory fake, so the constraints and delete rules are tested for real.

---

## CI/CD

The workflow `.github/workflows/part2-api-ci.yml` runs on every push and pull request. It:

1. checks the repository structure (solution, projects, `docs/` and `README.md`)
2. restores and builds the solution in Release
3. starts a `mcr.microsoft.com/mssql/server:2022` service container and runs all tests against it
4. uploads the test results as a build artifact

**CI build screenshot:**

> **TODO:** add a screenshot of the green workflow run here, saved as `docs/ci-green-build.png`, for example:
> `![Green CI build](docs/ci-green-build.png)`

---

## Video Presentation

> **TODO:** add the unlisted YouTube link for the Part 2 walkthrough (running API, code structure, endpoints, authentication and unit tests):
> **https://youtu.be/XXXXXXXXXXX**

---

## AI Tool Disclosure

AI tools were used to help draft and review parts of the code, tests and documentation in this part. The design, endpoint choices and testing were checked by the author, and the author can explain the code.

> **TODO for the author:** confirm this statement is accurate for your work before you submit.
