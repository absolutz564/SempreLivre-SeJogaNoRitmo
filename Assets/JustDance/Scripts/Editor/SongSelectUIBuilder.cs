#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Cria o modal de seleção de música com 3 cartas (Piseiro, Forró, Xote).
/// Use: JustDance > 7. Criar Menu de Seleção de Música
/// </summary>
public static class SongSelectUIBuilder
{
    static readonly string[] SongNames   = { "PISEIRO", "FORRÓ", "XOTE" };
    static readonly Color[]  CardColors  =
    {
        new Color(0.10f, 0.72f, 0.42f),   // verde  — Piseiro
        new Color(0.85f, 0.22f, 0.16f),   // vermelho — Forró
        new Color(0.18f, 0.45f, 0.90f),   // azul   — Xote
    };

    [MenuItem("JustDance/7. Criar Menu de Seleção de Música", priority = 7)]
    public static void Build()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) { Err("GameObject 'Canvas' não encontrado.\nCrie a cena primeiro: JustDance > 1."); return; }

        var playerModal = Object.FindObjectOfType<PlayerSelectModal>(true);
        if (playerModal == null) { Err("PlayerSelectModal não encontrado na cena.\nExecute: JustDance > 6. Configurar Multi-Jogador"); return; }

        // ── Overlay escuro ────────────────────────────────────────────────────
        var modalGO = EnsureChild(canvas, "SongSelectModal",
            Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        EnsureImage(modalGO, new Color(0f, 0f, 0f, 0.88f));

        // ── Título ────────────────────────────────────────────────────────────
        MakeTMP(modalGO, "Title", "ESCOLHA A MÚSICA",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -90f), new Vector2(900f, 80f),
            60f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

        // ── Cartas das músicas ────────────────────────────────────────────────
        var buttons   = new Button[3];
        var covers    = new Image[3];
        var labels    = new TextMeshProUGUI[3];

        // Distribui 3 cartas horizontalmente centradas (espaçamento de 340px)
        float[] xOffsets = { -340f, 0f, 340f };

        for (int i = 0; i < 3; i++)
        {
            var card = EnsureChild(modalGO, $"Card_{SongNames[i]}",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(xOffsets[i], 30f), new Vector2(290f, 380f));
            EnsureImage(card, new Color(0.12f, 0.12f, 0.12f));

            // Placeholder de capa (60% do topo da carta)
            var coverGO = EnsureChild(card, "Cover",
                new Vector2(0f, 0.38f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero);
            covers[i] = EnsureImage(coverGO, CardColors[i]);

            // Label do nome da música
            labels[i] = MakeTMP(card, "SongName", SongNames[i],
                new Vector2(0f, 0.18f), new Vector2(1f, 0.38f),
                Vector2.zero, Vector2.zero,
                34f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

            // Botão "SELECIONAR"
            var btnGO = EnsureChild(card, "BtnSelect",
                new Vector2(0.1f, 0.03f), new Vector2(0.9f, 0.16f),
                Vector2.zero, Vector2.zero);
            var btnImg = EnsureImage(btnGO, CardColors[i]);
            var btn    = btnGO.GetComponent<Button>() ?? btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            MakeTMP(btnGO, "BtnText", "SELECIONAR",
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                26f, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
            buttons[i] = btn;
        }

        // ── Componente SongSelectMenu ─────────────────────────────────────────
        var menu = modalGO.GetComponent<SongSelectMenu>() ?? modalGO.AddComponent<SongSelectMenu>();
        menu.songButtons        = buttons;
        menu.coverImages        = covers;
        menu.nameLabels         = labels;
        menu.playerSelectModal  = playerModal;
        menu.songs              = new SongSelectMenu.SongEntry[3];
        for (int i = 0; i < 3; i++)
            menu.songs[i] = new SongSelectMenu.SongEntry { songName = SongNames[i] };

        modalGO.SetActive(true);
        EditorUtility.SetDirty(menu);
        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("JustDance ✓",
            "Menu de seleção criado!\n\n" +
            "Passos seguintes no Inspector (SongSelectModal):\n" +
            "• Songs > Element 0/1/2: arraste as DanceChoreography\n" +
            "• Cover Sprite (opcional): arraste imagens de capa\n\n" +
            "No BtnJogar:\n" +
            "• OnClick → SongSelectMenu.Show()", "OK");
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
        var img = go.GetComponent<Image>() ?? go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static TextMeshProUGUI MakeTMP(GameObject parent, string name, string text,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta,
        float size, TextAlignmentOptions align, Color color, FontStyles style)
    {
        var tr  = parent.transform.Find(name);
        var go  = tr != null ? tr.gameObject : new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin        = anchorMin;
        rect.anchorMax        = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta        = sizeDelta;
        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.alignment = align;
        tmp.color     = color;
        tmp.fontStyle = style;
        return tmp;
    }

    static void Err(string msg) => EditorUtility.DisplayDialog("JustDance — Erro", msg, "OK");
}
#endif
