using UnityEngine;
using System.Collections;
using System.Collections.Generic;


/// <summary>
/// Controlador principal do gameplay de dança.
/// Orquestra: música → passos → avaliação Kinect → pontuação → gravação.
/// </summary>
public class DanceController : MonoBehaviour
{
    public static DanceController Instance { get; private set; }

    [Header("Coreografia")]
    public DanceChoreography choreography;

    [Header("Referências")]
    public UnityEngine.AudioSource musicSource;
    public SkeletonVisualizer skeletonVisualizer;
    public ScoreManager scoreManager;
    public DanceHUD hud;
    public DanceVideoRecorder videoRecorder;

    [Header("Configuração")]
    [Tooltip("Quantas vezes por segundo avalia a pose do jogador")]
    public float evaluationRate = 10f;

    // Estado interno
    private float _musicTime;
    private float _gameTimer;       // timer próprio usado quando não há AudioClip
    private bool _hasClip;
    private bool _isPlaying;
    private DanceChoreography.StepEntry _currentEntry;
    private Dictionary<KinectBodyTracker.JointId, Vector3> _referenceJoints;
    private float _nextEvalTime;
    private float _stepBestScore;

    // Propriedades públicas para StepPreviewLane e outros sistemas
    public float              MusicTime   => _musicTime;
    public DanceChoreography  Choreography => choreography;
    public bool               IsPlaying    => _isPlaying;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>Inicia a partida. Chamar do menu ou botão.</summary>
    public void StartDance()
    {
        if (choreography == null) { Debug.LogError("Coreografia não definida!"); return; }
        StartCoroutine(DanceRoutine());
    }

    IEnumerator DanceRoutine()
    {
        // 1. Aguardar Kinect estar pronto
        float waitTimer = 0f;
        while (!KinectBodyTracker.Instance.IsConnected && waitTimer < 5f)
        {
            waitTimer += Time.deltaTime;
            hud?.ShowMessage("Conectando Kinect...");
            yield return null;
        }

        // 2. Countdown
        hud?.ShowMessage("Prepare-se!");
        videoRecorder?.StartRecording();
        for (int i = 3; i >= 1; i--)
        {
            hud?.ShowCountdown(i);
            yield return new WaitForSeconds(1f);
        }
        hud?.HideCountdown();

        // 3. Tocar música (opcional — sem clip usa timer interno)
        _hasClip = choreography.musicClip != null;
        _gameTimer = 0f;
        if (_hasClip)
        {
            musicSource.clip = choreography.musicClip;
            musicSource.Play();
        }
        _isPlaying = true;
        scoreManager?.ResetScore();
        hud?.HideMessage();

        // 4. Loop de gameplay
        while (_isPlaying)
        {
            // Tempo: AudioSource.time quando há música, Time.deltaTime quando não há
            if (_hasClip)
                _musicTime = musicSource.time;
            else
            {
                _gameTimer += Time.deltaTime;
                _musicTime = _gameTimer;
            }

            // Verificar fim
            if (_musicTime >= choreography.TotalDuration)
            {
                _isPlaying = false;
                break;
            }

            // Buscar passo ativo
            var activeEntry = choreography.GetActiveStep(_musicTime);

            if (activeEntry != null && activeEntry != _currentEntry)
            {
                // Novo passo começou
                _currentEntry = activeEntry;
                _referenceJoints = activeEntry.step?.ToDictionary();
                _stepBestScore = 0f;
                skeletonVisualizer?.SetReferenceJoints(_referenceJoints);
                hud?.ShowStep(activeEntry.step);
            }
            else if (activeEntry == null && _currentEntry != null)
            {
                // Passo terminou: registrar melhor score
                FinalizeCurrentStep();
                _currentEntry = null;
                _referenceJoints = null;
                skeletonVisualizer?.SetReferenceJoints(null);
            }

            // Avaliar pose periodicamente
            if (_currentEntry != null && Time.time >= _nextEvalTime)
            {
                _nextEvalTime = Time.time + (1f / evaluationRate);
                EvaluatePose();
            }

            // Mostrar próximo passo no HUD
            var upcoming = choreography.GetUpcomingStep(_musicTime);
            hud?.ShowUpcoming(upcoming?.step);

            yield return null;
        }

        // Finaliza o passo que ainda estava ativo quando o loop terminou
        if (_currentEntry != null)
        {
            FinalizeCurrentStep();
            _currentEntry = null;
        }

        // 5. Fim da dança
        yield return new WaitForSeconds(1f);
        musicSource.Stop();
        videoRecorder?.StopRecording();
        Debug.Log($"[DanceController] Fim. Score={scoreManager?.TotalScore}  Perfect={scoreManager?.PerfectCount}  Great={scoreManager?.GreatCount}  Miss={scoreManager?.MissCount}");
        hud?.ShowResults(scoreManager.TotalScore, scoreManager.MaxPossibleScore);
    }

    void EvaluatePose()
    {
        if (_referenceJoints == null) return;

        var playerJoints = KinectBodyTracker.Instance?.GetNormalizedJoints();
        if (playerJoints == null)
        {
            Debug.LogWarning("[DanceController] EvaluatePose: KinectBodyTracker não tem joints — Kinect está rastreando o jogador?");
            return;
        }

        float score = PoseComparator.Compare(playerJoints, _referenceJoints,
                                              _currentEntry.step?.tolerance ?? 0.25f);

        Debug.Log($"[DanceController] Pose '{_currentEntry.step?.stepName}' — score: {score:F2}  joints do jogador: {playerJoints.Count}  refs: {_referenceJoints.Count}");

        if (score > _stepBestScore)
        {
            _stepBestScore = score;
            hud?.ShowLiveScore(score, PoseComparator.GetRating(score));
        }
    }

    void FinalizeCurrentStep()
    {
        ScoreRating rating = PoseComparator.GetRating(_stepBestScore);
        int points = RatingToPoints(rating);
        Debug.Log($"[DanceController] Finalizado '{_currentEntry.step?.stepName}': bestScore={_stepBestScore:F2}  rating={rating}  pontos={points}");
        scoreManager?.AddScore(points, rating);
        hud?.UpdateScore(scoreManager?.TotalScore ?? 0);
        hud?.ShowRatingPopup(rating);
    }

    private int RatingToPoints(ScoreRating r) => r switch
    {
        ScoreRating.Perfect => 300,
        ScoreRating.Great   => 200,
        ScoreRating.Good    => 100,
        ScoreRating.Ok      =>  50,
        _                   =>   0,
    };

    public void StopDance()
    {
        _isPlaying = false;
        musicSource?.Stop();
        StopAllCoroutines();
        videoRecorder?.StopRecording();
    }
}
