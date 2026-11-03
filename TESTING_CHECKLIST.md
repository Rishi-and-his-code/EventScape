# Virtual Auditorium manual testing checklist

Run these checks in Unity Editor **6000.3 (Unity 6.3 LTS)** after package import and script compilation. Record any Console errors before considering the project ready for a demo.

## Setup and scene generation

- [ ] Confirm **Active Input Handling** is **Input Manager (Old)** or **Both**.
- [ ] Run **Tools > Virtual Auditorium > Build Demo Scene**. Confirm `Assets/Scenes/MainAuditorium.unity` is created and enabled in the Build Profiles Scene List / Build Settings. Confirm the generated interface updates while furniture, IDs, cameras, and persistence roots remain.
- [ ] Confirm the hierarchy contains `Auditorium_Floor`, all four named perimeter walls, `Auditorium_Ceiling`, and `Auditorium_Stage`, plus `Lighting`, `Cameras`, `PlacedObjects`, `PlacementSystem`, `Persistence`, and `Interface`.
- [ ] Confirm architecture roots include `RearFeatureWall`, `StageBackdrop`, `Banners`, `Podium`, `LeftSideWall`, `RightSideWall`, `WallFixtures`, and `ReferenceSeating`; all additions remain within the 16 × 25 × 3.65 m interior except the existing wall thickness.
- [ ] Confirm clear interior bounds X=-8..+8 m, Z=-12.5..+12.5 m, floor Y=0, ceiling underside Y=3.65 m, and 0.15 m walls. Confirm the stage is 9.5 m × 3.7 m with top Y=0.45 m.
- [ ] Run the menu command a second time. Confirm a timestamped backup appears in `Assets/Scenes/Backups/`, and that one set of architecture roots exists while cameras, furniture, placement, persistence, EventSystem, and UI remain intact.
- [ ] With furniture already placed, rebuild once more and confirm all placed objects, stable IDs, camera mode setup, layout controls, EventSystem, and UI survive.
- [ ] Inspect the Scene view framed from the entrance toward the stage.
- [ ] Confirm the scene starts in Planning View and the title bar offers **Enter Walkthrough**.
- [ ] Compare all four wall directions against the supplied photos: rear walnut identity feature at negative Z, stage screen/walnut/banners/podium at positive Z, and independently treated side walls with right-side blinds.
- [ ] Inspect stage timber plank seams and fascia; verify stage collider footprint remains exactly 9.5 × 3.7 m and platform top remains Y=0.45 m.
- [ ] Inspect ceiling acoustic tiles, dark grid, curved fabric canopy, HVAC and fixtures in Walkthrough. Switch to Planning View and confirm the complete ceiling assembly hides; switch back and confirm it returns.
- [ ] Toggle **Reference Seating: ON/OFF**. Confirm fixed chairs are grouped under `ReferenceSeating`, do not affect furniture counts or JSON save/load, and block overlapping furniture placement only while visible.
- [ ] Confirm the temporary rear logo photo crop is legible but visibly photographic; verify a replacement can be assigned through the `rearLogoArtwork` slot and banner artwork through `stageBannerArtwork`.
- [ ] Open `Assets/Scenes/MainAuditorium.unity` and press **Play**. Confirm there are no missing references or compile/runtime errors.

## Cameras and interface

- [ ] Click **Enter Walkthrough**. Confirm the spawn is inside the rear entrance facing the stage, the button changes to **Exit Walkthrough**, and the small controls overlay appears while catalog and editing/status panels hide.
- [ ] Change `walkthroughSpawnX` and `walkthroughSpawnDepthFraction` in `AuditoriumDemoSettings`, rebuild the scene, and confirm the character spawns within the room bounds facing positive Z.
- [ ] Check `W/A/S/D` movement at about 2 m/s and `Shift` at about 3 m/s. Hold right mouse and drag to look; confirm vertical rotation is clamped and pointer lock releases when the button is released or the app loses focus.
- [ ] Walk into each wall, placed furniture, and visible reference seating. Confirm the CharacterController collides with solids and ghost previews do not block it.
- [ ] Walk over the two collidable front steps onto the 0.45 m stage. Confirm stage, screen, fixed podium, and steps do not intersect.
- [ ] Exit with **Exit Walkthrough**, **Planning View**, and `Escape` in separate attempts. Confirm the previous Planning camera pose and furniture layout return. Confirm clicking placed objects while in walkthrough never selects them.
- [ ] Confirm camera WASD input pauses over UI or focused text inputs. Verify mouse-look only works while the application is focused and right mouse is held.
- [ ] In Walkthrough, use `W/A/S/D` and right mouse drag. Confirm movement stops at walls and floor level remains stable.
- [ ] Confirm the ceiling is visible in Walkthrough and hidden in Planning View; switching back restores it. The ceiling lights, walls, floor, stage, and furniture remain visible in both modes.
- [ ] Switch to Planning View using the button and `Tab` when available. Drag with right mouse and use the wheel; confirm zoom remains bounded and the room/stage remain visible.
- [ ] Switch modes several times before editing and while a placement/move preview is active. Confirm the active camera changes and controls resume after the operation ends.
- [ ] Move and click over catalog, action, mode, and save/load buttons. Confirm UI clicks do not place/select furniture in the scene.
- [ ] Check the catalog, selection actions, and status/help areas at a common desktop resolution and a smaller Game view. Confirm text remains readable and controls do not cover the main auditorium view excessively.

## Responsive UI layout (Game View scale vs clipping)

**Important:** Unity Game View **Scale** (e.g. 2.5×) is editor magnification of the rendered framebuffer. It crops the left/right/top/bottom of the Game view and makes panels look clipped even when Canvas layout is correct. Your screenshot showing mid-word button cutoffs with Scale **2.5×** is this crop — not missing UI. Always verify with **Scale = 1×**. Entering Play Mode or choosing **Tools > Virtual Auditorium > Reset Game View Scale To 1x** forces Scale back to 1×.

- [ ] Inspect `Interface` Canvas: **Screen Space - Overlay**, one Canvas only, one EventSystem. `CanvasScaler` is **Scale With Screen Size**, reference **1920 × 1080**, **Match Width Or Height = 0.5**.
- [ ] Set Game View to **1920 × 1080** at **1×** scale. Confirm the full right catalog panel, header controls (title, Walkthrough / Planning, Layout Templates / My Saved Layouts / Save As), and bottom-left status/help panel are entirely inside the window with readable margins.
- [ ] Set Game View to **1366 × 768** and **1280 × 720** at **1×**. Confirm the right panel stays inside the right edge (width may shrink toward ~300 logical px) and the left help panel stays inside the bottom-left corner without horizontal overflow.
- [ ] Also spot-check **1600 × 900** and **1440 × 900**. Confirm header buttons do not overlap the title and no control straddles the window edge.
- [ ] On the right panel: scroll with the mouse wheel and the vertical scrollbar. Confirm every catalog category/button, Select/Edit / Finish Placing, selected-object actions, grid snap, Duplicate, Undo/Redo, camera view buttons, and room info are reachable. Confirm horizontal scrolling is not required.
- [ ] Confirm help text wraps inside the left panel and Save / Load / Reset remain fully clickable.
- [ ] Confirm catalog/status panels do not cover the centre auditorium more than a normal sidebar; the 3D view remains the primary focus.
- [ ] Temporarily set Game View Scale to **2.5×** then back to **1×**. Confirm any “clipping” at 2.5× is magnification of a letterboxed/cropped Game view preview, while 1× at the same resolution remains fully usable.
- [ ] Open Layout Templates and My Saved Layouts at 1366 × 768. Confirm dialogs fit inside the Canvas; Close and primary actions remain reachable.

## Furniture placement and validation

- [ ] Place each catalog type on open floor and confirm the preview follows the cursor and becomes green.
- [ ] Rotate a preview with `Q` and `E`; confirm visible 15-degree steps.
- [ ] Try placing a table through a side wall, at the back wall, and on/through the stage. Confirm the preview is red, explains the issue, and cannot be confirmed.
- [ ] Place a table, then try placing another item at the same location. Confirm overlap is rejected, including after rotating the preview.
- [ ] Place furniture near the perimeter and stage. Confirm fixed colliders and configured interior bounds prevent penetration while the floor marker collider does not block placement.
- [ ] Press `Escape` during new placement. Confirm the preview is removed and no extra item is created.
- [ ] Hold the left mouse button over a valid preview after one click. Confirm only one object is created per click.

## Select/edit and catalog mode handoff

- [ ] Select Bench in the catalog and place three benches. Confirm placement remains active after each success and each click adds only one item.
- [ ] Click **Finish Placing**. Confirm the preview disappears, the control returns to **Select / Edit Mode**, and all placed benches remain.
- [ ] Click an existing bench and confirm it becomes selected; move, rotate, duplicate, and delete it using the existing actions.
- [ ] Repeat placement, finish, and selection for Round Table, Square Table, Flower Pot, Classroom Desk, and Classroom Chair. Confirm the original six types are selectable/editable after finishing placement.
- [ ] While placing, click catalog or other UI controls and confirm the click does not place in the world. In Select / Edit mode, click empty floor and confirm selection clears.
- [ ] Press Escape during a live placement preview and confirm it cancels only that preview. Begin moving an existing object, then cancel with Escape or **Cancel Move** and confirm the original transform is restored.

## Classroom desk and chair

- [ ] Inspect Classroom Desk from front and side: grey rounded-edge top, blue perforated modesty panel, blue tube frame, and neutral feet are distinct and visible.
- [ ] Inspect Chair from front and side: moulded blue curved seat/back, slightly rear-tilted backrest, and four slender silver legs are visible.
- [ ] Verify approximate dimensions: desk 1.20 × 0.55 m footprint and 0.75 m high; chair 0.46 × 0.50 m footprint and 0.82 m high, seat near 0.45 m.
- [ ] Rotate each new item; confirm bounds and overlap validation still rejects invalid placements. Check all grid increments and disabled snapping for both.
- [ ] Duplicate each new item and confirm the original remains and the duplicate has a distinct ID. Verify undo/redo, save/load, and reset handle both types.
- [ ] Load a pre-change version-1 JSON layout containing the original four stable furniture IDs and confirm it still loads.

## Selection and editing

- [ ] Click placed furniture outside the UI. Confirm it receives a teal wireframe and its type appears in the selected-object panel without changing its material.
- [ ] Use **Rotate Left** and **Rotate Right**. Confirm 15-degree rotations are applied only when the result is valid.
- [ ] Use **Move Selected**, move to open floor, and confirm. Confirm the same object is moved and no duplicate is created.
- [ ] Start another move, hover at an invalid wall/overlap position, and press `Escape`. Confirm the original position and appearance are restored.
- [ ] Attempt to confirm an invalid move. Confirm it is rejected.
- [ ] Use **Delete Selected**. Confirm the object and outline disappear and the actions become disabled.
- [ ] Select a new catalog item while an edit operation is active. Confirm the prior move is restored or placement operation is cancelled cleanly before the new placement starts.

## Save, load, and reset

- [ ] Place at least two different furniture types and rotate one. Save. Confirm a success message and a JSON file at `Path.Combine(Application.persistentDataPath, "VirtualAuditorium", "layout.json")` (the resolved path is logged in the Unity Console).
- [ ] Start a move preview and then save. Confirm the move is cancelled/restored and the confirmed object is included in the saved layout.
- [ ] Move/delete objects or reset the current arrangement, then choose **Load Layout**. Confirm all saved furniture types, positions, rotations, and scales are restored.
- [ ] Try **Load Layout** before saving. Confirm a useful message appears and the current scene remains unchanged.
- [ ] Back up the JSON file, then test malformed JSON, an unknown furniture ID, an unsupported version, and overlapping/out-of-bounds records. Confirm the load is rejected and the current arrangement remains intact; restore the backup afterward.
- [ ] Choose **Reset Layout**. Confirm all placed furniture is cleared, the selection is cleared, and the saved JSON file remains available for a subsequent load.
- [ ] Save an empty layout and load it. Confirm the current furniture arrangement becomes empty.

## Grid snapping, duplication, and history

- [ ] Start placement with Grid Snap enabled at **0.25 m**. Move the preview across the floor and confirm its X/Z coordinates settle on quarter-metre steps; rotate with `Q/E` and confirm rotated bounds/collision validation still applies.
- [ ] Repeat placement and move previews at **0.5 m** and **1.0 m**. Confirm the selected increment has the active visual state and all previews use that step.
- [ ] Turn Grid Snap off. Confirm new placement and move previews follow the floor hit continuously without snapping. Load a saved layout containing off-grid coordinates and confirm the restored objects stay at those exact positions.
- [ ] For each of the thirteen types (Bench, Auditorium Chair, Classroom Chair, Classroom Desk, Round Table, Square Table, Lounge Chair, Registration Table, Event Lectern, Microphone Stand, PA Floor Speaker, Flower Pot, Roll-up Banner Stand): place/select one, duplicate it, confirm the original stays unchanged, rotate/move/delete it, and verify undo/redo.
- [ ] Save a layout containing all thirteen types, load it after deleting/moving objects, and confirm type, unique IDs, transforms and undo/redo work. Confirm old version-1 layouts with the original stable IDs still load.
- [ ] Place Event Lectern, Microphone Stand, and PA Floor Speaker on the stage top. Confirm their feet rest at Y=0.45 m, footprints stay inside the platform, and the fixed podium rejects overlap. Try a chair on the stage and confirm it remains invalid. Save/load and duplicate stage equipment; confirm stage elevation and IDs persist.
- [ ] Place an item, then undo and redo. Move an item, then undo and redo. Rotate an item, then undo and redo. Delete an item, then undo and redo. Duplicate an item, then undo and redo. Confirm type, stable instance ID, position, rotation, and scale survive each round trip.
- [ ] Perform several different actions, then undo and redo repeatedly through the sequence. Confirm counts and IDs remain unique, and each object appears at most once. Confirm Undo/Redo buttons disable when their respective history is empty.
- [ ] Begin and cancel a new placement, move, and duplicate preview. Confirm none creates an undo entry; cancelling a move keeps the original object and transform.
- [ ] Save a layout, make several committed edits, then load it. Confirm saved transforms and IDs return, undo/redo history is cleared, and the next new action can be undone without restoring pre-load objects. Repeat with a legacy version-1 JSON file that omits `instanceId` and confirm it loads with generated unique IDs.

## Camera shortcuts and measurement feedback

- [ ] Use **Reset Camera**, **Top View**, **Front View**, and **Isometric View** buttons from both camera modes. Confirm view presets enter Planning View, Reset Camera returns the active camera to its initial pose, and Walkthrough/Planning switching and ceiling visibility still work.
- [ ] With the pointer over a UI control or a UGUI text input focused, press `Home`, `F1`, `F2`, `F3`, and `Tab`. Confirm camera shortcuts do not fire while interacting with UI or typing. Away from UI, confirm Home resets and F1/F2/F3 select their views.
- [ ] Verify the information panel shows room **25.0 × 16.0 × 3.65 m**, stage **9.5 × 3.7 m** with **0.45 m** top elevation, the current confirmed furniture count, and selected type/world coordinates. Confirm count changes correctly after placement, move, delete, duplicate, undo/redo, reset, and load.
- [ ] Try a preview outside usable room bounds, against a wall, on the stage, and overlapping another item. Confirm the preview turns red, the status message distinguishes bounds from collisions, and invalid placements cannot be confirmed.

## Event templates and custom layout library

- [ ] Before rebuilding, open the current checked-in `Assets/Scenes/MainAuditorium.unity` and press Play. Confirm the gold **LAYOUT TEMPLATES**, **MY SAVED LAYOUTS**, and **SAVE AS MY LAYOUT** controls appear in the existing header without a second Canvas/EventSystem. Stop Play Mode, rebuild, reopen the scene, and confirm the serialized generated UI still exposes the same controls.
- [ ] Click **LAYOUT TEMPLATES** during normal planning. Confirm the visible dialog is titled **CHOOSE EVENT LAYOUT**, appears above the existing UI, states Select / Edit mode, and provides a working Close button. Open it during a live furniture placement or move preview and confirm that preview is safely cancelled before the gallery opens.
- [ ] In Planning View, open **Layout Templates**. Confirm exactly three cards appear: Theatre / Presentation, Classroom / Workshop, and Conference / Roundtable.
- [ ] Inspect each generated top-down preview. Confirm it shows the stage at the configured stage location, room outline, aisle and furniture footprints. Compare the displayed counts with the corresponding visible placed items after application.
- [ ] Apply Theatre / Presentation using **Replace Current Layout** with furniture already placed. Confirm the replacement confirmation explains the effect, all chairs face the actual stage, two seating blocks and central/side aisles remain, and every item is individually selectable/editable.
- [ ] Undo the complete template replacement once and confirm the exact previous layout returns; redo and confirm the template returns with unique IDs. Repeat with Classroom / Workshop and Conference / Roundtable.
- [ ] Apply each template using **Add to Current Layout**. Confirm every proposed item is checked against existing furniture and items within the template, and either the full template is added or none of it is. Verify overlap/bounds failures preserve the original arrangement and report a useful reason.
- [ ] Change template aisle, row, and round-table settings in `AuditoriumDemoSettings`, rebuild, and confirm previews/counts and actual arrangements reflect the configured settings without changing the room/stage dimensions.
- [ ] Confirm optional furniture in templates is present in the real registered `FurnitureCatalog`; no fixed stage podium is duplicated. Check custom furniture IDs are unique after applying the same template repeatedly.
- [ ] Save a customized template with **Save As My Layout**. Enter a name and optional description. Confirm the file appears under `Application.persistentDataPath/VirtualAuditorium/Layouts/` and the ordinary quick-save JSON remains separate.
- [ ] Try saving another layout with the same name. Confirm overwrite requires a separate confirmation and cancel leaves the existing file intact. Try an empty or invalid name and confirm a helpful message appears.
- [ ] In **My Layouts**, confirm each saved row shows its name, description, and item count. Load a saved layout after editing the active arrangement; confirm the selected saved layout replaces the active layout and successful load clears stale undo/redo history.
- [ ] Rename a saved layout. Test a rename that collides with another name and confirm overwrite is requested; cancel and verify both existing files remain unchanged. Delete a saved layout and confirm deletion requires a separate confirmation and affects only that named file.
- [ ] Save two independent arrangements as `Navrachana Annual Function` and `Technical Workshop`. In **My Saved Layouts**, confirm each exact name is the prominent row heading, with the correct furniture count, description (when provided), and modified date below it.
- [ ] Rename the first entry and confirm the updated heading appears immediately after the library refresh. Close/reopen the library and restart Play Mode; confirm both names still match their own row actions.
- [ ] Load both named layouts in turn and confirm each restores its own furniture arrangement. Confirm Load, Rename, and Delete act on the row's stored filename even after list order changes.
- [ ] Check a long saved name and long description. Confirm text wraps or truncates within the left metadata column and never overlaps the right-side buttons.
- [ ] Test a legacy named-layout JSON with a valid furniture list but no `layoutName`. Confirm it remains listed with a readable filename-derived name. Test a GUID-only filename and confirm the UI shows a neutral fallback, not the raw GUID or an empty heading.
- [ ] Corrupt or remove one custom layout file outside the app. Confirm malformed entries are skipped in the library and failed loads preserve the current furniture arrangement. Restore test files afterward.
- [ ] Switch between Planning View and Walkthrough while the gallery/dialog is closed. Confirm template and custom-layout controls use the existing Canvas/EventSystem and do not create duplicate EventSystems or input modules.

### Schematic preview rendering

- [ ] Open **Layout Templates** and confirm all three cards show a room outline, light floor, stage, circulation shading, and distinct furniture footprints rather than an empty/dark preview area.
- [ ] Confirm Theatre has two blue chair blocks with a central and side aisle; Classroom has gold desks with matching blue chairs plus its rear registration table; Conference has four gold round tables with surrounding chairs and green decorations.
- [ ] Compare the three cards and verify their diagrams differ. The counts and furniture marks must come from each card's displayed template, not shared placeholder art.
- [ ] Click Preview for each template. Confirm the large schematic updates to that template, shows a stage label and legend, and keeps the room's width-to-length proportions.
- [ ] Confirm chair/desk/table orientations track the template placement rotations, the stage is at the configured end, and all positions fit inside the full room boundary.
- [ ] Change a template row/count/aisle setting, close and reopen the gallery, then verify the refreshed preview and item count reflect the new settings.
- [ ] Confirm Preview does not change the placed-furniture count or create undo/redo history. Apply the same template and compare its real furniture positions/count with the preview; undo and redo the application.
- [ ] If preview data or dimensions are invalid, confirm the preview area shows a readable diagnostic and the Unity Console identifies the template ID.
- [ ] Repeat at 1920×1080, 1600×900, and 1366×768 Game view sizes. Check that the complete preview and footer controls remain visible and schematic room proportions do not change.

## Build handoff

- [ ] Confirm the main scene remains enabled in the Build Profiles Scene List / Build Settings after rebuilding.
- [ ] Make a desktop build and run it outside the Editor. Repeat a short camera, placement, save, reset, and load cycle.
- [ ] Check `Application.persistentDataPath` on the target machine if the user needs to back up or transfer the layout file; it is not stored in the project folder or synchronized automatically.

## Architecture recreation / collision checks

Use the detailed visual pass in [TESTING_CHECKLIST_ARCHITECTURE.md](TESTING_CHECKLIST_ARCHITECTURE.md) after every architecture rebuild.

- [ ] Confirm decorative cladding, pilasters, blinds, fans, speakers, screen, podium, acoustic panels, trusses, door leaves, and light fittings have no colliders and do not create false static-collision errors.
- [ ] Confirm the rear wall has a centred dark-walnut panel grid with the Navrachana logo, honey vertical cladding flanks, and a −X-side dark double door (square windows, silver pulls) under `Rear_Entrance_Door`; walkthrough can pass the opening.
- [ ] Confirm both stage banners are large boards with photo-extracted artwork (not thin red strips) and the floor uses a uniform grey stone material.
- [ ] Place furniture near each perimeter wall and along wall fixtures; confirm the bounds validator still prevents wall penetration without rejecting decorative geometry.
- [ ] Place furniture with reference seating hidden and visible. Confirm the static seating collider prevents overlaps while visible and removes that obstacle when hidden.
- [ ] Confirm the floor still has one placement-surface marker and placement rays hit it through the tile mesh; tile surfaces do not create extra placement colliders.
- [ ] Confirm generated logo/banner artwork comes from the supplied photographs only; replaceable slots remain on `AuditoriumDemoSettings`.

## Catalog dimensions and image quality

- [ ] Inspect each new procedural item from multiple angles: Auditorium Chair 0.48 × 0.55 × 0.82 m; Lounge Chair 0.80 × 0.80 × 0.90 m; Event Lectern 0.55 × 0.50 × 1.10 m; Microphone Stand 0.45 × 0.45 × 1.60 m; PA Floor Speaker 0.35 × 0.35 × 0.70 m; Roll-up Banner Stand 0.85 × 0.35 × 2.00 m; Registration Table 1.20 × 0.60 × 0.75 m. Confirm component-based geometry and sensible footprints.
- [ ] In `Assets/Settings/VirtualAuditoriumURP.asset`, confirm MSAA is 4× and render scale is 1.0. Confirm the generated Planning and Walkthrough cameras allow MSAA after rebuilding the scene.
- [ ] Confirm `VA_WoodGrain` is 512 × 256 with mipmaps and trilinear/anisotropic filtering after running the generator. Inspect wood at close range and at grazing angles. Check Game View at 1920 × 1080 and 1× display scale; geometry antialiasing does not repair low-resolution source artwork.
- [ ] Confirm no dynamic-resolution reduction is enabled and UGUI text remains sharp at the target Game View resolution.
