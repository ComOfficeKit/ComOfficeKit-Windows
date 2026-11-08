# Deployment

## What the package puts in your application

| File | Size | What it is |
|---|---|---|
| `ComOfficeKit.dll` (and its `.xml` documentation) | about 500 KB | the .NET API, `net8.0` |
| `OfficeEngine.dll` | about 75 MB | the native engine: everything it needs is linked in |
| `vcruntime140.dll`, `vcruntime140_1.dll`, `msvcp140.dll` | about 0.9 MB | Microsoft's Visual C++ runtime, which the engine needs |
| `libOfficeEngine.so`, and Carlito's and Caladea's font files | about 66 MB, and 3 MB of fonts | the native engine for Linux, and the two font families it carries |

The native files are in the package under `runtimes/win-x64/native` and `runtimes/linux-x64/native`.
Nothing is installed on the machine. On Windows nothing is loaded from it except Windows itself; on
Linux, the distribution's own libraries, glibc and libxml2 among them ([ComOffice on Linux](linux.md)).

**Where they land:**

- A build without a runtime identifier puts them in `bin\…\runtimes\win-x64\native\`, and .NET loads
  them from there.
- A build or publish for `win-x64` (`-r win-x64`) puts them beside your executable.
- On Linux, a publish for `linux-x64` puts `libOfficeEngine.so` and the fonts beside your application;
  a build without a runtime identifier keeps them in `runtimes/linux-x64/native`. Keep the fonts with
  the engine: it looks for them in its own folder.

Keep the files the package installs together, and of the same version: the API checks at its first
call that the engine is the one it was built with, and throws `EngineMismatch`, naming both
versions, when it is not.

## Publishing

| | Status |
|---|---|
| Framework-dependent build and run | tested |
| Self-contained publish for `win-x64` | tested: nothing outside Windows and the application's folder is loaded |
| Framework-dependent publish for `linux-x64` | tested, in clean Ubuntu 24.04, Debian 12 and Red Hat Enterprise Linux 9 images with only .NET 8 and libxml2 |
| Single-file publish | not yet tested. .NET leaves native libraries out of the single file unless `IncludeNativeLibrariesForSelfExtract` is set; without it, ship `OfficeEngine.dll` and the three runtime DLLs beside the executable |
| Trimming and native AOT | the library is marked trimmable and AOT-compatible; not yet tested in an application |

## Platforms

| | Status |
|---|---|
| Windows 11, x64 | supported and tested |
| Windows 10, x64 | expected to work; not yet tested |
| Windows Server 2022 and 2025, x64 | tested, and in Windows containers: see [Running on a server](servers.md#platforms) |
| Ubuntu 24.04, Debian 12, Red Hat Enterprise Linux 9, x64 | tested, in clean images: see [ComOffice on Linux](linux.md) |
| Other x64 Linux with glibc 2.34 or later | expected to work; not tested |
| Alpine and other musl-based Linux; Red Hat Enterprise Linux 8 | not supported |
| Arm64, x86 | not supported: the engine is x64 only. On Arm64 Windows an x64 process works |

## Servers and services

The engine needs no desktop and no graphics card: pages are drawn in software. [Running on a
server](servers.md) is the whole of it: worker processes, limits on disk, time and memory, fonts,
Windows and Linux containers, and documents in a browser through an ASP.NET service
(`Samples/Sample-WebViewer`). Not yet tested as a Windows service. What to plan for:

- **PDF** needs nothing more: the engine writes it, with no printer and no Print Spooler service
  ([Rendering and PDF](rendering-and-pdf.md)).
- **Throughput:** layouts take turns within a process ([Errors and threading](errors-and-threading.md)),
  so run several worker processes for many conversions at once.
- **The licence** kept with `LicenseStorage.Persistent` belongs to the account that activated
  it. A service runs under its own account: activate under that account, or use
  `LicenseStorage.Memory` and activate at every start ([Licensing](licensing.md)).
- **Temporary files** go to the account's temporary folder, and only for documents opened from or
  saved to streams. With `Engine.Configure`, they go to the working directory you give it
  ([Running on a server](servers.md#at-each-workers-start)).
