# Union Prime: Unity game sprites

This folder contains the nineteen exported sprites used by
[UnionPrimeVisual.prefab](../../../UI/Planets/UnionPrimeVisual.prefab).
Their `.meta` files hold Unity import settings and GUIDs referenced by the prefab.

The [art-production workspace](../../../../Art/Planets/UnionPrime/README.md)
contains editable sources, generation prompts, masks, layout data, rebuild tools
and preview compositions. The matching PNGs there are intentional source/export
copies. Start there when changing the artwork.

## Sprite map

Source paths below are relative to `Art/Planets/UnionPrime/`, except the shared
Laser files, which are relative to the repository root.

| Game sprite | Art source |
| --- | --- |
| `RearGround.png` | `rear-ground.png` |
| `Terrain.png` | `terrain.png` |
| `Foreground.png` | `foreground.png` |
| `Facade0.png` | `Facades/guild-support.png` |
| `Facade1.png` | `Facades/guild-facade.png` |
| `Facade2.png` | `Facades/guild-foundation-visible.png` |
| `Facade3.png` | `Facades/market-northwest.png` |
| `Facade4.png` | `Facades/market-northeast.png` |
| `Facade5.png` | `Facades/stand-west-upper.png` |
| `Facade6.png` | `Facades/stand-east-upper.png` |
| `Facade7.png` | `Facades/stand-west-lower.png` |
| `Facade8.png` | `Facades/stand-east-lower.png` |
| `Facade9.png` | `Facades/shop-southwest.png` |
| `Facade10.png` | `Facades/shop-southeast.png` |
| `Facade11.png` | `Facades/kiosk-southwest.png` |
| `Facade12.png` | `Facades/kiosk-southeast.png` |
| `LaserBase.png` | `Art/Towers/Laser/Redesign/Sweep/base-sprite.png` |
| `LaserGun.png` | `Art/Towers/Laser/Redesign/Sweep/gun-sprite.png` |
| `LaserPortrait.png` | `laser-portrait.png`, composed from the Laser parts during export |

The [export manifest](../../../../Art/Planets/UnionPrime/unity-import.json)
records the exact paths and source hashes.

## Updating artwork

Follow the [source rebuild instructions](../../../../Art/Planets/UnionPrime/README.md#rebuild)
from the repository root. The final `prepare_unity.py` step copies the sprites
here and updates the existing visual prefab's sprite references and mounts.
It preserves existing asset GUIDs and import metadata.

Direct pixel edits here will be overwritten by the next export. Adjust Unity
import settings on these assets and keep each sprite with its existing `.meta`
file. Sources, prompts, scripts and preview sheets belong in the art-production
workspace. No rebuild is needed just to open the existing Union Prime scene.
