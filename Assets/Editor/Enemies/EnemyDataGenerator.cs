#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Enemies > Create / Update README Enemy Data
/// Creates (or updates) one EnemyData asset per README enemy and boss, with the README numbers.
/// Asset names match the enemy AI's names (e.g. "Basic Goose 1"), so the AI recognises them.
/// Existing prefab / projectile references and tuning values are kept on update.
/// </summary>
public static class EnemyDataGenerator
{
    const string Folder = "Assets/Scriptable Objects/Enemies/README";

    struct Row
    {
        public string name; public float hp; public int money; public int fuel; public float damage;
        public EnemyAttackType attack; public EnemyMovement move; public bool shield; public bool stealth;
        public bool boss; public int cost; public float interval;
        public Row(string name, float hp, int money, int fuel, float damage, EnemyAttackType attack, EnemyMovement move,
                   bool shield, bool stealth, bool boss, int cost, float interval)
        {
            this.name = name; this.hp = hp; this.money = money; this.fuel = fuel; this.damage = damage; this.attack = attack;
            this.move = move; this.shield = shield; this.stealth = stealth; this.boss = boss; this.cost = cost; this.interval = interval;
        }
    }

    // README numbers. cost = enemy AI weight (HP + 2 x damage, +2 shield, +2 stealth); interval is not in the README.
    static readonly Row[] Rows =
    {
        new Row("Basic Goose 1",  1, 1, 1, 1, EnemyAttackType.Single, EnemyMovement.StopAndShoot,   false, false, false, 3,  1.5f),
        new Row("Basic Goose 2",  2, 1, 1, 1, EnemyAttackType.Single, EnemyMovement.StopAndShoot,   false, false, false, 4,  1.5f),
        new Row("Basic Goose 3",  4, 1, 1, 1, EnemyAttackType.Single, EnemyMovement.StopAndShoot,   false, false, false, 6,  1.5f),
        new Row("Shielded Goose", 4, 1, 1, 1, EnemyAttackType.Single, EnemyMovement.StopAndShoot,   true,  false, false, 8,  1.5f),
        new Row("Stealth Goose",  1, 1, 1, 1, EnemyAttackType.Single, EnemyMovement.StopAndShoot,   false, true,  false, 5,  1.5f),
        new Row("Basic Skunk 1",  2, 2, 2, 2, EnemyAttackType.AoE,    EnemyMovement.StopAndShoot,   false, false, false, 6,  2f),
        new Row("Basic Skunk 2",  4, 2, 2, 2, EnemyAttackType.AoE,    EnemyMovement.StopAndShoot,   false, false, false, 8,  2f),
        new Row("Basic Skunk 3",  8, 2, 2, 2, EnemyAttackType.AoE,    EnemyMovement.StopAndShoot,   false, false, false, 12, 2f),
        new Row("Shielded Skunk", 8, 2, 2, 2, EnemyAttackType.AoE,    EnemyMovement.StopAndShoot,   true,  false, false, 14, 2f),
        new Row("Basic Deer",    10, 4, 4, 5, EnemyAttackType.Single, EnemyMovement.OrbitAndCharge, false, false, false, 20, 4f),
        new Row("Shielded Deer", 10, 4, 4, 5, EnemyAttackType.Single, EnemyMovement.OrbitAndCharge, true,  false, false, 22, 4f),

        new Row("Healthy Goose",        50, 10, 10, 2, EnemyAttackType.Single, EnemyMovement.Orbit, false, false, true, 40, 2f),
        new Row("Healthy Skunk",        40, 10, 10, 3, EnemyAttackType.AoE,    EnemyMovement.Orbit, false, false, true, 40, 3f),
        new Row("Mother Goose",         40, 15, 15, 0, EnemyAttackType.None,   EnemyMovement.Orbit, false, false, true, 40, 8f),
        new Row("Mother Skunk",         35, 15, 15, 3, EnemyAttackType.AoE,    EnemyMovement.Orbit, false, false, true, 40, 4f),
        new Row("Restraining Buck",     50, 20, 20, 5, EnemyAttackType.Single, EnemyMovement.Orbit, false, false, true, 40, 4f),
        new Row("Sneaky Healthy Goose", 35, 10, 10, 2, EnemyAttackType.Single, EnemyMovement.Orbit, false, false, true, 40, 2f),
        new Row("Animal Ship",          25,  5,  5, 0, EnemyAttackType.None,   EnemyMovement.Orbit, false, false, true, 40, 1f),
        new Row("Endangered Deer",      60, 25, 25, 5, EnemyAttackType.Single, EnemyMovement.Orbit, false, false, true, 40, 4f),
    };

    [MenuItem("Tools/Enemies/Create or Update README Enemy Data")]
    public static void Generate()
    {
        Directory.CreateDirectory(Folder);
        int created = 0, updated = 0;
        foreach (Row r in Rows)
        {
            string path = Folder + "/" + r.name + ".asset";
            EnemyData d = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            bool isNew = d == null;
            if (isNew) d = ScriptableObject.CreateInstance<EnemyData>();

            d.cost = r.cost;
            d.isBoss = r.boss;
            d.maxHealth = r.hp;
            d.damage = r.damage;
            d.attackType = r.attack;
            d.moneyDrop = r.money;
            d.fuelDrop = r.fuel;
            d.hasShield = r.shield;
            d.isStealth = r.stealth;
            d.movement = r.move;
            d.orbitSpeed = 1f; // README: every boss has orbiting speed 1
            if (isNew) d.attackInterval = r.interval; // keep any tuning done in the Inspector

            if (isNew) { AssetDatabase.CreateAsset(d, path); created++; }
            else { EditorUtility.SetDirty(d); updated++; }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[EnemyDataGenerator] " + created + " created, " + updated + " updated in " + Folder + ". Assign a prefab to each.");
    }
}
#endif
