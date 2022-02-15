using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CubeRandomColor))]
public class CubeEditor : Editor
{
	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		// target - object currently being inspected
		CubeRandomColor cube = target as CubeRandomColor;

		GUILayout.BeginHorizontal( );		
		if( GUILayout.Button("Generate Color"))
		{
			cube.GenerateColor( );
		}

		if( GUILayout.Button("Reset Color"))
		{
			cube.ResetColor( );	
		}
		GUILayout.EndHorizontal( );
	}
}
