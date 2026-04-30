using UnityEngine;
using System.Collections.Generic;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int TotalScore { get; private set; }
    public int MaxPossibleScore { get; private set; }
    public int PerfectCount { get; private set; }
    public int GreatCount  { get; private set; }
    public int GoodCount   { get; private set; }
    public int OkCount     { get; private set; }
    public int MissCount   { get; private set; }

    // Histórico para tela de resultados
    private readonly List<(ScoreRating rating, int points)> _history = new();

    void Awake() { Instance = this; }

    public void ResetScore()
    {
        TotalScore = MaxPossibleScore = PerfectCount = GreatCount =
            GoodCount = OkCount = MissCount = 0;
        _history.Clear();
    }

    public void AddScore(int points, ScoreRating rating)
    {
        TotalScore += points;
        MaxPossibleScore += 300; // máximo por passo = Perfect
        _history.Add((rating, points));

        switch (rating)
        {
            case ScoreRating.Perfect: PerfectCount++; break;
            case ScoreRating.Great:   GreatCount++;   break;
            case ScoreRating.Good:    GoodCount++;    break;
            case ScoreRating.Ok:      OkCount++;      break;
            default:                  MissCount++;    break;
        }
    }

    public float Accuracy =>
        MaxPossibleScore > 0 ? (float)TotalScore / MaxPossibleScore : 0f;

    public string GetGrade()
    {
        float acc = Accuracy;
        if (acc >= 0.95f) return "S";
        if (acc >= 0.85f) return "A";
        if (acc >= 0.70f) return "B";
        if (acc >= 0.55f) return "C";
        return "D";
    }
}
