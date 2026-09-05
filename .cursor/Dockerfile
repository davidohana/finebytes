# Cloud Agent base image for the finebytes / Magic File Renamer (.NET 10 + Avalonia) repo.
# Stable, slow-changing system dependencies live here; repo-specific bootstrap
# (restore/build, tool restore, markdown venv) runs in the environment `install` step.
FROM ubuntu:24.04

ENV DEBIAN_FRONTEND=noninteractive \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_ROOT=/opt/dotnet \
    PATH=/opt/dotnet:/opt/dotnet/tools:/usr/local/bin:$PATH \
    DISPLAY=:99

# System packages: build/runtime deps for Avalonia desktop, a virtual X display
# (Xvfb) so the UI can run headlessly, fonts, Python for the markdown tooling,
# and imagemagick for capturing UI screenshots.
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        ca-certificates curl git \
        xvfb x11-utils x11-apps \
        libx11-6 libice6 libsm6 libfontconfig1 libfreetype6 \
        libgl1-mesa-dri libglx-mesa0 libegl1 libglib2.0-0 libgdiplus \
        fonts-liberation fonts-dejavu-core \
        python3 python3-venv python3-pip \
        imagemagick \
    && rm -rf /var/lib/apt/lists/*

# .NET 10 SDK (installed via the official script; not yet in the default apt feed).
RUN curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh \
    && chmod +x /tmp/dotnet-install.sh \
    && /tmp/dotnet-install.sh --channel 10.0 --install-dir /opt/dotnet \
    && rm /tmp/dotnet-install.sh

# `just` task runner (used by the repo for restore/build/test/format/lint/run-ui).
RUN curl -fsSL https://just.systems/install.sh | bash -s -- --to /usr/local/bin

# The ubuntu:24.04 image already ships a uid 1000 `ubuntu` user; ensure it exists.
RUN id ubuntu >/dev/null 2>&1 || useradd -m -u 1000 -s /bin/bash ubuntu
USER ubuntu
