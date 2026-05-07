// Modifications Copyright (c) 2026 TerminalJack
// Licensed under the MIT License. See the LICENSE.TXT file in the project root for details.
//
// Portions of this file are derived from the Spriter2UnityDX project.
// The original author provided an open-use permission statement, preserved in THIRD_PARTY_NOTICES.md.

using System.Collections.Generic;
using UnityEngine;

namespace Stui
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class SpatialAdapter : MonoBehaviour
    {
        public Vector2 Position = new Vector2();
        public Vector2 Scale = Vector2.one;

        private readonly List<SpatialAdapter> _cachedSpatialAdapters = new List<SpatialAdapter>();
        private readonly List<VirtualParent> _cachedVirtualParents = new List<VirtualParent>();
        private readonly List<int> _cachedVersions = new List<int>();

        private bool _isSpriteOrPivot; // If false then the component is on a bone or action point.
        private SpatialController _spatialController;

        void OnEnable()
        {
            _isSpriteOrPivot = GetComponent<SpriteRenderer>() != null || GetComponent<DynamicPivot2D>() != null;

            // The animation curves will use the Spatial Controller component to control whether to use Spriter
            // scaling or not.  When importing, it should be created before any SpatialAdapter components.
            _spatialController = GetComponentInParent<SpatialController>();

            if (_spatialController == null)
            {   // This is a programming error and shouldn't happen in production.  (Or the user deleted it.)
                Debug.LogWarning("A SpatialController component could not be found.");
            }

            ApplySpriterScaling(forceResolution: true);
        }

#if UNITY_EDITOR
        void OnDidApplyAnimationProperties() => ApplySpriterScaling();
        void Update() { if (!Application.isPlaying) ApplySpriterScaling(); }
#endif

        void LateUpdate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            {
                ApplySpriterScaling();
            }
        }

        private void ApplySpriterScaling(bool forceResolution = false)
        {
            ResolveChainIfNeeded(forceResolution);

            bool useSpriterScaling = _spatialController?.UseSpriterScaling ?? false;

            if (!useSpriterScaling)
            {   // The values in Position and Scale are already baked.  They just need to be assigned to the transform.
                transform.localPosition = Position;
                transform.localScale = new Vector3(Scale.x, Scale.y, 1f);

                return;
            }

            Vector2 finalLocalScale = Vector2.one;

            foreach (var spatialAdapter in _cachedSpatialAdapters)
            {
                finalLocalScale *= spatialAdapter.Scale;
            }

            finalLocalScale = new Vector2(Mathf.Abs(finalLocalScale.x), Mathf.Abs(finalLocalScale.y));

            transform.localPosition = Position * finalLocalScale;

            transform.localScale = _isSpriteOrPivot
                ? new Vector3(
                    Scale.x * finalLocalScale.x,
                    Scale.y * finalLocalScale.y,
                    1f)
                : new Vector3(
                    Scale.x > 0f ? 1f : -1f,
                    Scale.y > 0f ? 1f : -1f,
                    1f);
        }

        private void ResolveChainIfNeeded(bool forceResolution)
        {
            if (forceResolution)
            {
                ResolveChain();

                return;
            }

            // If there are no VirtualParents in the chain then the chain will always be valid and will need to be
            // resolved only once.
            if (_cachedVirtualParents.Count == 0)
            {
                return;
            }

            // Otherwise, check the version numbers of each of the virtual parents and rebuild the chain if any of them
            // have changed.
            for (int i = 0; i < _cachedVirtualParents.Count; i++)
            {
                var vp = _cachedVirtualParents[i];

                if (vp != null && vp.version != _cachedVersions[i])
                {
                    ResolveChain();

                    return;
                }
            }
        }

        private void ResolveChain()
        {
            _cachedSpatialAdapters.Clear();
            _cachedVirtualParents.Clear();
            _cachedVersions.Clear();

            Transform t = transform.parent;

            int depth = 0; // Used to guard against cycles.

            while (t != null && ++depth < 100)
            {
                if (t.TryGetComponent(out SpatialAdapter s))
                {
                    _cachedSpatialAdapters.Add(s);
                }

                // Cache VirtualParent and its version, if present.
                if (t.TryGetComponent(out VirtualParent vp))
                {
                    _cachedVirtualParents.Add(vp);
                    _cachedVersions.Add(vp.version);

                    // Follow virtual parent redirection.  Note that this component will run during an import and, in
                    // that case, the possibleParents list can be empty for a short time.  We guard against that here.
                    // We rely on the 'version' changing once the list is updated.

                    t = vp.possibleParents.Count > 0
                        ? vp.possibleParents[vp.parentIndex]
                        : t.parent;
                }
                else
                {
                    t = t.parent;
                }
            }
        }
    }
}