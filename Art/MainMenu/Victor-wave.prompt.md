# Victor's menu wave

Generated with the built-in image generation tool from the approved pixel-art background. Source art stays in `MainPage-background-victor.png`.

- `MainPage-background-wave.png`: stationary Victor with only his raised right arm removed, also imported as `Assets/Art/MainMenu/MainPage-background.png`.
- `Assets/Art/MainMenu/MainPage-victor-arm.png`: transparent arm layer, cropped in the Unity importer to exclude extraction residue. The generated arm is displayed at half size to match Victor.
- The right arm is on the viewer's left. Its shoulder stays fixed while it rotates +/-12 degrees over 1.8 seconds at a 12-frame cadence, using unscaled time.

## Background prompt

Use case: precise-object-edit.
Asset type: stationary background layer for a Unity pixel-art menu, prepared for a separately animated arm.
Input image: edit target, the approved background with pixel-art Victor E. Bull at the lower right.
Remove ONLY Victor's raised RIGHT ARM AND HAND (on the VIEWER'S LEFT side of his body). It is the small blue raised waving arm immediately left of his blue shirt, below-left of his pale muzzle, approximately inside x=1480..1548, y=720..786 on this 1672x941 canvas. Remove from the shoulder outward so there is no duplicate stationary waving arm remaining. Fill just the vacated arm silhouette with the underlying gray Baird Point stonework and green vegetation, using identical coarse pixel clusters. Keep a clean blue shoulder edge on the torso for the separate arm to overlap.
Keep Victor's face, horns, torso, emblem, other arm, legs, shoes, size, position, and all scenery EXACTLY unchanged. Keep the SAME 1672x941 dimensions, framing, palette and chunky square pixel art. Preserve the gray rabbit, turret, Baird Point's three columns and stepped top, sky, campus and river. The UFO is already absent and must stay absent. No new objects, no smoothing, no restyle, no UI or text. Fully opaque PNG.

## Arm prompt

Use case: background-extraction.
Asset type: transparent right-arm layer for animating the existing pixel-art Victor E. Bull.
Input image: edit target. Extract ONLY Victor's raised RIGHT ARM AND HAND, which appears on the VIEWER'S LEFT of the blue bull mascot at the lower right. Include the blue waving hand/fingers, wrist, forearm, elbow and upper arm all the way to its shoulder attachment at the blue shirt. This tiny arm occupies approximately x=1480..1548, y=720..786 on the 1672x941 canvas. Preserve the EXACT silhouette, pose, pixel clusters, black/navy outlines, blue palette, original size, location, and blocky pixel scale. Complete the hidden shoulder attachment with a small rounded blue overlap if necessary for a joint, but do not include his torso, head, white muzzle or emblem, other arm or any scenery.
REGISTRATION: output the SAME FULL 1672x941 landscape canvas; keep this tiny arm in its exact original lower-right location. Do NOT enlarge or center the arm. All remaining pixels must be genuinely TRANSPARENT. No grass, stone, sky, body, stars, no fake checkerboard. Hard stepped square-pixel edges, no smooth cartoon redraw, no glow, no shadow outside the arm. PNG with real alpha.
