# Changelog

## 1.14.0

Faster workbook saves, three fixes in saved workbooks, saves that leave other documents free, and shapes inside
presentation groups that move. Nothing in the API changes or goes away. The evaluation key in the README works until
2027-01-01.

**Workbooks:**

- **Saving is faster.** A save leaves every sheet nobody edited exactly as the file had it, and in an edited sheet
  writes only the rows whose cells changed, copying everything else as it is; a later save after more edits does the
  same. On Linux, with a workbook of two sheets of 40,000 and 20,000 rows of numbers, a save after one edit took 0.24
  to 0.43 s instead of 2.2 to 2.5 s, and a save with no edit about 0.1 s instead of 2 s.
- **Conditional formatting over a `$` range is kept.** A rule whose range uses `$` (as in `$A33:$XFD33`) was not
  read, so every save that wrote its sheet dropped it.
- **A workbook whose SpreadsheetML is bound to a prefix** (`<x:worksheet>`, as some tools write it): a cell or a style
  that a save added was written in no namespace, so Excel did not read it. What a save adds now takes the part's
  prefix.
- **A cell's style is no longer added again on each save.** Each save that wrote a cell's row appended the cell's
  `<xf>` to the styles once more.

**Presentations:**

- **Shapes inside groups move.** `PresentationSceneEditor.ReplacingFrame` now moves and resizes a shape inside a
  group, and the save writes it into its group, which grows or shrinks to hold its shapes. A shape in a rotated or
  flipped group, or in SmartArt, is still refused.

**All documents:**

- **A save leaves other documents free.** While one document is being saved, other documents can be opened, laid out
  and drawn on other threads; a call on the document being saved still waits for its save.

## 1.13.0

Faster saves, two fixes for text lost in Word documents, and a presentation's groups in the document model.
Nothing in the API changes or goes away. The evaluation key in the README works until 2027-01-01.

**New API**, on Windows and Linux:

- **Groups in presentations.** `SlideScene.Groups` lists a slide's groups, outermost first, each a `SlideGroup`
  with its `Id`, its `ParentID`, its `Frame` on the slide and `IsRotatedOrFlipped`. `SlideShape.GroupPath` names
  the groups a shape sits in, outermost first. In this version they can be read, not changed:
  `PresentationSceneEditor.ReplacingFrame` still does not move a shape that is inside a group.

**All documents:**

- **Saving is faster.** A save now copies the parts of the file it did not change as they are, instead of
  unpacking and packing every part again. It no longer compresses pictures, fonts, video and audio a second
  time, and it writes the file as it goes rather than building it in memory. A save after one edit measured 3 to
  4.5 times faster on Windows.
- **A save that would pass 4 GB throws and writes nothing.** Before, it ended the process.
- **An encrypted or ZIP64 package says so.** A document with an encrypted part, or one written as ZIP64, is
  refused when it is opened (`ComOfficeKitException`, `ErrorKind.Parse`, as before), and the message now names
  the reason; before, it said only that the file could not be decoded or was corrupt.

**Word documents:**

- **Text typed right after an inline picture is saved.** When `WordDocument.SceneEditor.Insert` put text where
  the caret stood right after a picture (a picture alone in its paragraph, or at a paragraph's end), the text
  went into the picture's run and was dropped on save, with no error, and the picture was written again as a new
  one. The text now goes into a run of its own after the picture, and the picture keeps its XML.
- **A document whose WordprocessingML is not bound to the `w:` prefix** (Python's ElementTree writes `ns0:`, for
  one) was saved as XML that was not well-formed after any edit: Word refused the file, and the edited paragraph
  lost its text when the file was read again. It now saves well-formed.

## 1.12.1

A fix for a crash. A formula that gives a function too few arguments (`SUMIF`, `AVERAGEIF`, `COUNTIF` or
`MATCH` with fewer than two, `INDEX` or `INDIRECT` with none, `RANK` with one) ended the process that held
the workbook: when `SetCellInput` or `SaveSync` typed it into a cell, when a workbook was opened whose file
had no saved result for it, and when a cell it reads changed. It was not an exception, so no `catch` saw it.
Every earlier release has it, so one such file could stop a server that opens workbooks from outside. Such a
formula is now `#VALUE!`, as other functions are when given too few arguments. Nothing in the API changes,
and pages lay out as in 1.12.0. The evaluation key in the README works until 2027-01-01.

## 1.12.0

OpenDocument text, and copy and paste in workbooks. ComOffice 1.10.0 to 1.11.1 were not released for
Windows and Linux: their changes are here with 1.12.0's. Pages can break differently from 1.9.0, so a
document's page count can change. Nothing in the API changes or goes away. The evaluation key in the
README works until 2027-01-01.

**New API**, on Windows and Linux:

- **OpenDocument text.** An `.odt` opens as a `WordDocument` (`DocumentFormat.Odt`), from a path or a
  stream, and saves as `.odt` or `.docx`. OpenDocument text does not carry comments, tracked changes or
  drawing shapes here, so a save as `.odt` that would leave some out throws `ContentLossException`
  (`ErrorKind.ContentLost`) and writes nothing; its `Loss` has the counts. `WordDocument.GetContentLoss()`
  reads them before you save, so you can ask in your own words, and `Save` and `SaveAs` with
  `SaveOptions { AcceptContentLoss = true }` write anyway. A save as `.docx` is never refused.
  [Getting started](docs/getting-started.md#opendocument-text) shows it.
- **Copy and paste in workbooks.** `Spreadsheet.PasteHtml(sheet, cell, html, text)` pastes what a
  clipboard holds: a table copied from Excel, Google Sheets or a web page comes in with its values,
  formats and merged cells, and text pastes as typing. `Spreadsheet.Copy(sheet, range)` returns a range
  as text and as an HTML table (`CopiedRange`), and pasting that text again brings the cells back,
  formulas included. [Spreadsheets](docs/spreadsheets.md#copy-and-paste) shows it.

**All documents:**

- **Parts found the way the file names them.** Files that name their parts by absolute paths, as
  Microsoft's own templates do, lost what those parts hold: a document its styles, numbering, headers
  and footers; a workbook its charts, tables, pivot tables and theme colours; a deck its layouts, masters
  and themes. They are read now, and kept on save. In one such workbook, cells typed before a save read
  back as other text after it; that no longer happens.

**Word documents:**

- **Page colour.** A document's page colour (Word's Design ▸ Page Color) fills every page in page
  images and in PDF, and a save keeps it.
- **Pages:** a table row that fits a page but not what is left of this one breaks at the page's foot, as
  Word breaks it, keeping two lines of a paragraph on each side; a row that may not split but is taller
  than a page starts a new page and carries on, where it was cut off; a picture on a line spaced at a
  multiple of single is given that multiple of its text's height; moved text that is tracked shows as a
  deletion and an insertion; lines in 仿宋 (FangSong) are as tall as Word sets them.

**Spreadsheets:**

- **Saving keeps more of the file:** a column's style, best-fit and outline attributes; cells you did not
  edit, as the file had them; a chart's cached values, updated
  when you change the cells it draws.
- **New functions:** `SUMIFS`, `COUNTIFS`, `AVERAGEIFS`, `MAXIFS`, `MINIFS` and `HYPERLINK`.

**Presentations:**

- A shape's text takes its style's font colour, and pictures draw their transparency, duotone and
  greyscale, in page images and in PDF.

## 1.9.0

A page's text, and where the page draws it, so an application can let its users select, copy and search over a
page image. Pages lay out and draw as in 1.8.0, and nothing in the API changes or goes away. The evaluation key in
the README works until 2027-01-01.

**New API**, on Windows and Linux:

- `Document.GetPageText(index)`, and an overload that takes a `CancellationToken`, returns the text a page draws and
  where: the page's lines in the order it draws them, each with its text, box, baseline and rotation, and each
  stretch of one size with every character's advance. It uses `RenderPage`'s pages and points, so it lies on the
  page image. It holds headers and footers, footnotes and their marks, watermarks, chart labels and turned shapes'
  text, and leaves out text a clip hides. It needs a licence that allows rendering.
  [Rendering and PDF](docs/rendering-and-pdf.md#page-text) shows its coordinates.

**Samples:**

- The samples' source code is under the MIT License ([`Samples/LICENSE`](Samples/LICENSE)), so you may copy it into
  your own code. The SDK they call is licensed as [`LICENSE.md`](LICENSE.md) says.

## 1.8.0

ComOffice 1.7.0 was not released for Windows and Linux: its changes are here with 1.8.0's. Pages can break
differently from 1.6.0, so a document's page count can change. The API is unchanged. The evaluation key in the
README works until 2027-01-01.

**Word documents**, on Windows and Linux:

- Chinese, Japanese and Korean text in a font from Windows or Office, such as 新細明體, 標楷體, 宋体, 微軟正黑體 or
  MS 明朝, is set at the line height Word gives it: 1.3 times the font's Windows height, where no document grid sets
  the lines. Those lines were about 8 to 23 percent shorter than Word's, so such a document can now have more pages,
  nearer Word's count.

**Presentations**, on Windows and Linux:

- Preset colours (`<a:prstClr>`, which PowerPoint writes for many shadows) are read wherever a deck uses them: in
  shadows, text, fills and charts. Shadows, white text and black fills that were missing now appear.

**Samples:**

- Sample-WebViewer refuses an upload larger than ASP.NET's request limit with a 413 that states the limit.
  [Running on a server](docs/servers.md) adds that a refusal carries the engine's own message, which can name your
  service's files: log it, and answer your users in your own words.

## 1.6.0

Documents are drawn more as Office draws them, in page images and PDFs. Pages can break differently from
1.5.0, so a document's page count can change. The API is unchanged.

**Spreadsheets**, on Windows and Linux unless a line says otherwise:

- Rows that Excel sized to their contents keep the height Excel saved, and pages break where Excel's do.
- A merged range's outline is drawn from the borders of the cells along each of its sides.
- Shapes and text boxes on a sheet are drawn as they are on a slide: arrowheads, transparency, every stop of a
  gradient, dashed outlines, shadows, underlined and vertical text, and text at 11 pt when no size is set.
- Dotted and dashed cell borders are dotted and dashed, and medium dash-dot borders have their full width. On
  Linux, page images still draw these borders solid; PDFs draw them dashed.
- The 3 Symbols icon set draws its cross, exclamation mark and tick.
- A formula that was saved without its result shows the value the engine calculates. The file is not changed.
- On Windows, EMF and WMF pictures on a sheet are drawn where a grey box stood. On Linux they are still left out,
  as 1.5.0 said.

**Presentations**, on Windows and Linux:

- A picture's border is drawn over the picture.
- Pattern fills draw their pattern: stripes, diagonals, grids, or a percentage of the foreground colour.
- A link is underlined in the theme's link colour, unless the file asks for the text's own colour.
- A shape with no fill and no outline casts its shadow from its text.

**East Asian vertical text**, on Windows and Linux: in text set vertically (`eaVert`), Chinese, Japanese and
Korean characters stand upright, Latin letters and digits turn, columns run right to left, and punctuation takes
its vertical form. This applies on slides and in text boxes on sheets.

**Fonts, on Windows:**

- A font named with its weight or width, such as Calibri Light, Arial Narrow or Segoe UI Semibold, is drawn in that
  face.
- A Mac or older East Asian family that Windows lacks is drawn in a Windows font of the same design, keeping the
  original's line height. `Document.FontSubstitutions` reports the new stand-ins.

## 1.5.0

**Linux.** The package now also runs on x64 Linux, with the same API: the Linux engine is in
`runtimes/linux-x64/native`, beside the Windows one. It needs glibc 2.34 or later, .NET 8 and the
distribution's libxml2; Ubuntu 24.04, Debian 12 and Red Hat Enterprise Linux 9 are tested, in clean
images. [ComOffice on Linux](docs/linux.md) covers what the machine needs, publishing for `linux-x64`,
activating, fonts, and what differs from Windows.

- **Licences** name the platforms they cover, and one that does not cover Linux is refused there with
  `LicenseError.PlatformMismatch`. On Linux the AppID is the activating assembly's name, as on Windows.
  `LicenseStorage.Persistent` keeps an activation in a file only that user can read, not encrypted; on a
  server, keep the licence in memory.
- **Fonts:** Carlito and Caladea ship beside the Linux engine, with the widths of Calibri and Cambria, for
  machines that lack a document's fonts. On Linux, `EngineOptions.FontDirectories` names folders to look
  in first.
- **Pictures:** on Linux, PNG, JPEG, GIF and BMP pictures are drawn; TIFF, EMF and WMF are left out of
  page images and PDFs.
- **Containers:** `Samples/Sample-ServerWorkers` gains `Dockerfile.linux`, on Microsoft's .NET 8 runtime
  image, and [Running on a server](docs/servers.md) a section on Linux containers.
- **The package** is about twice as large, 57 MB, as it carries both engines.

**The name.** The SDK is now called ComOffice. The package, namespaces, types and file names keep
`ComOfficeKit`, so nothing in your code or your project changes. Text the SDK writes says ComOffice
where it said ComOfficeKit:

- the messages of the exceptions it throws;
- the licence errors' messages, which now start "ComOffice: ";
- the `Producer` of every PDF the engine exports;
- the API documentation.

If your code reads a message's text, read `LicenseException.Error` or `ComOfficeKitException.Kind`
instead: they have not changed.

The API is unchanged.

## 1.4.0

For running on a server: a guide and two samples, a working directory with a quota, cancelling a call,
and a report of the fonts a document was drawn without. The engine now also loads in the smaller Windows
Server image.

New in the API:

- **`Engine.Configure(EngineOptions)`**, once in a process, before its first document: `WorkingDirectory`,
  where documents unpack and keep their pictures while they are open, and `WorkingDirectoryQuota`, the most
  the engine keeps there at once. An open or a save that would pass the quota throws `ComOfficeKitException`
  with the new `ErrorKind.LimitExceeded`, and writes nothing. With a working directory, a document's pictures
  are deleted when the document is disposed.
- **Cancelling a call.** `Document.Open`, `GetPageCount` (new), `RenderPage`, `ExportPdf`, `Save` and
  `SaveAs` each have an overload that takes a `CancellationToken`. A cancelled call throws
  `OperationCanceledException` at its next check and changes nothing: no document opened, no file replaced.
- **`Document.FontSubstitutions`**: each font family the document asked for that the machine does not have,
  and the family it was drawn in instead (`FontSubstitution`, with `Requested` and `Used`), once the document
  has been laid out.
- **The document model** deletes a table (`DeleteTable`) and moves a table's column border
  (`MovingTableColumnBorder`).

Where it runs:

- **Windows Server 2022 and 2025** are tested, and Windows containers on the full Windows Server images of
  2022 and 2025 (`mcr.microsoft.com/windows/server`).
- **The smaller Windows Server image of 2025** (`mcr.microsoft.com/windows/servercore:ltsc2025`): the engine
  now loads there. Before, it needed a printing library that image does not have. In the smaller image of
  2022, documents open, change and save, but page images are not available.

New in `docs/` and `Samples/`:

- **`docs/servers.md`**: running on a server in worker processes, with limits on disk, time and memory,
  fonts, Windows containers, and showing documents in a browser.
- **`Samples/Sample-ServerWorkers`**: a supervisor and its worker processes that convert a folder of
  documents to PDF, with a Dockerfile for a Windows container.
- **`Samples/Sample-WebViewer`**: a minimal ASP.NET service and its page, which show a document's pages as
  images and the document as PDF in a browser.

In `Scripts/`:

- `Scripts/fetch-nupkg.ps1 -Tag` no longer needs the GitHub CLI. It asks GitHub's REST API for the
  release and downloads the package from the release page, and checks its size. Before, it ran `gh`,
  which nothing said it needed, and stopped where `gh` was not installed. A tag that does not exist
  still lists the published releases.

## 1.3.0

No change to the API: this version changes what the engine draws and what it keeps. There was no 1.2.0
release for Windows; its changes are in this one.

Drawn as Word, Excel and PowerPoint show them, on screen, in page images and in PDF:

- **Word SmartArt and VML shape groups**, each shape with its outline, fill and text. Before, they took
  no room, and what followed them moved up.
- **WMF pictures**, converted to EMF and drawn as vectors. Only EMF was drawn before.
- **An embedded object** (`<w:object>`) shows its preview picture: in line with the text, or floating
  when its shape is positioned. It was skipped.
- **Word character styles** (`<w:rStyle>`): a hyperlink is blue and underlined, and a table of contents'
  entries take their TOC styles. Only paragraph styles were read, so links came out black and plain.
- **East Asian text in its East Asian font.** Each character takes its run's Latin or East Asian font,
  as Word does, and fonts named only through the document's theme are read. A run kept one family before.
- **Excel cell text:** a cell with Wrap Text is broken into lines that fit it, and at its line breaks,
  and placed by its vertical alignment; a rich-text cell keeps each run's font, size and style; and a
  number format's colour (`[Red]`) colours the value.
- **Slide charts** take the deck's theme colours, each point's own colour, their data labels and legend,
  and the plot's manual layout. A theme-coloured series fell back to a default palette.
- **A slide table without a table style** draws only its cells' own fills and borders, as tables from
  Google Slides and WPS expect. It was drawn in a built-in style, often in the deck's blue.

Kept when the document model edits a Word table:

- `InsertTableRow`, `InsertTableColumn`, `DeleteTableRow` and `DeleteTableColumn` keep the table's own
  properties: its style, alignment, indent and cell margins, and the borders its style gives it. A table
  of fixed width takes the new grid's width; a width given as a share of the page stays a share.
- `DeleteRange` deletes ranges it refused: over more than one paragraph of a cell, a header, a footer
  or a note; from one cell into another, or into or out of a table; and over a table. Cells and rows
  stay, and a cell the range empties keeps one empty paragraph.

## 1.1.0

The document model is no longer a preview. From this version it keeps to semantic versioning with the
rest of the API: a 1.x version adds to it, and nothing your code compiles against in
`ComOfficeKit.Model` is removed or changed before 2.0. It also gains the editing that ComOfficeKit for
Apple platforms had and it lacked. See [The document model](docs/object-model.md).

- **Word pictures:** `WordSceneEditor.InsertInlineImage` puts a picture, given as its bytes, at a
  position in the body, a table, a header, a footer or a note. `WordDocument.GetImageData` and
  `GetFloatingImageData` read a picture's bytes.
- **Conditional formats:** `WorkbookSceneEditor.ReplacingConditionalRules` sets a sheet's rules.
- **Charts:** `WorkbookSceneEditor.InsertingChart` inserts a chart of a range, made as Excel makes one
  from a selection.
- **Slides from a layout:** `Presentation.GetLayouts` reads the deck's layouts, `GetLayoutChoices` and
  `GetDefaultLayoutChoice` give those New Slide offers and uses, and `PresentationSceneEditor.Inserting`
  inserts the slide New Slide makes from one.

Fixed:

- A link in a header, a footer, a footnote or an endnote lost its address on save when it was put there
  with `InsertHyperlink`, or when its paragraph was edited, and so did a link in an edited text box.
  They keep it now.
- A picture in a footnote or an endnote is shown, and a link there has its address: both are now read
  through the notes' own relationships. Before, such a picture was never shown.

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
