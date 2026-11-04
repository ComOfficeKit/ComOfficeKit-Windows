# Getting started

## Install

The package is `ComOfficeKit`, for `net8.0` and later, on x64 Windows and Linux.

Each release attaches its package, a `.nupkg` file, to the release on GitHub; it is not on nuget.org.
Put it in a folder, `packages/` here:

```powershell
Scripts/fetch-nupkg.ps1 -Tag <release tag>          # into packages/
```

On Linux, or without PowerShell, `curl` does the same: [ComOffice on
Linux](linux.md#add-the-package-and-publish).

Then a `nuget.config` beside your project takes ComOfficeKit from that folder alone and everything else
from nuget.org, like the samples':

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

With the mapping, no other source can hand you a package of the same name. Add the release's version:

```
dotnet add package ComOfficeKit --version <version>
```

## Activate

```csharp
using ComOfficeKit;

License.Activate(licenceKey);
```

Activate once, before the first document. The activation is kept for the current user, so
the next start of your application finds it; [Licensing](licensing.md) has the choices. Until a
licence is active, every operation on a document throws `LicenseException`.

## The first program

```csharp
using ComOfficeKit;

License.Activate(args[0]);

using var document = Document.Open(args[1]);
Console.WriteLine($"{document.Kind}: {document.PageCount} page(s)");

for (int page = 0; page < document.PageCount; page++)
{
    document.RenderPage(page, dpi: 96).SavePng($"page-{page + 1}.png");
}
document.ExportPdf(Path.ChangeExtension(args[1], ".pdf"));
```

The engine is an x64 native library, so the process must be x64. On x64 Windows and Linux a .NET
application is an x64 process by default. On Arm64 Windows, build with
`<PlatformTarget>x64</PlatformTarget>` or publish for `win-x64`; an Arm64 process fails at the first call
with `PlatformNotSupportedException`. Arm64 Linux is not supported.

## The API at a glance

| Type | What it is |
|---|---|
| `Document` | an open file. `Open(path)`, `Open(stream, format)`, `CreateBlank(format, path)`; `Kind`, `Format`, `SourcePath`, `PageCount`, `RenderPage`, `ExportPdf`, `Save`, `SaveAs`, `Dispose` |
| `WordDocument` | a `.docx`: `GetText()`, `ReplaceAll(find, replacement)`, and the static `Compare(original, revised, destination)`. The model: `GetScene`, `GetBody`, `BodyCount`, `GetComments`, `CommentCount`, `EditComment`, `SetCommentResolved`, `GetImageData`, `GetFloatingImageData`, `SceneEditor`, `Caret`, `SelectionAnchor`, `ModelRevision` |
| `Spreadsheet` | a workbook: `SheetNames`, `ActiveSheet`, `GetCellText`, `GetCellInput`, `SetCellInput`. The model: `GetWorkbook`, `GetSheet`, `GetCells`, `GetCellCount`, `SceneEditor`, `SaveSync`, `ModelRevision` |
| `Presentation` | a deck: `SlideCount`. The model: `GetSlides`, `GetSlide`, `GetLayouts`, `GetLayoutChoices`, `GetDefaultLayoutChoice`, `SceneEditor`, `ModelRevision` |
| `PageImage` | a rendered page: `Width`, `Height`, `Stride`, `Dpi`, `Pixels` (BGRA32), `SavePng` |
| `License` | `Activate`, `ActivateFromFile`, `Configure`, `Deactivate`, `State`, `LicensedTo`, `Expires`, `CheckUnlocked` |
| `Engine` | `Version`: the native engine's version |

The members marked "the model" read and edit a document's content through the records and commands
in `ComOfficeKit.Model`: see [The document model](object-model.md).

Errors are `ComOfficeKitException` with an `ErrorKind`, and `LicenseException` with a `LicenseError`;
see [Errors and threading](errors-and-threading.md).

`Document.Open` returns the type for the file, so cast to reach what only that kind can do:

```csharp
switch (Document.Open(path))
{
    case WordDocument word:
        Console.WriteLine(word.GetText());
        break;
    case Spreadsheet book:
        Console.WriteLine(book.GetCellText(book.ActiveSheet, "A1"));
        break;
    case Presentation deck:
        Console.WriteLine($"{deck.SlideCount} slides");
        break;
}
```

(Dispose each document when you are done with it; `using` does it for you.)
