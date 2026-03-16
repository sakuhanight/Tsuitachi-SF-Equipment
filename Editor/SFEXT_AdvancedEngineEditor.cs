using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.SFEXT.SFEXT_AdvancedEngine))]
    public class SFEXT_AdvancedEngineEditor : UnityEditor.Editor
    {
        private const string PREF_SHOW_CONTROLS = "TSFE.AdvancedEngineEditor.ShowControls";
        private const string PREF_SHOW_STATE = "TSFE.AdvancedEngineEditor.ShowState";
        private const string PREF_SHOW_RESPONSE_CALC = "TSFE.AdvancedEngineEditor.ShowResponseCalculator";
        private const string PREF_SHOW_SETTINGS = "TSFE.AdvancedEngineEditor.ShowSettings";

        // 前回の値を記憶（変更検知用）
        private float previousStarterTime = -1f;
        private float previousFuelTime = -1f;
        private float previousN1Time = -1f;
        private float previousN1DecTime = -1f;
        private float previousN2StartupResponse = -1f;
        private float previousN2Response = -1f;
        private float previousN1Response = -1f;
        private float previousN1DecreaseResponse = -1f;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var engine = (TSFE.SFEXT.SFEXT_AdvancedEngine)target;

            EditorGUILayout.Space();

            // Play Mode中のみコントロール表示
            if (Application.isPlaying)
            {
                bool showControls = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_CONTROLS, "Engine Controls (Play Mode)", true);
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

                    EditorGUILayout.Space();

                    // 火災制御
                    EditorGUILayout.LabelField("Fire Control", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    using (new TSFEEditorUtil.ColorScope(engine.fireHandlePulled ? TSFEEditorUtil.StateOffColor : Color.white))
                    {
                        if (GUILayout.Button(engine.fireHandlePulled ? "Fire Handle: PULLED" : "Fire Handle: NORMAL", GUILayout.Height(30)))
                        {
                            engine.ToggleFireHandle();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUI.enabled = engine.fire;
                    if (GUILayout.Button("Discharge Extinguisher", GUILayout.Height(30)))
                    {
                        engine.DischargeExtinguisher();
                    }
                    GUI.enabled = Application.isPlaying;

                    using (new TSFEEditorUtil.ColorScope(engine.fireAlarmMuted ? TSFEEditorUtil.StateWarningColor : Color.white))
                    {
                        if (GUILayout.Button(engine.fireAlarmMuted ? "Fire Alarm: MUTED" : "Fire Alarm: UNMUTED", GUILayout.Height(30)))
                        {
                            engine.ToggleFireAlarmMute();
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space();

                bool showState = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_STATE, "Engine State (Real-time)", true);
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
                    Color egtColor = engine.EGT > engine.takeOffEGT ? TSFEEditorUtil.StateOffColor : (engine.EGT > engine.continuousEGT ? TSFEEditorUtil.StateWarningColor : Color.white);
                    Color ectColor = engine.ECT > engine.overheatECT ? TSFEEditorUtil.StateOffColor : (engine.ECT > engine.continuousECT ? TSFEEditorUtil.StateWarningColor : Color.white);

                    using (new TSFEEditorUtil.ColorScope(egtColor))
                    {
                        EditorGUILayout.LabelField("EGT (Exhaust Gas Temp)", $"{engine.EGT:F0} °C");
                    }
                    using (new TSFEEditorUtil.ColorScope(ectColor))
                    {
                        EditorGUILayout.LabelField("ECT (Engine Case Temp)", $"{engine.ECT:F0} °C");
                    }

                    EditorGUILayout.Space();

                    // Status
                    EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
                    bool isRunning = (engine.State == TSFE.SFEXT.EngineState.Running);
                    using (new TSFEEditorUtil.ColorScope(isRunning ? TSFEEditorUtil.StateOnColor : TSFEEditorUtil.StateInactiveColor))
                    {
                        EditorGUILayout.LabelField("Engine Running", isRunning ? "YES" : "NO");
                    }
                    using (new TSFEEditorUtil.ColorScope(engine.fire ? TSFEEditorUtil.StateOffColor : Color.white))
                    {
                        EditorGUILayout.LabelField("Fire", engine.fire ? "YES" : "NO");
                    }

                    EditorGUILayout.Space();

                    // Starter Power Status
                    EditorGUILayout.LabelField("Starter System", EditorStyles.boldLabel);

                    if (engine.starterPowerSource == null)
                    {
                        using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateInfoColor))
                        {
                            EditorGUILayout.LabelField("Mode", "Standalone (Self-Start)");
                        }
                    }
                    else
                    {
                        bool powerAvailable = engine.StarterPowerAvailable;
                        TSFEEditorUtil.DrawStateLabel("Starter Power", powerAvailable, "AVAILABLE", "NOT AVAILABLE");
                        EditorGUILayout.LabelField("Power Source", engine.starterPowerSource.name);
                    }

                    // Auto Starter Cutoff
                    if (engine.autoStarterCutoff)
                    {
                        float cutoffN2 = engine.idleN2 * engine.starterCutoffThreshold;
                        float cutoffPercent = engine.starterCutoffThreshold * 100f;
                        EditorGUILayout.LabelField("Auto Cutoff", $"Enabled at {cutoffN2:F0} RPM ({cutoffPercent:F0}% of idle)");
                    }
                    else
                    {
                        using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateWarningColor))
                        {
                            EditorGUILayout.LabelField("Auto Cutoff", "Disabled (Manual only)");
                        }
                    }

                    EditorGUILayout.Space();

                    // Thrust
                    EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
                    // 推力計算（エンジン本体と同じロジック）
                    float thrustRatio = 0f;
                    float thrust = 0f;

                    // Running状態の時のみ推力を計算
                    if (engine.State == TSFE.SFEXT.EngineState.Running && engine.N1 >= 0.01f)
                    {
                        if (engine.N1 < engine.idleN1)
                        {
                            // 0～idleN1: 線形に0～idleThrustRatioまで上昇
                            thrustRatio = engine.idleThrustRatio * Mathf.Clamp01(engine.N1 / engine.idleN1);
                        }
                        else
                        {
                            // idleN1～takeOffN1: idleThrustRatio～100%まで曲線的に上昇
                            float t = (engine.N1 - engine.idleN1) / (engine.takeOffN1 - engine.idleN1);
                            thrustRatio = Mathf.Lerp(engine.idleThrustRatio, 1f, Mathf.Pow(t, engine.thrustCurve));
                        }
                        thrust = engine.maxThrust * thrustRatio;
                        if (engine.reversing) thrust *= -engine.reverserRatio;
                    }

                    // 機体質量を取得して加速度換算表示
                    float vehicleMass = 19000f; // デフォルト値
                    if (engine.SAVControl != null)
                    {
                        var rigidbody = (UnityEngine.Rigidbody)engine.SAVControl.GetProgramVariable("VehicleRigidbody");
                        if (rigidbody != null)
                        {
                            vehicleMass = rigidbody.mass;
                        }
                    }
                    float thrustAcceleration = vehicleMass > 0f ? thrust / vehicleMass : 0f;

                    EditorGUILayout.LabelField("Thrust", $"{thrust:F1} N ({thrustAcceleration:F2} m/s²)");

                    EditorGUILayout.Space();

                    // Throttle Input
                    EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
                    if (engine.SAVControl != null)
                    {
                        float throttle = (float)engine.SAVControl.GetProgramVariable("ThrottleInput");
                        EditorGUILayout.LabelField("Throttle Input", $"{throttle:F2} ({throttle * 100:F0}%)");
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Throttle Input", "SAVControl not set");
                    }

                    EditorGUILayout.Space();

                    // Fuel Status
                    EditorGUILayout.LabelField("Fuel", EditorStyles.boldLabel);
                    if (engine.SAVControl != null)
                    {
                        float currentFuel = (float)engine.SAVControl.GetProgramVariable("Fuel");
                        float fullFuel = (float)engine.SAVControl.GetProgramVariable("FullFuel");
                        float fuelPct = fullFuel > 0f ? (currentFuel / fullFuel) * 100f : 0f;

                        Color fuelColor = fuelPct < 10f ? TSFEEditorUtil.StateOffColor : (fuelPct < 25f ? TSFEEditorUtil.StateWarningColor : Color.white);
                        using (new TSFEEditorUtil.ColorScope(fuelColor))
                        {
                            EditorGUILayout.LabelField("Remaining", $"{currentFuel:F1} kg ({fuelPct:F1}%)");
                        }

                        Rect fuelRect = GUILayoutUtility.GetRect(18, 18, GUILayout.ExpandWidth(true));
                        EditorGUI.ProgressBar(fuelRect, fuelPct / 100f, $"{fuelPct:F1}%");

                        if (engine.enableFuelConsumption && engine.State == TSFE.SFEXT.EngineState.Running)
                        {
                            // 現在の燃料消費率を表示（推力比率を再計算）
                            float fuelThrustRatio = 0f;
                            if (engine.N1 >= engine.idleN1)
                            {
                                float t = (engine.N1 - engine.idleN1) / (engine.takeOffN1 - engine.idleN1);
                                fuelThrustRatio = Mathf.Lerp(engine.idleThrustRatio, 1f, Mathf.Pow(t, engine.thrustCurve));
                            }
                            float fuelFlow = Mathf.Lerp(engine.idleFuelFlow, engine.maxFuelFlow, fuelThrustRatio);

                            EditorGUILayout.LabelField("Fuel Flow", $"{fuelFlow:F3} kg/s ({fuelFlow * 3600:F1} kg/h)");

                            if (fuelFlow > 0f)
                            {
                                float remainingTime = currentFuel / fuelFlow;
                                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                                EditorGUILayout.LabelField("Estimated Range", $"{minutes}m {seconds}s at current power");
                            }
                        }
                        else if (!engine.enableFuelConsumption)
                        {
                            using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateInfoColor))
                            {
                                EditorGUILayout.LabelField("Fuel Consumption", "Disabled");
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Fuel", "SAVControl not set");
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space();

                // PlayMode中は自動再描画
                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox("Play Mode に入るとエンジンコントロールと状態表示が利用可能になります", MessageType.Info);

                // Edit Mode中も機体質量情報と換算値を表示
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Thrust Configuration", EditorStyles.boldLabel);

                float vehicleMass = 19000f; // デフォルト値
                if (engine.SAVControl != null)
                {
                    var rigidbody = (UnityEngine.Rigidbody)engine.SAVControl.GetProgramVariable("VehicleRigidbody");
                    if (rigidbody != null)
                    {
                        vehicleMass = rigidbody.mass;
                        EditorGUILayout.LabelField("Vehicle Mass", $"{vehicleMass:F0} kg");
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Vehicle Mass", $"{vehicleMass:F0} kg (default - Rigidbody not found)");
                    }
                }
                else
                {
                    EditorGUILayout.LabelField("Vehicle Mass", $"{vehicleMass:F0} kg (default - SAVControl not set)");
                }

                float thrustAcceleration = vehicleMass > 0f ? engine.maxThrust / vehicleMass : 0f;
                float thrustKN = engine.maxThrust / 1000f;

                EditorGUILayout.LabelField("Max Thrust", $"{engine.maxThrust:F0} N ({thrustKN:F1} kN)");
                using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateInfoColor))
                {
                    EditorGUILayout.LabelField("Sacc Equivalent", $"{thrustAcceleration:F2} m/s² (ThrottleStrength)");
                }

                // 推力重量比
                float thrustToWeight = vehicleMass > 0f ? engine.maxThrust / (vehicleMass * 9.81f) : 0f;
                string twrDescription = thrustToWeight < 0.3f ? "(Low - Trainer/Transport)" :
                                       thrustToWeight < 0.6f ? "(Medium - Regional Jet)" :
                                       thrustToWeight < 1.0f ? "(High - Fighter/Performance)" :
                                       "(Very High - Supermaneuverability)";
                EditorGUILayout.LabelField("Thrust-to-Weight", $"{thrustToWeight:F2} {twrDescription}");

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            // Response Time Calculator (自動計算・即時反映)
            bool showResponseCalculator = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_RESPONSE_CALC, "Response Time Calculator (自動計算)", false);
            if (showResponseCalculator)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox("時間とResponseパラメータを相互に自動計算・即時反映します", MessageType.Info);

                EditorGUILayout.Space();

                // N2 Startup
                TSFEEditorUtil.DrawResponseTimeField(
                    $"N2 Startup (Starter → {engine.starterTargetN2Ratio * 100:F0}%)",
                    0f,
                    engine.takeOffN2 * engine.starterTargetN2Ratio,
                    ref previousStarterTime,
                    ref engine.n2StartupResponse,
                    ref previousStarterTime,
                    ref previousN2StartupResponse,
                    engine,
                    "n2StartupResponse");

                EditorGUILayout.Space();

                // N2 Response
                float idleN2Percent = engine.idleN2 / engine.takeOffN2 * 100f;
                TSFEEditorUtil.DrawResponseTimeField(
                    $"N2 Response ({engine.starterTargetN2Ratio * 100:F0}% → {idleN2Percent:F0}% Idle)",
                    engine.takeOffN2 * engine.starterTargetN2Ratio,
                    engine.idleN2,
                    ref previousFuelTime,
                    ref engine.n2Response,
                    ref previousFuelTime,
                    ref previousN2Response,
                    engine,
                    "n2Response");

                EditorGUILayout.Space();

                // N1 Response
                TSFEEditorUtil.DrawResponseTimeField(
                    "N1 Response (Idle → Take Off)",
                    engine.idleN1,
                    engine.takeOffN1,
                    ref previousN1Time,
                    ref engine.n1Response,
                    ref previousN1Time,
                    ref previousN1Response,
                    engine,
                    "n1Response");

                EditorGUILayout.Space();

                // N1 Decrease
                TSFEEditorUtil.DrawResponseTimeField(
                    "N1 Decrease (Take Off → Idle)",
                    engine.takeOffN1,
                    engine.idleN1,
                    ref previousN1DecTime,
                    ref engine.n1DecreaseResponse,
                    ref previousN1DecTime,
                    ref previousN1DecreaseResponse,
                    engine,
                    "n1DecreaseResponse");

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            bool showSettings = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_SETTINGS, "Settings", false);
            if (showSettings)
            {
                DrawDefaultInspector();
            }
        }

    }
}
