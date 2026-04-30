using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FfmpegUnity
{
    [CustomEditor(typeof(FfplayBytesCommand))]
    public class FfplayBytesCommandEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            Undo.RecordObject(target, "Parameter Change");

            EditorGUI.BeginChangeCheck();

            FfplayBytesCommand ffplayCommand = (FfplayBytesCommand)target;

            ffplayCommand.ExecuteOnStart = EditorGUILayout.Toggle("Execute On Start", ffplayCommand.ExecuteOnStart);
            ffplayCommand.Options = EditorGUILayout.TextField("Options", ffplayCommand.Options);
            ffplayCommand.VideoTexture =
                EditorGUILayout.ObjectField("Video Texture", ffplayCommand.VideoTexture, typeof(FfmpegPlayerVideoTexture), true)
                as FfmpegPlayerVideoTexture;
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
