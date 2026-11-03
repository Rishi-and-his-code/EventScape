# Navrachana University auditorium — reference photograph analysis

Primary source photographs live in this folder (copied from `ReferenceImages/*.png`). Filenames requested as `.jpg` in the brief map to the supplied PNG masters:

| Brief name | Actual file used |
| --- | --- |
| `01-rear-feature-wall.jpg` | `01-rear-feature-wall.png` |
| `02-long-side-wall-left.jpg` | `02-long-side-wall-left.png` |
| `03-stage-front.jpg` | `03-stage-front.png` |
| `04-long-side-wall-right.jpg` | `04-long-side-wall-right.png` |

These images are visual references, not measured drawings. Decorative dimensions in the Unity model are configurable estimates unless noted as the immutable room/stage frame.

## Unity coordinate mapping

| Axis | Meaning in this project |
| --- | --- |
| +X | Audience right when facing the stage |
| −X | Audience left when facing the stage |
| +Z | Stage end |
| −Z | Rear / entrance end |
| Y = 0 | Floor |
| Y = 3.65 | Ceiling underside (configured) |

Immutable clear interior: **16 m (X) × 25 m (Z) × 3.65 m (Y)**. Stage: **9.5 × 3.7 m**, top **Y = 0.45**, centred on X at the +Z end.

Photograph → wall mapping:

| Photo | Physical wall | Unity location |
| --- | --- | --- |
| 01 | Rear identity / feature wall | Interior face of wall at **−Z** |
| 02 | Long side wall (audience-left when facing stage) | Interior face at **−X** |
| 03 | Stage-facing backdrop | Interior face at **+Z**, plus stage platform |
| 04 | Opposite long side wall | Interior face at **+X** |

## Photograph 01 — Rear feature wall

**Visible architecture**

- Large central dark-walnut feature panel grid (approx. 3 columns × 4 rows of panels with thin light seams).
- White raised Navrachana leaf emblem + Devanagari identity centred on the walnut.
- Honey vertical timber cladding flanking the walnut feature.
- Warm off-white upper plaster above the wood.
- Grey stone-tiled floor with thin grout.
- Ceiling bulkhead / ducting and warm spotlights aimed at the logo.
- Doorway sliver on the **far right** of the frame (blue-toned reveal).

**3D geometry required**

- Individual walnut panels with seams/bevels (not one brown slab).
- Honey vertical slats beside the feature.
- Upper plaster plane.
- Physical doorway opening on the rear wall toward **−X** (far right of this photo / right of the logo when facing the rear wall).
- Ceiling bulkhead/duct cues (shared with ceiling system).

**Best as textures / materials**

- Navrachana logo artwork (crop from this photo; replaceable slot — glare and walnut background remain in the source crop).
- Walnut and honey wood grain albedos.
- Stone floor albedo (uniform grey; avoid per-tile colour lottery).

**Uncertain dimensions**

- Exact walnut feature width/height, panel module size, logo physical size, cladding height, doorway width/height, spotlight count/spacing.

## Photograph 02 — Long side wall (left / −X)

**Visible architecture**

- Repeating wall bays: honey lower vertical cladding, cream upper plaster, white pilasters.
- Beige/tan roller blinds in recessed bays (not the grey ceiling canopy).
- Circular wall-mounted fans on upper plaster.
- Black wall speakers.
- Electrical plates on cladding.
- Suspended acoustic panels, dark grid, curved grey fabric canopy.
- Warm recessed lighting; optional coloured cove glow near blinds.
- Dark auditorium seating (reference only — not planner furniture).
- Wooden door with blue glazed lights visible toward the rear end of the panorama.

**3D geometry required**

- Bay rhythm (pilasters + cladding + plaster), not one continuous wood plane.
- Roller blinds with roller tube, fabric sheet, bottom rail, side brackets.
- Wall fans with guard rings, spokes, blades, hub, mount arm.
- Speaker cabinets with thickness and grille face.
- Sparse electrical plates.

**Best as textures**

- Blind fabric, wood grain, plaster colour, acoustic fabric for canopy.

**Uncertain dimensions**

- Bay spacing, cladding height fraction, blind width/height, fan diameter/mount height, speaker size, exact door leaf count (photo suggests a paired leaf at the rear corner).

## Photograph 03 — Stage front (+Z)

**Visible architecture**

- Large central white projection screen with warm backlight halo.
- Dark walnut panels flanking the screen with horizontal/vertical reveals.
- Two large portrait/near-square geometric banners (red/magenta/orange polygons) with authentic Navrachana mark and lettering.
- Light timber stage deck with plank direction; timber-clad front fascia; curved/rounded front corners.
- Access steps on **audience-right** (+X).
- Fixed wooden podium on **audience-left** (−X).
- Acoustic ceiling panels, dark framework, projector, warm downlights.
- Grey stone floor in the audience area.

**3D geometry required**

- Stage platform (immutable footprint/elevation), curved front fascia approximation within footprint, right-side steps, podium.
- Walnut backdrop panels + thick screen surface.
- Two large banner boards with thickness and mounts (not thin red strips).

**Best as textures**

- Separate left/right banner crops from this photograph (perspective-limited; logo sharpness is source-limited).
- Stage timber albedo; screen remains near-matte white material.

**Uncertain dimensions**

- Exact banner width/height, screen size, fascia curve radius, step tread/riser detail, podium size, backlight fixture type.

## Photograph 04 — Long side wall (right / +X)

**Visible architecture**

- Independent bay rhythm: honey lower cladding, cream upper wall, beige roller blinds in multiple bays.
- Circular wall fans, black speakers, electrical plates.
- Recessed strip lighting along cladding transition.
- Acoustic ceiling grid + curved grey canopy.
- Dark door with narrow vertical glazed panel near the **rear** end of the wall (far end of the panorama).
- Existing seating and grey tiled floor.

**3D geometry required**

- Right wall built independently from the left (bay count, blind count, fixture placement may differ).
- Same fixture families as photo 02, not a mirrored clone of every accessory.
- Single rear doorway interpretation shared with photo 01/02 (opening on rear wall near −X / photo-right), not an extra side-wall door unless later measured.

**Best as textures**

- Same material set as the left wall; do not wallpaper the full photo onto the mesh.

**Uncertain dimensions**

- Exact blind count (panorama foreshortening), fan spacing, door leaf width vs rear-wall opening size.

## Door orientation conclusion

Photo 01 is taken **facing the rear wall (−Z)**. The entrance sits on the **far right of that frame**, which is world **−X** (right of the Navrachana logo). The close-up door reference shows **dark walnut double leaves** with square vision lites and long silver centre pulls in a light timber surround. The Unity builder opens the rear wall on the **−X** side and builds that double-door assembly (decorative; no leaf colliders). Opening size remains configurable.

## Geometry vs texture summary

| Element | Prefer geometry | Prefer texture / material |
| --- | --- | --- |
| Walnut/honey panels, seams, pilasters | Yes | Wood albedo / grain |
| Door opening, frame, leaves, handle | Yes | Dark finish + glass tint |
| Wall fans, speakers, blind hardware | Yes | Metal / fabric |
| Stage fascia curve, steps, podium | Yes | Timber albedo |
| Large stage banners | Physical boards | Extracted banner artwork |
| Rear logo | Thin raised board | Photo crop / replaceable logo |
| Floor tiles | Efficient tile mesh or UV tiles | Uniform stone albedo |
| Ceiling panels, grid, canopy, duct | Yes | Acoustic / fabric / metal |
| Warm lighting | Real Light components + fittings | Emissive lens materials only as accents |

## Extracted project textures

Generated under `Assets/Textures/Architecture/` from the supplied photographs and procedural fills:

- `NavrachanaRearLogoCrop.png` — rear emblem crop (source glare/background remain; replaceable).
- `StageBannerLeft.png` / `StageBannerRight.png` — banner graphics from photo 03.
- Procedural albedos for stone, honey/walnut/stage timber, acoustic fabric, roller-blind fabric.

No university lettering was invented; banner and logo glyphs come from the source photographs only.
