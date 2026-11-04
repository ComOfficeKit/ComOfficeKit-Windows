# ComOffice on Linux

The `ComOfficeKit` package runs on Linux as it does on Windows: the same API, with the engine for Linux in
`runtimes/linux-x64/native`. This page is what a Linux machine needs, how to publish for it, and what differs from
Windows. Running it as a service, in worker processes and in containers, is [Running ComOffice on a
server](servers.md).

## What the machine needs

- **x64 Linux with glibc 2.34 or later.** Tested: Ubuntu 24.04, Debian 12 and Red Hat Enterprise Linux 9 (in the
  Universal Base Image 9). Other distributions with glibc 2.34 or later are expected to work. Distributions built on
  musl, such as Alpine, are not supported, nor are older ones such as Red Hat Enterprise Linux 8.
- **.NET 8 or later.**
- **libxml2**, the distribution's own: `apt-get install libxml2` on Ubuntu and Debian, `dnf install libxml2` on Red
  Hat Enterprise Linux. Many images have it already.

Nothing else is needed. The machine needs no fonts to run the engine, though documents look best in their own
(Fonts, below).

## Add the package and publish

The package is not on nuget.org. Each release attaches it, a `.nupkg` file, to [the release on
GitHub](https://github.com/ComOfficeKit/ComOfficeKit-Windows/releases), as on Windows. Put it in a folder beside your
project, `packages/` here:

```bash
VERSION=<version>    # the release's version: its tag is v<version>
mkdir -p packages
curl -fL -o "packages/ComOfficeKit.$VERSION.nupkg" \
  "https://github.com/ComOfficeKit/ComOfficeKit-Windows/releases/download/v$VERSION/ComOfficeKit.$VERSION.nupkg"
```

`gh release download "v$VERSION" -R ComOfficeKit/ComOfficeKit-Windows -p '*.nupkg' -D packages` does the same. For
the samples, do it at the repository's root: their `nuget.config` looks in `packages/` there.

Then a `nuget.config` beside your project, like the samples', takes ComOfficeKit from that folder alone and
everything else from nuget.org:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="comofficekit" value="packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="comofficekit">
      <package pattern="ComOfficeKit" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
```

With the mapping, no other source can hand you a package of the same name. Add the package and publish:

```bash
dotnet add package ComOfficeKit --version "$VERSION"
dotnet publish -c Release -r linux-x64 --self-contained false -o publish
```

Publishing for `linux-x64` puts `libOfficeEngine.so`, and the fonts it carries, beside your application. A build
without a runtime identifier keeps them in `runtimes/linux-x64/native`, where .NET finds them as well. Keep the
fonts with the engine: the engine looks for them in its own folder.

## Activate

As on Windows:

```csharp
License.Configure(LicenseStorage.Memory);
License.Activate(licenceKey);
```

- A licence names the platforms it covers. One that does not cover Linux is refused with
  `LicenseError.PlatformMismatch`.
- On a server, keep the licence in memory and activate it at every start ([servers.md](servers.md)).
- `LicenseStorage.Persistent` keeps it in a file, `com.comofficekit.license.json` (or the identifier given to
  `License.Configure`, with `.json`), in `$XDG_DATA_HOME/comofficekit`, which is `~/.local/share/comofficekit` by
  default. The folder is the user's alone (mode `0700`), and so is the file (`0600`): the user who activated the
  licence can read it, and so can root, but no one else. The key is in the file as it was given, not encrypted,
  as a credential in a dotfile is; on Windows the same choice is encrypted for that user. On a server, prefer
  `LicenseStorage.Memory`, which writes nothing.

## Fonts

The engine lays a document out in the fonts it names. It looks for each family, in this order:

1. in the folders `EngineOptions.FontDirectories` names;
2. in the two the package carries beside the engine: Carlito, which has Calibri's widths, and Caladea, which has
   Cambria's;
3. in the system's fonts: `~/.local/share/fonts`, `~/.fonts`, then `/usr/local/share/fonts` and `/usr/share/fonts`.

A family found nowhere is drawn in a stand-in, so its lines can break and its pages fill differently from the
author's. A stand-in with the same widths is chosen first where the machine has one: Liberation Sans for Arial,
Liberation Serif for Times New Roman, Liberation Mono for Courier New. Once a document has been laid out,
`Document.FontSubstitutions` lists each family it asked for and the one it was drawn in.

The package carries no fonts for Chinese, Japanese or Korean. For documents in them, install Noto Sans CJK or Source
Han Sans; the engine draws such text in them when the document's own font is missing.

| | Ubuntu, Debian | Red Hat Enterprise Linux |
|---|---|---|
| Liberation | `fonts-liberation2` | `liberation-fonts` |
| Noto Sans CJK | `fonts-noto-cjk` | `google-noto-sans-cjk-ttc-fonts` |

The Universal Base Image's own repositories carry neither of Red Hat's: on an image built on it, add the fonts from
Red Hat's full repositories, or copy the font files into the image.

If no font can be found at all, not even the package's, laying a document out throws `ComOfficeKitException` with
`ErrorKind.Internal` and a message that starts "the text engine could not start: no fonts", naming the folders it
searched. The process goes on, and the next document looks again.

With a working directory (`EngineOptions.WorkingDirectory`), the engine keeps an index of the fonts it found there,
under `PortableRender/FontIndex`, so a later start reads only the font files that changed.

## What differs from Windows

- **Fonts:** Windows draws in the fonts installed on it, and `EngineOptions.FontDirectories` is for Linux.
- **Pictures:** on Linux the engine draws pictures in PNG, JPEG, GIF and BMP. Pictures in other formats, TIFF and the
  Windows metafiles EMF and WMF among them, are left out of page images and PDFs.
- **The licence's own file**, when kept with `LicenseStorage.Persistent`: above.

Everything else is the same: what a document can do, the errors, the limits and the cancellation in
[servers.md](servers.md).

## When the engine does not start

The first call that needs the engine throws `ComOfficeKitException` with `ErrorKind.EngineMismatch` when
`libOfficeEngine.so` cannot be loaded. The message ends with the system's own reason:

| The reason says | What to do |
|---|---|
| `libxml2.so.2: cannot open shared object file` | install the distribution's libxml2 |
| ``version `GLIBC_2.34' not found`` | use a distribution with glibc 2.34 or later |
| `libOfficeEngine.so: cannot open shared object file` | publish for `linux-x64`, or keep `runtimes/linux-x64/native` beside the application |

On a machine that is not x64 Linux or Windows, the first call throws `PlatformNotSupportedException`.
