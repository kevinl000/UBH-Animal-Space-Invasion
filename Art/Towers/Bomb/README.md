# Rabbit-operated Bomb tower

The Bomber Rabbit wears a brown jacket and amber goggles, with its ears exposed. Ivory/navy machinery and amber accents keep it related to the Laser tower. The rabbit, controls, seat and chassis stay fixed; a single wide-bore cannon rotates on a separate right-hand hinge through **local Z -60 to +60 degrees**, covering a 120-degree aiming arc relative to local outward/up.

Screen revision (2026-11-08): **base-source-v3.png** is current. It adds a small amber monitor on the stationary diagonal support below the moving cannon, facing the rabbit. The joystick remains. Cannon artwork, pivots, PPU, existing sprite metadata/GUIDs and prefab transforms are unchanged.

Open `preview.html` in a browser to move the aim slider or play the sweep. This preview renders the exact source sprites using the same pivots, scales and offsets as the Unity prefab. It also provides optional pivot and firing-line markers. Browser positive angles are converted to Unity's counterclockwise Z convention.

## Unity assembly

`Assets/Prefabs/Towers/BombTowerVisual.prefab` contains only these visual objects:

```text
BombTowerVisual             ground mounting origin
  Base                     stationary rabbit, chassis, hinge support and screen
  CannonPivot              rotate only this Transform for aiming
    Muzzle                 firing reference, local +Y points out of the bore
```

`Base` and `CannonPivot` have SpriteRenderers. Their assets are `Assets/Art/Towers/Bomb/BombBase.png` and `BombCannon.png`, with custom ground/hinge pivots, Point filtering, Clamp wrapping, alpha transparency, Full Rect meshes, no mipmaps/compression and Unity's default sprite material. Base draws at order 0 and cannon at order 1 on the Default layer. The neutral muzzle faces local **+Y**. The root and cannon use identity rotation by default.

The prefab has no aiming script or Animator. It supports the requested arc geometrically; gameplay must select targets and enforce the +/-60-degree clamp when rotating `CannonPivot`. Parent/rotate the entire tower with its planet so root +Y is outward. Aim offsets stay relative to that outward direction. The preview sweep is not an idle-animation requirement or an implementation of targeting.

Exact image-space points are in `assembly.json` (pixel-edge coordinates measured from top left):

| Part | Point / setting | Value |
| --- | --- | --- |
| Base | Ground mounting point | (582, 1204) |
| Base | Socket center | (1091.5, 478) |
| Base | Pixels per unit | 1500 |
| Cannon | Hinge center | (624.5, 980.5) |
| Cannon | Muzzle exit reference | (624.5, 126) |
| Cannon | Pixels per unit | 2600 |
| CannonPivot | Position relative to tower origin | (0.3396667, 0.484) |
| Muzzle | Position relative to cannon hinge | (0, 0.3286538) |

The separate PPU values normalize independently generated source images to a similar visual pixel scale. Both PNGs remain at their original 1254 by 1254 resolution; their transparent padding is preserved. At scale 1, the base's visible width is approximately 0.76 units, close to the Laser's 0.70-unit base footprint. The full aiming envelope is wider and taller than the mounting foot and must be checked against the final planet's available space. Reuse the intended overall tower scale when comparing to the scaled Laser on a planet; these are source-scale references, not a finished terrain fit.

## Artwork provenance

Built-in image generation created `base-source-v1.png` and `cannon-source-v1.png` using the approved Laser portrait as the family/style reference. The full firing-line review found that the initial base's socket was too low for left-limit shots to clear the ears. A targeted image-generation edit raised and reinforced that support in v2. The new **`base-source-v3.png`** is a targeted edit of that v2 base adding the amber monitor. Its rabbit and chassis remain visually consistent, but pixel-identical preservation through generation is not claimed. Original v1/v2 bases remain as source history and are not imported into Unity.

Exact prompts: `base.prompt-v1.txt`, `cannon.prompt-v1.txt`, `base-raised-hinge.prompt-v2.txt` and `base-screen.prompt-v3.txt`. Runtime PNGs are byte-identical copies of current base v3 and cannon v1. This tower revision changes no scenes, planet/socket prefabs, TowerData or gameplay code. Separate shell, flash and explosion visual assets already exist in [RabbitBomb](../../Projectiles/RabbitBomb/README.md); attack-radius logic, recoil and firing hookup remain separate work.

## Verification and manual review

Offline geometry sampling checked **241 angles at 0.5-degree intervals** again with the screen-equipped base, using alpha >=128 and cannon samples every four source pixels. Every sampled pose retained hinge contact, with zero sampled cannon/base overlap outside a 0.12-unit joint region and zero sampled base intersections along the muzzle's outward centerline. The screen's conservative bounds in `assembly.json` stay protected even near that region; the exclusion covers only the intended breech/socket and upper support, not the rabbit or lower chassis. Results and source hashes are in the manifest; detailed samples are local at `.git/menu-work/bomb-tower/geometry-samples.json`. Run `python Art/Towers/Bomb/verify_assembly.py` with Pillow to recheck and rebuild the five-angle `sweep-review.png`.

These are sampled image checks, not a continuous collision proof. A clear centerline does not establish clearance for a bomb of arbitrary width. No Unity tests or Unity validation runs were performed, per `AGENTS.md`.

In Unity, inspect the prefab at neutral and both limits, then intermediate poses. Confirm the rabbit stays fixed, the cannon covers the socket, `Muzzle` follows the bore and the sprites/materials import correctly. Review crater seating, foreground occlusion, adjacent-tower clearance, bomb width and readability at final camera scale on the intended planet before gameplay hookup.
