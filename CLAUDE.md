# CLAUDE.md — Levels project

Standing instructions for Claude Code in this repository. Read this first, then `README.md` and `docs/spec.md`.

## What this project is

**Levels**: the day's expected range (Phase 1, a TradingView Pine indicator) and later options-positioning levels — gamma by strike, TCS/TPS/TGS, gamma flip — from a small C# program, drawn on the owner's TradingView charts in the prices of the CFDs they trade. It never trades. Spec: `docs/spec.md` (single source of truth).

## Working rules

1. **Code only when the owner says so.** Build the phase asked for (spec §10), not the next ones.
2. **Spec-driven.** Any change in decisions, scope or design goes into `docs/spec.md`: bump its version in the header table, add a row to its history (§16), update the open questions (§13) and the phase table (§10). Commit messages like `levels v1.1: <what changed>`. Code comments point at spec sections, e.g. `(spec §5.2)`.
3. **Define difficult terms.** The owner is a telecom core-network engineer, not a C# developer: explain technical terms (Pine, C#, options, statistics) briefly in plain words.
4. **Explain before large changes**, in a few lines.
5. **Short, direct answers.**

## Separation — never break these

- Levels is a **separate project**: no references to Trader's or Rewind's code. A piece that is needed is **copied** with a comment naming its origin (file and commit).
- Levels never connects to a broker and never places orders. Never read, write or print `~/.config/trader/secrets.env` or anything else of Trader's; never change `~/code/Trader` or `~/tradebot/`, and never stop, restart or redeploy Trader's services. A Trader change (e.g. Phase 8) is made in the Trader repository under its own rules, on the owner's go.
- Outside data: public sources only (spec §12), polite request rates, keys in `~/.config/levels/secrets.env` (mode 600), never in the repo.

## Tech stack and conventions

- Pine Script v6, one file `pine/Levels.pine`. The first comment line carries the spec version it implements. Lines must not repaint (spec §5): only completed daily data, or values captured at the start of the day.
- From Phase 2: C#, .NET 10 LTS, nullable on, warnings as errors; one solution `Levels.sln`; `src/`, `tests/`. CSV and time rules as in Trader (UTF-8, comma, `.` decimals, UTC `yyyy-MM-dd HH:mm:ss`).
- Runtime data in `~/levels/`; never committed.

## Before finishing a task

- Pine: cannot be compiled here — re-read the script against the spec and Pine v6 rules, and say plainly that the owner's TradingView check is still due.
- C#: `dotnet build` passes; tests pass.
- Commit and `git push` (the owner works from GitHub `main`).
