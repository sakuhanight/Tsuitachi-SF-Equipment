using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using TSFE.Utility;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.SFEXT.MockSAVControl))]
    public class MockSAVControlEditor : UnityEditor.Editor
    {
        private const string PREF_SHOW_TEST_SCENARIOS = "TSFE.MockSAVControlEditor.ShowTestScenarios";
        private const string PREF_SHOW_QUICK_CONTROLS = "TSFE.MockSAVControlEditor.ShowQuickControls";

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var mock = (TSFE.SFEXT.MockSAVControl)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Mock SaccAirVehicle - Test Bench", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("SFEXT_AdvancedEngine/APU テスト用のモック SAVControl", MessageType.Info);

            EditorGUILayout.Space();

            // Play Mode 中はテストコントロール表示
            if (Application.isPlaying)
            {
                DrawTestScenarios(mock);
                EditorGUILayout.Space();
                DrawQuickControls(mock);
                EditorGUILayout.Space();
                DrawCurrentStatus(mock);

                // PlayMode中は自動再描画
                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox("Play Mode に入るとテストコントロールが利用可能になります", MessageType.Info);
            }

            EditorGUILayout.Space();

            // デフォルトInspector
            DrawDefaultInspector();
        }

        private void DrawTestScenarios(TSFE.SFEXT.MockSAVControl mock)
        {
            bool showTestScenarios = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_TEST_SCENARIOS, "Test Scenarios (Presets)", true);
            if (!showTestScenarios) return;

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.LabelField("Flight Conditions", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ground (0m, 0kt)", GUILayout.Height(25)))
            {
                mock.Altitude = 0f;
                mock.AirSpeed = 0f;
                mock.AirVel = Vector3.zero;
                mock.Atmosphere = 1.0f;
                mock.Taxiing = true;
                EditorUtility.SetDirty(mock);
            }
            if (GUILayout.Button("Takeoff (0m, 150kt)", GUILayout.Height(25)))
            {
                mock.Altitude = 0f;
                mock.AirSpeed = TSFEUtil.FromKnots(150f);
                mock.AirVel = mock.transform.forward * mock.AirSpeed;
                mock.Atmosphere = 1.0f;
                mock.Taxiing = false;
                EditorUtility.SetDirty(mock);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Cruise FL100 (300kt)", GUILayout.Height(25)))
            {
                mock.Altitude = 3048f; // 10,000 ft
                mock.AirSpeed = TSFEUtil.FromKnots(300f);
                mock.AirVel = mock.transform.forward * mock.AirSpeed;
                mock.Atmosphere = 0.74f;
                mock.Taxiing = false;
                EditorUtility.SetDirty(mock);
            }
            if (GUILayout.Button("Cruise FL300 (450kt)", GUILayout.Height(25)))
            {
                mock.Altitude = 9144f; // 30,000 ft
                mock.AirSpeed = TSFEUtil.FromKnots(450f);
                mock.AirVel = mock.transform.forward * mock.AirSpeed;
                mock.Atmosphere = 0.37f;
                mock.Taxiing = false;
                EditorUtility.SetDirty(mock);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Windmill (FL150, 350kt)", GUILayout.Height(25)))
            {
                mock.Altitude = 4572f; // 15,000 ft
                mock.AirSpeed = TSFEUtil.FromKnots(350f);
                mock.AirVel = mock.transform.forward * mock.AirSpeed;
                mock.Atmosphere = 0.64f;
                mock.Taxiing = false;
                EditorUtility.SetDirty(mock);
            }
            if (GUILayout.Button("APU Limit (FL200, 250kt)", GUILayout.Height(25)))
            {
                mock.Altitude = 6096f; // 20,000 ft (APU max altitude)
                mock.AirSpeed = TSFEUtil.FromKnots(250f);
                mock.AirVel = mock.transform.forward * mock.AirSpeed;
                mock.Atmosphere = 0.53f;
                mock.Taxiing = false;
                EditorUtility.SetDirty(mock);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawQuickControls(TSFE.SFEXT.MockSAVControl mock)
        {
            bool showQuickControls = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_QUICK_CONTROLS, "Quick Controls", true);
            if (!showQuickControls) return;

            EditorGUILayout.BeginVertical("box");

            // 速度コントロール
            EditorGUILayout.LabelField("Airspeed", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            float currentKts = TSFEUtil.ToKnots(mock.AirSpeed);
            float newKts = EditorGUILayout.Slider(currentKts, 0f, 500f);
            if (!Mathf.Approximately(currentKts, newKts))
            {
                mock.AirSpeed = TSFEUtil.FromKnots(newKts);
                mock.AirVel = mock.transform.forward * mock.AirSpeed;
                EditorUtility.SetDirty(mock);
            }
            EditorGUILayout.LabelField($"{newKts:F0} KIAS", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("0 kt"))
            {
                mock.AirSpeed = 0f;
                mock.AirVel = Vector3.zero;
                EditorUtility.SetDirty(mock);
            }
            if (GUILayout.Button("150 kt"))
            {
                mock.AirSpeed = TSFEUtil.FromKnots(150f);
                mock.AirVel = mock.transform.forward * mock.AirSpeed;
                EditorUtility.SetDirty(mock);
            }
            if (GUILayout.Button("250 kt"))
            {
                mock.AirSpeed = TSFEUtil.FromKnots(250f);
                mock.AirVel = mock.transform.forward * mock.AirSpeed;
                EditorUtility.SetDirty(mock);
            }
            if (GUILayout.Button("350 kt"))
            {
                mock.AirSpeed = TSFEUtil.FromKnots(350f);
                mock.AirVel = mock.transform.forward * mock.AirSpeed;
                EditorUtility.SetDirty(mock);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 高度コントロール
            EditorGUILayout.LabelField("Altitude", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            float currentFt = TSFEUtil.ToFeet(mock.Altitude);
            float newFt = EditorGUILayout.Slider(currentFt, 0f, 40000f);
            if (!Mathf.Approximately(currentFt, newFt))
            {
                mock.Altitude = TSFEUtil.FromFeet(newFt);
                // 高度に応じて大気密度を自動調整（簡易モデル）
                mock.Atmosphere = Mathf.Exp(-mock.Altitude / 8400f); // 8400m = scale height
                EditorUtility.SetDirty(mock);
            }
            EditorGUILayout.LabelField($"FL{(newFt / 100):F0}", GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ground"))
            {
                mock.Altitude = 0f;
                mock.Atmosphere = 1.0f;
                EditorUtility.SetDirty(mock);
            }
            if (GUILayout.Button("FL100"))
            {
                mock.Altitude = 3048f;
                mock.Atmosphere = 0.74f;
                EditorUtility.SetDirty(mock);
            }
            if (GUILayout.Button("FL200"))
            {
                mock.Altitude = 6096f;
                mock.Atmosphere = 0.53f;
                EditorUtility.SetDirty(mock);
            }
            if (GUILayout.Button("FL300"))
            {
                mock.Altitude = 9144f;
                mock.Atmosphere = 0.37f;
                EditorUtility.SetDirty(mock);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 燃料コントロール
            EditorGUILayout.LabelField("Fuel", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Empty", GUILayout.Height(25)))
            {
                mock.Fuel = 0f;
                EditorUtility.SetDirty(mock);
            }
            if (GUILayout.Button("25%", GUILayout.Height(25)))
            {
                mock.Fuel = mock.FullFuel * 0.25f;
                EditorUtility.SetDirty(mock);
            }
            if (GUILayout.Button("50%", GUILayout.Height(25)))
            {
                mock.Fuel = mock.FullFuel * 0.5f;
                EditorUtility.SetDirty(mock);
            }
            if (GUILayout.Button("Full", GUILayout.Height(25)))
            {
                mock.Fuel = mock.FullFuel;
                EditorUtility.SetDirty(mock);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawCurrentStatus(TSFE.SFEXT.MockSAVControl mock)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Current Status (Real-time)", EditorStyles.boldLabel);

            float kias = TSFEUtil.ToKnots(mock.AirSpeed);
            float altFt = TSFEUtil.ToFeet(mock.Altitude);
            float fuelPercent = mock.FullFuel > 0f ? (mock.Fuel / mock.FullFuel) * 100f : 0f;

            EditorGUILayout.LabelField("Airspeed", $"{mock.AirSpeed:F1} m/s ({kias:F0} KIAS)");
            EditorGUILayout.LabelField("Altitude", $"{mock.Altitude:F0} m (FL{altFt / 100:F0})");
            EditorGUILayout.LabelField("Atmosphere", $"{mock.Atmosphere:F2} ({mock.Atmosphere * 100:F0}%)");
            EditorGUILayout.LabelField("Fuel", $"{mock.Fuel:F0} / {mock.FullFuel:F0} kg ({fuelPercent:F0}%)");
            EditorGUILayout.LabelField("Throttle Input", mock.ThrottleInput.ToString("F2"));
            EditorGUILayout.LabelField("Throttle Strength", $"{mock.ThrottleStrength:F0} N");
            EditorGUILayout.LabelField("Extra Drag", mock.ExtraDrag.ToString("F3"));
            EditorGUILayout.LabelField("Extra Lift", mock.ExtraLift.ToString("F3"));
            EditorGUILayout.LabelField("Taxiing", mock.Taxiing ? "YES" : "NO");

            EditorGUILayout.EndVertical();
        }
    }
}
