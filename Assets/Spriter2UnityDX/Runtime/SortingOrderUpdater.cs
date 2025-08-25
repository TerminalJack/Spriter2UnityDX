using UnityEngine;

namespace Spriter2UnityDX
{
    [ExecuteInEditMode]
    public class SortingOrderUpdater : MonoBehaviour
    {
        private SpriteRenderer _spriteRendererComponent;

        private int _sortingOrder;

        public int SortingOrder
        {
            get { return _sortingOrder; }
            set
            {
                _sortingOrder = value;
                UpdateSortingOrder();
            }
        }

        private void OnEnable()
        {
            // The SpriteRenderer is on this game object or a child.
            _spriteRendererComponent = GetComponentInChildren<SpriteRenderer>(true); // true == includeInactive.
        }

        void OnDidApplyAnimationProperties() => UpdateSortingOrder(); // Called right after Animation system writes properties
        private void LateUpdate() => UpdateSortingOrder();

        private void UpdateSortingOrder()
        {
            if (_spriteRendererComponent)
            {
                // The 'magic number' here also appears in ScmlSupport.cs.
                _spriteRendererComponent.sortingOrder = Mathf.RoundToInt(-10000f * transform.localPosition.z);
            }
        }
    }
}