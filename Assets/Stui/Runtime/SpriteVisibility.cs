// Modifications Copyright (c) 2026 TerminalJack
// Licensed under the MIT License. See the LICENSE.TXT file in the project root for details.
//
// Portions of this file are derived from the Spriter2UnityDX project.
// The original author provided an open-use permission statement, preserved in THIRD_PARTY_NOTICES.md.

using UnityEngine;
using UnityEngine.Serialization;

// This script exists to provide compatibility between a wide variety of Unity versions.  At some point it was no
// longer possible to bind animation curves to the SpriteRenderer.enabled property.

namespace Stui
{
    [ExecuteAlways]
    public class SpriteVisibility : MonoBehaviour
    {
        [FormerlySerializedAs("isVisible")]
        public bool IsVisible = false;

        private SpriteRenderer _spriteRenderer;
        private bool _lastIsVisible = true; // Note: Using this roughly doubles the speed of ApplyVisibility().

        void OnEnable()
        {
            TryGetComponent(out _spriteRenderer);
            ApplyVisibility();
        }

#if UNITY_EDITOR
        void OnDidApplyAnimationProperties() => ApplyVisibility();
        void Update() { if (!Application.isPlaying) ApplyVisibility(); }
#endif

        void LateUpdate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            {
                ApplyVisibility();
            }
        }

        private void ApplyVisibility()
        {
            if (_spriteRenderer != null && _lastIsVisible != IsVisible)
            {
                _lastIsVisible = IsVisible;
                _spriteRenderer.enabled = IsVisible;
            }
        }
    }
}