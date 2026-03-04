using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.Utility.TSFE_PowerBus))]
    public class TSFE_PowerBusEditor : UnityEditor.Editor
    {
        private bool showControls = true;
        private bool showState = true;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var powerBus = (TSFE.Utility.TSFE_PowerBus)target;

            EditorGUILayout.Space();

            // Play Mode中のみコントロール表示
            if (Application.isPlaying)
            {
                showControls = EditorGUILayout.Foldout(showControls, "Power Controls (Play Mode)", true, EditorStyles.foldoutHeader);
                if (showControls)
                {
                    EditorGUILayout.BeginVertical("box");

                    // バッテリーコントロール
                    if (powerBus.batteryPoweredIndicator != null)
                    {
                        EditorGUILayout.LabelField("Battery Control", EditorStyles.boldLabel);

                        GUI.color = powerBus.BatteryOn ? Color.green : Color.red;
                        if (GUILayout.Button(powerBus.BatteryOn ? "Battery: ON" : "Battery: OFF", GUILayout.Height(40)))
                        {
                            powerBus.ToggleBattery();
                        }
                        GUI.color = Color.white;

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
                        GUI.color = Color.gray;
                        EditorGUILayout.LabelField("Battery", "Disabled (batteryPoweredIndicator = null)");
                        GUI.color = Color.white;
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space();

                showState = EditorGUILayout.Foldout(showState, "Power State (Real-time)", true, EditorStyles.foldoutHeader);
                if (showState)
                {
                    EditorGUILayout.BeginVertical("box");

                    // バッテリー状態
                    EditorGUILayout.LabelField("Battery", EditorStyles.boldLabel);
                    if (powerBus.batteryPoweredIndicator != null)
                    {
                        GUI.color = powerBus.BatteryOn ? Color.green : Color.red;
                        EditorGUILayout.LabelField("Switch Status", powerBus.BatteryOn ? "ON" : "OFF");
                        GUI.color = Color.white;

                        bool indicatorActive = powerBus.batteryPoweredIndicator.activeInHierarchy;
                        GUI.color = indicatorActive ? Color.green : Color.red;
                        EditorGUILayout.LabelField("Indicator GameObject", indicatorActive ? "Active" : "Inactive");
                        GUI.color = Color.white;

                        if (indicatorActive && !powerBus.BatteryOn)
                        {
                            GUI.color = Color.cyan;
                            EditorGUILayout.LabelField("Note", "Active by Bus Power");
                            GUI.color = Color.white;
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Status", "Disabled (No Indicator)");
                    }

                    EditorGUILayout.Space();

                    // バス電源状態
                    EditorGUILayout.LabelField("Bus Power (Auto)", EditorStyles.boldLabel);
                    GUI.color = powerBus.BusPowered ? Color.green : Color.red;
                    EditorGUILayout.LabelField("Status", powerBus.BusPowered ? "POWERED" : "NOT POWERED");
                    GUI.color = Color.white;

                    // 電源ソース表示
                    EditorGUILayout.LabelField("Power Sources", EditorStyles.miniBoldLabel);

                    // APU
                    if (powerBus.apuStartedIndicator != null)
                    {
                        bool apuActive = powerBus.apuStartedIndicator.activeInHierarchy;
                        GUI.color = apuActive ? Color.green : Color.gray;
                        EditorGUILayout.LabelField("  APU", apuActive ? "RUNNING" : "OFF");
                        GUI.color = Color.white;
                    }

                    // エンジン
                    if (powerBus.engineOnIndicators != null && powerBus.engineOnIndicators.Length > 0)
                    {
                        for (int i = 0; i < powerBus.engineOnIndicators.Length; i++)
                        {
                            if (powerBus.engineOnIndicators[i] != null)
                            {
                                bool engineActive = powerBus.engineOnIndicators[i].activeInHierarchy;
                                GUI.color = engineActive ? Color.green : Color.gray;
                                EditorGUILayout.LabelField($"  Engine {i + 1}", engineActive ? "ON" : "OFF");
                                GUI.color = Color.white;
                            }
                        }
                    }

                    // GPU
                    if (powerBus.gpuObject != null)
                    {
                        bool gpuActive = powerBus.gpuObject.activeInHierarchy;
                        GUI.color = gpuActive ? Color.green : Color.gray;
                        EditorGUILayout.LabelField("  GPU (Ground Power)", gpuActive ? "CONNECTED" : "DISCONNECTED");
                        GUI.color = Color.white;
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
                        GUI.color = Color.yellow;
                        EditorGUILayout.LabelField("Bus Indicator GameObject", "Not Set");
                        GUI.color = Color.white;
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
