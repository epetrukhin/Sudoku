# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

A C# / .NET 10 performance study of Sudoku solvers. The repo contains two solver implementations (`V1Solver`, `V2Solver`) that are benchmarked against each other across puzzles of increasing difficulty (easy → medium → hard → diabolical). The active development loop is: implement/tune a solver, benchmark it with BenchmarkDotNet, and verify V1 and V2 produce identical results via the Runner.

## Build & run

All projects target `net10.0`; the solution uses the new XML solution format `Sudoku.slnx`.

```bash
dotnet build Sudoku.slnx                 # build everything
dotnet run -c Release --project Runner     # verify V1 and V2 agree on all test cases
dotnet run -c Release --project Benchmarks # run the BenchmarkDotNet comparison
```

There is **no unit-test framework** here — the `Tests` project is not a test runner. It is a plain class library that only holds puzzle data (`TestCases`). Verification happens through `Runner` (solver cross-checking), and performance measurement through `Benchmarks`.

## Project layout

- **`Solvers/`** — the actual solvers, a class library. Both `V1Solver.cs` and `V2Solver.cs` expose a static entry:
  ```csharp
  public static void Solve(char[][] sudoku, char[][] result)
  ```
  They solve in place into `result`. Both currently share an identical internal skeleton (a nested `Board` + abstract `Cell` hierarchy); `V2Solver` is the work-in-progress candidate meant to out-perform `V1`, and the two files are edited in lockstep for comparison.
  - `Constants.cs` — `BoardSize = 9`, `BoxSize = 3`.
  - `FormatConverter.cs` — converts between the two board encodings used across the repo (see below).
- **`Tests/`** — `TestCases.cs` only: named puzzle strings grouped into `AllEasy` / `AllMedium` / `AllHard` / `AllDiabolical`, plus `All` (all of them, as a `ReadOnlySpan<string>`). Referenced by both Runner and Benchmarks.
- **`Runner/`** — console app. Iterates every case in `TestCases.All`, solves once with V1 and once with V2, and dumps any board where the results differ. Useful as a correctness check when one solver is changed.
- **`Benchmarks/`** — BenchmarkDotNet app. `Behchmarks.cs` runs V1 vs V2 for each difficulty tier (`V1Easy` is the `Baseline`). `Config.cs` selects .NET Core 10.0 runtime, adds a memory diagnoser and P95/ops-per-second columns, with 3 launches / 10 warmup / 100 iterations.

## Key conventions

- **Two board encodings** are used interchangeably; convert via `FormatConverter`:
  - *SudokuWiki* — a single 81-char string, `'0'` = empty (this is what `TestCases` stores).
  - *LeetCode* — `char[9][]`, `'.'` = empty (this is what `Solve(char[][], char[][])` operates on).
- Solvers mutate in place; a preallocated `Result` board is reused across benchmark iterations. The `Cell` hierarchy caches object instances aggressively (`Concrete` cells are singletons) to minimize allocation — preserve this when editing.
