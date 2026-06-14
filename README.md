# Pinmo

Endpoint ping monitor built with **ASP.NET Core** (C# backend) and **Electron** (desktop UI).

## Features

- **Dashboard** — live status overview, up/down counts, manual ping
- **Add Endpoints** — register URLs with method, interval, and enable/disable
- **History** — paginated ping log with filters
- **Settings** — default interval, timeout, retention, auto-start monitoring

## Prerequisites

- [.NET SDK 10+](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)

## Run the app

```bash
# 1. Build the backend
dotnet build

# 2. Install Electron dependencies
cd app
npm install

# 3. Start the desktop app (spawns the C# API automatically)
npm start
```

Development mode (opens DevTools):

```bash
npm run dev
```

## Architecture

```
pinmo/
├── src/
│   ├── Pinmo.Api/           # ASP.NET Core REST API (port 5199)
│   ├── Pinmo.Core/          # Domain models & DTOs
│   └── Pinmo.Infrastructure/ # EF Core SQLite, ping service, background monitor
└── app/                     # Electron shell + HTML/CSS/JS UI
```

The Electron main process starts `dotnet run --project src/Pinmo.Api` and waits for `/api/health` before opening the window.

Data is stored in SQLite at `%LOCALAPPDATA%\Pinmo\pinmo.db`.

## API endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/dashboard` | Dashboard summary |
| GET/POST | `/api/endpoints` | List / create endpoints |
| PUT/DELETE | `/api/endpoints/{id}` | Update / delete |
| POST | `/api/endpoints/{id}/ping` | Manual ping |
| GET | `/api/history` | Paginated history |
| GET/PUT | `/api/settings` | App settings |
