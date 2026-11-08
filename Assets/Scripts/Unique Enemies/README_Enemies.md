# Enemies

All 11 README enemies and 8 bosses, plus the spawner that uses the enemy AI.
Tested frame by frame without Unity: `Tools/EnemyBehaviourTest/run.sh` (24/24 pass, including a check that the enemy stats match the README).

Enemies move as kinematic **Rigidbody2D** bodies (`MovePosition` / `MoveRotation` in `FixedUpdate`), detect hits with **Collider2D** (`OnCollisionEnter2D` / `OnTriggerEnter2D`), and are reused through the level's single **`PoolManager`** (Unity's `ObjectPool<T>` inside) instead of `Instantiate` / `Destroy`.

```
GameEntity (Pooling/GameEntity.cs)      Rigidbody2D + Collider2D, Spawn/Despawn hooks
├── BaseEnemyData ── BasicEnemy (geese, skunks), DeerEnemy, bosses
├── BaseTowerData ── (towers: inherit GameEntity the same way)
└── EnemyProjectile
```

## Files
| File | What it does |
|---|---|
| `Pooling/GameEntity.cs` | Base class for enemies, towers and projectiles: Rigidbody2D setup, `OnSpawned` / `OnDespawned`, `Despawn()`, `SpawnId` |
| `Pooling/PoolManager.cs` | **The only pooling script in a level.** `Spawn(prefab, parent)`, `Despawn(entity)`, `Prewarm(prefab, n)`; one `ObjectPool` per prefab inside |
| `Data/EnemyData.cs` | README stats (HP, damage, attack type, drops, shield, stealth, movement) + tuning values + prefab references |
| `BaseEnemyData.cs` | Base for every enemy: approach, stop or orbit (Rigidbody2D), attack the slot below, collisions, shield, stealth, shiny, slow, drops, AI reporting, `Despawn()` back to the pool |
| `BasicEnemy.cs` | Geese and skunks (attack type comes from the data: geese single, skunks AoE) |
| `DeerEnemy.cs` | Deer: orbit, periodically stop and charge the planet, return |
| `Bosses/BossEnemy.cs` | Healthy Goose, Healthy Skunk (orbit + attack) and base for the others |
| `Bosses/MotherGooseBoss.cs` | No attack; calls flocks of Basic Goose 3 |
| `Bosses/MotherSkunkBoss.cs` | n AoE bombs per volley |
| `Bosses/RestrainingBuckBoss.cs` | Horns: 5 damage + freeze the closest tower for n seconds |
| `Bosses/SneakyHealthyGooseBoss.cs` | Every n seconds: stealth, heal HP/s, no attacks |
| `Bosses/AnimalShipBoss.cs` | No attack; releases 2 Healthy Skunk, 2 Healthy Goose, 5 Shielded Goose on death |
| `Bosses/EndangeredDeerBoss.cs` | Spawns a Shielded Deer every n seconds; can't be hurt while one lives |
| `EnemyProjectile.cs` | Optional visual projectile (GameEntity, Rigidbody2D, pooled); without one, hits are instant |
| `EnemySpawner.cs` | Picks enemies with the AI, spawns the halfway boss, raises boss warning / time-up. Gets instances from the PoolManager (owns no pools) |
| `PlanetDefense.cs` | `IDamageable`, `IFreezable`, `IPlanetDefense`, `IEnemyHitter`: what enemies need from the planet and towers |
| `SimplePlanetDefense.cs` | A working planet: N rotating slots, planet HP, tower registration |
| `EnemyEvents.cs` | `Killed` (with money/fuel), `Spawned`, `BossWarning`, `BossSpawned`, `LevelTimeUp` |
| `Editor/Enemies/EnemyDataGenerator.cs` | **Tools > Enemies > Create or Update README Enemy Data** |

## Setup in a level scene
1. Run **Tools > Enemies > Create or Update README Enemy Data**. It creates 19 EnemyData assets in `Assets/Scriptable Objects/Enemies/README/` with the README numbers. The names ("Basic Goose 1", ...) are what the enemy AI recognises, so don't rename them.
2. Make a prefab per enemy type with the right component (`BasicEnemy`, `DeerEnemy`, or the boss class), a sprite/image and a **Collider2D** (e.g. CircleCollider2D). A **Rigidbody2D** is added automatically and set to Kinematic in code. Drag the prefab into the asset's **prefab** field. For bosses, fill the boss fields (e.g. Mother Goose → flock enemy = Basic Goose 3; Animal Ship → cargo; Endangered Deer → guard = Shielded Deer).
3. In the scene: add one empty GameObject with **`PoolManager`** (exactly one per level), `SimplePlanetDefense` on the planet (set radius in the planet's local units, e.g. UI pixels, and the slot count), `WaveDirectorBridge`, and `EnemySpawner` (assign LevelData, bridge, planet defense).
4. Fill the LevelData roster with these assets and set the level's boss with `isLevelBoss`.

Enemies are parented under the planet's `Root` and placed by angle + distance, so it works on a UI canvas (RectTransform prefabs) or in world space.

## What towers / economy need to do
```csharp
// towers register in their slot (implement IDamageable; IFreezable too if they can be frozen)
planetDefense.RegisterTower(slot, TowerKind.Laser, this);
// tower bullets: a Collider2D (trigger or not) + a component implementing IEnemyHitter.
// The enemy's OnCollisionEnter2D / OnTriggerEnter2D applies Damage (shield, stealth and invulnerability rules included)
// and calls OnHitEnemy so the bullet can return to its own pool.
public class Bullet : MonoBehaviour, IEnemyHitter
{
    public float Damage => damage;
    public bool IgnoresShield => false;      // true for the Pest Trap
    public bool CanHitStealth => false;      // true for Radar
    public void OnHitEnemy(BaseEnemyData e) { /* release to the bullet pool */ }
}
// or direct targeting without physics
foreach (var e in BaseEnemyData.Active)
    if (e.IsTargetable(canSeeStealth)) { /* in FOV / range? then */ e.TakeDamage(dmg); }
// Pest Trap
e.TakeDamageIgnoringShield(1f * Time.deltaTime); e.ApplySlow(3f, 0.5f);
// economy
EnemyEvents.Killed += (enemy, money, fuel) => { /* add money and fuel */ };
planetDefense.SetFuelPerSecond(netFuelPerSecond); // lets the AI see a fuel deficit
```

## Pooling (enemies and towers)
```csharp
// towers: inherit GameEntity, reset state in OnSpawned
public class BaseTowerData : GameEntity
{
    protected override void OnSpawned()   { /* reset HP, cooldowns */ }
    protected override void OnDespawned() { /* unregister from the planet */ }
}
// placing / selling a tower
BaseTowerData t = PoolManager.Instance.Spawn(laserPrefab, slotTransform);
t.Despawn();
```
- Never `Destroy` an entity: call `Despawn()` (for enemies: no drops) or let an enemy die (`TakeDamage`). Both return it to the PoolManager.
- `spawner.DespawnAll()` clears the enemies at level end.
- A pooled instance is reused for a new object, so don't keep references to despawned ones. If you must, store `SpawnId` and compare it later (it changes on every reuse).
- Pool sizes are on the `PoolManager` (`defaultCapacity` 20, `maxSizePerPrefab` 200); `EnemySpawner.prewarmPerEnemy` (5) asks it to create enemies up front.
- `GameEntity` sets the Rigidbody2D to kinematic with no gravity. Override `ConfigureBody` if a tower needs something else.

## Not in the README (Inspector values, change freely)
- Movement: approach speed 0.5 planet radii/s, stop at 1.8 radii, orbit at 1.6 radii, 15°/s × README orbit speed.
- Attack timing: geese 1.5s, skunks 2s, deer charge every 4s, bosses 2–8s.
- AoE: hits every slot within 30° of the impact.
- Boss "n" values: Mother Goose 4 geese / 8s, Mother Skunk 3 bombs, Restraining Buck freeze 3s, Sneaky Healthy Goose stealth every 10s for 4s healing 2 HP/s, Endangered Deer guard every 12s.
- Shiny: 5% (bosses 1%), double HP and double drops, rolled by the AI / spawner.

## Tests (`Tools/EnemyBehaviourTest`, no Unity needed)
Generator matches README (19/19 rows) · goose stops and shoots the slot below · empty slot hits the planet · skunk AoE hits both neighbours · shield blocks only the first hit · stealth only visible to radar · shiny doubles HP and drops · DoT ignores the shield · deer orbit + charge for 5 · damage and death reach the AI's learner · all 8 boss abilities · a full 120s level through the spawner and the AI (no exceptions, boss + warning + time-up once, budget respected) · one PoolManager serving enemies and a tower-like GameEntity, spawner owning no pools · pooling (31 spawns served by 21 instances, killed enemies come back fresh, double despawn is safe, a reused guard isn't counted as the boss's old guard) · movement through a kinematic Rigidbody2D with the transform untouched · trigger and collision hits with `IEnemyHitter` (stealth, shield-ignoring DoT) · pooled projectile hits on impact.
