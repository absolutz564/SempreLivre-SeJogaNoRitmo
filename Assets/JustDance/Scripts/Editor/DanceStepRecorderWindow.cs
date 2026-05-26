#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.Video;
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
///   2. Carregue o VideoClip da música no campo "Vídeo"
///   3. Dê Play e pause no beat desejado — o preview mostra o frame exato
///   4. Fique em frente ao Kinect, assuma a pose e clique "Capturar Agora"
///   5. Clique "Salvar" — o passo é adicionado à coreografia com o tempo correto
/// </summary>
public class DanceStepRecorderWindow : EditorWindow
{
    // ── Configuração do passo ─────────────────────────────────────────────────
    string _stepName     = "Passo1";
    string _outputFolder = "Assets/JustDance/Data/Steps";
    float  _duration     = 0.8f;
    float  _tolerance    = 0.45f;
    Sprite _previewSprite;

    // ── Vídeo ─────────────────────────────────────────────────────────────────
    VideoClip      _videoClip;
    VideoPlayer    _previewPlayer;
    RenderTexture  _previewTexture;

    // ── Coreografia destino ───────────────────────────────────────────────────
    DanceChoreography _targetChoreo;

    // ── Countdown ─────────────────────────────────────────────────────────────
    bool   _counting;
    double _captureAt;
    const float CountdownSecs = 3f;

    // ── Última captura ────────────────────────────────────────────────────────
    Dictionary<JointId, Vector3> _captured;
    float  _capturedVideoTime = -1f;
    string _status      = "Aguardando Play Mode...";
    Color  _statusColor = Color.gray;

    // ── Skeleton preview ──────────────────────────────────────────────────────
    const float PreviewW = 200f;
    const float PreviewH = 300f;

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

    // ── Ciclo de vida ──────────────────────────────────────────────────────────

    void OnDisable() => DestroyPreviewPlayer();

    // ── GUI ───────────────────────────────────────────────────────────────────

    void OnGUI()
    {
        bool inPlay  = Application.isPlaying;
        bool tracker = inPlay && KinectBodyTracker.Instance != null;
        bool hasBody = tracker && KinectBodyTracker.Instance.GetNormalizedJoints() != null;

        GUILayout.Label("Dance Step Recorder", EditorStyles.boldLabel);
        DrawStatusBar(inPlay, tracker, hasBody);
        EditorGUILayout.Space(6);

        DrawVideoSection(inPlay);
        EditorGUILayout.Space(4);

        DrawChoreoSection();
        EditorGUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();

        var skeletonRect = GUILayoutUtility.GetRect(PreviewW, PreviewH,
            GUILayout.Width(PreviewW), GUILayout.Height(PreviewH));
        DrawSkeletonPreview(skeletonRect,
            hasBody ? KinectBodyTracker.Instance.GetNormalizedJoints() : _captured);

        EditorGUILayout.Space(8);

        EditorGUILayout.BeginVertical(GUILayout.Width(position.width - PreviewW - 24));
        DrawConfig();
        EditorGUILayout.Space(8);
        DrawButtons(inPlay, hasBody);
        DrawCountdownDisplay();
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        DrawCaptureResult();

        EditorGUILayout.Space(4);
        var style = new GUIStyle(EditorStyles.helpBox) { fontSize = 12, wordWrap = true };
        GUI.color = _statusColor;
        GUILayout.Label(_status, style);
        GUI.color = Color.white;

        if (inPlay) Repaint();

        if (_counting && EditorApplication.timeSinceStartup >= _captureAt)
        {
            _counting = false;
            DoCapture(hasBody);
        }
    }

    // ── Seção de vídeo ────────────────────────────────────────────────────────

    void DrawVideoSection(bool inPlay)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Vídeo", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        var newClip = (VideoClip)EditorGUILayout.ObjectField("VideoClip", _videoClip, typeof(VideoClip), false);
        if (EditorGUI.EndChangeCheck())
        {
            DestroyPreviewPlayer();
            _videoClip = newClip;
        }

        if (_videoClip != null)
        {
            if (!inPlay)
            {
                EditorGUILayout.HelpBox("Entre em Play Mode para controlar o vídeo.", MessageType.Info);
            }
            else
            {
                var vp = GetOrCreatePreviewPlayer();

                // Preview do frame atual
                if (_previewTexture != null)
                {
                    float previewW = position.width - 24f;
                    float previewH = previewW * 9f / 16f;
                    previewH = Mathf.Min(previewH, 200f);
                    var previewRect = GUILayoutUtility.GetRect(previewW, previewH);
                    GUI.DrawTexture(previewRect, _previewTexture, ScaleMode.ScaleToFit);
                }

                // Botões play / pause / stop
                EditorGUILayout.BeginHorizontal();
                bool playing = vp.isPlaying;
                if (GUILayout.Button(playing ? "⏸  Pausar" : "▶  Play", GUILayout.Height(28), GUILayout.Width(90)))
                {
                    if (playing)
                    {
                        vp.Pause();
                    }
                    else
                    {
                        if (vp.clip != _videoClip) { vp.clip = _videoClip; vp.time = 0; }
                        vp.Play();
                    }
                }
                if (GUILayout.Button("⏹  Stop", GUILayout.Height(28), GUILayout.Width(70)))
                {
                    vp.Stop();
                }
                EditorGUILayout.EndHorizontal();

                // Barra de seek
                float total = (float)_videoClip.length;
                float cur   = vp.clip == _videoClip ? (float)vp.time : 0f;

                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(FormatTime(cur), GUILayout.Width(46));

                EditorGUI.BeginChangeCheck();
                float seeked = GUILayout.HorizontalSlider(cur, 0f, total);
                if (EditorGUI.EndChangeCheck())
                {
                    if (vp.clip != _videoClip) vp.clip = _videoClip;
                    vp.time = Mathf.Clamp(seeked, 0f, total - 0.01f);
                    // Retoma a reprodução a partir da nova posição
                    if (!vp.isPlaying) vp.Play();
                }

                GUILayout.Label(FormatTime(total), GUILayout.Width(46));
                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.EndVertical();
    }

    // ── Seção de coreografia destino ──────────────────────────────────────────

    void DrawChoreoSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("Coreografia Destino (opcional)", EditorStyles.boldLabel);
        _targetChoreo = (DanceChoreography)EditorGUILayout.ObjectField(
            "Adicionar a", _targetChoreo, typeof(DanceChoreography), false);
        if (_targetChoreo != null)
        {
            int count = _targetChoreo.steps?.Length ?? 0;
            EditorGUILayout.HelpBox(
                $"{_targetChoreo.name}  ({count} passo{(count != 1 ? "s" : "")})\n" +
                "O passo salvo será inserido automaticamente com o tempo do vídeo.",
                MessageType.Info);
        }
        EditorGUILayout.EndVertical();
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
        _stepName     = EditorGUILayout.TextField("Nome",         _stepName);
        _duration     = EditorGUILayout.Slider("Duração (s)",    _duration,  0.3f, 3f);
        _tolerance    = EditorGUILayout.Slider("Tolerância",      _tolerance, 0.10f, 0.6f);
        _previewSprite = (Sprite)EditorGUILayout.ObjectField(
            "Preview Sprite", _previewSprite, typeof(Sprite), false);
        _outputFolder = EditorGUILayout.TextField("Pasta",        _outputFolder);
    }

    // ── Botões de captura ─────────────────────────────────────────────────────

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

        var numStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 64,
            alignment = TextAnchor.MiddleCenter,
        };
        numStyle.normal.textColor = number <= 1 ? Color.red
                                  : number == 2 ? Color.yellow
                                  : Color.white;
        GUILayout.Label(number > 0 ? number.ToString() : "GO!", numStyle, GUILayout.Height(80));

        float t = 1f - (float)(remaining / CountdownSecs);
        var   progressRect = GUILayoutUtility.GetRect(1, 8);
        EditorGUI.DrawRect(progressRect, new Color(0.2f, 0.2f, 0.2f));
        var fillRect = new Rect(progressRect.x, progressRect.y,
                                progressRect.width * t, progressRect.height);
        EditorGUI.DrawRect(fillRect, number <= 1 ? Color.red : Color.yellow);
    }

    // ── Resultado da captura ──────────────────────────────────────────────────

    void DrawCaptureResult()
    {
        if (_captured == null) return;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        string timeLabel = _capturedVideoTime >= 0
            ? $"Tempo: {FormatTime(_capturedVideoTime)}  ({_capturedVideoTime:F3} s)"
            : "Tempo: —  (vídeo não estava ativo)";

        GUILayout.Label($"Captura pronta  —  {_captured.Count} juntas", EditorStyles.boldLabel);
        GUILayout.Label(timeLabel);

        GUI.enabled = true;
        if (GUILayout.Button("💾  Salvar como DanceStep Asset", GUILayout.Height(36)))
            SaveAsset();

        EditorGUILayout.EndVertical();
    }

    // ── Preview do esqueleto ──────────────────────────────────────────────────

    void DrawSkeletonPreview(Rect rect, Dictionary<JointId, Vector3> joints)
    {
        EditorGUI.DrawRect(rect, new Color(0.1f, 0.1f, 0.15f));

        if (joints == null || joints.Count == 0)
        {
            var noBodyStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                { fontSize = 11, wordWrap = true };
            GUI.Label(new Rect(rect.x + 10, rect.center.y - 20, rect.width - 20, 40),
                      "Nenhum corpo\ndetectado", noBodyStyle);
            return;
        }

        if (Event.current.type != EventType.Repaint) return;

        Handles.BeginGUI();
        Handles.color = new Color(0.3f, 0.85f, 0.3f, 0.9f);
        foreach (var (a, b) in Bones)
        {
            if (joints.TryGetValue(a, out var pa) && joints.TryGetValue(b, out var pb))
                Handles.DrawLine(ToGUI(pa, rect), ToGUI(pb, rect));
        }
        foreach (var kv in joints)
        {
            var   gs    = ToGUI(kv.Value, rect);
            bool  isHand = kv.Key == JointId.HandLeft || kv.Key == JointId.HandRight;
            float r     = isHand ? 6f : 3.5f;
            Color c     = isHand ? Color.yellow : new Color(0.2f, 1f, 0.4f);
            EditorGUI.DrawRect(new Rect(gs.x - r, gs.y - r, r * 2, r * 2), c);
        }
        Handles.EndGUI();
    }

    static Vector3 ToGUI(Vector3 joint, Rect rect)
    {
        const float xMin = -1.5f, xMax = 1.5f;
        const float yMin = -3.5f, yMax =  2.0f;
        float nx = (joint.x - xMin) / (xMax - xMin);
        float ny = 1f - (joint.y - yMin) / (yMax - yMin);
        return new Vector3(rect.x + nx * rect.width, rect.y + ny * rect.height, 0f);
    }

    // ── Lógica de captura ─────────────────────────────────────────────────────

    void StartCountdown()
    {
        _counting  = true;
        _captureAt = EditorApplication.timeSinceStartup + CountdownSecs;
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

        _capturedVideoTime = (_previewPlayer != null
                              && _previewPlayer.clip == _videoClip
                              && _videoClip != null)
            ? (float)_previewPlayer.time
            : -1f;

        string timeInfo = _capturedVideoTime >= 0
            ? $"  |  Tempo: {FormatTime(_capturedVideoTime)}"
            : "";
        SetStatus($"Pose capturada! {joints.Count} juntas.{timeInfo} Clique em Salvar.", Color.cyan);
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

        asset.stepName     = _stepName;
        asset.duration     = _duration;
        asset.tolerance    = _tolerance;
        asset.previewSprite = _previewSprite;
        asset.FromDictionary(_captured);

        if (existing == null)
            AssetDatabase.CreateAsset(asset, path);
        else
            EditorUtility.SetDirty(asset);

        if (_targetChoreo != null && _capturedVideoTime >= 0f)
        {
            var list = new List<DanceChoreography.StepEntry>(
                _targetChoreo.steps ?? System.Array.Empty<DanceChoreography.StepEntry>());

            list.Add(new DanceChoreography.StepEntry
            {
                step         = asset,
                startTime    = _capturedVideoTime,
                windowBefore = 0.5f,
                windowAfter  = 0.3f,
            });

            list.Sort((a, b) => a.startTime.CompareTo(b.startTime));
            _targetChoreo.steps = list.ToArray();
            EditorUtility.SetDirty(_targetChoreo);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;

        string choreoInfo = (_targetChoreo != null && _capturedVideoTime >= 0f)
            ? $"\nAdicionado a '{_targetChoreo.name}' em {FormatTime(_capturedVideoTime)}"
            : _capturedVideoTime >= 0f
                ? $"\nTempo registrado: {FormatTime(_capturedVideoTime)} — arraste para sua DanceChoreography"
                : "";

        SetStatus($"Salvo: {path}{choreoInfo}", Color.green);

        _stepName  = IncrementName(_stepName);
        _captured  = null;
        _capturedVideoTime = -1f;
    }

    // ── VideoPlayer temporário ────────────────────────────────────────────────

    VideoPlayer GetOrCreatePreviewPlayer()
    {
        if (_previewPlayer != null) return _previewPlayer;

        var go = new GameObject("[StepRecorder_Video]") { hideFlags = HideFlags.HideAndDontSave };
        Object.DontDestroyOnLoad(go);

        // RenderTexture para exibir o frame na janela
        _previewTexture = new RenderTexture(640, 360, 0);
        _previewTexture.Create();

        _previewPlayer = go.AddComponent<VideoPlayer>();
        _previewPlayer.playOnAwake    = false;
        _previewPlayer.isLooping      = false;
        _previewPlayer.renderMode     = VideoRenderMode.RenderTexture;
        _previewPlayer.targetTexture  = _previewTexture;
        _previewPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _previewPlayer.SetTargetAudioSource(0, go.AddComponent<AudioSource>());

        return _previewPlayer;
    }

    void DestroyPreviewPlayer()
    {
        if (_previewPlayer != null)
        {
            _previewPlayer.Stop();
            if (_previewPlayer.gameObject != null)
                Object.DestroyImmediate(_previewPlayer.gameObject);
            _previewPlayer = null;
        }
        if (_previewTexture != null)
        {
            _previewTexture.Release();
            Object.DestroyImmediate(_previewTexture);
            _previewTexture = null;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static string FormatTime(float s)
    {
        int m = (int)(s / 60);
        return $"{m:0}:{s % 60:00.0}";
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
