using UnityEngine;

// This script exists to provide compatibility between a wide varity of Unity versions.

[ExecuteAlways]
public class SpriteVisibility : MonoBehaviour
{
    public float isVisible = 0f; // Bools aren't supported in the animator on newer versions of Unity so we get this hack.

    private SpriteRenderer _spriteRenderer;

    void OnEnable()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        ApplyVisibility();
    }

    void OnDidApplyAnimationProperties() => ApplyVisibility();
    void Update() { if (!Application.isPlaying) ApplyVisibility(); }
    void LateUpdate() { if (Application.isPlaying) ApplyVisibility(); }

    private void ApplyVisibility()
    {
        if (_spriteRenderer != null)
        {
            isVisible = Mathf.RoundToInt(isVisible);
            _spriteRenderer.enabled = isVisible != 0;
        }
    }
}