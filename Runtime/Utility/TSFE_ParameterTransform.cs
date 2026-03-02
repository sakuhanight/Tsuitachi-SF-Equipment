using UdonSharp;
using UnityEngine;

namespace TSFE.Utility
{
    /// <summary>
    /// 汎用パラメータ連動Transform制御コンポーネント
    /// 任意のUdonSharpコンポーネントのパラメータ値に応じてTransformを操作
    /// </summary>
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class TSFE_ParameterTransform : UdonSharpBehaviour
    {
        [Header("対象設定")]
        [Tooltip("パラメータを取得する対象コンポーネント")]
        public UdonSharpBehaviour targetComponent;

        [Tooltip("取得するパラメータ名（フィールド/プロパティ）")]
        public string parameterName;

        [Header("Transform設定")]
        [Tooltip("操作するTransform（空欄の場合は自身）")]
        public Transform targetTransform;

        [Header("入力範囲")]
        [Tooltip("パラメータの最小値")]
        public float inputMin = -1.0f;

        [Tooltip("パラメータの最大値")]
        public float inputMax = 1.0f;

        [Header("位置制御（ローカル座標）")]
        [Tooltip("位置を制御する")]
        public bool controlPosition = false;

        [Tooltip("最小値時の位置")]
        public Vector3 positionMin = Vector3.zero;

        [Tooltip("最大値時の位置")]
        public Vector3 positionMax = Vector3.zero;

        [Header("回転制御（ローカル回転）")]
        [Tooltip("回転を制御する")]
        public bool controlRotation = false;

        [Tooltip("最小値時の回転（Euler角）")]
        public Vector3 rotationMin = Vector3.zero;

        [Tooltip("最大値時の回転（Euler角）")]
        public Vector3 rotationMax = Vector3.zero;

        [Header("スケール制御")]
        [Tooltip("スケールを制御する")]
        public bool controlScale = false;

        [Tooltip("最小値時のスケール")]
        public Vector3 scaleMin = Vector3.one;

        [Tooltip("最大値時のスケール")]
        public Vector3 scaleMax = Vector3.one;

        [Header("オプション")]
        [Tooltip("配列の場合のインデックス（-1で無効）")]
        public int arrayIndex = -1;

        [Tooltip("更新間隔（秒）")]
        public float updateInterval = 0.0f;

        [Tooltip("補間を使用する（滑らかに動く）")]
        public bool useInterpolation = true;

        [Tooltip("補間速度（useInterpolation=trueの場合のみ）")]
        public float interpolationSpeed = 5.0f;

        private float lastUpdateTime = 0f;
        private Vector3 currentPosition;
        private Quaternion currentRotation;
        private Vector3 currentScale;
        private bool initialized = false;

        void Start()
        {
            if (targetTransform == null)
                targetTransform = transform;

            currentPosition = targetTransform.localPosition;
            currentRotation = targetTransform.localRotation;
            currentScale = targetTransform.localScale;
            initialized = true;
        }

        void Update()
        {
            if (!initialized)
                return;

            if (Time.time - lastUpdateTime < updateInterval)
                return;

            UpdateTransform();
            lastUpdateTime = Time.time;
        }

        private void UpdateTransform()
        {
            if (targetComponent == null || targetTransform == null)
                return;

            float value = GetParameterValue();
            float t = Mathf.InverseLerp(inputMin, inputMax, value);
            t = Mathf.Clamp01(t);

            if (useInterpolation)
            {
                if (controlPosition)
                {
                    Vector3 targetPos = Vector3.Lerp(positionMin, positionMax, t);
                    currentPosition = Vector3.Lerp(currentPosition, targetPos, Time.deltaTime * interpolationSpeed);
                    targetTransform.localPosition = currentPosition;
                }

                if (controlRotation)
                {
                    Quaternion targetRot = Quaternion.Euler(Vector3.Lerp(rotationMin, rotationMax, t));
                    currentRotation = Quaternion.Lerp(currentRotation, targetRot, Time.deltaTime * interpolationSpeed);
                    targetTransform.localRotation = currentRotation;
                }

                if (controlScale)
                {
                    Vector3 targetScale = Vector3.Lerp(scaleMin, scaleMax, t);
                    currentScale = Vector3.Lerp(currentScale, targetScale, Time.deltaTime * interpolationSpeed);
                    targetTransform.localScale = currentScale;
                }
            }
            else
            {
                if (controlPosition)
                {
                    targetTransform.localPosition = Vector3.Lerp(positionMin, positionMax, t);
                }

                if (controlRotation)
                {
                    targetTransform.localEulerAngles = Vector3.Lerp(rotationMin, rotationMax, t);
                }

                if (controlScale)
                {
                    targetTransform.localScale = Vector3.Lerp(scaleMin, scaleMax, t);
                }
            }
        }

        private float GetParameterValue()
        {
            if (targetComponent == null)
                return 0f;

            object value = targetComponent.GetProgramVariable(parameterName);

            // 配列の場合
            if (arrayIndex >= 0)
            {
                if (value == null)
                    return 0f;

                string typeName = value.GetType().Name;

                if (typeName == "Single[]")
                {
                    float[] arr = (float[])value;
                    if (arrayIndex < arr.Length)
                        return arr[arrayIndex];
                }
                else if (typeName == "Int32[]")
                {
                    int[] arr = (int[])value;
                    if (arrayIndex < arr.Length)
                        return (float)arr[arrayIndex];
                }

                return 0f;
            }

            // 単一値の場合
            if (value == null)
                return 0f;

            string valueTypeName = value.GetType().Name;

            if (valueTypeName == "Single")
                return (float)value;
            else if (valueTypeName == "Int32")
                return (float)(int)value;
            else if (valueTypeName == "Boolean")
                return (bool)value ? 1f : 0f;

            return 0f;
        }
    }
}
