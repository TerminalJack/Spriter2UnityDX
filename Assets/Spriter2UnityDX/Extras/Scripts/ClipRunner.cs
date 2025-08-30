using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Spriter2UnityDX.Extras
{
    [RequireComponent(typeof(Animator))]
    public class ClipRunner : MonoBehaviour
    {
        public enum ClipPlayOrder
        {
            OrderByName,
            RandomOrder
        }

        public float timePerClip = 5f;
        public bool crossFade = false;
        public ClipPlayOrder playOrder = ClipPlayOrder.RandomOrder;

        [Tooltip("Drag this around in-scene to move the label.")]
        public Transform labelAnchor;

        private Vector3 _defaultLabelOffset = new Vector3(0f, -1.5f, 0f);

        private List<AnimationClip> _clips = new List<AnimationClip>();
        private int _clipIndex;

        private Animator _animator;
        private Camera _mainCam;

        private readonly System.Random _rng = new System.Random();

        void Reset()
        {
            EnsureLabelAnchorIsCreated();
        }

        void OnEnable()
        {
            _mainCam = Camera.main;
            EnsureLabelAnchorIsCreated();
        }

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _mainCam = Camera.main;

            var rtController = _animator.runtimeAnimatorController;
            if (rtController != null)
            {
                _clips = rtController.animationClips
                    .GroupBy(c => c.name)
                    .Select(g => g.First())
                    .OrderBy(c => c.name)
                    .ToList();

                if (playOrder == ClipPlayOrder.RandomOrder)
                {
                    int n = _clips.Count;

                    while (n > 1)
                    {
                        n--;
                        int k = _rng.Next(n + 1);

                        AnimationClip temp = _clips[k];
                        _clips[k] = _clips[n];
                        _clips[n] = temp;
                    }
                }
            }
        }

        IEnumerator Start()
        {
            if (_clips.Count == 0)
            {
                yield break;
            }

            while (true)
            {
                _clipIndex = Mathf.Clamp(_clipIndex, 0, _clips.Count - 1);

                if (crossFade)
                {
                    _animator.CrossFade(_clips[_clipIndex].name, 0.3f, 0, 0f);
                }
                else
                {
                    _animator.Play(_clips[_clipIndex].name, 0, 0f);
                }

                _animator.Update(0f);

                yield return new WaitForSeconds(timePerClip);

                _clipIndex = (_clipIndex + 1) % _clips.Count;
            }
        }

        private void EnsureLabelAnchorIsCreated()
        {
            if (labelAnchor != null)
            {
                return;
            }

            if (_mainCam == null)
            {
                _mainCam = Camera.main;
            }

            var go = transform.Find("LabelAnchor")?.gameObject ?? new GameObject("LabelAnchor");
            go.transform.SetParent(transform, false);

            float bottomLocalY = GetBottomMostLocalY();
            go.transform.localPosition = new Vector3(0f, bottomLocalY, 0f) + _defaultLabelOffset;

            labelAnchor = go.transform;
        }


        private float GetBottomMostLocalY()
        {
            // Get the minimum local y value, taking into consideration all of the visible sprite renderers.  This
            // bottom-most bound doesn't take into account any of the sprites' alpha, however, so visually the value
            // may 'look' incorrect.

            float minY = float.PositiveInfinity;

            // Iterate all child SpriteRenderers
            foreach (var sr in GetComponentsInChildren<SpriteRenderer>(includeInactive: false))
            {
                if (sr.sprite == null || sr.enabled == false)
                {
                    continue;
                }

                // Get the sprite's local bounds
                var b = sr.sprite.bounds;
                Vector3[] localCorners = new Vector3[4]
                {
                    new Vector3(b.min.x, b.min.y, 0f),
                    new Vector3(b.min.x, b.max.y, 0f),
                    new Vector3(b.max.x, b.max.y, 0f),
                    new Vector3(b.max.x, b.min.y, 0f),
                };

                // Transform each corner to world space and track the minimum Y
                for (int i = 0; i < 4; i++)
                {
                    Vector3 worldCorner = sr.transform.TransformPoint(localCorners[i]);
                    if (worldCorner.y < minY)
                    {
                        minY = worldCorner.y;
                    }
                }
            }

            // If no sprites were found, default to this object's Y
            if (minY == float.PositiveInfinity)
            {
                minY = transform.position.y;
            }

            return transform.InverseTransformPoint(new Vector3(0f, minY, 0f)).y;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying && labelAnchor != null)
            {
                GUIStyle style = new GUIStyle
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal = new GUIStyleState { textColor = Color.yellow }
                };

                Handles.Label(labelAnchor.position, "Clip name [1 of X]", style);
            }
        }

        private void OnGUI()
        {
            if (_clips != null && _clips.Count > 0 && _clipIndex < _clips.Count)
            {
                string labelText = $"{_clips[_clipIndex].name}  [{_clipIndex + 1} of {_clips.Count}]";
                SetLabelText(labelText);
            }
        }

        private void SetLabelText(string labelText)
        {
            if (_mainCam == null)
            {
                _mainCam = Camera.main;
            }

            Vector3 worldPos = labelAnchor.position;
            Vector3 screenPos = _mainCam.WorldToScreenPoint(worldPos);

            float desiredWorldHeight = 0.8f; // tweak this to taste
            float pixelsPerUnit = Screen.height / (_mainCam.orthographicSize * 2f);
            int fontSize = Mathf.RoundToInt(pixelsPerUnit * desiredWorldHeight);

            GUIStyle style = new GUIStyle
            {
                fontSize = fontSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState { textColor = Color.yellow }
            };

            if (screenPos.z > 0f) // only if in front of camera
            {
                // flip Y for GUI
                float guiY = Screen.height - screenPos.y;

                GUI.Label(
                    new Rect(screenPos.x - 300f, guiY - 30f, 600f, 60f),
                    labelText,
                    style
                );
            }
        }
    }
}