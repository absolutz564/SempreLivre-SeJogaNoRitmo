using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Controla toda a interface do jogo durante a dança:
/// - Passo atual e próximo passo
/// - Score em tempo real
/// - Popup de rating (PERFECT / GREAT / GOOD...)
/// - Countdown
/// - Tela de resultado final
/// </summary>
public class DanceHUD : MonoBehaviour
{
    [Header("Gameplay")]
    public TextMeshProUGUI scoreText;
    public Slider          scoreSlider;
    public Image           currentStepImage;    // Preview sprite do passo atual
    public Image           upcomingStepImage;   // Preview do próximo passo
    public TextMeshProUGUI upcomingLabel;

    [Header("Rating Popup")]
    public TextMeshProUGUI ratingText;
    public Animator        ratingAnimator;      // Tem trigger "Show"

    [Header("Live Pose Bar")]
    public Slider          livePoseSlider;       // Barra verde/vermelha de aderência

    [Header("Countdown")]
    public GameObject     countdownPanel;
    public TextMeshProUGUI countdownText;

    [Header("Mensagem")]
    public TextMeshProUGUI messageText;

    [Header("Preview Lane")]
    public StepPreviewLane stepPreviewLane; // faixa de passos deslizantes estilo Just Dance

    [Header("Resultado Final")]
    public GameObject     resultsPanel;
    public TextMeshProUGUI finalScoreText;
    public TextMeshProUGUI gradeText;
    public TextMeshProUGUI perfectCountText;
    public TextMeshProUGUI greatCountText;
    public TextMeshProUGUI missCountText;

    private static readonly Color[] RatingColors =
    {
        Color.gray,    // Miss
        Color.white,   // Ok
        Color.cyan,    // Good
        Color.yellow,  // Great
        new Color(1f, 0.8f, 0f), // Perfect (gold)
    };

    void Start()
    {
        resultsPanel?.SetActive(false);
        countdownPanel?.SetActive(false);
        if (ratingText) ratingText.gameObject.SetActive(false);
    }

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

    public void ShowLiveScore(float score01, ScoreRating rating)
    {
        if (livePoseSlider) livePoseSlider.value = score01;
    }

    public void ShowRatingPopup(ScoreRating rating)
    {
        if (ratingText == null) return;
        ratingText.gameObject.SetActive(true);
        ratingText.text  = rating.ToString().ToUpper();
        ratingText.color = RatingColors[(int)rating];
        if (ratingAnimator != null) ratingAnimator.SetTrigger("Show");
        StopCoroutine(nameof(HideRatingAfterDelay));
        StartCoroutine(nameof(HideRatingAfterDelay));
    }

    IEnumerator HideRatingAfterDelay()
    {
        yield return new WaitForSeconds(1.2f);
        if (ratingText) ratingText.gameObject.SetActive(false);
    }

    public void UpdateScore(int score)
    {
        if (scoreText) scoreText.text = score.ToString("N0");
    }

    public void ShowCountdown(int number)
    {
        countdownPanel?.SetActive(true);
        if (countdownText) countdownText.text = number.ToString();
    }

    public void HideCountdown()
    {
        countdownPanel?.SetActive(false);
    }

    public void ShowMessage(string msg)
    {
        if (messageText) { messageText.text = msg; messageText.gameObject.SetActive(true); }
    }

    public void HideMessage()
    {
        if (messageText) messageText.gameObject.SetActive(false);
    }

    public void ShowResults(int score, int maxScore)
    {
        resultsPanel?.SetActive(true);
        var sm = ScoreManager.Instance;
        if (finalScoreText) finalScoreText.text = score.ToString("N0");
        if (gradeText)      gradeText.text       = sm?.GetGrade() ?? "-";
        if (perfectCountText) perfectCountText.text = $"PERFECT x{sm?.PerfectCount}";
        if (greatCountText)   greatCountText.text   = $"GREAT x{sm?.GreatCount}";
        if (missCountText)    missCountText.text     = $"MISS x{sm?.MissCount}";
    }
}
