# Coarse pixel restyle — built-in imagegen

The source globe stays unchanged in `globe-source.png`. The built-in imagegen
tool produced and edited the separate campus/ground sprite with transparent
background enabled. All source versions are retained; the current source is
`ground-campus-pixel-source-v7.png`.

## Coarse redraw, v5

Use case: precise-object-edit / pixel art style transfer.
IMAGE 1 is the EDIT TARGET: Silverman Luna's larger main library, two connected reading wings, trees, rounded continuous ground ring and exactly twelve outward-facing tower recesses, with transparent circular center. IMAGE 2 is the STYLE MASTER ONLY: the user's unchanged chunky pixel globe. The globe will be inserted separately; DO NOT draw or repaint a globe.

Restyle ALL visible artwork in image1 to match the VERY CHUNKY LOW-RES PIXEL ART OF IMAGE2. This is a true redraw in a coarser grid, not a sharpened detailed illustration. Match image2's large square color clusters, simple stepped silhouettes and flat cel shading. At 1254x1254 output each logical pixel is approximately 7x7 physical pixels. Think an approximately180x180px SNES game sprite scaled by nearest neighbor. Every visible feature must follow that coarse square grid, including outlines, stone borders, roofs, windows, tree silhouettes and socket rims.
Use a restrained palette of roughly24-32 solid colors across the asset. Dark navy outlines aboutone logical pixel thick, warm cream/tan stone with only3 tones, slate roofs with2-3 tones, amber windows and a few simple red/teal book blocks. Book emblem becomes a clear tiny pixel icon, doors large simple blocks. Trees have chunky blocky olive/sage foliage with ONLY3 tones, NO little leaves. Ground uses broad quiet tan stone/olive grass bands with a few large stone blocks; remove tiny flower dots, micro bricks, dithering, speckles, fine texture, thin lines, glows, soft highlights and gradients.
Preserve the existing DESIGN AND GEOMETRY: ONE slightly enlarged main library at top and TWO attached reading wings/low connecting arcades; same rounded ground shape and transparent circular globe opening; EXACTLY12 sockets at30degree orientation increments. Each socket remains a shallow carved outward-open notch with BROAD FLAT slate floor approximately100px wide and LOW inward cream front lip, not a detached balcony. Cardinal sockets face up/right/down/left respectively. No added buildings. Keep all floors wide and clear for the existing unchanged towers.
Retain the warm stone/navy roof campus palette, but rendering simplicity and pixel size must match image2. Entire image1 silhouette remains in approximately the same positions and fits within the square with a little transparent margin. Genuine transparent alpha outside AND inside circular hole. No globe, towers, characters, moon, text, UI, checkerboard or drop shadows.

## Upper arc and facade proportions, v6

Targeted geometry correction of this exact chunky pixel-art campus ring sprite. Preserve its new COARSE SQUARE PIXEL GRID, solid color clusters, flat3-tone shading, navy outlines and simplified trees/windows. Do not restore fine painted detail.
The transparent globe opening is too egg-shaped because the top ground dips too far into the center. Raise the TOP GROUND ARC and its TOP THREE SOCKETS by approximately55pixels on a1254px square: main library socket baseline about290px instead of345px; wing socket baselines about350px instead of405px. Compress only the heights of the main library and the two reading-wing facades by about15percent to connect naturally to their now-raised floors, keeping their roofs near the existing upper positions. They should still look a little larger than the previous detailed design, not huge. Keep ONE main library, TWO connected wings/arcades and the same trees.
The INNER EDGE of the continuous ground should now trace a ROUND CIRCLE centered near(627,700), radius about400pixels: top opening near y300, bottom near y1100, left x227, right x1027. Follow the same circle across the upper left/right arcs. No broad flattened top inside the circle, no egg-shaped opening. The lower ground and side sections stay approximately in place.
Keep EXACTLY12 empty sockets and their broad flat dark slate floors. They must face RADIALLY OUTWARD; all sockets on upper-right and lower-right rotated clockwise, rightmost opens right; lower sockets upside down with LOW rim on their inward/TOP side; leftmost opens left. Shallow open ground recesses, not complete oval wells. Maintain enough100px clear flat floor for unchanged90px-wide towers. Low cream front lip, no high foreground wall covering towers.
Keep real transparency both outside and inside the circular opening. No globe, moon, towers, extra buildings, labels, UI or soft gradients. Crisp low-resolution pixel-art sprite only, same palette and coarse pixel steps as this input. Whole silhouette remains within canvas with modest transparent margin.

## Outward sockets, v7

Edit ONLY the twelve socket cutouts in IMAGE1. Preserve IMAGE1's chunky low-resolution pixel-art style, large square clusters, flat solid palette, one main library/two connected wings, trees, rounded continuous ground, transparent central hole, and larger facade proportions.
IMAGE2 is a GEOMETRY REFERENCE ONLY for socket direction and proportions; DO NOT copy its fine detailed painting. Image1 is the only style target.

The side and diagonal recesses in image1 have become oval wells facing the viewer and have floors too narrow along the local tangent. Correct each into the SAME BROAD SHALLOW TOP POCKET, rigidly rotated to its radial outward orientation. EXACTLY12 cutouts.
Top pocket: flat horizontal100-120px slate seating edge, low cream lip toward planet center, open upward behind it.
At30degrees clockwise the entire U-pocket is rotated30degrees, its flat seating edge slopes30degrees.
At60degrees it is rotated60degrees; the seating edge steeply slopes60degrees and is100-120px LONG ALONG THAT SLOPE.
Rightmost pocket at90degrees: flat seating edge is VERTICAL, at least100-120px TALL, depth only30-40px horizontal. It OPENS RIGHT, no enclosing outer lip. This is critical: it is taller than it is wide, not a horizontal oval.
Bottom-right120 and150: same pocket rotated120 and150, opens lower-right.
Bottom180: upside down, seating edge horizontal100-120px long, LOW cream lip on the TOP/inward side; open down.
Bottom-left210 and240: same pocket rotated accordingly.
Leftmost270: flat seating edge VERTICAL100-120px tall, opens left, depth30-40px.
Upper-left300 and330: same pocket rotated to respective angle.
All are carved into the existing connected warm stone/grass surface, never floating. Raised side cheeks do not narrow the central flat100-120px clearance. Their LOW front lips must cover only a little of an existing90px-wide tower foundation. Keep the mouths open radially outward into transparency or the existing upper library facades behind them. Do not add floor crosshatching or detail. Flat slate and2-tone ivory/tan pixels only, thick navy pixel outlines.
Keep the existing rounded inner globe opening and the entire campus composition stable. No globe, moon, towers, characters, labels or UI. Genuine alpha both outside and in the center.
