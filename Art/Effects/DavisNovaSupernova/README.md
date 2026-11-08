# Davis Nova supernova

Generated with the built-in imagegen tool in transparent generation mode.
`supernova-source-v2.png` is preserved unchanged; `prompt.txt` records the prompt.
`prepare_frames.py` crops the eight equal-grid frames onto fixed 512px canvases,
removing only alpha below 8 without changing RGB or rescaling the artwork.

The eight runtime sprites are under `Assets/Art/Effects/DavisNovaSupernova/`,
with Point filtering, 100 PPU, no compression or mipmaps and centered pivots.
`DavisNovaSunBlast` swaps them at 10 fps using the combat clock. Ignition starts
0.3 combat seconds before the deadline. The full-disk fourth frame coincides
with destroying every occupied tower in one frame. The animation ends empty;
its renderer is reused for subsequent blasts. Pause and safe breaks hold it.

The opaque peak core covers radius 6.014 world units. All five available tower
types at all twelve sites and three aim poses fit inside radius 5.227 even when
using conservative sprite rectangles. No distance check limits the actual wipe.

- `animation-review.gif`: timing preview on a dark background.
- `frame-review.png`: exported frames.
- `gameplay-sizing-review.png`: native tower transforms against approved art.
- `socket-review.png`: five tower types at all twelve sockets.
- `blast-coverage-review.png`: peak coverage.
- `sizing-and-coverage.json`: measurements and offline verification scope.

These previews are offline composites, not Unity screenshots. The team must
check imports, gameplay readability and repeated blasts in Unity manually.
