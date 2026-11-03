using UnityEngine;

/// <summary>
/// Editable, approximate demo dimensions for the procedural auditorium.
/// Room and stage frame values are project-configured; decorative sizes are visual estimates.
/// </summary>
[CreateAssetMenu(fileName = "AuditoriumDemoSettings", menuName = "Virtual Auditorium/Demo Settings")]
public sealed class AuditoriumDemoSettings : ScriptableObject
{
    [Header("Interior demo dimensions (metres; replace with verified venue measurements)")]
    [Min(1f)] public float hallWidth = 16f;
    [Min(1f)] public float hallDepth = 25f;
    [Min(2f)] public float wallHeight = 3.65f;
    [Min(0.05f)] public float wallThickness = 0.15f;

    [Header("Entrance (rear wall, right of logo when facing −Z)")]
    [Min(0.8f)] public float doorwayWidth = 1.9f;
    [Min(1f)] public float doorwayHeight = 2.35f;
    [Tooltip("Door centre offset from the −X interior corner toward +X (metres). Photo 01: right of the Navrachana logo.")]
    [Min(0.6f)] public float doorwayInsetFromRight = 1.55f;

    [Header("First-person walkthrough spawn")]
    [Tooltip("Approximate spawn offset from room centre on X (metres). Negative faces the rear door (−X).")]
    public float walkthroughSpawnX = -5.0f;
    [Tooltip("Spawn depth as a fraction of hallDepth from the rear entrance; the player faces the stage.")]
    [Range(0.1f, 0.9f)] public float walkthroughSpawnDepthFraction = 0.36f;

    [Header("Stage")]
    [Min(1f)] public float stageWidth = 9.5f;
    [Min(1f)] public float stageDepth = 3.7f;
    [Min(0.1f)] public float stageHeight = 0.45f;
    [Min(0f)] public float stageBackClearance = 0f;

    [Header("Event layout template spacing (metres; design targets only)")]
    [Min(0f)] public float templateSideAisleClearance = 0.85f;
    [Min(0f)] public float templateCentralAisleWidth = 1.2f;
    [Min(0f)] public float templateFrontClearance = 1.1f;
    [Min(0f)] public float templateRearClearance = 1.0f;
    [Range(4, 10)] public int theatreTemplateRows = 8;
    [Range(3, 6)] public int classroomTemplateRows = 5;
    [Range(4, 6)] public int roundtableTemplateCount = 4;

    [Header("Navrachana architectural appearance (visual estimates)")]
    [Tooltip("Lower wood wall cladding height as a fraction of wall height (photos ~40–45%).")]
    [Range(0.35f, 0.50f)] public float sideCladdingHeightFraction = 0.42f;
    [Tooltip("Absolute cladding height override used when > 0; otherwise fraction × wallHeight.")]
    [Range(0f, 2.2f)] public float sideCladdingHeight = 0f;
    [Min(0.4f)] public float floorTileSize = 0.70f;
    [Range(0.001f, 0.01f)] public float floorGroutWidth = 0.003f;
    [Min(0.15f)] public float pilasterWidth = 0.30f;
    [Min(2f)] public float sideWallBaySpacing = 3.40f;
    [Min(0.8f)] public float rollerBlindWidth = 2.05f;
    [Min(0.6f)] public float rollerBlindHeight = 1.70f;
    [Min(0.35f)] public float wallFanDiameter = 0.62f;
    [Min(1.5f)] public float wallFanMountHeight = 2.62f;
    [Tooltip("Square stage poster width/height on each walnut flank (metres).")]
    [Min(0.8f)] public float stageBannerWidth = 1.55f;
    [Min(0.8f)] public float stageBannerHeight = 1.55f;
    [Range(2700f, 3200f)] public float warmLightKelvin = 3000f;
    [Min(0.2f)] public float downlightIntensity = 1.55f;
    [Min(0.2f)] public float stageSpotIntensity = 2.6f;
    [Min(0.1f)] public float rearLogoWashIntensity = 1.75f;
    [Tooltip("Uniform medium grey stone; avoid charcoal values that read almost black under warm light.")]
    public Color floorBaseColor = new Color(0.54f, 0.55f, 0.56f, 1f);
    [Tooltip("Show the separate, fixed reference seating arrangement when the scene is generated.")]
    public bool showReferenceSeating;
    [Range(3, 7)] public int referenceSeatingRows = 5;
    [Range(0.5f, 2.0f)] public float referenceSeatingRowSpacing = 1.2f;
    [Tooltip("Optional photo crop of the real rear-wall Navrachana identity. Replace with a clean transparent logo when available.")]
    public Texture2D rearLogoArtwork;
    [Tooltip("Optional shared replacement artwork for both stage banners.")]
    public Texture2D stageBannerArtwork;
    [Tooltip("Optional left-stage banner artwork extracted from the stage-front photograph.")]
    public Texture2D stageBannerLeftArtwork;
    [Tooltip("Optional right-stage banner artwork extracted from the stage-front photograph.")]
    public Texture2D stageBannerRightArtwork;

    /// <summary>Resolved lower cladding height in metres.</summary>
    public float ResolvedCladdingHeight
    {
        get
        {
            if (sideCladdingHeight > 0.05f)
                return Mathf.Clamp(sideCladdingHeight, wallHeight * 0.35f, wallHeight * 0.50f);
            return Mathf.Clamp(wallHeight * sideCladdingHeightFraction, wallHeight * 0.35f, wallHeight * 0.50f);
        }
    }

    /// <summary>
    /// Position of the stage center along the auditorium depth axis.
    /// The entrance is at negative Z and the stage is at positive Z.
    /// </summary>
    public float StageCenterZ
    {
        get { return hallDepth * 0.5f - stageDepth * 0.5f - stageBackClearance; }
    }

    /// <summary>Approximate warm-white colour for the configured Kelvin target.</summary>
    public Color WarmLightColor
    {
        get { return KelvinToRgb(warmLightKelvin); }
    }

    private static Color KelvinToRgb(float kelvin)
    {
        float t = Mathf.Clamp(kelvin, 2700f, 3200f) / 100f;
        float r = 1f;
        float g = Mathf.Clamp01((99.4708025861f * Mathf.Log(t) - 161.1195681661f) / 255f);
        float b = Mathf.Clamp01((138.5177312231f * Mathf.Log(t - 10f) - 305.0447927307f) / 255f);
        return new Color(r, Mathf.Lerp(0.82f, g, 0.85f), Mathf.Lerp(0.55f, b, 0.9f), 1f);
    }
}
