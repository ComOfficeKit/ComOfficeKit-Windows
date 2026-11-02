// Converts every document in a folder to PDF, several at a time, the way a server should: in worker processes,
// each with a working folder of its own, a quota on that folder, and a time limit on every document.
//
//   dotnet run -- <input folder> <output folder> [workers] [seconds]
//
// The licence comes from COMOFFICEKIT_LICENSE_KEY (the key) or COMOFFICEKIT_LICENSE_FILE (a licence file). Your own
// service keeps it wherever it keeps secrets.
//
// Why processes: the engine lays documents out one at a time within a process, so threads do not convert faster,
// and a document that crashes or overruns its worker ends that worker only. The supervisor gives each worker one
// document at a time over its standard input and output, ends one that runs past the time limit, starts another in
// its place, and deletes the folder the one it ended worked in.
using System.Diagnostics;
using ComOfficeKit;

if (args.Length >= 3 && args[0] == "--worker")
{
    return Worker.Run(args[1], int.Parse(args[2]));
}
if (args.Length < 2)
{
    Console.Error.WriteLine("usage: Sample-ServerWorkers <input folder> <output folder> [workers] [seconds]");
    return 2;
}
if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COMOFFICEKIT_LICENSE_KEY"))
    && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COMOFFICEKIT_LICENSE_FILE")))
{
    Console.Error.WriteLine("Set COMOFFICEKIT_LICENSE_KEY to your licence key, or COMOFFICEKIT_LICENSE_FILE to a licence file.");
    return 2;
}

var input = Path.GetFullPath(args[0]);
var output = Path.GetFullPath(args[1]);
var seconds = args.Length > 3 ? int.Parse(args[3]) : 60;
Directory.CreateDirectory(output);
var jobs = new Queue<string>(Directory.EnumerateFiles(input).Where(file =>
    Path.GetExtension(file).ToLowerInvariant() is ".docx" or ".xlsx" or ".xlsm" or ".pptx" or ".csv" or ".tsv"));
var workers = Math.Max(1, Math.Min(args.Length > 2 ? int.Parse(args[2]) : Environment.ProcessorCount, jobs.Count));
var workRoot = Directory.CreateTempSubdirectory("comofficekit-workers-").FullName;
var gate = new object();
var failures = 0;

var supervisors = Enumerable.Range(0, workers).Select(_ => Task.Run(() =>
{
    var worker = WorkerProcess.Start(workRoot, seconds);
    while (true)
    {
        string job;
        lock (gate)
        {
            if (jobs.Count == 0) break;
            job = jobs.Dequeue();
        }
        var pdf = Path.Combine(output, Path.GetFileNameWithoutExtension(job) + ".pdf");
        var answer = worker.Convert(job, pdf, TimeSpan.FromSeconds(seconds));
        if (answer is null)
        {
            // Past its time, or gone: end it, delete its folder, and carry on with another.
            Console.WriteLine($"timed out  {Path.GetFileName(job)}");
            Interlocked.Increment(ref failures);
            worker.End();
            worker = WorkerProcess.Start(workRoot, seconds);
            continue;
        }
        var converted = answer.StartsWith("ok\t", StringComparison.Ordinal);
        Console.WriteLine($"{(converted ? "converted" : "failed   ")}  {Path.GetFileName(job)}  {answer[(answer.IndexOf('\t') + 1)..]}");
        if (!converted) Interlocked.Increment(ref failures);
    }
    worker.End();
})).ToArray();
Task.WaitAll(supervisors);
Directory.Delete(workRoot, recursive: true);
return failures == 0 ? 0 : 1;

// One worker process, as the supervisor sees it: a line out, a line back.
sealed class WorkerProcess
{
    private readonly Process process;
    private readonly string folder;

    private WorkerProcess(Process process, string folder)
    {
        this.process = process;
        this.folder = folder;
    }

    public static WorkerProcess Start(string workRoot, int seconds)
    {
        var folder = Path.Combine(workRoot, Guid.NewGuid().ToString("N"));
        var start = new ProcessStartInfo(Environment.ProcessPath!)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add("--worker");
        start.ArgumentList.Add(folder);
        start.ArgumentList.Add(seconds.ToString());
        return new WorkerProcess(Process.Start(start)!, folder);
    }

    // The worker's answer, or null when it gave none within the limit. A worker that ended answers that.
    public string? Convert(string input, string pdf, TimeSpan limit)
    {
        try
        {
            process.StandardInput.WriteLine($"{input}\t{pdf}");
            process.StandardInput.Flush();
        }
        catch (IOException)
        {
            return "error\tthe worker ended";
        }
        var answer = process.StandardOutput.ReadLineAsync();
        return answer.Wait(limit) ? answer.Result ?? "error\tthe worker ended" : null;
    }

    public void End()
    {
        try
        {
            process.StandardInput.Close();
            if (!process.WaitForExit(5000)) process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
        catch (Exception e) when (e is InvalidOperationException or IOException)
        {
            // already gone
        }
        if (Directory.Exists(folder)) Directory.Delete(folder, recursive: true);
    }
}

// The worker: the engine configured once, then one document at a time, each with a time limit of its own.
static class Worker
{
    public static int Run(string folder, int seconds)
    {
        Engine.Configure(new EngineOptions { WorkingDirectory = folder, WorkingDirectoryQuota = 2L << 30 });
        License.Configure(LicenseStorage.Memory);
        if (Environment.GetEnvironmentVariable("COMOFFICEKIT_LICENSE_FILE") is { Length: > 0 } file)
        {
            License.ActivateFromFile(file);
        }
        else
        {
            License.Activate(Environment.GetEnvironmentVariable("COMOFFICEKIT_LICENSE_KEY")!);
        }
        while (Console.ReadLine() is { } line)
        {
            var parts = line.Split('\t');
            string answer;
            try
            {
                // A little under the supervisor's limit, so the worker answers before it is ended.
                using var limit = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(1, seconds - 5)));
                using var document = Document.Open(parts[0], limit.Token);
                document.ExportPdf(parts[1], limit.Token);
                var fonts = string.Join(", ", document.FontSubstitutions.Select(s => $"{s.Requested} drawn in {s.Used}"));
                answer = $"ok\t{document.PageCount} pages" + (fonts.Length > 0 ? $"; fonts: {fonts}" : "");
            }
            catch (OperationCanceledException)
            {
                answer = "error\tcancelled: past its time";
            }
            catch (ComOfficeKitException e)
            {
                answer = $"error\t{e.Kind}: {e.Message}";
            }
            Console.WriteLine(answer.ReplaceLineEndings(" "));
        }
        return 0;
    }
}
