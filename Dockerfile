# syntax=docker/dockerfile:1

# Inkarr Docker image.
#
# NOTE: this Dockerfile has NOT been build/run-verified in an actual Docker
# environment (Docker is not available in the environment that authored it).
# The linux-x64 backend publish it's built on WAS independently verified with
# a real `dotnet publish` outside a container. Smoke-test before relying on
# this in production. See DOCKER.md for details on what is and isn't proven.

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:8.0
ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:6.0
ARG NODE_IMAGE=node:20-bookworm

# ---- Frontend build ----
FROM ${NODE_IMAGE} AS frontend
WORKDIR /src
COPY package.json yarn.lock ./
COPY frontend ./frontend
RUN corepack enable && yarn install --frozen-lockfile --network-timeout 120000
RUN yarn run build --env production

# ---- Backend build ----
FROM ${DOTNET_SDK_IMAGE} AS backend
WORKDIR /src
COPY . .
# Backend publish only needs the .NET solution; the frontend's own build
# (yarn/webpack) runs in the separate `frontend` stage above and its output
# is copied in below. Publishing the console host (net6.0, cross-platform)
# is the correct entry point for a headless Linux/Docker deployment -- NOT
# the Windows-only tray app (NzbDrone/Readarr.csproj) or ServiceHelpers
# projects, neither of which build for linux-x64.
#
# -p:SolutionDir is required for the StyleCop ruleset to resolve correctly
# when publishing a single project directly rather than the full .sln --
# this is a known, pre-existing quirk of this repo's build configuration
# (see Pull List task setup-stylecop), not something specific to Docker.
RUN dotnet publish src/NzbDrone.Console/Inkarr.Console.csproj \
      -c Release -f net6.0 -r linux-x64 --self-contained false \
      -p:SolutionDir=/src/src/ \
      -o /app/publish

# ---- Runtime ----
FROM ${DOTNET_RUNTIME_IMAGE} AS runtime

# libsqlite3-0: the Servarr-maintained SQLite provider this app uses
# (Servarr.System.Data.SQLite.Core.Servarr) ships no bundled native library
# for linux-x64 in its NuGet package (confirmed by inspecting the installed
# package contents) -- it expects the OS to provide libsqlite3, consistent
# with the same requirement documented for Sonarr/Radarr's own Docker images.
# ca-certificates: outbound HTTPS to comicvine.gamespot.com and indexers.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libsqlite3-0 ca-certificates curl \
    && rm -rf /var/lib/apt/lists/*

# Run as a fixed non-root user rather than a full PUID/PGID entrypoint
# system (that's a reasonable later enhancement, not a blocker for a first
# working image). The /config volume must be writable by this UID on the
# host, or by `docker run --user`.
RUN groupadd -g 1000 inkarr && useradd -u 1000 -g inkarr -M -d /config inkarr

WORKDIR /app
COPY --from=backend /app/publish ./
COPY --from=frontend /src/_output/UI ./UI

RUN mkdir -p /config && chown -R inkarr:inkarr /app /config
USER inkarr

ENV DOTNET_RUNNING_IN_CONTAINER=true
VOLUME ["/config"]
EXPOSE 8787

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8787/ping || exit 1

ENTRYPOINT ["dotnet", "Inkarr.dll", "-nobrowser", "-data=/config"]
