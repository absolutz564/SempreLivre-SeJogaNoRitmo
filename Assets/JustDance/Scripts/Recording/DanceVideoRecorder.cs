using UnityEngine;
using System.IO;
using System.Collections;
using FfmpegUnity;
using System;
using Nexweron.WebCamPlayer;

public class DanceVideoRecorder : MonoBehaviour
{
    [Header("FfmpegUnity")]
    public FfmpegCaptureCommand ffmpegCapture;

    [Header("Upload")]
    public DanceVideoUploader uploader;

    [Header("Arquivo de saída")]
    public string outputFolder = "ExportedVideos";
    public string filePrefix   = "dance";

    [Header("Webcam")]
    [Tooltip("Referência ao WebCamStream — reinicia a câmera após a gravação liberar o dispositivo")]
    public WebCamStream webCamStream;

    public bool   IsRecording   { get; private set; }
    public string LastVideoPath { get; private set; }

    void Awake()
    {
        string dir = Path.Combine(Application.persistentDataPath, outputFolder);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
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
        webCamStream.Stop();

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

        if (uploader != null)
            uploader.UploadVideo(LastVideoPath);
        else
            UnityEngine.Debug.LogWarning("[Recorder] uploader não definido.");

        if (webCamStream != null)
        {
            webCamStream.Stop();
            UnityEngine.Debug.Log("[Recorder] Webcam reiniciada após gravação.");
        }
    }

    void OnDestroy()
    {
        if (IsRecording)
            ffmpegCapture.StopFfmpeg();
    }
}
