using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles communication with the backend LLM proxy server and drives the agent loop.
/// Dispatches tool calls to AICommandExecutor and feeds validation results back to the LLM.
/// </summary>
public sealed class AIChatService : MonoBehaviour
{
    // Use 5055 — macOS AirPlay / Control Center often owns port 5000 and returns 403.
    [SerializeField] private string backendUrl = "http://127.0.0.1:5055/chat";
    [SerializeField] private int maxRetries = 3;
    [SerializeField] private float requestTimeout = 60f;

    private PlacementSystem placementSystem;
    private AuditoriumDemoSettings settings;
    private AICommandExecutor executor;
    private AISceneContext sceneContext;

    public event Action<string, bool> MessageReceived; // (message, isUser)
    public event Action<bool> ProcessingChanged;       // (isProcessing)

    private bool isProcessing;
    private readonly JArray conversationHistory = new JArray();

    public bool IsProcessing => isProcessing;

    public void Configure(PlacementSystem placement, AuditoriumDemoSettings demoSettings,
        AICommandExecutor commandExecutor, AISceneContext context)
    {
        placementSystem = placement;
        settings = demoSettings;
        executor = commandExecutor;
        sceneContext = context;
    }

    public void SetBackendUrl(string url)
    {
        if (!string.IsNullOrEmpty(url))
            backendUrl = url;
    }

    public void SendUserMessage(string userText)
    {
        if (isProcessing || string.IsNullOrWhiteSpace(userText))
            return;

        MessageReceived?.Invoke(userText, true);

        JObject userMsg = new JObject
        {
            ["role"] = "user",
            ["content"] = userText.Trim()
        };
        conversationHistory.Add(userMsg);

        StartCoroutine(ProcessConversation());
    }

    public void ClearHistory()
    {
        conversationHistory.Clear();
    }

    private IEnumerator ProcessConversation()
    {
        isProcessing = true;
        ProcessingChanged?.Invoke(true);

        int retries = 0;
        int toolIterations = 0;
        const int MaxToolIterations = 5;
        bool done = false;

        while (!done && retries <= maxRetries)
        {
            string contextJson = sceneContext != null ? sceneContext.BuildContextJson() : "{}";
            string systemPrompt = AIToolDefinitions.GetSystemPrompt(contextJson);

            // Construct full message list starting with dynamic system prompt
            JArray messages = new JArray();
            messages.Add(new JObject
            {
                ["role"] = "system",
                ["content"] = systemPrompt
            });

            foreach (JToken msg in conversationHistory)
            {
                messages.Add(msg.DeepClone());
            }

            // Construct tools array
            JArray tools = new JArray();
            try
            {
                tools.Add(JObject.Parse(AIToolDefinitions.GetPlaceGridToolJson()));
            }
            catch (Exception ex)
            {
                Debug.LogError("AIChatService: Failed to parse tool definitions: " + ex.Message);
            }

            JObject requestPayload = new JObject
            {
                ["messages"] = messages,
                ["tools"] = tools
            };

            string requestJson = requestPayload.ToString(Formatting.None);

            using (UnityWebRequest request = new UnityWebRequest(backendUrl, "POST"))
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(requestJson);
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = (int)requestTimeout;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string errorMsg = "Backend connection failed: " + request.error +
                        "\n(Ensure backend proxy is running at " + backendUrl + ")";
                    MessageReceived?.Invoke(errorMsg, false);
                    done = true;
                    continue;
                }

                string responseText = request.downloadHandler.text;
                JObject responseObj;
                try
                {
                    responseObj = JObject.Parse(responseText);
                }
                catch (Exception ex)
                {
                    MessageReceived?.Invoke("Failed to parse backend response: " + ex.Message, false);
                    done = true;
                    continue;
                }

                JToken choices = responseObj["choices"];
                if (choices == null || !choices.HasValues)
                {
                    JToken errorToken = responseObj["error"];
                    string errMsg = errorToken != null
                        ? errorToken.ToString()
                        : "No choices returned by the model.";
                    MessageReceived?.Invoke("Model error: " + errMsg, false);
                    done = true;
                    continue;
                }

                JToken messageToken = choices[0]["message"];
                if (messageToken == null)
                {
                    MessageReceived?.Invoke("Invalid response structure from backend.", false);
                    done = true;
                    continue;
                }

                JArray toolCalls = messageToken["tool_calls"] as JArray;
                string content = messageToken.Value<string>("content");

                if (toolCalls != null && toolCalls.Count > 0)
                {
                    // Record assistant message with tool calls in history
                    JObject assistantMsg = new JObject
                    {
                        ["role"] = "assistant",
                        ["content"] = content,
                        ["tool_calls"] = toolCalls.DeepClone()
                    };
                    conversationHistory.Add(assistantMsg);

                    bool anyError = false;
                    string lastError = string.Empty;

                    foreach (JToken call in toolCalls)
                    {
                        string callId = call.Value<string>("id");
                        string toolName = call["function"]?.Value<string>("name");
                        string argsJson = call["function"]?.Value<string>("arguments");

                        JObject argsObj = null;
                        try
                        {
                            if (!string.IsNullOrEmpty(argsJson))
                                argsObj = JObject.Parse(argsJson);
                        }
                        catch (Exception parseEx)
                        {
                            Debug.LogWarning("AIChatService: Could not parse tool arguments JSON: " + parseEx.Message);
                        }

                        if (argsObj == null)
                            argsObj = new JObject();

                        var (success, resultMsg, _) = executor != null
                            ? executor.Execute(toolName, argsObj)
                            : (false, "AICommandExecutor is not available.", null);

                        JObject toolResponseData = new JObject
                        {
                            ["success"] = success,
                            ["message"] = resultMsg
                        };

                        if (!success)
                        {
                            anyError = true;
                            lastError = resultMsg;
                        }

                        JObject toolMsg = new JObject
                        {
                            ["role"] = "tool",
                            ["tool_call_id"] = callId,
                            ["content"] = toolResponseData.ToString(Formatting.None)
                        };
                        conversationHistory.Add(toolMsg);
                    }

                    if (anyError)
                    {
                        retries++;
                        if (retries > maxRetries)
                        {
                            MessageReceived?.Invoke(
                                "Placement could not be resolved after " + maxRetries.ToString(CultureInfo.InvariantCulture) +
                                " attempts: " + lastError, false);
                            done = true;
                        }
                        // Loop continues to let model retry with error feedback
                    }
                    else
                    {
                        // Tools succeeded; increment iteration guard and let model confirm
                        toolIterations++;
                        if (toolIterations >= MaxToolIterations)
                        {
                            MessageReceived?.Invoke("All requested furniture placements have been applied.", false);
                            done = true;
                        }
                        else
                        {
                            retries = 0;
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(content))
                {
                    // Final text response
                    JObject assistantMsg = new JObject
                    {
                        ["role"] = "assistant",
                        ["content"] = content
                    };
                    conversationHistory.Add(assistantMsg);

                    MessageReceived?.Invoke(content, false);
                    done = true;
                }
                else
                {
                    MessageReceived?.Invoke("Received empty response from assistant.", false);
                    done = true;
                }
            }
        }

        isProcessing = false;
        ProcessingChanged?.Invoke(false);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (isProcessing)
        {
            isProcessing = false;
            ProcessingChanged?.Invoke(false);
        }
    }
}
