using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ponte entre o KinectManager (K2Examples) e o sistema JustDance.
/// Lê as juntas do KinectManager e as normaliza:
///   origem = SpineBase, escala = distância SpineBase→SpineShoulder.
/// </summary>
public class KinectBodyTracker : MonoBehaviour
{
    public static KinectBodyTracker Instance { get; private set; }

    // Valores inteiros idênticos ao KinectInterop.JointType (0-24)
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

    private Dictionary<JointId, Vector3> _normalizedJoints;

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
            _normalizedJoints = null;
            return;
        }

        IsConnected = true;

        if (!km.IsUserDetected(0))
        {
            _normalizedJoints = null;
            return;
        }

        BuildNormalizedJoints(km, km.GetUserIdByIndex(0));
    }

    void BuildNormalizedJoints(KinectManager km, long userId)
    {
        Vector3 origin   = km.GetJointPosition(userId, (int)JointId.SpineBase);
        Vector3 shoulder = km.GetJointPosition(userId, (int)JointId.SpineShoulder);
        float   scale    = Mathf.Max(Vector3.Distance(origin, shoulder), 0.001f);

        _normalizedJoints = new Dictionary<JointId, Vector3>();
        for (int i = 0; i < (int)JointId.Count; i++)
        {
            if (km.IsJointTracked(userId, i))
                _normalizedJoints[(JointId)i] = (km.GetJointPosition(userId, i) - origin) / scale;
        }
    }

    /// <summary>Juntas normalizadas do jogador, ou null se nenhum corpo detectado.</summary>
    public Dictionary<JointId, Vector3> GetNormalizedJoints() => _normalizedJoints;

    /// <summary>Injeta juntas externas para simulação/testes sem Kinect.</summary>
    public void InjectJoints(Dictionary<JointId, Vector3> joints)
    {
        _normalizedJoints = joints;
        IsConnected = joints != null;
    }
}
