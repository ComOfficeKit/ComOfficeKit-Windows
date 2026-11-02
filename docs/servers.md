# Running ComOfficeKit on a server

A server converts documents it did not write, many at a time, with nobody at the screen. This page is how to run
ComOfficeKit there: in worker processes, each in a folder of its own, with limits on disk and time, and with the fonts
your documents use.

## Worker processes, not threads

The engine lays documents out one at a time within a process: layout keeps process-wide state, so two documents laid
out at once take turns, and so do drawing and PDF, which lay text out as they go. Threads in one process therefore
do not make conversions faster; processes do. Run one worker process per document you want converted at once,
usually one for each processor the machine has, and give each its own work.

Processes also keep a bad document from taking the service down. A document that makes a worker crash, or run past
its time, ends that worker, not the others: a supervisor starts another.

`Samples/Sample-ServerWorkers` is such a supervisor and its workers, in one console program.

## At each worker's start

```csharp
Engine.Configure(new EngineOptions
{
    WorkingDirectory = Path.Combine(workRoot, $"worker-{Environment.ProcessId}"),
    WorkingDirectoryQuota = 2L << 30,                  // 2 GB
});
License.Configure(LicenseStorage.Memory);
License.Activate(licenceKey);                          // a secret the service is given
```

- **`Engine.Configure`** comes first, once, before the first document. It cannot be called again in that process.
- **The working directory** is where a document unpacks while it is read or saved, where a file is staged before it
  replaces one, and where a document's pictures stay while it is open. ComOfficeKit's own temporary files go there too.
  Give each worker a folder of its own, on a volume with room, that nothing else empties; when a worker ends, its
  supervisor deletes the folder. With a working directory, a document's pictures are deleted when the document is
  disposed.
- **The quota** is the most the engine keeps in that folder at once. An open or a save that would pass it throws
  `ComOfficeKitException` with `ErrorKind.LimitExceeded`, and writes nothing. A document needs about its unpacked size
  to open, the size of its parts uncompressed: typically about five times the file for a .docx or .xlsx, about the
  file's size for a .pptx, at most 26 times across the 1,891 files measured. A save needs that again, and the file's
  size.
- **The licence** is kept in memory and activated at every start: a container has no lasting storage, and a service's
  account is not the one that installed it.

## Limits on every job

| Limit | Where it is |
|---|---|
| A document that inflates far past its size (a zip bomb) | built in: a package may unpack to 64 MB plus 100 bytes for each of its own bytes, and hold at most 10,000 parts; past that it is refused as `Parse`, before anything is written |
| Disk | `EngineOptions.WorkingDirectoryQuota` |
| Time | a `CancellationToken` on the call (below), and the supervisor's own time limit |
| Memory | the process: a container's memory limit, or a Windows job object, ends a worker that grows past it |

### Cancelling a call

Every call that can take long has an overload that takes a `CancellationToken`: `Document.Open`, `GetPageCount`,
`RenderPage`, `ExportPdf`, `Save` and `SaveAs`. Cancel the token from any thread and the call throws
`OperationCanceledException` at its next check, with nothing changed: no document opened, no file replaced.

```csharp
using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(60));
using var document = Document.Open(input, limit.Token);
document.ExportPdf(output, limit.Token);
```

The engine checks as it unpacks each part, reads each block, slide or sheet, lays out the document and writes each
page. It does not stop inside one part being read or one page being drawn, so a supervisor still ends a worker that
runs well past its time.

## Fonts

A document is drawn in the fonts it names when the machine has them. A family the machine lacks is drawn in a
stand-in, so its lines can break and its pages fill differently from the author's. After a document has been laid
out, `Document.FontSubstitutions` lists each family it asked for and the one it was drawn in; log it, and install the
families that keep appearing there.

On Windows the engine uses the fonts installed on the machine; `EngineOptions.FontDirectories` is for other platforms.

## What the engine writes, and where

- Nothing to the console, and no log: the engine prints nothing and keeps no record of the documents it reads.
- In the working directory: what is described above, and nothing once every document is disposed.
- Beside a PDF or a saved file: the new file, staged under a hidden name until it replaces the old one.

## In a Windows container

`Samples/Sample-ServerWorkers` comes with a Dockerfile. The Windows images have no .NET, so publish the sample
self-contained beside it first, then build the image:

```powershell
cd Samples\Sample-ServerWorkers
dotnet publish -c Release -r win-x64 --self-contained -o publish
docker build -t comofficekit-workers .
```

Run it with a folder of documents, a folder for the PDFs and the folder that holds the licence file, each mounted:

```powershell
docker run --rm -v C:\documents:C:\in:ro -v C:\converted:C:\out `
  -v C:\comofficekit:C:\licence:ro -e COMOFFICEKIT_LICENSE_FILE=C:\licence\licence.txt `
  comofficekit-workers C:\in C:\out
```

- **The image.** The Dockerfile builds on `mcr.microsoft.com/windows/servercore:ltsc2025`, for a Windows Server
  2025 host. With process isolation, Windows Server's default, a container's Windows must be the host's version,
  so on Windows Server 2022 build on the full Windows Server image of 2022, since the smaller one draws no page
  images there (see Platforms):
  `docker build --build-arg BASE=mcr.microsoft.com/windows/server:ltsc2022 -t comofficekit-workers .`
- **The licence** is a file, mounted read-only: a key passed with `-e` would show in `docker inspect`. Each worker
  activates it when it starts, as above.
- **Fonts.** A container has the fonts its image carries and no others. The sample prints the
  `Document.FontSubstitutions` of every document it converts; add the families it names to your image.

## Showing documents in a browser

A browser can show what the server makes: each page as an image, and the whole document as PDF, which browsers
display themselves. `Samples/Sample-WebViewer` is such a service and its page. The page sends a document once; the
service opens it and keeps it open; the page then shows each page as it scrolls into view, with a link to the PDF.

```powershell
cd Samples\Sample-WebViewer
dotnet run -- --urls http://localhost:5000
```

| Request | Answer |
|---|---|
| `POST /documents?name=<file name>`, the document as the body | its `id`, its number of `pages`, and the `fonts` the server lacked and what it drew them in |
| `GET /documents/{id}/pages/{n}?dpi=96` | page *n*, counting from 1, as PNG |
| `GET /documents/{id}/pdf` | the document as PDF, with no file name, so the browser shows it |
| `DELETE /documents/{id}` | closes it and deletes the service's copy |

- **The document travels to your server.** The engine runs there, not in the browser, which receives only images
  and PDF.
- **One document at a time per process.** The service takes requests to the engine one at a time, each under a time
  limit and cancelled when the browser goes away (Cancelling a call, above); a cancelled call leaves the document
  open and usable. For more users at once, run several processes and send each document's requests to the process
  that opened it.
- **What it refuses:** a file that is not .docx, .xlsx, .xlsm, .pptx, .csv or .tsv (400), a document the engine cannot
  read (422) or that passes the working directory's quota (413), and a call past its time limit (504). A request's
  body is limited to 30,000,000 bytes, ASP.NET's default.
- **Before it faces the internet,** add your own sign-in: the sample has none.

## Platforms

| | Status |
|---|---|
| Windows 11, x64, as a console program or a service | the worker model above; Windows 10 is expected to work, and is not yet tested |
| Windows Server 2022 and 2025, x64 | tested: the package's own checks pass on both, from opening, changing and saving Word documents, spreadsheets and presentations to page images and PDF |
| Windows containers, x64 | the full Windows Server image, `mcr.microsoft.com/windows/server:ltsc2022` or `:ltsc2025`, works. The smaller `mcr.microsoft.com/windows/servercore:ltsc2025` works from this version; 1.3.0 and earlier do not load in it. In `mcr.microsoft.com/windows/servercore:ltsc2022` documents open, change and save, but page images fail, and PDF is not tested there; use one of the others |
| Linux, and Linux containers | not yet: the Linux package is not released |
