using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.SFEXT.MockSAVControl))]
    public class MockSAVControlEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var mock = (TSFE.SFEXT.MockSAVControl)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mock SaccAirVehicle", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("SFEXT_AdvancedEngine テスト用のモック SAVControl", MessageType.Info);

            EditorGUILayout.Space();

            // Play Mode 中は値を表示
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Current Values (Real-time)", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Throttle Input", mock.ThrottleInput.ToString("F2"));
                EditorGUILayout.LabelField("Throttle Strength", $"{mock.ThrottleStrength:F2} N");
                EditorGUILayout.LabelField("Air Speed", $"{mock.AirSpeed:F1} m/s");

                EditorGUILayout.EndVertical();

                // PlayMode中は自動再描画
                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox("Play Mode に入ると値が表示されます", MessageType.Info);
            }

            EditorGUILayout.Space();

            // デフォルトInspector
            DrawDefaultInspector();
        }
    }
}
