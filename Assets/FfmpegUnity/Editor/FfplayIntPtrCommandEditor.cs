using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FfmpegUnity
{
    [CustomEditor(typeof(FfplayIntPtrCommand))]
    public class FfplayIntPtrCommandEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            Undo.RecordObject(target, "Parameter Change");

            EditorGUI.BeginChangeCheck();

            var ffplayCommand = (FfplayIntPtrCommand)target;

            ffplayCommand.ExecuteOnStart = EditorGUILayout.Toggle("Execute On Start", ffplayCommand.ExecuteOnStart);
            ffplayCommand.Options = EditorGUILayout.TextField("Options", ffplayCommand.Options);
            ffplayCommand.DefaultPath = (FfmpegPath.DefaultPath)EditorGUILayout.EnumPopup("Default Path", ffplayCommand.DefaultPath);
            ffplayCommand.InputPath = EditorGUILayout.TextField("Input Path", ffplayCommand.InputPath);
            ffplayCommand.AudioSourceComponent =
                EditorGUILayout.ObjectField("Audio Source", ffplayCommand.AudioSourceComponent, typeof(AudioSource), true)
                as AudioSource;
            ffplayCommand.PrintStdErr = EditorGUILayout.Toggle("Print StdErr", ffplayCommand.PrintStdErr);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(ffplayCommand);
            }
        }
    }
}
