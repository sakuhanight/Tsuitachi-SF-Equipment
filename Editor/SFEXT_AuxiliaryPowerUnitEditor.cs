using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.SFEXT.SFEXT_AuxiliaryPowerUnit))]
    public class SFEXT_AuxiliaryPowerUnitEditor : UnityEditor.Editor
    {
        private bool showControls = true;
        private bool showState = true;
        private bool showTimeCalculator = false;
        private bool showSettings = false;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var apu = (TSFE.SFEXT.SFEXT_AuxiliaryPowerUnit)target;

            EditorGUILayout.Space();

            // Play Mode中のみコントロール表示
            if (Application.isPlaying)
            {
                showControls = EditorGUILayout.Foldout(showControls, "APU Controls (Play Mode)", true, EditorStyles.foldoutHeader);
                if (showControls)
                {
                    EditorGUILayout.BeginVertical("box");

                    if (GUILayout.Button("Toggle APU", GUILayout.Height(30)))
                    {
                        apu.ToggleAPU();
                    }

                    EditorGUILayout.Space();

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("Start APU", GUILayout.Height(25)))
                    {
                        apu.StartAPU();
                    }
                    if (GUILayout.Button("Stop APU", GUILayout.Height(25)))
                    {
                        apu.StopAPU();
                    }
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space();

                showState = EditorGUILayout.Foldout(showState, "APU State (Real-time)", true, EditorStyles.foldoutHeader);
                if (showState)
                {
                    EditorGUILayout.BeginVertical("box");

                    // Power Status
                    EditorGUILayout.LabelField("Power System", EditorStyles.boldLabel);
                    if (apu.powerSource == null)
                    {
                        GUI.color = Color.cyan;
                        EditorGUILayout.LabelField("Mode", "Standalone (No Power Required)");
                        GUI.color = Color.white;
                    }
                    else
                    {
                        bool powerAvailable = apu.PowerAvailable;
                        GUI.color = powerAvailable ? Color.green : Color.red;
                        EditorGUILayout.LabelField("Power", powerAvailable ? "AVAILABLE" : "NOT AVAILABLE");
                        GUI.color = Color.white;
                        EditorGUILayout.LabelField("Power Source", apu.powerSource.name);
                    }

                    EditorGUILayout.Space();

                    // Status - APUState enumを直接読み取る
                    EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

                    // Get private _apuStateInt field via reflection
                    var apuStateField = typeof(TSFE.SFEXT.SFEXT_AuxiliaryPowerUnit).GetField("_apuStateInt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    int apuStateInt = apuStateField != null ? (int)apuStateField.GetValue(apu) : 0;
                    TSFE.SFEXT.APUState apuState = (TSFE.SFEXT.APUState)apuStateInt;

                    // APUState表示（大きく、カラー付き）
                    GUIStyle stateStyle = new GUIStyle(EditorStyles.boldLabel);
                    stateStyle.fontSize = 14;

                    switch (apuState)
                    {
                        case TSFE.SFEXT.APUState.Off:
                            GUI.color = Color.gray;
                            EditorGUILayout.LabelField("APU State", "OFF", stateStyle);
                            break;
                        case TSFE.SFEXT.APUState.Starting:
                            GUI.color = Color.yellow;
                            EditorGUILayout.LabelField("APU State", "STARTING", stateStyle);
                            break;
                        case TSFE.SFEXT.APUState.Running:
                            GUI.color = Color.green;
                            EditorGUILayout.LabelField("APU State", "RUNNING", stateStyle);
                            break;
                        case TSFE.SFEXT.APUState.Stopping:
                            GUI.color = new Color(1f, 0.5f, 0f); // オレンジ
                            EditorGUILayout.LabelField("APU State", "STOPPING", stateStyle);
                            break;
                    }
                    GUI.color = Color.white;

                    EditorGUILayout.Space();

                    // 詳細情報
                    EditorGUILayout.LabelField("Details", EditorStyles.miniBoldLabel);

                    // runフラグを取得
                    var runField = typeof(TSFE.SFEXT.SFEXT_AuxiliaryPowerUnit).GetField("run", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    bool run = runField != null ? (bool)runField.GetValue(apu) : false;

                    GUI.color = run ? Color.cyan : Color.gray;
                    EditorGUILayout.LabelField("  run (UdonSynced)", run.ToString());

                    GUI.color = Color.white;

                    EditorGUILayout.Space();

                    // RPM & Audio Debug
                    EditorGUILayout.LabelField("RPM & Audio", EditorStyles.boldLabel);

                    // RPM values with percentage
                    float starterTargetRPM = apu.ratedN * apu.starterTargetN;
                    float startCrossFadeRPM = starterTargetRPM * apu.startCrossFadeStart;
                    float stopCrossFadeRPM = apu.ratedN * apu.stopCrossFadeStart;

                    // Get private N field via reflection
                    var nField = typeof(TSFE.SFEXT.SFEXT_AuxiliaryPowerUnit).GetField("N", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    float currentN = nField != null ? (float)nField.GetValue(apu) : 0f;
                    float nPercent = currentN / apu.ratedN * 100f;

                    EditorGUILayout.LabelField("Current RPM", $"{currentN:F0} RPM ({nPercent:F1}%)");
                    EditorGUILayout.LabelField("Start Crossfade", $"{startCrossFadeRPM:F0} RPM");
                    EditorGUILayout.LabelField("Starter Target", $"{starterTargetRPM:F0} RPM");
                    EditorGUILayout.LabelField("Stop Crossfade", $"{stopCrossFadeRPM:F0} RPM");

                    EditorGUILayout.Space();

                    // AudioSource states
                    EditorGUILayout.LabelField("Audio Sources", EditorStyles.boldLabel);
                    if (apu.apuStartSound != null)
                    {
                        GUI.color = apu.apuStartSound.isPlaying ? Color.green : Color.gray;
                        EditorGUILayout.LabelField("Start Sound", $"{(apu.apuStartSound.isPlaying ? "Playing" : "Stopped")} | Vol: {apu.apuStartSound.volume:F2} | Pitch: {apu.apuStartSound.pitch:F2}");
                    }
                    if (apu.apuLoopSound != null)
                    {
                        GUI.color = apu.apuLoopSound.isPlaying ? Color.green : Color.gray;
                        EditorGUILayout.LabelField("Loop Sound", $"{(apu.apuLoopSound.isPlaying ? "Playing" : "Stopped")} | Vol: {apu.apuLoopSound.volume:F2} | Pitch: {apu.apuLoopSound.pitch:F2}");
                    }
                    if (apu.apuStopSound != null)
                    {
                        GUI.color = apu.apuStopSound.isPlaying ? Color.green : Color.gray;
                        EditorGUILayout.LabelField("Stop Sound", $"{(apu.apuStopSound.isPlaying ? "Playing" : "Stopped")} | Vol: {apu.apuStopSound.volume:F2} | Pitch: {apu.apuStopSound.pitch:F2}");
                    }
                    GUI.color = Color.white;

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space();

                // PlayMode中は自動再描画
                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox("Play Mode に入るとAPUコントロールと状態表示が利用可能になります", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Time Calculator (startup/shutdown time display)
            showTimeCalculator = EditorGUILayout.Foldout(showTimeCalculator, "Calculated Times (Read-only)", true, EditorStyles.foldoutHeader);
            if (showTimeCalculator)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox("RPMパラメータに基づく推定時間とクロスフェード情報", MessageType.Info);

                EditorGUILayout.Space();

                // Startup Phase 1: 0 → starterTargetN
                float starterTargetRPM = apu.ratedN * apu.starterTargetN;
                float startupPhase1Time = CalculateTimeFromResponse(0f, starterTargetRPM, apu.nStartupResponse);

                EditorGUILayout.LabelField("始動フェーズ1 (0 → 始動目標)", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("目標RPM", $"{starterTargetRPM:F0} RPM ({apu.starterTargetN * 100:F0}%)");
                EditorGUILayout.LabelField("推定時間", $"{startupPhase1Time:F1} 秒");
                EditorGUILayout.LabelField("使用パラメータ", $"nStartupResponse = {apu.nStartupResponse:F3}");

                EditorGUILayout.Space();

                // Startup Phase 2: starterTargetN → ratedN
                float startupPhase2Time = CalculateTimeFromResponse(starterTargetRPM, apu.ratedN, apu.nResponse);

                EditorGUILayout.LabelField("始動フェーズ2 (始動目標 → 定格)", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("目標RPM", $"{apu.ratedN:F0} RPM (100%)");
                EditorGUILayout.LabelField("推定時間", $"{startupPhase2Time:F1} 秒");
                EditorGUILayout.LabelField("使用パラメータ", $"nResponse = {apu.nResponse:F3}");

                EditorGUILayout.Space();

                // Total startup time
                float totalStartupTime = startupPhase1Time + startupPhase2Time;
                GUI.color = Color.cyan;
                EditorGUILayout.LabelField("合計始動時間", $"{totalStartupTime:F1} 秒", EditorStyles.boldLabel);
                GUI.color = Color.white;

                EditorGUILayout.Space();

                // Shutdown: ratedN → 0
                float shutdownTime = CalculateTimeFromResponse(apu.ratedN, 0f, apu.nDecreaseResponse);

                EditorGUILayout.LabelField("停止フェーズ (定格 → 0)", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("推定時間", $"{shutdownTime:F1} 秒");
                EditorGUILayout.LabelField("使用パラメータ", $"nDecreaseResponse = {apu.nDecreaseResponse:F3}");

                EditorGUILayout.Space();

                // Crossfade information
                EditorGUILayout.LabelField("クロスフェード情報", EditorStyles.boldLabel);

                // Start crossfade
                float startCrossFadeRPM = starterTargetRPM * apu.startCrossFadeStart;
                EditorGUILayout.LabelField("始動音→運転音", $"{startCrossFadeRPM:F0} RPM ({apu.startCrossFadeStart * apu.starterTargetN * 100:F0}%)");

                // Stop crossfade
                float stopCrossFadeRPM = apu.ratedN * apu.stopCrossFadeStart;
                float stopCrossFadeDuration = (apu.ratedN - stopCrossFadeRPM) / (apu.ratedN * apu.nDecreaseResponse);
                EditorGUILayout.LabelField("運転音→停止音", $"{stopCrossFadeRPM:F0} RPM ({apu.stopCrossFadeStart * 100:F0}%)");
                EditorGUILayout.LabelField("停止クロスフェード時間", $"約 {stopCrossFadeDuration:F1} 秒");

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();

            showSettings = EditorGUILayout.Foldout(showSettings, "Settings", true, EditorStyles.foldoutHeader);
            if (showSettings)
            {
                DrawDefaultInspector();
            }
        }

        /// <summary>
        /// response rateから目標時間を逆算
        /// 95%到達時間を返す
        /// </summary>
        private float CalculateTimeFromResponse(float from, float to, float response)
        {
            if (response <= 0f) return 0f;
            return 3f / response;
        }
    }
}
