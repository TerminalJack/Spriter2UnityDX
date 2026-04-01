using UnityEngine;

namespace Spriter2UnityDX
{
    [DisallowMultipleComponent]
    public class SpriterInt : MonoBehaviour
    {
        [Tooltip("The name of this int variable.")]
        public string variableName;

        [Tooltip("The int variable's default value.")]
        public int defaultValue = -1;

        [Tooltip("The variable's current value as a float.  This is the animated property.  Use the 'value' property " +
            "to get the variable's current value as an int.")]
        public float valueAsFloat = -1f;

        public int value { get { return (int)valueAsFloat; } }
     }
}
