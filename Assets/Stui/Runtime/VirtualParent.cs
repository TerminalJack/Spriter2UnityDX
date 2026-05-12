// Modifications Copyright (c) 2026 TerminalJack
// Licensed under the MIT License. See the LICENSE.TXT file in the project root for details.
//
// Portions of this file are derived from the Spriter2UnityDX project.
// The original author provided an open-use permission statement, preserved in THIRD_PARTY_NOTICES.md.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Stui
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class VirtualParent : MonoBehaviour
    {
        [Tooltip("List of Transforms whose local coordinate space this component will adopt when selected as its virtual parent.")]
        [FormerlySerializedAs("possibleParents")]
        public List<Transform> PossibleParents = new List<Transform>();

        [Tooltip("Selects which index from the Possible Parents list is currently active as the virtual parent.  " +
            "The importer reserves index 0 as the component's actual parent but you don't have to follow this norm.  " +
            "An invalid index will use the component's actual parent.")]
        [FormerlySerializedAs("parentIndex")]
        public int ParentIndex = -1;

        // Components that cache VirtualParents can use 'Version' to know when they need to rebuild their cache.
        public int Version
        {
            get
            {
                CheckForVersionChange();
                return _version;
            }
        }

        // Virtual parents with their own virtual parents as real ancestors and/or virtual ancestors need to ensure
        // the vp's further up the chain apply their constraints first.  That's what these are for.
        VirtualParent _firstVpAncestor;
        List<VirtualParent> _firstVpOfVirtualAncestors = new List<VirtualParent>();

        private int _version = 1;
        private int _lastParentIndex = -1;

        private int _lastFrameCount = -1; // The frame count when constraints were last applied.

        void OnEnable()
        {
            _firstVpAncestor = null;
            _firstVpOfVirtualAncestors.Clear();

            var parentTransform = transform.parent;

            while (parentTransform != null)
            {
                if (parentTransform.TryGetComponent(out _firstVpAncestor))
                {
                    break; // Found it.
                }

                parentTransform = parentTransform.parent;
            }

            // Note: The indices of _firstVpOfVirtualAncestors will match those of possibleParents.  That is,
            // _firstVpOfVirtualAncestors[n] is possibleParent[n]'s first vp, if any.  The exception to this is that
            // if there is an entry for the component's actual parent in possibleParents then the corresponding
            // entry in _firstVpOfVirtualAncestors will be null since _firstVpAncestor will already be set.

            for (int i = 0; i < PossibleParents.Count; ++i)
            {
                VirtualParent vp = null;

                if (PossibleParents[i] != _firstVpAncestor)
                {
                    var vpTransform = PossibleParents[i];

                    while (vpTransform != null)
                    {
                        if (vpTransform.TryGetComponent(out vp))
                        {
                            break;
                        }

                        vpTransform = vpTransform.parent;
                    }
                }

                _firstVpOfVirtualAncestors.Add(vp);
            }
        }

        void OnDidApplyAnimationProperties() => CheckForVersionChange();

        private void CheckForVersionChange()
        {
            if (ParentIndex != _lastParentIndex)
            {
                _version++;
                _lastParentIndex = ParentIndex;
            }
        }

#if UNITY_EDITOR
        void OnValidate() => ApplyConstraints();
        void Update() { if (!Application.isPlaying) ApplyConstraints(); }
#endif

        void LateUpdate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            {
                ApplyConstraints();
            }
        }

        private void ApplyConstraints()
        {
            if (_lastFrameCount == Time.frameCount)
            {
                return; // This component has already applied its constraints this frame.
            }

            _lastFrameCount = Time.frameCount;

            CheckForVersionChange();

            if (_firstVpAncestor != null && _firstVpAncestor._lastFrameCount != Time.frameCount)
            {
                _firstVpAncestor.ApplyConstraints();
            }

            if (ParentIndex > 0 && ParentIndex < _firstVpOfVirtualAncestors.Count)
            {
                var vp = _firstVpOfVirtualAncestors[ParentIndex];

                if (vp != null && vp._lastFrameCount != Time.frameCount)
                {
                    vp.ApplyConstraints();
                }
            }

            if (ParentIndex < 0 ||
                ParentIndex >= PossibleParents.Count ||
                PossibleParents[ParentIndex] == null ||
                PossibleParents[ParentIndex] == transform.parent)
            {
                // Either the parentIndex is invalid, its transform is null, or its transform is this transform's
                // actual parent.
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;

                return;
            }

            // For any other index, do manual "parent constraint" in 2D...

            var src = PossibleParents[ParentIndex];
            var virtualParent = transform;
            var realParent = transform.parent;

            if (src == null || realParent == null)
            {
                return;
            }

            AdoptSpace2D(virtualParent, src);
        }

        public static void AdoptSpace2D(Transform parent, Transform space)
        {
            if (parent == null || space == null)
            {
                return;
            }

            // world-to-local of grandparent
            var gp = parent.parent;
            var Mginv = gp ? gp.worldToLocalMatrix : Matrix4x4.identity;
            var Ms = space.localToWorldMatrix;

            // compute local TRS
            var Mlocal = Mginv * Ms;
            DecomposeLocal2D(
                in Mlocal,
                parent.localPosition.z,
                parent.localScale.z,
                out var lp,
                out var rotZ,
                out var ls);

            parent.localPosition = lp;
            parent.localRotation = Quaternion.AngleAxis(rotZ, Vector3.forward);
            parent.localScale = ls;
        }

        static void DecomposeLocal2D(
            in Matrix4x4 m,
            float currentZPos,
            float currentZScale,
            out Vector3 localPos,
            out float rotZDeg,
            out Vector3 localScale)
        {
            // XY position + preserved Z
            localPos = new Vector3(m.m03, m.m13, currentZPos);

            // XY basis
            Vector2 X = new Vector2(m.m00, m.m10);
            Vector2 Y = new Vector2(m.m01, m.m11);

            float sx = X.magnitude;
            float sy = Y.magnitude;
            // handle degenerate
            if (sx <= 1e-12f)
            {
                sx = 0f;
            }

            if (sy <= 1e-12f)
            {
                sy = 0f;
            }

            // handedness
            bool mirrored = (X.x * Y.y - X.y * Y.x) < 0f;
            // normalize for rotation
            Vector2 Xn = (sx > 0f) ? (X / sx) : Vector2.right;

            // mirror on Y axis only
            if (mirrored)
            {
                sy = -sy;
            }

            // rotation from Xn
            rotZDeg = Mathf.Atan2(Xn.y, Xn.x) * Mathf.Rad2Deg;
            localScale = new Vector3(sx, sy, currentZScale);
        }
    }
}