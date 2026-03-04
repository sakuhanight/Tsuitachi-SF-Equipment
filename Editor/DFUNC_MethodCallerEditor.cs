using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.DFUNC.DFUNC_MethodCaller))]
    public class DFUNC_MethodCallerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var methodCaller = target as TSFE.DFUNC.DFUNC_MethodCaller;
            if (methodCaller == null) return;

            EditorGUILayout.Space();

            // Play Mode中のテストボタン
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Method Caller Test (Play Mode)", EditorStyles.boldLabel);

                var targetComp = methodCaller.targetComponent;
                var method = methodCaller.methodName;

                if (targetComp == null)
                {
                    EditorGUILayout.HelpBox("Target Component が設定されていません", MessageType.Warning);
                }
                else if (string.IsNullOrEmpty(method))
                {
                    EditorGUILayout.HelpBox("Method Name が設定されていません", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.LabelField("Target", targetComp.GetType().Name);
                    EditorGUILayout.LabelField("Method", method);

                    EditorGUILayout.Space();

                    GUI.color = Color.cyan;
                    if (GUILayout.Button($"Execute: {method}()", GUILayout.Height(40)))
                    {
                        methodCaller.Execute();
                    }
                    GUI.color = Color.white;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            // 設定の検証とヘルプ
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

            if (methodCaller.targetComponent == null)
            {
                EditorGUILayout.HelpBox("Target Component を設定してください", MessageType.Error);
            }

            if (string.IsNullOrEmpty(methodCaller.methodName))
            {
                EditorGUILayout.HelpBox("Method Name を設定してください（例: StartSequence）", MessageType.Error);
            }

            // 実行タイミングのチェック
            bool hasExecutionTiming = methodCaller.executeOnSelected ||
                                     methodCaller.executeOnDeselected ||
                                     methodCaller.executeOnLeftDial ||
                                     methodCaller.executeOnRightDial ||
                                     methodCaller.executeOnTriggerPress ||
                                     methodCaller.executeOnKeyDown;

            if (!hasExecutionTiming)
            {
                EditorGUILayout.HelpBox("少なくとも1つの実行タイミングを有効化してください", MessageType.Warning);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 使用例
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("使用例", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "このDFUNCコンポーネントはVRダイヤルとキーボード入力に対応します:\n\n" +
                "1. VRダイヤル操作\n" +
                "   - ダイヤル選択時 (executeOnSelected)\n" +
                "   - ダイヤル選択解除時 (executeOnDeselected)\n" +
                "   - 左ダイヤル回転時 (executeOnLeftDial)\n" +
                "   - 右ダイヤル回転時 (executeOnRightDial)\n" +
                "   - VRトリガー押下時 (executeOnTriggerPress)\n\n" +
                "2. キーボード入力\n" +
                "   - executeOnKeyDown を有効化\n" +
                "   - keyCode を設定 (例: KeyCode.G)\n\n" +
                "対象コンポーネントの公開メソッド（引数なし）を呼び出します。",
                MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // デフォルトインスペクタ
            DrawDefaultInspector();
        }
    }
}
