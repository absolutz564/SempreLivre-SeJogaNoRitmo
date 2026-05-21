using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ponte entre o KinectManager (K2Examples) e o sistema JustDance.
/// Lê as juntas de até 2 corpos e as normaliza:
///   origem = SpineBase, escala = distância SpineBase→SpineShoulder.
/// </summary>
public class KinectBodyTracker : MonoBehaviour
{
    public static KinectBodyTracker Instance { get; private set; }

    public enum JointId
    {
        SpineBase = 0, SpineMid, Neck, Head,
        ShoulderLeft, ElbowLeft, WristLeft, HandLeft,
        ShoulderRight, ElbowRight, WristRight, HandRight,
        HipLeft, KneeLeft, AnkleLeft, FootLeft,
        HipRight, KneeRight, AnkleRight, FootRight,
        SpineShoulder,
        HandTipLeft, ThumbLeft, HandTipRight, ThumbRight,
        Count
    }

    public bool IsConnected { get; private set; }

    // Índice 0 = Jogador 1, índice 1 = Jogador 2
    const int MaxBodies = 6;
    private readonly Dictionary<JointId, Vector3>[] _bodyJoints =
        new Dictionary<JointId, Vector3>[MaxBodies];

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        KinectManager km = KinectManager.Instance;
        if (km == null || !km.IsInitialized())
        {
            IsConnected = false;
            for (int i = 0; i < MaxBodies; i++) _bodyJoints[i] = null;
            return;
        }

        IsConnected = true;

        int track = Mathf.Min(GameSession.PlayerCount, MaxBodies);
        for (int i = 0; i < track; i++)
        {
            _bodyJoints[i] = km.IsUserDetected(i)
                ? BuildNormalizedJoints(km, km.GetUserIdByIndex(i))
                : null;
        }
        // Limpa corpos além do necessário
        for (int i = track; i < MaxBodies; i++) _bodyJoints[i] = null;
    }

    Dictionary<JointId, Vector3> BuildNormalizedJoints(KinectManager km, long userId)
    {
        Vector3 origin   = km.GetJointPosition(userId, (int)JointId.SpineBase);
        Vector3 shoulder = km.GetJointPosition(userId, (int)JointId.SpineShoulder);
        float   scale    = Mathf.Max(Vector3.Distance(origin, shoulder), 0.001f);

        var joints = new Dictionary<JointId, Vector3>();
        for (int i = 0; i < (int)JointId.Count; i++)
        {
            if (km.IsJointTracked(userId, i))
                joints[(JointId)i] = (km.GetJointPosition(userId, i) - origin) / scale;
        }
        return joints;
    }

    /// <summary>Juntas do Jogador 1 (índice 0). Atalho de compatibilidade.</summary>
    public Dictionary<JointId, Vector3> GetNormalizedJoints() => GetNormalizedJoints(0);

    /// <summary>Juntas normalizadas do jogador no índice informado (0 = P1, 1 = P2).</summary>
    public Dictionary<JointId, Vector3> GetNormalizedJoints(int playerIndex) =>
        playerIndex >= 0 && playerIndex < MaxBodies ? _bodyJoints[playerIndex] : null;

    /// <summary>Injeta juntas externas para simulação/testes sem Kinect.</summary>
    public void InjectJoints(Dictionary<JointId, Vector3> joints, int playerIndex = 0)
    {
        if (playerIndex >= 0 && playerIndex < MaxBodies)
            _bodyJoints[playerIndex] = joints;
        IsConnected = joints != null;
    }
}
