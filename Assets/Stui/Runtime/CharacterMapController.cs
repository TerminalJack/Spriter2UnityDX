// Modifications Copyright (c) 2026 TerminalJack
// Licensed under the MIT License. See the LICENSE.TXT file in the project root for details.
//
// Portions of this file are derived from the Spriter2UnityDX project.
// The original author provided an open-use permission statement, preserved in THIRD_PARTY_NOTICES.md.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Stui
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
        [FormerlySerializedAs("activeMapNames")]
        public List<string> ActiveMapNames = new List<string>();

        [FormerlySerializedAs("baseMap")]
        public CharacterMapping BaseMap = new CharacterMapping("base");

        [FormerlySerializedAs("availableMaps")]
        public List<CharacterMapping> AvailableMaps = new List<CharacterMapping>();

        public void Clear()
        {
            ActiveMapNames.Clear();
            Refresh();
        }

        public bool Add(string mapName)
        {
            var map = AvailableMaps.Find(m => m.name == mapName);

            if (map == null)
            {
                return false;
            }

            var currentIdx = ActiveMapNames.FindIndex(n => n == mapName);

            if (currentIdx >= 0)
            {   // It is already in activeMaps.
                ActiveMapNames.RemoveAt(currentIdx);
            }

            ActiveMapNames.Add(map.name);

            Refresh();

            return true;
        }

        public bool Remove(string mapName)
        {
            var currentIdx = ActiveMapNames.FindIndex(n => n == mapName);

            if (currentIdx >= 0)
            {
                ActiveMapNames.RemoveAt(currentIdx);

                Refresh();

                return true;
            }

            return false; // Wasn't in activeMaps.
        }

        public void Refresh(bool logWarnings = true)
        {
            ApplyMap(BaseMap);

            foreach (var mapName in ActiveMapNames)
            {
                var map = AvailableMaps.Find(m => m.name == mapName);

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

                    if (targetTransform.TryGetComponent<SpriteRenderer>(out var spriteRenderer))
                    {
                        if (targetTransform.TryGetComponent<TextureController>(out var textureController))
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
        }
    }
}