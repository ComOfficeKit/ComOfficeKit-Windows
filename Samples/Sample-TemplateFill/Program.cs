// Fills a Word template's {{placeholders}}, builds a small invoice workbook, and saves both.
//
//   dotnet run -- <template.docx> <output folder> name=Ada "address=1 Main Street"
//
// Each name=value replaces every {{name}} in the template, keeping the formatting where it lands.
// The licence key comes from the COMOFFICEKIT_LICENSE_KEY environment variable.
using ComOfficeKit;

if (args.Length < 2)
{
    Console.Error.WriteLine("usage: Sample-TemplateFill <template.docx> <output folder> [name=value ...]");
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
    var output = Path.GetFullPath(args[1]);
    Directory.CreateDirectory(output);

    // ---- The letter -------------------------------------------------------------------------------
    var letterPath = Path.Combine(output, "letter.docx");
    using (var letter = (WordDocument)Document.Open(args[0]))
    {
        foreach (var pair in args.Skip(2))
        {
            var equals = pair.IndexOf('=');
            if (equals <= 0)
            {
                Console.Error.WriteLine($"not name=value: {pair}");
                return 2;
            }
            var placeholder = "{{" + pair[..equals] + "}}";
            int replaced = letter.ReplaceAll(placeholder, pair[(equals + 1)..]);
            Console.WriteLine($"{placeholder}: {replaced} replaced");
        }
        letter.SaveAs(letterPath);
    }
    Console.WriteLine(letterPath);

    // ---- The invoice ------------------------------------------------------------------------------
    var invoicePath = Path.Combine(output, "invoice.xlsx");
    Document.CreateBlank(DocumentFormat.Xlsx, invoicePath);
    using (var invoice = (Spreadsheet)Document.Open(invoicePath))
    {
        var sheet = invoice.ActiveSheet;
        string[,] rows =
        {
            { "Item", "Quantity", "Price", "Amount" },
            { "Consulting", "3", "120", "=B2*C2" },
            { "Travel", "1", "85.5", "=B3*C3" },
            { "Total", "", "", "=SUM(D2:D3)" },
        };
        for (int row = 0; row < rows.GetLength(0); row++)
        {
            for (int column = 0; column < rows.GetLength(1); column++)
            {
                var cell = $"{(char)('A' + column)}{row + 1}";
                invoice.SetCellInput(sheet, cell, rows[row, column]);
            }
        }
        Console.WriteLine($"total: {invoice.GetCellText(sheet, "D4")}");
        invoice.Save();
    }
    Console.WriteLine(invoicePath);
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
