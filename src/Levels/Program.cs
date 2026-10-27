using System.Globalization;
using Levels;
using Levels.Options;
using Levels.Sources;

// levels fetch — Phase 2: one snapshot of each option chain, saved raw, summary printed (spec §6.1).

const string Usage = """
    levels fetch [--crypto BTC,ETH|none] [--us SPY|none] [--out DIR]

      --crypto   Deribit currencies (default BTC,ETH)
      --us       US symbol from Tradier (default SPY); needs TRADIER_TOKEN in ~/.config/levels/secrets.env
      --out      folder for the raw snapshots (default ~/levels/snapshots/<UTC date>)
    """;

if (args.Length == 0 || args[0] != "fetch" || args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine(Usage);
    return args.Length == 0 || args[0] != "fetch" ? 2 : 0;
}

string[] crypto = ["BTC", "ETH"];
string? us = "SPY";
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
        case "--us":
            us = Next();
            if (us is null || us.Equals("none", StringComparison.OrdinalIgnoreCase))
                us = null;
            else
                us = us.ToUpperInvariant();
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
http.DefaultRequestHeaders.UserAgent.ParseAdd("Levels/1.2 (+https://github.com/ziash/Levels)");
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
            dvol is { } d ? (d / 100 / Math.Sqrt(365), $"DVOL {d.ToString("0.00", CultureInfo.InvariantCulture)}") : null,
            null);
        Console.WriteLine($"  saved {file}\n");
    }
    catch (Exception e) when (e is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or KeyNotFoundException)
    {
        Console.Error.WriteLine($"{currency}: Deribit fetch failed: {e.Message}\n");
        failures++;
    }
}

if (us is not null)
{
    string secretsPath = Secrets.DefaultPath;
    var secrets = Secrets.Load(secretsPath);
    if (Secrets.ModeWarning(secretsPath) is { } warning)
        Console.Error.WriteLine("Warning: " + warning);

    if (!secrets.TryGetValue("TRADIER_TOKEN", out var token) || token.Length == 0)
    {
        Console.WriteLine($"""
            {us}: skipped — no TRADIER_TOKEN in {secretsPath}.
              1. Create a free developer account at https://developer.tradier.com and copy the sandbox access token.
              2. mkdir -p ~/.config/levels && nano {secretsPath}
                 TRADIER_TOKEN=<your token>
              3. chmod 600 {secretsPath}
            """);
    }
    else
    {
        try
        {
            string baseUrl = secrets.GetValueOrDefault("TRADIER_BASE") is { Length: > 0 } b ? b : TradierSource.SandboxUrl;
            var tradier = new TradierSource(http, baseUrl, token);
            Console.WriteLine($"{us}: fetching from Tradier (one request a second) …");
            var raw = await tradier.FetchAsync(us, now, cts.Token);
            string file = Path.Combine(outDir, $"tradier-{us}-{stamp}.json");
            await File.WriteAllTextAsync(file, raw.ToJsonString(), cts.Token);

            var chain = TradierSource.Parse(us, raw, now);
            Report.Print(Console.Out, chain, ChainSummary.From(chain), null,
                "compare σ per day with the Levels table on the US500 chart (VIX1D)");
            Console.WriteLine($"  saved {file}\n");
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException or InvalidOperationException or FormatException)
        {
            Console.Error.WriteLine($"{us}: Tradier fetch failed: {e.Message}\n");
            failures++;
        }
    }
}

return failures == 0 ? 0 : 1;
