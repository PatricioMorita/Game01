using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Herramienta de editor que genera el contenido del prototipo:
/// sprites, sonidos, tiles, prefabs, las 3 escenas, Build Settings y el build de Windows.
/// Menú: Tools → Game01 → ...
/// </summary>
public static class ProjectBuilder
{
    const string SpritesDir = "Assets/Sprites";
    const string AudioDir = "Assets/Audio";
    const string TilesDir = "Assets/Tiles";
    const string PrefabsDir = "Assets/Prefabs";
    const string ScenesDir = "Assets/Scenes";
    const string PhysicsDir = "Assets/Physics";
    const string SceneTemplate = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
    const int PPU = 32;

    static readonly Color Sky = new Color(0.53f, 0.78f, 0.96f);

    // ------------------------------------------------------------------ Menús

    [MenuItem("Tools/Game01/1 - Importar TMP Essentials")]
    public static void ImportTMPEssentials()
    {
        if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) { Debug.Log("[Builder] TMP ya importado"); return; }
        string pkg = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
        AssetDatabase.ImportPackage(pkg, false);
        AssetDatabase.Refresh();
        Debug.Log("[Builder] TMP Essentials importado");
    }

    [MenuItem("Tools/Game01/2 - Generar contenido y escenas")]
    public static void GenerateAll()
    {
        if (TMP_Settings.defaultFontAsset == null)
            throw new Exception("[Builder] Falta TMP Essentials. Ejecuta primero el paso 1.");

        foreach (var d in new[] { SpritesDir, AudioDir, TilesDir, PrefabsDir, ScenesDir, PhysicsDir })
            EnsureFolder(d);

        int groundLayer = EnsureLayer("Ground");

        // Assets generados
        var sprites = GenerateSprites();
        var audio = GenerateAudio();
        var tiles = GenerateTiles(sprites);
        var noFriction = GeneratePhysicsMaterial();

        // Prefabs
        var audioManagerPrefab = BuildAudioManagerPrefab(audio["bgm"]);
        var playerPrefab = BuildPlayerPrefab(sprites["player"], noFriction, groundLayer, audio["jump"]);
        var coinPrefab = BuildCoinPrefab(sprites["coin"], audio["coin"]);
        var spikePrefab = BuildSpikePrefab(sprites["spikes"]);
        var goalPrefab = BuildGoalPrefab(sprites["flag"]);

        // Escenas
        BuildMainMenu(audioManagerPrefab, audio["click"], sprites);
        BuildLevel1(audioManagerPrefab, playerPrefab, coinPrefab, spikePrefab, goalPrefab,
                    tiles, sprites, groundLayer, audio);
        BuildGameOver(audioManagerPrefab, audio["click"]);

        // Build Settings: MainMenu primero
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesDir}/MainMenu.unity", true),
            new EditorBuildSettingsScene($"{ScenesDir}/Level1.unity", true),
            new EditorBuildSettingsScene($"{ScenesDir}/GameOver.unity", true),
        };

        ConfigurePlayerAndQuality();
        AssetDatabase.SaveAssets();
        Debug.Log("[Builder] Contenido y escenas generados OK");
    }

    [MenuItem("Tools/Game01/3 - Build Windows")]
    public static void BuildWindows()
    {
        string buildDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "Build"));
        Directory.CreateDirectory(buildDir);
        var options = new BuildPlayerOptions
        {
            scenes = new[] { $"{ScenesDir}/MainMenu.unity", $"{ScenesDir}/Level1.unity", $"{ScenesDir}/GameOver.unity" },
            locationPathName = Path.Combine(buildDir, "Game01.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"[Builder] Build result: {report.summary.result} ({report.summary.totalErrors} errores)");
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("[Builder] Build falló");
    }

    // ------------------------------------------------------------- Sprites

    static Dictionary<string, Sprite> GenerateSprites()
    {
        var s = new Dictionary<string, Sprite>();
        s["player"] = SaveSprite("Player", 32, 32, PlayerPixels, new Vector2(0.5f, 0.5f));
        s["grass"] = SaveSprite("GrassTile", 32, 32, GrassPixels, new Vector2(0.5f, 0.5f));
        s["dirt"] = SaveSprite("DirtTile", 32, 32, DirtPixels, new Vector2(0.5f, 0.5f));
        s["coin"] = SaveSprite("Coin", 16, 16, CoinPixels, new Vector2(0.5f, 0.5f), 16);
        s["spikes"] = SaveSprite("Spikes", 32, 16, SpikePixels, new Vector2(0.5f, 0f));
        s["flag"] = SaveSprite("Flag", 32, 64, FlagPixels, new Vector2(0.5f, 0f));
        s["cloud"] = SaveSprite("Cloud", 64, 32, CloudPixels, new Vector2(0.5f, 0.5f));
        s["hill"] = SaveSprite("Hill", 128, 64, HillPixels, new Vector2(0.5f, 0f));
        s["sign"] = SaveSprite("Sign", 32, 32, SignPixels, new Vector2(0.5f, 0f));
        s["house"] = SaveSprite("House", 64, 64, HousePixels, new Vector2(0.5f, 0f));
        return s;
    }

    static Sprite SaveSprite(string name, int w, int h, Func<int, int, Color> pixel, Vector2 pivot, int ppu = PPU)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                tex.SetPixel(x, y, pixel(x, y));
        tex.Apply();
        string path = $"{SpritesDir}/{name}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true;
        var settings = new TextureImporterSettings();
        imp.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(settings);
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static readonly Color Clear = new Color(0, 0, 0, 0);
    static float Noise(int x, int y) => Mathf.Abs(Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f) % 1f;

    static Color PlayerPixels(int x, int y)
    {
        // Cuerpo redondeado naranja con ojos y pies
        bool body = x >= 5 && x <= 26 && y >= 4 && y <= 28 &&
                    !((x < 8 || x > 23) && (y < 7 || y > 25));
        bool feet = y >= 1 && y <= 3 && ((x >= 7 && x <= 12) || (x >= 19 && x <= 24));
        if (feet) return new Color(0.35f, 0.2f, 0.12f);
        if (!body) return Clear;
        if (x == 5 || x == 26 || y == 4 || y == 28) return new Color(0.6f, 0.25f, 0.05f);
        bool eyeL = x >= 10 && x <= 14 && y >= 17 && y <= 22;
        bool eyeR = x >= 18 && x <= 22 && y >= 17 && y <= 22;
        if ((eyeL && x >= 12 && y <= 19) || (eyeR && x >= 20 && y <= 19)) return Color.black;
        if (eyeL || eyeR) return Color.white;
        if (y >= 10 && y <= 11 && x >= 12 && x <= 20) return new Color(0.45f, 0.15f, 0.05f); // boca
        return y > 16 ? new Color(1f, 0.6f, 0.15f) : new Color(0.95f, 0.5f, 0.1f);
    }

    static Color DirtPixels(int x, int y)
    {
        float n = Noise(x, y);
        var c = new Color(0.55f, 0.35f, 0.2f);
        if (n > 0.9f) c = new Color(0.45f, 0.28f, 0.15f);
        else if (n < 0.06f) c = new Color(0.65f, 0.45f, 0.28f);
        return c;
    }

    static Color GrassPixels(int x, int y)
    {
        int grassLine = 24 + (int)(Noise(x, 0) * 3);
        if (y >= grassLine) return y >= 30 ? new Color(0.45f, 0.85f, 0.3f) : new Color(0.3f, 0.7f, 0.2f);
        return DirtPixels(x, y);
    }

    static Color CoinPixels(int x, int y)
    {
        float dx = x - 7.5f, dy = y - 7.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
        if (d > 7.2f) return Clear;
        if (d > 6f) return new Color(0.8f, 0.55f, 0.05f);
        if (x >= 7 && x <= 8 && y >= 4 && y <= 11) return new Color(1f, 0.95f, 0.6f);
        return new Color(1f, 0.82f, 0.15f);
    }

    static Color SpikePixels(int x, int y)
    {
        int local = x % 8;                         // 4 pinchos de 8px
        int halfWidth = (int)((15 - y) * 4f / 16f);
        if (Mathf.Abs(local - 3.5f) > halfWidth + 0.5f) return Clear;
        return local < 4 ? new Color(0.85f, 0.85f, 0.9f) : new Color(0.6f, 0.6f, 0.68f);
    }

    static Color FlagPixels(int x, int y)
    {
        if (x >= 6 && x <= 8 && y <= 60) return new Color(0.4f, 0.4f, 0.45f);        // mástil
        if (y >= 61 && x >= 5 && x <= 9) return new Color(1f, 0.85f, 0.2f);            // punta
        if (x >= 9 && y >= 40 && y <= 58 && x <= 9 + (58 - y) * 1.2f + 4)               // bandera
            return new Color(0.2f, 0.75f, 0.3f);
        return Clear;
    }

    static Color CloudPixels(int x, int y)
    {
        float a = Ellipse(x, y, 20, 14, 14, 10), b = Ellipse(x, y, 38, 18, 16, 12), c = Ellipse(x, y, 50, 12, 12, 8);
        return (a < 1 || b < 1 || c < 1) ? new Color(1, 1, 1, 0.9f) : Clear;
    }

    static Color HillPixels(int x, int y)
    {
        float e = Ellipse(x, y, 64, 0, 62, 60);
        if (e >= 1) return Clear;
        return Noise(x / 4, y / 4) > 0.85f ? new Color(0.35f, 0.62f, 0.38f) : new Color(0.42f, 0.7f, 0.45f);
    }

    static Color SignPixels(int x, int y)
    {
        if (x >= 14 && x <= 17 && y < 16) return new Color(0.45f, 0.3f, 0.15f);       // poste
        if (y >= 14 && y <= 27 && x >= 3 && x <= 28)                                   // tabla
        {
            // flecha →
            bool shaft = y >= 19 && y <= 22 && x >= 8 && x <= 20;
            bool head = x >= 20 && x <= 25 && Mathf.Abs(y - 20.5f) <= (25 - x);
            if (shaft || head) return new Color(0.95f, 0.95f, 0.9f);
            return new Color(0.6f, 0.42f, 0.22f);
        }
        return Clear;
    }

    static Color HousePixels(int x, int y)
    {
        // Casa de Pixel (destino del relato)
        if (y >= 36 && Mathf.Abs(x - 31.5f) <= (62 - y) * 1.25f) return new Color(0.75f, 0.2f, 0.2f); // techo
        if (x >= 8 && x <= 55 && y < 36)
        {
            if (x >= 26 && x <= 37 && y < 20) return new Color(0.4f, 0.25f, 0.12f);                    // puerta
            if (x >= 12 && x <= 21 && y >= 20 && y <= 29) return new Color(0.6f, 0.85f, 1f);             // ventana
            if (x >= 42 && x <= 51 && y >= 20 && y <= 29) return new Color(0.6f, 0.85f, 1f);
            return new Color(0.95f, 0.88f, 0.7f);
        }
        return Clear;
    }

    static float Ellipse(int x, int y, float cx, float cy, float rx, float ry)
        => ((x - cx) * (x - cx)) / (rx * rx) + ((y - cy) * (y - cy)) / (ry * ry);

    // --------------------------------------------------------------- Audio

    const int Rate = 44100;

    static Dictionary<string, AudioClip> GenerateAudio()
    {
        var a = new Dictionary<string, AudioClip>();
        a["jump"] = SaveWav("SFX_Jump", Sweep(0.18f, 280, 720, 0.35f));
        a["coin"] = SaveWav("SFX_Coin", Concat(Tone(0.07f, 988, 0.3f, false), Tone(0.28f, 1319, 0.3f, true)));
        a["death"] = SaveWav("SFX_Death", Sweep(0.7f, 620, 90, 0.35f, vibrato: true));
        a["win"] = SaveWav("SFX_Win", Concat(Tone(0.12f, 523, 0.3f, false), Tone(0.12f, 659, 0.3f, false),
                                             Tone(0.12f, 784, 0.3f, false), Tone(0.5f, 1047, 0.3f, true)));
        a["click"] = SaveWav("SFX_Click", Tone(0.06f, 1200, 0.3f, true));
        a["bgm"] = SaveWav("BGM_Level", Music());
        return a;
    }

    static float Square(double phase) => (phase % 1.0) < 0.5 ? 1f : -1f;
    static float Triangle(double phase) { double p = phase % 1.0; return (float)(p < 0.5 ? 4 * p - 1 : 3 - 4 * p); }
    static float Midi(int n) => 440f * Mathf.Pow(2f, (n - 69) / 12f);

    static float[] Tone(float dur, float freq, float vol, bool decay)
    {
        int n = (int)(dur * Rate); var s = new float[n]; double ph = 0;
        for (int i = 0; i < n; i++)
        {
            float env = decay ? 1f - (float)i / n : (i > n - 200 ? (n - i) / 200f : 1f);
            ph += freq / Rate; s[i] = Square(ph) * vol * env;
        }
        return s;
    }

    static float[] Sweep(float dur, float f0, float f1, float vol, bool vibrato = false)
    {
        int n = (int)(dur * Rate); var s = new float[n]; double ph = 0;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            float f = Mathf.Lerp(f0, f1, t) * (vibrato ? 1f + 0.06f * Mathf.Sin(i * 0.004f) : 1f);
            ph += f / Rate; s[i] = Square(ph) * vol * (1f - t);
        }
        return s;
    }

    static float[] Concat(params float[][] parts)
    {
        var list = new List<float>(); foreach (var p in parts) list.AddRange(p); return list.ToArray();
    }

    static float[] Music()
    {
        // Melodía original en Do mayor, 150 bpm, 4 compases en loop (corcheas)
        int[] melody = {
            72,76,79,76, 72,76,79,84,
            81,79,76,72, 74,76,74,72,
            77,81,84,81, 77,81,79,77,
            79,77,76,74, 74,71,67,71 };
        int[] bass = { 48,48,55,48, 45,45,52,45, 41,41,48,41, 43,43,50,43 };
        float eighth = 0.2f;
        int noteLen = (int)(eighth * Rate);
        var s = new float[melody.Length * noteLen];
        double phM = 0, phB = 0;
        for (int i = 0; i < s.Length; i++)
        {
            int mi = i / noteLen, bi = i / (noteLen * 2);
            float tm = (float)(i % noteLen) / noteLen;
            float tb = (float)(i % (noteLen * 2)) / (noteLen * 2);
            phM += Midi(melody[mi]) / Rate;
            phB += Midi(bass[bi]) / Rate;
            float mEnv = tm < 0.85f ? 1f - tm * 0.5f : (1f - tm) / 0.15f * 0.575f;
            float bEnv = tb < 0.9f ? 1f : (1f - tb) * 10f;
            s[i] = Square(phM) * 0.12f * mEnv + Triangle(phB) * 0.22f * bEnv;
        }
        return s;
    }

    static AudioClip SaveWav(string name, float[] samples)
    {
        string path = $"{AudioDir}/{name}.wav";
        using (var fs = new FileStream(path, FileMode.Create))
        using (var w = new BinaryWriter(fs))
        {
            int dataBytes = samples.Length * 2;
            w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + dataBytes);
            w.Write(new[] { 'W', 'A', 'V', 'E' }); w.Write(new[] { 'f', 'm', 't', ' ' });
            w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2);
            w.Write((short)2); w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(dataBytes);
            foreach (var v in samples) w.Write((short)(Mathf.Clamp(v, -1f, 1f) * short.MaxValue));
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    // --------------------------------------------------------- Tiles / física

    static Dictionary<string, TileBase> GenerateTiles(Dictionary<string, Sprite> sprites)
    {
        var t = new Dictionary<string, TileBase>();
        foreach (var key in new[] { "grass", "dirt" })
        {
            string path = $"{TilesDir}/{char.ToUpper(key[0]) + key.Substring(1)}.asset";
            AssetDatabase.DeleteAsset(path);
            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprites[key];
            tile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(tile, path);
            t[key] = tile;
        }
        return t;
    }

    static PhysicsMaterial2D GeneratePhysicsMaterial()
    {
        string path = $"{PhysicsDir}/PlayerNoFriction.physicsMaterial2D";
        AssetDatabase.DeleteAsset(path);
        var mat = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // -------------------------------------------------------------- Prefabs

    static GameObject SavePrefab(GameObject go)
    {
        string path = $"{PrefabsDir}/{go.name}.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
        UnityEngine.Object.DestroyImmediate(go);
        return prefab;
    }

    static GameObject BuildAudioManagerPrefab(AudioClip bgm)
    {
        var go = new GameObject("AudioManager");
        var music = go.AddComponent<AudioSource>(); music.playOnAwake = false;
        var sfx = go.AddComponent<AudioSource>(); sfx.playOnAwake = false;
        var am = go.AddComponent<AudioManager>();
        Set(am, "musicSource", music);
        Set(am, "sfxSource", sfx);
        Set(am, "backgroundMusic", bgm);
        return SavePrefab(go);
    }

    static GameObject BuildPlayerPrefab(Sprite sprite, PhysicsMaterial2D mat, int groundLayer, AudioClip jump)
    {
        var go = new GameObject("Player");
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 5;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<CapsuleCollider2D>();
        col.size = new Vector2(0.7f, 0.85f); col.offset = new Vector2(0f, -0.06f);
        col.sharedMaterial = mat;

        var check = new GameObject("GroundCheck").transform;
        check.SetParent(go.transform); check.localPosition = new Vector3(0f, -0.5f, 0f);

        var pc = go.AddComponent<PlayerController>();
        Set(pc, "groundCheck", check);
        SetMask(pc, "groundLayer", 1 << groundLayer);
        Set(pc, "jumpSfx", jump);
        return SavePrefab(go);
    }

    static GameObject BuildCoinPrefab(Sprite sprite, AudioClip sfx)
    {
        var go = new GameObject("Coin");
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 3;
        go.transform.localScale = Vector3.one * 0.6f;
        var col = go.AddComponent<CircleCollider2D>(); col.isTrigger = true; col.radius = 0.5f;
        var coin = go.AddComponent<Coin>();
        Set(coin, "pickupSfx", sfx);
        return SavePrefab(go);
    }

    static GameObject BuildSpikePrefab(Sprite sprite)
    {
        var go = new GameObject("Spikes");
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 3;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true;
        col.size = new Vector2(0.8f, 0.3f); col.offset = new Vector2(0f, 0.15f);
        go.AddComponent<Hazard>();
        return SavePrefab(go);
    }

    static GameObject BuildGoalPrefab(Sprite sprite)
    {
        var go = new GameObject("Goal");
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 2;
        var col = go.AddComponent<BoxCollider2D>(); col.isTrigger = true;
        col.size = new Vector2(1f, 2f); col.offset = new Vector2(0f, 1f);
        go.AddComponent<Goal>();
        return SavePrefab(go);
    }

    // --------------------------------------------------------------- Escenas

    static Scene NewSceneFromTemplate(string name)
    {
        string path = $"{ScenesDir}/{name}.unity";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CopyAsset(SceneTemplate, path);
        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Sky;
        cam.transform.position = new Vector3(0, 0, -10);
        EnsureGlobalLight();
        return scene;
    }

    static void EnsureGlobalLight()
    {
        foreach (var l in UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if (l.lightType == Light2D.LightType.Global) return;
        var go = new GameObject("Global Light 2D");
        var light = go.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.intensity = 1f;
    }

    static void BuildMainMenu(GameObject audioPrefab, AudioClip click, Dictionary<string, Sprite> sprites)
    {
        var scene = NewSceneFromTemplate("MainMenu");
        PrefabUtility.InstantiatePrefab(audioPrefab);

        // Decoración de fondo (relato: el valle de Pixel)
        Deco(sprites["hill"], new Vector3(-6, -6, 0), -10, 1.5f);
        Deco(sprites["hill"], new Vector3(7, -6, 0), -10, 1.2f);
        Deco(sprites["cloud"], new Vector3(-7, 4, 0), -9, 1.5f);
        Deco(sprites["cloud"], new Vector3(8, 3, 0), -9, 1.2f);
        Deco(sprites["player"], new Vector3(-6, -3.5f, 0), 5, 2.5f);

        var canvas = CreateCanvas();
        var menu = canvas.gameObject.AddComponent<MenuUI>();
        Set(menu, "clickSfx", click);

        CreateText(canvas.transform, "Title", "Pixel en el Valle", 110, new Vector2(0, 330), new Vector2(1400, 160), Color.white, true);
        CreateText(canvas.transform, "Story",
            "Pixel se alejó demasiado de casa.\nCruza el valle, junta monedas y esquiva los pinchos\npara llegar a la bandera junto a su casa.",
            40, new Vector2(0, 150), new Vector2(1400, 200), new Color(0.1f, 0.15f, 0.3f), false);

        var play = CreateButton(canvas.transform, "PlayButton", "Jugar", new Vector2(0, -30));
        UnityEventTools.AddStringPersistentListener(play.onClick, menu.LoadScene, "Level1");
        var quit = CreateButton(canvas.transform, "QuitButton", "Salir", new Vector2(0, -160));
        UnityEventTools.AddPersistentListener(quit.onClick, menu.QuitGame);

        CreateText(canvas.transform, "Controls", "A / D o flechas: moverse   ·   Espacio: saltar", 32,
            new Vector2(0, -470), new Vector2(1400, 60), new Color(0.1f, 0.15f, 0.3f), false);

        CreateEventSystem();
        EditorSceneManager.SaveScene(scene);
    }

    static void BuildGameOver(GameObject audioPrefab, AudioClip click)
    {
        var scene = NewSceneFromTemplate("GameOver");
        Camera.main.backgroundColor = new Color(0.12f, 0.12f, 0.2f);
        PrefabUtility.InstantiatePrefab(audioPrefab);

        var canvas = CreateCanvas();
        var menu = canvas.gameObject.AddComponent<MenuUI>();
        Set(menu, "clickSfx", click);

        var title = CreateText(canvas.transform, "Title", "Game Over", 120, new Vector2(0, 260), new Vector2(1400, 170), Color.white, true);
        var score = CreateText(canvas.transform, "Score", "Monedas: 0", 56, new Vector2(0, 110), new Vector2(1000, 90), new Color(1f, 0.85f, 0.3f), false);

        var ui = canvas.gameObject.AddComponent<GameOverUI>();
        Set(ui, "titleText", title);
        Set(ui, "scoreText", score);

        var retry = CreateButton(canvas.transform, "RetryButton", "Reintentar", new Vector2(0, -60));
        UnityEventTools.AddStringPersistentListener(retry.onClick, menu.LoadScene, "Level1");
        var toMenu = CreateButton(canvas.transform, "MenuButton", "Menú", new Vector2(0, -190));
        UnityEventTools.AddStringPersistentListener(toMenu.onClick, menu.LoadScene, "MainMenu");

        CreateEventSystem();
        EditorSceneManager.SaveScene(scene);
    }

    static void BuildLevel1(GameObject audioPrefab, GameObject playerPrefab, GameObject coinPrefab,
                            GameObject spikePrefab, GameObject goalPrefab, Dictionary<string, TileBase> tiles,
                            Dictionary<string, Sprite> sprites, int groundLayer, Dictionary<string, AudioClip> audio)
    {
        var scene = NewSceneFromTemplate("Level1");
        PrefabUtility.InstantiatePrefab(audioPrefab);

        // Abrir una escena descarga assets recién creados: recargar los tiles desde disco
        tiles = new Dictionary<string, TileBase>
        {
            ["grass"] = AssetDatabase.LoadAssetAtPath<TileBase>($"{TilesDir}/Grass.asset"),
            ["dirt"] = AssetDatabase.LoadAssetAtPath<TileBase>($"{TilesDir}/Dirt.asset"),
        };

        // --- Tilemap del nivel
        var grid = new GameObject("Grid").AddComponent<Grid>();
        var tmGo = new GameObject("Ground");
        tmGo.transform.SetParent(grid.transform);
        tmGo.layer = groundLayer;
        var tm = tmGo.AddComponent<Tilemap>();
        tmGo.AddComponent<TilemapRenderer>().sortingOrder = 0;
        var rbGround = tmGo.AddComponent<Rigidbody2D>(); rbGround.bodyType = RigidbodyType2D.Static;
        tmGo.AddComponent<CompositeCollider2D>();
        var tmCol = tmGo.AddComponent<TilemapCollider2D>();
        tmCol.compositeOperation = Collider2D.CompositeOperation.Merge;

        // Suelo: (desdeX, hastaX, alturaSuperficie) — la fila superior es pasto
        void Ground(int x0, int x1, int top) { for (int x = x0; x <= x1; x++) for (int y = -6; y <= top; y++) tm.SetTile(new Vector3Int(x, y, 0), y == top ? tiles["grass"] : tiles["dirt"]); }
        void Platform(int x0, int x1, int y) { for (int x = x0; x <= x1; x++) tm.SetTile(new Vector3Int(x, y, 0), tiles["grass"]); }

        for (int y = -6; y <= 12; y++) tm.SetTile(new Vector3Int(-12, y, 0), tiles["dirt"]); // muro izquierdo
        Ground(-11, 14, -3);
        Ground(18, 32, -3);
        Platform(25, 29, -1);
        Ground(36, 39, -3);
        Ground(40, 41, -2);
        Ground(42, 47, -1);
        Ground(51, 66, -3);
        Platform(54, 58, -1);
        for (int y = -6; y <= 12; y++) tm.SetTile(new Vector3Int(67, y, 0), tiles["dirt"]);  // muro derecho

        // Generar la geometría del collider ahora (se guarda en la escena)
        tmCol.ProcessTilemapChanges();
        var composite = tmGo.GetComponent<CompositeCollider2D>();
        composite.GenerateGeometry();
        Debug.Log($"[Builder] Collider del suelo: {composite.pathCount} contornos");

        // --- Objetos (prefabs)
        var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
        player.transform.position = new Vector3(-8.5f, -1.5f, 0);

        void Coin(float x, float y) => ((GameObject)PrefabUtility.InstantiatePrefab(coinPrefab)).transform.position = new Vector3(x, y, 0);
        void Spike(int x, int top) => ((GameObject)PrefabUtility.InstantiatePrefab(spikePrefab)).transform.position = new Vector3(x + 0.5f, top + 1, 0);

        Coin(-3.5f, -1.3f); Coin(-1.5f, -1.3f); Coin(0.5f, -1.3f);
        Coin(16.5f, 0f);                                   // sobre el primer abismo
        Coin(26.5f, 0.7f); Coin(27.5f, 0.7f); Coin(28.5f, 0.7f);
        Coin(34f, 0.2f);                                   // segundo abismo
        Coin(44.5f, 0.7f); Coin(49f, 1.2f);
        Coin(55.5f, 0.7f); Coin(56.5f, 0.7f);

        Spike(22, -3);
        Spike(30, -3); Spike(31, -3);
        Spike(54, -3); Spike(55, -3); Spike(56, -3); Spike(57, -3); Spike(58, -3);

        var goal = (GameObject)PrefabUtility.InstantiatePrefab(goalPrefab);
        goal.transform.position = new Vector3(62.5f, -2f, 0);

        // --- Decoración / relato gráfico
        Deco(sprites["sign"], new Vector3(-6.5f, -2f, 0), 1, 1f);
        Deco(sprites["house"], new Vector3(65f, -2f, 0), 1, 1f);
        for (int i = 0; i < 7; i++) Deco(sprites["hill"], new Vector3(-8 + i * 12, -3.5f, 0), -10, 1.4f + (i % 2) * 0.4f);
        for (int i = 0; i < 9; i++) Deco(sprites["cloud"], new Vector3(-8 + i * 9, 2.5f + (i % 3), 0), -9, 1f + (i % 2) * 0.5f);

        // --- Cámara que sigue al jugador
        var cam = Camera.main;
        cam.transform.position = new Vector3(player.transform.position.x, player.transform.position.y + 1f, -10);
        var follow = cam.gameObject.AddComponent<CameraFollow>();
        Set(follow, "target", player.transform);

        // --- UI y GameManager
        var canvas = CreateCanvas();
        var scoreText = CreateText(canvas.transform, "ScoreText", "Monedas: 0", 52, Vector2.zero, new Vector2(500, 80), Color.white, true);
        Anchor(scoreText.rectTransform, new Vector2(0, 1), new Vector2(40, -30));
        scoreText.alignment = TextAlignmentOptions.TopLeft;
        var hint = CreateText(canvas.transform, "HintText", "Llega a la bandera junto a la casa", 36, Vector2.zero, new Vector2(1000, 60), new Color(0.1f, 0.15f, 0.3f), false);
        Anchor(hint.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -40));

        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        Set(gm, "scoreText", scoreText);
        Set(gm, "deathSfx", audio["death"]);
        Set(gm, "winSfx", audio["win"]);

        EditorSceneManager.SaveScene(scene);
    }

    // ------------------------------------------------------------ Helpers UI

    static Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, Vector2 pos, Vector2 box, Color color, bool outline)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        if (outline) { t.fontStyle = FontStyles.Bold; t.outlineWidth = 0.2f; t.outlineColor = new Color32(30, 40, 70, 255); }
        var rt = t.rectTransform; rt.anchoredPosition = pos; rt.sizeDelta = box;
        return t;
    }

    static void Anchor(RectTransform rt, Vector2 anchor, Vector2 pos)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = pos;
    }

    static Button CreateButton(Transform parent, string name, string label, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = new Color(0.95f, 0.55f, 0.15f);
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
        colors.pressedColor = new Color(0.8f, 0.6f, 0.4f);
        btn.colors = colors;
        var rt = (RectTransform)go.transform; rt.sizeDelta = new Vector2(420, 100); rt.anchoredPosition = pos;

        var txt = CreateText(go.transform, "Label", label, 48, Vector2.zero, Vector2.zero, Color.white, false);
        txt.fontStyle = FontStyles.Bold;
        txt.rectTransform.anchorMin = Vector2.zero; txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.sizeDelta = Vector2.zero;
        return btn;
    }

    static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    static void Deco(Sprite sprite, Vector3 pos, int order, float scale)
    {
        var go = new GameObject("Deco_" + sprite.name);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = order;
        go.transform.position = pos; go.transform.localScale = Vector3.one * scale;
    }

    // -------------------------------------------------------- Helpers varios

    static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field) ?? throw new Exception($"[Builder] Campo '{field}' no existe en {target.GetType().Name}");
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void SetMask(UnityEngine.Object target, string field, int mask)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).intValue = mask;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    static int EnsureLayer(string name)
    {
        int existing = LayerMask.NameToLayer(name);
        if (existing >= 0) return existing;
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var el = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(el.stringValue))
            {
                el.stringValue = name;
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
        }
        throw new Exception("[Builder] No hay capas libres");
    }

    static void ConfigurePlayerAndQuality()
    {
        PlayerSettings.productName = "Pixel en el Valle";
        PlayerSettings.companyName = "Game01";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;
        QualitySettings.vSyncCount = 1;
    }
}
