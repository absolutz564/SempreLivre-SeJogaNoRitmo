#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using JointId = KinectBodyTracker.JointId;

/// <summary>
/// Ferramenta para gravar poses do Kinect como DanceStep assets.
/// Abrir em: Window > JustDance > Step Recorder
///
/// FLUXO:
///   1. Entre em Play Mode na cena com KinectBodyTracker
///   2. Fique em frente ao Kinect (veja a silhueta aparecer)
///   3. Clique "Gravar em 3s", assuma a pose durante a contagem
///   4. Clique "Salvar" para criar o DanceStep.asset
/// </summary>
public class DanceStepRecorderWindow : EditorWindow
{
    // ── Configuração do passo ─────────────────────────────────────────────────
    string _stepName     = "Passo1";
    string _outputFolder = "Assets/JustDance/Data/Steps";
    float  _duration     = 0.8f;
    float  _tolerance    = 0.45f;

    // ── Countdown ─────────────────────────────────────────────────────────────
    bool   _counting;
    double _captureAt;          // EditorApplication.timeSinceStartup quando captura
    const float CountdownSecs = 3f;

    // ── Última captura ────────────────────────────────────────────────────────
    Dictionary<JointId, Vector3> _captured;
    string _status      = "Aguardando Play Mode...";
    Color  _statusColor = Color.gray;

    // ── Preview ───────────────────────────────────────────────────────────────
    const float PreviewW = 200f;
    const float PreviewH = 300f;

    // Joints que formam o esqueleto
    static readonly (JointId A, JointId B)[] Bones =
    {
        (JointId.Head,          JointId.Neck),
        (JointId.Neck,          JointId.SpineShoulder),
        (JointId.SpineShoulder, JointId.SpineMid),
        (JointId.SpineMid,      JointId.SpineBase),
        (JointId.SpineShoulder, JointId.ShoulderLeft),
        (JointId.ShoulderLeft,  JointId.ElbowLeft),
        (JointId.ElbowLeft,     JointId.WristLeft),
        (JointId.WristLeft,     JointId.HandLeft),
        (JointId.SpineShoulder, JointId.ShoulderRight),
        (JointId.ShoulderRight, JointId.ElbowRight),
        (JointId.ElbowRight,    JointId.WristRight),
        (JointId.WristRight,    JointId.HandRight),
        (JointId.SpineBase,     JointId.HipLeft),
        (JointId.HipLeft,       JointId.KneeLeft),
        (JointId.KneeLeft,      JointId.AnkleLeft),
        (JointId.SpineBase,     JointId.HipRight),
        (JointId.HipRight,      JointId.KneeRight),
        (JointId.KneeRight,     JointId.AnkleRight),
    };

    // ── Menu ──────────────────────────────────────────────────────────────────

    [MenuItem("Window/JustDance/Step Recorder")]
    public static void Open() => GetWindow<DanceStepRecorderWindow>("Step Recorder");

    // ── GUI ───────────────────────────────────────────────────────────────────

    void OnGUI()
    {
        bool inPlay  = Application.isPlaying;
        bool tracker = inPlay && KinectBodyTracker.Instance != null;
        bool hasBody = tracker && KinectBodyTracker.Instance.GetNormalizedJoints() != null;

        // ── Cabeçalho / status ────────────────────────────────────────────────
        GUILayout.Label("Dance Step Recorder", EditorStyles.boldLabel);
        DrawStatusBar(inPlay, tracker, hasBody);
        EditorGUILayout.Space(6);

        // ── Corpo: preview + config lado a lado ──────────────────────────────
        EditorGUILayout.BeginHorizontal();

        // Preview do esqueleto
        var previewRect = GUILayoutUtility.GetRect(PreviewW, PreviewH,
            GUILayout.Width(PreviewW), GUILayout.Height(PreviewH));
        DrawSkeletonPreview(previewRect,
            hasBody ? KinectBodyTracker.Instance.GetNormalizedJoints() : _captured);

        EditorGUILayout.Space(8);

        // Config + botões
        EditorGUILayout.BeginVertical(GUILayout.Width(position.width - PreviewW - 24));
        DrawConfig();
        EditorGUILayout.Space(8);
        DrawButtons(inPlay, hasBody);
        DrawCountdownDisplay();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // ── Última captura + salvar ───────────────────────────────────────────
        DrawCaptureResult();

        // ── Status ────────────────────────────────────────────────────────────
        EditorGUILayout.Space(4);
        var style = new GUIStyle(EditorStyles.helpBox) { fontSize = 12, wordWrap = true };
        GUI.color = _statusColor;
        GUILayout.Label(_status, style);
        GUI.color = Color.white;

        // Repaint contínuo em play mode
        if (inPlay) Repaint();

        // Verificar se chegou a hora de capturar
        if (_counting && EditorApplication.timeSinceStartup >= _captureAt)
        {
            _counting = false;
            DoCapture(hasBody);
        }
    }

    // ── Status bar ────────────────────────────────────────────────────────────

    void DrawStatusBar(bool inPlay, bool tracker, bool hasBody)
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        StatusDot(inPlay,    "Play Mode");
        StatusDot(tracker,   "KinectTracker");
        StatusDot(hasBody,   "Corpo Detectado");
        EditorGUILayout.EndHorizontal();
    }

    void StatusDot(bool ok, string label)
    {
        GUI.color = ok ? Color.green : new Color(1f, 0.3f, 0.3f);
        GUILayout.Label($"● {label}", GUILayout.Width(120));
        GUI.color = Color.white;
    }

    // ── Configuração ──────────────────────────────────────────────────────────

    void DrawConfig()
    {
        GUILayout.Label("Configuração", EditorStyles.boldLabel);
        _stepName    = EditorGUILayout.TextField("Nome",       _stepName);
        _duration    = EditorGUILayout.Slider("Duração (s)",  _duration,  0.3f, 3f);
        _tolerance   = EditorGUILayout.Slider("Tolerância",   _tolerance, 0.10f, 0.6f);
        _outputFolder = EditorGUILayout.TextField("Pasta",    _outputFolder);
    }

    // ── Botões ────────────────────────────────────────────────────────────────

    void DrawButtons(bool inPlay, bool hasBody)
    {
        bool canCapture = inPlay && hasBody && !_counting;

        GUILayout.Label("Gravação", EditorStyles.boldLabel);

        GUI.enabled = canCapture;
        if (GUILayout.Button("▶  Gravar em 3 segundos", GUILayout.Height(38)))
            StartCountdown();

        EditorGUILayout.Space(4);

        if (GUILayout.Button("⚡  Capturar Agora", GUILayout.Height(30)))
            DoCapture(hasBody);

        GUI.enabled = _counting;
        if (GUILayout.Button("✕  Cancelar Contagem", GUILayout.Height(24)))
        {
            _counting = false;
            SetStatus("Contagem cancelada.", Color.yellow);
        }
        GUI.enabled = true;
    }

    // ── Countdown display ─────────────────────────────────────────────────────

    void DrawCountdownDisplay()
    {
        if (!_counting) return;

        double remaining = _captureAt - EditorApplication.timeSinceStartup;
        int    number    = Mathf.CeilToInt((float)remaining);

        EditorGUILayout.Space(6);

        // Número grande
        var numStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 64,
            alignment = TextAnchor.MiddleCenter,
        };
        numStyle.normal.textColor = number <= 1 ? Color.red
                                  : number == 2 ? Color.yellow
                                  : Color.white;

        GUILayout.Label(number > 0 ? number.ToString() : "GO!", numStyle,
            GUILayout.Height(80));

        // Barra de progresso
        float t = 1f - (float)(remaining / CountdownSecs);
        var   progressRect = GUILayoutUtility.GetRect(1, 8);
        EditorGUI.DrawRect(progressRect, new Color(0.2f, 0.2f, 0.2f));
        var fillRect = new Rect(progressRect.x, progressRect.y,
                                progressRect.width * t, progressRect.height);
        EditorGUI.DrawRect(fillRect, number <= 1 ? Color.red : Color.yellow);
    }

    // ── Resultado da captura ─────────────────────────────────────────────────

    void DrawCaptureResult()
    {
        if (_captured == null) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label($"Captura pronta — {_captured.Count} juntas registradas", EditorStyles.boldLabel);

        GUI.enabled = true;
        if (GUILayout.Button("💾  Salvar como DanceStep Asset", GUILayout.Height(36)))
            SaveAsset();

        EditorGUILayout.EndVertical();
    }

    // ── Preview do esqueleto ──────────────────────────────────────────────────

    void DrawSkeletonPreview(Rect rect,
        Dictionary<JointId, Vector3> joints)
    {
        // Fundo
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.15f));

        if (joints == null || joints.Count == 0)
        {
            var noBodyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                { fontSize = 11, wordWrap = true };
            GUI.Label(new Rect(rect.x + 10, rect.center.y - 20,
                               rect.width - 20, 40),
                      "Nenhum corpo\ndetectado", noBodyStyle);
            return;
        }

        if (Event.current.type != EventType.Repaint) return;

        Handles.BeginGUI();

        // Ossos
        Handles.color = new Color(0.3f, 0.85f, 0.3f, 0.9f);
        foreach (var (a, b) in Bones)
        {
            if (joints.TryGetValue(a, out var pa) && joints.TryGetValue(b, out var pb))
            {
                var sa = ToGUI(pa, rect);
                var sb = ToGUI(pb, rect);
                Handles.DrawLine(sa, sb);
            }
        }

        // Juntas
        foreach (var kv in joints)
        {
            var  gs    = ToGUI(kv.Value, rect);
            bool isHand = kv.Key == JointId.HandLeft || kv.Key == JointId.HandRight;
            float r    = isHand ? 6f : 3.5f;
            Color c    = isHand ? Color.yellow : new Color(0.2f, 1f, 0.4f);
            EditorGUI.DrawRect(new Rect(gs.x - r, gs.y - r, r * 2, r * 2), c);
        }

        Handles.EndGUI();
    }

    // Coordenadas normalizadas → posição GUI dentro do rect de preview
    static Vector3 ToGUI(Vector3 joint, Rect rect)
    {
        // X: -1.5 … +1.5  →  rect.left … rect.right
        // Y: +2.0 … -3.5  →  rect.top  … rect.bottom (invertido)
        const float xMin = -1.5f, xMax = 1.5f;
        const float yMin = -3.5f, yMax =  2.0f;

        float nx = (joint.x - xMin) / (xMax - xMin);
        float ny = 1f - (joint.y - yMin) / (yMax - yMin); // inverte Y

        return new Vector3(
            rect.x + nx * rect.width,
            rect.y + ny * rect.height,
            0f);
    }

    // ── Lógica de captura ─────────────────────────────────────────────────────

    void StartCountdown()
    {
        _counting    = true;
        _captureAt   = EditorApplication.timeSinceStartup + CountdownSecs;
        SetStatus("Assuma a pose! Capturando em 3...", Color.yellow);
    }

    void DoCapture(bool hasBody)
    {
        if (!hasBody || KinectBodyTracker.Instance == null)
        {
            SetStatus("Nenhum corpo detectado — coloque-se à frente do Kinect.", Color.red);
            return;
        }

        var joints = KinectBodyTracker.Instance.GetNormalizedJoints();
        if (joints == null || joints.Count == 0)
        {
            SetStatus("Joints nulos — Kinect pode ter perdido o rastreamento.", Color.red);
            return;
        }

        _captured = new Dictionary<JointId, Vector3>(joints);
        SetStatus($"Pose capturada! {joints.Count} juntas. Clique em Salvar.", Color.cyan);
    }

    // ── Salvar asset ──────────────────────────────────────────────────────────

    void SaveAsset()
    {
        if (_captured == null) return;

        if (!Directory.Exists(_outputFolder))
            Directory.CreateDirectory(_outputFolder);

        string sanitized = _stepName.Replace(" ", "_").Replace("/", "_");
        string path      = $"{_outputFolder}/Step_{sanitized}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<DanceStep>(path);
        var asset    = existing != null ? existing : ScriptableObject.CreateInstance<DanceStep>();

        asset.stepName  = _stepName;
        asset.duration  = _duration;
        asset.tolerance = _tolerance;
        asset.FromDictionary(_captured);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(asset, path);
            SetStatus($"Asset criado: {path}", Color.green);
        }
        else
        {
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            SetStatus($"Asset atualizado: {path}", Color.green);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        // Sugere o próximo nome
        _stepName = IncrementName(_stepName);
        _captured = null; // limpa para próxima captura
    }

    static string IncrementName(string name)
    {
        if (name.Length > 0 && char.IsDigit(name[^1]))
        {
            if (int.TryParse(name[^1].ToString(), out int n))
                return name[..^1] + (n + 1);
        }
        return name + "2";
    }

    void SetStatus(string msg, Color color)
    {
        _status      = msg;
        _statusColor = color;
    }
}
#endif
