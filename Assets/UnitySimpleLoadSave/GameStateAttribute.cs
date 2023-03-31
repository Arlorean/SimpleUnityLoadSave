using System;

namespace UnitySimpleLoadSave {

    /// <summary>
    /// Add this attribute to any public fields of MonoBehaviours that implement the IGameState interface
    /// to include them in the save game state.
    /// 
    /// e.g. 
    /// public class Player {
    ///     [GameState]
    ///     public int health; // This is serialized
    ///     
    ///     [GameState]
    ///     public Item[] inventory; // This is serialized
    ///     
    ///     int avatarSpriteIndex; // This is not serialized (missing [GameState] attribute)
    /// }
    /// </summary>
    public class GameStateAttribute : Attribute {
    }
}
