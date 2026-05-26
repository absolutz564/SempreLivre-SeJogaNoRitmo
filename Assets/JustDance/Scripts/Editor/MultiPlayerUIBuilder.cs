#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Adiciona à cena ativa:
///   • PlayerSelectModal  — modal de seleção 1P / 2P
///   • 2× PlayerHUD       — painéis de pontuação per-player (esquerdo / direito)
///   • ResultsPanel2P     — tela de resultado para 2 jogadores
///   • Segundo ScoreManager no GameObject [Game]
///   • Wiring automático de DanceController.scoreManagers e DanceHUD.playerHUDs
///
/// Menu: JustDance > 6. Configurar Multi-Jogador
/// </summary>
public static class MultiPlayerUIBuilder
{
    [MenuItem("JustDance/6. Configurar Multi-Jogador", priority = 6)]
    public static void Build()
    {
        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null) { EditorUtility.DisplayDialog("Erro", "Nenhum Canvas encontrado na cena.", "OK"); return; }

        var hud = Object.FindObjectOfType<DanceHUD>();
        if (hud == null) { EditorUtility.DisplayDialog("Erro", "DanceHUD não encontrado. Execute primeiro 'JustDance > 3. Montar HUD'.", "OK"); return; }

        var ctrl = Object.FindObjectOfType<DanceController>();
        if (ctrl == null) { EditorUtility.DisplayDialog("Erro", "DanceController não encontrado.", "OK"); return; }

        var canvasRect = canvas.GetComponent<RectTransform>();

        BuildPlayerSelectModal(canvas.transform, ctrl, canvasRect);
        var huds = BuildPlayerHUDs(hud.transform, canvasRect);
        BuildResultsPanel2P(hud.transform, canvasRect);
        WireScoreManagers(ctrl);
        WireHUD(hud, huds, ctrl);

        EditorUtility.SetDirty(hud);
        EditorUtility.SetDirty(ctrl);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("JustDance ✓",
            "Multi-Jogador configurado!\n\n" +
            "O que foi criado:\n" +
            "• PlayerSelectModal  — sobrepõe a tela no início\n" +
            "• PlayerHUD_P1 / P2  — painéis de score/rating\n" +
            "• ResultsPanel2P     — tela final para 2 jogadores\n\n" +
            "Reconecte o botão 'Jogar' para chamar\n" +
            "PlayerSelectModal.Show() em vez de DanceController.StartDance().", "OK");
    }

    // ── Modal de seleção ──────────────────────────────────────────────────────

    static void BuildPlayerSelectModal(Transform canvasParent, DanceController ctrl, RectTransform canvasRect)
    {
        if (canvasParent.Find("PlayerSelectModal") != null) return;

        // Fundo escuro cobrindo tela toda
        var root = NewRect("PlayerSelectModal", canvasParent);
        Stretch(root);
        root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        // Título
        var titleGO = NewRect("Title", root);
        titleGO.anchorMin = new Vector2(0.2f, 0.6f);
        titleGO.anchorMax = new Vector2(0.8f, 0.78f);
        titleGO.offsetMin = titleGO.offsetMax = Vector2.zero;
        var titleTmp = titleGO.gameObject.AddComponent<TextMeshProUGUI>();
        titleTmp.text      = "QUANTOS JOGADORES?";
        titleTmp.fontSize  = 52f;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color     = Color.white;
        titleTmp.fontStyle = FontStyles.Bold;

        // Botão 1 Jogador
        var btn1 = BuildModalButton("Btn1Player", root,
            new Vector2(0.08f, 0.32f), new Vector2(0.46f, 0.58f),
            "1 JOGADOR", new Color(0.2f, 0.55f, 1f));

        // Botão 2 Jogadores
        var btn2 = BuildModalButton("Btn2Players", root,
            new Vector2(0.54f, 0.32f), new Vector2(0.92f, 0.58f),
            "2 JOGADORES", new Color(1f, 0.45f, 0.1f));

        var modal = root.gameObject.AddComponent<PlayerSelectModal>();
        modal.danceController = ctrl;
        modal.btn1Player      = btn1;
        modal.btn2Players     = btn2;
    }

    static Button BuildModalButton(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax, string label, Color color)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;

        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;

        var btn = rt.gameObject.AddComponent<Button>();
        var cb  = new ColorBlock
        {
            normalColor      = color,
            highlightedColor = color * 1.2f,
            pressedColor     = color * 0.8f,
            selectedColor    = color,
            colorMultiplier  = 1f,
            fadeDuration     = 0.1f,
        };
        btn.colors = cb;

        var txtGO = NewRect("Label", rt);
        Stretch(txtGO);
        var tmp = txtGO.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 38f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        return btn;
    }

    // ── PlayerHUDs ────────────────────────────────────────────────────────────

    static PlayerHUD[] BuildPlayerHUDs(Transform hudParent, RectTransform canvasRect)
    {
        var huds = new PlayerHUD[2];

        // Posições: P1 no terço esquerdo, P2 no terço direito
        float[,] anchors = {
            { 0.0f, 0.72f, 0.30f, 1.0f },   // P1
            { 0.70f, 0.72f, 1.0f, 1.0f },   // P2
        };
        string[] labels = { "JOGADOR 1", "JOGADOR 2" };
        Color[]  colors = { new Color(0.2f, 0.55f, 1f, 0.7f), new Color(1f, 0.45f, 0.1f, 0.7f) };

        for (int i = 0; i < 2; i++)
        {
            string pName = $"PlayerHUD_P{i + 1}";
            var existing = hudParent.Find(pName);
            if (existing != null) { huds[i] = existing.GetComponent<PlayerHUD>(); continue; }

            var root = NewRect(pName, hudParent);
            root.anchorMin  = new Vector2(anchors[i, 0], anchors[i, 1]);
            root.anchorMax  = new Vector2(anchors[i, 2], anchors[i, 3]);
            root.offsetMin  = root.offsetMax = Vector2.zero;

            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.4f);

            // Label do jogador
            var lblGO = NewRect("Label", root);
            lblGO.anchorMin = new Vector2(0f, 0.72f);
            lblGO.anchorMax = new Vector2(1f, 1.0f);
            lblGO.offsetMin = lblGO.offsetMax = Vector2.zero;
            var lblTmp = lblGO.gameObject.AddComponent<TextMeshProUGUI>();
            lblTmp.text      = labels[i];
            lblTmp.fontSize  = 18f;
            lblTmp.alignment = TextAlignmentOptions.Center;
            lblTmp.color     = colors[i];
            lblTmp.fontStyle = FontStyles.Bold;

            // Score
            var scoreGO = NewRect("Score", root);
            scoreGO.anchorMin = new Vector2(0f, 0.40f);
            scoreGO.anchorMax = new Vector2(1f, 0.72f);
            scoreGO.offsetMin = scoreGO.offsetMax = Vector2.zero;
            var scoreTmp = scoreGO.gameObject.AddComponent<TextMeshProUGUI>();
            scoreTmp.text      = "0";
            scoreTmp.fontSize  = 36f;
            scoreTmp.alignment = TextAlignmentOptions.Center;
            scoreTmp.color     = Color.white;
            scoreTmp.fontStyle = FontStyles.Bold;

            // Rating (Image — sprite trocado em runtime por PlayerHUD)
            var ratingGO  = NewRect("Rating", root);
            ratingGO.anchorMin = new Vector2(0.1f, 0.10f);
            ratingGO.anchorMax = new Vector2(0.9f, 0.42f);
            ratingGO.offsetMin = ratingGO.offsetMax = Vector2.zero;
            var ratingImg = ratingGO.gameObject.AddComponent<Image>();
            ratingImg.color           = Color.white;
            ratingImg.preserveAspect  = true;
            ratingGO.gameObject.SetActive(false);

            // Pose slider
            var sliderGO = NewRect("PoseSlider", root);
            sliderGO.anchorMin = new Vector2(0.05f, 0.0f);
            sliderGO.anchorMax = new Vector2(0.95f, 0.12f);
            sliderGO.offsetMin = sliderGO.offsetMax = Vector2.zero;
            var slider = sliderGO.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value    = 0f;

            var playerHud = root.gameObject.AddComponent<PlayerHUD>();
            playerHud.playerLabel    = lblTmp;
            playerHud.scoreText      = scoreTmp;
            playerHud.ratingImage    = ratingImg;
            playerHud.livePoseSlider = slider;

            huds[i] = playerHud;

            // P2 começa desativado
            if (i == 1) root.gameObject.SetActive(false);
        }

        return huds;
    }

    // ── ResultsPanel 2P ───────────────────────────────────────────────────────

    static void BuildResultsPanel2P(Transform hudParent, RectTransform canvasRect)
    {
        if (hudParent.Find("ResultsPanel2P") != null) return;

        var root = NewRect("ResultsPanel2P", hudParent);
        root.anchorMin = new Vector2(0.1f, 0.15f);
        root.anchorMax = new Vector2(0.9f, 0.88f);
        root.offsetMin = root.offsetMax = Vector2.zero;

        var bg = root.gameObject.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.95f);

        // Título "RESULTADO"
        AddLabel(root, "Title", "RESULTADO", new Vector2(0.1f, 0.80f), new Vector2(0.9f, 1.0f), 42f, Color.white);

        // Vencedor
        var winnerGO = NewRect("WinnerText", root);
        winnerGO.anchorMin = new Vector2(0.1f, 0.63f);
        winnerGO.anchorMax = new Vector2(0.9f, 0.82f);
        winnerGO.offsetMin = winnerGO.offsetMax = Vector2.zero;
        var winnerTmp = winnerGO.gameObject.AddComponent<TextMeshProUGUI>();
        winnerTmp.text      = "";
        winnerTmp.fontSize  = 34f;
        winnerTmp.alignment = TextAlignmentOptions.Center;
        winnerTmp.color     = new Color(1f, 0.8f, 0f);
        winnerTmp.fontStyle = FontStyles.Bold;

        // Colunas P1 / P2
        AddLabel(root, "LabelP1", "JOGADOR 1", new Vector2(0.05f, 0.50f), new Vector2(0.48f, 0.64f), 22f, new Color(0.2f, 0.55f, 1f));
        var scoreP1GO = NewRect("ScoreP1", root);
        scoreP1GO.anchorMin = new Vector2(0.05f, 0.30f); scoreP1GO.anchorMax = new Vector2(0.48f, 0.52f);
        scoreP1GO.offsetMin = scoreP1GO.offsetMax = Vector2.zero;
        var scoreP1Tmp = scoreP1GO.gameObject.AddComponent<TextMeshProUGUI>();
        scoreP1Tmp.text = "0"; scoreP1Tmp.fontSize = 38f;
        scoreP1Tmp.alignment = TextAlignmentOptions.Center; scoreP1Tmp.color = Color.white;
        var gradeP1GO = NewRect("GradeP1", root);
        gradeP1GO.anchorMin = new Vector2(0.05f, 0.13f); gradeP1GO.anchorMax = new Vector2(0.48f, 0.32f);
        gradeP1GO.offsetMin = gradeP1GO.offsetMax = Vector2.zero;
        var gradeP1Tmp = gradeP1GO.gameObject.AddComponent<TextMeshProUGUI>();
        gradeP1Tmp.text = "-"; gradeP1Tmp.fontSize = 32f;
        gradeP1Tmp.alignment = TextAlignmentOptions.Center; gradeP1Tmp.color = Color.yellow;

        AddLabel(root, "LabelP2", "JOGADOR 2", new Vector2(0.52f, 0.50f), new Vector2(0.95f, 0.64f), 22f, new Color(1f, 0.45f, 0.1f));
        var scoreP2GO = NewRect("ScoreP2", root);
        scoreP2GO.anchorMin = new Vector2(0.52f, 0.30f); scoreP2GO.anchorMax = new Vector2(0.95f, 0.52f);
        scoreP2GO.offsetMin = scoreP2GO.offsetMax = Vector2.zero;
        var scoreP2Tmp = scoreP2GO.gameObject.AddComponent<TextMeshProUGUI>();
        scoreP2Tmp.text = "0"; scoreP2Tmp.fontSize = 38f;
        scoreP2Tmp.alignment = TextAlignmentOptions.Center; scoreP2Tmp.color = Color.white;
        var gradeP2GO = NewRect("GradeP2", root);
        gradeP2GO.anchorMin = new Vector2(0.52f, 0.13f); gradeP2GO.anchorMax = new Vector2(0.95f, 0.32f);
        gradeP2GO.offsetMin = gradeP2GO.offsetMax = Vector2.zero;
        var gradeP2Tmp = gradeP2GO.gameObject.AddComponent<TextMeshProUGUI>();
        gradeP2Tmp.text = "-"; gradeP2Tmp.fontSize = 32f;
        gradeP2Tmp.alignment = TextAlignmentOptions.Center; gradeP2Tmp.color = Color.yellow;

        // Wire no DanceHUD
        var hud = Object.FindObjectOfType<DanceHUD>();
        if (hud != null)
        {
            hud.resultsPanel2P  = root.gameObject;
            hud.finalScoreP1Text = scoreP1Tmp;
            hud.gradeP1Text      = gradeP1Tmp;
            hud.finalScoreP2Text = scoreP2Tmp;
            hud.gradeP2Text      = gradeP2Tmp;
            hud.winnerText       = winnerTmp;
        }

        root.gameObject.SetActive(false);
    }

    // ── Wiring ────────────────────────────────────────────────────────────────

    static void WireScoreManagers(DanceController ctrl)
    {
        // Garante que existem 2 ScoreManagers no mesmo GameObject do controller
        var existing = ctrl.GetComponents<ScoreManager>();
        if (existing.Length == 0)
        {
            ctrl.gameObject.AddComponent<ScoreManager>();
            existing = ctrl.GetComponents<ScoreManager>();
        }
        if (existing.Length < 2)
        {
            ctrl.gameObject.AddComponent<ScoreManager>();
            existing = ctrl.GetComponents<ScoreManager>();
        }
        ctrl.scoreManagers = new ScoreManager[] { existing[0], existing[1] };
    }

    static void WireHUD(DanceHUD hud, PlayerHUD[] huds, DanceController ctrl)
    {
        hud.playerHUDs = huds;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.AddComponent<RectTransform>();
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;
    }

    static void AddLabel(RectTransform parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, float fontSize, Color color)
    {
        var rt = NewRect(name, parent);
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color; tmp.fontStyle = FontStyles.Bold;
    }
}
#endif
