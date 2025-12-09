# RestEleven

RestEleven ist eine komplette Zeiterfassungs-Suite bestehend aus einem Blazor WebAssembly PWA-Client, einem Push-Server für Web-Push-Subscriptions sowie einer serverseitigen Personio-Bridge, die Attendances sicher an die Personio-API weiterleitet.

## Architektur

- **RestEleven.Client** – Blazor WASM PWA mit lokaler SQLite-Datenbank (OPFS), Lernlogik für Vorschläge, Reminder-Einstellungen und History.
- **RestEleven.Shared** – DTOs, Lernmodelle und Result-Typen für Client und Server.
- **RestEleven.PushServer** – ASP.NET Core Web API (klassische Controller) mit Subscription-Store, WebPush-Versand & Swagger.
- **RestEleven.PersonioBridge** – ASP.NET Core Web API (klassische Controller) mit HttpClient + Polly für Personio.
- **tests/RestEleven.Tests** – xUnit-Tests für Lernlogik, SQLite-Storage und Benachrichtigungs-Fallbacks.

```
Client (Blazor PWA) --push/notify--> PushServer --VAPID--> Browser
Client (HTTP) ---------------------> PersonioBridge --REST--> Personio API
```

## Voraussetzungen

- .NET SDK 10.0 (Preview)
- PowerShell 7+ (für das Setup-Skript)
- Docker (optional für Container-Builds)

## Projekt-Setup

Alle CLI-Schritte sind im Skript `scripts/setup.ps1` automatisiert. Ausführen im Repo-Stamm:

```powershell
pwsh ./scripts/setup.ps1
```

Das Skript erzeugt (oder erzwingt mit `-Force`) exakt die folgenden Befehle:

```bash
dotnet new sln -n RestEleven
dotnet new blazorwasm -n RestEleven.Client --pwa -f net10.0
dotnet new classlib -n RestEleven.Shared -f net10.0
dotnet new webapi -n RestEleven.PushServer -f net10.0 --use-controllers true
dotnet new webapi -n RestEleven.PersonioBridge -f net10.0 --use-controllers true
dotnet sln RestEleven.sln add RestEleven.Client/RestEleven.Client.csproj RestEleven.Shared/RestEleven.Shared.csproj RestEleven.PushServer/RestEleven.PushServer.csproj RestEleven.PersonioBridge/RestEleven.PersonioBridge.csproj tests/RestEleven.Tests/RestEleven.Tests.csproj
dotnet add RestEleven.Client/RestEleven.Client.csproj reference ../RestEleven.Shared/RestEleven.Shared.csproj
dotnet add RestEleven.PushServer/RestEleven.PushServer.csproj reference ../RestEleven.Shared/RestEleven.Shared.csproj
dotnet add RestEleven.PersonioBridge/RestEleven.PersonioBridge.csproj reference ../RestEleven.Shared/RestEleven.Shared.csproj
```

## Konfiguration

### Client (`RestEleven.Client/wwwroot/appsettings.json`)
- `Client.PushServerBaseUrl` – HTTPS-URL des PushServers.
- `Client.PersonioBridgeBaseUrl` – HTTPS-URL der PersonioBridge.
- `Client.VapidPublicKey` – öffentlicher VAPID-Key für `navigator.pushManager`.

### PushServer (`RestEleven.PushServer/appsettings*.json`)
- `Vapid.Subject/PublicKey/PrivateKey` – mit `npx web-push generate-vapid-keys` generieren.
- `Cors.AllowedOrigins` – exakt die Origins des Blazor-Clients eintragen.

### PersonioBridge (`RestEleven.PersonioBridge/appsettings*.json`)
- `Personio.ClientId`, `ClientSecret`, `EmployeeId` – aus Personio Developer Console.
- optional per Secret-Manager: `dotnet user-secrets set "Personio:ClientId" "value" --project RestEleven.PersonioBridge`.

## Entwicklung & Start

In drei Terminals:

```bash
# PushServer
 dotnet run --project RestEleven.PushServer

# PersonioBridge
 dotnet run --project RestEleven.PersonioBridge

# Blazor Client (mit Hot Reload)
 dotnet watch --project RestEleven.Client run
```

Die PWA ist unter `https://localhost:5001` erreichbar, nutzt lokale SQLite-Datei `resteleven.db` (OPFS) und registriert Service-Worker inkl. Periodic-Sync.

## Tests

```bash
dotnet test --configuration Release
```

Tests decken die Lernlogik (EMA + 11h-Regel), SQLite-CRUD sowie JS-Fallbacks des Notification-Service ab.

## Docker

```
docker build -t resteleven/pushserver -f RestEleven.PushServer/Dockerfile .
docker run -p 8081:8080 resteleven/pushserver

docker build -t resteleven/personiobridge -f RestEleven.PersonioBridge/Dockerfile .
docker run -p 8082:8080 resteleven/personiobridge
```

## Continuous Integration

Die GitHub Action `.github/workflows/ci.yml` führt Restore, Build, Tests (inkl. Coverage) sowie Docker-Builds für beide APIs auf jedem Push/PR nach `main` aus.
