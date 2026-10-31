# Levels — specification

**Where can price go today, where will it stall, bounce or speed up, and which way is big money leaning this week?** Levels draws the day's expected range on your TradingView chart before you trade; from Phase 3, the price levels where the largest option positions sit, labelled in plain words (Price Stall, Bounce Back, Magnet, Speed Up) and converted into the prices of the CFDs you trade at IC Markets and Pepperstone; from Phase 9, the week's big-money bias from the COT report. It never trades.

| Item | Value |
|---|---|
| Status | **Phase 1 done** (28-Oct-2026): the expected-range indicator runs on the owner's TradingView; results in §5.6. **Phase 2 done on crypto** (28-Oct-2026): `levels fetch` (§6.1) checked live on Deribit BTC/ETH; US options source chosen 29-Oct-2026: **MarketData.app** (§6.1, §13 question 4); SPY not yet in `levels fetch`. **v1.7:** gamma lines get plain-language labels — Price Stall, Bounce Back, Magnet, Speed Up (§7); weekly big-money bias from COT added as Phase 9 (§6.2). **Phase 9 done** (31-Oct-2026): the COT row runs on the owner's TradingView (free plan) and matches the CFTC's numbers (§6.2.1) |
| Document version | v1.8 (31-Oct-2026) — history in §16 |
| Owner | Zeeshan |
| Repository | [github.com/ziash/Levels](https://github.com/ziash/Levels) (public) — separate from [Trader](https://github.com/ziash/Trader) and [Rewind](https://github.com/ziash/Rewind) |
| Display | TradingView in the browser (Ubuntu or Windows), as a Pine Script indicator (`pine/Levels.pine`) |
| Program (from Phase 2) | C# / .NET 10 console tool on the Ubuntu laptop; data folder `~/levels/` |
| Related | Trader's Audit Bot measures the same brokers' spreads; TM manages the trades you open. Levels shares no code with Trader and changes nothing there |

---

## 1. Idea

The 2nd Skies Terminal (GammaLens, OptionsLens, Daily Implied Move — $249/year, see §15) sells three morning reads: the day's **expected range**, the **open-interest map**, and the **gamma levels**. All three are arithmetic on public data. Levels builds the parts that fit the owner's instruments, for free, in the place where the owner already looks: the TradingView chart.

A morning with Levels:

1. Open the US500 chart in TradingView. The indicator has already drawn today's range: the open, ±1σ (where price closes on about two days in three) and ±2σ (stretched).
2. The table in the corner says where the range came from (e.g. "implied, VIX 15.2") and where price sits now ("+0.7σ").
3. From Phase 3: paste the morning line from the Levels program into the indicator. Four lines appear, already in US500 prices and labelled in plain words (§7): **Price Stall** (where a rise tends to stall), **Bounce Back** (where a fall tends to bounce), **Magnet** (where price tends to drift and stick) and **Speed Up** (below it, moves get faster and bigger).
4. From Phase 9: the table also says which way big money leaned in the latest COT report, e.g. "Big money: Buying (COT 74 ↑, 27-Oct)" or "Crowded long — late to buy" (§6.2).
5. Look for your price-action setups at those levels. A short from a supply zone that lines up with +1σ and Price Stall, in a week when big money is selling, has more behind it than one in the middle of the range.

Three speeds: **COT** = the lean over weeks (1D/4H bias), **range and gamma lines** = today's map, **your setup** (or Scout) = the entry.

## 2. Scope

**In:** expected daily range for every symbol the owner trades (§4); gamma and open-interest levels where a liquid options market exists (US500, USTEC, XAUUSD, XAGUSD, XTIUSD, BTCUSD, ETHUSD); the weekly futures positioning (COT) of the large traders for every symbol the CFTC reports (§6.2); a morning report; checks of how well the ranges and lines held.

**Out:** placing or managing orders (that is TM's job); live options order flow (FlowLens, DeltaTape — needs paid real-time feeds); single shares; signals that tell you to buy or sell. Levels shows context; the decision stays with the owner.

## 3. How it fits together

```
                          Phase 1 (no program needed)
TradingView ──── chart symbol (ICMARKETS:US500) ─────────┐
            ──── volatility index (TVC:VIX, CBOE:GVZ …) ──┼──► Levels.pine ──► range lines + table
            ──── COT data (Phase 9) ───────────────────────┤                    (+ COT row in the table)
                                                         │
                          Phase 2–4 (Ubuntu program)     │
Deribit, US options source ──► levels (C#) ──► morning line ── paste ──┘   (+ JSON / HTML report, Phase 4)
```

- Pine Script (TradingView's indicator language) **cannot read files or the internet**. Anything TradingView already carries — chart prices, volatility indices, daily candles — is computed inside the indicator. Option chains are not on TradingView, so gamma and open-interest levels come from the Levels program and reach the chart as one pasted line (§7). The COT report **is** on TradingView, so Phase 9 needs no program (§6.2).
- Prices: the indicator works on the chart's own symbol. Chart the **broker's** symbol (`ICMARKETS:US500`, `PEPPERSTONE:NAS100` …) so the lines are in the prices you trade.

## 4. Symbols and sources

| Owner's symbol | TradingView chart (IC Markets / Pepperstone) | Implied volatility on TradingView (§5.2) | Options for gamma levels (Phase 2–3) |
|---|---|---|---|
| US500 | `ICMARKETS:US500` / `PEPPERSTONE:US500` | VIX1D, else VIX | SPX / SPY |
| USTEC | `ICMARKETS:USTEC` / `PEPPERSTONE:NAS100` | VXN | NDX / QQQ |
| XAUUSD | `…:XAUUSD` | GVZ (gold ETF volatility) | GLD |
| XTIUSD | `ICMARKETS:XTIUSD` / `PEPPERSTONE:SPOTCRUDE` | OVX (oil ETF volatility) | USO |
| BTCUSD, ETHUSD | `…:BTCUSD`, `…:ETHUSD` | Deribit DVOL (`DERIBIT:DVOL`, found 28-Oct) | Deribit (free, real time; checked 27-Oct-2026: 876 BTC options) |
| XAGUSD | `…:XAGUSD` | none | SLV |
| Forex (AUDJPY, EURGBP, EURUSD, GBPJPY, USDJPY, GBPUSD), DE40, XBRUSD, XNGUSD, XPTUSD, SOLUSD | broker symbol | none → historical (§5.3) | none (no free options open interest) |

The TradingView names of the volatility indices are inputs of the indicator. Checked in Phase 1 (§5.6): `CBOE:VIX1D`, `CBOE:VXN`, `CBOE:GVZ` and `DERIBIT:DVOL` are found on the owner's free plan; `TVC:VIX`, `CBOE:OVX` and `DERIBIT:ETHDVOL` not yet tried (§13).

## 5. Expected range (Phase 1)

### 5.1 What is drawn

For each trading day of the chart's symbol (the broker's daily candle — for forex it starts at the 17:00 New York rollover):

| Line | Value | Meaning |
|---|---|---|
| Day open | the day's first price | the anchor |
| +1σ / −1σ | open × e^(±σ) | the day closes inside this band on about 68 % of days |
| +2σ / −2σ | open × e^(±2σ) | about 95 %; price out here is stretched — exhaustion more likely than continuation |

**σ (sigma)** is the expected size of one day's move, as a fraction of price (0.0052 = 0.52 %). The lines are fixed for the whole day — they never move after the day starts — and are drawn for past days too, so you can see how ranges held.

### 5.2 σ from a volatility index ("implied")

A volatility index (VIX, VXN, GVZ, OVX, DVOL) is the options market's forecast of movement, quoted as **annualised percent**. One day's σ:

  σ = index ÷ 100 ÷ √N, with N = 252 trading days, or 365 for an index annualised over calendar days (Deribit's DVOL — crypto trades every day). N is set per list line (`/365`), not by the chart's symbol type, because TradingView may class a broker's BTCUSD as a CFD rather than crypto.

Example: VIX 15.2 → σ = 0.152 ÷ 15.87 = 0.96 %; US500 at 7,680 → ±1σ = ±73 points.

- The value used is the **last index value before the chart's day began** (the previous session's close for VIX, which stops at 16:15 New York while US500's day starts at 18:00). So the range is known before the day starts and does not repaint.
- VIX looks 30 days ahead; **VIX1D** prices only the next day and is the better match when TradingView has it. The indicator tries a list (`CBOE:VIX1D|TVC:VIX`) and uses the first that has data.
- Which index belongs to which symbol is a text list in the indicator's settings (`US500=…`), so a renamed or missing symbol is a settings change, not a code change.

### 5.3 σ from price history ("historical")

For symbols without a volatility index (or when the index is missing): the **Yang–Zhang** estimate over the last 20 completed daily candles. It uses each candle's open, high, low and close, and counts the gap between one day's close and the next day's open separately (weekend gaps in forex, the overnight gap in DE40) — so it measures more of the real movement than close-to-close changes alone.

- With n days, o = ln(open ÷ previous close), c = ln(close ÷ open), u = ln(high ÷ open), d = ln(low ÷ open):
  σ² = Var(o) + k·Var(c) + (1 − k)·mean(u(u − c) + d(d − c)), k = 0.34 ÷ (1.34 + (n + 1) ÷ (n − 1))
- Only completed days are used, so today's range does not change during the day.
- It looks backward: a quiet week gives a narrow range even if a big day is coming. Options-based ranges (§5.2) see that; this one does not, except through the event-day widening of Phase 5.

### 5.4 Settings

| Setting | Default | Meaning |
|---|---|---|
| Range source | Auto | Auto = implied where an index is listed and has data, historical otherwise; or force one of them |
| Volatility index per symbol | the list of §4 | `TICKER=INDEX` per line; `A|B` = try A, then B; `/365` at the end = N for that index |
| Volatility index (override) | empty | one index for this chart, ignoring the list (same `A|B/N` form) |
| Historical: days | 20 | days in the Yang–Zhang estimate |
| Trading days per year | 252 | N in §5.2 where the list line has no `/N` |
| Show ±2σ, Show table, colours | on | display |

### 5.5 The table

Top right, on intraday charts: the source used (`implied CBOE:VIX1D 14.8` or `historical 20 d`), σ in percent and points, today's ±1σ band, where price is now in σ from the open (`+0.7σ`), and the other source's σ for comparison. From Phase 9 a last row, **Big money**, gives the COT bias (§6.2) — on every timeframe.

### 5.6 Phase 1 check (28-Oct-2026)

The owner's check in TradingView (free plan), 15-minute charts, 27-Oct-2026 about 21:00 UTC:

| Chart | Feed | Source found | Implied σ | Historical σ (20 d) | Implied ÷ historical |
|---|---|---|---|---|---|
| US500 | Pepperstone | `CBOE:VIX1D` 10.50 | 0.66 % | 0.67 % | 0.99 |
| USTEC | IC Markets | `CBOE:VXN` 26.40 | 1.66 % | 1.13 % | 1.47 |
| XAUUSD | OANDA | `CBOE:GVZ` 23.55 | 1.48 % | 1.03 % | 1.44 |
| EURUSD | OANDA | none listed → historical | — | 0.41 % | — |
| BTCUSD | Bitstamp | `DERIBIT:DVOL` 38.56 (`/365`) | 2.02 % | 1.67 % | 1.21 |

- Compiled at the first paste. Numbers checked by hand: US500 σ = 10.50 ÷ 100 ÷ √252 = 0.66 % = 50.8 points on the 7,682.3 open, ±1σ 7,631.7–7,733.3, "Now +0.8σ" = ln(7,721.9 ÷ 7,682.3) ÷ 0.0066; BTC σ = 38.56 ÷ 100 ÷ √365 = 2.02 % (the `/365` of the list line works).
- The day starts where intended: 22:00 UTC for Pepperstone US500 (18:00 New York), 21:00 UTC for OANDA forex (17:00 New York rollover), 00:00 UTC for Bitstamp BTC. The bar where a day starts is not drawn (the gap between days).
- XAUUSD, EURUSD and BTCUSD were checked on OANDA and Bitstamp, not on the broker feeds. The method does not depend on the feed, but for trading chart the broker's symbol (§3): its prices and day start can differ a little.
- **The 30-day indices give wider bands than the candles.** VXN and GVZ give σ about 45 % above the 20-day historical σ; VIX1D, which prices only the next day, matches it. Likely reasons: the horizon (30 days include events — the big-tech earnings of late October for the Nasdaq 100) and implied volatility normally sitting above realised (the premium option sellers are paid). Not changed in Phase 1; Phase 5 measures the hit rates and decides (§13 question 7).
- Days seen on the charts: US500 14-Oct broke −1σ after the 13:30 UTC cash open and stalled at −2σ (wicks about 10 points through, 15-minute closes held), the next day's band 30 % wider as VIX1D rose; US500 27-Oct high stopped at +1σ twice. USTEC 16-Oct and 22-Oct fell to about −2σ. XAUUSD 22-Oct closed near −2σ. BTCUSD 22-Oct fell to −2σ and bounced; 24-Oct's low touched −1σ. One-off examples, not statistics — that is Phase 5.
- Cosmetic: the status line shows the inputs "Auto 20 252" even where a list line sets `/365`.

## 6. Positioning: options (Phases 2–3) and futures COT (Phase 9)

Phase 2 is specified in §6.1; Phase 3 (gamma) is still an outline, agreed in detail when it starts. What it computes per underlying (SPY/SPX, QQQ/NDX, GLD, SLV, USO, Deribit BTC/ETH):

| Read | How |
|---|---|
| Implied move from options | at-the-money straddle of the nearest expiry → σ (§6.1) — cross-check for §5.2 |
| Put/call open-interest balance | total put OI vs call OI |
| Top expiries and top strikes by OI | where the most contracts are open |
| Gamma by strike | gamma × open interest × contract size × spot² × 1 %, calls +, puts − (assumes dealers are long calls and short puts — an estimate, as in every such tool) |
| **TCS / TPS / TGS** | strike with the most call gamma / put gamma / total gamma |
| **Gamma flip** | the price where total gamma changes sign; above it dealer hedging damps moves, below it amplifies them |

Open interest is published once a day (overnight), so one snapshot before the open is enough.

### 6.1 Phase 2: `levels fetch`

A console command on the Ubuntu laptop (`dotnet run --project src/Levels -- fetch`). It downloads one snapshot of each option chain, saves the raw answer, and prints a summary. No gamma yet (Phase 3).

**Sources**

| Underlying | Source | Key | What is fetched |
|---|---|---|---|
| BTC, ETH | Deribit public API (`www.deribit.com/api/v2/public/`), real time | none | `get_book_summary_by_currency` (every option: open interest, mark price, mark IV, underlying price) — one request per currency; `get_volatility_index_data` (latest DVOL, to compare with Phase 1) |
| SPY | **MarketData.app** (`api.marketdata.app/v1/`), chosen 29-Oct-2026 (§13 question 4). Not yet written | `MARKETDATA_TOKEN` in `~/.config/levels/secrets.env`, sent as `Authorization: Bearer` (never in the URL) | `options/chain/SPY/?date=<previous US session>&expiration=<expiry>` — the end-of-day chain: every strike of one expiry with bid/ask/mid, open interest, volume and the SPY price; **no IV or greeks**, so Levels derives IV (and in Phase 3 gamma) from the mid prices. 1 credit per 1,000 contracts, i.e. 1 credit per expiry. Front, next and monthly expiry ≈ 3 credits a day |

Strike and expiry of a Deribit option come from its name (`BTC-31OCT26-87000-C` = call, strike 87,000, expiry 31-Oct-2026 08:00 UTC). The chain code keeps a trading-session day count (16:00 New York expiry, weekdays 09:30–16:00) ready for a US source.

**MarketData.app check (29-Oct-2026)** — the owner's account (trial, 10,000 credits a day; the free plan is 100), 7 requests, 13 credits:

| Request | Credits | Result |
|---|---|---|
| SPY quote; SPY expiries | 1 each | 35 expiries, the first 29-Oct-2026 (same-day expiries included) |
| Chain with `date=2026-10-28`, expiry 30-Oct, all strikes | 1 | 498 contracts; open interest, volume, bid/ask/mid, SPY price; IV and greeks empty |
| Same, expiry 20-Nov | 1 | 474 contracts |
| Chain without `date` (latest), 8 contracts | 8 | IV, delta, gamma, theta, vega filled — but 1 credit **per contract** (≈ 500 per expiry), and still the 28-Oct close (20:00 UTC), not live |
| Chain with `date=2025-10-28` | 0 | Refused: "Free/Trial users can only access up to 1 year of data" |

Answers come with HTTP 203 (data from MarketData.app's cache) and `x-api-ratelimit-*` headers (limit, remaining, consumed) — `levels fetch` prints the credits used.

**Saved** — the raw answers, unchanged, in `~/levels/snapshots/<yyyy-MM-dd>/` (UTC date): `deribit-BTC-<HHmmss>.json`, `deribit-ETH-…`. Later phases and tests can re-read a snapshot without going online.

**Printed, per underlying**

| Read | How |
|---|---|
| Price | Deribit index price |
| Put/call balance | total call OI and put OI, as % of all OI (`calls 58 % / puts 42 %`) |
| Top expiries | the 3 expiries with the most OI, with their share |
| Top strikes | the 5 strikes with the most call OI and the 5 with the most put OI, all expiries together |
| Straddle σ | the nearest expiry at least 12 hours away; its strike closest to the underlying price; straddle = call + put mid price (Deribit: mark price × underlying price, in USD). σ to expiry = straddle × √(π/2) ÷ price (≈ 1.25 × straddle ÷ price — a straddle pays the average absolute move, which is 0.80σ). Then **σ per day** = σ to expiry ÷ √T, T = days to expiry (calendar days for crypto; trading sessions for a US underlying, holidays ignored) |
| ATM IV σ | the same strike's implied volatility ÷ 100 ÷ √365 (crypto) or √252 (US) |
| Phase 1 compare | the latest DVOL ÷ 100 ÷ √365, i.e. Phase 1's σ |

The often-quoted "straddle × 0.85" is a rule of thumb for the expected move; Levels uses the exact relation so the number compares directly with Phase 1's σ.

**First run (27-Oct-2026 21:19 UTC)** — Deribit, no key:

| | Price | Put/call OI | Top expiries | Straddle (29-Oct, 1.45 days) | σ per day: straddle / ATM IV / Phase 1 (DVOL) | Straddle ÷ Phase 1 |
|---|---|---|---|---|---|---|
| BTC | 80,598 | calls 63 % / puts 37 % | 25-Dec 34 %, 30-Oct 34 %, 26-Mar 11 % | strike 80,500: 1,335 USD | 1.73 % / 1.72 % / 1.96 % (DVOL 37.49) | 0.88 |
| ETH | 2,340 | calls 67 % / puts 33 % | 25-Dec 41 %, 30-Oct 22 %, 26-Mar 14 % | strike 2,340: 55.54 USD | 2.48 % / 2.48 % / 2.78 % (DVOL 53.11) | 0.89 |

- Straddle σ and ATM IV agree to 0.01 %, so the straddle → σ relation holds.
- The straddle is about 12 % below DVOL: DVOL looks 30 days ahead and the next days are priced quieter (the same direction as VXN and GVZ in §5.6, smaller). Input for §13 question 7.
- BTC top strikes: calls 95,000 / 90,000 / 100,000, puts 60,000 / 75,000 / 70,000. ETH: calls 3,200 / 2,500 / 3,000, puts 2,300 / 2,000 / 2,400.
- Snapshots: 392 KB (BTC) and 323 KB (ETH) per fetch.

### 6.2 Weekly bias: COT (Phase 9)

The **Commitments of Traders (COT)** report is published by the US futures regulator (CFTC) every week: positions as of **Tuesday**, released **Friday 15:30 New York**. It shows how the large traders in each US futures market are positioned. Levels turns it into one plain-language row in the table: which way big money leans, and whether that side is crowded. It is a slow read for the 1D/4H bias, never an entry signal.

**Source.** TradingView carries the COT data as special tickers, built with TradingView's own library `TradingView/LibraryCOT/6` (`cot.COTTickerid(report, code, false, "<Group> Positions", "Long"/"Short", "All")` — futures only, as in the CFTC's futures-only reports), that `request.security()` can read, so Phase 9 lives inside `Levels.pine` like Phase 1 — no program. Works on the owner's free plan (§6.2.1). Fallback if the owner's plan does not serve them (§13 q9): the Levels program fetches the CFTC's public API (`publicreporting.cftc.gov`, no key) and the values travel in the morning line. Checked 29-Oct-2026 on the CFTC API: E-mini S&P 500 report of 20-Oct (Traders in Financial Futures: asset managers long 1.10 M / short 0.20 M; leveraged funds net short ≈ 371 k) and gold (Disaggregated: managed money long 121 k / short 17 k).

**Markets and the group that counts as "big money"** (defaults; a setting per symbol, §13 q8):

| Owner's symbol | COT market (CFTC code) | Report | Group |
|---|---|---|---|
| US500 | E-mini S&P 500 (13874A) | Traders in Financial Futures | Leveraged funds |
| USTEC | E-mini Nasdaq-100 (209742) | Traders in Financial Futures | Leveraged funds |
| XAUUSD / XAGUSD | Gold (088691) / Silver (084691) | Disaggregated | Managed money |
| XTIUSD / XNGUSD / XPTUSD | WTI crude (067651) / Natural gas (023651) / Platinum (076651) | Disaggregated | Managed money |
| EURUSD, GBPUSD, USDJPY | Euro FX (099741), British pound (096742), Japanese yen (097741) | Traders in Financial Futures | Leveraged funds |
| AUDJPY, EURGBP, GBPJPY | both legs: Australian dollar (232741) with yen; euro with pound; pound with yen | Traders in Financial Futures | Leveraged funds |
| BTCUSD / ETHUSD / SOLUSD | CME Bitcoin (133741) / CME Ether (146021) / CME Micro SOL (177742, a small market) | Traders in Financial Futures | Leveraged funds |
| DE40, XBRUSD | none: Eurex and ICE Europe are not in the CFTC report — §13 q10 | — | — |

All codes checked on the CFTC API on 29-Oct-2026: each has a report of 20-Oct-2026.

**What is computed**

| Read | How |
|---|---|
| Net position | group's long − short contracts (for USD-quoted pairs like USDJPY, the yen future's net with the sign flipped) |
| **COT Index** (0–100) | where this week's net sits in its range of the last 3 years (156 weeks): (net − lowest) ÷ (highest − lowest) × 100 |
| Weekly change | this week's net − last week's, shown as ↑ / ↓ |
| Crosses | score = COT Index of the base leg − COT Index of the quote leg (GBPJPY: pound − yen), −100 … +100 |

**What the table says**

| COT Index (or cross score) | Words |
|---|---|
| ≥ 90 (≥ +60) | **Crowded long — late to buy** |
| 60–89 and rising (+20 … +59) | **Buying** |
| 11–40 and falling (−20 … −59) | **Selling** |
| ≤ 10 (≤ −60) | **Crowded short — late to sell** |
| otherwise | **Neutral** |

The row is labelled **Big money**; after the words come the index or score, the arrow, and the report date: `Big money | Selling (COT 38 ↓, 20-Oct)`. Rising and falling compare the whole numbers shown, so 100 → 100 is "→". A crowded side is a warning that the trend is late, not a signal to turn. The row shows on every timeframe, including 1D and above, where the range rows say "use an intraday chart".

**No look-ahead.** The COT values are requested on the chart symbol's **weekly** bars without lookahead (`request.security(syminfo.tickerid, "W", …)`, the COT tickers nested inside). A week's report reaches past bars only once that week has closed, i.e. after its Friday release, so nothing is used before it was published. The cost: the newest report appears when the chart's next weekly bar opens — Sunday evening for forex, gold and indices — while markets that trade at the weekend (crypto) show it as soon as TradingView has it. TradingView dates a COT week by its Monday; the table shows the report's own Tuesday.

**Speed.** Importing the COT library makes the script compile in about 40 s in the Pine Editor (Phase 1 alone: a few seconds).

#### 6.2.1 Phase 9 check (31-Oct-2026)

On the owner's TradingView (free plan), 4H charts, Saturday 31-Oct-2026; expected values computed independently from the CFTC API (futures-only TFF and Disaggregated reports, same 156-report window):

| Chart | Table | CFTC: COT Index (report 20-Oct, previous 13-Oct) | |
|---|---|---|---|
| ICMARKETS:XAUUSD | Selling (COT 38 ↓, 20-Oct) | gold managed money 37.9 (39.4) | match |
| PEPPERSTONE:US500 | Neutral (COT 44 ↑) | E-mini S&P leveraged funds 44.1 (32.7) | match |
| ICMARKETS:EURUSD | Crowded short — late to sell (COT 0 →) | euro 0.0 (0.0) | match |
| ICMARKETS:GBPJPY | Neutral (COT −22 ↑) | pound 38.0 − yen 60.1 = −22.1 (−29.6) | match |
| ICMARKETS:USDJPY | Selling (COT 40 ↓) | 100 − yen 60.1 = 39.9 (46.9) | match |
| ICMARKETS:BTCUSD | Crowded long — late to buy (COT 100 →, 27-Oct) | CME Bitcoin 100.0 for 27-Oct (100.0) | match; crypto already shows the 27-Oct report (released 30-Oct), the weekday markets show it from Sunday |
| ICMARKETS:XAUUSD 1D | range rows "use an intraday chart"; Big money row as on 4H | — | as designed |

- First compile in the browser pane failed once with "Failed to fetch" after a page reload (the editor had lost its document); pasting the script again compiled it. Not a script error.

## 7. Gamma lines on the chart and the morning line (Phase 3)

### 7.1 Plain-language labels (owner, 29-Oct-2026)

Each gamma level is drawn as a horizontal line across the day, labelled in words that say what price tends to do there. The technical name, strike and expiry are in the label's tooltip (hover); a setting switches labels to **Friendly** (default), **Technical** or **Both**.

| Line (label on the chart) | Technical | Look | What to expect |
|---|---|---|---|
| **Price Stall** | TCS — the strike with the most call gamma ("call wall") | red, solid | A rise tends to slow down or stall here |
| **Bounce Back** | TPS — the strike with the most put gamma ("put wall") | green, solid | A fall tends to bounce here |
| **Magnet** | TGS — the strike with the most gamma overall | purple, dotted | Price tends to drift toward it and stick, most of all near expiry |
| **Speed Up ↓** | gamma flip — where total gamma changes sign | orange, dashed; the area below it lightly shaded orange ("speed-up zone") | Below the line, dealers' hedging adds fuel: moves get faster and bigger. Above it, their hedging calms moves |

- The table gets a **Mood** row: `Calm (above Speed Up)` or `Fast (below Speed Up)`, from where price is now.
- Two levels closer than 0.1σ share one line with both labels (e.g. "Magnet · Price Stall").
- The range lines of Phase 1 get plain labels too: ±1σ **Normal range**, ±2σ **Stretched**.
- These are estimates: every gamma tool guesses which side dealers hold (§6). Phase 5 counts how often each label held on past days (how often a rise stalled at Price Stall, a fall bounced at Bounce Back, moves below Speed Up were bigger); the wording follows those counts.

### 7.2 The morning line

One line per underlying, printed by the program and pasted into the indicator's text box, e.g.

```
US500 SPY TCS=750 TPS=740 TGS=745 FLIP=738 PC=66/34
```

The indicator converts the strikes into the chart's prices with a **live ratio**: chart price ÷ underlying price (`ICMARKETS:US500` ÷ `AMEX:SPY`, both on TradingView), so dividends and futures basis are absorbed. Exact format agreed in Phase 3.

## 8. Decisions

| # | Decision | Why |
|---|---|---|
| 1 | Display in TradingView (Pine indicator), not a separate GUI or a cTrader indicator | Runs in the browser on Ubuntu next to the bots; cTrader Desktop only runs on Windows and cTrader Web indicators cannot read local files; TradingView is already the Auto Bot's decision screen |
| 2 | Separate repository, no code shared with Trader | Same rule as Rewind: Levels never trades and must not be able to disturb the bots |
| 3 | Phase-wise: each enhancement is its own phase with its own acceptance check (§10) | Useful from Phase 1; each phase can stop without breaking the earlier ones |
| 4 | Phase 1 needs no program and no outside data | Everything it needs is already on TradingView; useful at once and free of the option-data risk |
| 5 | Range anchored at the broker's daily open; ±1σ and ±2σ | Matches the daily candles the owner reads; 68 % / 95 % are the standard reads of an implied move |
| 6 | Implied σ from the previous session's index value; historical σ from completed days only | Lines are known before the day and never repaint |
| 7 | Yang–Zhang, 20 days, equal weights for the historical range | Uses the whole candle and the overnight gap; recency weighting is a Phase 5 option once there is data to judge it |
| 8 | Strike levels converted with a live ratio inside Pine (Phase 3) | The ratio is measured from the same chart the owner trades; no cTrader connection needed |
| 9 | US options from MarketData.app's end-of-day chain (`date=` the previous session), with IV and gamma computed by Levels — not its chain with ready-made greeks (29-Oct-2026) | 1 credit per expiry instead of ~500, so a day fits the free plan's 100 credits; open interest changes once a day anyway, so yesterday's close is the right input for morning levels |
| 10 | Gamma lines labelled in plain words — Price Stall, Bounce Back, Magnet, Speed Up — with the technical names in the tooltip (owner, 29-Oct-2026; §7.1) | Says what to expect at a level instead of what it is called |
| 11 | COT added as Phase 9: a plain-language row in the table, not lines; read from TradingView's own COT data inside Pine, the CFTC API through the program only as fallback (owner, 29-Oct-2026; §6.2) | COT is a weekly bias, not a price level; no program needed when TradingView carries it |

## 9. Technology

- **Pine Script v6** for the indicator, one file `pine/Levels.pine`; added to TradingView by pasting it into the Pine Editor (§11). TradingView keeps its own copy; the repo is the master.
- **C# / .NET 10** for the program (from Phase 2), the owner's stack (same conventions as Trader and Rewind): solution `Levels.sln`, `src/`, `tests/`, warnings as errors.
- CSV and time rules as in Trader: UTF-8, comma, `.` decimals, UTC `yyyy-MM-dd HH:mm:ss`.

## 10. Phases

Each phase is one enhancement, built only when the owner says so, and closed by its acceptance check.

| Phase | Name | Content | Acceptance | Status |
|---|---|---|---|---|
| 0 | Repo and spec | This document, README, CLAUDE.md | Owner reviews | **Done 27-Oct-2026** |
| 1 | Expected range | `pine/Levels.pine`: §5 — implied or historical σ, ±1σ/±2σ lines per day, table | Owner adds it in TradingView on US500, USTEC, XAUUSD, EURUSD, BTCUSD; it compiles; the table names the source; the volatility symbols of §4 are found or the list is corrected; one day's numbers checked by hand | **Done 28-Oct-2026** — results §5.6 |
| 2 | Options data check | `levels fetch` (§6.1): download one snapshot of Deribit BTC/ETH and one US source (SPY), save it raw to `~/levels/snapshots/`, print the straddle σ, top OI strikes and expiries, put/call balance | Snapshots saved; straddle σ within reason of Phase 1's implied σ; US source chosen and recorded (§13) | **Done 28-Oct-2026 on crypto only** (owner's decision) — Deribit: first run §6.1, straddle ÷ DVOL 0.88–0.89. US source chosen 29-Oct-2026: MarketData.app (§6.1, decision 9); SPY in `levels fetch` not yet written. US500 keeps VIX1D for its range (§5.6) |
| 3 | Gamma levels | Gamma by strike, TCS/TPS/TGS, flip, front and next expiry; SPY, QQQ and GLD from MarketData.app (≈ 3 credits each a day) and Deribit BTC/ETH; the morning line (§7.2); the indicator's paste box, the plain-language lines and Mood row (§7.1) with the live ratio; plain labels on the Phase 1 range lines | Calculations match a hand-worked example in the tests; lines appear at the right US500 and BTCUSD prices with the labels of §7.1 | — |
| 4 | Morning report | JSON + HTML page per day in `~/levels/reports/`; systemd timer before the London and New York opens; layout possibly designed with Claude Design | Report on screen before the open for a week | — |
| 5 | Calibration | From history, per symbol: how often a rise stalled at Price Stall, a fall bounced at Bounce Back, and moves below Speed Up were bigger than above (§7.1); how often the day closed inside ±1σ; how often ±1σ and ±2σ were touched; how often a ±1σ break went on to reach ±2σ; how often ±2σ held on closes. Implied vs historical σ and a per-symbol multiplier for the 30-day indices (§5.6, §13 question 7); wider range on NFP/CPI/central-bank days; recency weighting tried | Hit rates per symbol in a table; ranges adjusted where they were off | — |
| 6 | Alerts | TradingView alerts when price reaches ±1σ, ±2σ, a gamma line, or crosses Speed Up | Alert fires on a test | — |
| 7 | Intraday crypto gamma | Deribit gamma levels refreshed during the day | Levels update without a restart | — |
| 8 | TM link (optional) | TM reads the day's σ for its hedge distance or a warning — built in the Trader repo under Trader's rules, on the owner's go | Agreed in Trader's TM document | — |
| 9 | Weekly bias (COT) | §6.2 in `Levels.pine`: COT Index, weekly change, crosses from both legs, plain words and report date in the table; no look-ahead | Compiles on the owner's plan; for US500, XAUUSD, EURUSD, GBPJPY and BTCUSD the table matches the CFTC report of the same week, checked by hand; a past week's bias appears only from its Friday | **Done 31-Oct-2026** — results §6.2.1 |

Phases need not be built in number order. Suggested next: **2** for SPY, then **3**.

## 11. Using the indicator (Phase 1)

1. In TradingView: open a chart of `ICMARKETS:US500` on an intraday timeframe (M5–H1).
2. Pine Editor (bottom panel) → **Open → New indicator** → delete the template → paste the whole of `pine/Levels.pine` → **Save** (name it "Levels") → **Add to chart**.
3. Settings (gear icon on the indicator): check the volatility list; the table shows whether the implied source was found.
4. After an update in the repo: open the saved script in the Pine Editor, replace its text, save. The version in the first comment line shows which one is on the chart. Since Phase 9 a compile takes about 40 s (the COT library).
5. COT settings (group **Weekly bias (COT)**): the market list per symbol (`TICKER=F:code` or `D:code`, two legs for a cross), an override for one chart, which group counts as big money, and the index window (156 weeks).

## 12. Data and safety

- Levels never connects to a broker and never places an order. No broker secrets are read; Trader's `~/.config/trader/secrets.env` is never touched.
- From Phase 2 the program reads public data only (Deribit's public API; the US options source chosen in Phase 2). Any key for that source lives in `~/.config/levels/secrets.env` (mode 600), never in the repo.
- Runtime data in `~/levels/` (`snapshots/`, `reports/`), outside the repo.

## 13. Open questions

| # | Question | Needed for |
|---|---|---|
| 1 | ~~Exact TradingView names on the owner's plan~~ — mostly answered 28-Oct-2026 (§5.6): `CBOE:VIX1D`, `CBOE:VXN`, `CBOE:GVZ`, `DERIBIT:DVOL` found on the free plan. Still to try: `TVC:VIX` (fallback), `CBOE:OVX`, `DERIBIT:ETHDVOL`, a DAX volatility index (VDAX / VSTOXX) | Oil, ETH, DE40 |
| 2 | TradingView plan (the owner's is free): the four indices of §5.6 are available; still to check: indicators per chart, alerts | Phase 6 |
| 3 | Does IC Markets' (and Pepperstone's) TradingView connection trade on the **same cTrader account**? If not, TM does not see trades placed from TradingView | Trading from TradingView (not Levels itself) |
| 4 | ~~US options source~~ — **answered 29-Oct-2026: MarketData.app** (§6.1 check, decision 9). Sign-up from Pakistan worked; the end-of-day SPY chain costs 1 credit per expiry with open interest and bid/ask (IV and gamma computed by Levels). Earlier tries: Tradier (v1.2) does not open accounts for residents of Pakistan; Alpha Vantage (v1.3) `HISTORICAL_OPTIONS` is premium on a free key; yfinance (unofficial scraping) and CBOE's delayed JSON ruled out; Massive (Polygon) has no free options plan. **Still to check:** what the free plan gives when the trial ends (credits, delay); the 1-year history limit caps Phase 5's back-test for US symbols; Levels' own gamma vs MarketData.app's | Phase 3 (US), Phase 5 |
| 5 | Anchor of the range for US indices: the CFD's daily open (now) or the 09:30 New York cash open | Phase 5 |
| 6 | Where the morning line goes besides the paste box (file, Telegram) | Phase 4 |
| 7 | The 30-day indices (VXN, GVZ) give σ about 45 % above realised, VIX1D does not (§5.6). Scale them per symbol (e.g. a list line `USTEC=CBOE:VXN*0.7`), or derive USTEC from VIX1D × VXN ÷ VIX? Decided from Phase 5's hit rates | Phase 5 |
| 8 | Which COT group is "big money" per market (§6.2 defaults: leveraged funds for indices, currencies and crypto; managed money for commodities)? Asset managers or the Legacy report's non-commercials are the alternatives | Phase 9 |
| 9 | ~~Does the owner's TradingView plan serve the COT tickers, and is there look-ahead?~~ — **answered 31-Oct-2026**: served on the free plan; requested on weekly bars without lookahead, a report reaches the chart only after its week closes (§6.2, §6.2.1) | Phase 9 |
| 10 | COT for DE40 (Eurex) and XBRUSD (ICE Europe Brent) is not in the CFTC report: leave them without a COT row, or use WTI as a stand-in for Brent? SOLUSD's CME Micro SOL is reported but small — useful? | Phase 9 |

## 14. Glossary

- **σ (sigma), standard deviation** — the typical size of a move; about 68 % of outcomes fall within ±1σ, 95 % within ±2σ.
- **Implied volatility** — the movement option prices are paying for; a forecast. **Historical (realised) volatility** — the movement that actually happened.
- **Volatility index** — an index computed from option prices that quotes implied volatility in annualised percent: VIX (S&P 500), VXN (Nasdaq 100), GVZ (gold), OVX (oil), DVOL (Deribit bitcoin).
- **Annualised** — scaled to one year; divide by √(days per year) for one day.
- **Open interest (OI)** — the number of option contracts still open at a strike; updated once a day.
- **Gamma** — how fast an option's delta changes with price; dealers who are short gamma must chase moves, long gamma damps them.
- **Call wall / put wall** — the strike with the most call / put gamma; on the chart **Price Stall** / **Bounce Back** (§7.1).
- **Gamma flip** — the price where dealers' total gamma changes sign; on the chart **Speed Up**: below it moves tend to be faster and bigger.
- **COT (Commitments of Traders)** — the CFTC's weekly report of how large traders are positioned in US futures; positions as of Tuesday, released Friday.
- **COT Index** — where this week's net position sits in its 3-year range, 0 (most short) to 100 (most long).
- **Leveraged funds / asset managers / managed money** — COT's groups: hedge funds and fast traders / pension and mutual funds / commodity funds.
- **Pine Script** — TradingView's language for indicators; runs on TradingView's servers on the chart's data.
- **Repainting** — an indicator changing values on past bars after the fact; Levels' lines must not.

## 15. Background

The idea comes from 2nd Skies Trading (Chris Capre) product pages read on 27-Oct-2026: GammaLens (live gamma positioning: TCS, TPS, TGS, bull/bear pivot), OptionsLens (daily open-interest snapshot at 7:30 ET) and Daily Implied Move (the day's expected range), sold as the 2nd Skies Terminal ($249/year) or with the PFP course ($999–$1,499/year). The owner is a former student of the method. Levels reproduces the public-data arithmetic, not the products. The plain-language labels (§7.1) and the COT bias (§6.2) were added at the owner's request on 29-Oct-2026.

## 16. History

| Version | Date | Change |
|---|---|---|
| v1.0 | 27 Oct 2026 | First version: idea, scope, sources, Phase 1 range method (§5), outline of gamma levels and the morning line, decisions, phases 0–8, open questions. Phase 1 indicator written |
| v1.1 | 28 Oct 2026 | Phase 1 done: owner's check on US500, USTEC, XAUUSD, EURUSD, BTCUSD (§5.6); volatility index names found (§4, §13 q1–2); 30-day indices wider than realised → open question 7; Phase 5 gets the band-touch counts. Indicator unchanged |
| v1.2 | 28 Oct 2026 | Phase 2 specified (§6.1) and written: `levels fetch` for Deribit BTC/ETH and Tradier SPY; Tradier chosen as the US source (§13 q4); straddle → σ by the exact relation instead of × 0.85 |
| v1.3 | 28 Oct 2026 | US options source changed from Tradier to Alpha Vantage (Tradier refuses Pakistan residents): end-of-day SPY chain plus the daily close, time to expiry counted from the chain's close (§6.1, §13 q4) |
| v1.4 | 28 Oct 2026 | Alpha Vantage's options chain is premium on a free key; Alpha Vantage source and key handling removed. Phase 2 done on crypto only (owner's decision); §13 q4 reopened with what was tried and MarketData.app as the next candidate |
| v1.5 | 29 Oct 2026 | Repository made public (owner); no secrets in its history |
| v1.6 | 29 Oct 2026 | US options source: MarketData.app chosen (§13 q4 answered, decision 9), check results in §6.1 — end-of-day SPY chain with open interest for 1 credit per expiry, IV and gamma computed by Levels; 1-year history limit on Free/Trial. No code yet |
| v1.7 | 29 Oct 2026 | Refined for the owner: gamma lines in plain language — Price Stall, Bounce Back, Magnet, Speed Up with a shaded speed-up zone and a Mood row; Phase 1 range lines labelled Normal range / Stretched (§7.1, decision 10). COT added as Phase 9: weekly big-money bias in the table from TradingView's COT data, CFTC API as fallback (§6.2, decision 11; §13 q8–10). Phase 3 covers SPY, QQQ, GLD and BTC/ETH; Phase 5 counts how often the labels held; suggested build order 9 → 2 (SPY) → 3. No code yet |
| v1.8 | 31 Oct 2026 | **Phase 9 built and checked**: COT row in `Levels.pine` via `TradingView/LibraryCOT/6`, on weekly bars without lookahead; matches the CFTC on XAUUSD, US500, EURUSD, GBPJPY, USDJPY, BTCUSD (§6.2.1). Words shortened to Buying / Selling under the row label Big money; report date shown as the Tuesday; arrows compare whole numbers; newest report appears when the next weekly bar opens (§6.2). §13 q9 answered |
