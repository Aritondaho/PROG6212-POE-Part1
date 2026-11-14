# RaceDay — PROG6212 Portfolio of Evidence

RaceDay is a full-stack event management system for South African road running, walking and cycling events. **Organisers** create events, categories and results. **Participants** browse events, enter by choosing a category, and track their race history.

| Part | What it delivers | Where |
| --- | --- | --- |
| Part 1 | ERD, API endpoint plan, SQL script | [`docs/`](docs/) |
| Part 2 | ASP.NET Core Web API with EF Core, sessions, Swagger, tests and CI | [`src/RaceDay.Api`](src/RaceDay.Api), [`tests/`](tests/) |
| **Part 3 (POE)** | **ASP.NET Core MVC web app that uses the API, Azure Blob Storage for images, Docker** | [`src/RaceDay.Web`](src/RaceDay.Web) |

Part 3 is on the **`part-3-mvc`** branch.

---

# Part 3: MVC Web Application, Azure Blob Storage and Docker

## System Overview

```
 Browser ──HTTP──▶ RaceDay.Web (MVC, port 5167 / Docker 8080)
                       │  typed HttpClient + the user's API session cookie
                       ▼
                   RaceDay.Api (Web API, port 5285) ──EF Core──▶ SQL Server
                       │
                       └──Azure SDK──▶ Azure Blob Storage (event banners, profile pictures)
```

- **The MVC app has no database access and no copy of the business rules.** Every read and write goes through `RaceDayApiClient`, which calls the Part 2 API. The project does not reference Entity Framework or the API project.
- **The MVC app never talks to Azure.** Files are posted to the API, the API uploads them to Blob Storage and saves the URL, and the MVC pages show that URL.

## User Roles

| Role | Interface |
| --- | --- |
| **Organiser** | Lime "Organiser console" bar and role pill. Menu: Dashboard, Manage events, Public listing. Pages: dashboard, event create/edit/delete with banner upload, categories, enrolments (confirm/cancel), results entry, profile. |
| **Participant** | Orange role pill. Menu: Upcoming events, My events, My results. Pages: events home with filters, event detail with Enter, My events, My results, profile with picture upload. |
| **Visitor** (not signed in) | Can browse events and event details. Every other page redirects to Log in and returns afterwards. |

The API enforces the roles (Part 2). The MVC app uses the same roles for navigation through `[RequireRole]`: a visitor is sent to the login page, and a user with the wrong role sees the "access denied" page.

## Features (Part 3 requirements)

| Requirement | Implementation |
| --- | --- |
| Register and log in through the API | `AccountController` calls `POST /api/auth/register` and `/api/auth/login`. A new account is signed in straight after registration. |
| Session across all pages | The MVC session stores the user's ID, role and name. `ApiSessionCookieHandler` keeps each user's API session cookie in that session and sends it with every API call. |
| Distinct interface per role | Separate menus, role pill, organiser console bar and different home pages (organiser dashboard, participant events page). |
| Protected pages | `[RequireRole]` on controllers. An expired API session (401) clears the MVC session and asks the user to sign in again. |
| Log out | `POST /Account/Logout` ends the API session, clears the MVC session and cookie, and redirects to Log in. |
| Organiser dashboard | Events created, total enrolments, upcoming events and entries to confirm, a "next up" card, and a table with enrolment, confirmed, category and result counts per event (`GET /api/events/mine`). |
| Events management | Create, edit and delete with name, description, date and start time, location, distance and type. Delete explains when entries block it. |
| Event banner upload | File input with live preview on create and edit. The file is sent to `POST /api/events/{id}/upload-banner` and stored in Azure Blob Storage. |
| Categories | Add, edit and remove age or distance categories. Delete is disabled once participants have entered a category. |
| Enrolments | Every participant for an event with category, entry date and colour-coded status, with filters, single confirm/cancel and "confirm all pending". |
| Results entry | Inline finish time (h:mm:ss) and position per entry, published through the API. Capture opens when the event starts. |
| Participant home | Upcoming events with search, type, date and location filters, and an "include past events" switch. |
| Event detail | Banner, all fields, categories table and an Enter panel that adapts to the user (visitor, organiser, already entered, past event, no categories yet). |
| Enter an event | Choose a category and enter. The API records it straight away as Pending. |
| My events | All entries, split into upcoming and past, with colour-coded status. |
| My results | Event, date, category, finish time and position, for example "47th out of 312 finishers", plus best finish and total time on course. |
| Profile | View and update personal details. Participants upload a profile picture, which goes through the API to Blob Storage and appears in the navigation bar. |
| UI design | Bootstrap 5 with one RaceDay theme (navy, race orange, lime; Barlow Condensed headings and Inter body text) used on every page. Statuses: **green** Confirmed, **amber** Pending, **red** Cancelled, each with an icon. |
| Invalid input | Forms validate on the page and on the server, API messages are shown on the form, and friendly pages cover 404, 403, an unreachable API and unexpected errors. Raw errors are never shown. |

## Setup and Run

### Prerequisites

- **.NET 10 SDK** and **Visual Studio 2026** (or later) with the *ASP.NET and web development* workload
- **SQL Server** (see Part 2 below for connection options)
- **Docker Desktop** (for the container)
- An **Azure Storage account** for Blob Storage. Without one, the API stores images on its own disk so everything still runs.

### 1. Get the code and open it in Visual Studio

```bash
git clone https://github.com/Aritondaho/PROG6212-POE-Part1.git
cd PROG6212-POE-Part1
git checkout part-3-mvc
```

(Or in Visual Studio: **Clone a repository**, paste the URL, then switch to the `part-3-mvc` branch in **Git → Manage Branches**.)

1. Open **`RaceDay.slnx`** (File → Open → Project/Solution).
2. In the toolbar's start-up drop-down choose **RaceDay (API + Web)**. This profile comes from `RaceDay.slnLaunch`.
3. Press **F5**. The API starts on `http://localhost:5285` (Swagger at `/swagger`) and the website on **`http://localhost:5167`**.

From the command line instead, open two terminals:

```bash
dotnet run --project src/RaceDay.Api --launch-profile http
dotnet run --project src/RaceDay.Web --launch-profile http
```

Then sign in with a demo account from Part 2 (password `RaceDay2026!`), for example `race@pretoriasports.co.za` (Organiser) or `amanda.w@email.com` (Participant).

### 2. Connect Azure Blob Storage

1. In the Azure portal create a **Storage account** (Standard, LRS is enough).
2. Under **Security + networking → Access keys**, copy a **Connection string**.
3. Store it as a user secret for the API, so it is never committed:

```bash
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<your connection string>" --project src/RaceDay.Api
```

4. Restart the API. The first upload creates the `raceday-images` container, with folders `banners/` and `profile-pictures/`.

If the storage account blocks anonymous blob access (the Azure default), the API creates a private container and saves a long-lived, read-only SAS URL for each image, so images still display without exposing the account key. To show plain blob URLs instead, turn on *Allow Blob anonymous access* under the account's **Configuration**.

### 3. Run the MVC app in Docker

Start the API on the host first (step 1), then build the image from the repository root and run it:

```bash
docker build -f src/RaceDay.Web/Dockerfile -t raceday-web .
docker run --rm -p 8080:8080 --name raceday-web raceday-web
```

Open **http://localhost:8080**. The container reaches the API on the host through `host.docker.internal:5285`. To use a different API address, add `-e RaceDayApi__BaseUrl=http://<host>:<port>/`.

**Dockerfile best practices used:** multi-stage build (SDK image to build, small ASP.NET runtime image to run), a restore layer cached on the project file alone, `.dockerignore` to keep `bin/`, `obj/`, tests and secrets out of the context, configuration through environment variables, port 8080, and the image's non-root `app` user.

## Testing

- `dotnet test RaceDay.slnx` runs the **57 API tests** (Part 2 plus the Part 3 endpoints) against SQL Server.
- The MVC app was checked end to end against the running API, both locally and inside the Docker container. The checks covered: register and log in for both roles, role redirects, create event with banner, add and validate categories, enter an event, duplicate entry, profile picture upload and rejection, profile update, confirm enrolment, invalid and valid results, the delete-blocked message, My results ("1st out of 1 finisher"), logout and the redirect after logout. All passed.

## Project Structure (Part 3 additions)

```
src/RaceDay.Web/
├── Dockerfile                       Multi-stage container build
├── Program.cs                       Session, typed API client, error pages, culture
├── Services/
│   ├── RaceDayApiClient.cs          Every API call the site makes
│   ├── ApiSessionCookieHandler.cs   Carries each user's API session cookie
│   └── ImageUrlResolver.cs          Makes API image URLs loadable by the browser
├── Infrastructure/                  RequireRole filter, ApiExceptionFilter, session user, UI helpers
├── Controllers/                     Account, Events, Participant, Profile, Organiser,
│                                    ManageEvents, Categories, Enrolments, Results, Home
├── Models/                          View models and API contracts (Models/Api)
├── Views/                           Razor views, one shared layout and partials
└── wwwroot/css/site.css             RaceDay theme
src/RaceDay.Api/Services/
├── IImageStorage.cs                 Storage abstraction and image rules
├── AzureBlobImageStorage.cs         Azure Blob Storage implementation
└── LocalImageStorage.cs             Local fallback (tests and offline use)
RaceDay.slnLaunch                    Visual Studio "API + Web" launch profile
.github/workflows/ci.yml             Builds API + Web, runs tests, builds and runs the Docker image
```

## Part 3 Changes to the API

| Change | Why |
| --- | --- |
| `IBannerStorage` became `IImageStorage` with a folder name | Banners and profile pictures share the same rules and storage |
| `AzureBlobImageStorage` | Part 3 Blob Storage requirement. Used whenever a connection string is configured. |
| `GET /api/events/mine` | The dashboard needs each organiser's events with enrolment counts in one call |
| `POST /api/users/profile/picture` and `profilePictureUrl` on the profile | Participant profile picture (the column already existed in the Part 1 ERD) |
| Pinned `System.Security.Cryptography.Pkcs` 10.0.14 | Clears two NuGet vulnerability warnings from a transitive package |

## Part 3 CI/CD

`.github/workflows/ci.yml` runs on every push. It builds the API **and** the MVC project, publishes the web app, and runs all tests against a SQL Server container. A second job builds the MVC Docker image, starts it and checks that it serves the login page.

**CI build screenshot:**

> [![Part 3 CI build](docs/Screenshot%202026-11-14%20131748.png)](docs/Screenshot%202026-11-14%20131748.png)
> `![Part 3 green CI build](docs/part3-ci-green-build.png)`

## Part 3 Video Presentation

> https://youtu.be/cJcpEQO-a7U

## AI Tool Disclosure

AI tools (Claude Code) were used to help plan, write and review code, tests and documentation in Parts 2 and 3. The author reviewed and tested the work, checked the design decisions, and can explain the code.

> **TODO for the author:** confirm this statement is accurate for your work, and edit it if needed, before you submit.

---

# Part 2: RESTful API

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
git clone https://github.com/Aritondaho/PROG6212-POE-Part1.git
cd PROG6212-POE-Part1
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

**Expected result:** the last line reads `Passed!  - Failed:     0, Passed:    57, Skipped:     0, Total:    57`, and the command exits with code 0.

The tests use the same default connection as the API (local Windows authentication). If your SQL Server setup differs, set `RACEDAY_TEST_CONNECTION` in the same terminal, using the same format as step 2 above. For example, PowerShell:

```powershell
$env:RACEDAY_TEST_CONNECTION = "Server=localhost;User Id=sa;Password=YourPassword;TrustServerCertificate=True;"
dotnet test RaceDay.slnx
```

The test databases are named `RaceDayTests_<random>` and are dropped automatically.

The GitHub Actions workflow sets this for you.

**Test coverage (57 tests, including 6 added in Part 3):**

| Area | What is tested |
| --- | --- |
| Registration and login (`AuthenticationTests`) | Both roles register, the password is stored as a hash, duplicate email, bad role, missing role fields, short password, wrong or unknown credentials, session cookie set on login, logout ends the session, profile update |
| Event management and banners (`EventManagementTests`) | Create, update and delete by the owning Organiser, 403 for another Organiser, public list and filters, validation, 404, PNG banner stored, text file rejected |
| Role enforcement (`RoleAccessTests`) | Participant calls to Organiser endpoints return 403, Organiser calls to Participant endpoints return 403, anonymous calls return 401 |
| Part 3 endpoints (`Part3ApiTests`) | `GET /api/events/mine` returns only the caller's events with counts (403 for participants, 401 anonymous); profile picture upload stores the URL, rejects non-images and rejects organisers |
| Enrolment and results (`EnrolmentAndResultTests`) | Enrolment is saved with the event and category link, duplicate and cross-event enrolments are rejected, Organisers see only their own enrolments, results are recorded once and show position out of finishers, delete is blocked (409) when enrolments exist |

---

## Project Structure

```
PROG6212-POE-Part2/
├── RaceDay.slnx                      Solution (API, Web, tests)
├── NuGet.config                      Restores from nuget.org
├── docs/                             Part 1 planning documents (ERD, endpoint plan, SQL script)
├── .github/workflows/
│   ├── validate.yml                  Part 1 structure check (runs on main)
│   └── ci.yml                        Build, test and Docker workflow (Parts 2 and 3)
├── src/RaceDay.Api/
│   ├── Program.cs                    Services, sessions, Swagger, migrations, seeding
│   ├── Controllers/                  Auth, Users, Events, Categories, Enrolments, Results
│   ├── Models/                       EF Core entities (match the Part 1 ERD)
│   ├── Dtos/                         Request and response contracts
│   ├── Data/                         RaceDayDbContext, DbSeeder, design-time factory
│   ├── Migrations/                   EF Core Code-First migration (InitialCreate)
│   ├── Security/                     SessionAuthorizeAttribute and session keys
│   └── Services/                     IImageStorage: Azure Blob Storage (Part 3) or local files
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
| GET | `/api/events/mine` *(added in Part 3)* | Organiser | 200, 401, 403 |
| POST | `/api/users/profile/picture` *(added in Part 3)* | Participant | 200, 400, 401, 403 |

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

## Part 2 CI/CD

The workflow `.github/workflows/ci.yml` (named `part2-api-ci.yml` during Part 2) runs on every push and pull request. It:

1. checks the repository structure (solution, projects, `docs/` and `README.md`)
2. restores and builds the solution in Release
3. starts a `mcr.microsoft.com/mssql/server:2022` service container and runs all tests against it
4. uploads the test results as a build artifact

**CI build screenshot:**

> **TODO:** add a screenshot of the green workflow run here, saved as `docs/ci-green-build.png`, for example:
> `![Green CI build](docs/ci-green-build.png)`

---

## Part 2 Video Presentation

> **TODO:** add the unlisted YouTube link for the Part 2 walkthrough (running API, code structure, endpoints, authentication and unit tests):
> **https://youtu.be/XXXXXXXXXXX**

---

# Part 1: System Planning and Database

The Part 1 deliverables (ERD, endpoint plan, SQL script, CI validation and video) are kept here unchanged. The Part 2 API above is built to this plan.

## Part 1 overview

### System Description
RaceDay is a race event management system designed to manage organisers, participants, events, categories, event enrolments and race results.
Part 1 focuses on planning the database structure, RESTful API endpoints and SQL Server database schema before application development begins.

### User Roles
#### Organiser
Organisers can manage race events and categories, view participant enrolments and manage race results.

#### Participant

Participants can view available events and categories, enrol for events and view their race results and profile information.
### Planning Documents

The planning documents are available in the `docs` folder:

- [Entity Relationship Diagram](docs/Entity%20Relationship%20Diagram.jpg)
- [API and Database Plan](docs/API%20and%20Database%20Plan.pdf)
- [Database SQL Script](docs/Database%20SQL%20Script.sql)

### Database

The SQL script creates and populates the RaceDay database using SQL Server. It includes the required tables, primary keys, foreign keys, constraints and sample data.

### CI/CD

A GitHub Actions workflow is used to validate the repository structure and confirm that the required planning documents are present.
<img width="1267" height="622" alt="Screenshot 2026-09-03 234650" src="https://github.com/user-attachments/assets/d854aae1-e4f3-4666-b0cf-e095bf6368af" />

### YouTube Presentation

The unlisted YouTube presentation explains the planning documents, ERD decisions, API endpoint choices and the SQL script running in SQL Server Management Studio.
**YouTube Video:**  
https://youtu.be/MPQG_Ewtgg8

### Repository Structure

PROG6212-POE-Part1/
│
├── docs/
│   ├── Entity Relationship Diagram.jpg
│   ├── API and Database Plan.pdf
│   ├── Database SQL Script.sql
│   └── .gitkeep
│
├── .github/
│   └── workflows/
│       └── validate.yml
│
└── README.md
