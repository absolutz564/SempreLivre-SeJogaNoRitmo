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
    public string filePrefix = "dance";

    public bool IsRecording { get; private set; }
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
            Debug.LogWarning("[Recorder] Já está gravando.");
            return;
        }

        if (ffmpegCapture == null)
        {
            Debug.LogError("[Recorder] FfmpegCaptureCommand não configurado.");
            return;
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        LastVideoPath = Path.Combine(
            Application.persistentDataPath,
            outputFolder,
            $"{filePrefix}_{timestamp}.mp4"
        ).Replace('\\', '/');

        // 🔥 NÃO sobrescreve tudo — apenas troca o caminho final
        string originalOptions = ffmpegCapture.CaptureOptions;
        Debug.Log(ffmpegCapture.CaptureOptions);

        if (originalOptions.Contains("{PERSISTENT_DATA_PATH}/capture.mp4"))
        {
            ffmpegCapture.CaptureOptions = originalOptions.Replace(
                "{PERSISTENT_DATA_PATH}/capture.mp4",
                LastVideoPath
            );
        }
        else
        {
            Debug.LogWarning("[Recorder] Placeholder não encontrado, usando fallback.");
            ffmpegCapture.CaptureOptions += $" \"{LastVideoPath}\"";
        }

        ffmpegCapture.PrintStdErr = true;

        // 🔥 Garante que está ativo
        ffmpegCapture.gameObject.SetActive(true);

        // 🔥 Start no próximo frame (evita bug interno do plugin)
        StartCoroutine(StartNextFrame());

        IsRecording = true;

        Debug.Log($"[Recorder] Iniciando gravação em: {LastVideoPath}");
    }

    IEnumerator StartNextFrame()
    {
        yield return null;

        ffmpegCapture.StartFfmpeg();

        Debug.Log("[Recorder] FFmpeg iniciado.");
    }

    public void StopRecording()
    {
        if (!IsRecording) return;

        IsRecording = false;

        StartCoroutine(StopAndProcess());
    }

    IEnumerator StopAndProcess()
    {
        Debug.Log("[Recorder] Parando gravação...");

        ffmpegCapture.StopFfmpeg();

        // 🔴 Espera FFmpeg parar de verdade
        float timeout = 0f;
        while (ffmpegCapture.IsRunning && timeout < 15f)
        {
            timeout += Time.deltaTime;
            yield return null;
        }

        Debug.Log("[Recorder] FFmpeg finalizado.");

        // 🔴 Espera arquivo existir
        while (!File.Exists(LastVideoPath))
        {
            Debug.LogWarning("[Recorder] Arquivo ainda não existe...");
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("[Recorder] Arquivo encontrado.");

        // 🔴 Espera liberar lock (igual ao seu outro script)
        bool ready = false;

        while (!ready)
        {
            bool shouldWait = false;

            try
            {
                using (FileStream stream = File.Open(
                    LastVideoPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.None))
                {
                    ready = true;
                }
            }
            catch (IOException)
            {
                Debug.LogWarning("[Recorder] Arquivo ainda em uso...");
                shouldWait = true;
            }

            if (!ready && shouldWait)
                yield return new WaitForSeconds(1f);
        }

        Debug.Log($"[Recorder] Arquivo pronto ({new FileInfo(LastVideoPath).Length / 1024} KB)");

        // 🔴 Upload
        if (uploader != null)
        {
            uploader.UploadVideo(LastVideoPath);
        }
        else
        {
            Debug.LogWarning("[Recorder] uploader não definido.");
        }
    }

    void OnDestroy()
    {
        if (IsRecording)
        {
            ffmpegCapture.StopFfmpeg();
        }
    }
}