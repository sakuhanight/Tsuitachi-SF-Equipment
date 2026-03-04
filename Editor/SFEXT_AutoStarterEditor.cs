using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.SFEXT.SFEXT_AutoStarter))]
    public class SFEXT_AutoStarterEditor : UnityEditor.Editor
    {
        private bool showState = true;
        private bool showSettings = false;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var autoStarter = (TSFE.SFEXT.SFEXT_AutoStarter)target;

            EditorGUILayout.Space();

            // Play Mode中のみコントロール表示
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Auto Starter Controls (Play Mode)", EditorStyles.boldLabel);

                EditorGUILayout.Space();

                // 状態表示
                EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);

                // 状態に応じて色分け
                GUI.color = GetStateColor(autoStarter.state);
                EditorGUILayout.LabelField("Sequence State", autoStarter.state.ToString(), EditorStyles.boldLabel);
                GUI.color = Color.white;

                if (!string.IsNullOrEmpty(autoStarter.statusMessage))
                {
                    EditorGUILayout.LabelField("Status", autoStarter.statusMessage);
                }

                EditorGUILayout.Space();

                // コントロールボタン
                EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);

                bool canStart = (autoStarter.state == TSFE.SFEXT.AutoStarterSequenceState.Idle ||
                                autoStarter.state == TSFE.SFEXT.AutoStarterSequenceState.Completed ||
                                autoStarter.state == TSFE.SFEXT.AutoStarterSequenceState.Failed);

                GUI.enabled = canStart;
                GUI.color = Color.green;
                if (GUILayout.Button("Start Sequence", GUILayout.Height(40)))
                {
                    autoStarter.StartSequence();
                }
                GUI.color = Color.white;
                GUI.enabled = Application.isPlaying;

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();

                bool canAbort = (autoStarter.state != TSFE.SFEXT.AutoStarterSequenceState.Idle &&
                                autoStarter.state != TSFE.SFEXT.AutoStarterSequenceState.Completed &&
                                autoStarter.state != TSFE.SFEXT.AutoStarterSequenceState.Failed);

                GUI.enabled = canAbort;
                GUI.color = Color.red;
                if (GUILayout.Button("Abort Sequence", GUILayout.Height(30)))
                {
                    autoStarter.AbortSequence();
                }
                GUI.color = Color.white;
                GUI.enabled = Application.isPlaying;

                if (GUILayout.Button("Reset", GUILayout.Height(30)))
                {
                    autoStarter.ResetSequence();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                // コンポーネント状態表示
                showState = EditorGUILayout.Foldout(showState, "Component Status (Real-time)", true, EditorStyles.foldoutHeader);
                if (showState)
                {
                    EditorGUILayout.BeginVertical("box");

                    // PowerBus
                    if (autoStarter.powerBus != null)
                    {
                        EditorGUILayout.LabelField("PowerBus", EditorStyles.boldLabel);
                        GUI.color = autoStarter.powerBus.BatteryOn ? Color.green : Color.red;
                        EditorGUILayout.LabelField("Battery", autoStarter.powerBus.BatteryOn ? "ON" : "OFF");
                        GUI.color = Color.white;
                    }
                    else
                    {
                        GUI.color = Color.yellow;
                        EditorGUILayout.LabelField("PowerBus", "Not Set");
                        GUI.color = Color.white;
                    }

                    EditorGUILayout.Space();

                    // APU
                    if (autoStarter.apu != null)
                    {
                        EditorGUILayout.LabelField("APU", EditorStyles.boldLabel);
                        GUI.color = autoStarter.apu.started ? Color.green : (autoStarter.apu.terminated ? Color.red : Color.yellow);
                        EditorGUILayout.LabelField("Status", autoStarter.apu.started ? "STARTED" : (autoStarter.apu.terminated ? "OFF" : "STARTING"));
                        GUI.color = Color.white;
                    }
                    else
                    {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("APU", "Not Set");
                        GUI.color = Color.white;
                    }

                    EditorGUILayout.Space();

                    // Engines
                    if (autoStarter.engines != null && autoStarter.engines.Length > 0)
                    {
                        EditorGUILayout.LabelField("Engines", EditorStyles.boldLabel);
                        int runningCount = 0;
                        for (int i = 0; i < autoStarter.engines.Length; i++)
                        {
                            var engine = autoStarter.engines[i];
                            if (engine == null)
                            {
                                GUI.color = Color.gray;
                                EditorGUILayout.LabelField($"Engine {i + 1}", "Not Set");
                                GUI.color = Color.white;
                                continue;
                            }

                            if (engine.EngineOn) runningCount++;

                            GUI.color = engine.EngineOn ? Color.green : (engine.starter || engine.fuel ? Color.yellow : Color.red);
                            string status = engine.EngineOn ? "RUNNING" : (engine.starter || engine.fuel ? "STARTING" : "OFF");
                            EditorGUILayout.LabelField($"Engine {i + 1}", $"{status} | N2: {engine.N2:F0} RPM");
                            GUI.color = Color.white;
                        }

                        EditorGUILayout.Space();
                        GUI.color = Color.cyan;
                        EditorGUILayout.LabelField("Total", $"{runningCount}/{autoStarter.engines.Length} running", EditorStyles.boldLabel);
                        GUI.color = Color.white;
                    }
                    else
                    {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("Engines", "Not Set");
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
                EditorGUILayout.HelpBox("Play Mode に入ると自動始動シーケンスの操作と状態表示が利用可能になります", MessageType.Info);
            }

            EditorGUILayout.Space();

            // 設定表示
            showSettings = EditorGUILayout.Foldout(showSettings, "Settings", true, EditorStyles.foldoutHeader);
            if (showSettings)
            {
                DrawDefaultInspector();
            }
        }

        private Color GetStateColor(TSFE.SFEXT.AutoStarterSequenceState state)
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
