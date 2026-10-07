# Scheduler App release package

This archive contains the published ASP.NET Core API in `api/` and the production Angular site in `frontend/`.

## Requirements

- .NET 10 runtime
- Python 3 (to serve the static frontend)

## Run

Start the API from the `api` directory:

```sh
cd api
dotnet SchedulerApp.Api.dll --urls http://localhost:5000
```

In another terminal, serve the frontend:

```sh
cd frontend
python3 -m http.server 4200
```

Open <http://localhost:4200>. The API creates a local `scheduler.db` in its working directory. Keep the API at port 5000 and the frontend at port 4200; those are the configured API and CORS origins.
