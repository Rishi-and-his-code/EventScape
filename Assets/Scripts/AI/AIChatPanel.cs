using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Provides a collapsible chat sidebar within the UGUI planning interface.
/// Integrates with AIChatService to display conversation bubbles and dispatch user input.
/// </summary>
public sealed class AIChatPanel : MonoBehaviour
{
    private AIChatService chatService;
    private ScrollRect messageScrollRect;
    private RectTransform messageContent;
    private InputField inputField;
    private Button sendButton;
    private Button clearButton;
    private Button toggleButton;
    private GameObject panelRoot;
    private Text statusLabel;
    private readonly List<GameObject> messageObjects = new List<GameObject>();

    // Visual theme aligned with PlannerUIBuilder / PlannerControlsPanel
    private static readonly Color PanelBackground = new Color(0.045f, 0.065f, 0.085f, 0.96f);
    private static readonly Color HeaderBackground = new Color(0.08f, 0.11f, 0.15f, 1f);
    private static readonly Color UserBubbleColor = new Color(0.22f, 0.38f, 0.36f, 1f);
    private static readonly Color AssistantBubbleColor = new Color(0.12f, 0.16f, 0.22f, 1f);
    private static readonly Color ButtonIdle = new Color(0.12f, 0.17f, 0.22f, 1f);
    private static readonly Color ButtonActive = new Color(0.78f, 0.58f, 0.32f, 1f);
    private static readonly Color TextColor = new Color(0.94f, 0.96f, 0.97f, 1f);
    private static readonly Color MutedTextColor = new Color(0.72f, 0.78f, 0.82f, 1f);

    public void BuildUI(Canvas parentCanvas, AIChatService service)
    {
        chatService = service;
        chatService.MessageReceived += OnMessage;
        chatService.ProcessingChanged += OnProcessingChanged;

        // Main panel container — docked to right side
        panelRoot = CreatePanel(parentCanvas.transform, "AI Chat Panel",
            new Vector2(340f, 0f),
            new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(-170f, 0f),
            PanelBackground);
        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(340f, 0f);
        panelRect.anchoredPosition = new Vector2(-170f, 0f);

        // Header / Title bar
        GameObject titleBar = CreatePanel(panelRoot.transform, "Title Bar",
            new Vector2(0f, 44f),
            new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -22f),
            HeaderBackground);
        RectTransform titleRect = titleBar.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(0f, 44f);

        Text titleText = CreateText(titleBar.transform, "AI Layout Assistant", 14, TextAnchor.MiddleCenter);
        RectTransform titleTextRect = titleText.GetComponent<RectTransform>();
        titleTextRect.anchorMin = Vector2.zero;
        titleTextRect.anchorMax = Vector2.one;
        titleTextRect.sizeDelta = Vector2.zero;
        titleText.color = TextColor;

        // Message scroll container
        GameObject scrollArea = new GameObject("Message Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollArea.transform.SetParent(panelRoot.transform, false);
        RectTransform scrollRect = scrollArea.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(10f, 96f);
        scrollRect.offsetMax = new Vector2(-10f, -50f);
        scrollArea.GetComponent<Image>().color = new Color(0.03f, 0.04f, 0.06f, 0.85f);

        // Content container
        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(scrollArea.transform, false);
        messageContent = content.GetComponent<RectTransform>();
        messageContent.anchorMin = new Vector2(0f, 1f);
        messageContent.anchorMax = new Vector2(1f, 1f);
        messageContent.pivot = new Vector2(0.5f, 1f);
        messageContent.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;

        ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        messageScrollRect = scrollArea.GetComponent<ScrollRect>();
        messageScrollRect.content = messageContent;
        messageScrollRect.horizontal = false;
        messageScrollRect.vertical = true;
        messageScrollRect.movementType = ScrollRect.MovementType.Clamped;

        scrollArea.AddComponent<RectMask2D>();

        // Input section at bottom
        GameObject inputArea = CreatePanel(panelRoot.transform, "Input Area",
            new Vector2(0f, 88f),
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 44f),
            HeaderBackground);
        RectTransform inputAreaRect = inputArea.GetComponent<RectTransform>();
        inputAreaRect.sizeDelta = new Vector2(0f, 88f);

        // Input Field
        GameObject inputObj = new GameObject("Input Field", typeof(RectTransform), typeof(Image), typeof(InputField));
        inputObj.transform.SetParent(inputArea.transform, false);
        RectTransform inputRect = inputObj.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0f, 0.5f);
        inputRect.anchorMax = new Vector2(1f, 0.5f);
        inputRect.sizeDelta = new Vector2(-84f, 40f);
        inputRect.anchoredPosition = new Vector2(-36f, 12f);
        inputObj.GetComponent<Image>().color = new Color(0.10f, 0.14f, 0.18f, 1f);
        inputField = inputObj.GetComponent<InputField>();

        // Text inside Input Field
        GameObject inputTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        inputTextObj.transform.SetParent(inputObj.transform, false);
        RectTransform inputTextRect = inputTextObj.GetComponent<RectTransform>();
        inputTextRect.anchorMin = Vector2.zero;
        inputTextRect.anchorMax = Vector2.one;
        inputTextRect.offsetMin = new Vector2(8f, 2f);
        inputTextRect.offsetMax = new Vector2(-8f, -2f);
        Text inputText = inputTextObj.GetComponent<Text>();
        inputText.font = GetSafeFont();
        inputText.fontSize = 13;
        inputText.color = TextColor;
        inputField.textComponent = inputText;

        // Placeholder Text
        GameObject placeholderObj = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
        placeholderObj.transform.SetParent(inputObj.transform, false);
        RectTransform phRect = placeholderObj.GetComponent<RectTransform>();
        phRect.anchorMin = Vector2.zero;
        phRect.anchorMax = Vector2.one;
        phRect.offsetMin = new Vector2(8f, 2f);
        phRect.offsetMax = new Vector2(-8f, -2f);
        Text phText = placeholderObj.GetComponent<Text>();
        phText.font = GetSafeFont();
        phText.fontSize = 13;
        phText.color = MutedTextColor;
        phText.text = "Describe layout (e.g. 5 rows chairs)...";
        phText.fontStyle = FontStyle.Italic;
        inputField.placeholder = phText;

        // Send Button
        sendButton = CreateButton(inputArea.transform, "Send", "Send",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-32f, 12f), new Vector2(56f, 40f), ButtonActive);
        sendButton.onClick.AddListener(OnSendClicked);

        // Clear Chat Button
        clearButton = CreateButton(inputArea.transform, "Clear", "Clear Chat",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 14f), new Vector2(90f, 22f), ButtonIdle);
        clearButton.onClick.AddListener(OnClearClicked);

        // Status Label (e.g. "Thinking...")
        statusLabel = CreateText(inputArea.transform, "", 11, TextAnchor.MiddleCenter);
        RectTransform statusRect = statusLabel.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(0.35f, 0f);
        statusRect.sizeDelta = new Vector2(0f, 22f);
        statusRect.anchoredPosition = new Vector2(40f, 14f);
        statusLabel.color = ButtonActive;

        // Screen Edge Toggle Button ("AI")
        toggleButton = CreateButton(parentCanvas.transform, "Toggle AI", "AI",
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-18f, 0f), new Vector2(36f, 72f), ButtonActive);
        toggleButton.onClick.AddListener(TogglePanel);

        inputField.onEndEdit.AddListener(OnInputSubmit);

        // Start hidden
        panelRoot.SetActive(false);

        // Initial welcome message
        AddMessage("Hello! I am your AI Event Layout Assistant.\n\nDescribe the desired layout (e.g. \"Place 6 rows of 8 auditorium chairs facing the stage\") and I will automatically calculate and place them.", false);
    }

    private void OnSendClicked()
    {
        string text = inputField.text.Trim();
        if (string.IsNullOrEmpty(text))
            return;

        inputField.text = string.Empty;
        chatService.SendUserMessage(text);
        inputField.ActivateInputField();
    }

    private void OnInputSubmit(string text)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
        {
            OnSendClicked();
        }
    }

    private void OnClearClicked()
    {
        foreach (GameObject obj in messageObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        messageObjects.Clear();
        chatService.ClearHistory();
        AddMessage("Chat history cleared. What layout would you like to build next?", false);
    }

    private void TogglePanel()
    {
        bool nextState = !panelRoot.activeSelf;
        panelRoot.SetActive(nextState);
        Text label = toggleButton.GetComponentInChildren<Text>();
        if (label != null)
            label.text = nextState ? "X" : "AI";
    }

    private void OnMessage(string text, bool isUser)
    {
        AddMessage(text, isUser);
    }

    private void OnProcessingChanged(bool processing)
    {
        if (sendButton != null)
            sendButton.interactable = !processing;
        if (inputField != null)
            inputField.interactable = !processing;
        if (statusLabel != null)
            statusLabel.text = processing ? "Thinking..." : string.Empty;
    }

    private void AddMessage(string text, bool isUser)
    {
        if (messageContent == null)
            return;

        GameObject bubble = new GameObject(isUser ? "User Msg" : "AI Msg",
            typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        bubble.transform.SetParent(messageContent, false);

        Image bg = bubble.GetComponent<Image>();
        bg.color = isUser ? UserBubbleColor : AssistantBubbleColor;

        VerticalLayoutGroup bvlg = bubble.GetComponent<VerticalLayoutGroup>();
        bvlg.padding = new RectOffset(10, 10, 6, 6);
        bvlg.childForceExpandWidth = true;
        bvlg.childForceExpandHeight = false;
        bvlg.childControlHeight = true;
        bvlg.childControlWidth = true;

        Text msgText = CreateText(bubble.transform, text, 12,
            isUser ? TextAnchor.UpperRight : TextAnchor.UpperLeft);
        msgText.color = TextColor;

        LayoutRebuilder.ForceRebuildLayoutImmediate(messageContent);
        Canvas.ForceUpdateCanvases();
        if (messageScrollRect != null)
            messageScrollRect.verticalNormalizedPosition = 0f;

        messageObjects.Add(bubble);
    }

    private void OnDestroy()
    {
        if (chatService != null)
        {
            chatService.MessageReceived -= OnMessage;
            chatService.ProcessingChanged -= OnProcessingChanged;
        }
    }

    private static Font GetSafeFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Color color)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private static Button CreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
        Image img = btnObj.GetComponent<Image>();
        img.color = color;
        Button btn = btnObj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = color;
        cb.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
        cb.pressedColor = Color.Lerp(color, Color.black, 0.20f);
        btn.colors = cb;

        Text txt = CreateText(btnObj.transform, label, 12, TextAnchor.MiddleCenter);
        RectTransform txtRt = txt.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.sizeDelta = Vector2.zero;
        txt.color = Color.white;

        return btn;
    }

    private static Text CreateText(Transform parent, string content, int fontSize, TextAnchor alignment)
    {
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(parent, false);
        Text text = textObj.GetComponent<Text>();
        text.font = GetSafeFont();
        text.fontSize = fontSize;
        text.color = TextColor;
        text.alignment = alignment;
        text.text = content;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
