# Pinmo

Endpoint ping monitor built with **ASP.NET Core** (C# backend) and **Electron** (desktop UI).

## Features

- **Dashboard** — endpoint table with latest ping, average ping, and average packet loss
- **Add Endpoints** — register URLs with edit and delete management

Monitoring runs every 1 second with 1 packet per check.

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

Data is stored in the app folder:

- `app/endpoints.json` — monitored endpoints and their latest ping state
- `app/settings.json` — request timeout configuration
- `%LOCALAPPDATA%\Pinmo\pinmo.db` — ping history (SQLite)

## API endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/dashboard` | Dashboard endpoint metrics |
| GET/POST | `/api/endpoints` | List / create endpoints |
| PUT/DELETE | `/api/endpoints/{id}` | Update / delete |
| POST | `/api/endpoints/{id}/ping` | Manual ping |
