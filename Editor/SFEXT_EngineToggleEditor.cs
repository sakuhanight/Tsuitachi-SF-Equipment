using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.SFEXT.SFEXT_EngineToggle))]
    public class SFEXT_EngineToggleEditor : UnityEditor.Editor
    {
        private bool showState = true;
        private bool showSettings = false;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var engineToggle = (TSFE.SFEXT.SFEXT_EngineToggle)target;

            EditorGUILayout.Space();

            // Play Mode中のみコントロール表示
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Engine Toggle Controls (Play Mode)", EditorStyles.boldLabel);

                EditorGUILayout.Space();

                // 状態表示
                EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);

                bool allRunning = engineToggle.AllOperableEnginesRunning;
                GUI.color = allRunning ? Color.green : Color.red;
                EditorGUILayout.LabelField("Operable Engines", allRunning ? "ALL RUNNING" : "NOT ALL RUNNING", EditorStyles.boldLabel);
                GUI.color = Color.white;

                EditorGUILayout.Space();

                // トグルボタン
                EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);

                if (allRunning)
                {
                    GUI.color = Color.red;
                    if (GUILayout.Button("Cut Engines", GUILayout.Height(40)))
                    {
                        engineToggle.CutEngines();
                    }
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.color = Color.green;
                    if (GUILayout.Button("Start Engines (via AutoStarter)", GUILayout.Height(40)))
                    {
                        engineToggle.StartEngines();
                    }
                    GUI.color = Color.white;
                }

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Toggle", GUILayout.Height(30)))
                {
                    engineToggle.Toggle();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                // エンジン状態詳細
                showState = EditorGUILayout.Foldout(showState, "Engine Status (Real-time)", true, EditorStyles.foldoutHeader);
                if (showState)
                {
                    EditorGUILayout.BeginVertical("box");

                    if (engineToggle.engines != null && engineToggle.engines.Length > 0)
                    {
                        int operableCount = 0;
                        int runningCount = 0;
                        int inopCount = 0;

                        for (int i = 0; i < engineToggle.engines.Length; i++)
                        {
                            var engine = engineToggle.engines[i];
                            if (engine == null)
                            {
                                GUI.color = Color.gray;
                                EditorGUILayout.LabelField($"Engine {i + 1}", "Not Set");
                                GUI.color = Color.white;
                                continue;
                            }

                            bool isInop = engine.fireHandlePulled;
                            if (isInop)
                            {
                                inopCount++;
                                GUI.color = Color.magenta;
                                EditorGUILayout.LabelField($"Engine {i + 1}", $"INOP (Fire Handle Pulled) | N2: {engine.N2:F0} RPM");
                                GUI.color = Color.white;
                            }
                            else
                            {
                                operableCount++;
                                if (engine.EngineOn) runningCount++;

                                GUI.color = engine.EngineOn ? Color.green : (engine.starter || engine.fuel ? Color.yellow : Color.red);
                                string status = engine.EngineOn ? "RUNNING" : (engine.starter || engine.fuel ? "STARTING" : "OFF");
                                EditorGUILayout.LabelField($"Engine {i + 1}", $"{status} | N2: {engine.N2:F0} RPM");
                                GUI.color = Color.white;
                            }
                        }

                        EditorGUILayout.Space();
                        GUI.color = Color.cyan;
                        EditorGUILayout.LabelField("Summary", $"{runningCount}/{operableCount} operable running, {inopCount} INOP", EditorStyles.boldLabel);
                        GUI.color = Color.white;
                    }
                    else
                    {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("Engines", "Not Set");
                        GUI.color = Color.white;
                    }

                    EditorGUILayout.Space();

                    // AutoStarter状態
                    if (engineToggle.autoStarter != null)
                    {
                        EditorGUILayout.LabelField("AutoStarter", EditorStyles.boldLabel);
                        GUI.color = GetAutoStarterStateColor(engineToggle.autoStarter.state);
                        EditorGUILayout.LabelField("State", engineToggle.autoStarter.state.ToString());
                        GUI.color = Color.white;

                        if (!string.IsNullOrEmpty(engineToggle.autoStarter.statusMessage))
                        {
                            EditorGUILayout.LabelField("Status", engineToggle.autoStarter.statusMessage);
                        }
                    }
                    else
                    {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("AutoStarter", "Not Set");
                        GUI.color = Color.white;
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space();

                // PlayMode中は自動再描画
                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox("Play Mode に入るとエンジントグル操作と状態表示が利用可能になります", MessageType.Info);
            }

            EditorGUILayout.Space();

            // 設定表示
            showSettings = EditorGUILayout.Foldout(showSettings, "Settings", true, EditorStyles.foldoutHeader);
            if (showSettings)
            {
                DrawDefaultInspector();
            }
        }

        private Color GetAutoStarterStateColor(TSFE.SFEXT.AutoStarterSequenceState state)
        {
            if (state == TSFE.SFEXT.AutoStarterSequenceState.Idle)
                return Color.gray;
            else if (state == TSFE.SFEXT.AutoStarterSequenceState.Completed)
                return Color.green;
            else if (state == TSFE.SFEXT.AutoStarterSequenceState.Failed)
                return Color.red;
            else
                return Color.yellow;
        }
    }
}
