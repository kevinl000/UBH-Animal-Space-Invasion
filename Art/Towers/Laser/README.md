# Laser: accepted compact sweep design

2026-11-05: the user accepted the compact rabbit-operated Laser and requested
removal of unused designs. The current files are in
[Redesign/Sweep/](Redesign/Sweep/README.md).

The user selected **150%** for the accepted
[Union Prime terrain and tower fit](../../Planets/UnionPrime/README.md).
Obsolete planet prototypes were removed; current mounts and foreground contact
come from that pipeline. The retained Sweep sprites supply the tower mechanism.

- [Animated sweep](Redesign/Sweep/sweep.gif).
- [Separate base/operator](Redesign/Sweep/base-sprite.png) and
  [rotating gun](Redesign/Sweep/gun-sprite.png).
- [Twelve-site comparison](Redesign/Sweep/twelve-site-sweep.png).
- [Assembly, pivots, validation and rebuild instructions](Redesign/Sweep/README.md).

Retained the selected design's generated sources, prompts, sprites, previews,
mount coordinates and rebuild script. Removed 27 obsolete files: the original
single-piece concept, tall-mast experiment, static redesign intermediate and
obsolete tooling. The current rebuild no longer depends on them. Existing
current asset paths and approved image pixels are unchanged.

The gun rotates +/-60 degrees while the rabbit and base stay fixed. The offline
checks cover 241 angles at each of twelve mounts. Unity import, foreground soil
lips and final crater finishing remain pending. No gameplay is implemented here.
