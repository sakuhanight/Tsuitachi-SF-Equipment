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

        #region Response Time Calculator

        /// <summary>
        /// 目標時間からresponse rateを計算
        /// MoveTowards(current, target, response * Abs(target - current) * dt) の場合:
        /// 指数関数的収束: N(t) = target + (N0 - target) * exp(-response * t)
        /// 95%到達時間を基準: response = 3 / timeSeconds
        /// </summary>
        /// <param name="from">開始値</param>
        /// <param name="to">目標値</param>
        /// <param name="timeSeconds">95%到達時間（秒）</param>
        /// <returns>response rate</returns>
        public static float CalculateResponseRate(float from, float to, float timeSeconds)
        {
            if (timeSeconds <= 0f) return 0f;
            return 3f / timeSeconds;
        }

        /// <summary>
        /// response rateから目標時間を逆算
        /// 95%到達時間を返す
        /// </summary>
        /// <param name="from">開始値</param>
        /// <param name="to">目標値</param>
        /// <param name="response">response rate</param>
        /// <returns>95%到達時間（秒）</returns>
        public static float CalculateTimeFromResponse(float from, float to, float response)
        {
            if (response <= 0f) return 0f;
            return 3f / response;
        }

        /// <summary>
        /// Response Time Calculator: 時間とResponseパラメータの双方向編集GUI
        /// 時間フィールドを編集するとresponse rateが自動計算され、
        /// response rateを直接編集すると時間フィールドが自動更新される
        /// </summary>
        /// <param name="label">ラベル（例: "N2 Startup (0% → 25%)"）</param>
        /// <param name="fromValue">開始値</param>
        /// <param name="toValue">目標値</param>
        /// <param name="time">時間フィールド（ref）</param>
        /// <param name="response">response rate（ref）</param>
        /// <param name="previousTime">前回の時間値（ref）</param>
        /// <param name="previousResponse">前回のresponse値（ref）</param>
        /// <param name="targetObject">変更対象のUnityObject（Undo/SetDirty用）</param>
        /// <param name="responseFieldName">responseフィールドの名前（Undo表示用）</param>
        /// <returns>response rateが変更されたかどうか</returns>
        public static bool DrawResponseTimeField(
            string label,
            float fromValue,
            float toValue,
            ref float time,
            ref float response,
            ref float previousTime,
            ref float previousResponse,
            UnityEngine.Object targetObject,
            string responseFieldName)
        {
            bool responseChanged = false;

            // 初期化（最初の呼び出し時）
            if (previousTime < 0f)
            {
                previousTime = CalculateTimeFromResponse(fromValue, toValue, response);
                previousResponse = response;
            }

            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            float newTime = EditorGUILayout.FloatField("時間 (秒)", time);
            EditorGUILayout.LabelField("Response", $"{response:F4}");

            // 時間フィールドが変更された → responseを計算
            if (Mathf.Abs(newTime - previousTime) > 0.001f && newTime > 0f)
            {
                if (targetObject != null)
                {
                    Undo.RecordObject(targetObject, $"Update {responseFieldName} from Time");
                }

                response = CalculateResponseRate(fromValue, toValue, newTime);
                previousResponse = response;
                time = newTime;
                previousTime = newTime;

                if (targetObject != null)
                {
                    EditorUtility.SetDirty(targetObject);
                }

                responseChanged = true;
            }
            // responseが直接変更された → 時間を逆算
            else if (Mathf.Abs(response - previousResponse) > 0.0001f)
            {
                time = CalculateTimeFromResponse(fromValue, toValue, response);
                previousTime = time;
                previousResponse = response;
            }

            return responseChanged;
        }

        #endregion
    }
}
