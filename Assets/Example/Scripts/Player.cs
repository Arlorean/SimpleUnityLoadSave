using UnitySimpleLoadSave;
using UnityEngine;

public class Player : MonoBehaviour, IGameState
{
    [GameState]
    public Item[] inventory = new Item[10];
}
