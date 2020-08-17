//using RZP;
//using UnityEditor;
//using UnityEngine;

//public class SoundAnalyzerWindow : EditorWindow
//{
//    private AudioClip _audioClip;
//    private float _bpm;

//    [MenuItem("Tools/SoundAnalyzer")]
//    private static void OpenWindow()
//    {
//        EditorWindow.GetWindow(typeof(SoundAnalyzerWindow)).Show();
//    }

//    private void OnGUI()
//    {
//        _audioClip = EditorGUILayout.ObjectField("Audio clip", _audioClip, typeof(AudioClip), allowSceneObjects: false) as AudioClip;

//        var assetPath = AssetDatabase.GetAssetPath(_audioClip);
//        if (string.IsNullOrEmpty(assetPath)) return;

//        if (GUILayout.Button("Analyze"))
//        {
//            var detector = new BPMDetector(assetPath);
//            _bpm = (float)detector.getBPM();
//        }

//        EditorGUILayout.FloatField("Computed BPM:", _bpm);
//    }
//}
