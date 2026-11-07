# Lower-right bakery replacement

2026-11-05, `planets-page`, source commit `d7866ea`.

The user identified the lower-right cloth stand as unrecognizable. Replaced only
`stand-east-lower.png` with a bakery displaying three scored baguettes, a round
loaf and a bread pictogram. Retains its radial orientation, terracotta/timber
palette, 100 x 124 source dimensions and origin `(947, 680)`.

Created with built-in imagegen. [Exact prompt](generation.prompt.md) and
[unchanged generated output](bakery-generated.png) are retained here. Inputs were
the previous `Facades/stand-east-lower.png` at the source commit and the previous
`../../page-review.png` for style. Extraction resizes with nearest sampling
and intersects the generated alpha with the original accepted facade mask. It
does not expand the footprint; 555 original mask pixels are transparent in this
revision. Earlier generated sources remain unchanged.

The normal rebuild commands in `../../README.md` reproduce the replacement,
composition reviews, manifests and Unity `Facade8.png`, preserving its GUID.
All 2,892 sampled clearance checks and twelve foundation-contact checks pass;
the bakery retains 365 pixels of rear-ground overlap. Inspected the exported sprite
and empty/occupied planet compositions. Terrain, mounts, other facades, runtime
code and prefab are unchanged. Unity import and at-size Game view review remain
user-run through Hub; this is not visual acceptance or a Play Mode result.
