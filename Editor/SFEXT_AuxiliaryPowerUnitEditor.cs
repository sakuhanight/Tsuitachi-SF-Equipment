using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.SFEXT.SFEXT_AuxiliaryPowerUnit))]
    public class SFEXT_AuxiliaryPowerUnitEditor : UnityEditor.Editor
    {
        private const string PREF_SHOW_CONTROLS = "TSFE.APUEditor.ShowControls";
        private const string PREF_SHOW_STATE = "TSFE.APUEditor.ShowState";
        private const string PREF_SHOW_TIME_CALC = "TSFE.APUEditor.ShowTimeCalculator";
        private const string PREF_SHOW_SETTINGS = "TSFE.APUEditor.ShowSettings";

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var apu = (TSFE.SFEXT.SFEXT_AuxiliaryPowerUnit)target;

            EditorGUILayout.Space();

            // Play Mode中のみコントロール表示
            if (Application.isPlaying)
            {
                bool showControls = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_CONTROLS, "APU Controls (Play Mode)", true);
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

                bool showState = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_STATE, "APU State (Real-time)", true);
                if (showState)
                {
                    EditorGUILayout.BeginVertical("box");

                    // Power Status
                    EditorGUILayout.LabelField("Power System", EditorStyles.boldLabel);
                    if (apu.powerSource == null)
                    {
                        using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateInfoColor))
                        {
                            EditorGUILayout.LabelField("Mode", "Standalone (No Power Required)");
                        }
                    }
                    else
                    {
                        bool powerAvailable = apu.PowerAvailable;
                        TSFEEditorUtil.DrawStateLabel("Power", powerAvailable, "AVAILABLE", "NOT AVAILABLE");
                        EditorGUILayout.LabelField("Power Source", apu.powerSource.name);
                    }

                    EditorGUILayout.Space();

                    // Status - APUState表示
                    EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);

                    GUIStyle stateStyle = new GUIStyle(EditorStyles.boldLabel);
                    stateStyle.fontSize = 14;

                    TSFEEditorUtil.GetAPUStateDisplay(apu.State, out Color stateColor, out string stateText);
                    using (new TSFEEditorUtil.ColorScope(stateColor))
                    {
                        EditorGUILayout.LabelField("APU State", stateText, stateStyle);
                    }

                    EditorGUILayout.Space();

                    // 詳細情報
                    EditorGUILayout.LabelField("Details", EditorStyles.miniBoldLabel);

                    // runフラグを取得
                    var runField = typeof(TSFE.SFEXT.SFEXT_AuxiliaryPowerUnit).GetField("run", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    bool run = runField != null ? (bool)runField.GetValue(apu) : false;

                    using (new TSFEEditorUtil.ColorScope(run ? TSFEEditorUtil.StateInfoColor : TSFEEditorUtil.StateInactiveColor))
                    {
                        EditorGUILayout.LabelField("  run (UdonSynced)", run.ToString());
                    }

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
                        using (new TSFEEditorUtil.ColorScope(apu.apuStartSound.isPlaying ? TSFEEditorUtil.StateOnColor : TSFEEditorUtil.StateInactiveColor))
                        {
                            EditorGUILayout.LabelField("Start Sound", $"{(apu.apuStartSound.isPlaying ? "Playing" : "Stopped")} | Vol: {apu.apuStartSound.volume:F2} | Pitch: {apu.apuStartSound.pitch:F2}");
                        }
                    }
                    if (apu.apuLoopSound != null)
                    {
                        using (new TSFEEditorUtil.ColorScope(apu.apuLoopSound.isPlaying ? TSFEEditorUtil.StateOnColor : TSFEEditorUtil.StateInactiveColor))
                        {
                            EditorGUILayout.LabelField("Loop Sound", $"{(apu.apuLoopSound.isPlaying ? "Playing" : "Stopped")} | Vol: {apu.apuLoopSound.volume:F2} | Pitch: {apu.apuLoopSound.pitch:F2}");
                        }
                    }
                    if (apu.apuStopSound != null)
                    {
                        using (new TSFEEditorUtil.ColorScope(apu.apuStopSound.isPlaying ? TSFEEditorUtil.StateOnColor : TSFEEditorUtil.StateInactiveColor))
                        {
                            EditorGUILayout.LabelField("Stop Sound", $"{(apu.apuStopSound.isPlaying ? "Playing" : "Stopped")} | Vol: {apu.apuStopSound.volume:F2} | Pitch: {apu.apuStopSound.pitch:F2}");
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
                EditorGUILayout.HelpBox("Play Mode に入るとAPUコントロールと状態表示が利用可能になります", MessageType.Info);
            }

            EditorGUILayout.Space();

            // Time Calculator (startup/shutdown time display)
            bool showTimeCalculator = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_TIME_CALC, "Calculated Times (Read-only)", false);
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
                using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateInfoColor))
                {
                    EditorGUILayout.LabelField("合計始動時間", $"{totalStartupTime:F1} 秒", EditorStyles.boldLabel);
                }

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

            bool showSettings = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_SETTINGS, "Settings", false);
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
