# Enemies

All 11 README enemies and 8 bosses, plus the spawner that uses the enemy AI.
Tested frame by frame without Unity: `Tools/EnemyBehaviourTest/run.sh` (18/18 pass, including a check that the enemy stats match the README).

## Files
| File | What it does |
|---|---|
| `Data/EnemyData.cs` | README stats (HP, damage, attack type, drops, shield, stealth, movement) + tuning values + prefab references |
| `BaseEnemyData.cs` | Base for every enemy: approach, stop or orbit, attack the slot below, shield, stealth, shiny, slow, drops, AI reporting |
| `BasicEnemy.cs` | Geese and skunks (attack type comes from the data: geese single, skunks AoE) |
| `DeerEnemy.cs` | Deer: orbit, periodically stop and charge the planet, return |
| `Bosses/BossEnemy.cs` | Healthy Goose, Healthy Skunk (orbit + attack) and base for the others |
| `Bosses/MotherGooseBoss.cs` | No attack; calls flocks of Basic Goose 3 |
| `Bosses/MotherSkunkBoss.cs` | n AoE bombs per volley |
| `Bosses/RestrainingBuckBoss.cs` | Horns: 5 damage + freeze the closest tower for n seconds |
| `Bosses/SneakyHealthyGooseBoss.cs` | Every n seconds: stealth, heal HP/s, no attacks |
| `Bosses/AnimalShipBoss.cs` | No attack; releases 2 Healthy Skunk, 2 Healthy Goose, 5 Shielded Goose on death |
| `Bosses/EndangeredDeerBoss.cs` | Spawns a Shielded Deer every n seconds; can't be hurt while one lives |
| `EnemyProjectile.cs` | Optional visual projectile; without one, hits are instant |
| `EnemySpawner.cs` | Picks enemies with the AI, spawns the halfway boss, raises boss warning / time-up |
| `PlanetDefense.cs` | `IDamageable`, `IFreezable`, `IPlanetDefense`: what enemies need from the planet and towers |
| `SimplePlanetDefense.cs` | A working planet: N rotating slots, planet HP, tower registration |
| `EnemyEvents.cs` | `Killed` (with money/fuel), `Spawned`, `BossWarning`, `BossSpawned`, `LevelTimeUp` |
| `Editor/Enemies/EnemyDataGenerator.cs` | **Tools > Enemies > Create or Update README Enemy Data** |

## Setup in a level scene
1. Run **Tools > Enemies > Create or Update README Enemy Data**. It creates 19 EnemyData assets in `Assets/Scriptable Objects/Enemies/README/` with the README numbers. The names ("Basic Goose 1", ...) are what the enemy AI recognises, so don't rename them.
2. Make a prefab per enemy type with the right component (`BasicEnemy`, `DeerEnemy`, or the boss class) and a sprite/image, and drag it into the asset's **prefab** field. For bosses, fill the boss fields (e.g. Mother Goose → flock enemy = Basic Goose 3; Animal Ship → cargo; Endangered Deer → guard = Shielded Deer).
3. In the scene: add `SimplePlanetDefense` on the planet (set radius in the planet's local units, e.g. UI pixels, and the slot count), `WaveDirectorBridge`, and `EnemySpawner` (assign LevelData, bridge, planet defense).
4. Fill the LevelData roster with these assets and set the level's boss with `isLevelBoss`.

Enemies are parented under the planet's `Root` and placed by angle + distance, so it works on a UI canvas (RectTransform prefabs) or in world space.

## What towers / economy need to do
```csharp
// towers register in their slot (implement IDamageable; IFreezable too if they can be frozen)
planetDefense.RegisterTower(slot, TowerKind.Laser, this);
// tower targeting
foreach (var e in BaseEnemyData.Active)
    if (e.IsTargetable(canSeeStealth)) { /* in FOV / range? then */ e.TakeDamage(dmg); }
// Pest Trap
e.TakeDamageIgnoringShield(1f * Time.deltaTime); e.ApplySlow(3f, 0.5f);
// economy
EnemyEvents.Killed += (enemy, money, fuel) => { /* add money and fuel */ };
planetDefense.SetFuelPerSecond(netFuelPerSecond); // lets the AI see a fuel deficit
```

## Not in the README (Inspector values, change freely)
- Movement: approach speed 0.5 planet radii/s, stop at 1.8 radii, orbit at 1.6 radii, 15°/s × README orbit speed.
- Attack timing: geese 1.5s, skunks 2s, deer charge every 4s, bosses 2–8s.
- AoE: hits every slot within 30° of the impact.
- Boss "n" values: Mother Goose 4 geese / 8s, Mother Skunk 3 bombs, Restraining Buck freeze 3s, Sneaky Healthy Goose stealth every 10s for 4s healing 2 HP/s, Endangered Deer guard every 12s.
- Shiny: 5% (bosses 1%), double HP and double drops, rolled by the AI / spawner.

## Tests (`Tools/EnemyBehaviourTest`, no Unity needed)
Generator matches README (19/19 rows) · goose stops and shoots the slot below · empty slot hits the planet · skunk AoE hits both neighbours · shield blocks only the first hit · stealth only visible to radar · shiny doubles HP and drops · DoT ignores the shield · deer orbit + charge for 5 · damage and death reach the AI's learner · all 8 boss abilities · a full 120s level through the spawner and the AI (no exceptions, boss + warning + time-up once, budget respected) · projectile hits on impact.
