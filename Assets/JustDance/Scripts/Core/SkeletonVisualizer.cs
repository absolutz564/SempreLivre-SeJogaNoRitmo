using UnityEngine;
using System.Collections.Generic;
using JointId = KinectBodyTracker.JointId;

/// <summary>
/// Desenha o esqueleto do jogador (ciano) e da referência (amarelo) via GL.
/// Sem dependência do Kinect SDK — usa JointId próprio.
/// </summary>
public class SkeletonVisualizer : MonoBehaviour
{
    [Header("Visual")]
    public Material boneMaterial;
    public Color[] playerColors  = { Color.cyan, new Color(1f, 0.4f, 0f) }; // P1 ciano, P2 laranja
    public Color referenceColor  = Color.yellow;

    [Header("Projeção")]
    public Camera kinectProjectionCamera;

    private static readonly (JointId, JointId)[] Bones =
    {
        (JointId.SpineBase,    JointId.SpineShoulder),
        (JointId.SpineShoulder,JointId.ShoulderLeft),
        (JointId.SpineShoulder,JointId.ShoulderRight),
        (JointId.ShoulderLeft, JointId.ElbowLeft),
        (JointId.ShoulderRight,JointId.ElbowRight),
        (JointId.ElbowLeft,    JointId.WristLeft),
        (JointId.ElbowRight,   JointId.WristRight),
        (JointId.WristLeft,    JointId.HandLeft),
        (JointId.WristRight,   JointId.HandRight),
        (JointId.SpineBase,    JointId.HipLeft),
        (JointId.SpineBase,    JointId.HipRight),
        (JointId.HipLeft,      JointId.KneeLeft),
        (JointId.HipRight,     JointId.KneeRight),
        (JointId.KneeLeft,     JointId.AnkleLeft),
        (JointId.KneeRight,    JointId.AnkleRight),
        (JointId.SpineShoulder,JointId.Head),
    };

    private readonly Dictionary<JointId, Vector3>[] _playerJoints = new Dictionary<JointId, Vector3>[2];
    private Dictionary<JointId, Vector3> _referenceJoints;

    public void SetReferenceJoints(Dictionary<JointId, Vector3> joints) => _referenceJoints = joints;

    void Update()
    {
        int count = GameSession.PlayerCount;
        for (int i = 0; i < _playerJoints.Length; i++)
            _playerJoints[i] = i < count ? KinectBodyTracker.Instance?.GetNormalizedJoints(i) : null;
    }

    void OnRenderObject()
    {
        if (boneMaterial == null) return;
        boneMaterial.SetPass(0);
        if (_referenceJoints != null) DrawSkeleton(_referenceJoints, referenceColor, ghost: true);
        for (int i = 0; i < _playerJoints.Length; i++)
        {
            if (_playerJoints[i] == null) continue;
            Color c = i < playerColors.Length ? playerColors[i] : Color.cyan;
            DrawSkeleton(_playerJoints[i], c, ghost: false);
        }
    }

    void DrawSkeleton(Dictionary<JointId, Vector3> joints, Color color, bool ghost)
    {
        GL.Begin(GL.LINES);
        GL.Color(ghost ? new Color(color.r, color.g, color.b, 0.4f) : color);
        foreach (var (a, b) in Bones)
        {
            if (!joints.TryGetValue(a, out Vector3 pa)) continue;
            if (!joints.TryGetValue(b, out Vector3 pb)) continue;
            GL.Vertex(NormToScreen(pa));
            GL.Vertex(NormToScreen(pb));
        }
        GL.End();
    }

    Vector3 NormToScreen(Vector3 n)
    {
        float x = Mathf.Clamp01(n.x * 0.25f + 0.5f);
        float y = Mathf.Clamp01(n.y * 0.25f + 0.5f);
        if (kinectProjectionCamera != null)
            return kinectProjectionCamera.ViewportToWorldPoint(new Vector3(x, y, 1f));
        return new Vector3(x * Screen.width  - Screen.width  * 0.5f,
                           y * Screen.height - Screen.height * 0.5f, 0f);
    }
}
