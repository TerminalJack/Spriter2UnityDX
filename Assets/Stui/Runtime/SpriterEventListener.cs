using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Spriter2UnityDX
{
    [DisallowMultipleComponent]
    public class SpriterEventListener : MonoBehaviour
    {
        [Serializable]
        private class SpriterEventBinding
        {
            public UnityEvent callback;
        }

        // Hook events up in the Inspector via this property.
        [SerializeField] private List<SpriterEventBinding> _bindings = new List<SpriterEventBinding>();

        [HideInInspector] public string _eventName; // The importer will set this.
        private EventController _controller;

        // Public API for user scripts to register/unregister handlers programmatically.
        public void Register(Action callback)
        {
            if (_controller != null)
            {
                _controller.AddHandler(_eventName, callback);
            }
        }

        public void Unregister(Action callback)
        {
            if (_controller != null)
            {
                _controller.RemoveHandler(_eventName, callback);
            }
        }

        private void Awake()
        {
            // Find the EventController on the root of the prefab.
            _controller = GetComponentInParent<EventController>();

            if (_controller == null)
            {
                Debug.LogError($"SpriterEventListener on {gameObject.name} could not find an EventController in its parents.");
            }
        }

        private void OnEnable()
        {
            if (_controller != null)
            {
                // Register all bindings.
                foreach (var binding in _bindings)
                {
                    if (binding != null && binding.callback != null)
                    {
                        _controller.AddHandler(_eventName, binding.callback.Invoke);
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (_controller != null)
            {
                // Unregister all bindings
                foreach (var binding in _bindings)
                {
                    if (binding != null && binding.callback != null)
                    {
                        _controller.RemoveHandler(_eventName, binding.callback.Invoke);
                    }
                }
            }
        }
    }
}
