// Modifications Copyright (c) 2026 TerminalJack
// Licensed under the MIT License. See the LICENSE.TXT file in the project root for details.
//
// Portions of this file are derived from the Spriter2UnityDX project.
// The original author provided an open-use permission statement, preserved in THIRD_PARTY_NOTICES.md.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Stui
{
    [Serializable]
    public class Collider2DEvent : UnityEvent<Collider2D> {}

    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class CollisionRectangle : MonoBehaviour
    {
        [Serializable]
        private class CollisionEventBinding
        {
            public Collider2DEvent callback = new Collider2DEvent();
        }

        // Hook events up in the Inspector via these properties.
        [SerializeField] private List<CollisionEventBinding> _onEnterBindings = new List<CollisionEventBinding>();
        [SerializeField] private List<CollisionEventBinding> _onStayBindings = new List<CollisionEventBinding>();
        [SerializeField] private List<CollisionEventBinding> _onExitBindings = new List<CollisionEventBinding>();

        // Public API for user scripts to register/unregister handlers programmatically.
        [NonSerialized] public readonly Collider2DEvent OnEnter = new Collider2DEvent();
        [NonSerialized] public readonly Collider2DEvent OnStay = new Collider2DEvent();
        [NonSerialized] public readonly Collider2DEvent OnExit = new Collider2DEvent();

        void OnTriggerEnter2D(Collider2D collision)
        {
            OnEnter?.Invoke(collision);

            foreach (var onEnterAction in _onEnterBindings)
            {
                onEnterAction.callback?.Invoke(collision);
            }
        }

        void OnTriggerStay2D(Collider2D collision)
        {
            OnStay?.Invoke(collision);

            foreach (var onStayAction in _onStayBindings)
            {
                onStayAction.callback?.Invoke(collision);
            }
        }

        void OnTriggerExit2D(Collider2D collision)
        {
            OnExit?.Invoke(collision);

            foreach (var onExitAction in _onExitBindings)
            {
                onExitAction.callback?.Invoke(collision);
            }
        }
    }
}
