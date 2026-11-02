# escape=`

# This sample in a Windows container: docs/servers.md, "In a Windows container", builds and runs it.
#
# The images have no .NET, so publish the sample self-contained into publish\, beside this file, before building:
#
#   dotnet publish -c Release -r win-x64 --self-contained -o publish
#   docker build -t comofficekit-workers .
#
# BASE is the smaller Windows Server image of 2025, for a Windows Server 2025 host. A container runs the host's
# version of Windows, so on Windows Server 2022 build on the full image of 2022 instead; the smaller one of 2022
# draws no page images:
#
#   docker build --build-arg BASE=mcr.microsoft.com/windows/server:ltsc2022 -t comofficekit-workers .
ARG BASE=mcr.microsoft.com/windows/servercore:ltsc2025
FROM ${BASE}

WORKDIR C:\app
COPY publish/ .

# The arguments are the sample's: <input folder> <output folder> [workers] [seconds].
ENTRYPOINT ["C:\\app\\Sample-ServerWorkers.exe"]
