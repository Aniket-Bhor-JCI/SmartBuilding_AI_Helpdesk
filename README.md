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


## Build Checks

Backend:

```powershell
dotnet build
```

Frontend:

```powershell
npm run build
```
<img width="917" height="802" alt="Screenshot 2026-04-20 143142" src="https://github.com/user-attachments/assets/301596db-1f99-43ee-8401-1cae4f15ada4" />
<img width="902" height="774" alt="Screenshot 2026-04-20 143134" src="https://github.com/user-attachments/assets/487659c2-68bb-42fe-a7ec-0952b5c59805" />
<img width="1192" height="690" alt="Screenshot 2026-04-20 143403" src="https://github.com/user-attachments/assets/780b54b0-64c6-49da-80a8-d4ec21afed84" />
<img width="957" height="897" alt="Screenshot 2026-04-20 143118" src="https://github.com/user-attachments/assets/691cf4e5-a5c3-4a2d-ad2e-b0f24197bdb2" />
<img width="1154" height="909" alt="Screenshot 2026-04-20 143106" src="https://github.com/user-attachments/assets/e28e1bed-a375-4863-b72b-85fc3fe2ef8d" />

