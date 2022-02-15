using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SphereScaling))]
public class SphereEditor : Editor
{
	public override void OnInspectorGUI()
	{
		//base.OnInspectorGUI( );
		SphereScaling sphere = (SphereScaling)target;
		
		GUILayout.Label ("Base size:");
		sphere.baseSize = EditorGUILayout.Slider( "Size" , sphere.baseSize , .1f , 2f );
		
		sphere.transform.localScale = Vector3.one * sphere.baseSize;



	}
}
