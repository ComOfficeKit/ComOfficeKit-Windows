# Errors and threading

## What is thrown

Arguments are checked before the engine is called, with .NET's own exceptions; what only the engine
can find comes as `ComOfficeKitException`.

| Exception | When |
|---|---|
| `ArgumentNullException`, `ArgumentException`, `ArgumentOutOfRangeException` | a value the API cannot take: a null or empty path, a cell name that is not A1 style, a DPI outside 24 to 600, a page past the last. The message names the argument and the rule, for example `dpi must be between 24 and 600 (was 1200)` |
| `ObjectDisposedException` | any member of a document after `Dispose` |
| `InvalidOperationException` | `Save()` on a document opened from a stream, which has no file to save to |
| `PlatformNotSupportedException` | the process is not 64-bit Windows (x64) |
| `LicenseException` | the licence does not allow it; `Error` says why ([Licensing](licensing.md)) |
| `ComOfficeKitException` | everything else the engine reports; `Kind` says what kind |

| `ErrorKind` | Meaning |
|---|---|
| `InvalidArgument` | the engine refused a value, such as text a document cannot store, or a position the document does not have |
| `IO` | a file could not be read or written |
| `UnsupportedFormat` | not a file ComOfficeKit reads or writes |
| `Parse` | the file could not be read as its format |
| `Unsupported` | not available for this document, or not where it was asked |
| `License` | the exception is a `LicenseException` |
| `EngineMismatch` | the native engine is missing, or is not the one the package ships ([Deployment](deployment.md)) |
| `Internal` | the engine failed unexpectedly; the message is the engine's |

The engine's message is passed on unchanged.

## Threads

- **One document, one call at a time.** Each document takes its own lock around every call, so two
  threads calling the same document wait for each other and get what one thread would.
- **Different documents run in parallel.** Open one document per thread for throughput.
- **Laying out takes turns across the process.** The engine lays documents out one at a time, so
  `PageCount` of a new Word document, and its first render, wait for any other layout in progress.
  Rendering pages that are already laid out does not wait. For many conversions at once on a server,
  run several processes.
- **PDF exports** are written in your process, so they follow the same rules: different documents in
  parallel, layouts taking turns.
- **The document model** follows them too: a read or a command is one call on its document.
- **`License` and `Engine`** can be called from any thread.

## Lifetime

- A document owns what it opened in the engine, and any temporary file it made. `Dispose` frees them
  and can be called more than once; use `using`.
- A document that is never disposed is freed when the garbage collector finalises it. Its temporary
  files, if any, are deleted at the next `Document.Open` in the process, or left to Windows' cleanup
  of the temporary folder.
- A `PageImage` holds its own copy of the pixels and outlives its document.

## Streams

- `Document.Open(stream, format)` reads the stream from its current position to its end before it
  returns, and leaves it open. A stream has no extension, so you say the format.
- `SaveAs(stream)` and `ExportPdf(stream)` write at the stream's current position, flush it, and leave
  it open.
- The engine works on files, so a stream goes through a uniquely named file in the temporary folder
  (`Path.GetTempPath()`). The file behind `Open(stream)` and `SaveAs(stream)` is kept until the
  document is disposed, because the next save carries unchanged parts over from it.
