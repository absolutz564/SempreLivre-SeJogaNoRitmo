#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Adiciona os elementos HUD que faltam na cena 'Dance' e liga todos os campos do DanceHUD.
/// Use: JustDance > 3. Montar HUD da Cena Dance
/// </summary>
public static class WireDanceHUDTool
{
    [MenuItem("JustDance/3. Montar HUD da Cena Dance", priority = 3)]
    public static void WireDanceHUD()
    {
        var hudGO = GameObject.Find("HUD");
        if (hudGO == null) { Err("GameObject 'HUD' não encontrado na cena ativa."); return; }

        var hud = hudGO.GetComponent<DanceHUD>();
        if (hud == null) { Err("Componente DanceHUD não encontrado em 'HUD'."); return; }

        // ── upcomingStepImage ────────────────────────────────────────────────
        var upcomingPanel = GameObject.Find("UpcomingPanel");
        if (upcomingPanel != null)
        {
            hud.upcomingStepImage = upcomingPanel.GetComponent<Image>();

            var upLabelTr = upcomingPanel.transform.Find("UpcomingLabel");
            hud.upcomingLabel = upLabelTr != null
                ? upLabelTr.GetComponent<TextMeshProUGUI>()
                : NewTMP(upcomingPanel, "UpcomingLabel", "A SEGUIR", 18,
                         TextAlignmentOptions.Center, Color.white);
        }

        // ── RatingPopup / RatingText ─────────────────────────────────────────
        var ratingPopup = EnsureChild(hudGO, "RatingPopup",
            new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f), Vector2.zero, new Vector2(500, 90));

        var rtTr = ratingPopup.transform.Find("RatingText");
        var ratingTMP = rtTr != null
            ? rtTr.GetComponent<TextMeshProUGUI>()
            : NewTMP(ratingPopup, "RatingText", "PERFECT", 72,
                     TextAlignmentOptions.Center, new Color(1f, 0.82f, 0f), FontStyles.Bold);
        ratingTMP.gameObject.SetActive(false);
        hud.ratingText = ratingTMP;

        // ── CountdownPanel / CountdownText ───────────────────────────────────
        var cdPanel = EnsureChild(hudGO, "CountdownPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 220));
        EnsureImage(cdPanel, new Color(0, 0, 0, 0.5f));

        var cdTr = cdPanel.transform.Find("CountdownText");
        var cdTMP = cdTr != null
            ? cdTr.GetComponent<TextMeshProUGUI>()
            : NewTMP(cdPanel, "CountdownText", "3", 130,
                     TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        cdPanel.SetActive(false);
        hud.countdownPanel  = cdPanel;
        hud.countdownText   = cdTMP;

        // ── MessagePanel / MessageText ───────────────────────────────────────
        var msgPanel = EnsureChild(hudGO, "MessagePanel",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -55), new Vector2(700, 50));

        var msgTr = msgPanel.transform.Find("MessageText");
        hud.messageText = msgTr != null
            ? msgTr.GetComponent<TextMeshProUGUI>()
            : NewTMP(msgPanel, "MessageText", "", 34,
                     TextAlignmentOptions.Center, Color.white);

        // ── ResultsPanel + filhos ────────────────────────────────────────────
        var resultsPanel = EnsureChild(hudGO, "ResultsPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 420));
        EnsureImage(resultsPanel, new Color(0, 0, 0, 0.88f));
        resultsPanel.SetActive(false);
        hud.resultsPanel = resultsPanel;

        hud.finalScoreText   = EnsureTMP(resultsPanel, "FinalScoreText", "0",          80,  Color.yellow, FontStyles.Bold,   new Vector2(0,  110));
        hud.gradeText        = EnsureTMP(resultsPanel, "GradeText",      "S",         100,  Color.white,  FontStyles.Bold,   new Vector2(0,   20));
        hud.perfectCountText = EnsureTMP(resultsPanel, "PerfectCount",   "PERFECT x0", 30,  Color.white,  FontStyles.Normal, new Vector2(-80, -80));
        hud.greatCountText   = EnsureTMP(resultsPanel, "GreatCount",     "GREAT x0",   30,  Color.white,  FontStyles.Normal, new Vector2(-80,-120));
        hud.missCountText    = EnsureTMP(resultsPanel, "MissCount",      "MISS x0",    30,  Color.gray,   FontStyles.Normal, new Vector2(-80,-160));

        // ── BtnJogar ─────────────────────────────────────────────────────────
        if (hudGO.transform.Find("BtnJogar") == null)
        {
            var btn = EnsureChild(hudGO, "BtnJogar",
                new Vector2(0.5f, 0.12f), new Vector2(0.5f, 0.12f), Vector2.zero, new Vector2(260, 64));
            var btnImg = EnsureImage(btn, new Color(0.18f, 0.55f, 1f));
            var b = btn.AddComponent<Button>();
            b.targetGraphic = btnImg;
            NewTMP(btn, "BtnText", "▶  JOGAR", 36,
                   TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            Debug.Log("[JustDance] BtnJogar criado — conecte manualmente: Button.OnClick → DanceController.StartDance()");
        }

        // ── StepPreviewLane — faixa de passos estilo Just Dance ───────────────
        BuildPreviewLane(hudGO, hud);

        EditorUtility.SetDirty(hud);
        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("JustDance ✓",
            "HUD configurado!\n\n" +
            "Falta apenas conectar o botão:\n" +
            "• Selecione 'BtnJogar' na Hierarchy\n" +
            "• Button (Script) > OnClick (+)\n" +
            "• Arraste [Game] → DanceController.StartDance()",
            "OK");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static GameObject EnsureChild(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var tr = parent.transform.Find(name);
        if (tr != null) return tr.gameObject;

        var go   = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = sizeDelta;
        return go;
    }

    static Image EnsureImage(GameObject go, Color color)
    {
        var img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI NewTMP(GameObject parent, string name, string text, float size,
        TextAlignmentOptions align, Color color, FontStyles style = FontStyles.Normal)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = align;
        tmp.color     = color;
        tmp.fontStyle = style;
        return tmp;
    }

    static TextMeshProUGUI EnsureTMP(GameObject parent, string name, string text, float size,
        Color color, FontStyles style, Vector2 anchoredPos)
    {
        var tr = parent.transform.Find(name);
        if (tr != null) return tr.GetComponent<TextMeshProUGUI>();

        var go   = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = new Vector2(580, 70);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = color;
        tmp.fontStyle = style;
        return tmp;
    }

    // ── Preview Lane ─────────────────────────────────────────────────────────

    static void BuildPreviewLane(GameObject hudGO, DanceHUD hud)
    {
        // Faixa horizontal na base da tela (130px de altura)
        var laneGO = EnsureChild(hudGO, "StepPreviewLaneRoot",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 65f), new Vector2(0f, 130f));
        EnsureImage(laneGO, new Color(0f, 0f, 0f, 0.55f));
        if (laneGO.GetComponent<UnityEngine.UI.RectMask2D>() == null)
            laneGO.AddComponent<UnityEngine.UI.RectMask2D>();

        // Anel pulsante no hit zone (40% da largura = ligeiramente à esquerda do centro)
        var ringGO = EnsureChild(laneGO, "HitZoneRing",
            new Vector2(0.4f, 0.5f), new Vector2(0.4f, 0.5f),
            Vector2.zero, new Vector2(110f, 110f));
        var ringImg = EnsureImage(ringGO, new Color(1f, 1f, 1f, 0.28f));

        // Silhueta / pose ativa — posicionada acima da faixa, alinhada com o hit zone
        // anchoredPos.y=260 coloca o centro a ~260px acima da base (acima da faixa de 130px)
        var silGO = EnsureChild(hudGO, "SilhouetteRoot",
            new Vector2(0.4f, 0f), new Vector2(0.4f, 0f),
            new Vector2(0f, 260f), new Vector2(210f, 270f));
        var silImg = EnsureImage(silGO, new Color(0.25f, 0.35f, 0.80f, 0.75f));

        var silLabelTr = silGO.transform.Find("SilhouetteLabel");
        TextMeshProUGUI silLabel;
        if (silLabelTr != null)
            silLabel = silLabelTr.GetComponent<TextMeshProUGUI>();
        else
            silLabel = NewTMP(silGO, "SilhouetteLabel", "POSE AQUI", 26f,
                              TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

        // Componente StepPreviewLane
        var lane = laneGO.GetComponent<StepPreviewLane>()
                   ?? laneGO.AddComponent<StepPreviewLane>();
        lane.laneContainer    = laneGO.GetComponent<RectTransform>();
        lane.hitZoneRing      = ringImg;
        lane.silhouetteImage  = silImg;
        lane.silhouetteLabel  = silLabel;

        hud.stepPreviewLane = lane;
    }

    static void Err(string msg) => EditorUtility.DisplayDialog("WireDanceHUD — Erro", msg, "OK");
}
#endif
