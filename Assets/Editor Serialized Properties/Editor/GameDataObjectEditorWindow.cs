using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GameDataObjectEditorWindow : ExtendedEditorWindow
{
   public static void Open( GameDataObject dataObject )
	{
		GameDataObjectEditorWindow window = GetWindow<GameDataObjectEditorWindow> ("Game Data Editor");
		window.serializedObject = new SerializedObject(dataObject);  
	}

	private void OnGUI()
	{
		if (serializedObject == null)
        {
            Debug.Log("Null");
            return;
        }
		currentProperty = serializedObject.FindProperty( "gameData" );
		//DrawProperties( currentProperty, true );
		EditorGUILayout.BeginHorizontal( );
		
		EditorGUILayout.BeginVertical( "box" , GUILayout.MaxWidth( 150 ), GUILayout.ExpandHeight( true ) );
		
		DrawSidebar( currentProperty );
		
		EditorGUILayout.EndVertical( );

		EditorGUILayout.BeginVertical( "box" , GUILayout.ExpandHeight( true ) );
		
		if( selectedProperty != null )
		{
			//draws all like in inspector
			//DrawProperties( selectedProperty , true );

			//draws custom one
			DrawSelectedPropertiesPanel( );
		}
		else
		{
			EditorGUILayout.LabelField( "Select an item from the list" );
		}
		EditorGUILayout.EndVertical( );
		EditorGUILayout.EndHorizontal( );
		Apply( );
	}

	//for custom ones 
	void DrawSelectedPropertiesPanel( )
	{
		currentProperty = selectedProperty;
		EditorGUILayout.BeginHorizontal( "box" );

		DrawField( "name" , true );
		DrawField( "title" , true );
		
		bool isCharacter = false;

		EditorGUILayout.EndHorizontal( );

		EditorGUILayout.BeginHorizontal( "box" );

		if( GUILayout.Button( "Character" , EditorStyles.toolbarButton ) )
		{
			isCharacter = true;
		}

		if( GUILayout.Button( "Empty" , EditorStyles.toolbarButton ) )
		{
		
		}

		EditorGUILayout.EndHorizontal( );

		if( isCharacter )
		{
			EditorGUILayout.BeginHorizontal( "box" );

			DrawField( "description" , true );
			DrawField( "image" , true );
			DrawField( "IsCharacterModel" , true );
			DrawField( "IsFriendly" , true );
			DrawField( "ModelData" , true );

			EditorGUILayout.EndHorizontal( );
		}

	}
}
