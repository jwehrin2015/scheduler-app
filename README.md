# Scheduler App

A small-business employee scheduler built with Angular, ASP.NET Core, and SQLite. Managers can maintain employee records and create, edit, and remove dated shifts in a week-at-a-time calendar. The API prevents overlapping shifts for the same employee.

## Requirements

- .NET 10 SDK
- Node.js 22 or later and npm

## Run locally

Start the API in one terminal:

```sh
dotnet run --project backend/SchedulerApp.Api.csproj --urls http://localhost:5000
```

Start the Angular app in another terminal:

```sh
cd frontend
npm install
npm start
```

Open <http://localhost:4200>. The API creates `backend/scheduler.db` on first start and keeps employee and shift data there between runs.

## VS Code

Open this folder in VS Code. Use **Run and Debug → Scheduler: API + Angular** to debug the API and open the frontend in Chrome. Stop any existing API process on port 5000 before starting the debug session. To build both projects, run the default **build: scheduler** task with **Terminal → Run Build Task**. The **test: frontend** task runs the Angular unit tests.

## API

| Method | Path | Behavior |
| --- | --- | --- |
| `GET` | `/api/employees` | List employees |
| `POST` | `/api/employees` | Create an employee |
| `PUT` | `/api/employees/{id}` | Update an employee |
| `DELETE` | `/api/employees/{id}` | Remove an employee without assigned shifts |
| `GET` | `/api/shifts?startDate=YYYY-MM-DD&endDate=YYYY-MM-DD` | List shifts in a date range |
| `POST` | `/api/shifts` | Create a shift |
| `PUT` | `/api/shifts/{id}` | Update a shift |
| `DELETE` | `/api/shifts/{id}` | Remove a shift |

Employees require `firstName` and `lastName`; `jobTitle`, `email`, and `contactPhoneNumber` are optional. Shifts require `employeeId`, `date`, `startTime`, and `endTime`. Shifts are for a single date, must have an end later than their start, and may not overlap another shift assigned to that employee. Employee deletion is rejected until their shifts have been removed.

## Build and test

```sh
dotnet build backend/SchedulerApp.Api.csproj
cd frontend
npm run build
npm test -- --watch=false
```

## Database migrations

The API uses Entity Framework Core with SQLite. On startup, it applies pending
migrations to `backend/scheduler.db` (or to the database configured through the
`ConnectionStrings:Scheduler` setting).

Restore the repository's EF Core command-line tool and create migrations from
the repository root:

```sh
dotnet tool restore
dotnet ef migrations add DescribeYourChange --project backend/SchedulerApp.Api.csproj --output-dir Data/Migrations
```

Commit generated migrations with schema changes. The API applies committed
migrations automatically when it starts.

## GitHub workflows and releases

The **Build and test** workflow runs on pushes to `main`/`master`, pull requests, and manual dispatch. It builds the .NET API and Angular frontend and runs the frontend unit tests.

Push a version tag such as `v1.0.0` on a commit in the `main` branch to run the **Publish release package** workflow. Tags pointing to commits outside `main` are skipped. After build and test pass, it publishes a ZIP containing the API, production frontend, and [release instructions](release/README.md), then attaches it to a GitHub Release. The package requires the .NET 10 runtime and Python 3; see its included README for startup instructions.
