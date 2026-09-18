# EventScape — Progress Log

## 2026-09-18 — Scaffold kickoff

**Goal:** Land the first real project commits before the next check-in (Unity Editor not required for this push).

### Done
- Kept the CG IP professor baseline (`src/main.py`, `requirements.txt`, baseline screenshot).
- Added Unity-ready folder layout under `Unity/EventScape/` matching `PROJECT_CONTEXT.md` §10.
- Added C# stubs for core systems:
  - Camera: first-person, orbit, mode switcher
  - Placement: placement system, ghost preview, placeable object, collision validator
  - UI: object catalog panel + catalog button
  - Core: scene object manager (+ optional save/load stub)
- Added `Packages/manifest.json` for URP, Input System, Cinemachine, UGUI, TextMeshPro.
- Extended `.gitignore` for Unity / Git LFS-friendly ignores.
- Documented next milestones in this file and updated `README.md`.

### Not done yet (needs Unity Editor)
- Create/open a real Unity 2022.3 LTS project and import these scripts.
- Block-out auditorium shell (primitives or Blender `.fbx`).
- Prefabs for Bench / RoundTable / SquareTable / FlowerPot.
- Wire UI buttons → placement system in `MainAuditorium` scene.

### Suggested next pushes
1. **This push:** scaffold + docs (no Editor).
2. **Next push:** after Editor install — empty scene + floor plane + one placeable cube prefab wired to stubs.

---

## Milestone checklist

| Milestone | Status |
|---|---|
| Repo + baseline pipeline | Done |
| Unity folder + script stubs | Done (this push) |
| Open project in Unity 2022.3 LTS | Pending |
| Auditorium block-out | Pending |
| Camera FPS + orbit toggle | Pending |
| Placement + ghost + collision | Pending |
| Catalog UI (4 items) | Pending |
| Save/load JSON (stretch) | Optional |
