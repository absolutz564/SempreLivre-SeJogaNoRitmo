using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gerencia a pontuação de um único jogador.
/// Não é singleton — DanceController mantém uma instância por jogador.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public int TotalScore       { get; private set; }
    public int MaxPossibleScore { get; private set; }
    public int PerfeitoCount    { get; private set; }
    public int BomCount         { get; private set; }
    public int MissCount        { get; private set; }

    private readonly List<(ScoreRating rating, int points)> _history = new();

    public void ResetScore()
    {
        TotalScore = MaxPossibleScore = PerfeitoCount = BomCount = MissCount = 0;
        _history.Clear();
    }

    public void AddScore(int points, ScoreRating rating)
    {
        TotalScore       += points;
        MaxPossibleScore += 300;
        _history.Add((rating, points));

        switch (rating)
        {
            case ScoreRating.Perfeito: PerfeitoCount++; break;
            case ScoreRating.Bom:      BomCount++;      break;
            default:                   MissCount++;     break;
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
