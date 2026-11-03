# Virtual Auditorium — project context

## Purpose

An interactive 3D desktop tool for university event organizers to explore an auditorium and arrange event furniture and decorations before an event. The first milestone establishes a usable architectural-visualization foundation built from Unity primitives.

## Current scope

The foundation creates a lightweight, fully enclosed procedural auditorium, raised stage, entrance, restrained lighting/materials, two navigation camera modes, and a generated UGUI shell. The MVP includes a procedural thirteen-item furniture catalog, cursor-driven placement and editing, collision validation, JSON layout save/load/reset, three built-in event templates, and an independent named-layout library. Planning View is the initial mode; first-person walkthrough is entered explicitly from the title bar.

The current configured interior is 16 m wide (X) × 25 m long (Z) × 3.65 m floor-to-ceiling, with 0.15 m walls. The stage is 9.5 m × 3.7 m with its top at Y=0.45 m, centered at the positive-Z end. These values are the requested project-demo geometry; they are not verified measurements of the university's real hall. Replace them with measured values before operational use.

## Technical choices

- Unity Editor: 6000.3 (Unity 6.3 LTS).
- Rendering: Universal Render Pipeline, pinned to the matching 17.3 package line.
- UI: Unity UI (UGUI), built-in `Text`, `Button`, `CanvasScaler`, `GraphicRaycaster`, and `StandaloneInputModule`; no TextMesh Pro dependency.
- Input: legacy Input Manager APIs (`UnityEngine.Input`) with `StandaloneInputModule`. The project has no Input System package dependency; keep camera and UI input on this same path unless active input handling and the EventSystem module are deliberately migrated together.
- `ProjectSettings/ProjectSettings.asset` is present and sets `activeInputHandler: 0` (Input Manager / legacy); the runtime and EventSystem both use the legacy input path.
- Geometry: built-in primitives for structural colliders and batched procedural meshes for architectural detail, tiled flooring, stage finish, and optional reference seating.
- Rendering: URP render scale 1.0, 4× MSAA with generated cameras permitting MSAA, and trilinear/mipmapped/anisotropic filtering for the generated wood grain. There is no dynamic-resolution reduction. Game View scaling can still change perceived sharpness.
- Scale: approximately 1 Unity unit per metre.
- Performance: a small set of warm realtime lights (directional key, stage/rear/side spots, a few point fills), batched architectural meshes, shared materials, no external mesh packages.
- `Auditorium_Ceiling` is a generated geometry root containing acoustic panels, grid rails, a curved fabric canopy, HVAC ducting, and visible light fittings. `CameraModeController` shows it in Walkthrough and hides the complete ceiling assembly in Planning View.
- The Editor command creates/repairs URP, material, texture import, and settings assets. Re-running it backs up the main scene and replaces `Environment`, `Stage`, `Lighting`, `WallFixtures`, `ReferenceSeating`, `RearFeatureWall`, `LeftSideWall`, and `RightSideWall`; planner roots, cameras, EventSystem, persistence, and placed layouts are preserved. The generated `Interface` root is regenerated to update catalog controls.

## Architecture map

| Area | Responsibility |
| --- | --- |
| `Scripts/Core` | `AuditoriumDemoSettings`, the editable dimensions/configuration asset, including walkthrough spawn X offset and rear-depth fraction. |
| `Scripts/Environment` | `AuditoriumArchitectureBuilder` rebuilds fixed architecture; `AuditoriumBuilder` reuses planner systems; `ReferenceSeatingController` toggles fixed seating. `AuditoriumMaterialLibrary` holds reusable URP materials/artwork slots. |
| `Scripts/Camera` | `WalkthroughCameraController` uses a 1.75 m CharacterController, 0.28 m radius, 2 m/s walk, 3 m/s Shift walk, gravity, collisions and clamped mouse-look; `PlanningCameraController` retains orbit/zoom state; `CameraModeController` switches modes and handles Escape to exit walkthrough. |
| `Scripts/Placement` | `FurnitureCatalog` centralizes item names, categories, dimensions, and stable IDs; `FurnitureFactory` builds all thirteen types from low-poly primitives; `PlacementSystem` manages snapped previews, placement, selection, movement, rotation, duplication, deletion, and bounded undo/redo. Template replacement/addition is validated as a complete transaction and enters history as one before/after layout snapshot. `PlacementValidator` checks room boundaries, stage clearance, and physics overlaps. `LayoutTemplateCatalog` generates the three bundled arrangements from furniture dimensions, configurable aisle/clearance targets, auditorium bounds, and actual stage direction; `LayoutTemplateService` converts the local template data to world placement records and applies them through the existing system. |
| `Scripts/UI` | `PlannerUIBuilder` creates the scalable, scrollable desktop shell, furniture catalog, grid settings, editing/history actions, camera views, room measurements, and save controls. `PlannerUiResponsiveLayout` keeps the CanvasScaler at 1920×1080 / match 0.5 and clamps header, catalog, and help panels inside the Canvas as the Game view resizes. `LayoutTemplateUI` adds the gallery, data-generated top-down previews, add/replace confirmations, and named-layout save/list/load/rename/delete dialogs to the same Canvas and EventSystem, fitting dialogs to the current Canvas size. `LayoutTemplatePreviewGraphic` draws the same generated placement records used for application as an aspect-correct, lightweight UGUI schematic, with visible diagnostics for invalid data. `PlannerControlsPanel` connects UI events and reports layout/status information; at startup it repairs template entry points and responsive anchors on older generated scenes by reusing the existing `Interface` Canvas and `Persistence` root. |
| `Scripts/Persistence` | `AuditoriumLayoutData` records and `LayoutPersistenceService`, which validates and atomically writes/loads quick-save JSON under `Application.persistentDataPath/VirtualAuditorium/layout.json` and named layouts under `Application.persistentDataPath/VirtualAuditorium/Layouts/`. Named layouts retain the existing version-1 furniture record format plus optional name/description/timestamps; old version-1 files remain loadable, and files without a name use a readable filename-derived display fallback. |
| `Editor` | Idempotent scene generation, asset setup, and Build Settings registration. |

## Generated hierarchy

- `Environment`: `Auditorium_Floor` with one `PlacementSurface` collider and batched tile finish; named collidable perimeter walls and `Auditorium_Ceiling` detail root.
- `RearFeatureWall`, `LeftSideWall`, `RightSideWall`, and `WallFixtures`: separate generated architecture roots for surface detail and wall-mounted elements.
- `Stage`: `Auditorium_Stage` remains a solid 9.5 × 3.7 m platform from Y=0 to Y=0.45 with a batched timber plank finish, a `PlacementSurface` marker for stage AV items, two collidable access steps, walnut/screen backdrop, banners, and a collidable fixed podium.
- `WallFixtures`: batched fixed wall fans, speakers, and plates. These decorative elements do not have colliders.
- `ReferenceSeating`: optional fixed dark seating mesh and one static collision mesh. It remains separate from `PlacedObjects` and is toggled by the side-panel **Reference Seating: ON/OFF** button.
- `Lighting`: one directional light and two stage spotlights.
- `Cameras`: `WalkthroughRig` with a CharacterController and child `WalkthroughCamera`, plus the independent `PlanningCamera`; `CameraModeController` toggles between them.
- `PlacedObjects`: contains confirmed furniture instances; selected furniture gets a temporary wireframe outline without changing its materials.
- `PlacementSystem`: owns the current translucent placement preview and placement controller.
- `Persistence`: owns the configured layout persistence service; the JSON file remains in Unity's per-application persistent data directory, not in the project checkout.
- `Interface`: title/mode bar, category-organized thirteen-item scrollable furniture catalog, Finish Placing / Select-Edit action, selection/instruction text, status/help panel, compact walkthrough overlay, and Canvas/EventSystem integration.

## Naming and code conventions

- One public `MonoBehaviour` per file; filenames match class names.
- PascalCase types/public API; camelCase private fields.
- Runtime code stays free of `UnityEditor` references.
- Keep scene generation repeatable and keep geometry, camera, validation, UI, and persistence responsibilities separate.

## Controls

- Planning View is the initial mode. Click **Enter Walkthrough** or press `Tab` to enter first-person; click **Exit Walkthrough** / **Planning View**, or press `Escape`, to return to the retained planning camera pose.
- Walkthrough: `W/A/S/D` move at 2 m/s; hold `Shift` for 3 m/s; hold right mouse and drag to look. The spawn is near the rear entrance facing the stage and is configurable in `AuditoriumDemoSettings`. The CharacterController collides with walls, stage, fixed seating, and placed furniture; its 0.28 m step offset traverses the two stage steps.
- Planning view: hold right mouse and drag to orbit; scroll to zoom; `R` resets the planning view. `Home` resets the current camera; `F1/F2/F3` select Top/Front/Isometric planning views.
- `Tab` or the on-screen **Enter Walkthrough** / **Exit Walkthrough** and **Planning View** controls switches cameras. Entering walkthrough cancels any in-progress preview, retains confirmed furniture, and suspends object selection until Planning View resumes.
- Camera input pauses while the pointer is over UI controls or furniture placement is active.
- Choose a catalog item to enter repeated-placement mode; each successful click places an item and keeps the preview active. **Finish Placing** exits to Select / Edit mode, where clicking a placed object selects it. `Escape` cancels the current preview. The mode action offers **Cancel Move** or **Cancel Duplicate** during those previews. Placement blocks camera navigation until the operation ends.
- Event Lectern, Microphone Stand, and PA Floor Speaker can be placed on the stage surface if their rotated footprints fit; other catalog furniture remains blocked from the stage. The fixed architectural podium remains a collision obstacle.
- Click placed furniture to select it. Use **Move Selected** for a validated preview; click to confirm or `Escape` to restore the original pose. **Rotate Left/Right** turns the selected item by 15 degrees; **Duplicate Selected** creates a same-type, scale, and rotation preview with a 2 m offset; **Delete Selected** removes it. A selected object is highlighted with a temporary wireframe.
- New placement, selection, and moving are mutually exclusive. UI clicks do not place or select objects, and switching camera modes remains available during editing.
- **Save Layout** cancels any active preview (restoring a move when necessary) and stores confirmed furniture type IDs, stable instance IDs, world positions, upright Euler rotations, and local scales. **Load Layout** validates the entire file and all placements before replacing the active layout. **Reset Layout** clears placed furniture but keeps the saved JSON file. Successful load and reset clear undo/redo history.
- Grid snapping is enabled by default at 0.5 m and can be toggled or changed to 0.25 m or 1.0 m in the side panel. Snapping affects previews only; saved world positions are restored as written.
- The optional `Reference Seating: ON/OFF` control toggles fixed architectural reference chairs. The arrangement is separate from planner furniture and excluded from furniture counts and JSON.
- Undo/redo tracks committed placements, moves, rotations, deletions, and duplications (up to 100 entries); cancelling a preview does not create a history action.
- The side panel reports configured room/stage dimensions, placed furniture count, selected type, and selected world position. Existing validator feedback remains the source of bounds and collision reasons.
- The header includes a gold **LAYOUT TEMPLATES** button, **MY SAVED LAYOUTS**, and **SAVE AS MY LAYOUT**. If an older generated scene lacks those controls, `PlannerControlsPanel.Start` creates them under `Interface/Title and Camera Modes`, adds one `LayoutTemplateUI` to the existing `Interface` Canvas, and binds the buttons to the existing services. Templates are generated from the configured auditorium/stage frame and actual registered furniture dimensions; their exact counts appear in each card. Add and replace are all-or-nothing, and each successful template application is one undoable layout action. Reset and successful layout loads clear history as before.
- Named layouts are independent JSON files under `Application.persistentDataPath/VirtualAuditorium/Layouts/`; they do not overwrite the quick-save JSON or the bundled template definitions. The gallery can preview, apply, and edit every generated furniture instance through the existing placement system.

## Setup and run

See `README.md`. After Unity 6000.3 opens and imports the project, stop Play Mode and use `Tools > Virtual Auditorium > Build Demo Scene`, then open `Assets/Scenes/MainAuditorium.unity` and press Play. The scene generator backs up the target, rebuilds only architecture roots, preserves the cameras/UI/planner/layout, and verifies required room geometry before saving.

See `TESTING_CHECKLIST.md` for manual launch, camera, placement, collision, editing, persistence, and rebuild checks.

## Reference recreation limits

See `ReferenceImages/RealAuditorium/ARCHITECTURE_AUDIT.md`, `REFERENCE_ANALYSIS.md`, and `ASSET_SOURCES.md`. Photos 01–04 guide rear, left, stage, and right walls. Side walls share one bay plan so blinds, fans, and speakers never occupy the same Z volume. Floor uses a single medium-grey stone material (no charcoal underlay / no light-dark tile lottery). Extracted banner/logo textures preserve original university artwork; sharpness is source-limited. Decorative sizes remain editable estimates on `AuditoriumDemoSettings`. Room and stage frame stay immutable. Rear door is on −Z toward −X (right of the logo when facing the rear wall): dark walnut double leaves, square vision lites, silver pulls. Fans/blinds/door/ceiling are environment-only.

Textures live under `Assets/Textures/Auditorium/` (Wood, Stone, Fabric, Banners, Logo). URP render scale 1.0 with 4× MSAA; architecture textures use mipmaps, trilinear, and anisotropic filtering.
