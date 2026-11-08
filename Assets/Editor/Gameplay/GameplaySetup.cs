#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EnemyAI;
using Object = UnityEngine.Object;

/// <summary>
/// Tools > Gameplay > 1. Build All Prefabs and Tower Data
///   Does the "projectile prefab / tower prefab / TowerData" steps for every README tower and enemy:
///   prefab root = Rigidbody2D + trigger CircleCollider2D + the behaviour script, with the art as a child
///   (existing art prefabs/sprites where they exist, a coloured placeholder circle otherwise).
///   Then fills EnemyData.prefab / projectilePrefab and TowerData.prefab / projectilePrefab.
///   Prefabs that already exist are kept (so art swaps by hand survive); delete one to rebuild it.
///
/// Tools > Gameplay > 2. Add Gameplay To Open Level Scene
///   Adds PoolManager, LevelPlanetDefense, WaveDirectorBridge, EnemySpawner and TowerPlacementInput to the open
///   level scene, wires them to its LevelManager and gives it a LevelData if it has none. The HUD canvas stays
///   Screen Space - Overlay (sharp text, menus above the action); its plain full-screen Background image is hidden and
///   the camera clears to the same colour, so the enemy / tower sprites show between the background and the planet / HUD.
/// </summary>
public static class GameplaySetup
{
    const string EnemyPrefabs = "Assets/Prefabs/Gameplay/Enemies";
    const string TowerPrefabs = "Assets/Prefabs/Gameplay/Towers";
    const string ProjectilePrefabs = "Assets/Prefabs/Gameplay/Projectiles";
    const string EffectPrefabs = "Assets/Prefabs/Gameplay/Effects";
    const string ExplosionPath = EffectPrefabs + "/Tower Explosion.prefab";
    const string TowerDataFolder = "Assets/Scriptable Objects/Towers/README";
    const string EnemyDataFolder = "Assets/Scriptable Objects/Enemies/README";
    const string LevelDataFolder = "Assets/Scriptable Objects/Levels";
    const string PlaceholderPath = "Assets/Art/Placeholders/Circle.png";
    const int SortingOrder = 10; // above the level canvas (which is set to sorting order 0)

    // ------------------------------------------------------------------ README tower table
    struct TowerRow
    {
        public string name; public float hp; public int cost; public int fuel; public float fov; public float damage; public float rate;
        public TowerAttackType attack; public TowerKind kind; public Type script; public bool ignoresShield; public bool seesStealth;
        public string art; public Color color; public string projectile;
    }

    static TowerRow Row(string name, float hp, int cost, int fuel, float fov, float damage, float rate, TowerAttackType attack, TowerKind kind, Type script,
                      string art, Color color, string projectile, bool ignoresShield = false, bool seesStealth = false)
    {
        return new TowerRow { name = name, hp = hp, cost = cost, fuel = fuel, fov = fov, damage = damage, rate = rate, attack = attack, kind = kind, script = script,
            art = art, color = color, projectile = projectile, ignoresShield = ignoresShield, seesStealth = seesStealth };
    }

    // rate = seconds between shots (not in the README). art: a prefab or sprite path, or null for a placeholder.
    static readonly TowerRow[] Towers =
    {
        Row("Laser",       25,  1, -1, 120, 2, 1.0f, TowerAttackType.Single, TowerKind.Laser,      typeof(ShootingTower),   "Assets/Art/Planets/UnionPrime/LaserBase.png", Color.white, "Laser Bolt"),
        Row("Bomb",        25,  2, -1, 120, 1, 1.5f, TowerAttackType.AoE,    TowerKind.Bomb,       typeof(AoETower),        "Assets/Prefabs/Towers/BombTowerVisual.prefab", Color.white, "Bomb Shell"),
        Row("Sniper",      20,  2, -1, 120, 5, 2.5f, TowerAttackType.Single, TowerKind.Sniper,     typeof(ShootingTower),   null, new Color(0.55f, 0.75f, 1f), "Laser Bolt"),
        Row("Pest Trap",   20,  2, -2, 120, 1, 2.0f, TowerAttackType.Dot,    TowerKind.PestTrap,   typeof(PestTrapTower),   null, new Color(0.5f, 0.85f, 0.3f), "Sticky Goo", ignoresShield: true),
        Row("Radar",       10,  5, -1,  90, 0, 0.5f, TowerAttackType.None,   TowerKind.Radar,      typeof(RadarTower),      null, new Color(0.3f, 0.9f, 0.9f), null, seesStealth: true),
        Row("Oil Rig",     15,  5,  3,   0, 0, 1.0f, TowerAttackType.None,   TowerKind.OilRig,     typeof(ResourceTower),   null, new Color(0.25f, 0.25f, 0.25f), null),
        Row("Bank",        15,  5, -1,   0, 0, 1.0f, TowerAttackType.None,   TowerKind.Bank,       typeof(ResourceTower),   null, new Color(1f, 0.85f, 0.2f), null),
        Row("Laser SMG",   30, 10, -3, 120, 2, 0.4f, TowerAttackType.Single, TowerKind.LaserSMG,   typeof(ShootingTower),   "Assets/Art/Planets/UnionPrime/LaserBase.png", new Color(1f, 0.7f, 0.7f), "Laser Bolt"),
        Row("Tesla Coil",  25, 15, -3, 120, 2, 1.2f, TowerAttackType.AoE,    TowerKind.TeslaCoil,  typeof(AoETower),        null, new Color(0.6f, 0.5f, 1f), "Spark"),
        Row("Gamma Radar", 20, 20, -5, 120, 1, 1.0f, TowerAttackType.AoE,    TowerKind.GammaRadar, typeof(RadarTower),      null, new Color(0.2f, 1f, 0.5f), null, seesStealth: true),
        Row("Ballistica",  30, 25, -6, 120, 0, 2.5f, TowerAttackType.Single, TowerKind.Ballistica, typeof(BallisticaTower), null, new Color(0.8f, 0.5f, 0.2f), "Laser Bolt"),
    };

    // ------------------------------------------------------------------ enemies (names = EnemyDataGenerator / README)
    struct EnemyArt { public Type script; public string art; public Color tint; public float size; public string projectile; }

    static EnemyArt A(Type script, string art, Color tint, float size, string projectile)
    {
        return new EnemyArt { script = script, art = art, tint = tint, size = size, projectile = projectile };
    }

    const string Goose = "Assets/Prefabs/Enemies/BasicGoose1Visual.prefab";
    const string Skunk = "Assets/Prefabs/Enemies/BasicSkunk1Visual.prefab";
    static readonly Color Shield = new Color(0.6f, 0.8f, 1f);
    static readonly Color Deer = new Color(0.6f, 0.4f, 0.2f);

    static readonly Dictionary<string, EnemyArt> Enemies = new Dictionary<string, EnemyArt>
    {
        { "Basic Goose 1",  A(typeof(BasicEnemy), Goose, Color.white, 0.5f, "Goose Poop") },
        { "Basic Goose 2",  A(typeof(BasicEnemy), Goose, new Color(1f, 0.95f, 0.85f), 0.55f, "Goose Poop") },
        { "Basic Goose 3",  A(typeof(BasicEnemy), Goose, new Color(1f, 0.9f, 0.7f), 0.6f, "Goose Poop") },
        { "Shielded Goose", A(typeof(BasicEnemy), Goose, Shield, 0.6f, "Goose Poop") },
        { "Stealth Goose",  A(typeof(BasicEnemy), Goose, new Color(1f, 1f, 1f, 0.45f), 0.5f, "Goose Poop") },
        { "Basic Skunk 1",  A(typeof(BasicEnemy), Skunk, Color.white, 0.5f, "Skunk Bomb") },
        { "Basic Skunk 2",  A(typeof(BasicEnemy), Skunk, new Color(0.95f, 0.9f, 1f), 0.55f, "Skunk Bomb") },
        { "Basic Skunk 3",  A(typeof(BasicEnemy), Skunk, new Color(0.85f, 0.8f, 1f), 0.6f, "Skunk Bomb") },
        { "Shielded Skunk", A(typeof(BasicEnemy), Skunk, Shield, 0.6f, "Skunk Bomb") },
        { "Basic Deer",     A(typeof(DeerEnemy), null, Deer, 0.6f, null) },
        { "Shielded Deer",  A(typeof(DeerEnemy), null, Color.Lerp(Deer, Shield, 0.5f), 0.6f, null) },
        { "Healthy Goose",        A(typeof(BossEnemy),              Goose, new Color(1f, 0.8f, 0.8f), 1.1f, "Goose Poop") },
        { "Healthy Skunk",        A(typeof(BossEnemy),              Skunk, new Color(1f, 0.8f, 0.8f), 1.1f, "Skunk Bomb") },
        { "Mother Goose",         A(typeof(MotherGooseBoss),        Goose, new Color(1f, 0.85f, 1f), 1.3f, null) },
        { "Mother Skunk",         A(typeof(MotherSkunkBoss),        Skunk, new Color(1f, 0.85f, 1f), 1.3f, "Skunk Bomb") },
        { "Restraining Buck",     A(typeof(RestrainingBuckBoss),    null, new Color(0.5f, 0.3f, 0.15f), 1.2f, null) },
        { "Sneaky Healthy Goose", A(typeof(SneakyHealthyGooseBoss), Goose, new Color(0.8f, 0.8f, 1f, 0.8f), 1.1f, "Goose Poop") },
        { "Animal Ship",          A(typeof(AnimalShipBoss),         null, new Color(0.7f, 0.7f, 0.8f), 1.4f, null) },
        { "Endangered Deer",      A(typeof(EndangeredDeerBoss),     null, new Color(0.9f, 0.6f, 0.3f), 1.3f, null) },
    };

    // projectile name -> art (prefab / sprite path or null) and placeholder colour
    static readonly Dictionary<string, KeyValuePair<string, Color>> ProjectileArt = new Dictionary<string, KeyValuePair<string, Color>>
    {
        { "Goose Poop", new KeyValuePair<string, Color>("Assets/Prefabs/Projectiles/GoosePoopProjectileVisual.prefab", new Color(0.5f, 0.4f, 0.2f)) },
        { "Skunk Bomb", new KeyValuePair<string, Color>("Assets/Prefabs/Projectiles/SkunkBombProjectileVisual.prefab", new Color(0.3f, 0.6f, 0.2f)) },
        { "Laser Bolt", new KeyValuePair<string, Color>("Assets/Prefabs/Projectiles/RabbitLaserFlightVisual.prefab", new Color(1f, 0.3f, 0.3f)) },
        { "Bomb Shell", new KeyValuePair<string, Color>(null, new Color(0.15f, 0.15f, 0.15f)) },
        { "Sticky Goo", new KeyValuePair<string, Color>(null, new Color(0.5f, 0.9f, 0.3f)) },
        { "Spark",      new KeyValuePair<string, Color>(null, new Color(0.7f, 0.6f, 1f)) },
    };

    // ================================================================== 1. prefabs and data

    [MenuItem("Tools/Gameplay/1. Build All Prefabs and Tower Data")]
    public static void BuildAll()
    {
        EnemyDataGenerator.Generate(); // make sure the 19 README EnemyData assets exist
        Sprite placeholder = PlaceholderSprite();
        foreach (string f in new[] { EnemyPrefabs, TowerPrefabs, ProjectilePrefabs, EffectPrefabs, TowerDataFolder }) Directory.CreateDirectory(f);
        AssetDatabase.Refresh(); // let Unity see the new folders before saving into them

        int made = 0, kept = 0;

        // projectiles
        var enemyShots = new Dictionary<string, EnemyProjectile>();
        var towerShots = new Dictionary<string, TowerProjectile>();
        foreach (var kv in ProjectileArt)
        {
            bool enemySide = kv.Key == "Goose Poop" || kv.Key == "Skunk Bomb";
            string path = ProjectilePrefabs + "/" + kv.Key + ".prefab";
            bool existed = File.Exists(path);
            GameObject go = existed ? AssetDatabase.LoadAssetAtPath<GameObject>(path)
                : Build(path, kv.Key, enemySide ? typeof(EnemyProjectile) : typeof(TowerProjectile), kv.Value.Key, kv.Value.Value, Color.white, 0.25f, placeholder, null);
            if (existed) kept++; else made++;
            if (enemySide) enemyShots[kv.Key] = go.GetComponent<EnemyProjectile>();
            else towerShots[kv.Key] = go.GetComponent<TowerProjectile>();
        }

        // Davis Nova tower explosion (pooled effect)
        if (!File.Exists(ExplosionPath))
        {
            Build(ExplosionPath, "Tower Explosion", typeof(PooledEffect), null, Color.white, new Color(1f, 0.55f, 0.15f, 0.85f), 1.2f, placeholder, null);
            made++;
        }
        else kept++;

        // enemies
        var data = new Dictionary<string, EnemyData>();
        foreach (string name in Enemies.Keys)
        {
            EnemyData d = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataFolder + "/" + name + ".asset");
            if (d != null) data[name] = d;
            else Debug.LogWarning("[GameplaySetup] Missing EnemyData " + name);
        }
        foreach (var kv in Enemies)
        {
            EnemyData d;
            if (!data.TryGetValue(kv.Key, out d)) continue;
            EnemyArt art = kv.Value;
            string path = EnemyPrefabs + "/" + kv.Key + ".prefab";
            bool existed = File.Exists(path);
            GameObject go = existed ? AssetDatabase.LoadAssetAtPath<GameObject>(path)
                : Build(path, kv.Key, art.script, art.art, art.tint, art.tint, art.size, placeholder, root => ConfigureBoss(root, data));
            if (existed) kept++; else made++;

            d.prefab = go.GetComponent<BaseEnemyData>();
            if (d.projectilePrefab == null && art.projectile != null && enemyShots.ContainsKey(art.projectile)) d.projectilePrefab = enemyShots[art.projectile];
            EditorUtility.SetDirty(d);
        }

        // towers + TowerData
        foreach (TowerRow r in Towers)
        {
            string path = TowerPrefabs + "/" + r.name + ".prefab";
            bool existed = File.Exists(path);
            TowerRow row = r;
            GameObject go = existed ? AssetDatabase.LoadAssetAtPath<GameObject>(path)
                : Build(path, r.name, r.script, r.art, r.color, r.color, 0.6f, placeholder, root => ConfigureTower(root, row));
            if (existed) kept++; else made++;

            string dataPath = TowerDataFolder + "/" + r.name + ".asset";
            TowerData t = AssetDatabase.LoadAssetAtPath<TowerData>(dataPath);
            bool isNew = t == null;
            if (isNew) t = ScriptableObject.CreateInstance<TowerData>();
            t.maxHealth = r.hp;
            t.cost = r.cost;
            t.fuelIntake = r.fuel;
            t.fov = r.fov;
            t.damage = r.damage;
            if (isNew) t.fireRate = r.rate; // keep Inspector tuning
            t.attackType = r.attack;
            t.kind = r.kind;
            t.ignoresShield = r.ignoresShield;
            t.canHitStealth = r.seesStealth;
            t.prefab = go.GetComponent<BaseTowerData>();
            if (t.projectilePrefab == null)
                t.projectilePrefab = r.projectile != null && towerShots.ContainsKey(r.projectile) ? towerShots[r.projectile] : null;
            if (isNew) AssetDatabase.CreateAsset(t, dataPath);
            else EditorUtility.SetDirty(t);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[GameplaySetup] Prefabs: " + made + " created, " + kept + " kept. EnemyData and TowerData (" + TowerDataFolder + ") are wired. Next: open a level scene and run Tools > Gameplay > 2.");
    }

    static void ConfigureBoss(GameObject root, Dictionary<string, EnemyData> data)
    {
        EnemyData d;
        var mother = root.GetComponent<MotherGooseBoss>();
        if (mother != null && data.TryGetValue("Basic Goose 3", out d)) mother.flockEnemy = d;
        var endangered = root.GetComponent<EndangeredDeerBoss>();
        if (endangered != null && data.TryGetValue("Shielded Deer", out d)) endangered.guardEnemy = d;
        var ship = root.GetComponent<AnimalShipBoss>();
        if (ship != null)
        {
            var cargo = new List<AnimalShipBoss.Cargo>();
            if (data.TryGetValue("Healthy Skunk", out d)) cargo.Add(new AnimalShipBoss.Cargo { enemy = d, count = 2 });
            if (data.TryGetValue("Healthy Goose", out d)) cargo.Add(new AnimalShipBoss.Cargo { enemy = d, count = 2 });
            if (data.TryGetValue("Shielded Goose", out d)) cargo.Add(new AnimalShipBoss.Cargo { enemy = d, count = 5 });
            ship.cargo = cargo.ToArray();
        }
    }

    static void ConfigureTower(GameObject root, TowerRow r)
    {
        var aoe = root.GetComponent<AoETower>();
        if (aoe != null) aoe.effectRadius = 0.8f;
        var radar = root.GetComponent<RadarTower>();
        if (radar != null) radar.isGammaRadar = r.kind == TowerKind.GammaRadar;
        var res = root.GetComponent<ResourceTower>();
        if (res != null)
        {
            if (r.kind == TowerKind.Bank) { res.resourceType = ResourceTower.ResourceType.Money; res.generationAmount = 3; } // README $3/s
            else { res.resourceType = ResourceTower.ResourceType.Fuel; res.generationAmount = 0; }                         // Oil Rig: +3 F/s is its fuelIntake
        }
    }

    /// <summary>Root: Rigidbody2D + trigger CircleCollider2D + script. Child "Art": art prefab, sprite, or placeholder circle, scaled to size.</summary>
    static GameObject Build(string path, string name, Type script, string art, Color tint, Color placeholderColor, float size, Sprite placeholder, Action<GameObject> configure)
    {
        var root = new GameObject(name);
        var rb = root.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        var col = root.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = size * 0.45f;
        root.AddComponent(script);

        GameObject visual = null;
        bool isPlaceholder = false;
        if (!string.IsNullOrEmpty(art) && art.EndsWith(".prefab"))
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(art);
            if (prefab != null) visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }
        else if (!string.IsNullOrEmpty(art))
        {
            Sprite s = LoadSprite(art);
            if (s != null) { visual = new GameObject("Art"); visual.AddComponent<SpriteRenderer>().sprite = s; }
        }
        if (visual == null)
        {
            visual = new GameObject("Art (placeholder)");
            visual.AddComponent<SpriteRenderer>().sprite = placeholder;
            isPlaceholder = true;
        }
        visual.transform.SetParent(root.transform, false);
        visual.transform.localPosition = Vector3.zero;

        var renderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            sr.sortingOrder += SortingOrder;
            sr.color = isPlaceholder ? placeholderColor : sr.color * tint;
        }
        // scale the art so its biggest side is `size` world units
        float biggest = 0f;
        foreach (var sr in renderers)
            if (sr.sprite != null) biggest = Mathf.Max(biggest, Mathf.Max(sr.bounds.size.x, sr.bounds.size.y));
        if (biggest > 0.0001f) visual.transform.localScale *= size / biggest;

        if (configure != null) configure(root);
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return saved;
    }

    static Sprite LoadSprite(string path)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite) return (Sprite)o;
        return null;
    }

    static Sprite PlaceholderSprite()
    {
        if (!File.Exists(PlaceholderPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PlaceholderPath));
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x - n / 2f + 0.5f, dy = y - n / 2f + 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(n / 2f - d);           // soft edge
                    float rim = d > n / 2f - 5f ? 0.65f : 1f;       // darker rim so it reads as a token
                    tex.SetPixel(x, y, new Color(rim, rim, rim, a));
                }
            tex.Apply();
            File.WriteAllBytes(PlaceholderPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(PlaceholderPath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(PlaceholderPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = n; // 1 world unit across
            importer.SaveAndReimport();
        }
        return LoadSprite(PlaceholderPath);
    }

    // ================================================================== 2. scene

    // README planet sizes (slots) and level numbers come from the PlanetPresentation assets; these are the fallbacks.
    static readonly Dictionary<string, int> SlotsByScene = new Dictionary<string, int>
    {
        { "UnionPrime", 12 }, { "CapenMajor", 16 }, { "DavisNova", 12 }, { "SilvermanLuna", 12 }, { "C3", 18 }, { "CommonsMinor", 8 }, { "BairdReach", 8 },
    };
    static readonly string[] BossByLevel = { "Healthy Goose", "Mother Goose", "Mother Skunk", "Restraining Buck", "Sneaky Healthy Goose", "Animal Ship", "Endangered Deer" };
    // enemies that join the roster at each level (tutorial = 0)
    static readonly string[][] NewEnemiesByLevel =
    {
        new[] { "Basic Goose 1", "Basic Goose 2", "Basic Skunk 1" },
        new[] { "Basic Goose 3", "Shielded Goose" },
        new[] { "Basic Skunk 2", "Stealth Goose" },
        new[] { "Basic Skunk 3", "Basic Deer" },
        new[] { "Shielded Skunk" },
        new[] { "Shielded Deer" },
        new string[0],
    };

    [MenuItem("Tools/Gameplay/2. Add Gameplay To Open Level Scene")]
    public static void SetupScene()
    {
        if (Object.FindAnyObjectByType<GameplayLoopController>() != null)
        {
            Debug.Log("[GameplaySetup] This scene uses a configured gameplay loop. Edit its configuration; do not apply legacy scene setup.");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        LevelManager lm = Object.FindAnyObjectByType<LevelManager>();
        if (lm == null)
        {
            EditorUtility.DisplayDialog("Gameplay setup", "No LevelManager in '" + scene.name + "'. Open a level scene that has the Level Manager prefab (UnionPrime, CapenMajor, ...).", "OK");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<TowerData>(TowerDataFolder + "/Laser.asset") == null) BuildAll();

        int level = PlanetLevel(scene.name);
        var so = new SerializedObject(lm);

        // planet body: the Rigidbody2D + CircleCollider2D on the planet visual
        if (lm.planetBody == null)
        {
            foreach (var c in Object.FindObjectsByType<CircleCollider2D>(FindObjectsSortMode.None))
            {
                var rb = c.GetComponent<Rigidbody2D>();
                if (rb != null && c.GetComponentInParent<Canvas>() != null) { so.FindProperty("planetBody").objectReferenceValue = rb; break; }
            }
        }

        // LevelData
        LevelData data = (LevelData)so.FindProperty("levelData").objectReferenceValue;
        if (data == null)
        {
            Directory.CreateDirectory(LevelDataFolder);
            AssetDatabase.Refresh();
            string path = LevelDataFolder + "/" + scene.name + ".asset";
            data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            if (data == null) { data = ScriptableObject.CreateInstance<LevelData>(); AssetDatabase.CreateAsset(data, path); }
            so.FindProperty("levelData").objectReferenceValue = data;
        }
        FillLevelData(data, level);
        so.ApplyModifiedProperties();

        // Canvas: keep the HUD on Screen Space - Overlay (an earlier version of this menu switched it to Camera,
        // which blurred the text and drew enemies over the tower menu: put it back).
        // Sprites are drawn by the camera *under* an overlay canvas, so hide the HUD's opaque full-screen
        // "Background" image and let the camera clear to the same colour instead.
        Camera cam = Camera.main;
        foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceCamera) continue;
            Undo.RecordObject(canvas, "Canvas back to overlay");
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        foreach (var img in Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None))
        {
            if (img.gameObject.name != "Background" || img.sprite != null) continue;
            RectTransform rt = img.rectTransform;
            if (rt.anchorMin != Vector2.zero || rt.anchorMax != Vector2.one) continue; // only the full-screen plain one
            Undo.RecordObject(img, "Hide HUD background");
            img.enabled = false;
            if (cam != null)
            {
                Undo.RecordObject(cam, "Camera background");
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = img.color;
            }
        }

        // gameplay objects
        GameObject root = GameObject.Find("Gameplay");
        if (root == null) { root = new GameObject("Gameplay"); Undo.RegisterCreatedObjectUndo(root, "Gameplay"); }
        var pool = GetOrAdd<PoolManager>(root);
        var defense = GetOrAdd<LevelPlanetDefense>(root);
        var bridge = GetOrAdd<WaveDirectorBridge>(root);
        var spawner = GetOrAdd<EnemySpawner>(root);
        var input = GetOrAdd<TowerPlacementInput>(root);

        int slots;
        defense.levelManager = lm;
        defense.spawner = spawner;
        defense.slots = SlotsByScene.TryGetValue(scene.name, out slots) ? slots : 12;
        bridge.level = data;
        spawner.level = data;
        spawner.bridge = bridge;
        spawner.planetDefense = defense;
        spawner.pool = pool;
        spawner.spawnDistance = 4f;
        input.defense = defense;
        var towers = new List<TowerData>();
        foreach (TowerRow r in Towers)
        {
            var t = AssetDatabase.LoadAssetAtPath<TowerData>(TowerDataFolder + "/" + r.name + ".asset");
            if (t != null) towers.Add(t);
        }
        input.towers = towers.ToArray();
        input.cam = cam;
        foreach (var c in new Component[] { pool, defense, bridge, spawner, input }) EditorUtility.SetDirty(c);

        // planet rules from the README
        string extra = "";
        if (scene.name == "DavisNova")
        {
            if (!File.Exists(ExplosionPath)) BuildAll();
            var life = GetOrAdd<TowerLifespan>(root);
            if (life.explosionEffect == null)
            {
                var fx = AssetDatabase.LoadAssetAtPath<GameObject>(ExplosionPath);
                if (fx != null) life.explosionEffect = fx.GetComponent<PooledEffect>();
            }
            EditorUtility.SetDirty(life);
            extra = " + TowerLifespan (towers explode after " + life.lifespanSeconds + "s)";
        }
        if (scene.name == "SilvermanLuna")
        {
            var moon = GetOrAdd<MoonOrbit>(root);
            moon.planet = defense;
            if (moon.moon == null) moon.moon = FindMoon(lm);
            defense.moon = moon;
            input.moon = moon;
            EditorUtility.SetDirty(moon);
            extra = moon.moon != null ? " + MoonOrbit (moon '" + moon.moon.name + "', 4 slots)" : " + MoonOrbit (NO MOON FOUND: drag the moon object into MoonOrbit.moon)";
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[GameplaySetup] '" + scene.name + "' (level " + level + "): added Gameplay (PoolManager, LevelPlanetDefense with " + defense.slots
            + " slots, WaveDirectorBridge, EnemySpawner, TowerPlacementInput" + extra + "), LevelData '" + data.name + "', HUD canvas on Overlay with its background hidden. Saved the scene. Press Play.");
    }

    /// <summary>The scene's moon: an object named "Moon" with a CircleCollider2D that isn't the planet's body.</summary>
    static Transform FindMoon(LevelManager lm)
    {
        foreach (var c in Object.FindObjectsByType<CircleCollider2D>(FindObjectsSortMode.None))
        {
            if (lm.planetBody != null && c.gameObject == lm.planetBody.gameObject) continue;
            if (c.gameObject.name.IndexOf("moon", StringComparison.OrdinalIgnoreCase) >= 0) return c.transform;
        }
        return null;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : Undo.AddComponent<T>(go);
    }

    static int PlanetLevel(string sceneName)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:PlanetPresentation"))
        {
            var p = AssetDatabase.LoadAssetAtPath<PlanetPresentation>(AssetDatabase.GUIDToAssetPath(guid));
            if (p != null && p.sceneName == sceneName) return p.level;
        }
        return 0;
    }

    /// <summary>Fills only what's still empty, so a designer's numbers are never overwritten.</summary>
    static void FillLevelData(LevelData d, int level)
    {
        level = Mathf.Clamp(level, 0, BossByLevel.Length - 1);
        if (d.maxTime <= 0f) d.maxTime = 150f;
        if (d.startingDifficulty <= 0) d.startingDifficulty = 15 + 5 * level;
        if (d.maxDifficulty <= 0) d.maxDifficulty = 120 + 40 * level;
        if (d.postBossDifficulty <= 0) d.postBossDifficulty = d.maxDifficulty / 2;
        if (d.money <= 0) d.money = 10;
        if (d.fuel <= 0f) d.fuel = 150f;
        if (d.planetMaxHealth <= 0f) d.planetMaxHealth = 100f;
        if (d.planetRotationSpeed == 0f) d.planetRotationSpeed = 6f;
        if (d.enemyDataList == null) d.enemyDataList = new List<LevelEnemyEntry>();

        bool hasBoss = false;
        foreach (var e in d.enemyDataList) if (e != null && e.isLevelBoss && e.enemy != null) hasBoss = true;
        bool hasEnemies = false;
        foreach (var e in d.enemyDataList) if (e != null && !e.isLevelBoss && e.enemy != null) hasEnemies = true;

        if (!hasEnemies)
        {
            d.enemyDataList.RemoveAll(e => e == null || e.enemy == null);
            for (int l = 0; l <= level; l++)
                foreach (string n in NewEnemiesByLevel[l])
                {
                    var ed = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataFolder + "/" + n + ".asset");
                    if (ed != null) d.enemyDataList.Add(new LevelEnemyEntry { enemy = ed, startingBudget = l == level ? 0.5f : 1f, endingBudget = 1f });
                }
        }
        if (!hasBoss)
        {
            var boss = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyDataFolder + "/" + BossByLevel[level] + ".asset");
            if (boss != null) d.enemyDataList.Add(new LevelEnemyEntry { enemy = boss, isLevelBoss = true, startingBudget = 1f, endingBudget = 1f });
        }
        EditorUtility.SetDirty(d);
    }
}
#endif
