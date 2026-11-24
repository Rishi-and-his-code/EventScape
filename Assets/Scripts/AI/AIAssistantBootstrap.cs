using UnityEngine;

/// <summary>
/// Bootstraps the AI Assistant into the EventScape scene.
/// Automatically detects scene components (PlacementSystem, AuditoriumDemoSettings, Interface Canvas)
/// and initializes the AI Chat Service, AI Command Executor, and AI Chat Panel UI.
/// </summary>
public sealed class AIAssistantBootstrap : MonoBehaviour
{
    // Use 5055 — macOS AirPlay / Control Center often owns port 5000 and returns 403.
    [SerializeField] private string backendUrl = "http://127.0.0.1:5055/chat";

    private AIChatService chatService;
    private AIChatPanel chatPanel;
    private AICommandExecutor commandExecutor;
    private AISceneContext sceneContext;
    private bool isInitialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        EnsureInScene();
    }

    public static AIAssistantBootstrap EnsureInScene(string serverUrl = null)
    {
        AIAssistantBootstrap existing = FindFirstObjectByType<AIAssistantBootstrap>();
        if (existing != null)
            return existing;

        GameObject root = new GameObject("AI Assistant Bootstrap");
        AIAssistantBootstrap bootstrap = root.AddComponent<AIAssistantBootstrap>();
        if (!string.IsNullOrEmpty(serverUrl))
            bootstrap.backendUrl = serverUrl;
        return bootstrap;
    }

    private void Start()
    {
        TryInitialize();
    }

    private void Update()
    {
        if (!isInitialized)
        {
            TryInitialize();
        }
    }

    public bool TryInitialize()
    {
        if (isInitialized)
            return true;

        PlacementSystem placement = FindFirstObjectByType<PlacementSystem>();
        if (placement == null)
            return false;

        AuditoriumDemoSettings settings = placement.Settings;
        if (settings == null)
        {
            settings = Resources.Load<AuditoriumDemoSettings>("AuditoriumDemoSettings");
        }

        GameObject interfaceObj = GameObject.Find("Interface");
        if (interfaceObj == null)
            return false;

        Canvas canvas = interfaceObj.GetComponent<Canvas>();
        if (canvas == null)
            return false;

        // Create AI sub-components
        GameObject serviceObj = new GameObject("AI Services");
        serviceObj.transform.SetParent(transform, false);

        commandExecutor = serviceObj.AddComponent<AICommandExecutor>();
        commandExecutor.Configure(placement);

        sceneContext = serviceObj.AddComponent<AISceneContext>();
        sceneContext.Configure(settings, placement);

        chatService = serviceObj.AddComponent<AIChatService>();
        chatService.Configure(placement, settings, commandExecutor, sceneContext);
        chatService.SetBackendUrl(backendUrl);

        chatPanel = serviceObj.AddComponent<AIChatPanel>();
        chatPanel.BuildUI(canvas, chatService);

        isInitialized = true;
        Debug.Log("[AIAssistantBootstrap] AI Assistant successfully initialized! Backend target: " + backendUrl);
        return true;
    }
}
