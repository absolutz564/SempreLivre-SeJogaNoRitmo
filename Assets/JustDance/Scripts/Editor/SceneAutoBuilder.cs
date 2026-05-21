#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;

/// <summary>
/// Cria a cena de gameplay completa via menu.
/// Use: JustDance > 1. Criar Cena de Gameplay
/// </summary>
public static class SceneAutoBuilder
{
    [MenuItem("JustDance/1. Criar Cena de Gameplay", priority = 1)]
    public static void BuildGameplayScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ────────────────────────────────────────────────────────────
        var cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        var cam = cameraGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.orthographic = true;
        cameraGO.AddComponent<AudioListener>();

        // ── EventSystem ───────────────────────────────────────────────────────
        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // ── [System] – Kinect + Score ─────────────────────────────────────────
        var systemGO = new GameObject("[System]");
        systemGO.AddComponent<KinectManager>();
        systemGO.AddComponent<KinectBodyTracker>();

        // ── [Camera] – Insta360 ───────────────────────────────────────────────
        var cameraCapGO = new GameObject("[Camera]");
        var insta360 = cameraCapGO.AddComponent<Insta360Capture>();

        // ── [Game] – lógica central ───────────────────────────────────────────
        var gameGO = new GameObject("[Game]");
        var danceCtrl   = gameGO.AddComponent<DanceController>();
        var audioSrc    = gameGO.AddComponent<AudioSource>();
        var recorder    = gameGO.AddComponent<DanceVideoRecorder>();
        var uploader    = gameGO.AddComponent<DanceVideoUploader>();
        var skelVis     = gameGO.AddComponent<SkeletonVisualizer>();

        // Material simples para o SkeletonVisualizer
        var skelMat = new Material(Shader.Find("Hidden/Internal-Colored"));
        skelVis.boneMaterial = skelMat;
        skelVis.kinectProjectionCamera = cam;

        recorder.uploader = uploader;

        danceCtrl.musicSource        = audioSrc;
        danceCtrl.skeletonVisualizer = skelVis;
        danceCtrl.videoRecorder      = recorder;
        var sm0 = gameGO.AddComponent<ScoreManager>();
        var sm1 = gameGO.AddComponent<ScoreManager>();
        danceCtrl.scoreManagers[0] = sm0;
        danceCtrl.scoreManagers[1] = sm1;

        // ── Canvas ────────────────────────────────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler   = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Fundo: feed da câmera Insta360
        var bgGO  = NewUIObject("Background", canvasGO, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var bgImg = bgGO.AddComponent<RawImage>();
        bgImg.color = Color.white;
        insta360.previewImage = bgImg;

        // ── HUD ───────────────────────────────────────────────────────────────
        var hudGO = NewUIObject("HUD", canvasGO, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var hud   = hudGO.AddComponent<DanceHUD>();

        danceCtrl.hud = hud;

        // Ícone passo atual (base esquerda)
        var curStepGO  = NewUIObject("CurrentStepPanel", hudGO,
            new Vector2(0,0), new Vector2(0,0), new Vector2(70,80), new Vector2(120,120));
        var curStepImg = curStepGO.AddComponent<Image>();
        curStepImg.color = new Color(1,1,1,0.85f);
        hud.currentStepImage = curStepImg;

        // Próximo passo (base direita)
        var nextStepGO  = NewUIObject("UpcomingPanel", hudGO,
            new Vector2(1,0), new Vector2(1,0), new Vector2(-70,80), new Vector2(100,100));
        nextStepGO.AddComponent<Image>().color = new Color(1,1,1,0.45f);
        var nextStepImg = nextStepGO.AddComponent<Image>();
        nextStepImg.color = new Color(1,1,1,0.45f);
        hud.upcomingStepImage = nextStepImg;
        hud.upcomingLabel = MakeTMP(nextStepGO, "UpcomingLabel", "A SEGUIR", 18, TextAlignmentOptions.Center, Color.white);

        // Countdown (centro)
        var cdPanel = NewUIObject("CountdownPanel", hudGO,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(220,220));
        cdPanel.AddComponent<Image>().color = new Color(0,0,0,0.5f);
        var cdText = MakeTMP(cdPanel, "CountdownText", "3", 130, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        cdPanel.SetActive(false);
        hud.countdownPanel = cdPanel;
        hud.countdownText  = cdText;

        // Mensagem (topo centro)
        var msgGO  = NewUIObject("MessagePanel", hudGO,
            new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(0,-55), new Vector2(700,50));
        hud.messageText = MakeTMP(msgGO, "MessageText", "", 34, TextAlignmentOptions.Center, Color.white);

        // Resultados (centro, oculto)
        var resultsGO = NewUIObject("ResultsPanel", hudGO,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(640,420));
        var resBg = resultsGO.AddComponent<Image>();
        resBg.color = new Color(0,0,0,0.88f);
        hud.finalScoreText  = MakeTMP(resultsGO,"FinalScoreText","0",          80,TextAlignmentOptions.Center,Color.yellow,FontStyles.Bold, new Vector2(0, 110));
        hud.gradeText       = MakeTMP(resultsGO,"GradeText",     "S",         100,TextAlignmentOptions.Center,Color.white, FontStyles.Bold, new Vector2(0, 20));
        hud.perfectCountText= MakeTMP(resultsGO,"PerfectCount",  "PERFECT x0", 30,TextAlignmentOptions.Left,  Color.white, FontStyles.Normal, new Vector2(-80,-80));
        hud.greatCountText  = MakeTMP(resultsGO,"GreatCount",    "GREAT x0",   30,TextAlignmentOptions.Left,  Color.white, FontStyles.Normal, new Vector2(-80,-120));
        hud.missCountText   = MakeTMP(resultsGO,"MissCount",     "MISS x0",    30,TextAlignmentOptions.Left,  Color.gray,  FontStyles.Normal, new Vector2(-80,-160));
        resultsGO.SetActive(false);
        hud.resultsPanel = resultsGO;

        // Botão Jogar
        var btnGO  = NewUIObject("BtnJogar", hudGO,
            new Vector2(0.5f,0.12f), new Vector2(0.5f,0.12f), Vector2.zero, new Vector2(260,64));
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.18f,0.55f,1f);
        var btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        MakeTMP(btnGO,"BtnText","▶  JOGAR",36,TextAlignmentOptions.Center,Color.white,FontStyles.Bold);

        // ── Preview Lane — faixa de passos estilo Just Dance ──────────────────
        // Faixa horizontal na base (130px)
        var laneGO = NewUIObject("StepPreviewLaneRoot", hudGO,
            new Vector2(0f,0f), new Vector2(1f,0f), new Vector2(0f,65f), new Vector2(0f,130f));
        laneGO.AddComponent<Image>().color = new Color(0f,0f,0f,0.55f);
        laneGO.AddComponent<UnityEngine.UI.RectMask2D>();

        // Anel do hit zone (40% = levemente à esquerda do centro)
        var ringGO  = NewUIObject("HitZoneRing", laneGO,
            new Vector2(0.4f,0.5f), new Vector2(0.4f,0.5f), Vector2.zero, new Vector2(110f,110f));
        var ringImg = ringGO.AddComponent<Image>();
        ringImg.color = new Color(1f,1f,1f,0.28f);

        // Silhueta acima da faixa, alinhada com o hit zone
        var silGO   = NewUIObject("SilhouetteRoot", hudGO,
            new Vector2(0.4f,0f), new Vector2(0.4f,0f), new Vector2(0f,260f), new Vector2(210f,270f));
        var silImg  = silGO.AddComponent<Image>();
        silImg.color = new Color(0.25f,0.35f,0.80f,0.75f);
        var silLabel = MakeTMP(silGO,"SilhouetteLabel","POSE AQUI",26f,
            TextAlignmentOptions.Center,Color.white,FontStyles.Bold);

        var lane = laneGO.AddComponent<StepPreviewLane>();
        lane.laneContainer   = laneGO.GetComponent<RectTransform>();
        lane.hitZoneRing     = ringImg;
        lane.silhouetteImage = silImg;
        lane.silhouetteLabel = silLabel;

        hud.stepPreviewLane = lane;

        // ── Salvar cena ───────────────────────────────────────────────────────
        string scenePath = "Assets/JustDance/Scenes/Gameplay.unity";
        EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene(), scenePath);
        AssetDatabase.Refresh();

        Debug.Log("[JustDance] Cena criada: " + scenePath);
        EditorUtility.DisplayDialog("JustDance ✓",
            "Cena criada!\n\n" +
            "Próximo passo:\n" +
            "JustDance  ▶  2. Criar Dados de Teste (3 Passos)", "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject NewUIObject(string name, GameObject parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin     = anchorMin;
        rect.anchorMax     = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta     = sizeDelta;
        return go;
    }

    static TextMeshProUGUI MakeTMP(GameObject parent, string name, string text, float size,
        TextAlignmentOptions align, Color color,
        FontStyles style = FontStyles.Normal, Vector2? localPos = null)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        if (localPos.HasValue) { rect.anchorMin = rect.anchorMax = new Vector2(0.5f,0.5f); rect.anchoredPosition = localPos.Value; rect.sizeDelta = new Vector2(580,70); }
        else rect.sizeDelta = Vector2.zero;
        var tmp  = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = align;
        tmp.color     = color;
        tmp.fontStyle = style;
        return tmp;
    }
}
#endif
