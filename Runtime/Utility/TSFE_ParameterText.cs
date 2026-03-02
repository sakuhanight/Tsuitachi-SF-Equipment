using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TSFE.Utility
{
    /// <summary>
    /// 汎用パラメータ表示コンポーネント
    /// 任意のUdonSharpコンポーネントのパラメータをC#標準書式で表示
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TSFE_ParameterText : UdonSharpBehaviour
    {
        [Header("対象設定")]
        [Tooltip("パラメータを取得する対象コンポーネント")]
        public UdonSharpBehaviour targetComponent;

        [Tooltip("取得するパラメータ名（フィールド/プロパティ）")]
        public string parameterName;

        [Header("表示設定")]
        [Tooltip("表示用テキスト（TextMeshPro）")]
        public TextMeshProUGUI displayText;

        [Tooltip("表示用テキスト（Unity UI Text）")]
        public Text displayTextLegacy;

        [Tooltip("C#標準書式\n" +
                 "例:\n" +
                 "  \"F2\" → 123.46\n" +
                 "  \"F1\" → 123.5\n" +
                 "  \"F0\" → 123\n" +
                 "  \"0.00\" → 45.00\n" +
                 "  \"000\" → 045\n" +
                 "  \"#,##0\" → 1,234\n" +
                 "  \"ON/OFF\" → ON or OFF (Boolean用)\n" +
                 "  \"FLAP: UP/DN\" → FLAP: UP or FLAP: DN\n" +
                 "空欄の場合は既定値")]
        public string formatString = "";

        [Header("数値変換設定")]
        [Tooltip("数値に掛ける係数（例: m/s→kt変換は1.94384）\n1.0以外を指定すると係数が適用されます")]
        public float multiplier = 1.0f;

        [Tooltip("数値に加えるオフセット（係数適用後に加算）\n例: ℃→℉変換は multiplier=1.8, offset=32")]
        public float offset = 0.0f;

        [Header("Bool値設定（formatStringが指定されていない場合）")]
        [Tooltip("bool値がtrueの場合の表示")]
        public string boolTrueText = "ON";

        [Tooltip("bool値がfalseの場合の表示")]
        public string boolFalseText = "OFF";

        [Header("オプション")]
        [Tooltip("配列の場合のインデックス（-1で無効）")]
        public int arrayIndex = -1;

        [Tooltip("更新間隔（秒）")]
        public float updateInterval = 0.1f;

        [Tooltip("値が無効な場合の表示")]
        public string invalidValueText = "----";

        private float lastUpdateTime = 0f;

        void Update()
        {
            if (Time.time - lastUpdateTime < updateInterval)
                return;

            UpdateDisplay();
            lastUpdateTime = Time.time;
        }

        private void UpdateDisplay()
        {
            if (targetComponent == null)
                return;

            if (displayText == null && displayTextLegacy == null)
                return;

            object value = GetParameterValue();
            string formattedValue = FormatValue(value);

            if (displayText != null)
                displayText.text = formattedValue;

            if (displayTextLegacy != null)
                displayTextLegacy.text = formattedValue;
        }

        private object GetParameterValue()
        {
            if (targetComponent == null)
                return null;

            object value = targetComponent.GetProgramVariable(parameterName);

            // 配列の場合
            if (arrayIndex >= 0)
            {
                if (value == null)
                    return null;

                // 型名で判定（UdonSharpのobject型制約回避）
                string typeName = value.GetType().Name;

                if (typeName == "Single[]") // float[]
                {
                    float[] arr = (float[])value;
                    if (arrayIndex < arr.Length)
                        return arr[arrayIndex];
                }
                else if (typeName == "Int32[]") // int[]
                {
                    int[] arr = (int[])value;
                    if (arrayIndex < arr.Length)
                        return arr[arrayIndex];
                }
                else if (typeName == "Boolean[]") // bool[]
                {
                    bool[] arr = (bool[])value;
                    if (arrayIndex < arr.Length)
                        return arr[arrayIndex];
                }
                else if (typeName == "String[]") // string[]
                {
                    string[] arr = (string[])value;
                    if (arrayIndex < arr.Length)
                        return arr[arrayIndex];
                }
            }

            return value;
        }

        private string FormatValue(object value)
        {
            if (value == null)
                return invalidValueText;

            // 型名で判定（UdonSharpのobject型制約回避）
            string typeName = value.GetType().Name;

            // bool値の処理
            if (typeName == "Boolean")
            {
                bool b = (bool)value;

                // formatString内に "/" が含まれる場合、True/False表記として解釈
                if (formatString != null && formatString.Length > 0)
                {
                    int slashIndex = formatString.IndexOf('/');
                    if (slashIndex >= 0)
                    {
                        // "ON/OFF" や "PWR: ON/OFF" のような形式をパース
                        string truePart = formatString.Substring(0, slashIndex);
                        string falsePart = formatString.Substring(slashIndex + 1);

                        // 共通接頭辞を検出（例: "PWR: ON/OFF" → 接頭辞="PWR: "）
                        string prefix = "";
                        int lastSpace = truePart.LastIndexOf(' ');
                        if (lastSpace >= 0)
                        {
                            prefix = truePart.Substring(0, lastSpace + 1);
                            truePart = truePart.Substring(lastSpace + 1);
                        }

                        return prefix + (b ? truePart : falsePart);
                    }
                }

                // formatStringが指定されていない場合はフィールド値を使用
                return b ? boolTrueText : boolFalseText;
            }

            // 数値の処理
            if (typeName == "Single") // float
            {
                float f = (float)value;

                // 係数とオフセットを適用
                f = f * multiplier + offset;

                if (formatString == null || formatString.Length == 0)
                    return f.ToString("F2");
                else
                    return f.ToString(formatString);
            }
            else if (typeName == "Int32") // int
            {
                int i = (int)value;

                // 係数とオフセットを適用（floatに変換）
                float f = (float)i * multiplier + offset;

                if (formatString == null || formatString.Length == 0)
                {
                    // multiplierとoffsetが既定値の場合は整数として表示、それ以外は小数として表示
                    if (Mathf.Approximately(multiplier, 1.0f) && Mathf.Approximately(offset, 0.0f))
                        return i.ToString();
                    else
                        return f.ToString("F2");
                }
                else
                {
                    return f.ToString(formatString);
                }
            }

            // 文字列の処理
            if (typeName == "String")
            {
                return (string)value;
            }

            // その他
            return value.ToString();
        }
    }
}
