using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject que define a coreografia completa de uma música:
/// sequência de passos com timing sincronizado à música.
/// Crie via: Assets > Create > JustDance > Choreography
/// </summary>
[CreateAssetMenu(fileName = "Choreo_NomeDaMusica", menuName = "JustDance/Choreography")]
public class DanceChoreography : ScriptableObject
{
    [Header("Música")]
    public string songTitle;
    public string artist;
    public AudioClip musicClip;
    public Sprite coverArt;

    [Header("Configuração")]
    [Tooltip("BPM da música (beats per minute)")]
    public float bpm = 128f;

    [Tooltip("Offset em segundos para o primeiro beat")]
    public float firstBeatOffset = 0f;

    [Header("Sequência de Passos")]
    public StepEntry[] steps;

    [System.Serializable]
    public class StepEntry
    {
        [Tooltip("DanceStep a ser executado")]
        public DanceStep step;

        [Tooltip("Segundo da música em que este passo começa")]
        public float startTime;

        [Tooltip("Janela em segundos que o jogador tem para acertar (antecipação)")]
        public float windowBefore = 0.4f;

        [Tooltip("Janela em segundos após o beat para ainda aceitar input")]
        public float windowAfter = 0.2f;
    }

    /// <summary>Retorna o passo ativo para um dado tempo da música.</summary>
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

    /// <summary>Retorna o próximo passo que ainda não chegou (para preview no HUD).</summary>
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
        musicClip != null ? musicClip.length : (steps.Length > 0 ? steps[^1].startTime + 2f : 60f);
}
