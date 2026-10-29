// Edits a report, a workbook and a deck through the document model (ComOfficeKit.Model),
// saves them, then opens each saved file again and prints what it holds.
//
//   dotnet run -- <output folder>
//
// A read hands back records: a snapshot of the document as it stood. An edit is one call, which the
// engine applies as one undo step. The licence key comes from the COMOFFICEKIT_LICENSE_KEY environment
// variable.
using System.Globalization;
using ComOfficeKit;
using ComOfficeKit.Model;

if (args.Length != 1)
{
    Console.Error.WriteLine("usage: Sample-ObjectModel <output folder>");
    return 2;
}
var key = Environment.GetEnvironmentVariable("COMOFFICEKIT_LICENSE_KEY");
if (string.IsNullOrWhiteSpace(key))
{
    Console.Error.WriteLine("Set COMOFFICEKIT_LICENSE_KEY to your licence key.");
    return 2;
}

try
{
    License.Activate(key);
    var output = Path.GetFullPath(args[0]);
    Directory.CreateDirectory(output);

    // ---- The report -------------------------------------------------------------------------------
    var reportPath = Path.Combine(output, "report.docx");
    Document.CreateBlank(DocumentFormat.Docx, reportPath);
    using (var report = (WordDocument)Document.Open(reportPath))
    {
        var editor = report.SceneEditor;

        // A position is a paragraph, a run in it and an offset in the run's text, all from a read.
        editor.Insert("Quarterly report", StartOf(Paragraphs(report).First()));

        // Each edit leaves the caret where it would leave a user's, so the next one can follow on.
        editor.SplitParagraph(report.Caret!);
        editor.Insert("Revenue rose in every region, and revenue per customer held steady.", report.Caret!);

        // A read is a snapshot, so read again after an edit. Ids stay the same while the document is open.
        var title = Paragraphs(report).First();
        var titleRun = title.Runs[0];
        editor.ApplyFormatting(
            new WordScenePosition { ParagraphID = title.Id, RunID = titleRun.Id, CharOffset = 0 },
            new WordScenePosition { ParagraphID = title.Id, RunID = titleRun.Id, CharOffset = titleRun.Text.Length },
            new WordRunFormattingChange { Bold = true, SizePt = 20 });
        editor.SetAlignment(WordParagraphStyleAlignment.Center, title.Id, report.Caret!);

        // A match is a pair of positions, as a selection is. Formatting one splits its run, which makes
        // the positions of the others stale, so the matches are found again before each.
        int matches = editor.FindMatches("revenue").Count;
        for (int i = 0; i < matches; i++)
        {
            var match = editor.FindMatches("revenue")[i];
            editor.ApplyFormatting(match.Start, match.End, new WordRunFormattingChange { Italic = true });
        }

        // A table after the last paragraph. Each cell holds paragraphs of its own, written like any other.
        editor.InsertTable(3, 2, Paragraphs(report).Last().Id);
        var table = report.GetBody().OfType<WordBodyElement.Table>().Single().Value;
        string[,] figures = { { "Region", "Revenue" }, { "North", "1,250.00" }, { "South", "980.50" } };
        for (int row = 0; row < figures.GetLength(0); row++)
        {
            for (int column = 0; column < figures.GetLength(1); column++)
            {
                var cell = table.Rows[row].Cells[column].Content.OfType<WordBodyElement.Paragraph>().First().Value;
                editor.Insert(figures[row, column], StartOf(cell));
            }
        }
        report.Save();
    }

    // ---- The workbook -----------------------------------------------------------------------------
    var workbookPath = Path.Combine(output, "sales.xlsx");
    Document.CreateBlank(DocumentFormat.Xlsx, workbookPath);
    using (var workbook = (Spreadsheet)Document.Open(workbookPath))
    {
        // The editors name a sheet by its index in SheetNames, and a cell by its column and row from 1.
        const long sheet = 0;

        // Text as a user types it into a cell: a number, a formula after "=", TRUE or FALSE, else text.
        // What depends on the cells is recalculated, and the whole list is one undo step.
        workbook.SaveSync.CommittedCellEdits(new (CellReference, string)[]
        {
            (At(1, 1), "Region"), (At(2, 1), "Revenue"),
            (At(1, 2), "North"), (At(2, 2), "1250"),
            (At(1, 3), "South"), (At(2, 3), "980.5"),
            (At(1, 4), "Total"), (At(2, 4), "=SUM(B2:B3)"),
        }, sheet);

        // The editor changes what it is given and nothing else: it does not recalculate.
        var editor = workbook.SceneEditor;
        editor.SettingFormat(new CellRange { Start = At(2, 2), End = At(2, 4) }, sheet,
                             new CellFormatChange { NumberFormat = "#,##0.00" });
        editor.SettingFontTrait(WorkbookSceneEditorFontTrait.Bold, true, At(1, 1), sheet);
        editor.SettingFontTrait(WorkbookSceneEditorFontTrait.Bold, true, At(2, 1), sheet);
        workbook.Save();
    }

    // ---- The deck ---------------------------------------------------------------------------------
    var deckPath = Path.Combine(output, "deck.pptx");
    Document.CreateBlank(DocumentFormat.Pptx, deckPath);
    using (var deck = (Presentation)Document.Open(deckPath))
    {
        // A blank deck is one title slide. A slide lists the shapes its layout or master gives it too,
        // flagged IsInheritedFromLayoutOrMaster; only the slide's own text can be edited.
        var editor = deck.SceneEditor;
        var placeholders = deck.GetSlide(0).Shapes
            .Where(shape => !shape.IsInheritedFromLayoutOrMaster && shape.Content is SlideShapeContent.Text)
            .ToList();
        string[] lines = { "Quarterly report", "Revenue by region" };
        for (int i = 0; i < Math.Min(lines.Length, placeholders.Count); i++)
        {
            // Slides, paragraphs and runs are counted from 0. ReplacingRuns keeps the paragraph's own
            // properties, such as the alignment the placeholder gives it.
            editor.ReplacingRuns(0, placeholders[i].Id, 0, new[] { new TextRun { Text = lines[i] } });
        }
        editor.ReplacingNotes(0, "Open with the regional figures.");
        editor.Duplicating(0);
        deck.Save();
    }

    // ---- What the saved files hold ----------------------------------------------------------------
    using (var report = (WordDocument)Document.Open(reportPath))
    {
        Console.WriteLine($"{reportPath}:");
        foreach (var element in report.GetBody())
        {
            switch (element)
            {
                case WordBodyElement.Paragraph { Value: var paragraph }:
                    Console.WriteLine($"  paragraph: {string.Concat(paragraph.Runs.Select(Marked))}");
                    break;
                case WordBodyElement.Table { Value: var table }:
                    foreach (var row in table.Rows)
                    {
                        Console.WriteLine($"  table row: {string.Join(" | ", row.Cells.Select(CellText))}");
                    }
                    break;
            }
        }
    }
    using (var workbook = (Spreadsheet)Document.Open(workbookPath))
    {
        Console.WriteLine($"{workbookPath}:");
        var sheet = workbook.SheetNames[0];
        foreach (var cell in workbook.GetCells(sheet, "A1:B4"))
        {
            Console.WriteLine($"  {cell.Reference.A1,-3} {Shown(cell.Value),-10} {cell.Formula,-11} "
                              + $"{cell.Format?.NumberFormat} {(cell.Format?.Font?.Bold == true ? "bold" : "")}");
        }
        Console.WriteLine($"  B4 as displayed: {workbook.GetCellText(sheet, "B4")}");
    }
    using (var deck = (Presentation)Document.Open(deckPath))
    {
        Console.WriteLine($"{deckPath}:");
        foreach (var slide in deck.GetSlides())
        {
            var texts = slide.Shapes
                .Where(shape => !shape.IsInheritedFromLayoutOrMaster)
                .Select(shape => shape.Content)
                .OfType<SlideShapeContent.Text>()
                .Select(text => string.Join(" / ", text.Value.Paragraphs.Select(p => string.Concat(p.Runs.Select(r => r.Text)))));
            Console.WriteLine($"  slide {slide.SlideNumber}: {string.Join(" | ", texts)}; notes: {slide.Notes}");
        }
    }
    return 0;
}
catch (LicenseException e)
{
    Console.Error.WriteLine($"licence: {e.Error}: {e.Message}");
    return 3;
}
catch (ComOfficeKitException e)
{
    Console.Error.WriteLine($"{e.Kind}: {e.Message}");
    return 1;
}

static IEnumerable<WordParagraph> Paragraphs(WordDocument document) =>
    document.GetBody().OfType<WordBodyElement.Paragraph>().Select(element => element.Value);

// A paragraph's start: offset 0 in its first run. A paragraph with no run yet takes any run id there,
// and typing into it makes its first run.
static WordScenePosition StartOf(WordParagraph paragraph) => new()
{
    ParagraphID = paragraph.Id,
    RunID = paragraph.Runs.Count > 0 ? paragraph.Runs[0].Id : Guid.Empty,
};

static CellReference At(long column, long row) => new() { Column = column, Row = row };

// A run's text, *bold* and _italic_ marked.
static string Marked(WordRun run)
{
    var text = run.Text;
    if (run.Formatting?.Italic == true) text = $"_{text}_";
    if (run.Formatting?.Bold == true) text = $"*{text}*";
    return text;
}

static string CellText(WordTableCell cell) => string.Concat(
    cell.Content.OfType<WordBodyElement.Paragraph>().SelectMany(p => p.Value.Runs).Select(r => r.Text));

static string Shown(CellValue value) => value switch
{
    CellValue.Number number => number.Value.ToString(CultureInfo.InvariantCulture),
    CellValue.String text => $"\"{text.Value}\"",
    CellValue.Boolean truth => truth.Value ? "TRUE" : "FALSE",
    CellValue.Date date => date.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
    CellValue.Error error => error.Value,
    _ => "",
};
