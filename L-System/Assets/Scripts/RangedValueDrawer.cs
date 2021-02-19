using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

//Guide: https://catlikecoding.com/unity/tutorials/editor/custom-data/ 
[CustomPropertyDrawer(typeof(RangedFloat))]
public class RangedValueDrawer : PropertyDrawer
{
	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		label = EditorGUI.BeginProperty(position, label, property);
		Rect contentPosition = EditorGUI.PrefixLabel(position, label);
		contentPosition.width *= 0.5f;
		int indentLevel = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;
		EditorGUIUtility.labelWidth = 25f;
		EditorGUI.LabelField(contentPosition, "Min");
		contentPosition.x += EditorGUIUtility.labelWidth;
		contentPosition.width /= 2f;
		EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("minValue"), GUIContent.none);
		contentPosition.x += contentPosition.width + 5f;
		EditorGUI.LabelField(contentPosition, "Max");
		contentPosition.x += EditorGUIUtility.labelWidth;
		EditorGUI.PropertyField(contentPosition, property.FindPropertyRelative("maxValue"), GUIContent.none);
		EditorGUI.indentLevel = indentLevel;
		EditorGUI.EndProperty();
	}
}
