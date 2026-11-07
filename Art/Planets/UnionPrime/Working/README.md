# Historical source for structure extraction

The user approved the [circular terrain with natural grass](../Circular/README.md)
on 2026-11-04. This folder is no longer the current visual design.

`planet-isolated.png` is retained solely because `../Separation/separate_structures.py`
uses its original pixels and checks its hash to rebuild the accepted building
sprites and the terrain source used by the circular-terrain script.

The obsolete full-planet preview, remainder, generated placeholder repair, repair
mask and superseded repair script were removed at the approved checkpoint.
`placeholder-removal.json` and `placeholder-removal.prompt.md` remain historical
provenance. Their input/output filenames describe the earlier operation; removed
intermediates are not required to rebuild the current approved artifacts.

Do not restore the old terrain as the production design. Use `Circular/` for the
terrain and `Separation/` plus the two `Extraction/` guild layers for buildings.
