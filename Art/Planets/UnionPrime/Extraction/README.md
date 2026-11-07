# Retained original guild layers

The user-approved current terrain is in [Circular/](../Circular/README.md).
This folder retains the two original guild pieces needed by the structure pipeline:

- `guild-facade.png`: guild hall and its attached stalls.
- `guild-foundation-visible.png`: visible masonry/threshold.
- `guild-facade-mask.png` and `guild-foundation-mask.png`: editable source selections.

Both sprites use 370 x 252 canvases and the original top-left origin **(442, 65)**
on the 1254-square artwork. The [structure pipeline](../Separation/README.md) reuses
them without changing their pixels. Hidden edges and support alignment against the
approved circle remain unfinished.

Obsolete guild comparison/crop/reassembly images, the north-terrain sample, the
old planet mask and the superseded original extraction script were removed at the
2026-11-04 approved checkpoint. B-25 itself remains unchanged at
`Art/Concepts/UnionPrime-design-B-25.png`, as requested by the user.

`extraction.json` is historical fidelity evidence from the original extraction:
619,634 retained source pixels and zero reassembly differences. Its old export
paths are provenance, not a current assembly recipe. Use the current Separation
manifest and Circular README instead.
