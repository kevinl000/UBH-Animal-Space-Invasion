# Tutorial boss artwork

Open `preview.html` directly in a browser to compare the basic enemies, new Healthy Goose and Healthy Skunk sprites, and reused projectile art. Sliders preview size; their defaults match the new visual prefabs. `comparison.png` is a static review at the same relative scale.

Boss visual scale is 1.5. Each new texture's PPU normalizes its visible width at scale 1 to its corresponding basic enemy. Projectile visual prefabs reuse the current flight sprites at scale 1.3; existing impact visuals retain their current sizes and hit-area meaning.

Sources and exact built-in image_gen prompts are preserved separately in `../HealthyGoose/` and `../HealthySkunk/`. Runtime PNGs are unchanged copies under `Assets/Art/Enemies/`. `assembly.json` records paths, dimensions, facing and scales; `verification.json` records offline checks. Very faint alpha below 16/255 reaches a canvas edge in the generated originals; visible silhouettes at alpha >=16 have transparent margins and no clipping. Original alpha is preserved.

The new prefabs contain Transform and SpriteRenderer only. Use them as visual children of gameplay roots; follow the per-boss README orientation notes. The visual projectile prefabs cannot be assigned directly to fields typed as EnemyProjectile. No enemy data, stats, spawning, attack code, scenes, shiny artwork or shiny probability/rewards were changed. Shiny variants remain a future visual pass.

No Unity launch or Unity tests were performed. Manual review: inspect both boss prefabs beside their basic enemies through the intended camera, confirm transparency/readability and scale, then check inward outlet orientation and projectile spawn alignment after gameplay hookup. Check Skunk gas against actual damage recipients, keeping the existing angular damage rule.
