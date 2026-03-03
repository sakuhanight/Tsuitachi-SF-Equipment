using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.SFEXT.SFEXT_AdvancedEngine))]
    public class SFEXT_AdvancedEngineEditor : UnityEditor.Editor
    {
        private bool showControls = true;
        private bool showState = true;
        private bool showSettings = false;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var engine = (TSFE.SFEXT.SFEXT_AdvancedEngine)target;

            EditorGUILayout.Space();

            // Play Mode中のみコントロール表示
            if (Application.isPlaying)
            {
                showControls = EditorGUILayout.Foldout(showControls, "Engine Controls (Play Mode)", true, EditorStyles.foldoutHeader);
                if (showControls)
                {
                    EditorGUILayout.BeginVertical("box");

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(engine.starter ? "Starter: ON" : "Starter: OFF", GUILayout.Height(30)))
                    {
                        engine.starter = !engine.starter;
                        engine.RequestSerialization();
                    }
                    if (GUILayout.Button(engine.fuel ? "Fuel: ON" : "Fuel: OFF", GUILayout.Height(30)))
                    {
                        engine.fuel = !engine.fuel;
                        engine.RequestSerialization();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(engine.reversing ? "Reverser: ON" : "Reverser: OFF", GUILayout.Height(30)))
                    {
                        engine.reversing = !engine.reversing;
                        engine.RequestSerialization();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space();

                showState = EditorGUILayout.Foldout(showState, "Engine State (Real-time)", true, EditorStyles.foldoutHeader);
                if (showState)
                {
                    EditorGUILayout.BeginVertical("box");

                    float n1Pct = engine.N1 / engine.takeOffN1 * 100f;
                    float n2Pct = engine.N2 / engine.takeOffN2 * 100f;

                    // N1/N2 with progress bars
                    EditorGUILayout.LabelField("N1 (Low Pressure Spool)", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"{engine.N1:F1} RPM ({n1Pct:F1}%)");
                    Rect n1Rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
                    EditorGUI.ProgressBar(n1Rect, n1Pct / 100f, $"{n1Pct:F1}%");

                    EditorGUILayout.Space();

                    EditorGUILayout.LabelField("N2 (High Pressure Spool)", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"{engine.N2:F1} RPM ({n2Pct:F1}%)");
                    Rect n2Rect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
                    EditorGUI.ProgressBar(n2Rect, n2Pct / 100f, $"{n2Pct:F1}%");

                    EditorGUILayout.Space();

                    // Temperatures
                    EditorGUILayout.LabelField("Temperatures", EditorStyles.boldLabel);
                    Color egtColor = engine.EGT > engine.takeOffEGT ? Color.red : (engine.EGT > engine.continuousEGT ? Color.yellow : Color.white);
                    Color ectColor = engine.ECT > engine.overheatECT ? Color.red : (engine.ECT > engine.continuousECT ? Color.yellow : Color.white);

                    GUI.color = egtColor;
                    EditorGUILayout.LabelField("EGT (Exhaust Gas Temp)", $"{engine.EGT:F0} °C");
                    GUI.color = ectColor;
                    EditorGUILayout.LabelField("ECT (Engine Case Temp)", $"{engine.ECT:F0} °C");
                    GUI.color = Color.white;

                    EditorGUILayout.Space();

                    // Status
                    EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
                    GUI.color = engine.EngineOn ? Color.green : Color.gray;
                    EditorGUILayout.LabelField("Engine Running", engine.EngineOn ? "YES" : "NO");
                    GUI.color = engine.fire ? Color.red : Color.white;
                    EditorGUILayout.LabelField("Fire", engine.fire ? "YES" : "NO");
                    GUI.color = Color.white;

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space();

                // PlayMode中は自動再描画
                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox("Play Mode に入るとエンジンコントロールと状態表示が利用可能になります", MessageType.Info);
            }

            EditorGUILayout.Space();

            showSettings = EditorGUILayout.Foldout(showSettings, "Settings", true, EditorStyles.foldoutHeader);
            if (showSettings)
            {
                DrawDefaultInspector();
            }
        }
    }
}
