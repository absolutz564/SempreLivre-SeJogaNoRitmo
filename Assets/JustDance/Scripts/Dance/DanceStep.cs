using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject que define um único passo/pose de dança.
/// Usa JointId (enum próprio) — sem dependência do Kinect SDK.
/// Crie via: Assets > Create > JustDance > Dance Step
/// </summary>
[CreateAssetMenu(fileName = "Step_New", menuName = "JustDance/Dance Step")]
public class DanceStep : ScriptableObject
{
    [Header("Identificação")]
    public string stepName = "Novo Passo";

    [TextArea]
    public string description;

    [Header("Visual")]
    public Sprite previewSprite;
    public Color  arrowColor = Color.white;

    [Header("Timing")]
    [Tooltip("Duração em segundos que o passo deve ser mantido")]
    public float duration  = 0.8f;

    [Tooltip("Tolerância de erro (0.1=rígido  0.4=fácil)")]
    [Range(0.05f, 0.5f)]
    public float tolerance = 0.25f;

    [Header("Dados da Pose")]
    public JointEntry[] referenceJoints;

    [System.Serializable]
    public class JointEntry
    {
        public KinectBodyTracker.JointId jointId;
        public Vector3 normalizedPosition;
    }

    public Dictionary<KinectBodyTracker.JointId, Vector3> ToDictionary()
    {
        var dict = new Dictionary<KinectBodyTracker.JointId, Vector3>();
        if (referenceJoints == null) return dict;
        foreach (var e in referenceJoints) dict[e.jointId] = e.normalizedPosition;
        return dict;
    }

    public void FromDictionary(Dictionary<KinectBodyTracker.JointId, Vector3> joints)
    {
        referenceJoints = new JointEntry[joints.Count];
        int i = 0;
        foreach (var kvp in joints)
            referenceJoints[i++] = new JointEntry { jointId = kvp.Key, normalizedPosition = kvp.Value };
    }
}
