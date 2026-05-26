using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject que define a coreografia completa de uma música.
/// O áudio deve estar embutido no videoClip.
/// Crie via: Assets > Create > JustDance > Choreography
/// </summary>
[CreateAssetMenu(fileName = "Choreo_NomeDaMusica", menuName = "JustDance/Choreography")]
public class DanceChoreography : ScriptableObject
{
    [Header("Identificação")]
    public string songTitle;
    public string artist;
    public Sprite coverArt;

    [Header("Vídeo (áudio embutido)")]
    public VideoClip videoClip;

    [Header("Configuração")]
    [Tooltip("BPM da música (beats per minute)")]
    public float bpm = 128f;

    [Tooltip("Offset em segundos do início do vídeo até o primeiro beat")]
    public float firstBeatOffset = 0f;

    [Header("Sequência de Passos")]
    public StepEntry[] steps;

    [System.Serializable]
    public class StepEntry
    {
        [Tooltip("DanceStep a ser executado")]
        public DanceStep step;

        [Tooltip("Segundo do vídeo em que este passo começa")]
        public float startTime;

        [Tooltip("Janela em segundos que o jogador tem para acertar (antecipação)")]
        public float windowBefore = 0.4f;

        [Tooltip("Janela em segundos após o beat para ainda aceitar input")]
        public float windowAfter = 0.2f;
    }

    public StepEntry GetActiveStep(float musicTime)
    {
        foreach (var entry in steps)
        {
            float start = entry.startTime - entry.windowBefore;
            float end   = entry.startTime + entry.windowAfter + (entry.step?.duration ?? 0.8f);
            if (musicTime >= start && musicTime <= end)
                return entry;
        }
        return null;
    }

    public StepEntry GetUpcomingStep(float musicTime, float lookAheadSeconds = 1.5f)
    {
        foreach (var entry in steps)
        {
            if (entry.startTime > musicTime && entry.startTime <= musicTime + lookAheadSeconds)
                return entry;
        }
        return null;
    }

    public float TotalDuration =>
        videoClip != null ? (float)videoClip.length :
        (steps != null && steps.Length > 0 ? steps[^1].startTime + 2f : 60f);
}
