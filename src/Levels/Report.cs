using System.Globalization;
using Levels.Options;

namespace Levels;

/// <summary>The printed summary of one chain (spec §6.1).</summary>
internal static class Report
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    /// <param name="phase1">Phase 1's σ per day for this underlying and where it came from, e.g. (0.0196, "DVOL 37.51"); null if not known here.</param>
    public static void Print(TextWriter o, ChainSnapshot chain, ChainSummary s, (double Sigma, string Label)? phase1, string? phase1Hint)
    {
        o.WriteLine($"{chain.Underlying} ({chain.Source}, {chain.TakenUtc.ToString("yyyy-MM-dd HH:mm:ss", Inv)} UTC)  price {Num(chain.Price)}  {chain.Options.Count} options");

        double all = s.CallOi + s.PutOi;
        o.WriteLine(all > 0
            ? $"  Put/call OI    calls {Pct(s.CallOi / all, 0)} ({Num(s.CallOi)}) / puts {Pct(s.PutOi / all, 0)} ({Num(s.PutOi)})"
            : "  Put/call OI    no open interest");
        o.WriteLine("  Top expiries   " + string.Join(",  ", s.TopExpiries.Select(e => $"{Day(e.ExpiryUtc)} {Pct(e.Share, 0)}")));
        o.WriteLine("  Top call OI    " + Strikes(s.TopCallStrikes));
        o.WriteLine("  Top put OI     " + Strikes(s.TopPutStrikes));

        if (s.Straddle is not { } st)
        {
            o.WriteLine("  Straddle       none: no expiry 12 h away with both a call and a put price");
        }
        else
        {
            string unit = chain.Days == DayCount.Calendar ? "days" : "sessions";
            o.WriteLine($"  Straddle       expiry {Day(st.ExpiryUtc)} {st.ExpiryUtc.ToString("HH:mm", Inv)} UTC, strike {Num((double)st.Strike)}: " +
                        $"call {Num(st.Call)} + put {Num(st.Put)} = {Num(st.Call + st.Put)}; {st.Days.ToString("0.00", Inv)} {unit} to expiry");
            var parts = new List<string> { $"straddle {Pct(st.SigmaPerDay, 2)} ({Num(st.SigmaPerDay * chain.Price)})" };
            if (st.IvSigmaPerDay is { } iv)
                parts.Add($"ATM IV {Pct(iv, 2)}");
            if (phase1 is { } p)
                parts.Add($"Phase 1 ({p.Label}) {Pct(p.Sigma, 2)}");
            o.WriteLine("  σ per day      " + string.Join("   ", parts));
            if (phase1 is { } q && q.Sigma > 0)
                o.WriteLine($"  Straddle ÷ Phase 1  {(st.SigmaPerDay / q.Sigma).ToString("0.00", Inv)}");
        }
        if (phase1Hint is not null)
            o.WriteLine("  Phase 1        " + phase1Hint);
        o.WriteLine();
    }

    private static string Strikes(IReadOnlyList<StrikeOi> strikes) => strikes.Count == 0
        ? "—"
        : string.Join(",  ", strikes.Select(s => $"{Num((double)s.Strike)} ({Num(s.OpenInterest)})"));

    private static string Day(DateTime utc) => utc.ToString("dd-MMM-yyyy", Inv);
    private static string Pct(double x, int decimals) => (x * 100).ToString("F" + decimals, Inv) + " %";
    private static string Num(double x) => x.ToString(Math.Abs(x) >= 100 ? "#,0" : "#,0.##", Inv);
}
