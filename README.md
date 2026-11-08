# ComOffice for Windows and Linux

**Word, Excel and PowerPoint files from .NET, on Windows and x64 Linux, in your apps and on your
servers.** Open `.docx`, `.xlsx`, `.xlsm`, `.pptx`, `.csv` and `.tsv` files, render their pages as
images, export them as PDF, read and replace a document's text, compare two documents, read and write
cells, and edit paragraphs, cells and slides through a document model — in your process, with the
native engines for both platforms inside the one package. No Microsoft Office, no LibreOffice, no
network calls.

> **Current release: 1.6.0** — what changed: [CHANGELOG](CHANGELOG.md). Its package,
> `ComOfficeKit.1.6.0.nupkg` (57.0 MB, with the engines for Windows and Linux), is attached to
> [the release](https://github.com/ComOfficeKit/ComOfficeKit-Windows/releases/tag/v1.6.0); `Scripts/fetch-nupkg.ps1 -Tag v1.6.0`
> puts it in `packages/` for the samples.

```csharp
using ComOfficeKit;

License.Activate(licenceKey);                       // the key you received with your licence

using var document = Document.Open("report.docx");  // a WordDocument, a Spreadsheet or a Presentation
document.RenderPage(0, dpi: 144).SavePng("page-1.png");
document.ExportPdf("report.pdf");
```

## What it does

| | Word (.docx) | Spreadsheet (.xlsx, .xlsm, .csv, .tsv) | Presentation (.pptx) |
|---|---|---|---|
| **Open** | from a path or a stream | from a path or a stream | from a path or a stream |
| **Pages** | laid out as Word paginates | the sheet's printed pages, from its page setup | one per slide |
| **Render** | any page as a BGRA32 image, 24 to 600 dpi; PNG | the active sheet's pages | any slide |
| **PDF** | every page, text kept as text | the active sheet's pages | the slides that are not hidden |
| **Read** | the body's text; paragraphs, runs, tables and comments as records; a picture's bytes | each cell as it shows, or its formula; sheets, cells and formats as records | the slide count; slides, shapes and text as records; the deck's layouts |
| **Edit** | replace text everywhere, keeping its formatting; text, formatting, paragraphs, tables and pictures | type into a cell as in Excel; what depends on it recalculates; formats, conditional formats, rows, columns, tables and charts | text and its formatting, shapes, notes; insert a slide from a layout, copy and delete slides |
| **Compare** | two documents, written as tracked changes | — | — |
| **Save** | `.docx`, carrying unchanged parts over | `.xlsx`, `.xlsm`, `.csv`, `.tsv` | `.pptx` |

The whole public API is in [the API overview](docs/getting-started.md#the-api-at-a-glance), and the
document model in [The document model](docs/object-model.md).

## Requirements

| | |
|---|---|
| Windows | Windows 11; Windows Server 2022 and 2025. x64 |
| Linux | x64, with glibc 2.34 or later: Ubuntu 24.04, Debian 12 and Red Hat Enterprise Linux 9 are tested. Not Alpine ([ComOffice on Linux](docs/linux.md)) |
| .NET | .NET 8 or later. The library targets `net8.0` |
| Process | x64 |
| Installed on the machine | on Windows, nothing: the engine and the Visual C++ runtime it needs ship in the package. On Linux, the distribution's libxml2 |
| PDF | nothing more: the engine writes it, with no printer and no Print Spooler service |
| Network | none: licences are verified on the machine |

## Documentation

| | |
|---|---|
| [Getting started](docs/getting-started.md) | install, activate, the first program, the API at a glance |
| [Licensing](docs/licensing.md) | keys and licence files, where an activation is kept, the errors |
| [Rendering and PDF](docs/rendering-and-pdf.md) | page images, DPI and sizes, showing pages in WPF and WinUI, PDF |
| [Spreadsheets](docs/spreadsheets.md) | sheets, cell names, reading and typing into cells, CSV |
| [The document model](docs/object-model.md) | paragraphs, cells and slides as records, and the commands that edit them |
| [Errors and threading](docs/errors-and-threading.md) | what is thrown when, threads, disposal, streams |
| [Deployment](docs/deployment.md) | the native files, publishing, servers and services |
| [Running on a server](docs/servers.md) | worker processes, limits on disk, time and memory, fonts, Windows and Linux containers, documents in a browser |
| [ComOffice on Linux](docs/linux.md) | what a Linux machine needs, publishing for `linux-x64`, fonts, what differs from Windows |
| [Limits and FAQ](docs/limits-and-faq.md) | what this version does not do |

## Samples

| | |
|---|---|
| [Sample-Convert](Samples/Sample-Convert) | a console app: any supported file to PDF and PNG pages |
| [Sample-TemplateFill](Samples/Sample-TemplateFill) | fill a Word template and a workbook, then save them |
| [Sample-WpfPreview](Samples/Sample-WpfPreview) | page images in a WPF window |
| [Sample-ObjectModel](Samples/Sample-ObjectModel) | edit a report, a workbook and a deck through the document model |
| [Sample-ServerWorkers](Samples/Sample-ServerWorkers) | a supervisor and its worker processes convert a folder of documents to PDF; Dockerfiles for a Windows and a Linux container |
| [Sample-WebViewer](Samples/Sample-WebViewer) | an ASP.NET service and its page: a document's pages as images, and the document as PDF, in a browser |

## Free trial and licence

ComOffice is commercial software, quoted and licensed by our sales team. Every release also
publishes a free evaluation key, so you can try the whole SDK without signing up.

1. **How long.** The key below unlocks everything until **2027-01-01** at 00:00 UTC.
   It names no application, so it also works in command-line tools and test runners. A later release publishes a new key.

   ```text
   A/wTnfL8iVOyAMG379JKLVofO3a2QRm8Xk3dhHhyoqBrahUjsNwkMwW/0HuPCBBjU5bRr4qH54EwuO4ktWbkntoNOtAicNTtRbGbaR1umP9vNsYbV9F0ijXSK1q8KO6lFTogPph0RNdnogPKToBkrW5z1AOZ/UeJUb34iT/MxaudYFxNme5g80SbUu3m+1oO4OdvN9ifBjwN9kZS+4gfuEjoK/9XDnSxtHSCHyhTGx7V1m4qaruZ5YIw6K/qYWL34d5ziYBWVewGNoQfYlyLIGrS7gmxSiS9QMDBjbnq+bgXF937MEHWyA0ujHKDxtwLosPI8oLZ1NSW4VAFQIvYsPQU1RxzE+64PWmYdGnEDP9KbTjuURizVFohnOIYhXAo6AgGg8quOZ3xmFihFFn9jYq2SpFUJgAZC0k05IbNShB3mU/cKhp70f3mzawrOoVM3PUiUqulIgajhWa/Gt7mZXw5uGATAOFzunTEAM3g4ZUTLDtpTKuOoWfIWjQaSasgEbQQ9Xg4/3r+aORj1MId+2rmCSz7560nlqFWkACCsdt8paXaWto3VBS2WzrahOEwMBh++TdsSn51QfmpH3qRtAxXgVcJGHgB7S9A8Liucx5cV26PKdl2TTJsKvUdamWrC2l+b6sDxoJ4wc1wj9IhAvPy7bPFBc4Zd2HZKMN5/2wwXwT3aqD04EuuM67JBGr2dFTJAPcQ6eOSTGNzLk1YuB7Ulk6dxH3TmzKtt9wGsKeK3Q/oXO7ABmiNbFWHgTKvsqyDXxHeK7Ri4thUgS3Wxe5s8HX1i6TTW2KMkfcOQhtbO/c31yBBlr9CMJT39fKQzILEGwia819ICvTnnVSesfl4ZmeE8pVwheQHULazVzEfjseekmrp4PJSElsVx7iC88ykv0uQax7th1OHOyLiII0CyuZ07Lkw+R0S2bT34gTiaCstveMZLVejcoHzKN4XQe78CAoB9A1cYf6w6lQtGeERjNxsVewLRS4Y5j3DtT9XgNXQE60Jo0zK6p5pLpAWW87aOJwRGeVLTzPa//4gKQ==
   ```

2. **How to apply it.** Pass it to `License.Activate` before the first document;
   [Licensing](docs/licensing.md) covers where an activation is kept, the licence states and the
   errors. Once the key has expired, every operation on a document throws `LicenseException` until
   a licence key is applied.
3. **What it is for.** Evaluation only: build prototypes, run your tests and the samples. Do not
   ship, sell or distribute anything built on an evaluation key. The
   [Service Terms](https://comofficekit.com/service-terms/) and [LICENSE.md](LICENSE.md) apply.
4. **Buying a licence.** For a production licence or a quote, email our sales team at
   **sales@comofficekit.com**.

## Support

support@comofficekit.com
