using UnityEngine;

namespace Spriter2UnityDX
{
    [DisallowMultipleComponent]
    public class SpriterTag : MonoBehaviour
    {
        [Tooltip("The name of this tag.")]
        public string tagName;

        [Tooltip("Is this tag currently active?  This is the animated property.")]
        public bool isActive = false;
    }
}
