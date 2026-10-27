# Levels

**The day's expected range — and, later, where the big option positions sit — drawn on your TradingView chart before you trade.**

A Pine Script indicator draws today's open, ±1σ and ±2σ for the chart's symbol: from the options market's own forecast (VIX, VXN, GVZ, OVX …) where one exists, from the last 20 days' candles otherwise. Later phases add a small Ubuntu program that reads option chains (Deribit, a US source) and turns them into gamma levels (TCS, TPS, TGS, gamma flip) in the prices of your CFDs. Levels never trades.

| Item | Value |
|---|---|
| Status | **Phase 1 done** (28-Oct-2026) — expected-range indicator checked on US500, USTEC, XAUUSD, EURUSD, BTCUSD ([results](docs/spec.md#56-phase-1-check-28-oct-2026)) |
| Specification | [docs/spec.md](docs/spec.md) — the single source of truth (idea, method, phases, open questions, history) |
| Indicator | [pine/Levels.pine](pine/Levels.pine) — Pine Script v6 |
| Runs on | TradingView in the browser (Ubuntu or Windows); the program (from Phase 2) on Ubuntu, .NET 10 |
| Related | [Trader](https://github.com/ziash/Trader) (the bots) and [Rewind](https://github.com/ziash/Rewind) (replay). Levels shares no code with them |

## Phases

| Phase | Name | Status |
|---|---|---|
| 0 | Repo and spec | Done |
| 1 | Expected range (Pine) | Done 28-Oct-2026 |
| 2 | Options data check | — |
| 3 | Gamma levels + morning line | — |
| 4 | Morning report (JSON + HTML, timer) | — |
| 5 | Calibration (how well the ranges held) | — |
| 6 | Alerts | — |
| 7 | Intraday crypto gamma | — |
| 8 | TM link (optional, in Trader) | — |

Details and acceptance checks: [spec §10](docs/spec.md#10-phases).

## Add the indicator to TradingView

1. Open an intraday chart (M5–H1) of your broker's symbol, e.g. `ICMARKETS:US500`.
2. Pine Editor → **Open → New indicator** → replace the template with the whole of [`pine/Levels.pine`](pine/Levels.pine) → **Save** as "Levels" → **Add to chart**.
3. The table (top right) shows where the range came from. If it says *historical* on US500, the volatility index was not found: check the list in the settings ([spec §5.4](docs/spec.md#54-settings)).
