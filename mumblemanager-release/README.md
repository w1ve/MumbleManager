# MumbleManager

**MumbleManager** is a web-based administration interface for [Mumble](https://www.mumble.info/) (Murmur) voice-over-IP servers. It provides a modern, browser-accessible dashboard for managing virtual servers, channels, users, and SSH host connections — all without needing direct access to the server command line.

> **Author:** Gerald Hull, W1VE  
> **License:** MIT  
> **Released:** April 14, 2026

---

## Table of Contents

1. [Features](#features)
2. [Technology Stack](#technology-stack)
3. [Architecture Overview](#architecture-overview)
4. [Prerequisites](#prerequisites)
5. [Quick Start (Docker — Recommended)](#quick-start-docker--recommended)
6. [Configuration Reference](#configuration-reference)
7. [SSL / TLS Setup](#ssl--tls-setup)
8. [Local Development](#local-development)
9. [Project Structure](#project-structure)
10. [First Login & Initial Setup](#first-login--initial-setup)
11. [How It Works](#how-it-works)
12. [Security Notes](#security-notes)
13. [Contributing](#contributing)
14. [License](#license)

---

## Features

- **SSH Host Management** — Add and manage remote Mumble servers reached over SSH tunnels. Credentials are stored per-user and never shared.
- **Virtual Server Administration** — View, start, stop, and configure Murmur virtual servers via the ZeroC ICE API.
- **Channel Tree Editor** — Browse, create, rename, move, and delete channels on live Mumble servers.
- **Channel Templates** — Save named channel layouts as reusable templates and apply them to any server with one click.
- **User Management** — Admin users can create, promote, demote, reset passwords for, and delete application accounts.
- **Real-time Status Bar** — A SignalR-powered status strip shows live connection state and server activity.
- **Email Notifications** — Sends HTML email on account creation, password change, account deletion, and fatal errors.
- **JWT Authentication** — Stateless token-based auth with 8-hour expiry; per-browser session isolation.
- **Swagger UI** — Full OpenAPI documentation available in Development mode at `/swagger`.

---

## Technology Stack

### Backend

| Component | Technology |
|-----------|-----------|
| Runtime | .NET 9.0 (ASP.NET Core) |
| API style | Minimal APIs |
| Database | SQLite via Entity Framework Core 9 |
| Authentication | JWT Bearer tokens (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Real-time | ASP.NET Core SignalR |
| Mumble protocol | ZeroC ICE 3.7 (`zeroc.ice.net`) |
| SSH tunneling | SSH.NET 2024.2 |
| Email | MailKit 4.9 (SMTP / Gmail App Password) |
| Serialization | Newtonsoft.Json 13 |
| API docs | Swashbuckle / Swagger |

### Frontend

| Component | Technology |
|-----------|-----------|
| Framework | React 18 + TypeScript |
| Build tool | Vite 5 |
| State management | Zustand 4 |
| Real-time client | `@microsoft/signalr` 8 |
| Styling | CSS Modules |

### Infrastructure

| Component | Technology |
|-----------|-----------|
| Container runtime | Docker (multi-stage build) |
| Reverse proxy | Nginx 1.27-alpine |
| TLS termination | Nginx + Let's Encrypt certificates |
| Deployment target | Any Linux VPS (Ubuntu 22.04+ recommended) |
| Orchestration | Docker Compose |

---

## Architecture Overview

```
Browser
   │  HTTPS/WSS
   ▼
┌─────────────────────┐
│   Nginx (port 443)  │  ← TLS termination, reverse proxy
└────────┬────────────┘
         │ HTTP :5000
         ▼
┌─────────────────────────────────────────────────────┐
│   ASP.NET Core (MumbleManager.Api)                  │
│                                                     │
│  Minimal API endpoints  ─── JWT auth middleware     │
│  SignalR hub (/hubs/status)                         │
│  Static files (built React SPA served from here)   │
│                                                     │
│  ┌──────────────┐   ┌───────────────────────────┐  │
│  │  SQLite DB   │   │  SSH Tunnel → Murmur ICE  │  │
│  │  (EF Core)   │   │  (ZeroC ICE 3.7)          │  │
│  └──────────────┘   └───────────────────────────┘  │
└─────────────────────────────────────────────────────┘
```

The React SPA is compiled at Docker build time and served as static files by the .NET process — there is no separate Node process in production. Nginx proxies all requests (REST API, SignalR WebSocket upgrades, and the SPA fallback) to the single .NET container.

---

## Prerequisites

- A Linux VPS running **Ubuntu 22.04** or later (other distributions work; only `deploy.sh` is Ubuntu-specific)
- A **domain name** pointed at your server's IP address
- **Port 80 and 443** open in your firewall
- A **Mumble/Murmur server** running somewhere (local or remote) with ZeroC ICE enabled and an ICE secret configured

Optional but recommended:
- A **Gmail account** with 2FA and an App Password for email notifications

---

## Quick Start (Docker — Recommended)

### 1. Clone the repository

```bash
git clone https://github.com/YOUR_USERNAME/mumblemanager.git
cd mumblemanager
```

### 2. Obtain TLS certificates

You need `nginx/certs/fullchain.pem` and `nginx/certs/privkey.pem` before Nginx will start.

```bash
# Install certbot (if not already installed)
sudo apt install certbot

# Obtain a certificate (stop any existing service on port 80 first)
sudo certbot certonly --standalone -d your.domain.com

# Copy certificates into the nginx/certs directory
cp /etc/letsencrypt/live/your.domain.com/fullchain.pem nginx/certs/fullchain.pem
cp /etc/letsencrypt/live/your.domain.com/privkey.pem   nginx/certs/privkey.pem
chmod 600 nginx/certs/privkey.pem
```

### 3. Configure your environment

```bash
cp .env.example .env
nano .env          # Fill in your domain, JWT secret, email credentials
```

See [Configuration Reference](#configuration-reference) for all available values.

### 4. Update nginx.conf with your domain

Edit `nginx/nginx.conf` and replace `YOUR_DOMAIN.com` with your actual domain name in the two `server_name` directives.

### 5. Update docker-compose.yml with your settings

Edit `docker-compose.yml` and replace all `YOUR_*` placeholders with your actual values, or configure them to read from your `.env` file.

### 6. Run the one-shot deployment script

```bash
chmod +x deploy.sh
sudo ./deploy.sh
```

`deploy.sh` will:
1. Install Docker Engine + Docker Compose plugin (if not present)
2. Build the multi-stage Docker image (Node → .NET SDK → .NET runtime)
3. Start the `app` and `nginx` containers
4. Wait for the health check to pass
5. Tail logs briefly so you can confirm a clean start

The app will be available at `https://your.domain.com`.

### Manual Docker Compose (alternative to deploy.sh)

```bash
docker compose build
docker compose up -d
docker compose logs -f
```

---

## Configuration Reference

All runtime configuration is passed as environment variables to the `app` container (see `docker-compose.yml`). The table below lists every variable.

| Variable | Required | Description |
|----------|----------|-------------|
| `ConnectionStrings__Default` | Yes | SQLite connection string. Default: `Data Source=/data/mumblemanager.db` |
| `Jwt__Secret` | **Yes** | JWT signing key — minimum 32 random characters. Generate: `openssl rand -base64 48` |
| `AllowedOrigins` | Yes | Comma-separated CORS origins, e.g. `https://your.domain.com` |
| `Email__SmtpHost` | No | SMTP server hostname. Default: `smtp.gmail.com` |
| `Email__SmtpPort` | No | SMTP port (STARTTLS). Default: `587` |
| `Email__From` | No | Sender email address |
| `Email__FromName` | No | Sender display name. Default: `MumbleManager` |
| `Email__AppPassword` | No | Gmail App Password. Email is disabled when empty. |
| `Email__AdminAddress` | No | Email address for admin notifications |
| `ASPNETCORE_ENVIRONMENT` | No | Set to `Development` to enable Swagger UI. Default: `Production` |

### appsettings.json (development defaults)

The file `backend/appsettings.json` holds defaults for local development. In production, all sensitive values should be supplied via environment variables (which override `appsettings.json`).

---

## SSL / TLS Setup

Nginx handles TLS termination. The configuration in `nginx/nginx.conf`:
- Redirects all HTTP (port 80) traffic to HTTPS (port 443)
- Enables TLSv1.2 and TLSv1.3 with strong ciphers
- Proxies REST API requests and WebSocket (SignalR) upgrades to the .NET container
- Sets a 5-minute read timeout for REST and a 1-hour timeout for WebSocket connections

**Certificate files required:**
- `nginx/certs/fullchain.pem` — Full certificate chain
- `nginx/certs/privkey.pem` — Private key (keep this file private; `chmod 600`)

These paths are **gitignored** and should never be committed to source control.

**Certificate renewal:**  
Let's Encrypt certificates expire after 90 days. Set up auto-renewal:

```bash
sudo crontab -e
# Add:
0 3 * * * certbot renew --quiet && \
  cp /etc/letsencrypt/live/your.domain.com/fullchain.pem /path/to/mumblemanager/nginx/certs/fullchain.pem && \
  cp /etc/letsencrypt/live/your.domain.com/privkey.pem   /path/to/mumblemanager/nginx/certs/privkey.pem && \
  docker compose -f /path/to/mumblemanager/docker-compose.yml restart nginx
```

---

## Local Development

### Backend

```bash
cd backend

# Restore dependencies
dotnet restore

# Run in Development mode (enables Swagger at http://localhost:5000/swagger)
ASPNETCORE_ENVIRONMENT=Development \
  Jwt__Secret="dev-secret-key-at-least-32-characters!!" \
  dotnet run
```

The backend listens on `http://localhost:5000` by default.

### Frontend

```bash
cd frontend

npm install
npm run dev    # Starts Vite dev server at http://localhost:5173
```

The Vite config proxies `/api` and `/hubs` to `http://localhost:5000`, so the backend and frontend dev servers work together automatically.

### Database migrations

Entity Framework Core migrations live in `backend/Data/Migrations/`. To create a new migration after changing models:

```bash
cd backend
dotnet tool install --global dotnet-ef   # first time only
dotnet ef migrations add YourMigrationName
```

The database schema is applied automatically on startup via `DbSeeder.SeedAsync()`.

---

## Project Structure

```
mumblemanager/
│
├── backend/                     # ASP.NET Core 9 API
│   ├── Data/
│   │   ├── AppDbContext.cs      # EF Core database context
│   │   └── Migrations/          # EF Core migration history
│   ├── Endpoints/               # Minimal API route handlers
│   │   ├── AuthEndpoints.cs     # Login / change-password
│   │   ├── UserEndpoints.cs     # CRUD for application users
│   │   ├── HostEndpoints.cs     # SSH host management
│   │   ├── ServerEndpoints.cs   # Virtual server admin
│   │   ├── ChannelEndpoints.cs  # Channel tree operations
│   │   ├── ConnectionEndpoints.cs # ICE connection open/close
│   │   └── TemplateEndpoints.cs # Channel template CRUD
│   ├── Generated/               # Auto-generated ZeroC ICE bindings
│   │   ├── MumbleServer.cs      # ICE bindings for Murmur (current)
│   │   └── V14/Murmur14.cs      # ICE bindings for Murmur 1.4.x
│   ├── Hubs/
│   │   └── StatusHub.cs         # SignalR hub for real-time status
│   ├── Models/
│   │   ├── Models.cs            # EF entities (SshHostEntry, etc.)
│   │   └── UserModels.cs        # AppUser entity
│   ├── Services/
│   │   ├── AuthService.cs       # PBKDF2 hashing + JWT generation
│   │   ├── DbSeeder.cs          # Seeds the initial admin account
│   │   ├── EmailService.cs      # SMTP email via MailKit
│   │   ├── GlobalExceptionHandler.cs
│   │   ├── HostSession.cs       # Per-user ICE session registry
│   │   ├── IMurmurIceService.cs # ICE service interface
│   │   ├── MumbleServerIceService.cs  # Current Murmur ICE impl
│   │   ├── MurmurLegacyIceService.cs # Legacy Murmur 1.4.x ICE impl
│   │   ├── MurmurVersion.cs     # Version detection
│   │   └── SshTunnelService.cs  # SSH port forwarding via SSH.NET
│   ├── appsettings.json         # Development configuration defaults
│   ├── Program.cs               # App bootstrap and DI registration
│   └── MumbleManager.Api.csproj
│
├── frontend/                    # React + TypeScript SPA
│   └── src/
│       ├── api/index.ts         # Typed API client functions
│       ├── components/          # React UI components
│       │   ├── LoginPage.tsx
│       │   ├── ServerPanel.tsx  # Virtual server list & actions
│       │   ├── ChannelTree.tsx  # Interactive channel tree editor
│       │   ├── ConfigPanel.tsx  # Server settings editor
│       │   ├── HostPanel.tsx    # SSH host management
│       │   ├── TemplatePanel.tsx
│       │   ├── UserManagement.tsx
│       │   ├── HostDialog.tsx
│       │   ├── ChangePasswordDialog.tsx
│       │   └── StatusBar.tsx
│       ├── hooks/useSignalR.ts  # SignalR connection hook
│       ├── store/               # Zustand global state
│       ├── types/index.ts       # Shared TypeScript types
│       └── main.tsx             # React entry point
│
├── nginx/
│   ├── nginx.conf               # Nginx reverse proxy configuration
│   ├── certs/                   # TLS certificates (gitignored)
│   │   └── README.txt           # Instructions for cert placement
│   └── logs/                    # Nginx access/error logs (gitignored)
│
├── .env.example                 # Environment variable template
├── .gitignore
├── Dockerfile                   # Multi-stage Docker build
├── docker-compose.yml           # Service orchestration
├── deploy.sh                    # One-shot Ubuntu deployment script
└── README.md
```

---

## First Login & Initial Setup

When the application starts for the first time, `DbSeeder` automatically creates an admin account if no users exist:

| Field | Default Value |
|-------|--------------|
| Username | `admin` |
| Password | `YOUR_STRONG_ADMIN_PASSWORD` (set in `DbSeeder.cs` before deploying) |

**Important:** Change the default admin password immediately after first login using the **Change Password** option in the UI. You can also create additional admin or standard users from the **User Management** panel.

### Adding a Mumble Server

1. Log in with the admin account.
2. Click **Add Host** in the Hosts panel.
3. Enter:
   - **Display Name** — a friendly label for this server.
   - **SSH Host / Port** — the server where Murmur is running.
   - **SSH Username / Password** — credentials to open the SSH tunnel.
   - **ICE Secret** — the `icesecretread`/`icesecretwrite` value from your `murmur.ini`.
4. Click **Connect**. MumbleManager will open an SSH tunnel and connect to the Murmur ICE interface.

### Enabling ICE on Murmur

In your `murmur.ini` (or `mumble-server.ini`), enable the ICE interface:

```ini
ice=tcp -h 127.0.0.1 -p 6502
icesecretread=YOUR_ICE_SECRET
icesecretwrite=YOUR_ICE_SECRET
```

Restart Murmur after making changes. MumbleManager connects to ICE through the SSH tunnel, so Murmur does not need to be exposed publicly.

---

## How It Works

### SSH Tunneling

When you connect to a host, `SshTunnelService` uses SSH.NET to open a port-forwarded tunnel from a random local port on the MumbleManager server to `127.0.0.1:6502` (Murmur's ICE port) on the remote host. The ICE client connects through this local tunnel endpoint.

### ZeroC ICE Integration

Murmur exposes a ZeroC ICE API for programmatic control. The generated C# bindings in `backend/Generated/` were produced from Murmur's Slice definitions. MumbleManager supports both current Murmur (tested with 1.5.x) and legacy Murmur 1.4.x, with automatic version detection at connection time.

### Per-Session Isolation

Each user gets an independent ICE session. The `SessionRegistry` service maintains a dictionary of active `HostSession` objects keyed by a hash of the user's JWT token, ensuring that one user's connection does not affect another's.

### Real-Time Status

`StatusHub` (SignalR) pushes status updates to connected browsers whenever a host connection state changes. The frontend subscribes via `useSignalR.ts` and updates the status bar in real time without polling.

---

## Security Notes

- **Change the default admin password** immediately after first deployment.
- **Generate a strong JWT secret** — at least 48 random bytes. Use: `openssl rand -base64 48`
- **Never commit** `nginx/certs/` or your `.env` file to source control (both are gitignored).
- **SSH credentials** entered in the UI are stored in the SQLite database. Protect your database file (`/data/mumblemanager.db`) with appropriate filesystem permissions.
- The app runs behind Nginx with TLS; the .NET container itself is not exposed directly to the internet.
- Passwords are hashed using PBKDF2 with SHA-256, 100,000 iterations, and a random 16-byte salt.

---

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Commit your changes
4. Push to the branch and open a Pull Request

---

## License

Copyright © 2026 Gerald Hull, W1VE

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
