# The document model

The rest of the API works on whole documents. The document model, in the namespace
`ComOfficeKit.Model`, goes inside them: a Word document's paragraphs, runs and tables, a workbook's
sheets, cells and formats, a deck's slides, shapes and text. You read it as records, and change it with
commands that the engine applies.

> **Versioning.** Since 1.1.0 the model keeps to semantic versioning, as the rest of the API does: a 1.x
> version adds to it, and nothing you compile against is removed or changed before 2.0.

[Sample-ObjectModel](../Samples/Sample-ObjectModel) edits a report, a workbook and a deck through the
model, saves them, and reads the saved files back.

The types carry the names of ComOfficeKit for Apple platforms, capitalised as .NET names are:
`WordParagraph`, `CellValue`, `SlideShape`. Code that works on the model reads the same on both.

## Reading

| | Reads | Counts | In parts |
|---|---|---|---|
| `WordDocument` | `GetScene()`, the whole document; `GetBody()`, its paragraphs and tables; `GetComments()`; `GetImageData(paragraphId, runId)` and `GetFloatingImageData(paragraphId, index)`, a picture's bytes | `BodyCount`, `CommentCount` | `GetBody(from, count)`, `GetComments(from, count)` |
| `Spreadsheet` | `GetWorkbook()`; `GetSheet(name)`; `GetCells(sheet)`, the cells that hold something; `GetCells(sheet, "A1:D20")` | `GetCellCount(sheet)` | `GetCells(sheet, from, count)` |
| `Presentation` | `GetSlides()`, `GetSlide(index)`; `GetLayouts()`, the deck's layouts | `SlideCount` | `GetSlides(from, count)` |

**A read is a snapshot.** It returns records with `init`-only properties, compared by value, and an
edit does not change them: read again after one. Each document has a `ModelRevision`, a number that
moves when the document changes, so a snapshot is current while `ModelRevision` is where it was when you
read it. Saving does not move it.

**A large document is read in parts.** `GetBody(from, count)` reads only those paragraphs and tables,
and `GetCells(sheet, from, count)` only those cells, so a long document need not be read whole.

**A value of one of several kinds** is a nested record for each kind, matched with C# patterns:

```csharp
foreach (var element in report.GetBody())
{
    switch (element)
    {
        case WordBodyElement.Paragraph { Value: var paragraph }:
            Console.WriteLine(string.Concat(paragraph.Runs.Select(run => run.Text)));
            break;
        case WordBodyElement.Table { Value: var table }:
            Console.WriteLine($"a table of {table.Rows.Count} rows");
            break;
    }
}
```

The same goes for a cell's `CellValue` (`Number`, `String`, `Boolean`, `Date`, `Error`, `Empty`) and a
shape's `SlideShapeContent` (`Text`, `Table`, `Image`, `Chart`, `Empty`).

## Editing

Each document has a `SceneEditor`, and a workbook a `SaveSync` as well. Each of their commands is one
edit: the engine applies it to the document, and the command returns the document's `ModelRevision`
after it. A command names what it changes by the ids, indexes and positions of a read. Their questions,
such as `FindMatches`, change nothing. Save as you would after any other edit, with `Save()` or
`SaveAs(path)`.

A command the engine cannot apply throws `ComOfficeKitException` with `ErrorKind.InvalidArgument`, and
the message names the argument and what is wrong with it:

```
"at".charOffset is 999; the run has 16 UTF-16 units
"at".row is 0; it must be from 1 to 1048576
```

Reads and commands follow the rules of the rest of the API: one call at a time on a document, and
different documents in parallel ([Errors and threading](errors-and-threading.md)).

## Word

**Positions.** A place in the text is a `WordScenePosition`: a paragraph's `ParagraphID`, the `RunID` of
a run in that paragraph, and `CharOffset`, an offset in the run's `Text` counted as a .NET string index
counts. Ids stay the same while the document is open; offsets move as text goes in and out, so take a
position from a read made after the last edit. A paragraph with no runs takes any run id at offset 0,
and typing into it makes its first run. Paragraphs in table cells are addressed in the same way.

```csharp
// A paragraph's start: offset 0 in its first run, or any run id when it has none.
static WordScenePosition StartOf(WordParagraph paragraph) => new()
{
    ParagraphID = paragraph.Id,
    RunID = paragraph.Runs.Count > 0 ? paragraph.Runs[0].Id : Guid.Empty,
};
```

**The caret.** `Caret` is where the last command left the editor's caret, as the same edit by hand
would leave it, or null before it has one; `SelectionAnchor` is the other end of a selection. Text is
typed in sequence by following on from the caret:

```csharp
var editor = report.SceneEditor;
editor.Insert("Quarterly report", StartOf(firstParagraph));
editor.SplitParagraph(report.Caret!);                          // Enter
editor.Insert("Revenue rose in every region.", report.Caret!);
```

**Ranges.** A range is two positions, as a selection is: `ApplyFormatting(from, to, change)`,
`DeleteRange(from, to)`, `SelectedText(from, to)`. `FindMatches(text)` returns ranges, each a `Start`
and an `End`. Formatting a range splits its runs, which moves the positions of the matches after it, so
find again after each edit; the sample shows how.

| | `SceneEditor` |
|---|---|
| Text | `Insert`, `DeleteBackward`, `DeleteForward`, `DeleteRange`, `ReplacingRunText`, `SplitParagraph` |
| Characters | `ApplyFormatting` with a `WordRunFormattingChange`: the members you set (`Bold`, `Italic`, `Underline`, `Strikethrough`, `SizePt`, `FontFamily`, `ColorHex`, `Highlight`, `ShadingHex`, `LetterSpacingPt`, `VertAlign`) are set, those whose `Clear…` member you set are cleared, and the rest are kept |
| Paragraphs | `SetParagraphStyle` (by the style's id), `SetAlignment`, `SetLineSpacing`, `SetSpacingBefore`, `SetSpacingAfter`, `AdjustLeftIndent`, `AdjustRightIndent`, `AdjustFirstLineIndent`, `AddTabStop`, `RemoveLastTabStop`, `SetParagraphBorder`, `SetParagraphShading`, `ToggleList` |
| Tables | `InsertTable`, `InsertTableRow`, `InsertTableColumn`, `DeleteTableRow`, `DeleteTableColumn` |
| Fields and notes | `InsertHyperlink`, `InsertFootnote`, `InsertEndnote`, `InsertCaption`, `InsertPageNumber`, `InsertTableOfContents`, `UpdateTableOfContents` |
| Layout | `InsertSectionBreak`, `SetPageMargins`, `SetPageSize`, `EditHeaderFooter`, `InsertTextBox` |
| Pictures | `InsertInlineImage`, from the picture's bytes; `SetInlineImageSize`, `DeleteInlineImageRun`, `SetFloatingImageGeometry`, `RemoveFloatingImage` |
| Tracked changes | `ResolvingRevisions`: accept or reject |
| Questions, which change nothing | `RunFormatting`, `SelectedText`, `FindMatches`, `HasTableOfContents`, `CanInsertNote`, `TableSpanned`, `TextColumnWidthTwips` |

Comments are edited on the document itself: `EditComment(id, text)` and `SetCommentResolved(id,
resolved)`, with the ids `GetComments()` gives. A length whose name ends in `Twips` is in twentieths of
a point, and one ending in `Pt` in points.

**Pictures.** `InsertInlineImage(data, widthPt, heightPt, at, altText)` puts a picture inline at a
position in the body, a table, a header, a footer or a note, and the caret after it; the save writes it
into that part. A text box takes none. The bytes are PNG, JPEG, GIF, BMP or TIFF, at most 64 MB, and
each side is above 0 and at most 1,584 points. A run's `Image` describes its picture without the bytes:
`GetImageData(paragraphId, runId)` reads them, and `GetFloatingImageData(paragraphId, index)` reads a
floating picture's.

## Workbooks

A command names a sheet by its index in `SheetNames`, from 0, and a cell by a `CellReference` with its
`Column` and `Row`, each from 1: A1 is column 1, row 1. A read fills in the cell's `A1` name as well. A
range is a `CellRange` from its `Start` to its `End`.

The two editors differ in recalculation:

| | |
|---|---|
| `SaveSync` | types text into cells as `SetCellInput` does, as a user types into Excel: `=` starts a formula; a number, `TRUE` or `FALSE` is one; anything else is text. What depends on the cells is recalculated. `CommittedCellEdit(text, at, sheetIndex)`, or `CommittedCellEdits(edits, sheetIndex)` for many cells in one edit |
| `SceneEditor` | changes what it is given and nothing else. It does not recalculate |

So type values that formulas use with `SaveSync`, and use `SceneEditor` for formats and for the sheet's
structure. A value set with `SceneEditor.SettingCellValue` is in its cell at once, but a formula that
uses it keeps its old value until it is next calculated: when `SaveSync` or `SetCellInput` types into a
cell that the formula depends on.

```csharp
static CellReference At(long column, long row) => new() { Column = column, Row = row };

book.SaveSync.CommittedCellEdits(new (CellReference, string)[]
{
    (At(1, 1), "Region"), (At(2, 1), "Revenue"),
    (At(1, 2), "North"),  (At(2, 2), "1250"),
    (At(1, 3), "South"),  (At(2, 3), "980.5"),
    (At(1, 4), "Total"),  (At(2, 4), "=SUM(B2:B3)"),
}, 0);
book.SceneEditor.SettingFormat(new CellRange { Start = At(2, 2), End = At(2, 4) }, 0,
                               new CellFormatChange { NumberFormat = "#,##0.00" });
```

| | `SceneEditor` |
|---|---|
| Values | `SettingCellValue` (a `CellValue`, and a formula or null), `RemovingCells` |
| Formats | `SettingFormat` on a range, with a `CellFormatChange`: the members you set (`Font`, `Fill`, `Borders`, `NumberFormat`, `Alignment`) are set, those whose `Clear…` member you set are cleared, and the rest are kept. On one cell: `SettingNumberFormat`, `SettingFontTrait`, `SettingFontFamily`, `SettingFontSize`, `SettingFontColor`, `SettingFillColor`, `SettingHorizontalAlignment`, `ClearingFormat` |
| Conditional formats | `ReplacingConditionalRules(rules, sheetIndex)`: the sheet's rules, all of them, in the order of their `Priority`. A rule read from the sheet and given back as it was keeps what the file held of it |
| Rows, columns and sheets | `InsertingRows`, `InsertingColumns`, `DeletingRows`, `DeletingColumns`, `SettingColumnWidth`, `RenamingSheet`, `RemovingSheet` |
| Tables, charts and drawings | `InsertingTable` (`DefaultAuthoredTableStyle()` gives Excel's default look); `InsertingChart`, a chart of a range, made as Excel makes one from a selection; `MovingChart`, `RemovingChart`, `MovingDrawing`, `RemovingDrawing` |

`GetSheet` and `GetCells` read a hidden sheet too, whose cells `GetCellText` and `GetCellInput` refuse.

## Presentations

A command names a slide by its index, from 0; a shape by its `Id`, a string; and text in a shape by its
paragraph's index and its run's index, each from 0. A cell of a table takes a `TableCellAddress`, whose
row and column count from 0. A shape's `Frame` is in points.

A slide's `Shapes` also lists what its layout and master show on it, marked
`IsInheritedFromLayoutOrMaster`. Only the slide's own text is edited: a command on an inherited shape's
text throws `InvalidArgument`. A shape takes an edit as PowerPoint's editor would: a SmartArt part, a
grouped shape or a layout's does not move; a table, a chart or a line does not turn; a picture or a chart
takes no fill.

```csharp
var title = deck.GetSlide(0).Shapes.First(shape => shape.Placeholder?.Type == "ctrTitle");
deck.SceneEditor.ReplacingRuns(0, title.Id, 0, new[] { new TextRun { Text = "Quarterly report" } });
deck.SceneEditor.ReplacingNotes(0, "Open with the regional figures.");
```

| | `SceneEditor` |
|---|---|
| Text | `ReplacingText`, one run's text; `ReplacingRuns`, a paragraph's runs, keeping the paragraph's properties; `ReplacingParagraphs`, all of a shape's paragraphs |
| Formatting | `ReplacingFormatting`, a run's, with a `TextRunFormattingChange`; `ReplacingProperties`, a paragraph's alignment, level, bullet or line spacing, with a `ParagraphPropertiesChange` |
| Shapes | `ReplacingFrame`: position, size and rotation; `ReplacingStyle`: fill and outline, with a `SlideShapeStyleChange`, or a table cell's, with a `TableCellStyleChange` |
| Slides | `ReplacingNotes`, the speaker notes; `Inserting(layout, at)`, the slide New Slide makes from one of `GetLayouts()`, placed at `at`; `Duplicating`, a copy after the slide; `Deleting`, never the deck's last slide |

`GetLayoutChoices(afterSlide)` lists the layouts New Slide offers after a slide, as PowerPoint lists
them, and `GetDefaultLayoutChoice(afterSlide)` the one it uses: each an index into `GetLayouts()`.

## What the model does not do

- No undo or redo: a command is applied when it is called.
- **Word:** a picture does not go in a text box.
- **Workbooks:** no command adds a sheet. A conditional format of the kind `Unsupported` (Top 10, above
  average, a formula, a date) is kept or left out, not made or changed. A picture's bytes are not read.
- **Presentations:** no command adds a shape. A picture's bytes are not read.
