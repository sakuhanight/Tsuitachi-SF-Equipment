using UdonSharpEditor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using TSFE.SFEXT;

namespace TSFE.Editor
{
    [CustomEditor(typeof(SFEXT_AutoFlaps))]
    public class SFEXT_AutoFlapsEditor : UnityEditor.Editor
    {
        private const string PREF_SHOW_DEBUG = "TSFE.AutoFlapsEditor.ShowDebug";

        private ReorderableList scheduleFlapAngleList;
        private ReorderableList scheduleSpeedMaxList;
        private ReorderableList scheduleAoaMinList;
        private ReorderableList scheduleGMinList;
        private ReorderableList scheduleMachMaxList;
        private ReorderableList schedulePriorityList;

        private SerializedProperty modeProp;
        private SerializedProperty scheduleFlapAngleProp;
        private SerializedProperty scheduleSpeedMaxProp;
        private SerializedProperty scheduleAoaMinProp;
        private SerializedProperty scheduleGMinProp;
        private SerializedProperty scheduleMachMaxProp;
        private SerializedProperty schedulePriorityProp;

        private void OnEnable()
        {
            modeProp = serializedObject.FindProperty("mode");
            scheduleFlapAngleProp = serializedObject.FindProperty("scheduleFlapAngle");
            scheduleSpeedMaxProp = serializedObject.FindProperty("scheduleSpeedMax");
            scheduleAoaMinProp = serializedObject.FindProperty("scheduleAoaMin");
            scheduleGMinProp = serializedObject.FindProperty("scheduleGMin");
            scheduleMachMaxProp = serializedObject.FindProperty("scheduleMachMax");
            schedulePriorityProp = serializedObject.FindProperty("schedulePriority");

            InitializeReorderableList();
        }

        private void InitializeReorderableList()
        {
            scheduleFlapAngleList = new ReorderableList(serializedObject, scheduleFlapAngleProp, true, true, true, true);
            scheduleFlapAngleList.drawHeaderCallback = (Rect rect) => { EditorGUI.LabelField(rect, "Flap Angle"); };
            scheduleFlapAngleList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = scheduleFlapAngleProp.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), element, GUIContent.none);
            };
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            var autoFlaps = (SFEXT_AutoFlaps)target;

            // References
            EditorGUILayout.LabelField("参照", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("advancedFlaps"), new GUIContent("高度フラップ"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SAVControl"), new GUIContent("SAVControl"));

            EditorGUILayout.Space();

            // Debug
            EditorGUILayout.LabelField("デバッグ", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enableDebugLog"), new GUIContent("デバッグログ有効化"));

            EditorGUILayout.Space();

            // Initial State
            EditorGUILayout.LabelField("初期状態", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoEnabledOnStart"), new GUIContent("起動時に有効化"));

            EditorGUILayout.Space();

            // Mode selection
            EditorGUILayout.LabelField("モード", EditorStyles.boldLabel);
            AutoFlapMode currentMode = (AutoFlapMode)modeProp.intValue;
            AutoFlapMode newMode = (AutoFlapMode)EditorGUILayout.EnumPopup("オートフラップモード", currentMode);
            if (newMode != currentMode)
            {
                modeProp.intValue = (int)newMode;
            }

            EditorGUILayout.Space();

            // Mode-specific settings
            if (currentMode == AutoFlapMode.Civilian || currentMode == AutoFlapMode.Military)
            {
                DrawScheduleEditor(currentMode);
            }
            else if (currentMode == AutoFlapMode.IDLC)
            {
                DrawIDLCSettings();
            }

            EditorGUILayout.Space();

            // Common settings
            DrawCommonSettings();

            serializedObject.ApplyModifiedProperties();

            // Play mode debug info
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(10);
                DrawPlayModeDebugInfo(autoFlaps);
                Repaint();
            }
        }

        private void DrawScheduleEditor(AutoFlapMode mode)
        {
            EditorGUILayout.LabelField("スケジュール（並行配列）", EditorStyles.boldLabel);

            // Array length validation
            int maxLength = Mathf.Max(
                scheduleFlapAngleProp.arraySize,
                scheduleSpeedMaxProp.arraySize,
                scheduleAoaMinProp.arraySize,
                scheduleGMinProp.arraySize,
                scheduleMachMaxProp.arraySize,
                schedulePriorityProp.arraySize
            );

            bool lengthMismatch = !(
                scheduleFlapAngleProp.arraySize == maxLength &&
                scheduleSpeedMaxProp.arraySize == maxLength &&
                scheduleAoaMinProp.arraySize == maxLength &&
                scheduleGMinProp.arraySize == maxLength &&
                scheduleMachMaxProp.arraySize == maxLength &&
                schedulePriorityProp.arraySize == maxLength
            );

            if (lengthMismatch)
            {
                EditorGUILayout.HelpBox("警告: 配列の長さが一致しません！すべてのスケジュール配列の長さを揃えてください。", MessageType.Warning);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("エントリ追加"))
            {
                scheduleFlapAngleProp.arraySize++;
                scheduleSpeedMaxProp.arraySize++;
                scheduleAoaMinProp.arraySize++;
                scheduleGMinProp.arraySize++;
                scheduleMachMaxProp.arraySize++;
                schedulePriorityProp.arraySize++;
            }
            if (GUILayout.Button("末尾削除") && scheduleFlapAngleProp.arraySize > 0)
            {
                scheduleFlapAngleProp.arraySize--;
                scheduleSpeedMaxProp.arraySize--;
                scheduleAoaMinProp.arraySize--;
                scheduleGMinProp.arraySize--;
                scheduleMachMaxProp.arraySize--;
                schedulePriorityProp.arraySize--;
            }
            if (GUILayout.Button("全削除"))
            {
                scheduleFlapAngleProp.arraySize = 0;
                scheduleSpeedMaxProp.arraySize = 0;
                scheduleAoaMinProp.arraySize = 0;
                scheduleGMinProp.arraySize = 0;
                scheduleMachMaxProp.arraySize = 0;
                schedulePriorityProp.arraySize = 0;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            // Table header
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("番号", EditorStyles.boldLabel, GUILayout.Width(40));
            EditorGUILayout.LabelField("フラップ角度", EditorStyles.boldLabel, GUILayout.Width(80));
            EditorGUILayout.LabelField("最大速度", EditorStyles.boldLabel, GUILayout.Width(80));

            if (mode == AutoFlapMode.Military)
            {
                EditorGUILayout.LabelField("最小AoA", EditorStyles.boldLabel, GUILayout.Width(70));
                EditorGUILayout.LabelField("最小G", EditorStyles.boldLabel, GUILayout.Width(60));
                EditorGUILayout.LabelField("最大Mach", EditorStyles.boldLabel, GUILayout.Width(80));
                EditorGUILayout.LabelField("優先度", EditorStyles.boldLabel, GUILayout.Width(70));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical("box");

            int count = Mathf.Min(
                scheduleFlapAngleProp.arraySize,
                Mathf.Min(scheduleSpeedMaxProp.arraySize,
                Mathf.Min(scheduleAoaMinProp.arraySize,
                Mathf.Min(scheduleGMinProp.arraySize,
                Mathf.Min(scheduleMachMaxProp.arraySize, schedulePriorityProp.arraySize))))
            );

            for (int i = 0; i < count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(i.ToString(), GUILayout.Width(40));

                EditorGUILayout.PropertyField(scheduleFlapAngleProp.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(80));
                EditorGUILayout.PropertyField(scheduleSpeedMaxProp.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(80));

                if (mode == AutoFlapMode.Military)
                {
                    EditorGUILayout.PropertyField(scheduleAoaMinProp.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(70));
                    EditorGUILayout.PropertyField(scheduleGMinProp.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(60));
                    EditorGUILayout.PropertyField(scheduleMachMaxProp.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(80));
                    EditorGUILayout.PropertyField(schedulePriorityProp.GetArrayElementAtIndex(i), GUIContent.none, GUILayout.Width(70));
                }

                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    scheduleFlapAngleProp.DeleteArrayElementAtIndex(i);
                    scheduleSpeedMaxProp.DeleteArrayElementAtIndex(i);
                    scheduleAoaMinProp.DeleteArrayElementAtIndex(i);
                    scheduleGMinProp.DeleteArrayElementAtIndex(i);
                    scheduleMachMaxProp.DeleteArrayElementAtIndex(i);
                    schedulePriorityProp.DeleteArrayElementAtIndex(i);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Militaryモードでパラメータを無視する場合は -1 を使用してください。", MessageType.Info);
        }

        private void DrawIDLCSettings()
        {
            EditorGUILayout.LabelField("IDLC設定", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("idlcBaseAngle"), new GUIContent("基準角度"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idlcPitchGain"), new GUIContent("ピッチゲイン"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idlcThrottleGain"), new GUIContent("スロットルゲイン"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idlcAngleMin"), new GUIContent("最小角度"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idlcAngleMax"), new GUIContent("最大角度"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("idlcFallbackIAS"), new GUIContent("フォールバック速度"));

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("IDLCモードは、速度がフォールバック速度を超えるとCivilianモードに切り替わります。", MessageType.Info);
        }

        private void DrawCommonSettings()
        {
            EditorGUILayout.LabelField("ヒステリシス", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("extendHysteresisKnots"), new GUIContent("展開ヒステリシス (kt)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("retractMarginKnots"), new GUIContent("収納マージン (kt)"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("制限", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inhibitOnGearUp"), new GUIContent("脚収納時制限"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("inhibitMaxAngle"), new GUIContent("最大許容角度 (°)"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("タイミング", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("changeDebounceTime"), new GUIContent("変更間隔 (秒)"));
        }

        private void DrawPlayModeDebugInfo(SFEXT_AutoFlaps autoFlaps)
        {
            EditorGUILayout.LabelField("プレイモードデバッグ情報", EditorStyles.boldLabel);

            bool showDebug = TSFEEditorUtil.DrawPersistentFoldout(PREF_SHOW_DEBUG, "実行時状態", true);

            if (showDebug)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Auto Flap State
                EditorGUILayout.LabelField("オートフラップ状態", EditorStyles.boldLabel);

                bool autoActive = (bool)typeof(SFEXT_AutoFlaps).GetField("_autoActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(autoFlaps);
                TSFEEditorUtil.DrawStateLabel("オート有効", autoActive, "有効", "無効");

                EditorGUILayout.Space(5);

                // Mode
                EditorGUILayout.LabelField("現在のモード", EditorStyles.boldLabel);
                AutoFlapMode currentMode = (AutoFlapMode)autoFlaps.mode;
                string modeText = currentMode == AutoFlapMode.Civilian ? "民間機" :
                                  currentMode == AutoFlapMode.Military ? "軍用機" : "IDLC";
                EditorGUILayout.LabelField("モード", modeText);

                EditorGUILayout.Space(5);

                // Commanded Angle
                EditorGUILayout.LabelField("角度制御", EditorStyles.boldLabel);
                float commandedAngle = (float)typeof(SFEXT_AutoFlaps).GetField("_commandedAngle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(autoFlaps);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("コマンド済み角度");
                EditorGUILayout.LabelField($"{commandedAngle:F1}°", EditorStyles.textField);
                EditorGUILayout.EndHorizontal();

                if (autoFlaps.advancedFlaps != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("目標角度（フラップ）");
                    EditorGUILayout.LabelField($"{autoFlaps.advancedFlaps.targetAngle:F1}° (デテント#{autoFlaps.advancedFlaps.targetDetentIndex})", EditorStyles.textField);
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PrefixLabel("現在角度（フラップ）");
                    EditorGUILayout.LabelField($"{autoFlaps.advancedFlaps.detentAngle:F1}° (デテント#{autoFlaps.advancedFlaps.detentIndex})", EditorStyles.textField);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.Space(5);

                // Flight State
                if (autoFlaps.SAVControl != null)
                {
                    EditorGUILayout.LabelField("飛行状態", EditorStyles.boldLabel);

                    float airSpeedMS = (float)autoFlaps.SAVControl.GetProgramVariable("AirSpeed");
                    float ias = TSFE.Utility.TSFEUtil.ToKnots(airSpeedMS);
                    EditorGUILayout.LabelField("対気速度", $"{ias:F1} KIAS");

                    if (currentMode == AutoFlapMode.Military || currentMode == AutoFlapMode.IDLC)
                    {
                        // HUDと同じAngleOfAttackPitchフィールドを使用（既に度数）
                        object aoaPitchObj = autoFlaps.SAVControl.GetProgramVariable("AngleOfAttackPitch");
                        if (aoaPitchObj != null)
                        {
                            float aoa = (float)aoaPitchObj;
                            EditorGUILayout.LabelField("迎角", $"{aoa:F1}°");
                        }

                        object gForceObj = autoFlaps.SAVControl.GetProgramVariable("GForces");
                        if (gForceObj != null)
                        {
                            float g = (float)gForceObj;
                            EditorGUILayout.LabelField("G荷重", $"{g:F2}G");
                        }

                        float mach = airSpeedMS / 340f;
                        EditorGUILayout.LabelField("マッハ数", $"{mach:F2}");
                    }
                }

                EditorGUILayout.EndVertical();
            }
        }
    }
}
