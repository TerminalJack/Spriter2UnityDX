using UnityEngine;

// -----------------------------------------------------------------------------
// How to determine the ideal scaling factor and PPU for an imported Spriter entity
// -----------------------------------------------------------------------------
//
// TL;DR — Quick Use:
//   1. Place prefab in scene with your final orthographic camera.
//   2. Attach this script to the prefab root.
//   3. Set Reference & Target resolutions, assign camera.
//   4. Uniform-scale prefab to final in-game size.
//   5. Read Scaling Factor & Recommended PPU in Inspector.
//   6. Use Scaling Factor when resizing Spriter project, and Recommended PPU when importing.
//
// Full Guide:
// 1. In your scene, ensure the orthographic camera you intend to use in-game
//    (the "Target Camera") is set to its final orthographic size.
// 2. Drag your Spriter prefab into the scene.
// 3. Attach this script to the root GameObject of the instantiated prefab.
// 4. In the Inspector, set:
//      • Reference (Design) Resolution = your game's original design resolution.
//      • Target (Bake) Resolution      = the resolution you are targeting for the resize.
//      • Target Camera                 = your in-game orthographic camera.
//    Note: Target Resolution will often be the same as the Reference Resolution.
//    It represents the 'ideal' resolution. You can set it lower than the reference
//    resolution if you want to generate textures that will be scaled up for
//    higher in-game resolutions.  This is handy when targeting low-end devices.
// 5. Scale the prefab in the scene to match its intended final in-game size.
//    Only uniform scaling (same X and Y) is supported.
// 6. While the scene is running or in Edit mode, the script will display:
//      • Scaling Factor  = the ideal scaling factor to use when generating a
//                          resized Spriter project.
//      • Recommended PPU = the Pixels Per Unit value to use when importing
//                          the resized Spriter project.
// 7. Use the calculated Scaling Factor with either the 'Resize Spriter Project'
//    utility or Spriter's 'Save as resized project' feature.
// 8. Use the Recommended PPU when importing the .scml file generated in step 7.
// -----------------------------------------------------------------------------

[ExecuteAlways]
public class IdealScalingFactorCalculator : MonoBehaviour
{
    [Header("Reference (Design) Resolution")]
    [Tooltip("The resolution you originally designed/baked for.")]
    public int referenceScreenWidth = 3840;
    public int referenceScreenHeight = 2160;

    [Header("Target (Bake) Resolution")]
    [Tooltip("The resolution you now want to bake for.")]
    public int targetScreenWidth = 3840;
    public int targetScreenHeight = 2160;

    [Header("Camera")]
    [Tooltip("The orthographic camera used for display.")]
    public Camera targetCamera;

    [Header("Results (read-only)")]
    [Tooltip("The ideal scaling factor--that which produces images that are 'pixel-perfect' at the target resolution.  " +
        "Use this value when generating the resized Spriter project.")]
    public float scalingFactor;

    [Tooltip("Recommended PPU to use when importing the resized Spriter project.")]
    public float recommendedPPU;

    private SpriteRenderer _renderer;

    private void Awake()
    {
        _renderer = GetRenderer();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void OnValidate()
    {
        _renderer = GetRenderer();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        UpdateInfo();
    }

    private void Update()
    {
        UpdateInfo();
    }

    private SpriteRenderer GetRenderer()
    {
        // Find and return the sprite renderer that has the largest width or height dimension.

        float maxDimension = float.MinValue;
        SpriteRenderer result = null;

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(includeInactive: false))
        {
            if (sr.enabled && sr.sprite != null)
            {
                if (sr.sprite.rect.width > maxDimension)
                {
                    maxDimension = sr.sprite.rect.width;
                    result = sr;
                }

                if (sr.sprite.rect.height > maxDimension)
                {
                    maxDimension = sr.sprite.rect.height;
                    result = sr;
                }
            }
        }

        return result;
    }

    private void UpdateInfo()
    {
        if (_renderer == null ||
            targetCamera == null ||
            !targetCamera.orthographic ||
            _renderer.sprite == null)
        {
            scalingFactor = 0;
            recommendedPPU = 0;
            return;
        }

        // This sprite has the largest width OR height of all the visible sprite renderers.
        Sprite sprite = _renderer.sprite;

        float origPPU = sprite.pixelsPerUnit;
        float origPxWidth = sprite.rect.width;
        float origPxHeight = sprite.rect.height;
        float worldScaleX = _renderer.transform.lossyScale.x;
        float worldScaleY = _renderer.transform.lossyScale.y;

        // Pixels per world unit for reference and target resolutions
        float pxPerUnitRef = referenceScreenHeight / (2f * targetCamera.orthographicSize);
        float pxPerUnitTarget = targetScreenHeight / (2f * targetCamera.orthographicSize);

        // Use the larger of the two dimensions
        if (origPxHeight > origPxWidth)
        {
            // Actual on-screen pixel height at target resolution
            float actualPxHeight = origPxHeight / origPPU * worldScaleY * pxPerUnitTarget;

            // Scaling factor relative to original sprite pixel height
            scalingFactor = actualPxHeight / origPxHeight;
        }
        else
        {
            // Actual on-screen pixel width at target resolution
            float actualPxWidth = origPxWidth / origPPU * worldScaleX * pxPerUnitTarget;

            // Scaling factor relative to original sprite pixel width
            scalingFactor = actualPxWidth / origPxWidth;
        }

        // How much the pixel density changes between resolutions
        float ppuMultiplierFromReference = pxPerUnitTarget / pxPerUnitRef;

        // Recommended PPU for resized texture
        // If baking for target resolution directly from reference assets:
        recommendedPPU = origPPU * ppuMultiplierFromReference;
    }
}
