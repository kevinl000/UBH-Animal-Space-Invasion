# MainPage animation layers

Created from the approved concept using the built-in image generation tool.

## Files

- `MainPage-background.png`: opaque background with the original UFO removed. The rabbit, turret, Baird Point and scenery remain part of the static background.
- `MainPage-ufo.png`: Canada-goose UFO on a transparent canvas, retaining its upper-right placement.
- Both PNGs are 1672 x 941, matching `../Concepts/MainPage-approved.png`.
- `MainPage-background-victor.png`: the original complete, stationary Victor beside Baird Point, retained as a source reference.
- `MainPage-background-wave.png`: retained source for the previous version, with Victor's body painted into the background and his raised arm removed.
- The live background again matches the clean `MainPage-background.png` source. Victor is now separate from the scenery.
- `Assets/Art/MainMenu/MainPage-victor-body.png`: transparent stationary Victor, including his lowered arm.
- `Assets/Art/MainMenu/MainPage-victor-wave.png`: four transparent raised-arm poses, sliced into sprites. `VictorWave.anim` swaps those sprites; `VictorWave.controller` plays the loop.
- The old `MainPage-victor-arm.png` remains as a source reference; scenes no longer use it. See `Victor-sprite-wave.prompt.md` for the new generation prompts.

## Open the menu

- Branch: `main-menu`.
- Open `Assets/Scenes/MainMenu.unity` in Unity 6000.6.7f1 and press Play.
- Working title: **Game of Mascot**. Edit the `GameTitle` Text component to change it.
- New Game opens `LevelSelect`. Choose a campus destination and use Back (or Escape) to return.
- Continue is disabled because no save system exists yet. Level launching is also disabled until gameplay scenes are connected; destination selection already works.
- Keyboard/gamepad navigation and mouse selection use the project's existing Input System package. Selection, confirm, and back actions use Kenney sounds.
- MainMenu is the first enabled build scene, followed by LevelSelect. The existing SampleScene remains in the build list.
- The title and buttons are native Unity UI. `MenuUfoMotion` moves the separate UFO in whole reference-pixel steps. Victor is an independent UI Image with a child `RightArm` Image. His Animator swaps four arm sprites while his body stays still; it does not rotate his arm. Both animations use unscaled time and appear in both menu scenes.
- To edit the wave, select `ArtworkFrame > Victor`, then open **Window > Animation > Animation**. `VictorWave` is a looping 1.8-second clip. Move its sprite keys to adjust timing, or replace the arm sprites to change the poses. The Animator uses **Unscaled Time** so it continues when gameplay time is paused.
- To reposition the entire mascot, move `Victor`; its arm follows as a child. Keep `Raycast Target` disabled on both images so the artwork cannot intercept menu clicks.
- `Tools > Main Menu > Create Missing Menu Scenes` can create either missing menu scene and configure imports. It intentionally does not overwrite existing scenes.
- `Tools > Main Menu > Set Up Victor Sprite Wave` migrates the previous arm object in saved menus. It preserves existing Victor objects and animation assets, including hand-edited timing.
- The artwork frame preserves its aspect ratio on other screen shapes, using dark borders where needed.

## Asset credits

- UI: Kenney, **UI Pack - Pixel Adventure**, CC0. Selected source: `Tiles/Large tiles/Thick outline/tile_0000.png`. https://kenney.nl/assets/ui-pack-pixel-adventure
- Sounds: Kenney, **Interface Sounds**, CC0. Uses `tick_001.ogg`, `confirmation_001.ogg`, `back_001.ogg`. https://kenney.nl/assets/interface-sounds
- Font: **Pixelify Sans**, SIL Open Font License; downloaded from the Google Fonts repository. https://github.com/google/fonts/tree/main/ofl/pixelifysans
- License files accompany each imported third-party asset folder.
- Generated background and UFO: built-in image generation tool; prompts below and in `../Concepts/MainPage-approved.prompt.md`.
- Victor appearance reference: UB photo by Meredith Forrest Kulwicki. The photo is a reference, not an imported game asset. https://www.buffalo.edu/ubnow/stories/2022/03/true-blue-victor.html

## Placement and import

The UFO layer contains a few faint extraction residues away from the craft. Import only the following rectangle as a sprite to exclude them:

- Rectangle in image coordinates (origin top-left): x=1308, y=94, width=224, height=166.
- Equivalent Unity sprite rectangle (origin bottom-left): x=1308, y=681, width=224, height=166.
- Center in the full image: (1420, 177) from the top-left, or (1420, 764) from the bottom-left.
- A centered 1672 x 941 UI layout places the cropped sprite at offset (584, 293.5) with size 224 x 166.
- Use point filtering, no mipmaps and no texture compression to retain the pixel edges.
- Preserve the background aspect ratio. Do not stretch it to a different aspect ratio.
- Victor's body uses Unity rectangle `(1450, 47, 188, 296)` from the 1672 x 941 transparent source, displayed at `131.6 x 207.2`, at `(733, -297.5)` in the artwork frame.
- The arm sheet is 1254 x 1254, sliced into four 627 x 627 cells in reading order. The `RightArm` Image is `84 x 84`, with pivot `(0.87, 0.32)` and position `(-32, 0)` relative to Victor. Transparent padding preserves the shoulder registration across frames.
- Wave defaults: four poses held for 0.45 seconds each, looping every 1.8 seconds. The clip has sprite keys only; there are no position or rotation curves. The rabbit and monument remain static.

## Verification

- Background is fully opaque, and both layers retain the approved canvas dimensions.
- UFO has genuine alpha transparency. All pixels with alpha >= 16 are contained within x=1314, y=100, width=211, height=150 (top-left coordinates), inside the padded import rectangle above.
- Source copies were verified with SHA-256 hashes.
- Visually checked the removed UFO region, the landmark silhouette, the light-gray rabbit, the Canada-goose markings, and the black bill.
- Unity compilation and the Play Mode flow test passed: keyboard entry, mouse destination selection, return navigation, duplicate-click protection, UFO motion, and audio availability. Test source: `Assets/Tests/Menu/`.
- Off-screen scene previews were checked at 1672 x 941 and 1280 x 960; the narrower display preserves the art with dark borders. Destination labels use a smaller size so Student Union fits.
- The sprite-wave update changes the existing Play Mode test to check all four poses while `Time.timeScale` is zero, fixed body/arm transforms, destination-menu animation, and the existing navigation/audio checks. See the latest validation result below.
- Sprite-wave validation on 2026-11-02 in Unity 6000.6.7f1: **1 Play Mode test passed, 0 failed**. Rendered all four Victor poses, both scenes at 1672 x 941, and MainPage at 1280 x 960. Checked the shoulder join and aspect-preserving borders. Results and previews are local under `.git/menu-work/sprite-*` and `.git/menu-work/Victor-frame-*.png`; no standalone build was run.

## Background generation prompt

Use case: precise-object-edit.
Asset type: clean background layer for an animated Unity game main menu.
Edit the supplied approved pixel-art image by REMOVING ONLY the small Canada-goose UFO at the UPPER RIGHT, including its bubble canopy, goose pilot, saucer, orange lamps and orange exhaust flame. Fill the small area it occupied with the surrounding dark navy starfield, continuing the existing sky seamlessly. Leave the nearby purple planet and the existing stars intact. No replacement object.
Keep the SAME full image canvas dimensions, framing, composition and pixel scale. Preserve all remaining artwork as exactly as possible, particularly the light-gray rabbit and turret at lower left, Baird Point's three slender columns with stepped two-tier stone top and short column remnants at lower right, foreground plants, campus buildings, lake, galaxies, stars, moon, palette and empty central title/button area. This is an object-removal pass, NOT a restyle, NOT a new composition. Do not simplify or add any objects. No text, no UI, no watermark. Fully opaque PNG background.

## UFO generation prompt

Use case: background-extraction.
Asset type: transparent animated layer for a Unity pixel-art menu.
Extract ONLY the small CANADA GOOSE UFO from the UPPER RIGHT of the supplied approved illustration. Keep the exact same UFO design: left-facing black-billed Canada goose with black head/neck, white cheek patch, brown-gray body, blue pixel bubble canopy, blue-gray pixel saucer, orange pixel lamps, and small orange pixel exhaust underneath. It must remain crisp stepped PIXEL ART, not smooth cartoon/vector art, no new detail, no redesign. Preserve the exact colors, shape, angle, original size and placement.
IMPORTANT REGISTRATION: Keep the same FULL LANDSCAPE CANVAS dimensions as the supplied reference and keep the UFO at its original upper-right pixel position. Do not enlarge or center the UFO. All pixels outside the UFO must be genuinely TRANSPARENT, including the entire former scenery, stars, moon, rabbit, building, sky and foreground. Preserve the transparent areas inside/around the curved silhouette; no opaque square backing, no printed checkerboard, no fake transparency. Within the canopy keep the existing blue glass and the goose so it reads against the background. Do not include the adjacent purple planet, surrounding stars or galaxy textures. Output one PNG layer with real alpha, no text or labels.

