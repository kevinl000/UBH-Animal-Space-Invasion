# Silverman Luna: latest flat-center sockets

The current main-planet source is `main-flat-sockets-source-v13.png` (2026-11-08).
It supersedes v11's deep crescent sockets and the earlier capped troughs.
The user's Commons Minor v6 example supplies socket geometry only: wide flat
seating centers, short rounded shoulders, shallow openings and thin flush rims.
The fine pixel-art library campus, globe, trees and warm limestone/slate palette
remain the visual direction. Exactly twelve sockets face outward.

Built-in imagegen performed the artwork edits with transparent backgrounds.
The prompt and reference roles are in [flat-socket-prompt-v13.md](flat-socket-prompt-v13.md).
Original sources remain separate; the approved moon files are unchanged.
V12 was discarded because it retained raised tray borders.

- [Empty and occupied main planet](FittedSites/page-review.png)
- [Twelve empty socket shapes](FittedSites/socket-shape-review.png)
- [Tower contact and three aiming poses](FittedSites/contact-review.png)
- [Main planet beside the preserved moon](FittedSites/with-moon-review.png)
- [Assembly and measured mounts](FittedSites/assembly.json)
- [Offline verification](FittedSites/verification.json)

## Assembly and Unity

Render globe, terrain/background, independent towers, then low inner lips.
The raised shoulders stay behind towers. The shared Laser base/operator and gun
sprites remain separate from terrain. Tower mounts follow each painted flat
floor's measured orientation rather than assuming twelve identical radial angles.
The 600-unit main canvas uses tower scale 1.25; the independent 180-unit moon
uses 4.166667. Both display bases at 41.866 by 43.062 units. Moon orbit/rotation
is not configured.

`Assets/UI/Planets/SilvermanLunaVisual.prefab` and imported Globe, Terrain and
SocketFronts sprites are updated for v13, preserving existing GUIDs and point
filtering. Moon imports retain their artwork. This revision requires no changes
to gameplay/managers, scenes, menu setup or project settings.

Rebuild from the project root with Python/Pillow and Chrome:

```powershell
python Art/Planets/SilvermanLuna/Main/build_review.py
python Art/Planets/SilvermanLuna/Main/prepare_unity.py
```

## Verification and limits

Offline checks cover all 744 supported/overlapped foundation samples, protected
upper-base visibility, and 23,004 gun pixels at -60/0/+60 degrees. Another 14,760
samples check the full globe/ground seam. See the generated verification report
for the current results; these are artwork checks, not combat or collision tests.
Contact sheets and full compositions are visually reviewed after rebuilding.

No new Play Mode result is claimed. The main project is open in Unity, so batch
startup was rejected. An isolated validation copy failed package resolution when
C: ran out of space. Logs are under `.git/menu-work/silverman-moon-shape/`.
Earlier README test results described older artwork and do not verify v13.

Manual check in Unity 6000.6.7f1: open `Assets/Scenes/SilvermanLuna.unity` and
press Play. Select Laser, click each of the 12 main and 4 moon sockets, aim with
Q/E, reset aim with R, and right-click to remove. Check flat foundation contact,
independent occupancy, visible guns, and page zoom. Placement is free; it does
not implement spending or combat. User art approval remains pending.
