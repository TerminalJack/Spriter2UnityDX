using System.Collections.Generic;
using UnityEngine;

namespace Spriter2UnityDX
{
    [DisallowMultipleComponent]
    public class SpriterString : MonoBehaviour
    {
        [Tooltip("The name of this string variable.")]
        public string variableName;

        [Tooltip("List of strings that this string variable can have as a value.  The first entry is reserved for " +
            "the string variable's default value.")]
        public List<string> possibleValues = new List<string>();

        [Tooltip("The index, from the Possible Values list, of the current value of the string variable.  This is " +
            "the animated property.  Use the 'value' property to get the string's current value.")]
        public int valueIndex = -1;

        public string defaultValue { get { return GetDefaultValue();  } }

        public string value { get { return GetCurrentValue(); } }

        private string GetDefaultValue()
        {
            return possibleValues.Count > 0
                ? possibleValues[0]
                : "";
        }

        private string GetCurrentValue()
        {
            return valueIndex >= 0 && valueIndex < possibleValues.Count
                ? possibleValues[valueIndex]
                : defaultValue;
        }
    }
}
