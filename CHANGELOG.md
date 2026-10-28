# Changelog

## 1.0.0

The first release of ComOfficeKit for Windows, for .NET 8 and later on Windows x64. From this version
the API keeps to semantic versioning: a 1.x version adds and deprecates, and nothing your code compiles
against is removed or changed before 2.0. The document model is the exception, below.

- **Documents:** open `.docx`, `.xlsx`, `.xlsm`, `.pptx`, `.csv` and `.tsv` from a path or a stream;
  create blank `.docx`, `.xlsx` and `.pptx` files; save, carrying unchanged parts over.
- **Pages:** count them, render any page as a BGRA32 image from 24 to 600 dpi, save it as PNG, and
  export the document as PDF, which the engine writes itself: no printer, no Print Spooler service.
- **Word:** read the body's text, replace text everywhere in one edit keeping its formatting, and
  compare two documents into tracked changes.
- **Spreadsheets:** list sheets, choose the active one, read cells as shown or as typed, type into
  cells as in Excel with recalculation.
- **Presentations:** count slides.
- **The document model (preview):** a Word document's paragraphs, runs, tables and comments, a
  workbook's sheets, cells and formats, and a deck's slides, shapes and text, read as records; commands
  that edit text, formatting, tables, rows and columns, shapes, notes and slides. See
  [The document model](docs/object-model.md). It is a preview: its types and members can still change
  in a 1.x version.
- **Licensing:** a key or a licence file (`.lic`, XML or plain text), verified on the machine; kept per
  Windows user, or in memory. A licence bound to one application names the assembly that activates it.
- **Package:** one native DLL with everything it needs, and the Visual C++ runtime; nothing installed
  on the machine.

Not in this version: printing to a printer, a UI control.
See [Limits and FAQ](docs/limits-and-faq.md).
