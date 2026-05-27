using UnityEngine;
using System.Collections;

/// <summary>
/// Gerencia o feed da câmera Insta360 Link (aparece como webcam USB no Windows).
/// Exibe o preview em um RawImage e fornece o nome do dispositivo para o FFmpeg.
/// </summary>
public class Insta360Capture : MonoBehaviour
{
    public static Insta360Capture Instance { get; private set; }

    [Header("Preview")]
    public UnityEngine.UI.RawImage previewImage;

    [Header("Configuração")]
    [Tooltip("Nome parcial do dispositivo preferido.")]
    public string deviceNameHint = "Insta360";

    [Tooltip("Nome parcial da câmera a evitar (usada só como último recurso).")]
    public string excludeNameHint = "Kinect";

    [Tooltip("Resolução de captura preferida")]
    public int preferredWidth  = 1920;
    public int preferredHeight = 1080;
    public int preferredFPS    = 30;

    public WebCamTexture CamTexture { get; private set; }
    public string DeviceName        { get; private set; }
    public bool IsReady => CamTexture != null && CamTexture.isPlaying;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        StartCoroutine(InitCamera());
    }

    IEnumerator InitCamera()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("[Insta360] Nenhuma câmera encontrada.");
            yield break;
        }

        DeviceName = null;
        string kinectFallback = null;

        foreach (var d in devices)
        {
            Debug.Log($"[Insta360] Câmera disponível: {d.name}");

            bool isExcluded = !string.IsNullOrEmpty(excludeNameHint) &&
                              d.name.Contains(excludeNameHint, System.StringComparison.OrdinalIgnoreCase);

            if (d.name.Contains(deviceNameHint, System.StringComparison.OrdinalIgnoreCase))
            {
                // Câmera preferida encontrada — usa imediatamente
                DeviceName = d.name;
                break;
            }

            if (isExcluded)
            {
                // Guarda como último recurso mas não usa ainda
                if (kinectFallback == null) kinectFallback = d.name;
            }
            else if (DeviceName == null)
            {
                // Qualquer câmera não-excluída vira candidata
                DeviceName = d.name;
            }
        }

        // Último recurso: só Kinect disponível
        if (DeviceName == null)
        {
            DeviceName = kinectFallback;
            Debug.LogWarning($"[Insta360] Apenas câmera excluída ({excludeNameHint}) disponível. Usando: {DeviceName}");
        }
        else if (!DeviceName.Contains(deviceNameHint, System.StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"[Insta360] '{deviceNameHint}' não encontrado. Usando: {DeviceName}");
        }

        CamTexture = new WebCamTexture(DeviceName, preferredWidth, preferredHeight, preferredFPS);
        if (previewImage != null) previewImage.texture = CamTexture;

        CamTexture.Play();
        Debug.Log($"[Insta360] Câmera iniciada: {DeviceName} {CamTexture.width}x{CamTexture.height}");
    }

    public void StopCamera()
    {
        CamTexture?.Stop();
    }

    void OnDestroy()
    {
        StopCamera();
    }

    /// <summary>
    /// Retorna o nome do dispositivo formatado para uso no FFmpeg:
    /// -f dshow -i video="Insta360 Link"
    /// </summary>
    public string GetFFmpegDeviceArg()
    {
        return string.IsNullOrEmpty(DeviceName) ? "" : $"video=\"{DeviceName}\"";
    }
}
