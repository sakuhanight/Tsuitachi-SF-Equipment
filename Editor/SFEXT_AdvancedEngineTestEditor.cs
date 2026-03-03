using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.SFEXT.SFEXT_AdvancedEngineTest))]
    public class SFEXT_AdvancedEngineTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var test = (TSFE.SFEXT.SFEXT_AdvancedEngineTest)target;
            var engine = test.engine;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SFEXT_AdvancedEngine Test Controls", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Play Mode中にこのInspectorでエンジンを操作できます", MessageType.Info);

            EditorGUILayout.Space();

            // Play Mode 中のみ操作可能
            GUI.enabled = Application.isPlaying && engine != null;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Engine Controls", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(engine != null && engine.starter ? "Starter: ON" : "Starter: OFF", GUILayout.Height(30)))
            {
                if (engine != null)
                {
                    engine.starter = !engine.starter;
                    engine.RequestSerialization();
                }
            }
            if (GUILayout.Button(engine != null && engine.fuel ? "Fuel: ON" : "Fuel: OFF", GUILayout.Height(30)))
            {
                if (engine != null)
                {
                    engine.fuel = !engine.fuel;
                    engine.RequestSerialization();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(engine != null && engine.reversing ? "Reverser: ON" : "Reverser: OFF", GUILayout.Height(30)))
            {
                if (engine != null)
                {
                    engine.reversing = !engine.reversing;
                    engine.RequestSerialization();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Throttle", EditorStyles.boldLabel);
            if (engine != null && engine.SAVControl != null)
            {
                float throttle = (float)engine.SAVControl.GetProgramVariable("ThrottleInput");
                float newThrottle = EditorGUILayout.Slider(throttle, 0f, 1f);
                if (!Mathf.Approximately(throttle, newThrottle))
                {
                    engine.SAVControl.SetProgramVariable("ThrottleInput", newThrottle);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Idle (0%)", GUILayout.Height(25)))
                {
                    engine.SAVControl.SetProgramVariable("ThrottleInput", 0f);
                }
                if (GUILayout.Button("50%", GUILayout.Height(25)))
                {
                    engine.SAVControl.SetProgramVariable("ThrottleInput", 0.5f);
                }
                if (GUILayout.Button("Take Off (100%)", GUILayout.Height(25)))
                {
                    engine.SAVControl.SetProgramVariable("ThrottleInput", 1f);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("SAVControl が設定されていません", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();

            GUI.enabled = true;

            EditorGUILayout.Space();

            // エンジン状態表示
            if (Application.isPlaying && engine != null)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Engine State (Real-time)", EditorStyles.boldLabel);

                float n1Pct = engine.N1 / engine.takeOffN1 * 100f;
                float n2Pct = engine.N2 / engine.takeOffN2 * 100f;

                EditorGUILayout.LabelField("N1", $"{engine.N1:F1} RPM ({n1Pct:F1}%)");
                EditorGUILayout.LabelField("N2", $"{engine.N2:F1} RPM ({n2Pct:F1}%)");
                EditorGUILayout.LabelField("EGT", $"{engine.EGT:F0} °C");
                EditorGUILayout.LabelField("ECT", $"{engine.ECT:F0} °C");
                EditorGUILayout.LabelField("Fire", engine.fire ? "YES" : "NO");
                EditorGUILayout.LabelField("Engine On", engine.EngineOn ? "YES" : "NO");

                if (engine.SAVControl != null)
                {
                    float thrust = (float)engine.SAVControl.GetProgramVariable("ThrottleStrength");
                    EditorGUILayout.LabelField("Thrust", $"{thrust:F2} N");
                }

                EditorGUILayout.EndVertical();

                // PlayMode中は自動再描画
                Repaint();
            }
            else if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode に入るとエンジン状態が表示されます", MessageType.Info);
            }

            EditorGUILayout.Space();

            // デフォルトInspector
            DrawDefaultInspector();
        }
    }
}
