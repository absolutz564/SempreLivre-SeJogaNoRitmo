using UnityEngine;

/// <summary>
/// Posiciona e rotaciona o Quad de captura para que o CameraRecorder
/// grave o conteúdo da webcam na mesma orientação do RawImageRotator.
/// </summary>
public class CaptureQuadRotator : MonoBehaviour
{
    [Header("Referências")]
    public RenderTexture captureRT;
    public RawImageRotator rotator;

    [Tooltip("Deve ser igual ao Orthographic Size do CameraRecorder")]
    public float orthoSize = 1f;

    void Start()
    {
        int steps = rotator != null
            ? rotator.CurrentSteps
            : PlayerPrefs.GetInt("RawImageRotator_Steps", 0);

        float aspect = (float)captureRT.width / captureRT.height; // ex: 1080/1920 = 0.5625
        float visH   = 2f * orthoSize;
        float visW   = visH * aspect;

        // Se há rotação de 90° ou 270°, o Quad precisa ter as dimensões invertidas
        // para preencher o frame após a rotação
        float quadW = (steps % 2 == 0) ? visW : visH;
        float quadH = (steps % 2 == 0) ? visH : visW;

        transform.localScale       = new Vector3(quadW, quadH, 1f);
        transform.localEulerAngles = new Vector3(0f, 0f, steps * 90f);

        Debug.Log($"[CaptureQuad] steps={steps} scale=({quadW:F3},{quadH:F3}) rot={steps * 90f}°");
    }
}
