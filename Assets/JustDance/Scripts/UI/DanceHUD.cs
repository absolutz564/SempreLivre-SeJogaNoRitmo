using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controla toda a interface do jogo durante a dança.
/// Elementos compartilhados (countdown, mensagem, step preview) ficam aqui.
/// Elementos per-player (score, rating, pose bar) são delegados ao PlayerHUD[].
/// </summary>
public class DanceHUD : MonoBehaviour
{
    // ── Passos ───────────────────────────────────────────────────────────────
    [Header("Passos")]
    public Image           currentStepImage;
    public Image           upcomingStepImage;
    public TextMeshProUGUI upcomingLabel;

    // ── Multi-Jogador ─────────────────────────────────────────────────────────
    [Header("Multi-Jogador — painéis per-player")]
    public PlayerHUD[] playerHUDs;           // [0]=P1  [1]=P2

    // ── Rastreamento ──────────────────────────────────────────────────────────
    [Header("Rastreamento")]
    public GameObject trackingWaitPanel;

    // ── Countdown / Mensagem ─────────────────────────────────────────────────
    [Header("Countdown / Mensagem")]
    public GameObject      countdownPanel;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI messageText;

    // ── Preview Lane ──────────────────────────────────────────────────────────
    [Header("Preview Lane")]
    public StepPreviewLane stepPreviewLane;

    // ── Resultado Final ───────────────────────────────────────────────────────
    [Header("Resultado Final")]
    public GameObject      resultsPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI perfectCountText;
    public TextMeshProUGUI bomCountText;
    public TextMeshProUGUI errorsCountText;
    public TextMeshProUGUI precisionText;
    public Image           resultIconImage;

    // ── Sprites de Performance ────────────────────────────────────────────────
    [Header("Sprites de Performance")]
    public Sprite spriteIncrivel;
    public Sprite spriteMandoubem;
    public Sprite spriteFoibem;
    public Sprite spriteQuasela;
    public Sprite spriteTenteOutravez;

    void Start()
    {
        trackingWaitPanel?.SetActive(false);
        resultsPanel?.SetActive(false);
        countdownPanel?.SetActive(false);
    }

    // ── Setup ─────────────────────────────────────────────────────────────────

    public void SetupForPlayerCount(int count)
    {
        if (playerHUDs == null) return;
        for (int i = 0; i < playerHUDs.Length; i++)
            playerHUDs[i]?.gameObject.SetActive(i < count);
    }

    // ── Per-player ────────────────────────────────────────────────────────────

    public void UpdateScore(int score, int playerIndex = 0)
    {
        if (playerHUDs != null && playerIndex < playerHUDs.Length)
            playerHUDs[playerIndex]?.UpdateScore(score);
    }

    public void ShowLiveScore(float score01, ScoreRating rating, int playerIndex = 0)
    {
        if (playerHUDs != null && playerIndex < playerHUDs.Length)
            playerHUDs[playerIndex]?.ShowLiveScore(score01);
    }

    public void ShowRatingPopup(ScoreRating rating, int playerIndex = 0)
    {
        if (playerHUDs != null && playerIndex < playerHUDs.Length)
            playerHUDs[playerIndex]?.ShowRatingPopup(rating);
    }

    // ── Passos ────────────────────────────────────────────────────────────────

    public void ShowStep(DanceStep step)
    {
        if (currentStepImage != null)
            currentStepImage.sprite = step?.previewSprite;
    }

    public void ShowUpcoming(DanceStep step)
    {
        if (upcomingStepImage != null)
        {
            upcomingStepImage.sprite = step?.previewSprite;
            upcomingStepImage.gameObject.SetActive(step != null);
        }
        if (upcomingLabel != null)
            upcomingLabel.gameObject.SetActive(step != null);
    }

    // ── Countdown / Mensagem ─────────────────────────────────────────────────

    public void ShowCountdown(int number)
    {
        countdownPanel?.SetActive(true);
        if (countdownText) countdownText.text = number.ToString();
    }

    public void HideCountdown() => countdownPanel?.SetActive(false);

    public void ShowMessage(string msg)
    {
        if (messageText) { messageText.text = msg; messageText.gameObject.SetActive(true); }
    }

    public void HideMessage()
    {
        if (messageText) messageText.gameObject.SetActive(false);
    }

    public void ShowTrackingWait() => trackingWaitPanel?.SetActive(true);
    public void HideTrackingWait() => trackingWaitPanel?.SetActive(false);

    // ── Resultados ────────────────────────────────────────────────────────────

    // Agrega scores de todos os jogadores ativos no mesmo painel.
    // Para 2 jogadores o denominador vira passos×2 automaticamente.
    public void ShowResults(ScoreManager[] managers)
    {
        resultsPanel?.SetActive(true);

        int count        = GameSession.PlayerCount;
        int totalScore   = 0;
        int totalPerfeito = 0;
        int totalBom     = 0;
        int totalMiss    = 0;

        for (int p = 0; p < count && managers != null && p < managers.Length; p++)
        {
            var sm = managers[p];
            if (sm == null) continue;
            totalScore    += sm.TotalScore;
            totalPerfeito += sm.PerfeitoCount;
            totalBom      += sm.BomCount;
            totalMiss     += sm.MissCount;
        }

        int   total = totalPerfeito + totalBom + totalMiss;
        float acc   = total > 0 ? (totalPerfeito + totalBom) / (float)total * 100f : 0f;

        if (finalScoreText)   finalScoreText.text   = totalScore.ToString("N0");
        if (perfectCountText) perfectCountText.text = $"x{totalPerfeito}";
        if (bomCountText)     bomCountText.text     = $"x{totalBom}";
        if (errorsCountText)  errorsCountText.text  = $"x{totalMiss}";
        if (precisionText)    precisionText.text    = $"{acc:F0}%";
        ApplyPerformanceIcon(resultIconImage, acc);
    }

    // ── Helper de resultado ───────────────────────────────────────────────────

    void ApplyPerformanceIcon(Image img, float accuracyPct)
    {
        if (img == null) return;
        img.sprite = accuracyPct >= 85f ? spriteIncrivel
                   : accuracyPct >= 65f ? spriteMandoubem
                   : accuracyPct >= 45f ? spriteFoibem
                   : accuracyPct >= 25f ? spriteQuasela
                   :                      spriteTenteOutravez;
        img.gameObject.SetActive(img.sprite != null);
    }
}
