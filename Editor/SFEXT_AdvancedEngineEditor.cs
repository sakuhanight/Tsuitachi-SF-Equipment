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
        private bool showResponseCalculator = false;

        // Response calculator values
        private float starterToN2Time = 10f;
        private float fuelToIdleTime = 20f;
        private float n1ResponseTime = 5f;
        private float n1DecreaseTime = 8f;

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

            // Response Time Calculator
            showResponseCalculator = EditorGUILayout.Foldout(showResponseCalculator, "Response Time Calculator", true, EditorStyles.foldoutHeader);
            if (showResponseCalculator)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox("目標時間を入力すると、自動的にResponseパラメータを計算します", MessageType.Info);

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("N2 Startup Response", EditorStyles.boldLabel);
                starterToN2Time = EditorGUILayout.FloatField("Starter → N2 30% 到達時間 (秒)", starterToN2Time);
                if (starterToN2Time > 0f)
                {
                    float calculatedN2Startup = CalculateResponseRate(0f, engine.idleN2 * 0.3f, starterToN2Time);
                    EditorGUILayout.LabelField("計算値", $"n2StartupResponse = {calculatedN2Startup:F4}");
                    if (GUILayout.Button("n2StartupResponse に適用", GUILayout.Height(25)))
                    {
                        Undo.RecordObject(engine, "Set n2StartupResponse");
                        engine.n2StartupResponse = calculatedN2Startup;
                        EditorUtility.SetDirty(engine);
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("N2 Response (Fuel ON)", EditorStyles.boldLabel);
                fuelToIdleTime = EditorGUILayout.FloatField("Fuel ON → Idle 到達時間 (秒)", fuelToIdleTime);
                if (fuelToIdleTime > 0f)
                {
                    float calculatedN2Response = CalculateResponseRate(engine.idleN2 * 0.3f, engine.idleN2, fuelToIdleTime);
                    EditorGUILayout.LabelField("計算値", $"n2Response = {calculatedN2Response:F4}");
                    if (GUILayout.Button("n2Response に適用", GUILayout.Height(25)))
                    {
                        Undo.RecordObject(engine, "Set n2Response");
                        engine.n2Response = calculatedN2Response;
                        EditorUtility.SetDirty(engine);
                    }
                }

                EditorGUILayout.Space();

                EditorGUILayout.LabelField("N1 Response", EditorStyles.boldLabel);
                n1ResponseTime = EditorGUILayout.FloatField("N1 上昇時間 (Idle → Take Off, 秒)", n1ResponseTime);
                if (n1ResponseTime > 0f)
                {
                    float calculatedN1Response = CalculateResponseRate(engine.idleN1, engine.takeOffN1, n1ResponseTime);
                    EditorGUILayout.LabelField("計算値", $"n1Response = {calculatedN1Response:F4}");
                    if (GUILayout.Button("n1Response に適用", GUILayout.Height(25)))
                    {
                        Undo.RecordObject(engine, "Set n1Response");
                        engine.n1Response = calculatedN1Response;
                        EditorUtility.SetDirty(engine);
                    }
                }

                EditorGUILayout.Space();

                n1DecreaseTime = EditorGUILayout.FloatField("N1 減少時間 (Take Off → Idle, 秒)", n1DecreaseTime);
                if (n1DecreaseTime > 0f)
                {
                    float calculatedN1Decrease = CalculateResponseRate(engine.takeOffN1, engine.idleN1, n1DecreaseTime);
                    EditorGUILayout.LabelField("計算値", $"n1DecreaseResponse = {calculatedN1Decrease:F4}");
                    if (GUILayout.Button("n1DecreaseResponse に適用", GUILayout.Height(25)))
                    {
                        Undo.RecordObject(engine, "Set n1DecreaseResponse");
                        engine.n1DecreaseResponse = calculatedN1Decrease;
                        EditorUtility.SetDirty(engine);
                    }
                }

                EditorGUILayout.Space();

                if (GUILayout.Button("リアル CFM56 プリセット適用", GUILayout.Height(30)))
                {
                    Undo.RecordObject(engine, "Apply CFM56 Preset");
                    engine.n2StartupResponse = CalculateResponseRate(0f, engine.idleN2 * 0.3f, 10f);
                    engine.n2Response = CalculateResponseRate(engine.idleN2 * 0.3f, engine.idleN2, 20f);
                    engine.n1Response = CalculateResponseRate(engine.idleN1, engine.takeOffN1, 5f);
                    engine.n1DecreaseResponse = CalculateResponseRate(engine.takeOffN1, engine.idleN1, 8f);
                    starterToN2Time = 10f;
                    fuelToIdleTime = 20f;
                    n1ResponseTime = 5f;
                    n1DecreaseTime = 8f;
                    EditorUtility.SetDirty(engine);
                    Debug.Log("CFM56 リアルプリセット適用: Starter 10秒, Fuel→Idle 20秒, N1上昇 5秒, N1減少 8秒");
                }

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
        /// 目標時間からresponse rateを計算
        /// Formula: response = 1 / (time * difference)
        ///
        /// MoveTowards(current, target, response * Abs(target - current) * dt) の場合:
        /// time = 1 / response (おおよそ)
        /// </summary>
        private float CalculateResponseRate(float from, float to, float timeSeconds)
        {
            if (timeSeconds <= 0f) return 0f;
            float difference = Mathf.Abs(to - from);
            if (difference <= 0f) return 0f;

            // 経験的に、約 63% (1 - 1/e) 到達時間がtimeSecondsになるように調整
            // response = 1 / timeSeconds で近似
            return 1f / timeSeconds;
        }
    }
}
