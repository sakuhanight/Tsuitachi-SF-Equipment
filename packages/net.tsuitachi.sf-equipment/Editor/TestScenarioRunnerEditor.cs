using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.Utility.TestScenarioRunner))]
    public class TestScenarioRunnerEditor : UnityEditor.Editor
    {
        private const string PREF_SHOW_SCENARIOS = "TSFE.TestScenarioRunnerEditor.ShowScenarios";
        private const string PREF_SHOW_STATUS = "TSFE.TestScenarioRunnerEditor.ShowStatus";
        private const string PREF_SHOW_SETTINGS = "TSFE.TestScenarioRunnerEditor.ShowSettings";

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var runner = (TSFE.Utility.TestScenarioRunner)target;

            EditorGUILayout.Space();

            // Play Mode中のみコントロール表示
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Test Scenario Runner Controls (Play Mode)", EditorStyles.boldLabel);

                EditorGUILayout.Space();

                // Run All Scenariosボタン
                using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateOnColor))
                {
                    if (GUILayout.Button("Run All Scenarios", GUILayout.Height(40)))
                    {
                        runner.RunAllScenarios();
                    }
                }

                EditorGUILayout.Space();

                // Stopボタン
                using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateOffColor))
                {
                    if (GUILayout.Button("Stop", GUILayout.Height(30)))
                    {
                        runner.StopAllScenarios();
                    }
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space();

                // 状態表示
                bool showStatus = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_STATUS, "Test Status (Real-time)", true);
                if (showStatus)
                {
                    EditorGUILayout.BeginVertical("box");

                    // Test対象の状態
                    if (runner.mockSAV != null)
                    {
                        EditorGUILayout.LabelField("MockSAV", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("Altitude", $"{runner.mockSAV.Altitude:F0} m");
                        EditorGUILayout.LabelField("AirSpeed", $"{runner.mockSAV.AirSpeed:F1} m/s");
                        EditorGUILayout.LabelField("Atmosphere", $"{runner.mockSAV.Atmosphere:F2}");
                        EditorGUILayout.LabelField("Fuel", $"{runner.mockSAV.Fuel:F0} / {runner.mockSAV.FullFuel:F0} kg");
                    }

                    EditorGUILayout.Space();

                    if (runner.autoStarter != null)
                    {
                        EditorGUILayout.LabelField("AutoStarter", EditorStyles.boldLabel);
                        TSFEEditorUtil.GetAutoStarterStateDisplay(runner.autoStarter.state, out Color stateColor, out string stateText);
                        using (new TSFEEditorUtil.ColorScope(stateColor))
                        {
                            EditorGUILayout.LabelField("State", stateText);
                        }
                    }

                    EditorGUILayout.Space();

                    if (runner.apu != null)
                    {
                        EditorGUILayout.LabelField("APU", EditorStyles.boldLabel);
                        TSFEEditorUtil.GetAPUStateDisplay(runner.apu.State, out Color apuColor, out string apuText);
                        using (new TSFEEditorUtil.ColorScope(apuColor))
                        {
                            EditorGUILayout.LabelField("State", apuText);
                        }
                    }

                    EditorGUILayout.Space();

                    if (runner.engines != null && runner.engines.Length > 0)
                    {
                        EditorGUILayout.LabelField("Engines", EditorStyles.boldLabel);
                        int runningCount = 0;
                        for (int i = 0; i < runner.engines.Length; i++)
                        {
                            if (runner.engines[i] != null)
                            {
                                bool isRunning = (runner.engines[i].State == TSFE.SFEXT.EngineState.Running);
                                if (isRunning) runningCount++;

                                Color engineColor = isRunning ? TSFEEditorUtil.StateOnColor : TSFEEditorUtil.StateOffColor;
                                using (new TSFEEditorUtil.ColorScope(engineColor))
                                {
                                    EditorGUILayout.LabelField($"Engine {i + 1}", $"{runner.engines[i].State}");
                                }
                            }
                        }

                        EditorGUILayout.Space();
                        using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateInfoColor))
                        {
                            EditorGUILayout.LabelField("Running", $"{runningCount}/{runner.engines.Length}", EditorStyles.boldLabel);
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
                EditorGUILayout.HelpBox("Play Mode に入るとテストシナリオの実行が可能になります", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Scenarios表示
            bool showScenarios = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_SCENARIOS, "Scenarios", true);
            if (showScenarios)
            {
                EditorGUILayout.BeginVertical("box");

                if (runner.scenarios != null && runner.scenarios.Length > 0)
                {
                    for (int i = 0; i < runner.scenarios.Length; i++)
                    {
                        var scenario = runner.scenarios[i];
                        EditorGUILayout.LabelField($"{i + 1}. {scenario.scenarioName}", EditorStyles.boldLabel);
                        EditorGUILayout.LabelField("Description", scenario.description, EditorStyles.wordWrappedMiniLabel);
                        EditorGUILayout.LabelField("Conditions", $"Alt={scenario.altitude:F0}m, Speed={scenario.airSpeed:F0}m/s, Fuel={scenario.fuelPercent:F0}%");
                        EditorGUILayout.LabelField("Expected", $"APU={scenario.expectAPURunning}, AllEngines={scenario.expectAllEnginesRunning}");
                        EditorGUILayout.Space();
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("No scenarios defined");
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            // Settings表示
            bool showSettings = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_SETTINGS, "Settings", false);
            if (showSettings)
            {
                DrawDefaultInspector();
            }
        }
    }
}
