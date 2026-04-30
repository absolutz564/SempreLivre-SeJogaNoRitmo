#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using JointId = KinectBodyTracker.JointId;

/// <summary>
/// Cria 3 DanceStep assets de teste + 1 DanceChoreography.
/// Use: JustDance > 2. Criar Dados de Teste (3 Passos)
/// </summary>
public static class TestDanceDataCreator
{
    const string StepsFolder  = "Assets/JustDance/Data/Steps";
    const string ChoreoFolder = "Assets/JustDance/Data/Choreographies";

    [MenuItem("JustDance/2. Criar Dados de Teste (3 Passos)", priority = 2)]
    public static void CreateTestData()
    {
        Directory.CreateDirectory(StepsFolder);
        Directory.CreateDirectory(ChoreoFolder);

        var step1 = CreateStep("Maos_Alto",     BuildMaosAlto(),     1.2f, 0.45f);
        var step2 = CreateStep("Maos_Quadril",  BuildMaosQuadril(),  1.2f, 0.45f);
        var step3 = CreateStep("Braco_Direito", BuildBracoDireito(), 1.0f, 0.45f);
        AssetDatabase.SaveAssets();

        var choreo        = ScriptableObject.CreateInstance<DanceChoreography>();
        choreo.songTitle  = "Teste - 3 Passos";
        choreo.artist     = "JustDance Dev";
        choreo.bpm        = 120f;
        choreo.steps      = new DanceChoreography.StepEntry[]
        {
            new() { step = step1, startTime = 3f,  windowBefore = 0.5f, windowAfter = 0.3f },
            new() { step = step2, startTime = 6f,  windowBefore = 0.5f, windowAfter = 0.3f },
            new() { step = step3, startTime = 9f,  windowBefore = 0.5f, windowAfter = 0.3f },
        };

        string choreoPath = $"{ChoreoFolder}/Choreo_Teste3Passos.asset";
        if (File.Exists(choreoPath)) AssetDatabase.DeleteAsset(choreoPath);
        AssetDatabase.CreateAsset(choreo, choreoPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = choreo;
        EditorGUIUtility.PingObject(choreo);

        EditorUtility.DisplayDialog("JustDance ✓",
            "Dados criados!\n\n" +
            "Passo 1 – Mãos no Alto\n" +
            "Passo 2 – Mãos no Quadril\n" +
            "Passo 3 – Braço Direito Estendido\n\n" +
            "Próximo:\n" +
            "• Selecione [Game] na Hierarchy\n" +
            "• Arraste Choreo_Teste3Passos → DanceController > Choreography\n" +
            "• Play → JOGAR", "OK");
    }

    // ── Poses normalizadas (origem=SpineBase, escala=SpineBase→SpineShoulder) ─

    static Dictionary<JointId, Vector3> BuildMaosAlto() => new()
    {
        { JointId.SpineBase,     new Vector3( 0.00f,  0.00f, 0) },
        { JointId.SpineShoulder, new Vector3( 0.00f,  1.00f, 0) },
        { JointId.Head,          new Vector3( 0.00f,  1.55f, 0) },
        { JointId.ShoulderLeft,  new Vector3(-0.75f,  1.00f, 0) },
        { JointId.ShoulderRight, new Vector3( 0.75f,  1.00f, 0) },
        { JointId.ElbowLeft,     new Vector3(-0.80f,  1.55f, 0) },
        { JointId.ElbowRight,    new Vector3( 0.80f,  1.55f, 0) },
        { JointId.WristLeft,     new Vector3(-0.85f,  2.00f, 0) },
        { JointId.WristRight,    new Vector3( 0.85f,  2.00f, 0) },
        { JointId.HandLeft,      new Vector3(-0.85f,  2.20f, 0) },
        { JointId.HandRight,     new Vector3( 0.85f,  2.20f, 0) },
        { JointId.HipLeft,       new Vector3(-0.35f, -0.35f, 0) },
        { JointId.HipRight,      new Vector3( 0.35f, -0.35f, 0) },
        { JointId.KneeLeft,      new Vector3(-0.38f, -1.50f, 0) },
        { JointId.KneeRight,     new Vector3( 0.38f, -1.50f, 0) },
        { JointId.AnkleLeft,     new Vector3(-0.38f, -2.90f, 0) },
        { JointId.AnkleRight,    new Vector3( 0.38f, -2.90f, 0) },
    };

    static Dictionary<JointId, Vector3> BuildMaosQuadril() => new()
    {
        { JointId.SpineBase,     new Vector3( 0.00f,  0.00f,  0    ) },
        { JointId.SpineShoulder, new Vector3( 0.00f,  1.00f,  0    ) },
        { JointId.Head,          new Vector3( 0.00f,  1.55f,  0    ) },
        { JointId.ShoulderLeft,  new Vector3(-0.75f,  1.00f,  0    ) },
        { JointId.ShoulderRight, new Vector3( 0.75f,  1.00f,  0    ) },
        { JointId.ElbowLeft,     new Vector3(-1.00f,  0.45f,  0    ) },
        { JointId.ElbowRight,    new Vector3( 1.00f,  0.45f,  0    ) },
        { JointId.WristLeft,     new Vector3(-0.65f, -0.20f,  0.25f) },
        { JointId.WristRight,    new Vector3( 0.65f, -0.20f,  0.25f) },
        { JointId.HandLeft,      new Vector3(-0.60f, -0.30f,  0.30f) },
        { JointId.HandRight,     new Vector3( 0.60f, -0.30f,  0.30f) },
        { JointId.HipLeft,       new Vector3(-0.35f, -0.35f,  0    ) },
        { JointId.HipRight,      new Vector3( 0.35f, -0.35f,  0    ) },
        { JointId.KneeLeft,      new Vector3(-0.38f, -1.50f,  0    ) },
        { JointId.KneeRight,     new Vector3( 0.38f, -1.50f,  0    ) },
        { JointId.AnkleLeft,     new Vector3(-0.38f, -2.90f,  0    ) },
        { JointId.AnkleRight,    new Vector3( 0.38f, -2.90f,  0    ) },
    };

    static Dictionary<JointId, Vector3> BuildBracoDireito() => new()
    {
        { JointId.SpineBase,     new Vector3( 0.00f,  0.00f,  0    ) },
        { JointId.SpineShoulder, new Vector3( 0.00f,  1.00f,  0    ) },
        { JointId.Head,          new Vector3( 0.00f,  1.55f,  0    ) },
        { JointId.ShoulderLeft,  new Vector3(-0.75f,  1.00f,  0    ) },
        { JointId.ShoulderRight, new Vector3( 0.75f,  1.00f,  0    ) },
        { JointId.ElbowLeft,     new Vector3(-0.90f,  0.45f,  0    ) },
        { JointId.ElbowRight,    new Vector3( 1.20f,  0.95f,  0    ) },
        { JointId.WristLeft,     new Vector3(-0.60f, -0.15f,  0.20f) },
        { JointId.WristRight,    new Vector3( 1.70f,  0.95f,  0    ) },
        { JointId.HandLeft,      new Vector3(-0.55f, -0.25f,  0.25f) },
        { JointId.HandRight,     new Vector3( 2.00f,  0.95f,  0    ) },
        { JointId.HipLeft,       new Vector3(-0.35f, -0.35f,  0    ) },
        { JointId.HipRight,      new Vector3( 0.35f, -0.35f,  0    ) },
        { JointId.KneeLeft,      new Vector3(-0.38f, -1.50f,  0    ) },
        { JointId.KneeRight,     new Vector3( 0.38f, -1.50f,  0    ) },
        { JointId.AnkleLeft,     new Vector3(-0.38f, -2.90f,  0    ) },
        { JointId.AnkleRight,    new Vector3( 0.38f, -2.90f,  0    ) },
    };

    static DanceStep CreateStep(string id, Dictionary<JointId, Vector3> pose,
        float duration, float tolerance)
    {
        var step       = ScriptableObject.CreateInstance<DanceStep>();
        step.stepName  = id.Replace("_", " ");
        step.duration  = duration;
        step.tolerance = tolerance;
        step.FromDictionary(pose);
        string path = $"{StepsFolder}/Step_{id}.asset";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(step, path);
        return step;
    }
}
#endif
