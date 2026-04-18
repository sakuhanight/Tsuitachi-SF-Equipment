using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.SFEXT.SFEXT_AutoStarter))]
    public class SFEXT_AutoStarterEditor : UnityEditor.Editor
    {
        private const string PREF_SHOW_STATE = "TSFE.AutoStarterEditor.ShowState";
        private const string PREF_SHOW_SETTINGS = "TSFE.AutoStarterEditor.ShowSettings";

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
                TSFEEditorUtil.GetAutoStarterStateDisplay(autoStarter.state, out Color stateColor, out string stateText);
                using (new TSFEEditorUtil.ColorScope(stateColor))
                {
                    EditorGUILayout.LabelField("Sequence State", stateText, EditorStyles.boldLabel);
                }

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
                using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateOnColor))
                {
                    if (GUILayout.Button("Start Sequence", GUILayout.Height(40)))
                    {
                        autoStarter.StartSequence();
                    }
                }
                GUI.enabled = Application.isPlaying;

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();

                bool canAbort = (autoStarter.state != TSFE.SFEXT.AutoStarterSequenceState.Idle &&
                                autoStarter.state != TSFE.SFEXT.AutoStarterSequenceState.Completed &&
                                autoStarter.state != TSFE.SFEXT.AutoStarterSequenceState.Failed);

                GUI.enabled = canAbort;
                using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateOffColor))
                {
                    if (GUILayout.Button("Abort Sequence", GUILayout.Height(30)))
                    {
                        autoStarter.AbortSequence();
                    }
                }
                GUI.enabled = Application.isPlaying;

                if (GUILayout.Button("Reset", GUILayout.Height(30)))
                {
                    autoStarter.ResetSequence();
                }

                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                // コンポーネント状態表示
                bool showState = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_STATE, "Component Status (Real-time)", true);
                if (showState)
                {
                    EditorGUILayout.BeginVertical("box");

                    // PowerBus
                    if (autoStarter.powerBus != null)
                    {
                        EditorGUILayout.LabelField("PowerBus", EditorStyles.boldLabel);
                        TSFEEditorUtil.DrawStateLabel("Battery", autoStarter.powerBus.BatteryOn);
                    }
                    else
                    {
                        using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateWarningColor))
                        {
                            EditorGUILayout.LabelField("PowerBus", "Not Set");
                        }
                    }

                    EditorGUILayout.Space();

                    // APU
                    if (autoStarter.apu != null)
                    {
                        EditorGUILayout.LabelField("APU", EditorStyles.boldLabel);

                        TSFEEditorUtil.GetAPUStateDisplay(autoStarter.apu.State, out Color apuColor, out string apuText);
                        using (new TSFEEditorUtil.ColorScope(apuColor))
                        {
                            EditorGUILayout.LabelField("Status", apuText);
                        }
                    }
                    else
                    {
                        using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateOffColor))
                        {
                            EditorGUILayout.LabelField("APU", "Not Set");
                        }
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
                                using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateInactiveColor))
                                {
                                    EditorGUILayout.LabelField($"Engine {i + 1}", "Not Set");
                                }
                                continue;
                            }

                            bool isRunning = (engine.State == TSFE.SFEXT.EngineState.Running);
                            if (isRunning) runningCount++;

                            Color engineColor = isRunning ? TSFEEditorUtil.StateOnColor : (engine.starter || engine.fuel ? TSFEEditorUtil.StateWarningColor : TSFEEditorUtil.StateOffColor);
                            string status = isRunning ? "RUNNING" : (engine.starter || engine.fuel ? "STARTING" : "OFF");
                            using (new TSFEEditorUtil.ColorScope(engineColor))
                            {
                                EditorGUILayout.LabelField($"Engine {i + 1}", $"{status} | N2: {engine.N2:F0} RPM");
                            }
                        }

                        EditorGUILayout.Space();
                        using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateInfoColor))
                        {
                            EditorGUILayout.LabelField("Total", $"{runningCount}/{autoStarter.engines.Length} running", EditorStyles.boldLabel);
                        }
                    }
                    else
                    {
                        using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateOffColor))
                        {
                            EditorGUILayout.LabelField("Engines", "Not Set");
                        }
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
            bool showSettings = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_SETTINGS, "Settings", false);
            if (showSettings)
            {
                DrawDefaultInspector();
            }
        }
    }
}
