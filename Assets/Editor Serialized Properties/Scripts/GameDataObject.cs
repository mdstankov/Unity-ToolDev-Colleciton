using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GameDataObject", menuName = "ScriptableObjects/GameDataObject", order = 1)]
[System.Serializable]public class GameDataObject : ScriptableObject
{
    public GameData[] gameData;
}