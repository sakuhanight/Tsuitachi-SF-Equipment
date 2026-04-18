using UdonSharpEditor;
using UnityEditor;
using UnityEngine;

namespace TSFE.Editor
{
    [CustomEditor(typeof(TSFE.SFEXT.SFEXT_AuxiliaryPowerUnitTest))]
    public class SFEXT_AuxiliaryPowerUnitTestEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target))
                return;

            var test = (TSFE.SFEXT.SFEXT_AuxiliaryPowerUnitTest)target;
            var apu = test.apu;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("SFEXT_AuxiliaryPowerUnit Test Controls", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Play Mode中にこのInspectorでAPUを操作できます", MessageType.Info);

            EditorGUILayout.Space();

            // Play Mode 中のみ操作可能
            GUI.enabled = Application.isPlaying && apu != null;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("APU Controls", EditorStyles.boldLabel);

            if (GUILayout.Button("Toggle APU", GUILayout.Height(30)))
            {
                if (apu != null)
                {
                    apu.ToggleAPU();
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Start APU", GUILayout.Height(25)))
            {
                if (apu != null)
                {
                    apu.StartAPU();
                }
            }
            if (GUILayout.Button("Stop APU", GUILayout.Height(25)))
            {
                if (apu != null)
                {
                    apu.StopAPU();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            GUI.enabled = true;

            EditorGUILayout.Space();

            // APU状態表示
            if (Application.isPlaying && apu != null)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("APU State (Real-time)", EditorStyles.boldLabel);

                // APU State表示
                GUIStyle stateStyle = new GUIStyle(EditorStyles.boldLabel);
                stateStyle.fontSize = 14;

                TSFEEditorUtil.GetAPUStateDisplay(apu.State, out Color stateColor, out string stateText);
                using (new TSFEEditorUtil.ColorScope(stateColor))
                {
                    EditorGUILayout.LabelField("State", stateText, stateStyle);
                }

                EditorGUILayout.EndVertical();

                // PlayMode中は自動再描画
                Repaint();
            }
            else if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Play Mode に入るとAPU状態が表示されます", MessageType.Info);
            }

            EditorGUILayout.Space();

            // デフォルトInspector
            DrawDefaultInspector();
        }
    }
}
