using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Painel de HUD de um único jogador: pontuação, popup de rating e barra de pose ao vivo.
/// Coloque um por jogador na cena; o DanceHUD delega chamadas per-player a eles.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Identidade")]
    public TextMeshProUGUI playerLabel;   // ex.: "JOGADOR 1"

    [Header("Pontuação")]
    public TextMeshProUGUI scoreText;

    [Header("Rating Popup")]
    public TextMeshProUGUI ratingText;
    public Animator        ratingAnimator;

    [Header("Pose ao vivo")]
    public Slider livePoseSlider;

    static readonly string[] Labels = { "MISS", "OK", "GOOD", "GREAT", "PERFECT" };
    static readonly Color[] Colors  =
    {
        Color.gray,
        Color.white,
        Color.cyan,
        Color.yellow,
        new Color(1f, 0.8f, 0f),
    };

    void Start()
    {
        if (ratingText) ratingText.gameObject.SetActive(false);
    }

    public void UpdateScore(int score)
    {
        if (scoreText) scoreText.text = score.ToString("N0");
    }

    public void ShowLiveScore(float score01)
    {
        if (livePoseSlider) livePoseSlider.value = score01;
    }

    public void ShowRatingPopup(ScoreRating rating)
    {
        if (ratingText == null) return;
        ratingText.gameObject.SetActive(true);
        ratingText.text  = Labels[(int)rating];
        ratingText.color = Colors[(int)rating];
        if (ratingAnimator != null) ratingAnimator.SetTrigger("Show");
        StopCoroutine(nameof(HideRating));
        StartCoroutine(nameof(HideRating));
    }

    IEnumerator HideRating()
    {
        yield return new WaitForSeconds(1.2f);
        if (ratingText) ratingText.gameObject.SetActive(false);
    }
}
