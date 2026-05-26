using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using System.Collections;
using System.IO;

/// <summary>
/// Envia o vídeo gravado para o servidor.
/// Mesmo padrão do PhotoTaker.cs do projeto PetrobrasVideo.
/// </summary>
public class DanceVideoUploader : MonoBehaviour
{
    [Header("Servidor")]
    public string serverUrl      = "http://SEU_SERVIDOR:3003";
    public string uploadEndpoint = "/agent/participants/upload";
    public string bearerToken    = "";
    public string accessToken    = "";

    [Header("Metadados")]
    public string participantName = "";
    public string sessionId       = "";

    [Header("UI Feedback")]
    public GameObject uploadingPanel;
    public GameObject successPanel;
    public UnityEngine.UI.Image qrCodeImage;

    [Header("Preview de Vídeo (PanelVideoSended)")]
    public UnityEngine.UI.RawImage videoPreviewImage;
    public AudioSource videoAudioSource;

    public bool IsUploading { get; private set; }

    private VideoPlayer _previewPlayer;
    private RenderTexture _previewRT;

    public void UploadVideo(string videoFilePath)
    {
        if (!File.Exists(videoFilePath))
        {
            Debug.LogError($"[Uploader] Arquivo não encontrado: {videoFilePath}");
            return;
        }
        StartCoroutine(UploadRoutine(videoFilePath));
    }

    IEnumerator UploadRoutine(string videoFilePath)
    {
        // Mostra preview imediatamente, independente do upload
        successPanel?.SetActive(true);
        PlayPreviewLoop(videoFilePath);

        if (string.IsNullOrEmpty(serverUrl) || serverUrl.Contains("SEU_SERVIDOR"))
        {
            Debug.LogWarning("[Uploader] Servidor não configurado — exibindo apenas preview local.");
            yield break;
        }

        IsUploading = true;
        uploadingPanel?.SetActive(true);

        byte[] videoBytes = File.ReadAllBytes(videoFilePath);
        Debug.Log($"[Uploader] Enviando {videoBytes.Length / 1024} KB para {serverUrl}{uploadEndpoint}");

        WWWForm form = new WWWForm();
        form.AddField("isFileIdentify", "false");
        form.AddField("identify",       "false");
        form.AddField("session_id",     sessionId);
        form.AddField("participant",    participantName);
        form.AddBinaryData("file", videoBytes, Path.GetFileName(videoFilePath), "video/mp4");

        using UnityWebRequest request = UnityWebRequest.Post(serverUrl + uploadEndpoint, form);
        request.SetRequestHeader("Authorization", $"Bearer {bearerToken}");
        if (!string.IsNullOrEmpty(accessToken))
            request.SetRequestHeader("access_token", accessToken);

        yield return request.SendWebRequest();

        uploadingPanel?.SetActive(false);
        IsUploading = false;

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[Uploader] Erro: {request.error} | Código: {request.responseCode}");
            Debug.LogError($"[Uploader] Resposta: {request.downloadHandler.text}");
            yield break;
        }

        Debug.Log("[Uploader] Upload concluído com sucesso!");

        // Tentar exibir QR code se servidor retornar (mesmo padrão PetrobrasVideo)
        TryShowQRCode(request.downloadHandler.text);
    }

    void TryShowQRCode(string json)
    {
        if (qrCodeImage == null) return;
        try
        {
            QRResponse resp = JsonUtility.FromJson<QRResponse>(json);
            if (string.IsNullOrEmpty(resp?.qrcode)) return;
            string base64 = resp.qrcode.Contains(",")
                ? resp.qrcode[(resp.qrcode.IndexOf(',') + 1)..]
                : resp.qrcode;
            byte[] bytes = System.Convert.FromBase64String(base64);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(bytes);
            qrCodeImage.sprite = Sprite.Create(tex,
                new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);
            qrCodeImage.gameObject.SetActive(true);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Uploader] QR code não encontrado na resposta: {e.Message}");
        }
    }

    void PlayPreviewLoop(string videoPath)
    {
        if (videoPreviewImage == null) return;

        if (_previewPlayer != null)
        {
            _previewPlayer.Stop();
            Destroy(_previewPlayer);
        }
        if (_previewRT != null)
            _previewRT.Release();

        _previewRT = new RenderTexture(1080, 1920, 0);
        videoPreviewImage.texture = _previewRT;

        _previewPlayer = gameObject.AddComponent<VideoPlayer>();
        _previewPlayer.playOnAwake  = false;
        _previewPlayer.isLooping    = true;
        _previewPlayer.url          = "file://" + videoPath;
        _previewPlayer.targetTexture = _previewRT;

        if (videoAudioSource != null)
        {
            _previewPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            _previewPlayer.SetTargetAudioSource(0, videoAudioSource);
        }
        else
        {
            _previewPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
        }

        _previewPlayer.Play();
        Debug.Log($"[Uploader] Preview iniciado: {videoPath}");
    }

    public void StopPreview()
    {
        if (_previewPlayer != null)
        {
            _previewPlayer.Stop();
            Destroy(_previewPlayer);
            _previewPlayer = null;
        }
    }

    void OnDestroy()
    {
        StopPreview();
        if (_previewRT != null) { _previewRT.Release(); _previewRT = null; }
    }

    [System.Serializable] private class QRResponse { public string qrcode; }
}
