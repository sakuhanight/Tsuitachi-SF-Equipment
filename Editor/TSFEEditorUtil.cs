using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    /// <summary>
    /// TSFE Editor用ユーティリティクラス
    /// 色定義、GUI描画ヘルパー、State表示ヘルパー等
    /// </summary>
    public static class TSFEEditorUtil
    {
        // ========================================
        // 状態色定義
        // ========================================

        public static readonly Color StateOnColor = Color.green;
        public static readonly Color StateOffColor = Color.red;
        public static readonly Color StateInactiveColor = Color.gray;
        public static readonly Color StateWarningColor = Color.yellow;
        public static readonly Color StateTransitionColor = new Color(1f, 0.5f, 0f); // オレンジ
        public static readonly Color StateInfoColor = Color.cyan;

        // ========================================
        // ColorScope（using対応、自動復元）
        // ========================================

        /// <summary>
        /// GUI.colorを一時的に変更するスコープ
        /// using構文で使用することで、スコープ終了時に自動復元される
        /// </summary>
        /// <example>
        /// using (new TSFEEditorUtil.ColorScope(Color.green))
        /// {
        ///     EditorGUILayout.LabelField("Status", "ON");
        /// } // ここで自動的にGUI.colorが元に戻る
        /// </example>
        public struct ColorScope : System.IDisposable
        {
            private readonly Color previousColor;

            public ColorScope(Color color)
            {
                previousColor = GUI.color;
                GUI.color = color;
            }

            public void Dispose()
            {
                GUI.color = previousColor;
            }
        }

        // ========================================
        // 汎用State表示ヘルパー
        // ========================================

        /// <summary>
        /// ON/OFF状態をラベル表示（色付き）
        /// </summary>
        public static void DrawStateLabel(string label, bool isOn, string onText = "ON", string offText = "OFF")
        {
            using (new ColorScope(isOn ? StateOnColor : StateOffColor))
            {
                EditorGUILayout.LabelField(label, isOn ? onText : offText);
            }
        }

        /// <summary>
        /// ON/OFF状態をボールドラベル表示（色付き）
        /// </summary>
        public static void DrawStateLabelBold(string label, bool isOn, string onText = "ON", string offText = "OFF")
        {
            using (new ColorScope(isOn ? StateOnColor : StateOffColor))
            {
                EditorGUILayout.LabelField(label, isOn ? onText : offText, EditorStyles.boldLabel);
            }
        }

        // ========================================
        // APU State表示ヘルパー
        // ========================================

        /// <summary>
        /// APU状態を色付きラベルで表示
        /// </summary>
        public static void DrawAPUStateLabel(TSFE.SFEXT.APUState state)
        {
            GetAPUStateDisplay(state, out Color color, out string text);

            using (new ColorScope(color))
            {
                EditorGUILayout.LabelField("State", text, EditorStyles.boldLabel);
            }
        }

        /// <summary>
        /// APU状態を色付きラベルで表示（カスタムスタイル）
        /// </summary>
        public static void DrawAPUStateLabel(TSFE.SFEXT.APUState state, GUIStyle style)
        {
            GetAPUStateDisplay(state, out Color color, out string text);

            using (new ColorScope(color))
            {
                EditorGUILayout.LabelField("State", text, style);
            }
        }

        /// <summary>
        /// APU状態の表示用色とテキストを取得
        /// </summary>
        public static void GetAPUStateDisplay(TSFE.SFEXT.APUState state, out Color color, out string text)
        {
            switch (state)
            {
                case TSFE.SFEXT.APUState.Off:
                    color = StateOffColor;
                    text = "OFF";
                    break;
                case TSFE.SFEXT.APUState.Starting:
                    color = StateWarningColor;
                    text = "STARTING";
                    break;
                case TSFE.SFEXT.APUState.Running:
                    color = StateOnColor;
                    text = "RUNNING";
                    break;
                case TSFE.SFEXT.APUState.Stopping:
                    color = StateTransitionColor;
                    text = "STOPPING";
                    break;
                default:
                    color = Color.white;
                    text = "UNKNOWN";
                    break;
            }
        }

        // ========================================
        // Engine State表示ヘルパー
        // ========================================

        /// <summary>
        /// Engine状態を色付きラベルで表示
        /// </summary>
        public static void DrawEngineStateLabel(TSFE.SFEXT.EngineState state)
        {
            GetEngineStateDisplay(state, out Color color, out string text);

            using (new ColorScope(color))
            {
                EditorGUILayout.LabelField("State", text, EditorStyles.boldLabel);
            }
        }

        /// <summary>
        /// Engine状態を色付きラベルで表示（カスタムスタイル）
        /// </summary>
        public static void DrawEngineStateLabel(TSFE.SFEXT.EngineState state, GUIStyle style)
        {
            GetEngineStateDisplay(state, out Color color, out string text);

            using (new ColorScope(color))
            {
                EditorGUILayout.LabelField("State", text, style);
            }
        }

        /// <summary>
        /// Engine状態の表示用色とテキストを取得
        /// </summary>
        public static void GetEngineStateDisplay(TSFE.SFEXT.EngineState state, out Color color, out string text)
        {
            switch (state)
            {
                case TSFE.SFEXT.EngineState.Off:
                    color = StateOffColor;
                    text = "OFF";
                    break;
                case TSFE.SFEXT.EngineState.Windmilling:
                    color = StateInfoColor;
                    text = "WINDMILLING";
                    break;
                case TSFE.SFEXT.EngineState.Starting:
                    color = StateWarningColor;
                    text = "STARTING";
                    break;
                case TSFE.SFEXT.EngineState.Running:
                    color = StateOnColor;
                    text = "RUNNING";
                    break;
                case TSFE.SFEXT.EngineState.Seized:
                    color = StateOffColor;
                    text = "SEIZED";
                    break;
                default:
                    color = Color.white;
                    text = "UNKNOWN";
                    break;
            }
        }

        // ========================================
        // AutoStarter SequenceState表示ヘルパー
        // ========================================

        /// <summary>
        /// AutoStarterシーケンス状態を色付きラベルで表示
        /// </summary>
        public static void DrawAutoStarterStateLabel(TSFE.SFEXT.AutoStarterSequenceState state)
        {
            GetAutoStarterStateDisplay(state, out Color color, out string text);

            using (new ColorScope(color))
            {
                EditorGUILayout.LabelField("Sequence State", text, EditorStyles.boldLabel);
            }
        }

        /// <summary>
        /// AutoStarterシーケンス状態の表示用色とテキストを取得
        /// </summary>
        public static void GetAutoStarterStateDisplay(TSFE.SFEXT.AutoStarterSequenceState state, out Color color, out string text)
        {
            switch (state)
            {
                case TSFE.SFEXT.AutoStarterSequenceState.Idle:
                    color = StateInactiveColor;
                    text = "IDLE";
                    break;
                case TSFE.SFEXT.AutoStarterSequenceState.StartingBattery:
                    color = StateWarningColor;
                    text = "STARTING BATTERY";
                    break;
                case TSFE.SFEXT.AutoStarterSequenceState.StartingAPU:
                    color = StateWarningColor;
                    text = "STARTING APU";
                    break;
                case TSFE.SFEXT.AutoStarterSequenceState.WaitingAPU:
                    color = StateWarningColor;
                    text = "WAITING APU";
                    break;
                case TSFE.SFEXT.AutoStarterSequenceState.StartingEngines:
                    color = StateWarningColor;
                    text = "STARTING ENGINES";
                    break;
                case TSFE.SFEXT.AutoStarterSequenceState.WaitingEngines:
                    color = StateWarningColor;
                    text = "WAITING ENGINES";
                    break;
                case TSFE.SFEXT.AutoStarterSequenceState.StoppingAPU:
                    color = StateTransitionColor;
                    text = "STOPPING APU";
                    break;
                case TSFE.SFEXT.AutoStarterSequenceState.Completed:
                    color = StateOnColor;
                    text = "COMPLETED";
                    break;
                case TSFE.SFEXT.AutoStarterSequenceState.Failed:
                    color = StateOffColor;
                    text = "FAILED";
                    break;
                default:
                    color = Color.white;
                    text = "UNKNOWN";
                    break;
            }
        }

        // ========================================
        // EditorPrefs Foldout永続化ヘルパー
        // ========================================

        /// <summary>
        /// EditorPrefsで永続化されたFoldout状態を取得
        /// </summary>
        public static bool GetFoldoutState(string key, bool defaultValue = true)
        {
            return EditorPrefs.GetBool(key, defaultValue);
        }

        /// <summary>
        /// EditorPrefsでFoldout状態を保存
        /// </summary>
        public static void SetFoldoutState(string key, bool value)
        {
            EditorPrefs.SetBool(key, value);
        }

        /// <summary>
        /// Foldoutを描画し、状態変更があればEditorPrefsに保存
        /// </summary>
        /// <param name="key">EditorPrefsキー（例: "TSFE.EngineEditor.ShowControls"）</param>
        /// <param name="content">表示テキスト</param>
        /// <param name="defaultValue">デフォルト値</param>
        /// <returns>現在のFoldout状態</returns>
        public static bool DrawPersistentFoldout(string key, string content, bool defaultValue = true)
        {
            bool currentState = GetFoldoutState(key, defaultValue);
            bool newState = EditorGUILayout.Foldout(currentState, content, true, EditorStyles.foldoutHeader);

            if (newState != currentState)
            {
                SetFoldoutState(key, newState);
            }

            return newState;
        }
    }
}
