using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Card visual de um único passo na faixa de preview.
/// Mostra previewSprite do DanceStep ou um disco colorido como placeholder.
/// </summary>
public class StepPreviewItem : MonoBehaviour
{
    [HideInInspector] public DanceChoreography.StepEntry entry;

    public Image               backgroundImage;
    public Image               poseImage;
    public TextMeshProUGUI     stepLabel;

    static readonly Color[] Palette =
    {
        new Color(0.20f, 0.55f, 1.00f),
        new Color(1.00f, 0.40f, 0.20f),
        new Color(0.20f, 0.85f, 0.35f),
        new Color(0.85f, 0.25f, 0.85f),
        new Color(1.00f, 0.80f, 0.00f),
        new Color(0.25f, 0.85f, 0.85f),
    };

    Color _baseColor;
    Color _highlightColor;

    public void Init(DanceChoreography.StepEntry e, int stepIndex)
    {
        entry = e;
        var step = e.step;
        // stepLabel.text = step != null ? step.stepName : "?";

        if (step?.previewSprite != null)
        {
            poseImage.sprite  = step.previewSprite;
            poseImage.enabled = true;
            backgroundImage.color = new Color(0.08f, 0.08f, 0.08f);
            _baseColor      = new Color(0, 0, 0, 0);
            _highlightColor = new Color(1f, 1f, 0.75f, 0.2f);
        }
        else
        {
            poseImage.enabled = false;
            _baseColor        = Palette[stepIndex % Palette.Length];
            _highlightColor   = Color.Lerp(_baseColor, Color.white, 0.40f);
            backgroundImage.color = _baseColor;
        }
    }

    public void SetHighlight(bool on)
    {
        transform.localScale  = on ? Vector3.one * 1.20f : Vector3.one;
        backgroundImage.color = on ? _highlightColor : _baseColor;
    }
}
