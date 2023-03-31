# Unity Simple Load/Save

This library provides simple load and save of game state for fields marked with a ```[GameState]``` attribute on any ```MonoBehaviour``` that implements the ```IGameState``` interface. The state is stored in a JSON format file called ```GameState.json``` in the ```Application.persistentDataPath``` directory. 

## Example ##

In this example we have three classes, each of which implement the ```IGameState``` interface (no methods needed).
The ```Player``` has an array of 10 ```Item``` slots in its ```inventory```. 
Those slots could be null. They can point to an ```Item``` or any class derived from ```Item``` such as ```StackableItem```.
Only **fields** that are marked with the ```[GameState]``` attribute will be serialized.

```
public class Player : MonoBehaviour, IGameState
{
    [GameState]
    public Item[] inventory = new Item[10];
}

public class Item : MonoBehaviour, IGameState
{
    public Sprite sprite; // Not serialized
}

public class StackableItem : Item
{
    [GameState]
    public int count;
}
```

Calling ```Save()``` on an instance of the ```GameStateManager``` class will save the current state of any ```GameObject``` that has any component that implements the ```IGameState``` interface. The current state of ```activeSelf``` and the ```transform``` states of ```position``` and ```eulerAngles``` will be serialized, together with and ```[GameState]``` tagged fields on those components of the ```GameObject```.

## Limitations ## 

- GameObjects to be serialized must have unique names.
- GameObjects can only have one instance of each MonoBehaviour type.
- GameObjects must exists at startup of the game and persist (not be deleted) for the duration of the game.
- The save only works for the current scene. Multiple scenes will need their own save files.
- Dynamically spawned objects will not be re-spawned when the game state is re-loaded.

## Installation ## 

Add the directory ```UnitySimpleLoadSave``` with the three script files to the ```Assets``` folder of your project. It will appear as a standalone automatically referenced library in Visual Studio (or your editor of choice).

Netwonsoft Json package [com.unity.nuget.newtonsoft-json](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@3.1/manual/index.html) is in the packages.json file already for this example but you may will need to add it to your own project to get it to work:

![image](https://user-images.githubusercontent.com/1547800/229220651-4baa6feb-5714-4a24-8bca-1a997caf347a.png)

## Save File Format ##

This is an example save file using the ```Player```, ```Item``` and ```StackableItem``` classes from the example above:

```
{
  "Player": {
    "activeSelf": true,
    "position": {
      "x": -2.31874037,
      "y": -0.774278641,
      "z": -1.66420293
    },
    "eulerAngles": {
      "x": 63.9910469,
      "y": 308.2669,
      "z": 284.2996
    },
    "behaviours": {
      "Player": {
        "inventory": [
          "Gun",
          null,
          null,
          null,
          "Ammo2",
          null,
          null,
          null,
          "Book",
          null
        ]
      }
    }
  },
  "Gun": {
    "activeSelf": true,
    "position": {
      "x": -3.466169,
      "y": -1.2760514,
      "z": -1.82558358
    },
    "eulerAngles": {
      "x": 3.41509462E-06,
      "y": 307.7737,
      "z": 269.999847
    },
    "behaviours": {
      "Item": {}
    }
  },
  "Ammo1": {
    "activeSelf": true,
    "position": {
      "x": 1.18000007,
      "y": -1.22605121,
      "z": -4.37000036
    },
    "eulerAngles": {
      "x": -6.02945534E-07,
      "y": 1.63636942E-05,
      "z": -1.35425307E-05
    },
    "behaviours": {
      "StackableItem": {
        "count": 0
      }
    }
  }
}
```

