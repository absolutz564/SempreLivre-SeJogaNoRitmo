using UnityEngine;

public class PulseEffect : MonoBehaviour
{
    [Tooltip("Escala mínima do pulso")]
    public float minScale = 0.95f;

    [Tooltip("Escala máxima do pulso")]
    public float maxScale = 1.05f;

    [Tooltip("Ciclos por segundo")]
    public float speed = 1.2f;

    private Vector3 baseScale;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    private void OnEnable()
    {
        transform.localScale = baseScale;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * speed * Mathf.PI * 2f) + 1f) / 2f;
        float s = Mathf.Lerp(minScale, maxScale, t);
        transform.localScale = baseScale * s;
    }

    private void OnDisable()
    {
        transform.localScale = baseScale;
    }
}
