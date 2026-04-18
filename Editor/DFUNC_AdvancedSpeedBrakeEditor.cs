using UnityEditor;
using UnityEngine;
using UdonSharpEditor;
using TSFE.DFUNC;

namespace TSFE.Editor
{
    [CustomEditor(typeof(DFUNC_AdvancedSpeedBrake))]
    public class DFUNC_AdvancedSpeedBrakeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // デフォルトのInspector表示
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;
            base.OnInspectorGUI();

            // PlayMode時のみデバッグ情報を表示
            if (!Application.isPlaying) return;

            var speedBrake = (DFUNC_AdvancedSpeedBrake)target;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("PlayMode Debug Info", EditorStyles.boldLabel);

            // 現在のパラメータ確認
            EditorGUILayout.LabelField("Current Parameters", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("Drag Multiplier", speedBrake.dragMultiplier);
            EditorGUILayout.FloatField("Lift Multiplier", speedBrake.liftMultiplier);
            EditorGUILayout.FloatField("Response", speedBrake.response);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space(5);

            EditorGUI.BeginDisabledGroup(true);

            // 目標展開率
            float targetAngle = speedBrake.TargetAngle;
            EditorGUILayout.FloatField("Target Angle", targetAngle);
            EditorGUILayout.Slider("Target (%)", targetAngle * 100f, 0f, 100f);

            EditorGUILayout.Space(5);

            // 実際の展開率
            float angle = speedBrake.Angle;
            EditorGUILayout.FloatField("Actual Angle", angle);
            EditorGUILayout.Slider("Actual (%)", angle * 100f, 0f, 100f);

            EditorGUILayout.Space(5);

            EditorGUILayout.Space(5);

            // SAVControl状態
            var savControl = speedBrake.SAVControl;
            if (savControl != null)
            {
                EditorGUILayout.LabelField("SAVControl Info", EditorStyles.boldLabel);
                EditorGUILayout.ObjectField("SAVControl", savControl, typeof(UdonSharp.UdonSharpBehaviour), true);
                EditorGUILayout.TextField("GameObject Name", savControl.gameObject.name);
                EditorGUILayout.TextField("Type", savControl.GetType().Name);

                var extraDrag = savControl.GetProgramVariable("ExtraDrag");
                var extraLift = savControl.GetProgramVariable("ExtraLift");
                var airSpeed = savControl.GetProgramVariable("AirSpeed");
                var vehicleRb = savControl.GetProgramVariable("VehicleRigidbody");

                // 重要なSAV変数を追加表示
                var angVelPitch = savControl.GetProgramVariable("AngVelPitch");
                var liftPush = savControl.GetProgramVariable("LiftPush");
                var vehicleMass = savControl.GetProgramVariable("VehicleMass");

                EditorGUILayout.FloatField("ExtraDrag", extraDrag != null ? (float)extraDrag : 0f);
                EditorGUILayout.FloatField("ExtraLift", extraLift != null ? (float)extraLift : 0f);
                EditorGUILayout.FloatField("AirSpeed (m/s)", airSpeed != null ? (float)airSpeed : 0f);

                if (liftPush != null)
                    EditorGUILayout.FloatField("LiftPush", (float)liftPush);
                if (vehicleMass != null)
                    EditorGUILayout.FloatField("VehicleMass", (float)vehicleMass);

                if (vehicleRb != null)
                {
                    var rb = (UnityEngine.Rigidbody)vehicleRb;
                    EditorGUILayout.Vector3Field("Rigidbody Velocity", rb.velocity);
                    EditorGUILayout.FloatField("Rigidbody Speed", rb.velocity.magnitude);
                }
                else
                {
                    EditorGUILayout.HelpBox("VehicleRigidbody is null", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("SAVControl is null", MessageType.Warning);
            }

            EditorGUILayout.Space(5);

            // サマリー
            EditorGUILayout.HelpBox(
                $"Target: {targetAngle:F3} ({targetAngle * 100f:F1}%)\n" +
                $"Actual: {angle:F3} ({angle * 100f:F1}%)\n" +
                $"Difference: {Mathf.Abs(targetAngle - angle):F3}\n" +
                $"Expected Drag: +{angle * speedBrake.dragMultiplier:F3}\n" +
                $"Expected Lift: +{angle * speedBrake.liftMultiplier:F3}",
                MessageType.Info
            );

            EditorGUI.EndDisabledGroup();

            // 自動更新のためRepaint
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
