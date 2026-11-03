# Virtual Auditorium

An interactive Unity desktop application for exploring an approximate auditorium and arranging event furniture before an event. Geometry and furniture are generated from built-in Unity primitives; no external models, downloads, services, or custom shaders are required.

## Requirements

- Unity Editor **6000.3 (Unity 6.3 LTS)**.
- macOS or Windows desktop target.
- URP 17.3 and UGUI 2.0, resolved from `Packages/manifest.json` when Unity opens the project.

The project uses Unity's legacy Input Manager API and UGUI `StandaloneInputModule`; the checked-in Player setting is Input Manager (Old). If Unity changes it during import, confirm **Edit > Project Settings > Player > Active Input Handling** is **Input Manager (Old)** or **Both**.

## Open and run

1. In Unity Hub, choose **Add** and select this project folder, then open it with Unity 6000.3.
2. Wait for package import and C# compilation to finish.
3. Choose **Tools > Virtual Auditorium > Build Demo Scene**. This creates/repairs the URP, materials, settings, and scene assets, backs up the current main scene under `Assets/Scenes/Backups/`, rebuilds generated architecture and its generated interface, saves `Assets/Scenes/MainAuditorium.unity`, and registers that scene in **File > Build Profiles > Scene List** (or Build Settings in older editor layouts). Stop Play Mode before running it. The command prompts to save dirty scenes first. Confirmed furniture, IDs, cameras, placement state, and persistence objects are retained. The generated `Interface` root is rebuilt to install current catalog controls.
4. Open `Assets/Scenes/MainAuditorium.unity` and press **Play**.

The template feature is also repaired at runtime for an older saved scene: `PlannerControlsPanel` adds the three template-library buttons to the existing `Interface/Title and Camera Modes` header and attaches `LayoutTemplateUI` to the existing `Interface` Canvas. It reuses the current `Persistence`, `PlacementSystem`, and EventSystem. The Editor rebuild serializes the current generated UI; the runtime check means opening an older `MainAuditorium.unity` does not require manually wiring components first. `PlannerUiResponsiveLayout` keeps the single Overlay Canvas at reference **1920×1080** with match **0.5**, clamps the right catalog and bottom-left help panels inside the screen, and repairs those anchors on older scenes at Play. Verify Game View at **1×** scale—editor magnification (for example 2.5×) can look like clipping even when the layout is correct.

Re-run the menu command to rebuild the architecture. It preserves `Cameras`, `PlacedObjects`, `PlacementSystem`, `Persistence`, `EventSystem`, and `Interface`, including confirmed furniture and its UI wiring. It replaces only the generated `Environment`, `Stage`, `Lighting`, `WallFixtures`, and `ReferenceSeating` roots. Each run keeps a timestamped scene backup in `Assets/Scenes/Backups/` and reuses the main scene's build-settings entry.

If Unity cannot create the URP asset automatically, use **Assets > Create > Rendering > URP Asset (with Universal Renderer)** in `Assets/Settings`, then run **Build Demo Scene** again.

## Controls

### First-person walkthrough

- The project opens in Planning View. Click **Enter Walkthrough** (or press `Tab`) to enter a first-person view near the rear entrance, facing the stage. Adjust `walkthroughSpawnX` and `walkthroughSpawnDepthFraction` in `AuditoriumDemoSettings` to tune its spawn.
- `W/A/S/D` move at 2 m/s; hold `Shift` for 3 m/s. The upright 1.75 m `CharacterController` has a 0.28 m radius and collides with the room, stage and placed furniture.
- Hold the **right mouse button** and drag to look around. Rotation is clamped vertically; look input requires application focus and pauses over UI or text input.
- Click **Exit Walkthrough**, click **Planning View**, or press `Escape` to return to the preserved Planning camera pose. Furniture editing UI and selection input are disabled while walking. The catalog and status panels return in Planning View.
- Two collidable 0.15 m rises at the stage front make the 0.45 m platform walkable with the CharacterController step offset.

### Planning view

- Hold the **right mouse button** and drag to orbit around the planning area.
- Use the **mouse wheel** to zoom; the distance is clamped.
- Press `R` or **Home** to reset the planning camera (Home resets the current camera in either mode).
- Press `F1`, `F2`, or `F3` for Top, Front, or Isometric planning views. The matching side-panel buttons also work. These shortcuts pause while the pointer is over UI or a text field has focus.

Use `Tab` or the **Enter Walkthrough** / **Exit Walkthrough** and **Planning View** buttons to switch cameras. Entering Walkthrough cancels any live placement or move preview without changing confirmed furniture. The Planning camera retains its orbit pose. During placement or moving, camera navigation pauses; the on-screen buttons can still change camera mode.

## Furniture and layouts

- Select any catalog item. The catalog contains **Furniture** (Bench, Auditorium Chair, Classroom Chair, Classroom Desk, Round Table, Square Table, Lounge Chair, Registration Table), **Stage & AV** (Event Lectern, Microphone Stand, PA Floor Speaker), and **Decorative** (Flower Pot, Roll-up Banner Stand). New approximate dimensions in metres are: Auditorium Chair 0.48 × 0.55 × 0.82; Lounge Chair 0.80 × 0.80 × 0.90; Event Lectern 0.55 × 0.50 × 1.10; Microphone Stand 0.45 × 0.45 × 1.60; PA Floor Speaker 0.35 × 0.35 × 0.70; Roll-up Banner Stand 0.85 × 0.35 × 2.00; Registration Table 1.20 × 0.60 × 0.75 (width × depth × height). These are catalog defaults, not measured venue equipment.
- Move its translucent preview over valid auditorium floor; green is valid and red explains an invalid location. Left click places, `Q/E` rotate by 15 degrees, and `Escape` cancels the current preview. Event Lectern, Microphone Stand, and PA Floor Speaker may also be placed on the raised stage top if their rotated footprint fits and they do not overlap the fixed podium; other furniture remains excluded from the stage.
- Catalog placement remains active after each successful placement so multiple copies can be added. Use the visible **Finish Placing** button below the catalog entries to return to **Select / Edit** mode. In Select / Edit mode, click furniture to select it; clicking empty floor clears the selection. The same control offers **Cancel Move** or **Cancel Duplicate** during those previews.
- **Grid Snap** is enabled at 0.5 m by default. Toggle it off or choose 0.25 m, 0.5 m, or 1.0 m. Snapping is applied to placement and move previews relative to the centered room origin; loaded saved positions are not snapped.
- Click placed furniture to select it. Use **Move Selected** for a validated preview; click to confirm or press `Escape` to restore its old position. **Rotate Left/Right** rotate by 15 degrees, **Duplicate Selected** starts a preview with the same type/scale/rotation and a configurable 2 m local-right offset, and **Delete Selected** removes it.
- **Undo** and **Redo** are available for committed placement, move, rotation, duplication, and deletion. Cancelling a preview does not add a history entry. The in-memory history keeps the latest 100 actions; a successful load or layout reset clears it.
- The side panel's **Room & Layout** section displays room/stage dimensions from the current demo settings, item count, and the selected object's type and world position. Invalid previews continue to show the validator's bounds/collision reason in the status area.
- The gold **LAYOUT TEMPLATES** header button opens three generated presets: Theatre / Presentation, Classroom / Workshop, and Conference / Roundtable. Their top-down previews and item counts use the same template placement records as application. The lightweight UGUI schematics preserve the room's 16:25 proportions, show the stage/aisles and rotated furniture footprints, and display a diagnostic if preview data cannot render. Template spacing is configurable in `AuditoriumDemoSettings`; positions are relative to the auditorium and stage direction.
- Applying a template offers **Add to Current Layout** or **Replace Current Layout**. Add validates every proposed item against the existing layout and the rest of the template; replacement asks for confirmation and validates/stages the complete replacement before removing the old furniture. Either successful operation is one whole-layout Undo action. Failed applications leave the active layout intact.
- **Save As My Layout** creates an independent named layout. **My Saved Layouts** shows each stored name as the row heading, followed by its furniture count, available date and description; Load, Rename, and Delete remain associated with that row's filename. Legacy files with no name metadata use a readable filename-derived label. Overwrite and delete actions require confirmation. These JSON files live under `Path.Combine(Application.persistentDataPath, "VirtualAuditorium", "Layouts")`; the original quick-save file remains at `.../VirtualAuditorium/layout.json`. Built-in templates are bundled definitions and cannot be overwritten by user saves.
- **Save Layout** cancels any live preview (restoring a move if needed) and writes the confirmed objects to JSON. **Load Layout** validates the file and all placements before replacing the current arrangement. If loading fails, the current arrangement is preserved. **Reset Layout** clears only the current arrangement; it leaves the saved JSON file unchanged.
- The layout file is stored at `Path.Combine(Application.persistentDataPath, "VirtualAuditorium", "layout.json")`. Save/load status appears in the interface; the full resolved path is also logged in Unity's Console after success. Version-1 records include a stable furniture type ID, per-instance ID, world position, upright rotation, and local scale. The per-instance ID is optional when loading older version-1 files; missing IDs are generated.

## Dimensions and limitations

`Assets/Settings/AuditoriumDemoSettings.asset` provides configurable room and stage dimensions. The current 16 m × 25 m × 3.65 m interior values were requested for this project build, but have not been verified against the university's actual hall. Replace them with measured venue dimensions before relying on a layout for a real event. Furniture placement currently supports upright rotation around the vertical axis; custom scaling is preserved in saved layouts, but there are no scale-editing controls. Save files are local to the machine and are not synchronized between users.

The procedural room has a 16 m clear width (X), 25 m clear length (Z), and 3.65 m floor-to-ceiling height, with 0.15 m walls. The raised stage is 9.5 m wide by 3.7 m deep; its top is exactly 0.45 m above the floor and it is centered at the positive-Z end. These are the configured project-demo dimensions supplied for this build, not independently verified measurements of a real venue. Replace them with measured dimensions before using the planner for a real event. Furniture is low-poly and collider-backed; chairs, lecterns, AV stands, speaker and banner are recognizable component-based approximations. New types retain independent identity through selection, movement, rotation, duplication, undo/redo and JSON persistence.

The ceiling and its integrated light fittings are real geometry. They are shown in Walkthrough mode and hidden together in Planning View so the room and furniture can be inspected from above. The generated scene opens with the ceiling inactive in Edit mode and the Scene view framed in an elevated three-quarter view; entering Play in Walkthrough mode enables it. Use the mode buttons or `Tab` to switch the runtime ceiling view.

## Navrachana architectural recreation

The four supplied images under `ReferenceImages/RealAuditorium/` (analysis in `REFERENCE_ANALYSIS.md`) guide the architectural treatment; they are visual references rather than measured drawings. Photo 01 maps to the rear wall at negative Z, photo 03 maps to the stage wall at positive Z, and photos 02/04 map independently to the left/right long walls. The clear room (**16 × 25 × 3.65 m**) and stage (**9.5 × 3.7 m**, top **Y = 0.45 m**) remain unchanged.

The procedural architecture now builds:

- Individual walnut rear feature panels with seams, honey vertical cladding, and a replaceable rear logo board.
- Independent left/right side-wall bay systems (cladding, cream plaster, pilasters, roller blinds with rollers/rails, circular wall fans with guard rings/blades, speakers, sparse plates).
- A real rear doorway opening on the −X side of the −Z wall (right of the Navrachana logo when facing the rear wall), with dark walnut double leaves, square vision lites, long silver pulls, and a light timber surround (leaves have no colliders so walkthrough stays clear).
- Stage timber planks, approximated curved front fascia, audience-right steps, audience-left fixed podium, walnut backdrop, two square Navrachana posters on the screen flanks (photo 03 crops), and a matte projection screen.
- Uniform charcoal-grey stone floor tiles (shared material; no random tile tinting).
- Acoustic ceiling panels, dark grid, curved grey canopy with perforation cues, cylindrical duct, and warm downlight fittings.
- Warm ~3000 K lighting via a directional key, stage/rear/side spots, and a few point fills (emissive fittings are visual only).

Geometry is batched by material. Decorative parts have no colliders; structural perimeter walls, floor, stage, and steps remain collidable. Optional **Reference Seating: ON/OFF** remains separate from planner furniture and layout JSON.

Artwork slots (`rearLogoArtwork`, `stageBannerLeftArtwork`, `stageBannerRightArtwork`, optional shared `stageBannerArtwork`) are wired through `AuditoriumDemoSettings` / `AuditoriumMaterialLibrary`. Logo and banner glyphs come from the supplied photographs only—nothing is invented. Source glare and softness remain; replace slots with approved clean artwork when available. See `ASSET_SOURCES.md`.

Configurable visual estimates (cladding height, tile size/grout, bay spacing, blind/fan/banner sizes, Kelvin and intensities, door inset) live on `AuditoriumDemoSettings`. URP stays at render scale 1.0 with 4× MSAA; architecture textures use mipmaps, trilinear filtering, and anisotropic filtering. No new packages or paid assets are required.

After changing architecture code or textures, stop Play Mode and re-run **Tools > Virtual Auditorium > Build Demo Scene**, then use `TESTING_CHECKLIST_ARCHITECTURE.md` for visual verification. See `ReferenceImages/RealAuditorium/ARCHITECTURE_AUDIT.md` for the current photo mapping, known visual failure causes, and fixture layout rules (blinds, fans, and speakers share one non-overlapping bay plan).

## Project structure

- `Assets/Scenes`: generated main auditorium scene.
- `Assets/Scripts/Core`, `Environment`, `Camera`, `Placement`, `UI`, and `Persistence`: runtime settings, procedural geometry, camera control, furniture editing/validation, UI, and JSON persistence.
- `Assets/Editor`: scene, material, URP, and Build Settings setup.
- `Assets/Materials`, `Assets/Settings`: generated shared materials and editable demo settings.
- `Assets/Prefabs`: reserved for future reusable prefab assets; current furniture is procedurally generated by `FurnitureFactory`.

See [TESTING_CHECKLIST.md](TESTING_CHECKLIST.md) for the manual verification procedure, including the template gallery and custom layout library.
