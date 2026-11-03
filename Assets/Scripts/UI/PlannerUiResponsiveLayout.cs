using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps generated planner panels inside the Canvas reference space when the Game view
/// or window size changes. Does not create additional Canvas or EventSystem objects.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlannerUiResponsiveLayout : MonoBehaviour
{
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private const float HeaderHeight = 88f;
    private const float SideMargin = 12f;
    private const float BottomMargin = 12f;
    private const float MinCatalogWidth = 300f;
    private const float MaxCatalogWidth = 360f;
    private const float PreferredCatalogFraction = 0.188f;
    private const float MinHelpWidth = 320f;
    private const float MaxHelpWidth = 460f;
    private const float HelpHeight = 220f;
    private const float MinCenterGap = 48f;

    private RectTransform canvasRect;
    private CanvasScaler scaler;
    private RectTransform header;
    private RectTransform catalogPanel;
    private RectTransform statusPanel;
    private RectTransform headerButtonRow;
    private float lastWidth = -1f;
    private float lastHeight = -1f;

    private void Awake()
    {
        Cache();
        ApplyLayout(true);
    }

    private void OnEnable()
    {
        Cache();
        ApplyLayout(true);
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyLayout(false);
    }

    private void LateUpdate()
    {
        if (canvasRect == null) return;
        Vector2 size = canvasRect.rect.size;
        if (!Mathf.Approximately(size.x, lastWidth) || !Mathf.Approximately(size.y, lastHeight))
            ApplyLayout(false);
    }

    private void Cache()
    {
        canvasRect = transform as RectTransform;
        scaler = GetComponent<CanvasScaler>();
        header = transform.Find("Title and Camera Modes") as RectTransform;
        catalogPanel = transform.Find("Furniture Catalog Panel") as RectTransform;
        statusPanel = transform.Find("Status and Controls") as RectTransform;
        if (header != null)
            headerButtonRow = header.Find("Header Button Row") as RectTransform;
    }

    public void ApplyLayout(bool force)
    {
        if (canvasRect == null) Cache();
        if (canvasRect == null) return;

        ConfigureScaler();
        Vector2 size = canvasRect.rect.size;
        if (!force && Mathf.Approximately(size.x, lastWidth) && Mathf.Approximately(size.y, lastHeight))
            return;
        lastWidth = size.x;
        lastHeight = size.y;

        float catalogWidth = Mathf.Clamp(size.x * PreferredCatalogFraction, MinCatalogWidth, MaxCatalogWidth);
        // Keep a usable centre viewport between help and catalog panels.
        float maxCatalog = Mathf.Max(MinCatalogWidth, size.x - MinHelpWidth - MinCenterGap - SideMargin * 3f);
        catalogWidth = Mathf.Min(catalogWidth, maxCatalog);

        if (header != null)
        {
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.sizeDelta = new Vector2(0f, HeaderHeight);
            header.anchoredPosition = Vector2.zero;
            LayoutHeaderButtons(size.x, catalogWidth);
        }

        if (catalogPanel != null)
        {
            catalogPanel.anchorMin = new Vector2(1f, 0f);
            catalogPanel.anchorMax = new Vector2(1f, 1f);
            catalogPanel.pivot = new Vector2(1f, 0.5f);
            catalogPanel.offsetMin = new Vector2(-(catalogWidth + SideMargin), BottomMargin);
            catalogPanel.offsetMax = new Vector2(-SideMargin, -HeaderHeight - 8f);
            EnsureCatalogScroll(catalogPanel);
        }

        if (statusPanel != null)
        {
            float available = Mathf.Max(240f, size.x - catalogWidth - SideMargin * 3f - MinCenterGap);
            float helpWidth = Mathf.Clamp(available, Mathf.Min(MinHelpWidth, available), MaxHelpWidth);
            statusPanel.anchorMin = new Vector2(0f, 0f);
            statusPanel.anchorMax = new Vector2(0f, 0f);
            statusPanel.pivot = new Vector2(0f, 0f);
            statusPanel.anchoredPosition = new Vector2(SideMargin + 4f, BottomMargin);
            statusPanel.sizeDelta = new Vector2(helpWidth, HelpHeight);
            LayoutHelpContent(statusPanel, helpWidth);
        }
    }

    private void ConfigureScaler()
    {
        if (scaler == null) scaler = GetComponent<CanvasScaler>();
        if (scaler == null) return;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }

    private void LayoutHeaderButtons(float canvasWidth, float catalogWidth)
    {
        if (header == null) return;
        if (headerButtonRow == null)
        {
            Transform existing = header.Find("Header Button Row");
            if (existing != null) headerButtonRow = existing as RectTransform;
        }
        if (headerButtonRow == null) return;

        float reservedLeft = 460f;
        float available = Mathf.Max(420f, canvasWidth - reservedLeft - SideMargin * 2f);
        headerButtonRow.anchorMin = new Vector2(1f, 0.5f);
        headerButtonRow.anchorMax = new Vector2(1f, 0.5f);
        headerButtonRow.pivot = new Vector2(1f, 0.5f);
        headerButtonRow.anchoredPosition = new Vector2(-SideMargin - 4f, 0f);
        headerButtonRow.sizeDelta = new Vector2(Mathf.Min(available, 980f), 52f);

        HorizontalLayoutGroup layout = headerButtonRow.GetComponent<HorizontalLayoutGroup>();
        if (layout != null)
        {
            bool compact = canvasWidth < 1600f;
            layout.spacing = compact ? 6f : 8f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = true;
        }

        float[] preferred = canvasWidth < 1500f
            ? new[] { 132f, 132f, 150f, 138f, 138f }
            : new[] { 150f, 150f, 170f, 150f, 150f };
        for (int i = 0; i < headerButtonRow.childCount && i < preferred.Length; i++)
        {
            LayoutElement element = headerButtonRow.GetChild(i).GetComponent<LayoutElement>();
            if (element == null) continue;
            element.preferredWidth = preferred[i];
            element.minWidth = preferred[i] * 0.85f;
            element.preferredHeight = 48f;
            element.flexibleWidth = 0f;
        }
    }

    private static void LayoutHelpContent(RectTransform statusPanel, float helpWidth)
    {
        if (statusPanel == null) return;
        string[] names = { "Save Layout Button", "Load Layout Button", "Reset Layout Button" };
        float gap = 8f;
        float inset = 16f;
        float usable = helpWidth - inset * 2f - gap * 2f;
        float buttonWidth = Mathf.Clamp(usable / 3f, 96f, 140f);
        float startX = inset;
        for (int i = 0; i < names.Length; i++)
        {
            Transform child = statusPanel.Find(names[i]);
            if (child == null) continue;
            RectTransform buttonRect = child as RectTransform;
            buttonRect.anchorMin = Vector2.zero;
            buttonRect.anchorMax = Vector2.zero;
            buttonRect.pivot = Vector2.zero;
            buttonRect.anchoredPosition = new Vector2(startX + i * (buttonWidth + gap), 12f);
            buttonRect.sizeDelta = new Vector2(buttonWidth, 40f);
        }

        Transform statusTransform = statusPanel.Find("Status Message");
        if (statusTransform != null)
        {
            Text status = statusTransform.GetComponent<Text>();
            if (status != null)
            {
                status.horizontalOverflow = HorizontalWrapMode.Wrap;
                status.verticalOverflow = VerticalWrapMode.Truncate;
            }

            RectTransform statusRect = statusTransform as RectTransform;
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.offsetMin = new Vector2(16f, -48f);
            statusRect.offsetMax = new Vector2(-16f, -12f);
        }

        Transform helpTransform = statusPanel.Find("Control Help");
        if (helpTransform != null)
        {
            Text help = helpTransform.GetComponent<Text>();
            if (help != null)
            {
                help.horizontalOverflow = HorizontalWrapMode.Wrap;
                help.verticalOverflow = VerticalWrapMode.Truncate;
            }

            RectTransform helpRect = helpTransform as RectTransform;
            helpRect.anchorMin = new Vector2(0f, 1f);
            helpRect.anchorMax = new Vector2(1f, 1f);
            helpRect.pivot = new Vector2(0.5f, 1f);
            helpRect.offsetMin = new Vector2(16f, -148f);
            helpRect.offsetMax = new Vector2(-16f, -52f);
        }
    }

    private static void EnsureCatalogScroll(RectTransform catalogPanel)
    {
        ScrollRect scroll = catalogPanel.GetComponent<ScrollRect>();
        if (scroll == null) return;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 28f;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        if (scroll.viewport == null)
        {
            Transform viewport = catalogPanel.Find("Catalog Viewport");
            if (viewport != null) scroll.viewport = viewport as RectTransform;
        }
        if (scroll.content == null)
        {
            Transform content = catalogPanel.Find("Catalog Viewport/Catalog Content");
            if (content != null) scroll.content = content as RectTransform;
        }
    }
}
