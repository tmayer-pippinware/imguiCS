# Dear ImGui C# Port (imguiCS)

This repository tracks a from-scratch C# port of Dear ImGui using the upstream sources in `imgui_reference/` as the golden reference. The goal is API and behavior parity with the original C/C++ code while keeping file and section layout aligned to make diffing against upstream straightforward.

## Layout
- `imgui_reference/` — upstream Dear ImGui snapshot (C/C++) kept for reference and fixtures.
- `src/ImGui.Core/` — managed port of the core library (partial files per upstream translation unit, generated enums/typedefs, stb wrappers).
- `tests/ImGui.Core.Tests/` — unit tests for containers, config, stb text edit, and future parity fixtures.
- `docs/` — project instructions (`AGENTS.md`), TODO/milestones/TDD, and daily dev logs under `docs/dev_log/`.

## Current status
- Milestone 1 and 2 complete: solution scaffolding, shared build props, compile-time config scaffold, single-file placeholder, generated public enums/typedefs, container primitives, stb rectpack/truetype bindings, managed text edit port, and initial tests (all passing).
- Upcoming work (Milestone 3+): translate core imgui*.cpp logic into partials, internal structs/layouts, backends, examples, and packaging/CI.

## Development workflow
- Follow `docs/AGENTS.md`: log every change in `docs/dev_log/YYYY-MM-DD.md` with timestamps, maintain `docs/TODO.md`, and run automated tests after each change (add screenshot tests when rendering applies).
- Preferred tooling: `dotnet test` for unit tests; mirror upstream file boundaries when adding ported code.

## Getting started
1) Install .NET 8 SDK.
2) Restore/build/tests: `dotnet test`.
3) Browse `docs/TODO.md` and `docs/MILESTONES.md` for the current backlog and milestones.

## License
Upstream Dear ImGui uses the MIT license (see `imgui_reference/LICENSE.txt`). New C# sources should remain MIT unless noted otherwise.
