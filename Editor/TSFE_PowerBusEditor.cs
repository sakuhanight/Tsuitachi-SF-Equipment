using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.Utility.TSFE_PowerBus))]
    public class TSFE_PowerBusEditor : UnityEditor.Editor
    {
        private const string PREF_SHOW_CONTROLS = "TSFE.PowerBusEditor.ShowControls";
        private const string PREF_SHOW_STATE = "TSFE.PowerBusEditor.ShowState";

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var powerBus = (TSFE.Utility.TSFE_PowerBus)target;

            EditorGUILayout.Space();

            // Play Mode中のみコントロール表示
            if (Application.isPlaying)
            {
                bool showControls = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_CONTROLS, "Power Controls (Play Mode)", true);
                if (showControls)
                {
                    EditorGUILayout.BeginVertical("box");

                    // バッテリーコントロール
                    if (powerBus.batteryPoweredIndicator != null)
                    {
                        EditorGUILayout.LabelField("Battery Control", EditorStyles.boldLabel);

                        using (new TSFEEditorUtil.ColorScope(powerBus.BatteryOn ? TSFEEditorUtil.StateOnColor : TSFEEditorUtil.StateOffColor))
                        {
                            if (GUILayout.Button(powerBus.BatteryOn ? "Battery: ON" : "Battery: OFF", GUILayout.Height(40)))
                            {
                                powerBus.ToggleBattery();
                            }
                        }

                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Turn ON", GUILayout.Height(30)))
                        {
                            powerBus.SetBatteryOn();
                        }
                        if (GUILayout.Button("Turn OFF", GUILayout.Height(30)))
                        {
                            powerBus.SetBatteryOff();
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateInactiveColor))
                        {
                            EditorGUILayout.LabelField("Battery", "Disabled (batteryPoweredIndicator = null)");
                        }
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space();

                bool showState = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_STATE, "Power State (Real-time)", true);
                if (showState)
                {
                    EditorGUILayout.BeginVertical("box");

                    // バッテリー状態
                    EditorGUILayout.LabelField("Battery", EditorStyles.boldLabel);
                    if (powerBus.batteryPoweredIndicator != null)
                    {
                        TSFEEditorUtil.DrawStateLabel("Switch Status", powerBus.BatteryOn);

                        bool indicatorActive = powerBus.batteryPoweredIndicator.activeInHierarchy;
                        TSFEEditorUtil.DrawStateLabel("Indicator GameObject", indicatorActive, "Active", "Inactive");

                        if (indicatorActive && !powerBus.BatteryOn)
                        {
                            using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateInfoColor))
                            {
                                EditorGUILayout.LabelField("Note", "Active by Bus Power");
                            }
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Status", "Disabled (No Indicator)");
                    }

                    EditorGUILayout.Space();

                    // バス電源状態
                    EditorGUILayout.LabelField("Bus Power (Auto)", EditorStyles.boldLabel);
                    TSFEEditorUtil.DrawStateLabel("Status", powerBus.BusPowered, "POWERED", "NOT POWERED");

                    // 電源ソース表示
                    EditorGUILayout.LabelField("Power Sources", EditorStyles.miniBoldLabel);

                    // APU
                    if (powerBus.apuStartedIndicator != null)
                    {
                        bool apuActive = powerBus.apuStartedIndicator.activeInHierarchy;
                        using (new TSFEEditorUtil.ColorScope(apuActive ? TSFEEditorUtil.StateOnColor : TSFEEditorUtil.StateInactiveColor))
                        {
                            EditorGUILayout.LabelField("  APU", apuActive ? "RUNNING" : "OFF");
                        }
                    }

                    // エンジン
                    if (powerBus.engineOnIndicators != null && powerBus.engineOnIndicators.Length > 0)
                    {
                        for (int i = 0; i < powerBus.engineOnIndicators.Length; i++)
                        {
                            if (powerBus.engineOnIndicators[i] != null)
                            {
                                bool engineActive = powerBus.engineOnIndicators[i].activeInHierarchy;
                                using (new TSFEEditorUtil.ColorScope(engineActive ? TSFEEditorUtil.StateOnColor : TSFEEditorUtil.StateInactiveColor))
                                {
                                    EditorGUILayout.LabelField($"  Engine {i + 1}", engineActive ? "ON" : "OFF");
                                }
                            }
                        }
                    }

                    // GPU
                    if (powerBus.gpuObject != null)
                    {
                        bool gpuActive = powerBus.gpuObject.activeInHierarchy;
                        using (new TSFEEditorUtil.ColorScope(gpuActive ? TSFEEditorUtil.StateOnColor : TSFEEditorUtil.StateInactiveColor))
                        {
                            EditorGUILayout.LabelField("  GPU (Ground Power)", gpuActive ? "CONNECTED" : "DISCONNECTED");
                        }
                    }

                    EditorGUILayout.Space();

                    // バス電源インジケータ
                    if (powerBus.busPoweredIndicator != null)
                    {
                        bool indicatorActive = powerBus.busPoweredIndicator.activeInHierarchy;
                        EditorGUILayout.LabelField("Bus Indicator GameObject", indicatorActive ? "Active" : "Inactive");
                    }
                    else
                    {
                        using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateWarningColor))
                        {
                            EditorGUILayout.LabelField("Bus Indicator GameObject", "Not Set");
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
                EditorGUILayout.HelpBox("Play Mode に入るとバッテリー操作と状態表示が利用可能になります", MessageType.Info);
            }

            EditorGUILayout.Space();

            // デフォルトインスペクタ表示
            DrawDefaultInspector();
        }
    }
}
