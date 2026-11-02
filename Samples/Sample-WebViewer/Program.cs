// A document viewer over the engine: the browser sends a document to this service, and gets back each page as a PNG
// image and the whole document as PDF, which browsers show themselves. Its page is wwwroot/index.html.
//
//   dotnet run -- --urls http://localhost:5000 [--WorkingDirectory <folder>]
//
// The licence comes from COMOFFICEKIT_LICENSE_KEY (the key) or COMOFFICEKIT_LICENSE_FILE (a licence file).
//
//   POST   /documents?name=<file name>      the document as the request body; answers its id, pages and fonts
//   GET    /documents/{id}/pages/{n}?dpi=96 page n, counting from 1, as PNG
//   GET    /documents/{id}/pdf              the document as PDF
//   DELETE /documents/{id}                  closes it and deletes the service's copy
//
// The engine lays documents out one at a time within a process, so the service takes them to it one at a time, each
// call under a time limit and stopped when the browser goes away. A service with many users runs several processes
// (docs/servers.md) and sends each document's requests to the process that opened it. There is no sign-in: add your
// own before the service faces the internet.
using ComOfficeKit;

if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COMOFFICEKIT_LICENSE_KEY"))
    && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COMOFFICEKIT_LICENSE_FILE")))
{
    Console.Error.WriteLine("Set COMOFFICEKIT_LICENSE_KEY to your licence key, or COMOFFICEKIT_LICENSE_FILE to a licence file.");
    return 2;
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    // Published, the page is beside the program; under dotnet run, in the project folder, the current one.
    ContentRootPath = Directory.Exists(Path.Combine(AppContext.BaseDirectory, "wwwroot")) ? AppContext.BaseDirectory : null,
});
var app = builder.Build();

// A folder of the service's own, inside WorkingDirectory or the temporary folder, deleted when it stops: the engine's
// working directory, with a quota, and the copies of the documents it has open.
var parent = app.Configuration["WorkingDirectory"] is { Length: > 0 } configured ? configured : Path.GetTempPath();
var workRoot = Directory.CreateDirectory(Path.Combine(parent, $"comofficekit-viewer-{Guid.NewGuid():N}")).FullName;
var copies = Directory.CreateDirectory(Path.Combine(workRoot, "documents")).FullName;
Engine.Configure(new EngineOptions { WorkingDirectory = Path.Combine(workRoot, "engine"), WorkingDirectoryQuota = 2L << 30 });
License.Configure(LicenseStorage.Memory);
if (Environment.GetEnvironmentVariable("COMOFFICEKIT_LICENSE_FILE") is { Length: > 0 } file)
{
    License.ActivateFromFile(file);
}
else
{
    License.Activate(Environment.GetEnvironmentVariable("COMOFFICEKIT_LICENSE_KEY")!);
}

var documents = new OpenDocuments(capacity: 16, timeLimit: TimeSpan.FromSeconds(60));

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/documents", async (HttpRequest request, string? name, CancellationToken aborted) =>
{
    var extension = Path.GetExtension(name ?? "").ToLowerInvariant();
    if (extension is not (".docx" or ".xlsx" or ".xlsm" or ".pptx" or ".csv" or ".tsv"))
    {
        return Results.Problem("Send a .docx, .xlsx, .xlsm, .pptx, .csv or .tsv file, with its name in ?name=.", statusCode: 400);
    }
    // The service names its copy itself: the name the browser sends gives the format, and nothing else.
    var id = Guid.NewGuid().ToString("N");
    var copy = Path.Combine(copies, id + extension);
    try
    {
        await using (var stream = File.Create(copy))
        {
            await request.Body.CopyToAsync(stream, aborted);
        }
    }
    catch
    {
        File.Delete(copy);
        throw;
    }
    return await documents.Run(aborted, limit => documents.Open(id, copy, limit));
});

app.MapGet("/documents/{id}/pages/{page:int}", (string id, int page, double? dpi, CancellationToken aborted) =>
    documents.Run(aborted, limit =>
    {
        if (documents.Find(id) is not { } entry || page < 1 || page > entry.Pages)
        {
            return Results.NotFound();
        }
        var image = entry.Document.RenderPage(page - 1, Math.Clamp(dpi ?? 96, 24, 300), limit);
        using var png = new MemoryStream();
        image.SavePng(png);
        return Results.File(png.ToArray(), "image/png");
    }));

app.MapGet("/documents/{id}/pdf", (string id, CancellationToken aborted) =>
    documents.Run(aborted, limit =>
    {
        if (documents.Find(id) is not { } entry)
        {
            return Results.NotFound();
        }
        using var pdf = new MemoryStream();
        entry.Document.ExportPdf(pdf, limit);
        // No file name, so the browser shows it instead of saving it.
        return Results.File(pdf.ToArray(), "application/pdf");
    }));

app.MapDelete("/documents/{id}", (string id, CancellationToken aborted) =>
    documents.Run(aborted, _ => documents.Close(id) ? Results.NoContent() : Results.NotFound()));

await app.RunAsync();
documents.Dispose();
Directory.Delete(workRoot, recursive: true);
return 0;

// The documents this process has open, and the gate that takes them to the engine one at a time. Everything but
// Dispose runs inside Run, behind the gate.
sealed class OpenDocuments : IDisposable
{
    public sealed record Entry(Document Document, string Copy, int Pages);

    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<string, Entry> open = new();
    private readonly Queue<string> opened = new();
    private readonly int capacity;
    private readonly TimeSpan timeLimit;

    public OpenDocuments(int capacity, TimeSpan timeLimit)
    {
        this.capacity = capacity;
        this.timeLimit = timeLimit;
    }

    // Waits for the engine, then runs work with a token that the browser going away or the time limit cancels. A
    // cancelled call changes nothing, so the document stays open and usable.
    public async Task<IResult> Run(CancellationToken aborted, Func<CancellationToken, IResult> work)
    {
        await gate.WaitAsync(aborted);
        try
        {
            using var limit = CancellationTokenSource.CreateLinkedTokenSource(aborted);
            limit.CancelAfter(timeLimit);
            return work(limit.Token);
        }
        catch (OperationCanceledException) when (!aborted.IsCancellationRequested)
        {
            return Results.Problem($"The document took longer than {timeLimit.TotalSeconds:0} seconds.", statusCode: 504);
        }
        catch (ComOfficeKitException e)
        {
            return Results.Problem(e.Message, statusCode: e.Kind switch
            {
                ErrorKind.UnsupportedFormat or ErrorKind.Parse or ErrorKind.Unsupported => 422,
                ErrorKind.LimitExceeded => 413,
                ErrorKind.License => 503,
                _ => 500,
            });
        }
        finally
        {
            gate.Release();
        }
    }

    // Opens the copy and keeps it open under id, closing the one open longest past capacity. Counting the pages lays
    // the document out, which also tells which fonts it was drawn in instead of its own.
    public IResult Open(string id, string copy, CancellationToken limit)
    {
        Document document;
        try
        {
            document = Document.Open(copy, limit);
        }
        catch
        {
            File.Delete(copy);
            throw;
        }
        int pages;
        try
        {
            pages = document.GetPageCount(limit);
        }
        catch
        {
            document.Dispose();
            File.Delete(copy);
            throw;
        }
        open[id] = new Entry(document, copy, pages);
        opened.Enqueue(id);
        while (open.Count > capacity && opened.TryDequeue(out var oldest))
        {
            Close(oldest);
        }
        return Results.Created($"/documents/{id}", new
        {
            id,
            pages,
            kind = document.Kind.ToString(),
            fonts = document.FontSubstitutions.Select(s => new { requested = s.Requested, used = s.Used }).ToArray(),
        });
    }

    public Entry? Find(string id) => open.TryGetValue(id, out var entry) ? entry : null;

    public bool Close(string id)
    {
        if (!open.Remove(id, out var entry))
        {
            return false;
        }
        entry.Document.Dispose();
        File.Delete(entry.Copy);
        return true;
    }

    public void Dispose()
    {
        foreach (var entry in open.Values)
        {
            entry.Document.Dispose();
        }
        open.Clear();
        gate.Dispose();
    }
}
