using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Compara a pose do jogador com uma referência e retorna score 0-1.
/// Usa JointId (enum próprio) — sem dependência do Kinect SDK.
/// </summary>
public static class PoseComparator
{
    private static readonly Dictionary<KinectBodyTracker.JointId, float> JointWeights = new()
    {
        { KinectBodyTracker.JointId.HandLeft,      2.0f },
        { KinectBodyTracker.JointId.HandRight,     2.0f },
        { KinectBodyTracker.JointId.WristLeft,     1.5f },
        { KinectBodyTracker.JointId.WristRight,    1.5f },
        { KinectBodyTracker.JointId.ElbowLeft,     1.2f },
        { KinectBodyTracker.JointId.ElbowRight,    1.2f },
        { KinectBodyTracker.JointId.ShoulderLeft,  1.0f },
        { KinectBodyTracker.JointId.ShoulderRight, 1.0f },
        { KinectBodyTracker.JointId.KneeLeft,      1.5f },
        { KinectBodyTracker.JointId.KneeRight,     1.5f },
        { KinectBodyTracker.JointId.AnkleLeft,     1.8f },
        { KinectBodyTracker.JointId.AnkleRight,    1.8f },
        { KinectBodyTracker.JointId.HipLeft,       1.0f },
        { KinectBodyTracker.JointId.HipRight,      1.0f },
        { KinectBodyTracker.JointId.Head,          0.5f },
        { KinectBodyTracker.JointId.SpineBase,     0.3f },
        { KinectBodyTracker.JointId.SpineShoulder, 0.3f },
    };

    public static float Compare(
        Dictionary<KinectBodyTracker.JointId, Vector3> player,
        Dictionary<KinectBodyTracker.JointId, Vector3> reference,
        float tolerance = 0.25f)
    {
        if (player == null || reference == null) return 0f;
        float totalWeight = 0f, weightedScore = 0f;
        foreach (var kvp in reference)
        {
            if (!player.TryGetValue(kvp.Key, out Vector3 playerPos)) continue;

            // Compara apenas XY — Z é profundidade da câmera e não deve afetar
            // a avaliação de poses frontais (braços levantados, laterais, etc.)
            float dx   = playerPos.x - kvp.Value.x;
            float dy   = playerPos.y - kvp.Value.y;
            float dist = Mathf.Sqrt(dx * dx + dy * dy);

            float weight = JointWeights.TryGetValue(kvp.Key, out float w) ? w : 1f;
            weightedScore += Mathf.Clamp01(1f - dist / tolerance) * weight;
            totalWeight   += weight;
        }
        return totalWeight > 0f ? weightedScore / totalWeight : 0f;
    }

    public static float ThresholdPerfeito = 0.85f;
    public static float ThresholdBom      = 0.55f;

    public static ScoreRating GetRating(float score)
    {
        if (score >= ThresholdPerfeito) return ScoreRating.Perfeito;
        if (score >= ThresholdBom)      return ScoreRating.Bom;
        return ScoreRating.Miss;
    }
}

public enum ScoreRating { Miss, Bom, Perfeito }
