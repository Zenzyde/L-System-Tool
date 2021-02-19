using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(L_SystemGenerator))]
public class L_SystemEditor : Editor
{
	private L_SystemGenerator l_System;
	private bool destroyedSystem = false;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();
		if (GUILayout.Button("Generate New L-System"))
		{
			if (l_System == null && !GameObject.Find("L_SystemGenerator"))
			{
				l_System = (L_SystemGenerator)target;
			}
			l_System.CreateLSystem();
			destroyedSystem = false;
		}
		else if (l_System != null && !destroyedSystem && GUILayout.Button("Destroy Current L-System"))
		{
			l_System.DestroySystem();
			destroyedSystem = true;
		}
		else if (l_System != null && !destroyedSystem && GUILayout.Button("Save L-System"))
		{
			l_System.SaveSystem();
		}
	}
}
