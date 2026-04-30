#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

/// <summary>
/// Cria uma cena mínima para gravar poses com o Step Recorder.
/// Contém apenas: câmera, KinectManager, KinectBodyTracker e SkeletonVisualizer.
/// Use: JustDance > 5. Criar Cena de Gravação de Poses
/// </summary>
public static class RecordingSceneBuilder
{
    const string ScenePath = "Assets/JustDance/Scenes/PoseRecording.unity";

    [MenuItem("JustDance/5. Criar Cena de Gravação de Poses", priority = 5)]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureScenesFolder();

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Câmera ────────────────────────────────────────────────────────────
        var camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
        cam.orthographic    = false;
        cam.fieldOfView     = 60f;
        camGO.transform.position = new Vector3(0, 1f, -3f);
        camGO.AddComponent<AudioListener>();

        // ── EventSystem ───────────────────────────────────────────────────────
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── [System] — Kinect ─────────────────────────────────────────────────
        var systemGO = new GameObject("[System]");
        systemGO.AddComponent<KinectManager>();
        systemGO.AddComponent<KinectBodyTracker>();

        // ── [Skeleton] — visualização em tempo real ───────────────────────────
        var skelGO  = new GameObject("[Skeleton]");
        var skelVis = skelGO.AddComponent<SkeletonVisualizer>();
        var skelMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        skelVis.boneMaterial             = skelMat;
        skelVis.kinectProjectionCamera   = cam;

        // ── Instruções na cena (GameObject com nome descritivo) ───────────────
        var infoGO = new GameObject("[ Abra: Window > JustDance > Step Recorder ]");
        infoGO.transform.position = Vector3.zero;

        // ── Salvar ────────────────────────────────────────────────────────────
        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.Refresh();

        // Adicionar ao Build Settings se ainda não estiver lá
        AddToBuildSettings(ScenePath);

        EditorUtility.DisplayDialog("JustDance ✓",
            "Cena de gravação criada!\n\n" +
            "Como usar:\n" +
            "1. Abra esta cena: Assets/JustDance/Scenes/PoseRecording.unity\n" +
            "2. Clique em Play\n" +
            "3. Menu: Window > JustDance > Step Recorder\n" +
            "4. Veja a silhueta verde aparecer no preview\n" +
            "5. Grave as poses e salve os DanceStep assets", "OK");
    }

    static void EnsureScenesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/JustDance"))
            AssetDatabase.CreateFolder("Assets", "JustDance");
        if (!AssetDatabase.IsValidFolder("Assets/JustDance/Scenes"))
            AssetDatabase.CreateFolder("Assets/JustDance", "Scenes");
    }

    static void AddToBuildSettings(string scenePath)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);

        foreach (var s in scenes)
            if (s.path == scenePath) return; // já existe

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
#endif
