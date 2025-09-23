using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
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

        public void Refresh(bool logWarnings = true)
        {
            ApplyMap(baseMap);

            foreach (var mapName in activeMapNames)
            {
                var map = availableMaps.Find(m => m.name == mapName);

                if (map != null)
                {
                    ApplyMap(map);
                }
                else if (logWarnings)
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
#if UNITY_EDITOR
                        EditorUtility.SetDirty(textureController);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(textureController);
#endif

                        if (textureController.DisplayedSprite == imageIndex)
                        {
                            spriteRenderer.sprite = spriteMap.sprite;
#if UNITY_EDITOR
                            EditorUtility.SetDirty(spriteRenderer);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(spriteRenderer);
#endif
                        }
                    }
                    else
                    {
                        spriteRenderer.sprite = spriteMap.sprite;
#if UNITY_EDITOR
                        EditorUtility.SetDirty(spriteRenderer);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(spriteRenderer);
#endif
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
                "this list.  Updates should be automatic but the 'Apply Active Maps' button can be used to ensure " +
                "that all changes take effect.  (As well as validate all of the names in the list.)");

            private static readonly GUIContent _baseMapContent = new GUIContent(
                text: "Base Map",
                tooltip: "Base Map is this prefab's default character mapping.");

            private static readonly GUIContent _availableMapsContent = new GUIContent(
                text: "Available Maps",
                tooltip: "These are the character maps that are available for this prefab.  Add/remove their name(s) " +
                "to Active Maps via the +/- buttons.");

            private static readonly GUIContent _applyActiveMapsButtonContent = new GUIContent(
                text: "Apply Active Maps",
                tooltip: "Click this to apply any changes that you make to Active Maps and validate the names in the " +
                "list.  Any invalid names will be logged to the console.");

            private SerializedProperty _activeMapNamesProperty;
            private SerializedProperty _baseMapProperty;
            private SerializedProperty _availableMapsProperty;

            private ReorderableList _availableMapsList;
            private bool _showAvailableMaps;

            private void OnEnable()
            {
                _activeMapNamesProperty = serializedObject.FindProperty("activeMapNames");
                _baseMapProperty = serializedObject.FindProperty("baseMap");
                _availableMapsProperty = serializedObject.FindProperty("availableMaps");

                _availableMapsList = new ReorderableList(
                    serializedObject,
                    _availableMapsProperty,
                    draggable: true,
                    displayHeader: false,
                    displayAddButton: true,
                    displayRemoveButton: true
                );

                _availableMapsList.elementHeightCallback = index =>
                {
                    var element = _availableMapsProperty.GetArrayElementAtIndex(index);

                    // Base line + spacing
                    float height = EditorGUIUtility.singleLineHeight + 4;

                    // If its foldout is open, add the full height of all child properties
                    if (element.isExpanded)
                    {
                        height = EditorGUI.GetPropertyHeight(element, true) + 4;
                    }

                    return height;
                };

                _availableMapsList.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    rect.y += 1;

                    var element = _availableMapsProperty.GetArrayElementAtIndex(index);

                    // Foldout field
                    var fieldRect = new Rect(rect.x + 10f, rect.y, rect.width - 34f, EditorGUIUtility.singleLineHeight);
                    EditorGUI.PropertyField(fieldRect, element);

                    // +/- Button on the right
                    var addRemoveButtonRect = new Rect(
                        fieldRect.x + fieldRect.width + 4f,
                        rect.y + 1f,
                        24f,
                        EditorGUIUtility.singleLineHeight - 1f
                    );

                    var controller = (CharacterMapController)target;
                    var mapName = controller.availableMaps[index].name;
                    bool isActiveMapName = controller.activeMapNames.Contains(mapName);

                    Color oldBG = GUI.backgroundColor;

                    if (EditorGUIUtility.isProSkin)
                    {
                        GUI.backgroundColor = isActiveMapName
                            ? new Color(0.75f, 0f, 0f)     // red
                            : new Color(0.2f, 0.8f, 0.2f); // green
                    }
                    else
                    {
                        GUI.backgroundColor = isActiveMapName
                            ? new Color(1f, 0.75f, 0.75f) // red
                            : new Color(0.7f, 1f, 0.7f);  // green
                    }

                    if (GUI.Button(addRemoveButtonRect, isActiveMapName ? "-" : "+"))
                    {
                        if (isActiveMapName)
                        {
                            controller.Remove(mapName);
                        }
                        else
                        {
                            controller.Add(mapName);
                        }

                        EditorUtility.SetDirty(controller);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
                    }

                    GUI.backgroundColor = oldBG;

                    // If expanded, draw all of the children
                    if (element.isExpanded)
                    {
                        var childRect = new Rect(
                            fieldRect.x,
                            fieldRect.y,
                            fieldRect.width + addRemoveButtonRect.width + 2f,
                            EditorGUI.GetPropertyHeight(element, true) - EditorGUIUtility.singleLineHeight);

                        EditorGUI.PropertyField(childRect, element, GUIContent.none, includeChildren: true);
                    }
                };
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

                EditorGUI.BeginChangeCheck();

                EditorGUILayout.PropertyField(_activeMapNamesProperty, _activeMapNamesContent);

                if (EditorGUI.EndChangeCheck())
                {   // Something changed: add, remove, reorder, or edit strings
                    EditorApplication.delayCall += () => RefreshCharacterMapController(characterMapController, logWarnings: false);
                }

                if (GUILayout.Button(_applyActiveMapsButtonContent))
                {
                    RefreshCharacterMapController(characterMapController);
                }

                _showAvailableMaps = EditorGUILayout.Foldout(
                    _showAvailableMaps,
                    _availableMapsContent,
                    toggleOnLabelClick: true);

                if (_showAvailableMaps)
                {
                    EditorGUI.indentLevel++;
                    _availableMapsList.DoLayoutList();
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.PropertyField(_baseMapProperty, _baseMapContent);

                serializedObject.ApplyModifiedProperties();
            }

            private void RefreshCharacterMapController(CharacterMapController characterMapController, bool logWarnings = true)
            {
                characterMapController.Refresh(logWarnings);

                EditorUtility.SetDirty(characterMapController);
                PrefabUtility.RecordPrefabInstancePropertyModifications(characterMapController);

                var stage = PrefabStageUtility.GetCurrentPrefabStage();
                if (stage != null)
                {
                    EditorSceneManager.MarkSceneDirty(stage.scene);
                }

                AssetDatabase.SaveAssets();
            }
        }

#endif // UNITY_EDITOR

    }
}