// SHAIQAS · Редактор: жоба бірінші ашылғанда өзі бапталады.
//  • Art/ суреттері: mipmap жоқ, 4096 шегі, parts/ — оқылатын (атлас үшін), сығымдаусыз
//  • Materials/FighterAtlas.mat жасалады
//  • Player Settings: көлденең экран, атауы, 60 FPS
// Мәзір: SHAIQAS → Setup Project (қайта баптау), SHAIQAS → Reimport Art, SHAIQAS → Reset Save.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ShaiqasSetup
{
    static ShaiqasSetup() { EditorApplication.delayCall += () => { EnsureGamma(); if (!SessionState.GetBool("shaiqas-setup", false)) { SessionState.SetBool("shaiqas-setup", true); Setup(false); } }; }

    /// <summary>Веб-кенеп (Canvas 2D) мөлдір қабаттарды sRGB-де араластырады. Linear режимде тұман мен
    /// бұлттар «ағарып» кетеді, сондықтан ойын Gamma түс кеңістігінде жұмыс істейді.</summary>
    static void EnsureGamma()
    {
        if (PlayerSettings.colorSpace == ColorSpace.Gamma) return;
        PlayerSettings.colorSpace = ColorSpace.Gamma;
        Debug.Log("SHAIQAS: түс кеңістігі Gamma-ға ауыстырылды (веб-кенеппен бірдей араластыру)");
    }

    [MenuItem("SHAIQAS/Setup Project")]
    public static void SetupMenu() { Setup(true); }

    static void Setup(bool verbose)
    {
        EnsureMaterial();
        EnsureScene();
        PlayerSettings.productName = "SHAIQAS";
        PlayerSettings.companyName = "SHAIQAS";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        if (verbose) Debug.Log("SHAIQAS: жоба бапталды ✔");
    }

    static void EnsureMaterial()
    {
        const string path = "Assets/Resources/Materials/FighterAtlas.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        Shader sh = Shader.Find("Shaiqas/FighterAtlas");
        if (sh == null) { Debug.LogWarning("SHAIQAS: Shaiqas/FighterAtlas шейдері табылмады"); return; }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Materials")) AssetDatabase.CreateFolder("Assets/Resources", "Materials");
        AssetDatabase.CreateAsset(new Material(sh), path);
        AssetDatabase.SaveAssets();
        Debug.Log("SHAIQAS: FighterAtlas материалы жасалды");
    }

    /// <summary>Құрастыру (APK/EXE) үшін бір сахна керек: Assets/Scenes/Main.unity (ішінде тек камера — қалғанын код жасайды).</summary>
    static void EnsureScene()
    {
        const string path = "Assets/Scenes/Main.unity";
        if (EditorBuildSettings.scenes.Length > 0) return;
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
        {
            if (EditorSceneManager.GetActiveScene().isDirty) return;   // пайдаланушының сақталмаған өзгерісін бұзбаймыз
            if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
            var sc = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(sc, path);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        Debug.Log("SHAIQAS: Main сахнасы құрастыруға қосылды");
    }

    [MenuItem("SHAIQAS/Reimport Art")]
    public static void ReimportArt() { AssetDatabase.ImportAsset("Assets/Resources/Art", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate); }

    [MenuItem("SHAIQAS/Reset Save (тест)")]
    public static void ResetSave() { PlayerPrefs.DeleteKey(Game.KEY); PlayerPrefs.Save(); Debug.Log("SHAIQAS: сақтау өшірілді"); }
}

/// <summary>Art/ ішіндегі суреттерді импорттау баптаулары.</summary>
public class ShaiqasArtImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("/Resources/Art/")) return;
        TextureImporter ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Default;
        ti.mipmapEnabled = false;
        ti.npotScale = TextureImporterNPOTScale.None;
        ti.wrapMode = TextureWrapMode.Clamp;
        ti.filterMode = FilterMode.Bilinear;
        ti.alphaIsTransparency = true;
        ti.maxTextureSize = 4096;
        bool part = assetPath.Contains("/Art/parts/") || assetPath.Contains("uiatlas");
        ti.isReadable = part;
        ti.textureCompression = part || assetPath.Contains("/live/") || assetPath.Contains("/Art/sk/") ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
    }
}
