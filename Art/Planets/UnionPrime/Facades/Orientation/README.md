# Lower-arc orientation correction

2026-11-05, `planets-page`, source `3fb005e` (committed bakery replacement).

The user identified incorrectly oriented entities between 4 and 8 o'clock.
The lower kiosks' bread, basket, sack and books, and the small produce crates
beside the lower shops, were screen-upright despite sitting on the planet's
underside. Their local up now points away from planet center `(627, 610)`:
down-left on the southwest side and down-right on the southeast side. Bases and
supporting shelves face the planet; sack ties, bread tips and produce face outward.

Used built-in imagegen with the source commit's `TerrainPolish/planet-empty.png`.
Retained the [exact prompt](generation.prompt.md) and unchanged generated output
`orientation-generated.png`. The generation also changed shop framing, so only
the two kiosk regions and polygon-bounded crate patches are imported by
`extract_facades.py`. Original lower shop framing, awnings and signs are retained.
Coordinates and source hashes are recorded in `facades.json`. Original masks,
sprite dimensions, origins and Unity metadata/GUIDs remain intact; transparent
generated pixels may trim alpha within those masks.

Review [empty and occupied lower arc](lower-arc-review.png) and
[full composition](../../page-review.png). The normal three rebuild commands in
`../../README.md` reproduce the exports, reviews and Unity Facade9-12.
The committed lower-right bakery is unchanged.

Offline verification: all 2,892 clearance samples and all twelve foundation
contacts pass; mounts and clearance results match the source commit. All four
affected facades still overlap supporting ground. Changes to assembled pixels
stay inside the four documented extraction regions. Imported PNG hashes match
their sources. No gameplay/runtime, terrain, prefab, scene or metadata changes.

Unity import and Game view review remain user-run: open UnionPrime through Hub,
inspect the lower arc with craters empty and occupied, and check Q/E/R aiming.
No Play Mode or standalone build was run. Orientation correction is uncommitted
pending visual review; no push.
