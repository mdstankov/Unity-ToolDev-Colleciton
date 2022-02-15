using UnityEngine;
using UnityEditor;

public class ColorizerWindow : EditorWindow
{
	Color color;

	[MenuItem("Tools/Colorizer")]
	public static void ShowWindow( )
	{
		EditorWindow.GetWindow<ColorizerWindow>( "Colorizer" );
	}

	private void OnGUI()
	{
		//Window Code
		GUILayout.Label( "Color the selected objects" , EditorStyles.boldLabel );

		color = EditorGUILayout.ColorField( "Color" , color);

		if( GUILayout.Button( "Colorize" ) )
		{
			Colorize();
		}
	}

	void Colorize( )
	{
		//Get currently selected objects
		// .gameObjects array of currently selected objects
		foreach( GameObject obj in Selection.gameObjects )
		{
			Renderer renderer = obj.GetComponent<Renderer>( );
			if( renderer != null )
			{
				renderer.sharedMaterial.color = color;
			}
		}
	}
}
