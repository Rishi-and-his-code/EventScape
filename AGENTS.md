# Repository guide

## Project

Virtual Auditorium is a Unity 6.3 LTS desktop application for exploring an approximate 3D auditorium and planning event layouts. The project uses C#, URP, UGUI, and Unity built-in primitives. It must remain runnable without imported models, downloaded assets, external services, or custom shaders.

## Scope

- Keep the initial auditorium procedural and lightweight.
- Treat all current hall dimensions as approximate demo values, never as measurements of a real university venue.
- The current foundation includes the procedural auditorium, walkthrough and planning camera modes, a generated UGUI shell, procedural furniture, interactive placement/editing, and JSON layout persistence.
- Keep placement and furniture definitions in `Assets/Scripts/Placement`; the current MVP supports thirteen procedural furniture/event items registered through `FurnitureCatalog` and built by `FurnitureFactory`. Keep JSON layout storage in `Assets/Scripts/Persistence`.

## Architecture

- `Assets/Scripts/Core`: shared settings and small project-wide types.
- `Assets/Scripts/Environment`: `AuditoriumArchitectureBuilder` owns fixed generated room/stage detail, `AuditoriumBuilder` preserves and connects runtime planner roots, and `ReferenceSeatingController` toggles separate fixed reference chairs.
- `Assets/Scripts/Camera`: first-person collision-aware walkthrough, orbit/planning camera, and camera mode switching.
- `Assets/Scripts/Placement`: centralized furniture definitions/factory, floor markers, placement validation, and the raycast-driven placement system.
- `Assets/Scripts/UI`: generated UGUI presentation, camera mode controls, status feedback, and `PlannerUiResponsiveLayout` (CanvasScaler 1920×1080 / match 0.5, clamped side panels, scrollable catalog).
- `Assets/Scripts/Persistence`: serializable layout data and `LayoutPersistenceService`; JSON lives under `Application.persistentDataPath/VirtualAuditorium/layout.json`.
- `Assets/Editor`: Editor-only scene generation, asset setup, and Build Settings registration.
- `Assets/Scenes`, `Assets/Materials`, `Assets/Prefabs`, and `Assets/Settings`: generated scene, reusable materials, future reusable prefabs, and editable demo settings.

Keep environment building, camera navigation, placement validation, UI, and persistence as separate responsibilities. Editor-only code must stay under `Assets/Editor`; runtime scripts must not depend on `UnityEditor` or `AssetDatabase`.

## Conventions

- Use one public `MonoBehaviour` per C# file, with the filename matching its class. Non-component serializable types may share a file when they form one data model.
- Use PascalCase for types and public members, camelCase for private fields, and descriptive Unity hierarchy names.
- Prefer serialized references and shared materials; avoid repeated runtime material instances.
- Use metres approximately: 1 Unity unit = 1 metre.
- Use low-cost built-in meshes, a small number of realtime lights, and no unnecessary packages.
- Keep the demo scene generator repeatable. Re-running `Tools > Virtual Auditorium > Build Demo Scene` must rebuild the target scene instead of appending duplicate objects.

## Demo controls

- Planning View is the initial mode. **Enter Walkthrough**, `Tab`, or the mode button enters first-person; **Exit Walkthrough**, **Planning View**, or `Escape` returns to the retained planning pose. Walkthrough uses `W/A/S/D` at about 2 m/s, `Shift` for about 3 m/s, and hold right mouse to look. The CharacterController collides with architecture, stage, fixed seating and placed furniture; paired stage steps are collidable.
- Planning: hold right mouse and drag to orbit.
- Mouse wheel: zoom.
- `R`: reset the planning view.
- `Tab` or the on-screen buttons switches camera modes.
- Furniture catalog buttons start repeated placement; left click confirms, `Q/E` rotate by 15 degrees, and `Escape` cancels the current preview. Use the visible Finish Placing button to return to Select / Edit mode, then click placed furniture to select it. Grid snapping uses the auditorium world origin and supports 0.25 m, 0.5 m, and 1.0 m increments. The side panel can move, rotate, duplicate, or delete selected furniture.
- Event Lectern, Microphone Stand, and PA Floor Speaker can be placed on the marked stage top when they fit and clear the fixed podium. Other furniture remains excluded from the stage.
- Undo/redo records committed per-object layout changes only. Reset and successful load clear history. Version-1 JSON may include an optional stable instance ID; old records without it remain loadable.
- `Home` resets the active camera, `F1` selects top view, `F2` front view, and `F3` isometric view. The view buttons and existing Walkthrough/Planning controls remain available.
- **Save Layout** writes the current furniture layout to JSON; **Load Layout** replaces the current layout after validation; **Reset Layout** clears the current layout but leaves the saved JSON file untouched.
- The optional **Reference Seating: ON/OFF** control toggles fixed architectural chairs that are not planner furniture and are not saved to layout JSON.
- Camera navigation is suspended while placement is active; UI clicks never confirm scene placement.

The runtime and UGUI EventSystem use Unity's legacy Input Manager (`UnityEngine.Input` and `StandaloneInputModule`). Keep future input changes consistent across both camera scripts and the EventSystem module; do not add Input System-only controls without changing the project's active input handling and UI module together.

Keep `README.md` and `PROJECT_CONTEXT.md` aligned with meaningful architecture or control changes. The Editor scene command refreshes the generated interface; `PlannerControlsPanel` also repairs the template-library header controls on older generated scenes at runtime without creating another Canvas or EventSystem.
