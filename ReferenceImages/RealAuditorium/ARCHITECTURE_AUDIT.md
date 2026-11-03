# Architecture audit — Navrachana Virtual Auditorium 2.0

Inspection date: 2026-11-03. Sources: four photographs in this folder, current `AuditoriumArchitectureBuilder.cs` / `BuildDemoScene.cs` / materials, and compiled Assembly-CSharp symbols after the CS0136 fix.

## Photograph → wall → Unity mapping

| Photo | Wall | Unity |
| --- | --- | --- |
| `01-rear-feature-wall.png` | Rear identity wall | Interior −Z (`z ≈ -12.5`) |
| `02-long-side-wall-left.png` | Long wall, audience-left facing stage | Interior −X (`x ≈ -8`) |
| `03-stage-front.png` | Stage / projection wall | Interior +Z (`z ≈ +12.5`) + stage platform |
| `04-long-side-wall-right.png` | Long wall, audience-right | Interior +X (`x ≈ +8`) |

Immutable frame: clear **16 × 25 × 3.65 m**; stage **9.5 × 3.7 m**, top **Y = 0.45**.

## Observed problems in the previous generated model

Root causes confirmed in code/materials (not “camera angle” issues):

1. **Floor too dark / patchy** — `VA_StoneTile` base colour ≈ (0.30, 0.31, 0.32) multiplied by a charcoal albedo (~RGB 78). Grout gaps expose an even darker `stoneTileDark` base collider, so tiles read almost black with high-contrast seams.
2. **Side walls read as flat beige/brown slabs** — one continuous cladding slab + tiny slat boxes; pilasters exist but bay rhythm and depth are weak; honey tone variation is material-swapped rather than modelled depth.
3. **Blinds indistinct** — fabric sheet only **0.025 m** thick, colour close enough to wall under warm light that blinds disappear at grazing angles; same MeshBatch as cladding.
4. **Speakers on blinds** — `BuildWallFixtures` places speakers/fans on a fixed Z grid **independent** of blind Z from `BuildSideWalls`, so fixtures sit on blind volumes.
5. **Fans tiny / weak silhouette** — diameter configurable but rings/spokes are thin; independent Z placement often lands on cluttered bays.
6. **Banners** — code now builds ~1.55×1.90 m quads with photo crops, but an older scene (or failed rebuild while scripts were broken) still showed thin strips. Artwork crops still include minor wood fringe.
7. **Door “absent” / wrong side** — code cuts a rear (−X) opening (right of the logo when facing the rear wall) under `Rear_Entrance_Door`. If the scene was never regenerated after compile, an old +X or solid rear wall may remain.
8. **Stage blocky** — plank strips and faceted fascia exist but remain shallow decorative boxes on a cube collider.
9. **Lighting flat** — warm lights exist in code, but dark floor albedo + modest ambient make the room feel underexposed; emissive fittings do not light geometry by themselves.

## Major visible elements (geometry vs texture)

| Element | Prefer geometry | Prefer texture / material | Notes |
| --- | --- | --- | --- |
| Walnut rear panels + seams | Yes | Walnut albedo | 3×4 panel grid estimate |
| Honey side cladding + pilasters | Yes | Honey oak albedo | ~40–45% wall height estimate |
| Cream upper plaster | Yes | Plaster colour | Distinct from blind fabric |
| Roller blinds | Yes (roller, sheet, rail, brackets) | Beige fabric | Not the grey ceiling canopy |
| Wall fans | Yes (housing, 3 blades, cage, arm) | Metal | Clear plaster bays only |
| Speakers | Yes (cabinet + grille + bracket) | Charcoal | Must not occupy blind Z |
| Rear door | Yes (dark double leaf + square lites + silver pulls) | Dark walnut pair | −Z wall, toward −X (right of logo) |
| Stage banners | Thick boards | Extracted left/right crops | Large portrait panels |
| Projection screen | Yes | Matte white | Centre +Z |
| Floor tiles | Efficient mesh or UV tiling | **Lighter** uniform grey | One shared material |
| Ceiling panels / grid / canopy / duct | Yes | Acoustic / fabric / metal | Keep hide/show root |
| Logo | Thin board | Photo crop / replaceable slot | No invented lettering |

## Approximate fixture layout (estimates)

**Door:** rear (−Z) wall, centre ≈ `−hallWidth/2 + doorwayInsetFromRight` (default inset 1.55 m from −X corner — right of the logo in photo 01). Opening ~1.9 × 2.35 m dark walnut double leaves with square vision lites and long silver pulls in a honey timber surround.

**Right wall (+X, photo 04):** more blinds (~5–6 bays), ~3 fans on plaster between blind bays, speakers on pilaster/plaster gaps only.

**Left wall (−X, photo 02):** fewer blinds (~3), ~4 fans, speakers between blind-free bays; rear door visible at panorama end.

**Banners (photo 03):** flanking screen, ~1.5–1.8 m wide × ~1.8–2.1 m tall, audience-left and audience-right of white screen.

**Known vs estimated:** room/stage frame known (project config). Decorative module sizes, bay spacing, blind/fan/speaker coordinates, fascia curve, canopy shape = visual estimates from perspective photos.

## Correction plan (this iteration)

1. Brighten uniform floor material + texture; remove dark gap readout.
2. Unified side-wall bay planner so blinds, fans, speakers never share Z occupancy.
3. Thicker, warmer, clearly offset roller blinds.
4. Larger fans with stronger cage/blade silhouette on clear plaster only.
5. More open/visible rear door assembly inside a real wall cut.
6. Ensure large photo-textured banners and stage/ceiling/lighting balance after regenerate.
