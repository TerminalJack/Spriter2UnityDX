// Modifications Copyright (c) 2026 TerminalJack
// Licensed under the MIT License. See the LICENSE.TXT file in the project root for details.
//
// Portions of this file are derived from the Spriter2UnityDX project.
// The original author provided an open-use permission statement, preserved in THIRD_PARTY_NOTICES.md.

using System.Collections.Generic;
using UnityEngine;

namespace Stui
{
    public interface ITransformModifier
    {
        void ApplyTransformModifier();
    }

    // This component ensures that VirtualParents and SpatialAdapters in the prefab's hierarchy run in the proper order.
    // Child components require that their parents (_both_ real and virtual) apply any modifications to their transforms
    // before they (the child) do.  This allows for arbitrary nesting of VirtualParents and/or SpatialAdapters.  If a
    // prefab has any VirtualParents or SpatialAdapters at the time of import then a DependencyResolver component will
    // be placed on the prefab's root game object.

    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class DependencyResolver : MonoBehaviour
    {
        private class VpInfo
        {
            public VirtualParent virtualParent;
            public int version;
        }

        private struct TransformModifierInfo
        {
            public ITransformModifier modifier;
            public Transform thisModifierTransform; // The transform the modifier is on.
            public Transform nextModifierTransform; // The transform of the next ancestor that is also a modifier, if any.
        }

        private readonly List<TransformModifierInfo> _modifierInfos = new List<TransformModifierInfo>();
        private readonly Dictionary<Transform, VpInfo> _vpInfos = new Dictionary<Transform, VpInfo>();
        private readonly List<ITransformModifier> _sortedTransformModifiers = new List<ITransformModifier>();
        private readonly Dictionary<Transform, bool> _visitedTransforms = new Dictionary<Transform, bool>();

        private bool _isDirty = false;

        public void MarkDirty() // The importer will use this.
        {
            _isDirty = true;
        }

        void OnEnable()
        {
            RefreshAndRebuild();

#if UNITY_EDITOR
            UnityEditor.SceneView.duringSceneGui += OnSceneGUI;
#endif
        }

#if UNITY_EDITOR

        void OnDisable()
        {
            UnityEditor.SceneView.duringSceneGui -= OnSceneGUI;
        }

        void OnSceneGUI(UnityEditor.SceneView view)
        {
            // Note: This fixes a one-frame glitch in the editor when switching between two animation previews whose
            // hierarchies have different virtual parent configurations.

            if (Application.isPlaying)
            {
                return;
            }

            if (NeedsRebuild())
            {
                RebuildResolvedParents();
                RebuildDependencyOrder();
            }

            RunTransformModifiers();
        }
#endif

        private void RefreshAndRebuild()
        {
            RefreshVirtualParents();
            RefreshTransformModifiers();
            RebuildResolvedParents();
            RebuildDependencyOrder();
        }

        private void RefreshVirtualParents()
        {
            _vpInfos.Clear();

            foreach (VirtualParent vp in GetComponentsInChildren<VirtualParent>())
            {
                _vpInfos.Add(vp.transform, new VpInfo { virtualParent = vp, version = vp.Version });
            }
        }

        private void RefreshTransformModifiers()
        {
            _modifierInfos.Clear();

            List<ITransformModifier> _transformModifiers = new List<ITransformModifier>();

            GetComponentsInChildren(includeInactive: true, _transformModifiers);

            foreach (var modifier in _transformModifiers)
            {
                _modifierInfos.Add(new TransformModifierInfo
                {
                    modifier = modifier,
                    thisModifierTransform = ((MonoBehaviour)modifier).transform,
                    nextModifierTransform = null
                });
            }
        }

        // Note: This is used to avoid generating garbage.
        private int FindModifierTransformIndex(Transform t)
        {
            for (int i = 0; i < _modifierInfos.Count; ++i)
            {
                if (_modifierInfos[i].thisModifierTransform == t)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RebuildResolvedParents()
        {
            // Walk up the chain and find each of the modifiers' next ancestor that is also a modifier, if any.
            for (int i = 0; i < _modifierInfos.Count; i++)
            {
                var info = _modifierInfos[i];

                Transform nextAncestor = info.thisModifierTransform.parent;

                // If this modifier is a virtual parent then get the appropriate parent and update the cached vp version.
                if (_vpInfos.TryGetValue(info.thisModifierTransform, out var vpInfo))
                {
                    nextAncestor = vpInfo.virtualParent.GetVirtualParentTransform();

                    vpInfo.version = vpInfo.virtualParent.Version;
                }

                // Continue up the chain of ancestors until there are no more or one is found in _modifierinfos.
                while (nextAncestor != null && FindModifierTransformIndex(nextAncestor) == -1)
                {
                    nextAncestor = nextAncestor.parent;
                }

                info.nextModifierTransform = nextAncestor;
                _modifierInfos[i] = info;
            }
        }

        private bool NeedsRebuild()
        {
            foreach (var vpInfo in _vpInfos.Values)
            {
                if (vpInfo.virtualParent.Version != vpInfo.version)
                {
                    return true;
                }
            }

            return false;
        }

        private void RebuildDependencyOrder()
        {
            _sortedTransformModifiers.Clear();
            _visitedTransforms.Clear();

            void Visit(int index)
            {
                var info = _modifierInfos[index];
                Transform t = info.thisModifierTransform;

                if (_visitedTransforms.TryGetValue(t, out bool done) && done)
                {
                    return;
                }

                _visitedTransforms[t] = true;

                if (info.nextModifierTransform != null)
                {
                    int parentIndex = FindModifierTransformIndex(info.nextModifierTransform);

                    if (parentIndex >= 0)
                    {
                        Visit(parentIndex);
                    }
                }

                _sortedTransformModifiers.Add(info.modifier);
            }

            for (int i = 0; i < _modifierInfos.Count; ++i)
            {
                Visit(i);
            }
        }

#if UNITY_EDITOR
        void Update() { if (!Application.isPlaying) RunTransformModifiers(); }
#endif

        void LateUpdate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
#endif
            {
                RunTransformModifiers();
            }
        }

        private void RunTransformModifiers()
        {
            if (_isDirty)
            {
                RefreshAndRebuild();
                _isDirty = false;
            }
            else if (NeedsRebuild())
            {
                RebuildResolvedParents();
                RebuildDependencyOrder();
            }

            for (int i = 0; i < _sortedTransformModifiers.Count; i++)
            {
                _sortedTransformModifiers[i].ApplyTransformModifier();
            }
        }
    }
}