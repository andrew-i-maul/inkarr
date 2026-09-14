# Running Inkarr in Docker

## Status: not build/run-verified

This Dockerfile and compose file were authored and reasoned through carefully,
but **Docker itself was not available in the environment that created them**,
so the actual `docker build` / `docker run` has never been executed. What
*was* verified directly:

- The backend (`src/NzbDrone.Console/Inkarr.Console.csproj`, the correct
  cross-platform console host -- not the Windows-only tray app or
  ServiceHelpers projects) genuinely publishes for `linux-x64` with a real
  `dotnet publish` and produces a valid Linux ELF binary. No Windows-only
  dependency broke the publish.
- The real CLI flags (`-nobrowser`, `-data=<path>`) and the real default port
  (`8787`) and health endpoint (`/ping`, confirmed in
  `src/Inkarr.Http/Ping/PingController.cs`, `[AllowAnonymous]` so it needs no
  API key) by reading the actual source, not guessing.
- That the SQLite provider this app uses
  (`Servarr.System.Data.SQLite.Core.Servarr`) ships **no bundled native
  library for linux-x64** in its NuGet package -- confirmed by inspecting the
  installed package contents directly. The Dockerfile installs
  `libsqlite3-0` from apt to cover this, which matches the same requirement
  documented for Sonarr/Radarr's own Docker images, but this specific
  combination has not been run-tested here.

**Before relying on this**, build the image and confirm the container
actually starts, serves `/ping`, and can create its database on a fresh
`/config` volume.

## Build

```
docker build -t inkarr .
```

## Run

```
docker run -d \
  --name inkarr \
  -p 8787:8787 \
  -v inkarr-config:/config \
  -v /path/to/your/comics:/comics \
  inkarr
```

Or with the included example:

```
docker compose up -d
```

Edit the `/path/to/your/comics` bind mount in `docker-compose.yml` first --
that placeholder won't exist on your machine.

## Volumes

- `/config` -- database, `config.xml`, logs. Must persist across container
  restarts/recreates or you lose your entire library configuration. The
  container runs as a fixed non-root user (uid/gid 1000); make sure the host
  path (if using a bind mount instead of a named volume) is writable by that
  uid, or override it with `docker run --user`.
- `/comics` (or whatever you name it) -- your actual comic library storage,
  bind-mounted from the host. This is just a mount point; you tell Inkarr
  about it as a Root Folder inside the app itself after first start, same as
  any other library location.

## Port

`8787` (HTTP only in this image -- no SSL termination is configured; put a
reverse proxy in front of it if you need HTTPS).

## First run

A fresh `/config` volume means a fresh install. After the container starts:

1. Open `http://<host>:8787`.
2. Complete the authentication setup (Inkarr requires an authentication
   method to be configured before use, unlike older Servarr defaults).
3. Add your ComicVine API key under Settings → General → Metadata Source
   (free key from comicvine.gamespot.com/api).
4. Add a Root Folder pointing at your mounted `/comics` path (or whatever you
   named the mount).

## Known gaps, deliberately not addressed here

- **No PUID/PGID entrypoint logic.** The image runs as a fixed uid/gid 1000
  non-root user. This is simpler and reasonable for a first working image,
  but doesn't let you match an arbitrary host user the way linuxserver.io-style
  images do. A real enhancement, not a blocker.
- **net6.0 is EOL.** This entire repo currently targets net6.0 (see Pull List
  task tracking the broader upgrade). The Docker runtime base image
  (`mcr.microsoft.com/dotnet/aspnet:6.0`) reflects that; it'll need to move
  to a supported TFM/base image together with the rest of the project, not
  as a Docker-specific fix.
- **No multi-arch build.** Only tested (well -- reasoned about) for
  `linux-x64`. ARM64 (Raspberry Pi, Apple Silicon hosts running Linux
  containers) would need `--platform` build args and confirming the SQLite
  package's ARM64 native behavior separately.
