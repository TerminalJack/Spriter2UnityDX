//This project is open source. Anyone can use any part of this code however they wish
//Feel free to use this code in your own projects, or expand on this code
//If you have any improvements to the code itself, please visit
//https://github.com/Dharengo/Spriter2UnityDX and share your suggestions by creating a fork
//-Dengar/Dharengo

using UnityEngine;

namespace Spriter2UnityDX
{
	// This component is automatically added to sprite parts that have multiple possible
	// textures, such as facial expressions. This component will override any changes
	// you make to the SpriteRenderer's textures, so if you want to change textures
	// at runtime, please make these changes to this component, rather than SpriteRenderer

    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    [AddComponentMenu("")]
	[RequireComponent (typeof(SpriteRenderer))]
	public class TextureController : MonoBehaviour
    {
		public float DisplayedSprite = 0f; // Input from the AnimationClip
		public Sprite[] Sprites; // If you want to swap textures at runtime, change the sprites in this array

		private SpriteRenderer srenderer;
		private Animator animator;

        private void Start() => SelectSprite();
        private void OnDidApplyAnimationProperties() => SelectSprite();
        private void LateUpdate() => SelectSprite();

        private void Awake()
        {
            srenderer = GetComponent<SpriteRenderer>();
			animator = GetComponentInParent<Animator> ();
        }

        private void SelectSprite()
        {
			// Ignore changes that happen during transitions because it might get messy otherwise.
            if (!IsTransitioning())
            {
                srenderer.sprite = Sprites[Mathf.RoundToInt(DisplayedSprite)];
            }
        }

		private bool IsTransitioning()
        {
            if (Application.isPlaying &&
                animator.isActiveAndEnabled &&
                animator.runtimeAnimatorController != null)
            {
                for (var i = 0; i < animator.layerCount; i++)
                {
                    if (animator.IsInTransition(i))
                    {
                        return true;
                    }

                }
            }

            return false;
		}
	}
}
