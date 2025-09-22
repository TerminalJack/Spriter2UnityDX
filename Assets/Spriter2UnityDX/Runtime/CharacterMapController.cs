using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spriter2UnityDX
{
    [Serializable]
    public class SpriteMapTarget
    {
        public Transform rendererTransform;
        public int imageIndex;

        public SpriteMapTarget(Transform _renderTransform, int _imageIndex)
        {
            rendererTransform = _renderTransform;
            imageIndex = _imageIndex;
        }
    }

    [Serializable]
    public class SpriteMapping
    {
        public Sprite sprite;
        public List<SpriteMapTarget> targets = new List<SpriteMapTarget>();

        public SpriteMapping(Sprite _sprite)
        {
            sprite = _sprite;
        }
    }

    [Serializable]
    public class CharacterMapping
    {
        public string name;
        public List<SpriteMapping> spriteMaps = new List<SpriteMapping>();

        public CharacterMapping(string _name)
        {
            name = _name;
        }

        public void Clear()
        {
            spriteMaps.Clear();
        }

        public void Add(Sprite sprite, SpriteMapTarget target)
        {
            SpriteMapping spriteMapping = spriteMaps.Find(s => s.sprite == sprite);

            if (spriteMapping == null)
            {
                spriteMapping = new SpriteMapping(sprite);
                spriteMapping.targets.Add(target);

                spriteMaps.Add(spriteMapping);
            }
            else
            {
                spriteMapping.targets.Add(target);
            }
        }
    }

    public class CharacterMapController : MonoBehaviour
    {
        public List<string> activeMapNames = new List<string>();

        public CharacterMapping baseMap = new CharacterMapping("base");
        public List<CharacterMapping> availableMaps = new List<CharacterMapping>();

        public void Clear()
        {
            activeMapNames.Clear();
            Refresh();
        }

        public bool Add(string mapName)
        {
            var map = availableMaps.Find(m => m.name == mapName);

            if (map == null)
            {
                return false;
            }

            var currentIdx = activeMapNames.FindIndex(n => n == mapName);

            if (currentIdx >= 0)
            {   // It is already in activeMaps.
                activeMapNames.RemoveAt(currentIdx);
            }

            activeMapNames.Add(map.name);

            Refresh();

            return true;
        }

        public bool Remove(string mapName)
        {
            var currentIdx = activeMapNames.FindIndex(n => n == mapName);

            if (currentIdx >= 0)
            {
                activeMapNames.RemoveAt(currentIdx);

                Refresh();

                return true;
            }

            return false; // Wasn't in activeMaps.
        }

        public void Refresh()
        {
            ApplyMap(baseMap);

            foreach (var mapName in activeMapNames)
            {
                var map = availableMaps.Find(m => m.name == mapName);

                if (map != null)
                {
                    ApplyMap(map);
                }
                else
                {
                    Debug.LogWarning($"CharacterMapController.Refresh(): The map name '{mapName}' is not valid.");
                }
            }
        }

        private void ApplyMap(CharacterMapping map)
        {
            foreach (var spriteMap in map.spriteMaps)
            {
                foreach (var target in spriteMap.targets)
                {
                    Transform targetTransform = target.rendererTransform;
                    int imageIndex = target.imageIndex;

                    TextureController textureController = targetTransform.GetComponent<TextureController>();
                    SpriteRenderer spriteRenderer = targetTransform.GetComponent<SpriteRenderer>();

                    if (textureController)
                    {
                        textureController.Sprites[imageIndex] = spriteMap.sprite;

                        if (textureController.DisplayedSprite == imageIndex)
                        {
                            spriteRenderer.sprite = spriteMap.sprite;
                        }
                    }
                    else
                    {
                        spriteRenderer.sprite = spriteMap.sprite;
                    }
                }
            }
        }

#if UNITY_EDITOR

        [CustomEditor(typeof(CharacterMapController))]
        protected class CharacterMapController_Editor : Editor
        {

            private static readonly GUIContent _activeMapNamesContent = new GUIContent(
                text: "Active Maps",
                tooltip: "The active character maps.  Add, remove, and rearrange the names from Available Maps to " +
                "this list and hit the 'Apply Active Maps' button to apply your changes.");

            private static readonly GUIContent _baseMapContent = new GUIContent(
                text: "Base Map",
                tooltip: "Base Map is this prefab's default character mapping.");

            private static readonly GUIContent _availableMapsContent = new GUIContent(
                text: "Available Maps",
                tooltip: "These are the character maps that are available for this prefab.  " +
                "Add their name(s) to Active Maps to activate them.");

            private static readonly GUIContent _applyActiveMapsButtonContent = new GUIContent(
                text: "Apply Active Maps",
                tooltip: "Click this to apply any changes that you make to Active Maps.");

            private SerializedProperty _activeMapNamesProperty;
            private SerializedProperty _baseMapProperty;
            private SerializedProperty _availableMapsProperty;

            private void OnEnable()
            {
                _activeMapNamesProperty = serializedObject.FindProperty("activeMapNames");
                _baseMapProperty = serializedObject.FindProperty("baseMap");
                _availableMapsProperty = serializedObject.FindProperty("availableMaps");
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();

                var characterMapController = target as CharacterMapController;

                GUI.enabled = false;
                EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour(characterMapController),
                    typeof(CharacterMapController), false);
                EditorGUILayout.Space();
                GUI.enabled = true;

                EditorGUILayout.PropertyField(_activeMapNamesProperty, _activeMapNamesContent);

                if (GUILayout.Button(_applyActiveMapsButtonContent))
                {
                    characterMapController.Refresh();

                    foreach (var textureController in characterMapController.GetComponentsInChildren<TextureController>())
                    {
                        EditorUtility.SetDirty(textureController);
                    }

                    foreach (var spriteRenderer in characterMapController.GetComponentsInChildren<SpriteRenderer>())
                    {
                        EditorUtility.SetDirty(spriteRenderer);
                    }

                    var stage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (stage != null)
                    {
                        EditorSceneManager.MarkSceneDirty(stage.scene);
                    }

                    AssetDatabase.SaveAssets();
                }

                EditorGUILayout.PropertyField(_baseMapProperty, _baseMapContent);
                EditorGUILayout.PropertyField(_availableMapsProperty, _availableMapsContent);

                serializedObject.ApplyModifiedProperties();
            }
        }

#endif // UNITY_EDITOR

    }
}