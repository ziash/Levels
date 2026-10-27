namespace Levels.Options;

public sealed record StrikeOi(decimal Strike, double OpenInterest);

public sealed record ExpiryOi(DateTime ExpiryUtc, double OpenInterest, double Share);

/// <summary>The at-the-money straddle of the nearest expiry and the σ it implies (spec §6.1).</summary>
/// <param name="Days">Time to expiry: calendar days or trading sessions, by the chain's <see cref="DayCount"/>.</param>
/// <param name="IvSigmaPerDay">The same strike's implied volatility as σ per day; null if the source gives none.</param>
public sealed record Straddle(
    DateTime ExpiryUtc, decimal Strike, double Call, double Put, double UnderlyingPrice, double Days,
    double SigmaToExpiry, double SigmaPerDay, double? IvSigmaPerDay);

/// <summary>The Phase 2 reads of one chain: put/call balance, top expiries and strikes, straddle σ (spec §6.1).</summary>
public sealed record ChainSummary(
    double CallOi, double PutOi, IReadOnlyList<ExpiryOi> TopExpiries,
    IReadOnlyList<StrikeOi> TopCallStrikes, IReadOnlyList<StrikeOi> TopPutStrikes, Straddle? Straddle)
{
    /// <summary>The straddle's expiry must be at least this far away; closer ones are mostly noise.</summary>
    internal static readonly TimeSpan MinTimeToExpiry = TimeSpan.FromHours(12);

    /// <summary>A straddle pays the average absolute move, which is √(2/π) ≈ 0.80 of σ; so σ = straddle × √(π/2) ÷ price.</summary>
    internal static readonly double StraddleToSigma = Math.Sqrt(Math.PI / 2);

    public static ChainSummary From(ChainSnapshot chain, int top = 5)
    {
        var options = chain.Options;
        double callOi = options.Where(o => o.IsCall).Sum(o => o.OpenInterest);
        double putOi = options.Where(o => !o.IsCall).Sum(o => o.OpenInterest);
        double allOi = callOi + putOi;

        var topExpiries = options
            .GroupBy(o => o.ExpiryUtc)
            .Select(g => new ExpiryOi(g.Key, g.Sum(o => o.OpenInterest), allOi > 0 ? g.Sum(o => o.OpenInterest) / allOi : 0))
            .OrderByDescending(e => e.OpenInterest).ThenBy(e => e.ExpiryUtc)
            .Take(3).ToList();

        return new ChainSummary(callOi, putOi, topExpiries, TopStrikes(options, true, top), TopStrikes(options, false, top),
            FindStraddle(chain));
    }

    private static List<StrikeOi> TopStrikes(IEnumerable<OptionQuote> options, bool calls, int top) => options
        .Where(o => o.IsCall == calls)
        .GroupBy(o => o.Strike)
        .Select(g => new StrikeOi(g.Key, g.Sum(o => o.OpenInterest)))
        .Where(s => s.OpenInterest > 0)
        .OrderByDescending(s => s.OpenInterest).ThenBy(s => s.Strike)
        .Take(top).ToList();

    internal static Straddle? FindStraddle(ChainSnapshot chain)
    {
        var expiry = chain.Options
            .Select(o => o.ExpiryUtc)
            .Where(e => e - chain.TakenUtc >= MinTimeToExpiry)
            .Order()
            .Cast<DateTime?>()
            .FirstOrDefault();
        if (expiry is null)
            return null;

        var atExpiry = chain.Options.Where(o => o.ExpiryUtc == expiry).ToList();
        double underlying = atExpiry.Select(o => o.UnderlyingPrice).Where(p => p > 0).DefaultIfEmpty(chain.Price).First();

        // Strikes with both a call and a put price, the one closest to the underlying.
        var pair = atExpiry
            .Where(o => o.Mid is > 0)
            .GroupBy(o => o.Strike)
            .Select(g => (Strike: g.Key, Call: g.FirstOrDefault(o => o.IsCall), Put: g.FirstOrDefault(o => !o.IsCall)))
            .Where(p => p.Call is not null && p.Put is not null)
            .OrderBy(p => Math.Abs((double)p.Strike - underlying))
            .FirstOrDefault();
        if (pair.Call is null || pair.Put is null)
            return null;

        double days = chain.Days == DayCount.Calendar
            ? (expiry.Value - chain.TakenUtc).TotalDays
            : TradingSessions(chain.TakenUtc, expiry.Value);
        if (days <= 0)
            return null;

        double call = pair.Call.Mid!.Value, put = pair.Put.Mid!.Value;
        double sigmaToExpiry = (call + put) * StraddleToSigma / underlying;

        double? iv = pair.Call.Iv is > 0 && pair.Put.Iv is > 0 ? (pair.Call.Iv.Value + pair.Put.Iv.Value) / 2
                   : pair.Call.Iv is > 0 ? pair.Call.Iv : pair.Put.Iv is > 0 ? pair.Put.Iv : null;
        double perYear = chain.Days == DayCount.Calendar ? 365 : 252;

        return new Straddle(expiry.Value, pair.Strike, call, put, underlying, days,
            sigmaToExpiry, sigmaToExpiry / Math.Sqrt(days), iv / 100 / Math.Sqrt(perYear));
    }

    internal static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    /// <summary>
    /// New York sessions (09:30–16:00 on weekdays, holidays ignored) from <paramref name="fromUtc"/> up to the expiry
    /// day's close. A session already under way counts as the part still to come.
    /// </summary>
    internal static double TradingSessions(DateTime fromUtc, DateTime expiryUtc)
    {
        var from = TimeZoneInfo.ConvertTimeFromUtc(fromUtc, NewYork);
        var lastDay = TimeZoneInfo.ConvertTimeFromUtc(expiryUtc, NewYork).Date;
        double sessions = 0;
        for (var day = from.Date; day <= lastDay; day = day.AddDays(1))
        {
            if (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;
            var open = day.AddHours(9.5);
            var close = day.AddHours(16);
            if (from >= close)
                continue;
            sessions += from <= open ? 1 : (close - from).TotalHours / 6.5;
        }
        return sessions;
    }
}
