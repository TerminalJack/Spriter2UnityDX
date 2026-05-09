// Modifications Copyright (c) 2026 TerminalJack
// Licensed under the MIT License. See the LICENSE.TXT file in the project root for details.
//
// Portions of this file are derived from the Spriter2UnityDX project.
// The original author provided an open-use permission statement, preserved in THIRD_PARTY_NOTICES.md.

using UnityEngine;

namespace Stui
{
    [DisallowMultipleComponent]
    public class ScaleTracker : MonoBehaviour
    {
        // Bones that don't have animated bone scales but are the ancestors of bones with animated bone scales will
        // need to track their raw scales.  The 'raw scale' being the scale read from the Spriter project file.
        // These particular bones will still use their baked scale and positions via the transform.  The scale
        // tracking is done with this component.  The component does nothing other than hold the (keyed and animated)
        // scale data.  This data will be used by descendants with SpatialAdapters when they need to calculate their
        // final scale and position.
        //
        // Typically the value of RawScale will be static but it _can_ change in certain circumstances:
        //
        //     * Parent changes.
        //     * Pivot changes.
        //     * Bone scale changes that are too quick or too small to bother animating.

        public Vector2 RawScale = Vector2.one;
    }
}