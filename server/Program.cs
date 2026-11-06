using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseWindowsService();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);
var app = builder.Build();

var compiler = builder.Configuration.GetSection("Compiler");
var security = builder.Configuration.GetSection("Security");
var exe = compiler["Executable"] ?? "";
var replay = compiler["ReplayExecutable"] ?? "";
var workRoot = compiler["WorkDirectory"] ?? Path.Combine(AppContext.BaseDirectory, "jobs");
var maxUpload = long.TryParse(compiler["MaxUploadBytes"], out var mb) ? mb : 50L * 1024 * 1024;
var timeoutMinutes = int.TryParse(compiler["JobTimeoutMinutes"], out var tm) ? tm : 10;
var token = security["ApiToken"] ?? "";
var allowedOrigins = security.GetSection("AllowedOrigins").Get<string[]>() ?? ["https://scarcro1920.github.io"];
var replaySha = "68fb1cbcb2924182724004039de55a4c50152bb6561803c4898b7930b38132f0";
var jobs = new ConcurrentDictionary<string, Job>();
var buildLock = new SemaphoreSlim(1, 1);

Directory.CreateDirectory(workRoot);
app.Use(async (ctx, next) =>
{
    var origin = ctx.Request.Headers["Origin"].ToString();
    if (!string.IsNullOrEmpty(origin) && allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase))
    {
        ctx.Response.Headers["Access-Control-Allow-Origin"] = origin;
        ctx.Response.Headers["Access-Control-Allow-Headers"] = "Authorization, Content-Type";
        ctx.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
        ctx.Response.Headers["Vary"] = "Origin";
    }
    if (ctx.Request.Method == "OPTIONS") { ctx.Response.StatusCode = 204; return; }
    if (ctx.Request.Path.StartsWithSegments("/api"))
    {
        var supplied = ctx.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrWhiteSpace(token) || token.StartsWith("REPLACE_", StringComparison.Ordinal) ||
            supplied != "Bearer " + token)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
            return;
        }
    }
    await next();
});

app.MapGet("/health", () => Results.Ok(new { ok = true, service = "IW8 ZoneTool Remote Compiler" }));

app.MapPost("/api/jobs", async (HttpRequest request) =>
{
    if (!File.Exists(exe)) return Results.Problem("Native compiler is not installed on this server.", statusCode: 503);
    if (!File.Exists(replay)) return Results.Problem("Replay executable is not installed on this server.", statusCode: 503);
    await using var replayStream = File.OpenRead(replay);
    var replayHash = Convert.ToHexString(await SHA256.HashDataAsync(replayStream)).ToLowerInvariant();
    if (replayHash != replaySha) return Results.Problem("Configured Replay executable SHA-256 does not match supported Replay 1.20.4.", statusCode: 503);
    if (!request.HasFormContentType) return Results.BadRequest(new { error = "Expected multipart/form-data." });
    var form = await request.ReadFormAsync();
    var map = form["map"].ToString().Trim().ToLowerInvariant();
    if (!Regex.IsMatch(map, "^mp_[a-z0-9_]{1,12}$")) return Results.BadRequest(new { error = "Invalid map ID." });
    var upload = form.Files.GetFile("dump");
    if (upload is null || upload.Length == 0) return Results.BadRequest(new { error = "Missing dump ZIP." });
    if (upload.Length > maxUpload) return Results.BadRequest(new { error = $"ZIP exceeds {maxUpload} bytes." });

    var id = Guid.NewGuid().ToString("N");
    var dir = Path.Combine(workRoot, id);
    Directory.CreateDirectory(dir);
    var zipPath = Path.Combine(dir, "input.zip");
    await using (var fs = File.Create(zipPath)) await upload.CopyToAsync(fs);
    try
    {
        SafeExtract(zipPath, Path.Combine(dir, "dump"), 300L * 1024 * 1024);
        var dump = Path.Combine(dir, "dump");
        var asset = Path.Combine(dump, "maps", "mp", map + ".d3dbsp");
        var required = new[] { Path.Combine(dump, map + "_iw8_ents.txt"), asset + ".render.json", asset + ".lighting.json" };
        var missing = required.Where(p => !File.Exists(p)).ToArray();
        if (missing.Length > 0) throw new InvalidDataException("Dump is missing required files: " + string.Join(", ", missing.Select(Path.GetFileName)));
        var collision = asset + ".mwcoll";
        var havok = asset + ".havok";
        if (!File.Exists(collision) && !File.Exists(havok)) throw new InvalidDataException("Dump has neither MWCOLL nor Havok collision.");
        var job = new Job(id, map, "queued", "Upload received; waiting for compiler.", DateTimeOffset.UtcNow);
        jobs[id] = job;
        _ = Task.Run(() => RunBuildAsync(job, dump, dir, collision, havok));
        return Results.Accepted($"/api/jobs/{id}", new { jobId = id, status = "queued" });
    }
    catch (Exception e)
    {
        try { Directory.Delete(dir, true); } catch { }
        return Results.BadRequest(new { error = e.Message });
    }
});

app.MapGet("/api/jobs/{id}", (string id) =>
{
    if (!jobs.TryGetValue(id, out var job)) return Results.NotFound(new { error = "Job not found." });
    return Results.Ok(new { job.JobId, job.Map, job.Status, job.Message, job.CreatedAt, job.Log, downloadUrl = job.Status == "complete" ? $"/api/jobs/{id}/download" : null });
});

app.MapGet("/api/jobs/{id}/download", (string id) =>
{
    if (!jobs.TryGetValue(id, out var job) || job.Status != "complete" || job.ResultZip is null || !File.Exists(job.ResultZip))
        return Results.NotFound(new { error = "Fastfile output is not ready." });
    return Results.File(job.ResultZip, "application/zip", job.Map + "_fastfiles.zip");
});

app.MapGet("/", () => Results.Redirect("/health"));
app.Run();

async Task RunBuildAsync(Job job, string dump, string jobDir, string collision, string havok)
{
    await buildLock.WaitAsync();
    try
    {
        job.Status = "building";
        job.Message = "Running native Replay fastfile compiler…";
        var output = Path.Combine(jobDir, "output");
        Directory.CreateDirectory(output);
        var args = new List<string> { "build-map", dump, job.Map, "-o", output, "--replay", replay };
        if (File.Exists(collision)) { args.Add("--collision"); args.Add(collision); }
        var build = await RunProcess(exe, args, timeoutMinutes);
        job.Log = build.Output;
        if (build.ExitCode != 0) throw new Exception($"build-map failed with exit code {build.ExitCode}.");
        job.Message = "Build complete; validating fastfile output…";
        var validate = await RunProcess(exe, ["validate-output", output, job.Map], timeoutMinutes);
        job.Log += "\n\n--- validate-output ---\n" + validate.Output;
        if (validate.ExitCode != 0) throw new Exception($"validate-output failed with exit code {validate.ExitCode}.");
        var files = Directory.GetFiles(output, "*", SearchOption.AllDirectories);
        if (files.Length == 0) throw new Exception("Compiler produced no output files.");
        var resultZip = Path.Combine(jobDir, job.Map + "_fastfiles.zip");
        ZipFile.CreateFromDirectory(output, resultZip, CompressionLevel.Optimal, false);
        job.ResultZip = resultZip;
        job.Status = "complete";
        job.Message = $"Fastfile build and validate-output succeeded. {files.Length} output file(s) packaged.";
    }
    catch (Exception e)
    {
        job.Status = "failed";
        job.Message = e.Message;
        job.Log += "\nERROR: " + e;
    }
    finally { buildLock.Release(); }
}

static async Task<(int ExitCode, string Output)> RunProcess(string file, IEnumerable<string> args, int timeoutMinutes)
{
    var psi = new ProcessStartInfo(file) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
    foreach (var arg in args) psi.ArgumentList.Add(arg);
    using var process = new Process { StartInfo = psi };
    process.Start();
    var stdout = process.StandardOutput.ReadToEndAsync();
    var stderr = process.StandardError.ReadToEndAsync();
    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(timeoutMinutes));
    try { await process.WaitForExitAsync(cts.Token); }
    catch (OperationCanceledException) { try { process.Kill(true); } catch { } throw new TimeoutException($"Compiler timed out after {timeoutMinutes} minutes."); }
    return (process.ExitCode, (await stdout) + "\n" + (await stderr));
}

static void SafeExtract(string zipPath, string destination, long maxExpandedBytes)
{
    Directory.CreateDirectory(destination);
    long total = 0;
    using var archive = ZipFile.OpenRead(zipPath);
    foreach (var entry in archive.Entries)
    {
        var target = Path.GetFullPath(Path.Combine(destination, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
        if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unsafe ZIP entry path.");
        total += entry.Length;
        if (total > maxExpandedBytes) throw new InvalidDataException("Expanded ZIP exceeds server limit.");
        if (entry.FullName.EndsWith('/')) { Directory.CreateDirectory(target); continue; }
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        entry.ExtractToFile(target, true);
    }
}

sealed class Job(string jobId, string map, string status, string message, DateTimeOffset createdAt)
{
    public string JobId { get; } = jobId;
    public string Map { get; } = map;
    public string Status { get; set; } = status;
    public string Message { get; set; } = message;
    public DateTimeOffset CreatedAt { get; } = createdAt;
    public string Log { get; set; } = "";
    public string? ResultZip { get; set; }
}
