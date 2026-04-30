#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

using System.Collections.Generic;

/// <summary>
/// Componente de teste: attach numa cena de teste para validar um DanceStep
/// em tempo real, mostrando o score via Gizmos e console.
/// </summary>
[ExecuteInEditMode]
public class DanceStepTester : MonoBehaviour
{
    [Header("Passo a testar")]
    public DanceStep stepToTest;

    [Header("Resultado")]
    [Range(0, 1)] public float currentScore;
    public ScoreRating currentRating;

    private Dictionary<KinectBodyTracker.JointId, Vector3> _refJoints;

    void Update()
    {
        if (!Application.isPlaying) return;
        if (stepToTest == null || KinectBodyTracker.Instance == null) return;

        if (_refJoints == null)
            _refJoints = stepToTest.ToDictionary();

        var playerJoints = KinectBodyTracker.Instance.GetNormalizedJoints();
        if (playerJoints == null) return;

        currentScore  = PoseComparator.Compare(playerJoints, _refJoints, stepToTest.tolerance);
        currentRating = PoseComparator.GetRating(currentScore);
    }

    // Mostra o score no Inspector mesmo em Play Mode
    [CustomEditor(typeof(DanceStepTester))]
    public class DanceStepTesterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var tester = (DanceStepTester)target;
            if (!Application.isPlaying) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("-- Resultado em Tempo Real --", EditorStyles.boldLabel);

            Color orig = GUI.color;
            GUI.color = tester.currentScore >= 0.7f ? Color.green : Color.red;
            EditorGUILayout.LabelField($"Score: {tester.currentScore:P0}   Rating: {tester.currentRating}",
                EditorStyles.boldLabel);
            GUI.color = orig;

            // Barra de progresso
            Rect rect = EditorGUILayout.GetControlRect(false, 20);
            EditorGUI.ProgressBar(rect, tester.currentScore, tester.currentRating.ToString());

            Repaint(); // atualizar a cada frame
        }
    }
}
#endif
