using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]public class GameData
{
    public string name = "";
    public string title = "";
    public string description= "";
    public Sprite image;
    public bool IsCharacterModel = false;
    public bool IsFriendly = false;
    public GameObject ModelData;

    public GameData(string name1 , string title1, string description1, Sprite image1, bool isCharacterModel1, bool isFriendly1, GameObject modelData1)
    {
        name = name1;
        title = title1;
        description = description1;
        image = image1;
        IsCharacterModel = isCharacterModel1;
        IsFriendly = isFriendly1;
		ModelData = modelData1;
    }
}