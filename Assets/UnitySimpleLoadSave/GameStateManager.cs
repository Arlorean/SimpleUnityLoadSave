using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Component to manage loading and saving the game state for the all GameObjects that have MonoBehaviours that implement the IGameState interface.
/// The GameObject.activeSelf and Transform.position and Transform.eulerAngles are also serialized with the MonoBehaviour's
/// fields that are marked with the [GameState] attribute.
/// </summary>
namespace UnitySimpleLoadSave {

    public class GameStateManager : MonoBehaviour {
        public static string FileName => "GameState.json";

#if UNITY_WEBGL
        public static string FilePath => Path.Combine(Path.GetDirectoryName(Application.persistentDataPath), Application.productName, FileName);
#else
    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);
#endif

        [ContextMenu(nameof(Load))]
        public void Load() {
            try {
                var context = new Context() { gameObjectMap = AllGameStates.ToDictionary(s => s.Key.name, s => s.Key) };
                var gameState = JObject.Parse(File.ReadAllText(FilePath));
                ReadGameState(gameState, context);
            }
            catch (Exception e) {
                Debug.LogWarning($"Error reading game save: {e}");
            }
        }

        [ContextMenu(nameof(Save))]
        public void Save() {
            try {
#if UNITY_WEBGL
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
#endif
                File.WriteAllText(FilePath, Write().ToString());
            }
            catch (Exception e) {
                Debug.LogWarning($"Error writing game save: {e}");
            }
        }

        [ContextMenu(nameof(Delete))]
        public void Delete() {
            try {
                File.Delete(FilePath);
            }
            catch (Exception e) {
                Debug.LogWarning($"Error deleting game save: {e}");
            }
        }


        JObject Write() => new JObject(
            from state in AllGameStates
            select new JProperty(state.Key.name, Write(state.Key, state))
        );

        JObject Write(GameObject gameObject, IEnumerable<MonoBehaviour> behaviours) => new JObject() {
        { "activeSelf", Write(gameObject.activeSelf) },
        { "position", Write(gameObject.transform.position) },
        { "eulerAngles", Write(gameObject.transform.eulerAngles) },
        { "behaviours", new JObject(from b in behaviours select new JProperty(b.GetType().Name, Write(b))) },
    };

        JObject Write(MonoBehaviour behaviour) => new JObject(
            from field in behaviour.GetType().GetFields()
            where field.GetCustomAttribute<GameStateAttribute>() != null
            select new JProperty(field.Name, Write(field.GetValue(behaviour)))
        );

        JToken Write(object value) {
            if (value is Array array) {
                return new JArray(from object v in array select Write(v));
            }
            if (value is UnityEngine.Object unityObject) {
                return new JValue(unityObject.name);
            }
            return new JValue(value);
        }

        JObject Write(Vector3 v) => new JObject {
        { "x", v.x },
        { "y", v.y },
        { "z", v.z },
    };

        class Context {
            public Dictionary<string, GameObject> gameObjectMap;
        }

        void ReadGameState(JObject obj, Context context) {
            foreach (var prop in obj) {
                var name = prop.Key;
                if (context.gameObjectMap.TryGetValue(prop.Key, out var gameObject)) {
                    if (prop.Value is JObject state) {
                        ReadGameObject(state, gameObject, context);
                    }
                    else {
                        Debug.LogWarning($"Game object '{prop.Key}' is not valid in the save game file.");
                    }
                }
                else {
                    Debug.LogWarning($"Unknown game object '{prop.Key}' found in save game file but not in scene.");
                }
            }
        }

        void ReadGameObject(JObject obj, GameObject gameObject, Context context) {
            if (obj.TryGetValue("activeSelf", out var activeSelf)) {
                gameObject.SetActive((bool)activeSelf);
            }
            if (obj.TryGetValue("position", out var position)) {
                gameObject.transform.position = ReadVector3(position);
            }
            if (obj.TryGetValue("eulerAngles", out var eulerAngles)) {
                gameObject.transform.eulerAngles = ReadVector3(eulerAngles);
            }
            if (obj.TryGetValue("behaviours", out var behaviours)) {
                foreach (JProperty prop in behaviours) {
                    if (prop.Value is JObject propObj) {
                        if (gameObject.GetComponent(prop.Name) is MonoBehaviour behaviour) {
                            ReadBehaviour(propObj, behaviour, context);
                        }
                        else {
                            Debug.LogWarning($"Cannot find component of type '{prop.Name}' on game object '{gameObject}'");
                        }
                    }
                    else {
                        Debug.LogWarning($"Component of type '{prop.Name}' on game object '{gameObject}' is not valid in the save file.");
                    }
                }
            }
        }

        void ReadBehaviour(JObject obj, MonoBehaviour behaviour, Context context) {
            var fields = behaviour.GetType().GetFields()
                .Where(f => f.GetCustomAttribute<GameStateAttribute>() != null)
                .ToDictionary(f => f.Name);

            foreach (var prop in obj) {
                if (fields.TryGetValue(prop.Key, out var field)) {
                    field.SetValue(behaviour, ReadValue(prop.Value, field.FieldType, context));
                }
                else {
                    Debug.LogWarning($"Value '{prop.Value}' found for field '{prop.Key}' that doesn't exists on the '{behaviour.GetType().Name}' MonoBehaviour type on game object '{behaviour.name}'");
                }
            }
        }

        object ReadValue(JToken token, Type type, Context context) {
            if (token.Type == JTokenType.Null) {
                return null;
            }

            switch (Type.GetTypeCode(type)) {
                case TypeCode.Boolean: return ((Boolean)token);
                case TypeCode.Int32: return ((Int32)token);
                case TypeCode.Single: return ((Single)token);
                case TypeCode.Double: return ((Double)token);
            }

            if (typeof(Vector3).IsAssignableFrom(type)) {
                return ReadVector3(token);
            }

            if (token is JArray jarray) {
                if (type.IsArray) {
                    var elementType = type.GetElementType();
                    var array = Array.CreateInstance(elementType, jarray.Count);
                    for (var i = 0; i < array.Length; ++i) {
                        array.SetValue(ReadValue(jarray[i], elementType, context), i);
                    }
                    return array;
                }
                else {
                    Debug.LogWarning($"Trying to read an array into a '{type}' field.");
                    return null;
                }
            }

            if (typeof(GameObject).IsAssignableFrom(type)) {
                var name = token.ToString();
                if (context.gameObjectMap.TryGetValue(name, out var gameObject)) {
                    return gameObject;
                }
                else {
                    Debug.LogWarning($"Unknown game object '{name}'.");
                    return null;
                }
            }

            if (typeof(MonoBehaviour).IsAssignableFrom(type)) {
                var name = token.ToString();
                if (context.gameObjectMap.TryGetValue(name, out var gameObject)) {
                    var behaviour = gameObject.GetComponent(type.Name);
                    if (!behaviour) {
                        Debug.LogWarning($"Cannot find MonoBehaviour '{type.Name}' on game object '{name}'.");
                    }
                    return behaviour;
                }
                else {
                    Debug.LogWarning($"Unknown game object '{name}' (looking for MonoBehaviour '{type.Name}').");
                    return null;
                }
            }

            return null;
        }

        Vector3 ReadVector3(JToken o) => new Vector3((float)o["x"], (float)o["y"], (float)o["z"]);

        static IEnumerable<IGrouping<GameObject, MonoBehaviour>> AllGameStates =>
            from b in FindObjectsOfType<MonoBehaviour>(includeInactive: true)
            where b is IGameState
            group b by b.gameObject;
    }
}