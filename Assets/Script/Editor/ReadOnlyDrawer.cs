using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // Save the current GUI enabled state
        bool previousGUIState = GUI.enabled;
        
        // Disable the property field (make it read-only)
        GUI.enabled = false;
        
        // Draw the property field as disabled
        EditorGUI.PropertyField(position, property, label);
        
        // Restore the previous GUI state
        GUI.enabled = previousGUIState;
    }
}