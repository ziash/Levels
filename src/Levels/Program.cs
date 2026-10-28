using System.Globalization;
using Levels;
using Levels.Options;
using Levels.Sources;

// levels fetch — Phase 2: one snapshot of each option chain, saved raw, summary printed (spec §6.1).

const string Usage = """
    levels fetch [--crypto BTC,ETH|none] [--out DIR]

      --crypto   Deribit currencies (default BTC,ETH)
      --out      folder for the raw snapshots (default ~/levels/snapshots/<UTC date>)
    """;

if (args.Length == 0 || args[0] != "fetch" || args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine(Usage);
    return args.Length == 0 || args[0] != "fetch" ? 2 : 0;
}

string[] crypto = ["BTC", "ETH"];
string? outDir = null;
for (int i = 1; i < args.Length; i++)
{
    string? Next() => i + 1 < args.Length ? args[++i] : null;
    switch (args[i])
    {
        case "--crypto":
            var c = Next() ?? "";
            crypto = c.Equals("none", StringComparison.OrdinalIgnoreCase)
                ? []
                : c.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(x => x.ToUpperInvariant()).ToArray();
            break;
        case "--out":
            outDir = Next();
            break;
        default:
            Console.Error.WriteLine($"Unknown option {args[i]}\n{Usage}");
            return 2;
    }
}

var now = DateTime.UtcNow;
outDir ??= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "levels", "snapshots",
    now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
Directory.CreateDirectory(outDir);
string stamp = now.ToString("HHmmss", CultureInfo.InvariantCulture);

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
http.DefaultRequestHeaders.UserAgent.ParseAdd("Levels/1.4 (+https://github.com/ziash/Levels)");
using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

int failures = 0;

var deribit = new DeribitSource(http);
foreach (var currency in crypto)
{
    try
    {
        string book = await deribit.FetchBookAsync(currency, cts.Token);
        string file = Path.Combine(outDir, $"deribit-{currency}-{stamp}.json");
        await File.WriteAllTextAsync(file, book, cts.Token);

        var chain = DeribitSource.Parse(currency, book, now);
        double? dvol = null;
        try { dvol = await deribit.FetchDvolAsync(currency, now, cts.Token); }
        catch (HttpRequestException e) { Console.Error.WriteLine($"{currency}: DVOL not fetched ({e.Message})"); }

        Report.Print(Console.Out, chain, ChainSummary.From(chain),
            dvol is { } d ? (d / 100 / Math.Sqrt(365), $"DVOL {d.ToString("0.00", CultureInfo.InvariantCulture)}") : null);
        Console.WriteLine($"  saved {file}\n");
    }
    catch (Exception e) when (e is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or KeyNotFoundException)
    {
        Console.Error.WriteLine($"{currency}: Deribit fetch failed: {e.Message}\n");
        failures++;
    }
}

return failures == 0 ? 0 : 1;
