using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controlador principal do gameplay de dança.
/// Suporta 1 ou 2 jogadores. O áudio vem embutido no VideoClip da coreografia.
/// </summary>
public class DanceController : MonoBehaviour
{
    public static DanceController Instance { get; private set; }

    [Header("Coreografia")]
    public DanceChoreography choreography;

    [Header("Referências")]
    public VideoPlayer        danceVideoPlayer;
    public SkeletonVisualizer skeletonVisualizer;
    public DanceHUD           hud;
    public DanceVideoRecorder videoRecorder;

    [Header("Pontuação — um ScoreManager por jogador (máx 2)")]
    public ScoreManager[] scoreManagers = new ScoreManager[2];

    [Header("Configuração")]
    [Tooltip("Avaliações de pose por segundo")]
    public float evaluationRate = 10f;

    [Header("Debug")]
    [Tooltip("Ignora rastreamento do Kinect — simula scores aleatórios para testar o fluxo")]
    public bool mockTracking = false;

    [Header("Precisão de Avaliação")]
    [Tooltip("Tolerância padrão de distância por joint — menor = mais rigoroso")]
    public float defaultTolerance = 0.15f;

    [Tooltip("Score mínimo para PERFEITO")] [Range(0f, 1f)] public float scorePerfeito = 0.68f;
    [Tooltip("Score mínimo para BOM")]      [Range(0f, 1f)] public float scoreBom       = 0.38f;

    // Estado interno
    private float  _musicTime;
    private float  _gameTimer;
    private bool   _isPlaying;
    private DanceChoreography.StepEntry                      _currentEntry;
    private Dictionary<KinectBodyTracker.JointId, Vector3>  _referenceJoints;
    private float  _nextEvalTime;
    private float[] _stepBestScore = new float[2];

    public float             MusicTime    => _musicTime;
    public DanceChoreography Choreography => choreography;
    public bool              IsPlaying    => _isPlaying;

    void Awake()
    {
        Instance = this;
        PoseComparator.ThresholdPerfeito = scorePerfeito;
        PoseComparator.ThresholdBom      = scoreBom;
    }

    public void StartDance()
    {
        if (GameSession.SelectedChoreography != null)
            choreography = GameSession.SelectedChoreography;
        if (choreography == null) { Debug.LogError("Coreografia não definida!"); return; }
        StartCoroutine(DanceRoutine());
    }

    IEnumerator DanceRoutine()
    {
        int players = GameSession.PlayerCount;

        // 1. Aguardar Kinect
        if (!mockTracking)
        {
            float waitTimer = 0f;
            while (!KinectBodyTracker.Instance.IsConnected && waitTimer < 5f)
            {
                waitTimer += Time.deltaTime;
                hud?.ShowMessage("Conectando Kinect...");
                yield return null;
            }

            // 2. Aguardar rastreamento do corpo
            hud?.HideMessage();
            hud?.ShowTrackingWait();
            while (!AllPlayersTracked(players))
                yield return null;
            hud?.HideTrackingWait();
        }

        // 3. Preparar HUD e scores
        hud?.SetupForPlayerCount(players);
        for (int p = 0; p < players && p < scoreManagers.Length; p++)
            scoreManagers[p]?.ResetScore();
        for (int p = 0; p < 2; p++) _stepBestScore[p] = 0f;

        // 3. Prepara o vídeo e exibe o primeiro frame antes do countdown
        hud?.ShowMessage("Prepare-se!");
        videoRecorder?.StartRecording();
        if (danceVideoPlayer != null && choreography.videoClip != null)
        {
            danceVideoPlayer.clip        = choreography.videoClip;
            danceVideoPlayer.playOnAwake = false;
            danceVideoPlayer.isLooping   = false;
            danceVideoPlayer.Prepare();
            yield return new WaitUntil(() => danceVideoPlayer.isPrepared);

            // Play+Pause renderiza o primeiro frame imediatamente
            danceVideoPlayer.Play();
            danceVideoPlayer.Pause();
        }

        for (int i = 3; i >= 1; i--)
        {
            hud?.ShowCountdown(i);
            yield return new WaitForSeconds(1f);
        }
        hud?.HideCountdown();

        // 4. Retoma o vídeo do ponto onde pausou (primeiro frame)
        _gameTimer = 0f;
        if (danceVideoPlayer != null && choreography.videoClip != null)
        {
            danceVideoPlayer.Play();
        }
        _isPlaying = true;
        hud?.HideMessage();

        // 5. Loop de gameplay
        while (_isPlaying)
        {
            if (danceVideoPlayer != null && danceVideoPlayer.isPlaying)
            {
                _musicTime = (float)danceVideoPlayer.time;
                _gameTimer = _musicTime;
            }
            else
            {
                _gameTimer += Time.deltaTime;
                _musicTime  = _gameTimer;
            }

            if (_musicTime >= choreography.TotalDuration)
            {
                _isPlaying = false;
                break;
            }

            float stepTime = _musicTime - choreography.firstBeatOffset;

            var activeEntry = choreography.GetActiveStep(stepTime);

            if (activeEntry != null && activeEntry != _currentEntry)
            {
                if (_currentEntry != null)
                    FinalizeCurrentStep();
                _currentEntry    = activeEntry;
                _referenceJoints = activeEntry.step?.ToDictionary();
                for (int p = 0; p < 2; p++) _stepBestScore[p] = 0f;
                skeletonVisualizer?.SetReferenceJoints(_referenceJoints);
                hud?.ShowStep(activeEntry.step);
            }
            else if (activeEntry == null && _currentEntry != null)
            {
                FinalizeCurrentStep();
                _currentEntry    = null;
                _referenceJoints = null;
                skeletonVisualizer?.SetReferenceJoints(null);
            }

            if (_currentEntry != null && Time.time >= _nextEvalTime)
            {
                _nextEvalTime = Time.time + (1f / evaluationRate);
                EvaluatePose();
            }

            hud?.ShowUpcoming(choreography.GetUpcomingStep(stepTime)?.step);
            yield return null;
        }

        if (_currentEntry != null)
        {
            FinalizeCurrentStep();
            _currentEntry = null;
        }

        // 6. Fim
        yield return new WaitForSeconds(1f);
        danceVideoPlayer?.Stop();
        videoRecorder?.StopRecording();

        for (int p = 0; p < players && p < scoreManagers.Length; p++)
        {
            var sm = scoreManagers[p];
            Debug.Log($"[DanceController] J{p + 1}: Score={sm?.TotalScore}  Perfeito={sm?.PerfeitoCount}  Bom={sm?.BomCount}  Miss={sm?.MissCount}");
        }

        hud?.ShowResults(scoreManagers);
    }

    void EvaluatePose()
    {
        if (_referenceJoints == null) return;

        int players = GameSession.PlayerCount;
        for (int p = 0; p < players && p < scoreManagers.Length; p++)
        {
            float score;
            if (mockTracking)
            {
                score = Random.Range(0.5f, 1f);
            }
            else
            {
                var joints = KinectBodyTracker.Instance?.GetNormalizedJoints(p);
                if (joints == null)
                {
                    Debug.LogWarning($"[DanceController] J{p + 1}: nenhum joint — Kinect rastreando?");
                    continue;
                }
                score = PoseComparator.Compare(joints, _referenceJoints,
                                               _currentEntry.step?.tolerance ?? defaultTolerance);
            }

            Debug.Log($"[DanceController] J{p + 1} '{_currentEntry.step?.stepName}' score={score:F2}");

            if (score > _stepBestScore[p])
            {
                _stepBestScore[p] = score;
                hud?.ShowLiveScore(score, PoseComparator.GetRating(score), p);
            }
        }
    }

    void FinalizeCurrentStep()
    {
        int players = GameSession.PlayerCount;
        for (int p = 0; p < players && p < scoreManagers.Length; p++)
        {
            ScoreRating rating = PoseComparator.GetRating(_stepBestScore[p]);
            int points = RatingToPoints(rating);
            Debug.Log($"[DanceController] J{p + 1} '{_currentEntry.step?.stepName}': best={_stepBestScore[p]:F2}  rating={rating}  pts={points}");
            scoreManagers[p]?.AddScore(points, rating);
            hud?.UpdateScore(scoreManagers[p]?.TotalScore ?? 0, p);
            hud?.ShowRatingPopup(rating, p);
        }
    }

    private int RatingToPoints(ScoreRating r) => r switch
    {
        ScoreRating.Perfeito => 300,
        ScoreRating.Bom      => 150,
        _                    =>   0,
    };

    public void StopDance()
    {
        _isPlaying = false;
        danceVideoPlayer?.Stop();
        StopAllCoroutines();
        videoRecorder?.StopRecording();
        hud?.HideTrackingWait();
    }

    bool AllPlayersTracked(int playerCount)
    {
        var tracker = KinectBodyTracker.Instance;
        if (tracker == null || !tracker.IsConnected) return false;
        for (int p = 0; p < playerCount; p++)
        {
            if (tracker.GetNormalizedJoints(p) == null) return false;
        }
        return true;
    }
}
