using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using TSFE.DFUNC;

namespace TSFE.Editor
{
    [CustomEditor(typeof(DFUNC_AdvancedFlaps))]
    public class DFUNC_AdvancedFlapsEditor : UnityEditor.Editor
    {
        private bool showDebugInfo = true;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            DrawDefaultInspector();

            if (!Application.isPlaying) return;

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("PlayMode Debug Info", EditorStyles.boldLabel);

            showDebugInfo = EditorGUILayout.Foldout(showDebugInfo, "Runtime Status", true);

            if (showDebugInfo)
            {
                var flaps = (DFUNC_AdvancedFlaps)target;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Current State
                EditorGUILayout.LabelField("Current State", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Current Angle");
                EditorGUILayout.LabelField($"{flaps.angle:F2}°", EditorStyles.textField);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Target Angle");
                EditorGUILayout.LabelField($"{flaps.targetAngle:F2}°", EditorStyles.textField);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Detents
                EditorGUILayout.LabelField("Detent Information", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Current Detent");
                EditorGUILayout.LabelField($"#{flaps.detentIndex} ({flaps.detentAngle:F1}°)", EditorStyles.textField);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Target Detent");
                EditorGUILayout.LabelField($"#{flaps.targetDetentIndex} ({flaps.targetDetentAngle:F1}°)", EditorStyles.textField);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Speed Limits
                EditorGUILayout.LabelField("Speed Limits", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Current Limit");
                EditorGUILayout.LabelField($"{flaps.speedLimit:F0} KIAS", EditorStyles.textField);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel("Target Limit");
                EditorGUILayout.LabelField($"{flaps.targetSpeedLimit:F0} KIAS", EditorStyles.textField);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Progress Bar
                EditorGUILayout.LabelField("Extension Progress", EditorStyles.boldLabel);
                var normalizedAngle = flaps.maxAngle > 0 ? flaps.angle / flaps.maxAngle : 0;
                var rect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(rect, normalizedAngle, $"{normalizedAngle * 100:F1}%");

                EditorGUILayout.Space(5);

                // Detent Visual
                EditorGUILayout.LabelField("Detent Positions", EditorStyles.boldLabel);
                DrawDetentVisual(flaps);

                EditorGUILayout.EndVertical();
            }

            // Force repaint while playing
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void DrawDetentVisual(DFUNC_AdvancedFlaps flaps)
        {
            if (flaps.detents == null || flaps.detents.Length == 0) return;

            var rect = EditorGUILayout.GetControlRect(false, 40);
            var maxAngle = flaps.maxAngle;

            if (maxAngle <= 0) return;

            // Background
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 15, rect.width, 10), new Color(0.2f, 0.2f, 0.2f));

            // Draw detent markers
            for (int i = 0; i < flaps.detents.Length; i++)
            {
                var detentAngle = flaps.detents[i];
                var normalizedPos = detentAngle / maxAngle;
                var x = rect.x + rect.width * normalizedPos;

                // Detent marker
                var markerColor = i == flaps.targetDetentIndex ? Color.yellow : Color.gray;
                EditorGUI.DrawRect(new Rect(x - 1, rect.y + 12, 2, 16), markerColor);

                // Detent label
                var labelStyle = new GUIStyle(EditorStyles.miniLabel);
                labelStyle.alignment = TextAnchor.UpperCenter;
                labelStyle.normal.textColor = markerColor;
                GUI.Label(new Rect(x - 15, rect.y, 30, 12), $"{i}", labelStyle);
            }

            // Current angle indicator
            var currentNormalized = flaps.angle / maxAngle;
            var currentX = rect.x + rect.width * currentNormalized;
            EditorGUI.DrawRect(new Rect(currentX - 2, rect.y + 10, 4, 20), Color.green);

            // Target angle indicator (if different from current)
            if (!Mathf.Approximately(flaps.angle, flaps.targetAngle))
            {
                var targetNormalized = flaps.targetAngle / maxAngle;
                var targetX = rect.x + rect.width * targetNormalized;
                EditorGUI.DrawRect(new Rect(targetX - 1, rect.y + 13, 2, 14), new Color(1f, 0.5f, 0f, 0.7f));
            }

            // Legend
            EditorGUILayout.BeginHorizontal();
            DrawColorBox(Color.green);
            EditorGUILayout.LabelField("Current", GUILayout.Width(60));
            DrawColorBox(new Color(1f, 0.5f, 0f, 0.7f));
            EditorGUILayout.LabelField("Target", GUILayout.Width(60));
            DrawColorBox(Color.yellow);
            EditorGUILayout.LabelField("Target Detent", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawColorBox(Color color)
        {
            var rect = EditorGUILayout.GetControlRect(false, 12, GUILayout.Width(12));
            EditorGUI.DrawRect(rect, color);
        }
    }
}
