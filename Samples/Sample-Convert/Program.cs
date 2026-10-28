// Converts a document ComOfficeKit opens to a PDF and to a PNG of each page.
//
//   dotnet run -- <file> [output folder]
//
// The licence key comes from the COMOFFICEKIT_LICENSE_KEY environment variable. Your own application
// keeps its key wherever it keeps secrets.
using ComOfficeKit;

if (args.Length < 1)
{
    Console.Error.WriteLine("usage: Sample-Convert <file> [output folder]");
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

    var input = Path.GetFullPath(args[0]);
    var output = args.Length > 1 ? Path.GetFullPath(args[1]) : Path.GetDirectoryName(input)!;
    Directory.CreateDirectory(output);
    var name = Path.GetFileNameWithoutExtension(input);

    using var document = Document.Open(input);
    Console.WriteLine($"{Path.GetFileName(input)}: {document.Kind}, {document.PageCount} page(s)");

    for (int page = 0; page < document.PageCount; page++)
    {
        var png = Path.Combine(output, $"{name}-{page + 1}.png");
        document.RenderPage(page, dpi: 144).SavePng(png);
        Console.WriteLine($"  {png}");
    }

    var pdf = Path.Combine(output, $"{name}.pdf");
    try
    {
        document.ExportPdf(pdf);
        Console.WriteLine($"  {pdf}");
    }
    catch (ComOfficeKitException e) when (e.Kind == ErrorKind.Unsupported)
    {
        // No "Microsoft Print to PDF" or no Print Spooler on this machine: the images are still there.
        Console.WriteLine($"  no PDF: {e.Message}");
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
