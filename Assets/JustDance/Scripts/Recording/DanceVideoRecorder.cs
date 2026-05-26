using UnityEngine;
using System.IO;
using System.Collections;
using FfmpegUnity;
using System;

public class DanceVideoRecorder : MonoBehaviour
{
    [Header("FfmpegUnity")]
    public FfmpegCaptureCommand ffmpegCapture;

    [Header("Upload")]
    public DanceVideoUploader uploader;

    [Header("Arquivo de saída")]
    public string outputFolder = "ExportedVideos";
    public string filePrefix   = "dance";

    [Header("Moldura (overlay no vídeo final)")]
    public Sprite molduraSprite;

    public bool   IsRecording   { get; private set; }
    public string LastVideoPath { get; private set; }

    private string _overlayPngPath;
    private bool   _overlayReady;
    private bool   _overlaySuccess;

    void Awake()
    {
        string dir = Path.Combine(Application.persistentDataPath, outputFolder);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        PrepareMoldura();
    }

    void PrepareMoldura()
    {
        if (molduraSprite == null) return;

        _overlayPngPath = Path.Combine(Application.persistentDataPath, "moldura_overlay.png");

        try
        {
            var src = molduraSprite.texture;
            var rt  = RenderTexture.GetTemporary(src.width, src.height, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            File.WriteAllBytes(_overlayPngPath, tex.EncodeToPNG());
            Destroy(tex);
            _overlayReady = true;
            UnityEngine.Debug.Log($"[Recorder] Moldura exportada: {_overlayPngPath}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"[Recorder] Falha ao exportar moldura: {e.Message}");
            _overlayReady = false;
        }
    }

    public void StartRecording()
    {
        if (IsRecording)
        {
            UnityEngine.Debug.LogWarning("[Recorder] Já está gravando.");
            return;
        }

        if (ffmpegCapture == null)
        {
            UnityEngine.Debug.LogError("[Recorder] FfmpegCaptureCommand não configurado.");
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        LastVideoPath = Path.Combine(
            Application.persistentDataPath,
            outputFolder,
            $"{filePrefix}_{timestamp}.mp4"
        ).Replace('\\', '/');

        string originalOptions = ffmpegCapture.CaptureOptions;
        UnityEngine.Debug.Log(ffmpegCapture.CaptureOptions);

        if (originalOptions.Contains("{PERSISTENT_DATA_PATH}/capture.mp4"))
        {
            ffmpegCapture.CaptureOptions = originalOptions.Replace(
                "{PERSISTENT_DATA_PATH}/capture.mp4",
                LastVideoPath
            );
        }
        else
        {
            UnityEngine.Debug.LogWarning("[Recorder] Placeholder não encontrado, usando fallback.");
            ffmpegCapture.CaptureOptions += $" \"{LastVideoPath}\"";
        }

        ffmpegCapture.PrintStdErr = true;
        ffmpegCapture.gameObject.SetActive(true);

        StartCoroutine(StartNextFrame());

        IsRecording = true;

        UnityEngine.Debug.Log($"[Recorder] Iniciando gravação em: {LastVideoPath}");
    }

    IEnumerator StartNextFrame()
    {
        yield return null;
        ffmpegCapture.StartFfmpeg();
        UnityEngine.Debug.Log("[Recorder] FFmpeg iniciado.");
    }

    public void StopRecording()
    {
        if (!IsRecording) return;
        IsRecording = false;
        StartCoroutine(StopAndProcess());
    }

    IEnumerator StopAndProcess()
    {
        UnityEngine.Debug.Log("[Recorder] Parando gravação...");

        ffmpegCapture.StopFfmpeg();

        float timeout = 0f;
        while (ffmpegCapture.IsRunning && timeout < 15f)
        {
            timeout += Time.deltaTime;
            yield return null;
        }

        UnityEngine.Debug.Log("[Recorder] FFmpeg finalizado.");

        while (!File.Exists(LastVideoPath))
        {
            UnityEngine.Debug.LogWarning("[Recorder] Arquivo ainda não existe...");
            yield return new WaitForSeconds(0.5f);
        }

        UnityEngine.Debug.Log("[Recorder] Arquivo encontrado.");

        bool ready = false;
        while (!ready)
        {
            bool shouldWait = false;
            try
            {
                using (FileStream stream = File.Open(
                    LastVideoPath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    ready = true;
                }
            }
            catch (IOException)
            {
                UnityEngine.Debug.LogWarning("[Recorder] Arquivo ainda em uso...");
                shouldWait = true;
            }

            if (!ready && shouldWait)
                yield return new WaitForSeconds(1f);
        }

        UnityEngine.Debug.Log($"[Recorder] Arquivo pronto ({new FileInfo(LastVideoPath).Length / 1024} KB)");

        // Overlay da moldura antes do upload
        if (_overlayReady && File.Exists(_overlayPngPath))
        {
            string finalPath = LastVideoPath.Replace(".mp4", "_final.mp4");
            UnityEngine.Debug.Log("[Recorder] Aplicando moldura...");
            yield return StartCoroutine(RunOverlayAsync(LastVideoPath, _overlayPngPath, finalPath));

            long finalSize = File.Exists(finalPath) ? new FileInfo(finalPath).Length : 0;
            if (_overlaySuccess && finalSize > 10000)
            {
                File.Delete(LastVideoPath);
                LastVideoPath = finalPath;
                UnityEngine.Debug.Log($"[Recorder] Moldura aplicada: {finalPath} ({finalSize / 1024} KB)");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[Recorder] Overlay falhou (success={_overlaySuccess}, size={finalSize}). Usando vídeo original.");
                if (File.Exists(finalPath)) File.Delete(finalPath); // limpa arquivo parcial
            }
        }

        if (uploader != null)
            uploader.UploadVideo(LastVideoPath);
        else
            UnityEngine.Debug.LogWarning("[Recorder] uploader não definido.");
    }

    IEnumerator RunOverlayAsync(string input, string overlayPng, string output)
    {
        _overlaySuccess = false;

        var cmd = gameObject.AddComponent<FfmpegCommand>();
        cmd.ExecuteOnStart = false;
        cmd.PrintStdErr    = true;

        cmd.Options = $"-y -i \"{input}\" -i \"{overlayPng}\" " +
                      $"-filter_complex \"[0:v][1:v]overlay=0:0,format=yuv420p\" " +
                      $"-c:v libopenh264 -b:v 4000k -maxrate 4000k -bufsize 8000k " +
                      $"-g 30 -pix_fmt yuv420p " +
                      $"-colorspace bt709 -color_trc bt709 -color_primaries bt709 " +
                      $"-map 0:a? -c:a copy \"{output}\"";

        Debug.Log($"[Recorder] Overlay cmd: {cmd.Options}");

        cmd.ExecuteFfmpeg();

        // Aguarda thread do FFmpeg iniciar (IsRunning fica true)
        float startWait = 0f;
        while (!cmd.IsRunning && startWait < 5f)
        {
            startWait += Time.deltaTime;
            yield return null;
        }

        if (!cmd.IsRunning)
        {
            Debug.LogError("[Recorder] Overlay: FFmpeg não iniciou em 5s.");
            Destroy(cmd);
            yield break;
        }

        // Aguarda FFmpeg terminar
        while (cmd.IsRunning)
            yield return null;

        yield return null; // frame extra para ReturnCode ser escrito pela thread

        _overlaySuccess = cmd.ReturnCode == 0;

        if (!_overlaySuccess)
            Debug.LogError($"[Recorder] Overlay retornou código {cmd.ReturnCode} — verifique os logs do FFmpeg acima.");

        Destroy(cmd);
    }

    void OnDestroy()
    {
        if (IsRecording)
            ffmpegCapture.StopFfmpeg();
    }
}
