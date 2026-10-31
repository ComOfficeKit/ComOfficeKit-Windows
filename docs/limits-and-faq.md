# Limits and FAQ

## What this version does not do

- **The document model** does not add sheets or slide shapes, put a picture in a text box, read the bytes
  of a picture on a sheet or a slide, or undo an edit
  ([The document model](object-model.md#what-the-model-does-not-do)).
- **No printing to a printer**, and no UI control: the samples show how to put pages on screen.
- **Windows x64 only**, .NET 8 or later. Not .NET Framework, not .NET on macOS or Linux.
- **PDF** is not written as PDF/A or tagged PDF, and is not encrypted, signed or given form fields
  ([Rendering and PDF](rendering-and-pdf.md)).

## By kind of document

**Word**
- `GetText` and `ReplaceAll` cover the body: its paragraphs and table cells. Headers, footers, text
  boxes, footnotes and comments are not included.
- `Compare` compares body paragraphs and table cells, word by word. Not compared: formatting, headers,
  footers, footnotes, text boxes, pictures, comments. A document's own tracked changes are compared as
  accepted, and `CompareResult` says when there were any.
- WMF and EMF pictures are drawn, as vectors: a WMF is converted to EMF first.

**Spreadsheets**
- About 105 of Excel's functions are calculated; dynamic arrays are not ([Spreadsheets](spreadsheets.md)).
- A hidden sheet cannot be made active, and `GetCellText` and `GetCellInput` refuse its cells; the
  document model's `GetSheet` and `GetCells` read them.
- PDF and page images are of the active sheet.

**Presentations**
- `PageCount` and `RenderPage` count hidden slides; `ExportPdf` leaves them out, as PowerPoint prints.

## FAQ

**Does it need Microsoft Office or LibreOffice?** No. The engine reads and writes the files itself.

**Does it use the network?** No, not for documents and not for the licence.

**Are my documents sent anywhere?** No. Everything happens in your process; the only files written are
the ones you ask for, and temporary files for streams.

**Why is `OfficeEngine.dll` 74 MB?** It carries everything it needs, so nothing has to be installed:
the engine, its runtime, and the Unicode data for text in every language (about half of it).

**Can two threads use one document?** Yes; they take turns. Different documents run in parallel
([Errors and threading](errors-and-threading.md)).

**Where do I report a problem?** support@comofficekit.com, with the file if you can share it, and the
exception's message.
