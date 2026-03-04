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
            var engines = test.engines;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SFEXT_AdvancedEngine Test Controls (Multiple Engines)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("スロットル/リバーサーをこのInspectorで一括制御、各エンジンのスターター/燃料は各エンジンのInspectorで個別操作", MessageType.Info);

            EditorGUILayout.Space();

            // Play Mode 中のみ操作可能
            GUI.enabled = Application.isPlaying && engines != null && engines.Length > 0 && engines[0] != null;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Throttle & Reverser Control (All Engines)", EditorStyles.boldLabel);

            if (engines != null && engines.Length > 0 && engines[0] != null && engines[0].SAVControl != null)
            {
                // Throttle
                float throttle = (float)engines[0].SAVControl.GetProgramVariable("ThrottleInput");
                float newThrottle = EditorGUILayout.Slider("Throttle", throttle, 0f, 1f);
                if (!Mathf.Approximately(throttle, newThrottle))
                {
                    engines[0].SAVControl.SetProgramVariable("ThrottleInput", newThrottle);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Idle (0%)", GUILayout.Height(25)))
                {
                    engines[0].SAVControl.SetProgramVariable("ThrottleInput", 0f);
                }
                if (GUILayout.Button("50%", GUILayout.Height(25)))
                {
                    engines[0].SAVControl.SetProgramVariable("ThrottleInput", 0.5f);
                }
                if (GUILayout.Button("Take Off (100%)", GUILayout.Height(25)))
                {
                    engines[0].SAVControl.SetProgramVariable("ThrottleInput", 1f);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                // Afterburner quick buttons (if any engine has afterburner)
                bool hasAfterburner = false;
                if (engines != null && engines.Length > 0)
                {
                    foreach (var engine in engines)
                    {
                        if (engine != null && engine.hasAfterburner)
                        {
                            hasAfterburner = true;
                            break;
                        }
                    }
                }

                if (hasAfterburner)
                {
                    EditorGUILayout.LabelField("Afterburner Quick Control", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Military Power (94%)", GUILayout.Height(25)))
                    {
                        engines[0].SAVControl.SetProgramVariable("ThrottleInput", 0.94f);
                    }
                    if (GUILayout.Button("Full AB (100%)", GUILayout.Height(25)))
                    {
                        engines[0].SAVControl.SetProgramVariable("ThrottleInput", 1f);
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space();
                }

                // Reverser
                bool reversing = engines[0].reversing;
                if (GUILayout.Button(reversing ? "Reverser: ON" : "Reverser: OFF", GUILayout.Height(30)))
                {
                    foreach (var engine in engines)
                    {
                        if (engine != null)
                        {
                            engine.reversing = !reversing;
                            engine.RequestSerialization();
                        }
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("エンジンまたはSAVControl が設定されていません", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();

            GUI.enabled = true;

            EditorGUILayout.Space();

            // エンジン状態サマリー表示
            if (Application.isPlaying && engines != null && engines.Length > 0)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Engines Summary (Real-time)", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("詳細な状態確認・個別操作は各エンジンのInspectorを参照してください", MessageType.Info);

                for (int i = 0; i < engines.Length; i++)
                {
                    var engine = engines[i];
                    if (engine == null) continue;

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField($"Engine {i + 1}", EditorStyles.boldLabel);

                    float n1Pct = engine.N1 / engine.takeOffN1 * 100f;
                    float n2Pct = engine.N2 / engine.takeOffN2 * 100f;

                    EditorGUILayout.LabelField("N1", $"{engine.N1:F1} RPM ({n1Pct:F1}%)");
                    EditorGUILayout.LabelField("N2", $"{engine.N2:F1} RPM ({n2Pct:F1}%)");

                    // 推力計算（エンジン本体と同じロジック）
                    float thrustRatio = 0f;
                    if (engine.N1 >= engine.idleN1)
                    {
                        float t = (engine.N1 - engine.idleN1) / (engine.takeOffN1 - engine.idleN1);
                        thrustRatio = Mathf.Lerp(engine.idleThrustRatio, 1f, Mathf.Pow(t, engine.thrustCurve));
                    }
                    float thrust = engine.maxThrust * thrustRatio;

                    // アフターバーナー適用（リフレクションでafterburnerLevelを取得）
                    if (engine.hasAfterburner)
                    {
                        var afterburnerLevelField = typeof(TSFE.SFEXT.SFEXT_AdvancedEngine).GetField("afterburnerLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (afterburnerLevelField != null)
                        {
                            float afterburnerLevel = (float)afterburnerLevelField.GetValue(engine);
                            if (afterburnerLevel > 0.01f)
                            {
                                thrust *= Mathf.Lerp(1f, engine.afterburnerThrustMultiplier, afterburnerLevel);
                            }
                        }
                    }

                    if (engine.reversing) thrust *= -engine.reverserRatio;

                    EditorGUILayout.LabelField("Thrust", $"{thrust:F1} N");

                    // アフターバーナーレベル表示
                    if (engine.hasAfterburner)
                    {
                        var afterburnerLevelField = typeof(TSFE.SFEXT.SFEXT_AdvancedEngine).GetField("afterburnerLevel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (afterburnerLevelField != null)
                        {
                            float afterburnerLevel = (float)afterburnerLevelField.GetValue(engine);
                            GUI.color = afterburnerLevel > 0.5f ? Color.cyan : Color.white;
                            EditorGUILayout.LabelField("Afterburner", $"{afterburnerLevel * 100:F1}%");
                            GUI.color = Color.white;
                        }
                    }

                    // 温度表示
                    Color egtColor = engine.EGT > engine.takeOffEGT ? Color.red : (engine.EGT > engine.continuousEGT ? Color.yellow : Color.white);
                    Color ectColor = engine.ECT > engine.overheatECT ? Color.red : (engine.ECT > engine.continuousECT ? Color.yellow : Color.white);

                    GUI.color = egtColor;
                    EditorGUILayout.LabelField("EGT", $"{engine.EGT:F0} °C");
                    GUI.color = ectColor;
                    EditorGUILayout.LabelField("ECT", $"{engine.ECT:F0} °C");
                    GUI.color = Color.white;

                    GUI.color = engine.EngineOn ? Color.green : Color.gray;
                    EditorGUILayout.LabelField("Running", engine.EngineOn ? "YES" : "NO");
                    GUI.color = Color.white;

                    if (engine.fire)
                    {
                        GUI.color = Color.red;
                        EditorGUILayout.LabelField("Fire", "YES");
                        GUI.color = Color.white;
                    }
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

        private void LogEngineDebugInfo(TSFE.SFEXT.SFEXT_AdvancedEngine engine)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== SFEXT_AdvancedEngine Debug Info ===");

            // Basic state
            sb.AppendLine($"Engine Running: {engine.EngineOn} | Starter: {engine.starter} | Fuel: {engine.fuel} | Reversing: {engine.reversing} | Fire: {engine.fire}");

            // RPM
            float n1Pct = engine.N1 / engine.takeOffN1 * 100f;
            float n2Pct = engine.N2 / engine.takeOffN2 * 100f;
            sb.AppendLine($"N1: {engine.N1:F1} RPM ({n1Pct:F1}%) | N2: {engine.N2:F1} RPM ({n2Pct:F1}%)");

            // Temperature
            sb.AppendLine($"EGT: {engine.EGT:F0}°C | ECT: {engine.ECT:F0}°C | Ambient: {engine.ambientTemp:F0}°C");

            // Throttle & Thrust
            if (engine.SAVControl != null)
            {
                float throttle = (float)engine.SAVControl.GetProgramVariable("ThrottleInput");

                // N1 target calculation
                float n1Target = Mathf.Lerp(engine.idleN1, engine.takeOffN1, throttle);
                float n2Min = engine.idleN2 * 0.99f;
                float n1Limit = (engine.N2 - n2Min) / (engine.takeOffN2 - n2Min) * (engine.takeOffN1 - engine.idleN1) + engine.idleN1;
                n1Limit = Mathf.Clamp(n1Limit, engine.idleN1, engine.takeOffN1);
                float n1FinalTarget = Mathf.Min(n1Target, n1Limit);

                sb.AppendLine($"Throttle: {throttle:F2} ({throttle * 100:F0}%) | N1 Target: {n1Target:F1} | N1 Limit: {n1Limit:F1} | N1 Final: {n1FinalTarget:F1}");

                // Thrust
                float n1Norm = Mathf.Clamp01((engine.N1 - engine.idleN1) / (engine.referenceN1 - engine.idleN1));
                float thrust = engine.maxThrust * Mathf.Pow(n1Norm, engine.thrustCurve);
                float appliedThrust = (float)engine.SAVControl.GetProgramVariable("ThrottleStrength");

                sb.AppendLine($"N1 Normalized: {n1Norm:F3} ({n1Norm * 100:F1}%) | Calculated Thrust: {thrust:F2} N | Applied Thrust: {appliedThrust:F2} N");
            }
            else
            {
                sb.AppendLine("SAVControl: Not set");
            }

            // Configuration
            float idleN2Pct = engine.idleN2 / engine.takeOffN2 * 100f;
            sb.AppendLine($"Config: starterTargetN2={engine.starterTargetN2:F2} ({engine.starterTargetN2 * 100:F0}%) | minN2ForIgnition={engine.minN2ForIgnition:F2} ({engine.minN2ForIgnition * 100:F0}%)");
            sb.AppendLine($"Config: idleN2={engine.idleN2:F1} RPM ({idleN2Pct:F1}%) | needsStarter threshold={engine.idleN2 * 0.99f:F1} RPM");

            sb.AppendLine("=== End Debug Info ===");

            UnityEngine.Debug.Log(sb.ToString());
        }
    }
}
