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
    [Tooltip("Nome parcial do dispositivo. Deixar vazio para detectar automaticamente.")]
    public string deviceNameHint = "Insta360";

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

        // Tentar encontrar a Insta360 pelo nome
        DeviceName = null;
        foreach (var d in devices)
        {
            Debug.Log($"[Insta360] Câmera disponível: {d.name}");
            if (d.name.Contains(deviceNameHint, System.StringComparison.OrdinalIgnoreCase))
            {
                DeviceName = d.name;
                break;
            }
        }

        // Fallback: usar primeira câmera disponível
        if (DeviceName == null)
        {
            DeviceName = devices[0].name;
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
