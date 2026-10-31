# Changelog

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
