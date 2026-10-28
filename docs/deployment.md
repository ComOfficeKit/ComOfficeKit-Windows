# Deployment

## What the package puts in your application

| File | Size | What it is |
|---|---|---|
| `ComOfficeKit.dll` (and its `.xml` documentation) | about 500 KB | the .NET API, `net8.0` |
| `OfficeEngine.dll` | about 74 MB | the native engine: everything it needs is linked in |
| `vcruntime140.dll`, `vcruntime140_1.dll`, `msvcp140.dll` | about 0.9 MB | Microsoft's Visual C++ runtime, which the engine needs |

The native files are in the package under `runtimes/win-x64/native`. Nothing is installed on the
machine and nothing is loaded from it except Windows itself.

**Where they land:**

- A build without a runtime identifier puts them in `bin\…\runtimes\win-x64\native\`, and .NET loads
  them from there.
- A build or publish for `win-x64` (`-r win-x64`) puts them beside your executable.

Keep the files the package installs together, and of the same version: the API checks at its first
call that the engine is the one it was built with, and throws `EngineMismatch`, naming both
versions, when it is not.

## Publishing

| | Status |
|---|---|
| Framework-dependent build and run | tested |
| Self-contained publish for `win-x64` | tested: nothing outside Windows and the application's folder is loaded |
| Single-file publish | not yet tested. .NET leaves native libraries out of the single file unless `IncludeNativeLibrariesForSelfExtract` is set; without it, ship `OfficeEngine.dll` and the three runtime DLLs beside the executable |
| Trimming and native AOT | the library is marked trimmable and AOT-compatible; not yet tested in an application |

## Windows versions

| | Status |
|---|---|
| Windows 11, x64 | supported and tested |
| Windows 10, x64 | expected to work; not yet tested |
| Windows Server | not yet tested |
| Arm64, x86 | not supported: the engine is x64 only. On Arm64 Windows an x64 process works |

## Servers and services

The engine needs no desktop and no graphics card: pages are drawn in software. Not yet tested under
ASP.NET Core or as a Windows service; what to plan for:

- **PDF** needs nothing more: the engine writes it, with no printer and no Print Spooler service
  ([Rendering and PDF](rendering-and-pdf.md)).
- **Throughput:** layouts take turns within a process ([Errors and threading](errors-and-threading.md)),
  so run several worker processes for many conversions at once.
- **The licence** kept with `LicenseStorage.Persistent` belongs to the Windows account that activated
  it. A service runs under its own account: activate under that account, or use
  `LicenseStorage.Memory` and activate at every start ([Licensing](licensing.md)).
- **Temporary files** go to the account's temporary folder, and only for documents opened from or
  saved to streams.
