# Architecture realism — manual inspection checklist

Run after **Tools > Virtual Auditorium > Build Demo Scene** (Edit Mode only) on Unity **6000.3**. Use Walkthrough for close inspection and Planning View for overall composition.

## Architectural geometry

1. [ ] Inspect rear feature wall from inside: individual walnut panels, seams, honey flanks, cream upper wall, centred logo slot.
2. [ ] Inspect the stage wall: white projection screen, walnut flanks, two large geometric banners (not thin strips).
3. [ ] Inspect both long side walls: bay rhythm, cladding, pilasters, blinds, fans, speakers (walls are not identical clones).
4. [ ] Confirm clear room still **25 × 16 × 3.65 m**.
5. [ ] Confirm stage still **9.5 × 3.7 m**, top **Y = 0.45 m**.
6. [ ] Confirm rear doorway is on the −X side (right of the logo when facing −Z), with dark double leaves, square vision lites, long silver pulls, and a light timber surround under `Rear_Entrance_Door`.
6b. [ ] Confirm two square colourful Navrachana posters sit on the walnut flanks left and right of the stage projection screen (`Stage Poster Left` / `Stage Poster Right`).
7. [ ] Confirm door is on the rear (−Z) wall toward −X (right of the logo when facing the rear wall), matching photo 01.
8. [ ] Inspect wood-panel subdivisions and grain direction at close range.
9. [ ] Confirm each stage banner is a large rectangular board with extracted artwork.
10. [ ] Inspect the projection screen (matte, readable, not overexposed).
11. [ ] Inspect podium (audience-left) and curved timber stage fascia; steps on audience-right.
12. [ ] Inspect wall fans close and far: guard rings, spokes, three blades, mount arm (not flat discs).
13. [ ] Inspect roller blinds: top roller, fabric sheet, bottom rail, brackets.
14. [ ] Inspect black speakers and sparse electrical plates only.
15. [ ] Inspect ceiling panels, dark grid, curved grey canopy, ducting, downlight fittings in Walkthrough.

## Floor

16. [ ] Floor colour is uniform charcoal/medium grey across the hall.
17. [ ] Tile sizes are consistent (~0.7 m configurable).
18. [ ] Grout lines are thin and regular.
19. [ ] No random bright/dark tile patches.
20. [ ] Floor looks sharp near and far; placement rays still hit `Auditorium_Floor`.

## Lighting

21. [ ] Warm-white (~3000 K) atmosphere throughout.
22. [ ] Stage spots aim at the stage/backdrop.
23. [ ] Rear logo wash is visible on the walnut feature.
24. [ ] Projection screen is not blown out.
25. [ ] Corners are not excessively dark; room is not neon orange.
26. [ ] Ceiling hides in Planning View and returns in Walkthrough without disabling wall/fan/furniture lights incorrectly.

## Rendering quality

27. [ ] Wood textures readable close up.
28. [ ] Banner artwork readable close up (source-limited sharpness noted).
29. [ ] Fan cages/blades readable close up.
30. [ ] Blind and canopy fabrics readable close up.
31. [ ] Floor tiles consistent at multiple distances.
32. [ ] Texture scale/filtering sensible (aniso + mipmaps on architecture textures).
33. [ ] Materials look matte/satin, not plastic-glossy.
34. [ ] Game View at 1× / 1920×1080 is not unexpectedly soft from render scale (URP scale ~1.0, 4× MSAA).

## Existing functionality

35. [ ] Apply Theatre, Classroom, and Conference templates.
36. [ ] Place and edit catalog furniture.
37. [ ] Collision/bounds validation still works near walls, stage, and door.
38. [ ] Walkthrough inspects new architecture.
39. [ ] Walk near the rear doorway without collider traps.
40. [ ] Return to Planning View.
41. [ ] Named custom layouts still save/load.
42. [ ] Layout JSON contains no fans/blinds/door/architecture objects.

## Repeat-build and stability

43. [ ] Run **Build Demo Scene** twice outside Play Mode.
44. [ ] No duplicate walls, ceiling, fans, banners, or lights; planner roots preserved.
45. [ ] No missing scripts on generated objects.
46. [ ] No red Console errors from generation or Play.
47. [ ] Scene remains responsive on MacBook Air M3 8 GB during walkthrough + templates.
