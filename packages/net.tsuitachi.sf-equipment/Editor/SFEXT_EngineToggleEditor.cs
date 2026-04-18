using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.SFEXT.SFEXT_EngineToggle))]
    public class SFEXT_EngineToggleEditor : UnityEditor.Editor
    {
        private const string PREF_SHOW_STATE = "TSFE.EngineToggleEditor.ShowState";
        private const string PREF_SHOW_SETTINGS = "TSFE.EngineToggleEditor.ShowSettings";

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
                using (new TSFEEditorUtil.ColorScope(allRunning ? TSFEEditorUtil.StateOnColor : TSFEEditorUtil.StateOffColor))
                {
                    EditorGUILayout.LabelField("Operable Engines", allRunning ? "ALL RUNNING" : "NOT ALL RUNNING", EditorStyles.boldLabel);
                }

                EditorGUILayout.Space();

                // トグルボタン
                EditorGUILayout.LabelField("Control", EditorStyles.boldLabel);

                if (allRunning)
                {
                    using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateOffColor))
                    {
                        if (GUILayout.Button("Cut Engines", GUILayout.Height(40)))
                        {
                            engineToggle.CutEngines();
                        }
                    }
                }
                else
                {
                    using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateOnColor))
                    {
                        if (GUILayout.Button("Start Engines (via AutoStarter)", GUILayout.Height(40)))
                        {
                            engineToggle.StartEngines();
                        }
                    }
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
                bool showState = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_STATE, "Engine Status (Real-time)", true);
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
                                using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateInactiveColor))
                                {
                                    EditorGUILayout.LabelField($"Engine {i + 1}", "Not Set");
                                }
                                continue;
                            }

                            bool isInop = engine.fireHandlePulled;
                            if (isInop)
                            {
                                inopCount++;
                                using (new TSFEEditorUtil.ColorScope(Color.magenta))
                                {
                                    EditorGUILayout.LabelField($"Engine {i + 1}", $"INOP (Fire Handle Pulled) | N2: {engine.N2:F0} RPM");
                                }
                            }
                            else
                            {
                                operableCount++;
                                bool isRunning = (engine.State == TSFE.SFEXT.EngineState.Running);
                                if (isRunning) runningCount++;

                                Color engineColor = isRunning ? TSFEEditorUtil.StateOnColor : (engine.starter || engine.fuel ? TSFEEditorUtil.StateWarningColor : TSFEEditorUtil.StateOffColor);
                                string status = isRunning ? "RUNNING" : (engine.starter || engine.fuel ? "STARTING" : "OFF");
                                using (new TSFEEditorUtil.ColorScope(engineColor))
                                {
                                    EditorGUILayout.LabelField($"Engine {i + 1}", $"{status} | N2: {engine.N2:F0} RPM");
                                }
                            }
                        }

                        EditorGUILayout.Space();
                        using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateInfoColor))
                        {
                            EditorGUILayout.LabelField("Summary", $"{runningCount}/{operableCount} operable running, {inopCount} INOP", EditorStyles.boldLabel);
                        }
                    }
                    else
                    {
                        using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateOffColor))
                        {
                            EditorGUILayout.LabelField("Engines", "Not Set");
                        }
                    }

                    EditorGUILayout.Space();

                    // AutoStarter状態
                    if (engineToggle.autoStarter != null)
                    {
                        EditorGUILayout.LabelField("AutoStarter", EditorStyles.boldLabel);
                        TSFEEditorUtil.GetAutoStarterStateDisplay(engineToggle.autoStarter.state, out Color stateColor, out string stateText);
                        using (new TSFEEditorUtil.ColorScope(stateColor))
                        {
                            EditorGUILayout.LabelField("State", stateText);
                        }

                        if (!string.IsNullOrEmpty(engineToggle.autoStarter.statusMessage))
                        {
                            EditorGUILayout.LabelField("Status", engineToggle.autoStarter.statusMessage);
                        }
                    }
                    else
                    {
                        using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateOffColor))
                        {
                            EditorGUILayout.LabelField("AutoStarter", "Not Set");
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
                EditorGUILayout.HelpBox("Play Mode に入るとエンジントグル操作と状態表示が利用可能になります", MessageType.Info);
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
