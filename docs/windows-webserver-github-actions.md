# Windows Webserver GitHub Actions Deploy

This repo now includes a deploy workflow at [.github/workflows/roblox-webserver-deploy.yml](/C:/Users/user/RiderProjects/korone-revival/.github/workflows/roblox-webserver-deploy.yml).

It does this:

1. Builds and publishes:
   - `Roblox.Website`
   - `Roblox.ApiProxy`
   - `Roblox.Services.DataStore`
2. Uploads the publish output as a workflow artifact.
3. Copies that bundle to the Windows webserver over SSH.
4. Runs [Deploy-Roblox-WebServices.ps1](/C:/Users/user/RiderProjects/korone-revival/scripts/windows/Deploy-Roblox-WebServices.ps1) on the server.
5. Stops the three Windows services, syncs the new files in, preserves server-local `appsettings*.json` and `game-servers.json`, and starts the services again.

## GitHub Side Setup

Create these repository secrets:

- `WEB_SERVER_HOST`
  - Your Windows webserver public IP or hostname.
- `WEB_SERVER_USER`
  - The Windows account that OpenSSH accepts.
- `WEB_SERVER_SSH_KEY`
  - The private key GitHub Actions will use.
- `WEB_SERVER_PORT`
  - Optional. Defaults to `22`.

Create these repository variables if you want to override defaults:

- `WEB_DEPLOY_ROOT`
  - Default: `C:\KoroneServices`
- `WEB_STAGING_ROOT`
  - Default: `korone-deploy`
- `WEB_APIPROXY_SERVICE`
  - Default: `Roblox.ApiProxy`
  - Must match the actual Windows service name or display name.
- `WEB_WEBSITE_SERVICE`
  - Default: `Roblox.Website`
  - Must match the actual Windows service name or display name.
- `WEB_DATASTORE_SERVICE`
  - Default: `Roblox.Services.DataStore`
  - Must match the actual Windows service name or display name.

## One-Time Windows Server Setup

### 1. Install OpenSSH Server

Make sure the Windows server accepts SSH logins.

### 2. Create a deployment root

Example:

```powershell
New-Item -ItemType Directory -Force -Path 'C:\KoroneServices\Roblox.ApiProxy' | Out-Null
New-Item -ItemType Directory -Force -Path 'C:\KoroneServices\Roblox.Website' | Out-Null
New-Item -ItemType Directory -Force -Path 'C:\KoroneServices\Roblox.Services.DataStore' | Out-Null
```

### 3. Put production config on the server

Because the deploy script preserves `appsettings*.json`, keep your production settings on the server inside those deploy folders.

At minimum:

- `C:\KoroneServices\Roblox.ApiProxy\appsettings.Production.json`
- `C:\KoroneServices\Roblox.Website\appsettings.Production.json`
- `C:\KoroneServices\Roblox.Services.DataStore\appsettings.Production.json`

If you use website game server mappings, also keep this on the server:

- `C:\KoroneServices\Roblox.Website\game-servers.json`

Recommended split:

- `Roblox.ApiProxy`
  - `Authorization`
  - `InternalServiceHosts`
  - `ReverseProxy`
  - `Jwt:Sessions`
  - `Postgres`
  - `Redis`
  - optional `RedisAuthentication`
- `Roblox.Website`
  - keep your existing full production config
- `Roblox.Services.DataStore`
  - `Postgres`
  - `Redis`
  - optional `RedisAuthentication`
  - `Authorization`

### 4. Register the three Windows services

Use NSSM or your preferred service wrapper.

Each service should run from its deployed folder, not from the git checkout.

Suggested ports:

- `Roblox.ApiProxy` -> `http://127.0.0.1:5200`
- `Roblox.Website` -> `http://127.0.0.1:5000`
- `Roblox.Services.DataStore` -> `http://127.0.0.1:5203`

Set environment variables per service:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://127.0.0.1:PORT`

Example NSSM layout:

- Application: `C:\Program Files\dotnet\dotnet.exe`
- Arguments: `C:\KoroneServices\Roblox.ApiProxy\Roblox.ApiProxy.dll`
- Startup directory: `C:\KoroneServices\Roblox.ApiProxy`

Repeat for the other two apps.

After creating the services, verify the exact names with:

```powershell
Get-Service | Where-Object { $_.DisplayName -like '*Roblox*' -or $_.Name -like '*Roblox*' } | Select-Object Name, DisplayName
```

If the names differ from the defaults, set the GitHub repository variables:

- `WEB_APIPROXY_SERVICE`
- `WEB_WEBSITE_SERVICE`
- `WEB_DATASTORE_SERVICE`

## nginx Side

Point nginx at `Roblox.ApiProxy`, not `Roblox.Website`.

Public flow:

- `nginx` -> `Roblox.ApiProxy`
- `Roblox.ApiProxy` -> `Roblox.Website`
- `Roblox.ApiProxy` -> `Roblox.Services.DataStore` for `gamepersistence.pekora.zip`

Keep these headers:

- `Host $host`
- `CF-Connecting-IP $http_cf_connecting_ip`
- WebSocket upgrade headers for upgraded connections

## Workflow Behavior

The deploy workflow triggers on:

- push to `main` when files under `Roblox/` or the deploy workflow/scripts change
- manual `workflow_dispatch`

The workflow always deploys all three web apps together. That is deliberate for now so the proxy, website, and extracted datastore service stay in sync.

The uploaded publish bundle is staged under `%USERPROFILE%\korone-deploy\<run id>` on the server, then synced into `C:\KoroneServices\...`.

## Recommended Rollout

1. Set up the Windows services manually first.
2. Confirm you can run all three apps from `C:\KoroneServices`.
3. Switch nginx to `Roblox.ApiProxy`.
4. Add the GitHub secrets and variables.
5. Run the workflow manually once with `workflow_dispatch`.
6. After that, let pushes to `main` deploy automatically.
