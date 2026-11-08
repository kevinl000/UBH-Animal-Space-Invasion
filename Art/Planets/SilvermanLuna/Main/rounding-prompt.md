# Larger facades and rounded ground — built-in imagegen, 2026-11-08

The built-in imagegen edit used `library-planet-source-v3.png` as the edit target
and `globe-source.png` as the user's required globe reference. Transparent
background was enabled. The generated output is preserved unchanged as
`ground-campus-source-v4.png`. The supplied globe remains an unchanged separate
source, uniformly scaled by the assembly. Its pixels are not regenerated.

## Exact generation prompt

Use case: precise-object-edit.
Edit Image 1 (current Silverman Luna campus planet). Image 2 is the user's REQUIRED globe insert, which will be composited separately at its original pixel style; DO NOT PAINT A NEW GLOBE.
Deliver one transparent square PNG sprite of ONLY THE CAMPUS BUILDINGS + CONTINUOUS ROUNDED GROUND EDGE + TWELVE SOCKETS, with a LARGE TRANSPARENT CIRCULAR HOLE in its center for Image 2. Outside is also transparent. No checkerboard, glow or shadows.

Required changes:
1. Enlarge the main library facade and both existing reading-wing buildings by about 15 percent, keeping their familiar ivory stone/navy roofs, amber bookcase windows, oak doors and open-book emblem. ONE main library with TWO connected wings/arcades across the upper arc, same trees. Larger buildings should feel naturally grounded and fit entirely within the canvas, with modest transparent margin at top. Do not add buildings elsewhere.
2. Refine the continuous ground into a truly ROUND circular surface around the globe. The inner edge must follow ONE CLEAN CIRCLE, not the current sagging flattened/wavy lower oval. No scalloped inner shoreline and no irregular green band intruding into the globe. Keep a narrow, consistent-radius cream limestone inner border. Adjacent ground between sockets follows smooth circular arcs with crisp pixel steps. A little grass and quiet earth/stone texture, no excess visual noise. Blend all 12 recesses into this continuous ground instead of separate balcony pieces.
3. Geometry guidance on 1254x1254 canvas: circular globe opening centered near (627,680), radius about410px, continuous ground outer radius about480px. The twelve socket floors are at about radius440px with 30 degree spacing. Main building and wings may extend above this ground to fill the upper margin. Every part of the lower half follows the same circular geometry; left/right roundness symmetric.
4. Keep EXACTLY TWELVE existing outward-facing sockets, one each at 0,30,60,90,120,150,180,210,240,270,300,330 degrees clockwise from top. Their mouths open radially OUT into transparent space, the rightmost floor vertical and the bottom floor inverted. Each has broad flat dark slate floor approximately100px wide and a LOW cream foreground lip on the inward/globe side. Keep the size suitable for the existing unchanged tower, roughly90px wide. Shallow recessed ground cutouts, not big oval wells.
5. The entire blue/green center becomes REAL ALPHA TRANSPARENCY. Do not repaint the Image 2 globe. Do not put blue/green color in the circular opening; we need separate sprites for exact image compositing. Preserve pixel art with dark outlines and stepped shading, consistent with current architecture and the supplied chunky globe.
No towers, characters, moon, text or UI. Whole asset comfortably fits square canvas.
