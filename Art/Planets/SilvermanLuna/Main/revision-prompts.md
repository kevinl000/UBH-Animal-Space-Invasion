# Connected campus revision — built-in imagegen, 2026-11-08

Both revisions used the built-in imagegen tool with transparent background enabled.
The final generated source is `library-planet-source-v3.png`; it is preserved
unchanged. Cropping, alpha cleanup, measured layer extraction and sprite assembly
are performed separately by `build_review.py`.

## Connected campus (`library-planet-source-v2.png`)

Use case: precise-object-edit / pixel-art production sprite.
Image 1 is the rejected Silverman Luna draft to REBUILD. Image 2 (Union Prime) is a composition and grounded socket reference ONLY, do not copy its market/guild buildings. Image 3 is the existing tower sprite for PIXEL SCALE and STYLE ONLY, do not bake it into the output.

Rebuild Image 1 into a cohesive library campus planet, NOT separate objects stuck around a globe. Transparent square PNG, approximately 1254x1254 canvas. Entire asset fits with transparent margin. No text/UI/moon/characters/towers.

Main center: a normal blue-green spherical planet with calm teal oceans and sage green continents. Refine the hugely coarse chunky continent steps in image 1 so the logical pixel scale matches the architecture and the rabbit tower. All parts use consistent crisp 16-bit pixel-art clusters, dark outlines, restrained flat stepped shading, no smooth or painterly rendering.

CRITICAL surface/socket construction: the globe has a CONTINUOUS substantial warm ivory limestone + earth surface edge around the entire circumference, physically joined to the blue/green center. This is the actual planet surface. Carve EXACTLY TWELVE inward, shallow U-shaped tower recesses INTO THIS CONTINUOUS SURFACE, at 12,1,2,3,4,5,6,7,8,9,10,11 o'clock, 30 degree spacing. At each recess the surface silhouette dips inward; the cheeks of the recess are integrated with neighboring ground. The twelve recesses must be identical and OUTWARD-FACING. Give each a dark slate inset floor approximately 100 pixels wide, broad FLAT floor for the tower's two feet, and a LOW warm cream foreground stone lip on its inward/planet side. At the top site the tower will stand upright pointing up, with its foot baseline approximately (627,250); successive sites rotate clockwise around center (627,650) with radius400. Socket total width approximately130px. Keep their central floor height shallow and low so a 90px-high tower is visible. The pockets are depressions in the planet edge, NOT standalone balcony cups outside the globe, NOT separate floating platforms. Connect every neighboring pocket with visible uninterrupted stone/soil and restrained grass.

Architecture: ONE welcoming large main library at 12 o'clock behind the top tower recess, warm ivory stone, navy/slate roof, central oak double doors, amber arched bookcase windows and modest open-book emblem. TWO smaller reading wings on its immediate upper-left and upper-right, at roughly 10:30 and 1:30 o'clock, physically CONNECTED to the main library by low curved stone arcades/terraces following the globe's surface. Together this is ONE connected campus skyline across the upper arc. No repeated library buildings on the sides or bottom. A few small olive/sage campus trees integrated with this upper campus and sparse trees along side terraces, never filling tower floors. Most of the lower hemisphere perimeter is quiet continuous limestone/earth with twelve clear usable sockets around the entire globe. Normal planet center remains dominant.

Match the reference's convincing carved ground pockets and tower-friendly scale. Preserve the library theme from image 1 but replace its disconnected geometry completely. Transparent outside the silhouette with real alpha, no glow, no drop shadows, no checkerboard.

## Outward-facing sockets (`library-planet-source-v3.png`)

Edit this exact Silverman Luna pixel-art planet. Preserve the cohesive SINGLE upper library campus, two connected reading wings/arcades and trees, the normal teal-green globe, continuous warm stone and grass surface, and overall polished pixel rendering. Transparent square PNG.
Correct ONLY the twelve tower sockets and the adjacent ground they carve into. Current sockets wrongly look like cups lying flat on a round map; all are facing the viewer. This is a 2D SIDE-VIEW planet and the towers must stand perpendicular to the local surface, pointing radially OUTWARD.
Use the TOP socket as the orientation master: its horizontal recessed slate floor and LOW cream foreground lip face UP away from planet center. Make twelve instances of that same shallow inset cutout around the globe, at 30 degree clockwise steps. Top one angle0; upper-right30; right-upper60; RIGHTMOST SOCKET MUST BE TURNED90 DEGREES so its flat slate floor is VERTICAL and opens to the RIGHT into transparent space; lower-right120 and150; BOTTOM SOCKET MUST BE ROTATED180 DEGREES, inverted so it opens DOWN into transparent space and its low lip is on its TOP/planet side; lower-left210 and240; LEFTMOST SOCKET MUST BE TURNED270 DEGREES with VERTICAL flat floor and open toward LEFT into space; upper-left300 and330.
All 12 must be outward-facing not upright-on-the-page. They are shallow U-shaped notches carved INWARD into the planet's edge, not complete oval wells visible on the planet face. Open mouth at outer silhouette, low cream retaining lip toward planet center. Remove the wrong oval cups and reconnect the continuous ground where needed. Every socket stays physically attached to continuous planet terrain. Preserve a broad flat 100px central floor for 90px-wide tower foundations; raised cheeks do not block it. No extra sockets beyond12.
Do NOT add towers, characters, labels, UI, rings or moon. Outside remains real transparent alpha; remove stray bright red edge pixels.
