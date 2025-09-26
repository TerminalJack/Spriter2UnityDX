using System;
using System.Collections.Generic;
using UnityEngine;

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