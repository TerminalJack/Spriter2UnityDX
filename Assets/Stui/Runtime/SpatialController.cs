// Modifications Copyright (c) 2026 TerminalJack
// Licensed under the MIT License. See the LICENSE.TXT file in the project root for details.
//
// Portions of this file are derived from the Spriter2UnityDX project.
// The original author provided an open-use permission statement, preserved in THIRD_PARTY_NOTICES.md.

using UnityEngine;

namespace Stui
{
    [DisallowMultipleComponent]
    public class SpatialController : MonoBehaviour
    {
        // When an entity has one or more animatons that use animated bone scales AND the user enabled support for the
        // feature, this component will be placed on the prefab's root.  Animation curves will control the
        // UseSpriterScaling property.  SpatialAdapter components will then use this property to determine how to apply
        // positions and scales.

        public bool UseSpriterScaling;
    }
}
