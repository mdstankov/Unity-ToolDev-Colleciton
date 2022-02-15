using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SnapperTool : EditorWindow
{
	public enum GridType { Cortesian, Polar }


	[MenuItem("Tools/Snapper")]
	public static void OpenTheThing() => GetWindow<SnapperTool>("Snapper Tool");

	public float gridSize = 1f;
	public GridType gridType = GridType.Cortesian;
	public int angularDivision = 24; //unity default rotation snapping


	SerializedObject so;
	SerializedProperty propGridSize;
	SerializedProperty propGridType;
	SerializedProperty propAngularDivisions;

	const float TAU = 6.28318530718f;

	private void OnEnable()
	{
		//to be able to use undo
		so = new SerializedObject(this);
		propGridSize = so.FindProperty("gridSize");
		propGridType = so.FindProperty("gridType");
		propAngularDivisions = so.FindProperty("angularDivision");

		//load saved config
		gridSize = EditorPrefs.GetFloat("SNAPPER_TOOL_gridSize", 1);
		gridType = (GridType)EditorPrefs.GetInt("SNAPPER_TOOL_gridType", 0);
		angularDivision = EditorPrefs.GetInt("SNAPPER_TOOL_angularDivision", 24);

		//call back to repaint the UI when selection is changed
		Selection.selectionChanged += Repaint;
		SceneView.duringSceneGui += DuringSceneGUI;
	}
	private void OnDisable()
	{
		//save config
		EditorPrefs.SetFloat("SNAPPER_TOOL_gridSize", gridSize);
		EditorPrefs.SetInt("SNAPPER_TOOL_gridType", (int)gridType);
		EditorPrefs.SetInt("SNAPPER_TOOL_angularDivision", angularDivision);

		Selection.selectionChanged -= Repaint;
		SceneView.duringSceneGui -= DuringSceneGUI;
	}

	void DuringSceneGUI(SceneView sceneView)
	{
		// EventType.Repaint - The UI loop that is rawing  
		if (Event.current.type == EventType.Repaint)
		{
			float gridDrawExtent = 16;

			if (gridType == GridType.Cortesian)
				DrawGridCartesian(gridDrawExtent);
			else
				DrawGridPolar(gridDrawExtent);
		}
	}

	void DrawGridCartesian(float gridDrawExtent)
	{
		int lineCount = Mathf.RoundToInt((gridDrawExtent * 2) / gridSize);

		// makeing odd number of lines to have one drawned always in the center of the grid 
		//and symetry on both sides
		if (lineCount % 2 == 0)
			lineCount++;

		int halfLineCount = lineCount / 2;

		for (int i = 0; i < lineCount; i++)
		{
			//draw behind objects
			Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
			float intOffset = i - halfLineCount;
			float xCoord = intOffset * gridSize;
			float zCoord0 = halfLineCount * gridSize;
			float zCoord1 = -halfLineCount * gridSize;
			Vector3 p0 = new Vector3(xCoord, 0f, zCoord0);
			Vector3 p1 = new Vector3(xCoord, 0f, zCoord1);
			Handles.DrawAAPolyLine(p0, p1);
			p0 = new Vector3(zCoord0, 0f, xCoord);
			p1 = new Vector3(zCoord1, 0f, xCoord);
			Handles.DrawAAPolyLine(p0, p1);
		}
	}

	void DrawGridPolar(float gridDrawExtent)
	{
		int ringCount = Mathf.RoundToInt(gridDrawExtent / gridSize);
		float radiusOuter = (ringCount - 1) * gridSize;

		//radial grid(rings)
		for (int i = 1; i < ringCount; i++)
		{
			Handles.DrawWireDisc(Vector3.zero, Vector3.up, i * gridSize);
		}
		//angular grid(lines)
		for (int i = 0; i < ringCount; i++)
		{
			float t = i / (float)angularDivision; // 0 to last step to 1
			float angRad = t * TAU; // percentage to radius
			Vector3 dir = new Vector3(Mathf.Cos(angRad), 0, Mathf.Sin(angRad));
			Handles.DrawAAPolyLine(Vector3.zero, dir * radiusOuter);
		}
	}

	private void OnGUI()
	{
		so.Update(); //mandatory
		EditorGUILayout.PropertyField(propGridType);
		EditorGUILayout.PropertyField(propGridSize);

		if (gridType == GridType.Polar)
		{
			EditorGUILayout.PropertyField(propAngularDivisions);
			propAngularDivisions.intValue = Mathf.Max(4, propAngularDivisions.intValue);
		}
		so.ApplyModifiedProperties(); //mandatory

		using (new EditorGUI.DisabledScope(Selection.gameObjects.Length == 0))
		{
			if (GUILayout.Button("Snap Selection"))
				SnapSelection();

			if (GUILayout.Button("Snap Selection To Ground"))
				SnapSelectionToGroundGrid();
		}

	}

	void SnapSelection()
	{
		foreach (GameObject go in Selection.gameObjects)
		{
			Undo.RecordObject(go.transform, "snap object");
			go.transform.position = GetSnappedPosition(go.transform.position);
		}
	}

	void SnapSelectionToGroundGrid()
	{
		foreach (GameObject go in Selection.gameObjects)
		{
			Undo.RecordObject(go.transform, "snap object");

			Vector3 snappped;
			snappped.x = Mathf.Round(go.transform.position.x / gridSize) * gridSize;
			snappped.y = 0;
			snappped.z = Mathf.Round(go.transform.position.z / gridSize) * gridSize;
			go.transform.position = snappped;
			//go.transform.position = go.transform.position.Round(gridSize);
		}
	}

	Vector3 GetSnappedPosition(Vector3 posOriginal)
	{
		if (gridType == GridType.Cortesian)
			return posOriginal.Round(gridSize);

		if (gridType == GridType.Polar)
		{
			Vector2 vec = new Vector2(posOriginal.x, posOriginal.z);
			float dist = vec.magnitude;
			float distSnapped = dist.Round(gridSize);

			float angRad = Mathf.Atan2(vec.y, vec.x); // 0 to Tau
			float angTurn = angRad / TAU; // 0 to 1 

			float angTurnsSnapped = angTurn.Round(1f / angularDivision); // Mathf.RoundToInt(angTurn * angularDivision ) / angularDivision ;
			float angRadStapped = angTurnsSnapped * TAU;

			Vector2 dirSnapped = new Vector2(Mathf.Cos(angRadStapped), Mathf.Sin(angRadStapped));
			Vector2 snappedVec = dirSnapped * distSnapped;

			return new Vector3(snappedVec.x, posOriginal.y, snappedVec.y);
		}

		return posOriginal.Round(gridSize);
	}

}
