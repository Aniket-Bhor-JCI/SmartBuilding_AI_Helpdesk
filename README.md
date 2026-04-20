# Smart Building Helpdesk

Simple full-stack AI chatbot helpdesk for smart-building support.

## Stack

- Frontend: Angular standalone app
- Backend: ASP.NET Core Web API
- Database: SQL Server / LocalDB
- AI: OpenRouter chat completions

## Features

- Login and register with `User` or `Admin` role
- Chatbot-first support flow
- AI or fallback analysis returns issue, category, location, priority, and suggestion
- Ticket confirmation flow before creation
- User ticket list
- Admin dashboard with counts, assignment, and status updates

## Requirements

- .NET 10 SDK
- Node.js 20 LTS recommended
- SQL Server Express, SQL Server Developer, or LocalDB

Your current backend is configured for LocalDB by default:

```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=SmartBuildingHelpdeskDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

If another laptop does not have LocalDB, change `backend/appsettings.json` to a SQL Server instance that exists on that machine.

Example for SQL Server Express:

```json
"DefaultConnection": "Server=.\\SQLEXPRESS;Database=SmartBuildingHelpdeskDb;Trusted_Connection=True;TrustServerCertificate=True;"
```

## OpenRouter Key

Set the key in PowerShell before starting the backend:

```powershell
$env:OPENROUTER_API_KEY="your_openrouter_api_key"
```

To save it permanently for your Windows user:

```powershell
setx OPENROUTER_API_KEY "your_openrouter_api_key"
```

Then open a new terminal.

If the key is missing or OpenRouter fails, the backend falls back to a simple built-in issue classifier so the app still works.

## Run Locally

### 1. Start backend

From `backend/`:

```powershell
dotnet run
```

Default API URL:

```text
http://localhost:5182
```

### 2. Start frontend

From `frontend/`:

```powershell
npm install
npm start
```

Default frontend URL:

```text
http://localhost:4200
```

Angular uses `frontend/proxy.conf.json`, so requests to `/api` are forwarded to `http://localhost:5182`.

## Demo Accounts

- Admin: `admin@smarthelpdesk.local` / `Admin123!`
- User: `user@smarthelpdesk.local` / `User123!`

These accounts are created automatically on backend startup if they do not already exist.

## Another Laptop Checklist

1. Copy the whole `new_chatbot` folder.
2. Install .NET 10 SDK.
3. Install Node.js LTS.
4. Make sure SQL Server or LocalDB exists.
5. Update `backend/appsettings.json` if that laptop uses a different SQL Server instance.
6. Set `OPENROUTER_API_KEY`.
7. Run `dotnet run` in `backend/`.
8. Run `npm install` and `npm start` in `frontend/`.

## Config You May Need To Change

- Database connection: `backend/appsettings.json`
- Frontend allowed origin: `backend/appsettings.json`
- Frontend proxy target: `frontend/proxy.conf.json`

If you change backend port from `5182`, update `frontend/proxy.conf.json` too.

## Build Checks

Backend:

```powershell
dotnet build
```

Frontend:

```powershell
npm run build
```
