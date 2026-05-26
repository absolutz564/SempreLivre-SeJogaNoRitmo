using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Painel de HUD de um único jogador: pontuação, popup de rating (imagem) e barra de pose ao vivo.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("Identidade")]
    public TextMeshProUGUI playerLabel;

    [Header("Pontuação")]
    public TextMeshProUGUI scoreText;

    [Header("Rating Popup — imagens")]
    public Image   ratingImage;        // Image que exibe o sprite do rating
    public Sprite  spriteBom;
    public Sprite  spritePerfeito;
    public Animator ratingAnimator;    // opcional — acione trigger "Show"

    [Header("Pose ao vivo")]
    public Slider livePoseSlider;

    void Start()
    {
        if (ratingImage) ratingImage.gameObject.SetActive(false);
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
        if (ratingImage == null || rating == ScoreRating.Miss)
        {
            if (ratingImage) ratingImage.gameObject.SetActive(false);
            return;
        }

        ratingImage.sprite = rating == ScoreRating.Perfeito ? spritePerfeito : spriteBom;
        ratingImage.gameObject.SetActive(true);
        ratingAnimator?.SetTrigger("Show");
        StopCoroutine(nameof(HideRating));
        StartCoroutine(nameof(HideRating));
    }

    IEnumerator HideRating()
    {
        yield return new WaitForSeconds(1.2f);
        if (ratingImage) ratingImage.gameObject.SetActive(false);
    }
}
