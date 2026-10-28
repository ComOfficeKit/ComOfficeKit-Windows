# Spreadsheets

`Document.Open` returns a `Spreadsheet` for `.xlsx`, `.xlsm`, `.csv` and `.tsv`. A `.csv` or `.tsv` file
is a workbook of one sheet.

## Sheets

```csharp
using var book = (Spreadsheet)Document.Open("figures.xlsx");
foreach (var name in book.SheetNames) Console.WriteLine(name);   // in the workbook's order, hidden ones included
book.ActiveSheet = "Q3";                                          // what PageCount, RenderPage and ExportPdf are about
```

Sheets are named as the workbook names them; a name that differs only in case finds the same sheet,
as in Excel. A name that is not a sheet's throws `ArgumentException`. A hidden sheet cannot be made
active, and `GetCellText` and `GetCellInput` refuse its cells; the document model reads them
([The document model](object-model.md#workbooks)).

## Cells

Cells are named A1 style, without `$`: `B7`, `XFD1048576`. Letters may be in either case.

```csharp
string shown = book.GetCellText("Q3", "C12");     // as the cell shows it: "1,234.50", "15/10/2026", "#DIV/0!"
string input = book.GetCellInput("Q3", "C12");    // its formula, "=SUM(C2:C11)", or its value as typed
bool changed = book.SetCellInput("Q3", "C2", "42");
```

`SetCellInput` works as typing into the cell in Excel:

| You type | The cell holds |
|---|---|
| `=A1+A2` | a formula, calculated now |
| `42`, `-3.5` | a number |
| `TRUE`, `FALSE` | a boolean |
| anything else | text |
| an empty string | nothing: the cell is emptied, its formatting kept |

Whatever depends on the cell is recalculated at once, so the next `GetCellText` of a total reads the
new value. The cell keeps its formatting.

`SetCellInput` returns `false` when the cell already held that input. An input the workbook refuses,
as Excel would, throws `ComOfficeKitException` with `ErrorKind.InvalidArgument` and Excel's reason:
typing into a PivotTable's cell gives `Sales!F2: Cannot change this part of a PivotTable report.`

Reading or writing a cell of another sheet does not change `ActiveSheet`.

## Formats, rows and columns

Number formats, fonts, fills, borders and alignment, inserting and deleting rows and columns, and
tables are edited through the document model's `SceneEditor` ([The document
model](object-model.md#workbooks)).

## Saving

```csharp
book.Save();                 // to the file it was opened from, in its format
book.SaveAs("figures.csv");  // .xlsx, .xlsm, .csv or .tsv, by the extension
```

What the workbook holds and ComOfficeKit does not change is carried over as it was: charts,
pictures, pivot tables, and the macros of an `.xlsm`, which are kept and never run. A formula you
typed is saved with its calculated value, so another application shows the value before it
recalculates.

- **`.csv` and `.tsv`** hold the active sheet, from A1 to its last cell in use, each cell as it shows:
  a formula as its value, a formatted number formatted. The workbook itself is not changed, so a later
  save as `.xlsx` still has every sheet. The encoding is the file's own when it came from the same kind
  of file, otherwise UTF-8 with a BOM, as Excel's "CSV UTF-8".
- **An `.xlsm` saved as `.xlsx`** loses its macros, as in Excel. Saved as `.xlsm`, it keeps them.
- **Opening a `.csv` or `.tsv`** reads each field as if it were typed into its cell, in the encoding it
  was written in (UTF-8 with or without a BOM, UTF-16 with a BOM, Big5, GB18030, else Latin-1). The
  sheet is named after the file.

## The formula engine

About 105 of Excel's functions are calculated: arithmetic, statistics, text, dates, logic and lookups
(`SUM`, `AVERAGE`, `IF`, `VLOOKUP`, `INDEX`, `MATCH`, `TEXT`, `DATE`, ...). A formula shows the value
saved in the file until a cell it depends on changes; then it is calculated. A function outside that
set calculates to `#NAME?`, as Excel shows for a function it does not know. Dynamic arrays are not
calculated.
