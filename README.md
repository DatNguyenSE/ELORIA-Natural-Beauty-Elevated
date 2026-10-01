# ELORIA
### Natural Beauty, Elevated

[View the website](https://dcmlfhmb5y305.cloudfront.net/) · [Project overview](#1-project-overview) · [Run locally](#2-run-locally)

## 1. Project overview

ELORIA is a natural hair-care storefront with product browsing, combos, shopping carts, orders, VNPay checkout, customer accounts, and an admin area.

- **Frontend:** Angular 20 · TypeScript · Tailwind CSS
- **Backend:** ASP.NET Core 9 · Entity Framework Core · PostgreSQL
- **Integrations:** Cloudinary images · Email OTP · Hangfire background jobs

The database is already hosted. **You do not need to install PostgreSQL or create a local database.**

```text
ELORIA-DEPLOY/
├── SportZone.Client/           Angular frontend
│   ├── src/app/                Pages, components, and services
│   └── public/                 Static images and logo
├── SportZone.API/
│   ├── API/                    API endpoints and startup configuration
│   ├── Application/            Business logic, DTOs, and interfaces
│   ├── Domain/                 Entities
│   └── Infrastructure/         Database access and external services
└── .github/workflows/          Build and deploy to AWS
```

> The `SportZone` folder names come from the original project. The storefront brand is ELORIA.

## 2. Run locally

### Before you start

- Install **Node.js 22.x (22.12+)** with npm.
- Install **.NET SDK 9** if you want to run the backend.
- Keep an internet connection: the API/database and other services are hosted.

Run the commands below in **PowerShell**, starting from the repository root.

### A. Frontend — start here

```powershell
cd SportZone.Client

# Install packages after cloning, or when dependencies change.
npm ci

# Start the Angular development server.
npm start
```

Open **[http://localhost:4200](http://localhost:4200)**.

`npm start` runs `ng serve`. You can also use `npx ng serve`, or `ng serve` if Angular CLI is installed globally.

> **Ready to explore:** the current development configuration already calls the hosted API. You can browse and try the app with only the frontend running.

### B. Backend — when working on API code

Open a **second terminal** at the repository root:

```powershell
cd SportZone.API/API

# Restore missing .NET packages.
dotnet restore

# Trust the local HTTPS certificate (first-time setup).
dotnet dev-certs https --trust

# Run the API and reload when code changes.
dotnet watch run --launch-profile https
```

Open **[Swagger](https://localhost:5144/swagger)** to test the API.

The backend uses the hosted database. Obtain the existing connection settings and secrets from the project maintainer before running it: at minimum, `ConnectionStrings:DefaultConnection` and `TokenKey` must be configured locally, plus the relevant service credentials for OTP, uploads, and payments. Deployment secrets are not automatically available on your computer.

To make Angular call **your local backend**, update `SportZone.Client/src/app/environments/environment.development.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5144/api/',
  hubUrl: 'https://localhost:5144/hubs/'
};
```

Restart the frontend after changing its environment configuration. Without this change, Angular continues calling the hosted API even when `dotnet watch` is running.

### Everyday commands

**Frontend** — inside `SportZone.Client`:

```powershell
npm ci          # Install/restore packages from the lockfile
npm start       # Start local development (ng serve)
npm run build   # Build the production frontend
```

**Backend** — inside `SportZone.API/API`:

```powershell
dotnet restore                            # Restore packages
dotnet watch run --launch-profile https    # Start with automatic reload
```

Press **Ctrl + C** in each terminal to stop it. The frontend production build goes to `SportZone.API/API/wwwroot`.

### Things to know

> **Refreshing may sign you out on local.** The user is held in memory, and restoring the session depends on a refresh-token cookie. The default local setup calls the hosted API over HTTP, while this cookie requires HTTPS. You may need to sign in again after F5; your account is still saved in the database.

- **Shared data:** local testing uses the hosted data, so orders and admin edits can be visible to everyone using the project.
- **Missing Angular packages:** if you see `@angular/build:dev-server` missing, run `npm ci` inside `SportZone.Client`, then restart.
- **`ng` is not recognized:** use `npm start` after installing packages.
- **Local API fails to start:** check the .NET SDK, hosted database access, and local connection/secret configuration. Keep secrets out of Git.
- **Old logo or images:** try **Ctrl + Shift + R**. Browser or CDN cache may still contain an older file.
- **Local changes are not live changes:** the deployment workflow runs when changes are pushed to `main`.
