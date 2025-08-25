using UnityEngine;

namespace Spriter2UnityDX.Extensions
{
    public static class CustomExtensions
    {
        /// <summary>
        /// Gets the component of type T from the Transform's GameObject, or adds it if missing.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <param name="transform">Target Transform</param>
        /// <returns>Existing or newly added component of type T</returns>
        public static T GetOrAddComponent<T>(this Transform transform) where T : Component
        {
            T component = transform.GetComponent<T>();
            if (component == null)
            {
                component = transform.gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Gets the component of type T if it exists, otherwise adds it and returns the new instance.
        /// </summary>
        /// <typeparam name="T">Type of the component</typeparam>
        /// <param name="gameObject">Target GameObject</param>
        /// <returns>Existing or newly added component of type T</returns>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }
    }
}