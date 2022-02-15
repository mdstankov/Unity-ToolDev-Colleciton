using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PlacementTool : EditorWindow
{	private struct SpawnData
	{
		public Vector2 pointInDisc;
		public float randAngleDeg;
		public GameObject prefab;

		public bool isValidHeight;

		public void SetRandomValues(List<GameObject> prefabs )
		{
			pointInDisc = Random.insideUnitSphere;
			randAngleDeg = Random.value * 360;
			prefab = prefabs.Count == 0 ? null : prefabs[Random.Range(0, prefabs.Count)];
		}
	}

	private class SpawnPoint
	{
		public SpawnData spawnData;
		public Vector3 position;
		public Quaternion rotation;
		public bool isValid = false;
		public Vector3 Up => rotation * Vector3.up;
		public SpawnPoint(Vector3 position, Quaternion rotation, SpawnData spawnData)
		{
			this.spawnData = spawnData;
			this.position = position;
			this.rotation = rotation;

			//check if mesh can be placed/fit the current location
			//using custom hight property inside a prefab script
			//if prefab missing we ignore the check and allow the placement
			//calculating volume boxes can lead to issues inside partilecs
			if (spawnData.prefab == null)
				return;

			SpawnablePrefab spawnablePrefab = spawnData.prefab.GetComponent<SpawnablePrefab>();

			if( spawnablePrefab )
			{
				float h = spawnablePrefab.height;
				Ray ray = new Ray( position, Up);
				isValid =  Physics.Raycast( ray , h ) == false;
			}
			else
			{
				isValid = true;
			}
		}
	}

	[MenuItem("Tools/PlacementTool")]
	public static void OpenTool() => GetWindow<PlacementTool>();

	public float _radius = 2f;
	public int _spawnCount = 8;
	
	//public enum PlacementOrigin { Camera, Mouse }
	//public PlacementOrigin _originMode = PlacementOrigin.Mouse;

	SerializedObject _so;
	SerializedProperty _propRadius;
	SerializedProperty _propSpawnCount;
	//SerializedProperty _propOriginMode;

	SpawnData[] _randPoints;
	List<GameObject> _prefabsToSpawn = new List<GameObject>();
	GameObject[] _assetPrefabs;
	[SerializeField] bool[] _prefabSelectionStates;

	Material _materialInvalid;


	private void OnEnable()
	{
		_so = new SerializedObject(this);
		_propRadius = _so.FindProperty("_radius");
		_propSpawnCount = _so.FindProperty("_spawnCount");
		//_propOriginMode = _so.FindProperty("_originMode");

		Shader sh = Shader.Find("Unlit/InvalidSpawnShader");
		_materialInvalid = new Material(sh);

		GenerateRandomPoints();
		SceneView.duringSceneGui += DuringSceneGUI;
		//Load prefabs;		
		string[] guids = AssetDatabase.FindAssets("t:prefab" , new[] { "Assets/PlacementTool/Prefabs" } );
		IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
		//_spawnPrefabs = paths.Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path)).ToArray();
		_assetPrefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
		if(_prefabSelectionStates == null || _prefabSelectionStates.Length != _assetPrefabs.Length )
		{
			_prefabSelectionStates = new bool[_assetPrefabs.Length];
		}		
	}

	private void OnDisable()
	{
		SceneView.duringSceneGui -= DuringSceneGUI;
		DestroyImmediate(_materialInvalid);
	}

	void GenerateRandomPoints( )
	{
		_randPoints = new SpawnData[_spawnCount];
		for( int i= 0; i < _spawnCount; i++)
		{			
			_randPoints[i].SetRandomValues(_prefabsToSpawn);
		}
	}

	//Editor window GUI function
	private void OnGUI()
	{
		_so.Update();
		EditorGUILayout.PropertyField(_propRadius);
		_propRadius.floatValue = _propRadius.floatValue.AtLeast(1);
		EditorGUILayout.PropertyField(_propSpawnCount);
		_propSpawnCount.intValue = _propSpawnCount.intValue.AtLeast(1);
		//EditorGUILayout.PropertyField(_propOriginMode);
		//EditorGUILayout.PropertyField(_propSpawnPrefab);

		GUILayout.BeginHorizontal();
		GUILayout.Label("Randomize" , GUILayout.Width(147));
		if (GUILayout.Button("Ctrl + R" )  )
		{
			GenerateRandomPoints();
		}
		GUILayout.EndHorizontal();
		GUILayout.Label("Place: Ctrl + Space");
		GUILayout.Label("Radius Shortcut: Ctrl + ScrollWheel");

		//Forces repaint if something is modified
		if (_so.ApplyModifiedProperties())
		{
			GenerateRandomPoints();
			SceneView.RepaintAll();
		}

		//Left click in editor window will deselect the field/remove focus from the ui
		if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
		{
			GUI.FocusControl(null);
			Repaint(); //repaints on the editorwindow ui(makes deselection instant)
		}
	}

	void TrySpawnObjects( List<SpawnPoint> spawnPoints)
	{
		if (_prefabsToSpawn.Count == 0)
			return;

		foreach (SpawnPoint spawnPoint in spawnPoints)
		{
			if (spawnPoint.isValid == false) 
				continue;

			GameObject spawnThing = (GameObject)PrefabUtility.InstantiatePrefab( spawnPoint.spawnData.prefab);
			Undo.RegisterCreatedObjectUndo(spawnThing, "Spawn Object");
			spawnThing.transform.position = spawnPoint.position;			
			spawnThing.transform.rotation = spawnPoint.rotation;

			//wrong way of ding it, this will lose the prefab reference tp the original prefab,will be clones
			//Undo also wont work
			//Instantiate(_spawnPrefab, hit.point, rot );
		}

		GenerateRandomPoints(); //Update points
	}

	//Scene GUI function
	void DuringSceneGUI(SceneView sceneView)
	{	
		//UI STUFF
		Handles.BeginGUI();

		Rect rect = new Rect(8, 8, 64, 64);
		for (int i = 0; i < _assetPrefabs.Length; i++)
		{
			GameObject prefab = _assetPrefabs[i];
			Texture icon = AssetPreview.GetAssetPreview(prefab);

			EditorGUI.BeginChangeCheck();
			_prefabSelectionStates[i] = GUI.Toggle(rect, _prefabSelectionStates[i], new GUIContent(icon));

			if (EditorGUI.EndChangeCheck())
			{
				//update selection list
				_prefabsToSpawn.Clear();
				for (int j = 0; j < _assetPrefabs.Length; j++)
				{
					if (_prefabSelectionStates[j] == true)
						_prefabsToSpawn.Add(_assetPrefabs[j]);
				}

				GenerateRandomPoints();
			}

			//Alternative buttom version for single selection
			//if( GUI.Button(rect, new GUIContent( prefab.name , icon) ) )
			//if (GUI.Button(rect, new GUIContent( icon)))
			//	_spawnPrefab = prefab;
			rect.y += rect.height + 2;
		}
		Handles.EndGUI();

		//3d stuff
		Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
		Transform camTF = sceneView.camera.transform;

		//Ray ray = _originMode == PlacementOrigin.Mouse ? HandleUtility.GUIPointToWorldRay(Event.current.mousePosition) : new Ray(camTF.position, camTF.forward);

		//Forces repaints in mouse mode.
		if (/*_originMode == PlacementOrigin.Mouse &&*/ Event.current.type == EventType.MouseMove)
		{			
			sceneView.Repaint( );
		}

		//Change radius
		bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;		
		if (holdingAlt & Event.current.type == EventType.ScrollWheel)
		{
			float scrollDir = Mathf.Sign(Event.current.delta.y); //change that happened during the event			
			_so.Update(); //since we update prop based on serialized object, so its also undo-able action
			_propRadius.floatValue *= 1f + scrollDir * 0.1f;
			_so.ApplyModifiedProperties();
			Repaint();//update editor window			
			Event.current.Use(); //consume event
		}

		if (TryRaycastFromCamera(camTF.up, out Matrix4x4 tangetToWorldMtx))
		{
			List<SpawnPoint> spawnPoints = GetSpawnPoses(tangetToWorldMtx);

			if (Event.current.type == EventType.Repaint)
			{
				DrawCircleRegion(tangetToWorldMtx);
				DrawSpawnPreviews(spawnPoints, sceneView.camera);
			}

			bool holdingCtrl = (Event.current.modifiers & EventModifiers.Control) != 0;


			if (holdingCtrl && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.R)
			{
				GenerateRandomPoints();
				sceneView.Repaint();
			}

			//spawn on click
			if (holdingCtrl && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
				TrySpawnObjects(spawnPoints);

		}
	}
	bool TryRaycastFromCamera(Vector2 cameraUp, out Matrix4x4 tangetToWorldMtx)
	{
		Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		if (Physics.Raycast(ray, out RaycastHit hit))
		{
			Vector3 hitNormal = hit.normal;
			Vector3 hitTangent = Vector3.Cross(hitNormal, cameraUp).normalized;
			Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);

			tangetToWorldMtx = Matrix4x4.TRS(hit.point, Quaternion.LookRotation(hitNormal, hitBitangent), Vector3.one);
			return true;
		}

		tangetToWorldMtx = default;
		return false;
	}

	List<SpawnPoint> GetSpawnPoses(Matrix4x4 tangentToWorldMtx)
	{
		List<SpawnPoint> spawnPoints= new List<SpawnPoint>();				
		foreach (SpawnData rndDataPoint in _randPoints)
		{
			//create ray for the point
			Ray ptRay = GetCircleRay(tangentToWorldMtx, rndDataPoint.pointInDisc);
			//find point on surface
			if ( Physics.Raycast( ptRay , out RaycastHit ptHit) )
			{
				//calc rotation and position, and assign to pose 
				Quaternion randRot = Quaternion.Euler(0, 0, rndDataPoint.randAngleDeg );
				Quaternion rot = Quaternion.LookRotation(ptHit.normal) * (randRot * Quaternion.Euler(90f, 0f, 0f));
				SpawnPoint point = new SpawnPoint(ptHit.point, rot , rndDataPoint);
				spawnPoints.Add(point);
			}
		}

		return spawnPoints;
	}

	Ray GetCircleRay(Matrix4x4 tangentToWorldMtx, Vector2 pointInCircle)
	{
		Vector3 normal = tangentToWorldMtx.MultiplyVector(Vector3.forward);
		Vector3 rayOrigin = tangentToWorldMtx.MultiplyPoint3x4(pointInCircle * _radius);
		rayOrigin += normal * 2;
		Vector3 rayDirection = -normal;
		return new Ray(rayOrigin, rayDirection);
	}

	void RandomizeSpawnPointMeshes( )
	{
		//foreach (var item in collection)
		//{

		//}
	}

	void DrawSpawnPreviews(List<SpawnPoint> spawnPoints, Camera cam)
	{
		foreach (SpawnPoint point in spawnPoints)
		{
			if ( point.spawnData.prefab != null )
			{
				//draw preview of all meshes
				Matrix4x4 poseToWolrldMtx = Matrix4x4.TRS(point.position, point.rotation, Vector3.one);
				DrawPrefab( point.spawnData.prefab, poseToWolrldMtx, cam , point.isValid );
			}
			else
			{
				//Prefab missing, draw sphere and normal on the suface instead
				Handles.SphereHandleCap(-1, point.position, Quaternion.identity, 0.1f, EventType.Repaint);
				Handles.DrawAAPolyLine(point.position, point.position + point.Up);
			}
		}
	}

	void DrawPrefab(GameObject prefab, Matrix4x4 poseToWorldMtx, Camera cam, bool valid)
	{
		MeshFilter[] filters = prefab.GetComponentsInChildren<MeshFilter>();
		foreach (MeshFilter filter in filters)
		{
			Matrix4x4 localMtx = filter.transform.localToWorldMatrix;
			Matrix4x4 childToWorldMtx = poseToWorldMtx * localMtx;
			Mesh mesh = filter.sharedMesh;

			Material mat = valid? filter.GetComponent<MeshRenderer>().sharedMaterial : _materialInvalid ;
			//to render instantly ignoring scene
			//mat.SetPass(0); // global setting Grpagics.X commands
			//Graphics.DrawMeshNow(mesh, childToWorldMtx);

			Graphics.DrawMesh(mesh, childToWorldMtx, mat, 0 , cam );
		}
	}	

	void DrawCircleRegion(Matrix4x4 localToWorldMtx)
	{
		DrawAxes(localToWorldMtx);
		// draw circle adapted to the terrain
		const int circleDetail = 128;
		Vector3[] ringPoints = new Vector3[circleDetail];
		for (int i = 0; i < circleDetail; i++)
		{
			float t = i / ((float)circleDetail - 1); // go back to 0/1 position
			const float TAU = 6.28318530718f;
			float angRad = t * TAU;
			Vector2 dir = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
			Ray r = GetCircleRay(localToWorldMtx, dir);
			if (Physics.Raycast(r, out RaycastHit cHit))
			{
				ringPoints[i] = cHit.point + cHit.normal * 0.02f;
			}
			else
			{
				ringPoints[i] = r.origin;
			}
		}

		Handles.DrawAAPolyLine(ringPoints);
	}

	void DrawAxes(Matrix4x4 localToWorldMtx)
	{
		Vector3 pt = localToWorldMtx.MultiplyPoint3x4(Vector3.zero);
		Handles.color = Color.red;
		Handles.DrawAAPolyLine(6, pt, pt + localToWorldMtx.MultiplyVector(Vector3.right));
		Handles.color = Color.green;
		Handles.DrawAAPolyLine(6, pt, pt + localToWorldMtx.MultiplyVector(Vector3.up));
		Handles.color = Color.blue;
		Handles.DrawAAPolyLine(6, pt, pt + localToWorldMtx.MultiplyVector(Vector3.forward));
	}

	void DrawSpehere( Vector3 pos )
	{
		Handles.SphereHandleCap( -1 , pos , Quaternion.identity , 0.1f , EventType.Repaint );
	}

}
