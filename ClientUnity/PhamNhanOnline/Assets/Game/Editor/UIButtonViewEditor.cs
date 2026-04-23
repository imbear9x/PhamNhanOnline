using PhamNhanOnline.Client.UI.Common;
using UnityEditor;

namespace PhamNhanOnline.Client.Editor
{
    [CustomEditor(typeof(UIButtonView))]
    public sealed class UIButtonViewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "animHover",
                "animClick",
                "hoverScaleMultiplier",
                "pressedScaleMultiplier",
                "pressedOffset",
                "colorTweenDuration",
                "transformTweenDuration");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation", EditorStyles.boldLabel);

            var animHoverProperty = serializedObject.FindProperty("animHover");
            var animClickProperty = serializedObject.FindProperty("animClick");
            var hoverScaleMultiplierProperty = serializedObject.FindProperty("hoverScaleMultiplier");
            var pressedScaleMultiplierProperty = serializedObject.FindProperty("pressedScaleMultiplier");
            var pressedOffsetProperty = serializedObject.FindProperty("pressedOffset");
            var colorTweenDurationProperty = serializedObject.FindProperty("colorTweenDuration");
            var transformTweenDurationProperty = serializedObject.FindProperty("transformTweenDuration");

            EditorGUILayout.PropertyField(animHoverProperty);
            if (animHoverProperty.boolValue)
                EditorGUILayout.PropertyField(hoverScaleMultiplierProperty);

            EditorGUILayout.PropertyField(animClickProperty);
            if (animClickProperty.boolValue)
            {
                EditorGUILayout.PropertyField(pressedScaleMultiplierProperty);
                EditorGUILayout.PropertyField(pressedOffsetProperty);
            }

            EditorGUILayout.PropertyField(colorTweenDurationProperty);
            if (animHoverProperty.boolValue || animClickProperty.boolValue)
                EditorGUILayout.PropertyField(transformTweenDurationProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
