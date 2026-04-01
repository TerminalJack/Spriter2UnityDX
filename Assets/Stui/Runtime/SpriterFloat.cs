using UnityEngine;

namespace Spriter2UnityDX
{
    [DisallowMultipleComponent]
    public class SpriterFloat : MonoBehaviour
    {
        [Tooltip("The name of this float variable.")]
        public string variableName;

        [Tooltip("The float variable's default value.")]
        public float defaultValue = -1.0f;

        [Tooltip("The float variable's current value.  This is the animated property.")]
        public float value = -1.0f;
    }
}