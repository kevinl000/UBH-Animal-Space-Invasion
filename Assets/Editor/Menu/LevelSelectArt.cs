using System.IO;
using UnityEditor;
using UnityEngine;

// Small, deterministic source sprites. All drawing happens in the editor.
public static class LevelSelectArt
{
    public const string Folder = "Assets/Art/LevelSelect/";
    public static readonly Color32 Navy = new Color32(9, 17, 37, 255);
    public static readonly Color32 Cream = new Color32(255, 239, 195, 255);
    public static readonly Color32 Blue = new Color32(155, 183, 218, 255);
    private static readonly Color32 Orbit = new Color32(24, 38, 61, 255);
    private static readonly Color32[] Palette =
    {
        Navy, Orbit, new Color32(42, 61, 86, 255), Blue, Cream,
        new Color32(35, 68, 105, 255), new Color32(58, 113, 155, 255), new Color32(110, 182, 204, 255),
        new Color32(41, 78, 78, 255), new Color32(72, 126, 110, 255), new Color32(147, 187, 133, 255),
        new Color32(72, 57, 100, 255), new Color32(118, 91, 143, 255), new Color32(182, 151, 189, 255),
        new Color32(146, 79, 57, 255), new Color32(210, 132, 66, 255), new Color32(246, 193, 104, 255)
    };

    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(Folder.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/Art", "LevelSelect");
        CreateSun();
        int[] ramps = { 5, 14, 8, 11, 5, 14, 11, 8, 5 };
        for (int i = 0; i < ramps.Length; i++)
            CreatePlanet(i, i == 1 || i == 5 ? 23 : 20, ramps[i]);
        CreateFocus();
        GenerateSelectionHalos();
        GenerateDetailsArt();
        AssetDatabase.SaveAssets();
    }

    public static void GenerateDetailsArt()
    {
        var panel = new Color32[16 * 16];
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++)
            panel[y * 16 + x] = x == 0 || x == 15 || y == 0 || y == 15 ? Palette[2] : Navy;
        Save("DetailsPanel", 16, 16, panel);
        var importer = (TextureImporter)AssetImporter.GetAtPath(Folder + "DetailsPanel.png");
        importer.spriteBorder = new Vector4(2, 2, 2, 2);
        importer.SaveAndReimport();

        const int size = 24;
        var marker = new Color32[size * size];
        string[] question = { "01110", "10001", "00001", "00010", "00100", "00000", "00100" };
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int edge = Mathf.Min(x, y, size - 1 - x, size - 1 - y);
            marker[y * size + x] = edge == 0 ? Palette[2] : Orbit;
            int column = (x - 7) / 2, row = (18 - y) / 2;
            if (x >= 7 && x < 17 && y >= 5 && y <= 18 && question[row][column] == '1')
                marker[y * size + x] = Blue;
        }
        Save("UnknownBoss", size, size, marker);
    }

    public static void GenerateSelectionHalos()
    {
        foreach (int size in new[] { 28, 31 })
        {
            var pixels = new Color32[size * size];
            float center = (size - 1) / 2f, radius = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                if (distance > radius || distance < radius - 2) continue;
                var color = distance < radius - 1 ? Cream : Palette[16];
                if (distance >= radius - 1) color.a = 90;
                pixels[y * size + x] = color;
            }
            Save(size == 28 ? "SelectionHalo" : "SelectionHaloLarge", size, size, pixels);
        }
    }

    public static void CreateBackground(Vector2 sunPosition, Vector2[] planetPositions)
    {
        const int width = 418, height = 236;
        // Scene coordinates use four UI units per art pixel. Sample at pixel centers.
        var center = new Vector2(width / 2f, height / 2f) + sunPosition / 4f;
        var radii = new float[planetPositions.Length];
        for (int i = 0; i < radii.Length; i++)
            radii[i] = Vector2.Distance(sunPosition, planetPositions[i]) / 4f;
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            pixels[y * width + x] = Navy;
            float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
            for (int ring = 0; ring < radii.Length; ring++)
                if (Mathf.Abs(distance - radii[ring]) < 0.5f)
                    pixels[y * width + x] = Orbit;
        }
        var random = new System.Random(2026);
        for (int i = 0; i < 145; i++)
        {
            int x = random.Next(3, width - 3), y = random.Next(3, height - 3);
            // Keep the title and map labels quiet enough to read.
            if (y > 170 && y < 216 || y > 76 && y < 106) continue;
            pixels[y * width + x] = Palette[i % 9 == 0 ? 3 : 2];
            if (i % 23 != 0) continue;
            pixels[y * width + x] = Cream;
            pixels[y * width + x - 1] = Blue;
            pixels[y * width + x + 1] = Blue;
            pixels[(y - 1) * width + x] = Blue;
            pixels[(y + 1) * width + x] = Blue;
        }
        Save("StarMap", width, height, pixels);
    }

    private static void CreatePlanet(int index, int size, int ramp)
    {
        var pixels = new Color32[size * size];
        float center = (size - 1) / 2f, radius = size / 2f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - center) / radius, dy = (y - center) / radius;
            float distance = dx * dx + dy * dy;
            if (distance > 1) continue;
            float light = -dx * 0.6f + dy * 0.5f + Mathf.Sqrt(1 - distance) * 0.45f;
            int shade = light > 0.6f ? 2 : light > 0 ? 1 : 0;
            pixels[y * size + x] = distance > 0.88f ? Palette[ramp] : Palette[ramp + shade];
        }
        Save("Planet" + index, size, size, pixels);
    }

    private static void CreateSun()
    {
        const int size = 96;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - 47.5f, dy = y - 47.5f;
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            if (distance > 47) continue;
            int color = distance > 44 ? 14 : distance > 41 ? 15 : distance > 38 ? 16 : 4;
            if (distance < 38 && (x * 7 + y * 13) % 61 < 3) color = 16;
            pixels[y * size + x] = Palette[color];
        }
        Save("Sun", size, size, pixels);
    }

    private static void CreateFocus()
    {
        const int size = 30;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            int edgeX = Mathf.Min(x, size - 1 - x), edgeY = Mathf.Min(y, size - 1 - y);
            if (edgeX == 0 && edgeY < 5 || edgeY == 0 && edgeX < 5)
                pixels[y * size + x] = Cream;
        }
        Save("Focus", size, size, pixels);
    }

    private static void Save(string name, int width, int height, Color32[] pixels)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        texture.Apply();
        string path = Folder + name + ".png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 100;
        importer.isReadable = false;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
}
