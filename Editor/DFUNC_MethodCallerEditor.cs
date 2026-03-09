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

                    using (new TSFEEditorUtil.ColorScope(TSFEEditorUtil.StateInfoColor))
                    {
                        if (GUILayout.Button($"Execute: {method}()", GUILayout.Height(40)))
                        {
                            methodCaller.Execute();
                        }
                    }
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
                EditorGUILayout.HelpBox("Method Name を設定してください（例: Toggle）", MessageType.Error);
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // 使用例
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("使用例", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "このDFUNCコンポーネントはシンプルなメソッド呼び出しに対応します:\n\n" +
                "【VR操作】\n" +
                "  1. DFUNCで選択（スティック上下）\n" +
                "  2. 該当する側のトリガーを引く → メソッド実行\n" +
                "     - 左ダイヤル配置 → 左トリガー\n" +
                "     - 右ダイヤル配置 → 右トリガー\n\n" +
                "【デスクトップ操作】\n" +
                "  - keyCode で設定したキーを押す → メソッド実行\n" +
                "    （デフォルト: G キー）\n\n" +
                "対象コンポーネントの公開メソッド（引数なし）を呼び出します。",
                MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // デフォルトインスペクタ
            DrawDefaultInspector();
        }
    }
}
