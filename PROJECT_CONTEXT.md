# Project Context: Virtual Auditorium — Interactive 3D Auditorium Decoration & Event Layout Planner

This document is a complete context brief for an AI coding agent (e.g. Claude Code) picking up work on this project. Read this fully before writing or modifying any code.

---

## 1. Course Context

- University: Navrachana University
- Course: Computer Graphics and Image Processing (CMP513 + CMP514), 5th semester
- Type: End-semester group project
- Formal proposal submissions (Part A and Part B) have already been written and submitted to faculty based on this plan — this document should stay consistent with those, but is the working technical reference for actually building the project.

---

## 2. Problem & Motivation

The university auditorium is used by the college and student clubs to host events. It is booked almost continuously, so decoration and layout planning (benches, tables, flower pots, stage decor, etc.) can only happen in the final day(s) before an event. There is currently no way to preview or plan an arrangement in advance — layouts are decided on the spot or from rough paper sketches, leading to wasted time, space, and last-minute rearrangement on the event day itself.

## 3. Goal

Build an interactive 3D desktop application that recreates the university auditorium to scale, lets a user freely navigate the space (walk or orbit), and lets them place, rotate, and remove decoration objects from a side catalog panel to plan and finalize an event layout digitally before the real setup day.

It is explicitly **not** a full game — no scoring, no win condition, no NPCs. It's closer to a lightweight "interior design / event-planning simulator."

## 4. Target Users

University event committees and student club members who are not necessarily technical — the UI and interaction model must stay simple and self-explanatory.

---

## 5. Scope

### In scope
- Navigable 3D model of the auditorium (to real/approximate scale)
- Object catalog side panel with at least: benches, round tables, square tables, flower pots (extendable to more decoration items)
- Real-time placement, rotation, and deletion of objects
- Collision-aware placement (objects should not overlap walls or each other)
- Two camera modes: first-person walkthrough and top-down/orbit view

### Out of scope (do not build these unless explicitly asked later)
- Multiplayer / real-time collaborative editing
- Photorealistic rendering or ray tracing
- Mobile app version
- AI-based automatic layout suggestions
- Budget/cost estimation for decoration items

---

## 6. Core Systems (build these as largely independent modules)

### A. Auditorium Environment
A 3D model of the real auditorium: floor, walls, stage, ceiling, and any fixed structural elements (pillars, fixed seating if present). Built to scale based on real measurements of the actual hall. Source: modeled in Blender and imported as `.fbx`, or blocked out directly in Unity using primitives if time is short.

### B. Camera / Navigation System
- **First-person mode**: WASD movement + mouse look, collision with walls/floor.
- **Orbit/top-down mode**: click-drag to rotate, scroll to zoom, similar to a map/planning view.
- User can toggle between the two modes at runtime.
- Recommended: Unity's Cinemachine package for managing camera state/transitions.

### C. Object Catalog + Side UI Panel
A Unity Canvas UI (docked to one side of the screen) listing available decoration objects as buttons/icons. Clicking a button sets "currently selected object type to place."

### D. Placement System (core mechanic)
1. User selects an object type from the UI panel.
2. A semi-transparent "ghost" preview of that object follows the cursor, using a raycast from the camera through the cursor position to the floor.
3. User optionally rotates the ghost (key press or scroll) before confirming.
4. Click places (instantiates) the real object at that position.
5. Placed objects can be reselected afterward to move, rotate, or delete.
6. Collision checks prevent placing objects overlapping walls, other objects, or outside the auditorium bounds.

### E. Assets (furniture/decoration)
Simple, low-poly, game-ready 3D models. Preferred free sources: Kenney.nl asset packs, Poly Pizza, Sketchfab (filtered to downloadable/free license). Simple geometric items (basic tables, benches) can also be modeled directly in Blender or built from Unity primitives if suitable free assets aren't found.

### F. (Optional, stretch goal) Save/Load Layout
Serialize placed object types, positions, and rotations to JSON so a layout can be saved and reloaded later. Only build this if core systems (A–E) are complete and stable. Requires the `com.unity.nuget.newtonsoft-json` package.

---

## 7. System Architecture (data/control flow)

```
User
  |
3D Auditorium Scene (Unity)
  |
Camera / Navigation Controller  <-- toggles first-person <-> orbit
  |
UI Object Selection Panel  --select object type-->  Placement System
                                                        |
                                        Raycast (camera -> floor) + Ghost Preview
                                                        |
                                          Scene Object Manager
                                    (Instantiate / Rotate / Delete / Collision Check)
                                                        |
                                    Final Layout (live preview + optional saved JSON)
```

---

## 8. Technology Stack

| Component | Choice |
|---|---|
| Engine | Unity (2022.3 LTS or newer) |
| Language | C# |
| Render pipeline | URP (Universal Render Pipeline) |
| 3D modeling | Blender (3.6+), export to `.fbx` |
| Camera system | Unity Cinemachine |
| Input | Unity Input System (`com.unity.inputsystem`) |
| UI | Unity UGUI (Canvas) + TextMeshPro |
| Optional serialization | Newtonsoft.Json for Unity |
| IDE | Visual Studio 2022 Community or VS Code + C# Dev Kit |
| Version control | Git + Git LFS (required — binary assets like `.fbx`/`.blend`/textures get large) |

A full `requirements.txt`-equivalent environment file already exists in the repo root documenting exact packages — keep it in sync if new packages are added.

---

## 9. Computer Graphics Concepts / Techniques Involved

Relevant for both implementation and for the course's grading criteria (this is a CG&IP course project, not just a generic app):
- Ray casting (camera → world, for placement targeting)
- 3D affine transformations (translate/rotate/scale of placed objects)
- Perspective projection and camera view transformation
- Bounding-box / collision detection
- Real-time lighting and shading (via URP)
- UV and texture mapping (auditorium surfaces and furniture materials)
- Mesh import/optimization (Blender → Unity FBX pipeline)
- Frustum culling (rendering optimization, mostly handled by Unity but worth understanding/documenting)

---

## 10. Suggested Unity Project Structure

```
Assets/
  Scenes/
    MainAuditorium.unity
  Scripts/
    Camera/
      FirstPersonController.cs
      OrbitCameraController.cs
      CameraModeSwitcher.cs
    Placement/
      PlacementSystem.cs
      GhostPreview.cs
      PlaceableObject.cs
      CollisionValidator.cs
    UI/
      ObjectCatalogPanel.cs
      CatalogButton.cs
    Core/
      SceneObjectManager.cs
      LayoutSaveLoad.cs (optional/stretch)
  Prefabs/
    Furniture/
      Bench.prefab
      RoundTable.prefab
      SquareTable.prefab
      FlowerPot.prefab
    Environment/
      AuditoriumShell.prefab
  Models/
    Environment/   (.fbx from Blender)
    Furniture/      (.fbx from Blender or downloaded assets)
  Materials/
  Textures/
  UI/
    Icons/
Packages/
  manifest.json
  packages-lock.json
requirements.txt
PROJECT_CONTEXT.md   <- this file
```

## 11. Naming Conventions

- C# scripts: PascalCase, one public class per file, filename matches class name.
- Prefabs: PascalCase, descriptive (`RoundTable.prefab`, not `Table2.prefab`).
- Scenes: PascalCase (`MainAuditorium.unity`).
- GameObjects in hierarchy: PascalCase, grouped under empty parent objects (`--- ENVIRONMENT ---`, `--- PLACED_OBJECTS ---`, `--- UI ---`) for readability.

---

## 12. Development Plan (10-week timeline)

| Week | Activity |
|---|---|
| 1 | Literature survey, requirement analysis |
| 2 | Requirement analysis, finalize object catalog list |
| 3 | Auditorium measurement + 3D modeling in Blender |
| 4 | Import assets, scene setup in Unity |
| 5 | Camera & navigation system (first-person + orbit) |
| 6 | Placement system (raycast + ghost preview + instantiate) |
| 7 | UI panel integration, rotate/delete, collision handling |
| 8 | Testing and bug fixing |
| 9 | Documentation |
| 10 | Final presentation / demo prep |

## 13. Work Breakdown (generic — adjust once team roles are finalized)

- Member 1: 3D modeling & environment design
- Member 2: Camera, navigation & placement system (Unity/C#)
- Member 3: UI design & object catalog
- All members: testing & documentation

## 14. Expected Deliverables

- Working Unity application (walkthrough + placement demo)
- Source code (Unity project + C# scripts) in this GitHub repo
- 3D model files (auditorium + furniture assets)
- Project documentation/report (already drafted — Part A and Part B proposal docs)
- User manual for event committee members
- Presentation slides
- Demo video of the walkthrough and placement features

---

## 15. Known Decisions So Far

- Engine: Unity, confirmed (not Unreal, not a web/Three.js build)
- Title: working title is "Virtual Auditorium – Interactive 3D Auditorium Decoration & Event Layout Planner"; alternate shorter names considered but not finalized: AuditoriumVR, EventScape 3D, AudiPlan 3D
- Literature survey benchmarked against: AllSeated, Social Tables (Cvent Diagramming), RoomSketcher
- No external dataset used — auditorium is modeled from manual on-site measurements and reference photos

## 16. Open / Not Yet Decided

- Final team member names and enrollment numbers (needed for proposal cover pages)
- Final project title (if the team wants to move off the working title)
- Exact final list of decoration objects beyond the four core ones (benches, round tables, square tables, flower pots) — e.g. whether to add stage backdrops, lighting fixtures, banners
- Whether the save/load-layout stretch goal will be attempted
- Real measured dimensions of the actual auditorium (needed before finalizing the 3D environment model)

---

## 17. Instructions for an AI Coding Agent Picking This Up

- Treat sections 5–11 as binding technical scope unless the user says otherwise.
- Do not add out-of-scope features (section 5) without explicit confirmation.
- Follow the folder structure and naming conventions in sections 10–11 for any new files.
- Keep `requirements.txt` (environment doc) and `Packages/manifest.json` in sync when adding new Unity packages.
- This is a student coursework project graded partly on demonstrating computer-graphics fundamentals (section 9) — prefer implementations that make those concepts explicit and explainable (e.g. a clearly separated raycast-based placement script) over black-boxed shortcuts.
