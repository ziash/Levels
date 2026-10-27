namespace Levels.Options;

/// <summary>How days to expiry are counted for σ per day (spec §6.1).</summary>
public enum DayCount
{
    /// <summary>Calendar days, 365 a year — crypto trades every day.</summary>
    Calendar,
    /// <summary>New York trading sessions (09:30–16:00, weekdays, holidays ignored), 252 a year.</summary>
    TradingSessions,
}

/// <summary>One option at the time of the snapshot.</summary>
/// <param name="Mid">Price of one option, in the same units as <paramref name="UnderlyingPrice"/>
/// (Deribit: mark price × underlying price, USD; SPY: bid/ask mid per share); null if unknown.</param>
/// <param name="UnderlyingPrice">Price of what the option is on, for its expiry (Deribit: that expiry's future).</param>
/// <param name="Iv">Implied volatility in percent (41.19 = 41.19 %); null if unknown.</param>
public sealed record OptionQuote(
    DateTime ExpiryUtc, decimal Strike, bool IsCall, double OpenInterest, double? Mid, double UnderlyingPrice, double? Iv);

/// <summary>One option chain as downloaded (spec §6.1).</summary>
/// <param name="Price">Spot price: Deribit's index price, SPY's last trade.</param>
public sealed record ChainSnapshot(
    string Underlying, string Source, DateTime TakenUtc, double Price, DayCount Days, IReadOnlyList<OptionQuote> Options);
