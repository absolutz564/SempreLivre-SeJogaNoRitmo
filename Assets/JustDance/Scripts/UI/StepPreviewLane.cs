using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Faixa de preview estilo Just Dance: cards de passos deslizam da direita até o
/// hit zone (zona de execução), sincronizados com o tempo da música.
/// Lê DanceController.Instance — sem dependências diretas no Inspector além das
/// referências de UI listadas abaixo.
/// </summary>
public class StepPreviewLane : MonoBehaviour
{
    [Header("UI — faixa")]
    public RectTransform   laneContainer;   // strip horizontal (RectMask2D para clip)
    public Image           hitZoneRing;     // anel pulsante no hit zone

    [Header("UI — silhueta (pose atual)")]
    public Image           silhouetteImage; // imagem grande da pose ativa
    public TextMeshProUGUI silhouetteLabel; // nome do passo ativo (placeholder)

    [Header("Configuração")]
    [Range(2f, 10f)]
    public float lookAheadSeconds = 4f;

    [Range(0.1f, 0.9f)]
    [Tooltip("Posição normalizada do hit zone (0=esq, 1=dir). 0.4 = ligeiramente esquerda do centro.")]
    public float hitZoneNorm = 0.4f;

    // ── Pool ───────────────────────────────────────────────────────────────────
    readonly List<StepPreviewItem>                                    _pool     = new();
    readonly Dictionary<DanceChoreography.StepEntry, StepPreviewItem> _active   = new();
    readonly List<DanceChoreography.StepEntry>                        _toRemove = new();
    int _spawnCount;

    static readonly Color[] Palette =
    {
        new Color(0.20f, 0.55f, 1.00f),
        new Color(1.00f, 0.40f, 0.20f),
        new Color(0.20f, 0.85f, 0.35f),
        new Color(0.85f, 0.25f, 0.85f),
        new Color(1.00f, 0.80f, 0.00f),
        new Color(0.25f, 0.85f, 0.85f),
    };

    // ── Unity ─────────────────────────────────────────────────────────────────

    void Update()
    {
        var ctrl = DanceController.Instance;
        if (ctrl == null || !ctrl.IsPlaying) return;

        var choreo = ctrl.Choreography;
        if (choreo?.steps == null || choreo.steps.Length == 0) return;

        float laneW = laneContainer != null ? laneContainer.rect.width : 0f;
        if (laneW < 1f) return; // layout ainda não calculado

        float t = ctrl.MusicTime;
        RefreshItems(t, choreo, laneW);
        RefreshSilhouette(t, choreo);
        PulseHitZone();
    }

    // ── Faixa ─────────────────────────────────────────────────────────────────

    void RefreshItems(float musicTime, DanceChoreography choreo, float laneW)
    {
        // Coordenadas locais no laneContainer (origem = centro do rect)
        float hitX   = (hitZoneNorm - 0.5f) * laneW;          // ex: -192 para 40%
        float rightX =  laneW * 0.5f + 100f;                  // além da borda direita
        float leftX  = -laneW * 0.5f - 100f;                  // além da borda esquerda

        var visited = new HashSet<DanceChoreography.StepEntry>();

        for (int i = 0; i < choreo.steps.Length; i++)
        {
            var entry      = choreo.steps[i];
            float timeLeft = entry.startTime - musicTime;      // > 0 = futuro

            if (timeLeft  >  lookAheadSeconds) continue;  // ainda não visível
            if (timeLeft  < -1.2f)             continue;  // já passou

            visited.Add(entry);

            if (!_active.TryGetValue(entry, out var item))
            {
                item = Acquire();
                item.Init(entry, i);
                _active[entry] = item;
            }

            // Posição X: interpola hit zone → borda direita conforme timeLeft
            float xPos;
            if (timeLeft >= 0f)
                xPos = Mathf.Lerp(hitX, rightX, timeLeft / lookAheadSeconds);
            else
                // Pós-hit: continua leftward até leftX em 1.2s
                xPos = hitX + (timeLeft / 1.2f) * (hitX - leftX);

            item.GetComponent<RectTransform>().anchoredPosition = new Vector2(xPos, 0f);
            item.SetHighlight(Mathf.Abs(timeLeft) < 0.4f);
            item.gameObject.SetActive(true);
        }

        // Devolver itens fora da janela ao pool
        _toRemove.Clear();
        foreach (var kv in _active)
            if (!visited.Contains(kv.Key)) _toRemove.Add(kv.Key);
        foreach (var key in _toRemove)
        {
            Release(_active[key]);
            _active.Remove(key);
        }
    }

    // ── Silhueta ──────────────────────────────────────────────────────────────

    void RefreshSilhouette(float musicTime, DanceChoreography choreo)
    {
        if (silhouetteImage == null) return;

        var active = choreo.GetActiveStep(musicTime);

        if (active?.step?.previewSprite != null)
        {
            silhouetteImage.sprite  = active.step.previewSprite;
            silhouetteImage.color   = Color.white;
            silhouetteImage.enabled = true;
            if (silhouetteLabel) silhouetteLabel.text = active.step.stepName;
        }
        else if (active?.step != null)
        {
            silhouetteImage.sprite  = null;
            silhouetteImage.color   = Palette[StepIndex(choreo, active) % Palette.Length];
            silhouetteImage.enabled = true;
            if (silhouetteLabel) silhouetteLabel.text = active.step.stepName;
        }
        else
        {
            silhouetteImage.enabled = false;
            if (silhouetteLabel) silhouetteLabel.text = string.Empty;
        }
    }

    static int StepIndex(DanceChoreography choreo, DanceChoreography.StepEntry entry)
    {
        for (int i = 0; i < choreo.steps.Length; i++)
            if (choreo.steps[i] == entry) return i;
        return 0;
    }

    // ── Animação do anel ──────────────────────────────────────────────────────

    void PulseHitZone()
    {
        if (hitZoneRing == null) return;
        float s = 0.92f + 0.08f * Mathf.Sin(Time.time * 4.5f);
        hitZoneRing.transform.localScale = Vector3.one * s;
    }

    // ── Pool helpers ──────────────────────────────────────────────────────────

    StepPreviewItem Acquire()
    {
        foreach (var p in _pool)
            if (!p.gameObject.activeSelf) return p;
        return Spawn();
    }

    void Release(StepPreviewItem item) => item.gameObject.SetActive(false);

    StepPreviewItem Spawn()
    {
        // Item root
        var go   = new GameObject($"StepItem_{_spawnCount++}");
        go.transform.SetParent(laneContainer, false);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta        = new Vector2(240f, 240f);
        rect.anchorMin        = new Vector2(0.5f, 0.5f);
        rect.anchorMax        = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        // Background colorido
        var bg = go.AddComponent<Image>();
        bg.color = Color.gray;

        // Imagem da pose (visível apenas quando previewSprite existe)
        var pGO   = new GameObject("PoseImg");
        pGO.transform.SetParent(go.transform, false);
        var pRect = pGO.AddComponent<RectTransform>();
        pRect.anchorMin = new Vector2(0.08f, 0.24f);
        pRect.anchorMax = new Vector2(0.92f, 0.92f);
        pRect.sizeDelta = Vector2.zero;
        var pImg = pGO.AddComponent<Image>();
        pImg.preserveAspect = true;

        // Label com nome do passo
        var lGO   = new GameObject("Label");
        lGO.transform.SetParent(go.transform, false);
        var lRect = lGO.AddComponent<RectTransform>();
        lRect.anchorMin = new Vector2(0f, 0f);
        lRect.anchorMax = new Vector2(1f, 0.28f);
        lRect.sizeDelta = Vector2.zero;
        var lTmp  = lGO.AddComponent<TextMeshProUGUI>();
        lTmp.fontSize   = 11f;
        lTmp.alignment  = TextAlignmentOptions.Center;
        lTmp.color      = Color.white;
        lTmp.fontStyle  = FontStyles.Bold;
        lTmp.overflowMode = TextOverflowModes.Ellipsis;

        var item = go.AddComponent<StepPreviewItem>();
        item.backgroundImage = bg;
        item.poseImage       = pImg;
        item.stepLabel       = lTmp;

        _pool.Add(item);
        return item;
    }
}
