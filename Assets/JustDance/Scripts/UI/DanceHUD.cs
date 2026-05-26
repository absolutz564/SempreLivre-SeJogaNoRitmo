using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

    // ── Countdown / Mensagem ─────────────────────────────────────────────────
    [Header("Countdown / Mensagem")]
    public GameObject      countdownPanel;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI messageText;

    // ── Preview Lane ──────────────────────────────────────────────────────────
    [Header("Preview Lane")]
    public StepPreviewLane stepPreviewLane;

    // ── Resultado Final — 1 Jogador ───────────────────────────────────────────
    [Header("Resultado Final — 1 Jogador")]
    public GameObject      resultsPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI perfectCountText;
    public TextMeshProUGUI greatCountText;
    public TextMeshProUGUI missCountText;

    // ── Resultado Final — 2 Jogadores ─────────────────────────────────────────
    [Header("Resultado Final — 2 Jogadores")]
    public GameObject      resultsPanel2P;
    public TextMeshProUGUI finalScoreP1Text;
    public TextMeshProUGUI gradeP1Text;
    public TextMeshProUGUI finalScoreP2Text;
    public TextMeshProUGUI gradeP2Text;
    public TextMeshProUGUI winnerText;

    void Start()
    {
        resultsPanel?.SetActive(false);
        resultsPanel2P?.SetActive(false);
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

    // ── Resultados ────────────────────────────────────────────────────────────

    public void ShowResults(ScoreManager[] managers)
    {
        int count = GameSession.PlayerCount;

        if (count >= 2 && resultsPanel2P != null)
        {
            resultsPanel2P.SetActive(true);

            var sm0 = managers != null && managers.Length > 0 ? managers[0] : null;
            var sm1 = managers != null && managers.Length > 1 ? managers[1] : null;

            if (finalScoreP1Text) finalScoreP1Text.text = sm0?.TotalScore.ToString("N0") ?? "0";
            if (gradeP1Text)      gradeP1Text.text      = sm0?.GetGrade() ?? "-";
            if (finalScoreP2Text) finalScoreP2Text.text = sm1?.TotalScore.ToString("N0") ?? "0";
            if (gradeP2Text)      gradeP2Text.text      = sm1?.GetGrade() ?? "-";

            if (winnerText)
            {
                int s1 = sm0?.TotalScore ?? 0;
                int s2 = sm1?.TotalScore ?? 0;
                winnerText.text = s1 > s2 ? "JOGADOR 1 VENCEU!"
                               : s2 > s1 ? "JOGADOR 2 VENCEU!"
                               :            "EMPATE!";
            }
        }
        else
        {
            resultsPanel?.SetActive(true);
            var sm = managers != null && managers.Length > 0 ? managers[0] : null;
            if (finalScoreText)   finalScoreText.text   = sm?.TotalScore.ToString("N0") ?? "0";
            if (gradeText)        gradeText.text        = sm?.GetGrade() ?? "-";
            if (perfectCountText) perfectCountText.text = $"PERFEITO x{sm?.PerfeitoCount}";
            if (greatCountText)   greatCountText.text   = $"BOM x{sm?.BomCount}";
            if (missCountText)    missCountText.text     = $"MISS x{sm?.MissCount}";
        }
    }
}
