using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Game View Scale above 1× magnifies and crops the framebuffer, which looks like
/// clipped side panels even when Canvas layout is correct. Reset to 1× on Play / rebuild.
/// </summary>
[InitializeOnLoad]
internal static class GameViewScaleGuard
{
    private const float MaxAllowedScale = 1.01f;

    static GameViewScaleGuard()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode ||
            state == PlayModeStateChange.ExitingEditMode)
            EnsureGameViewScaleOne("Play Mode");
    }

    [MenuItem("Tools/Virtual Auditorium/Reset Game View Scale To 1x")]
    public static void ResetFromMenu()
    {
        EnsureGameViewScaleOne("menu command");
    }

    public static void EnsureGameViewScaleOne(string reason)
    {
        try
        {
            Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null) return;

            EditorWindow[] windows = Resources.FindObjectsOfTypeAll(gameViewType) as EditorWindow[];
            if (windows == null || windows.Length == 0) return;

            for (int i = 0; i < windows.Length; i++)
            {
                EditorWindow window = windows[i];
                if (window == null) continue;

                float scale = ReadScale(gameViewType, window);
                if (scale <= MaxAllowedScale) continue;

                if (TryWriteScale(gameViewType, window, 1f))
                {
                    window.Repaint();
                    Debug.Log(
                        "[Virtual Auditorium] Game View Scale was " + scale.ToString("0.##") +
                        "× (crops side UI). Reset to 1× for " + reason +
                        ". Use Tools > Virtual Auditorium > Reset Game View Scale To 1x anytime.");
                }
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[Virtual Auditorium] Could not reset Game View Scale: " + exception.Message);
        }
    }

    private static float ReadScale(Type gameViewType, EditorWindow window)
    {
        PropertyInfo scaleProperty = gameViewType.GetProperty("scale",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (scaleProperty != null && scaleProperty.PropertyType == typeof(float))
            return (float)scaleProperty.GetValue(window, null);

        FieldInfo scaleField = gameViewType.GetField("m_Scale",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (scaleField != null && scaleField.FieldType == typeof(float))
            return (float)scaleField.GetValue(window);

        object zoomArea = GetZoomArea(gameViewType, window);
        if (zoomArea != null)
        {
            FieldInfo scaleInZoom = zoomArea.GetType().GetField("m_Scale",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (scaleInZoom != null && scaleInZoom.FieldType == typeof(Vector2))
                return ((Vector2)scaleInZoom.GetValue(zoomArea)).x;
        }

        return 1f;
    }

    private static bool TryWriteScale(Type gameViewType, EditorWindow window, float scale)
    {
        PropertyInfo scaleProperty = gameViewType.GetProperty("scale",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (scaleProperty != null && scaleProperty.CanWrite && scaleProperty.PropertyType == typeof(float))
        {
            scaleProperty.SetValue(window, scale, null);
            return true;
        }

        MethodInfo setScale = gameViewType.GetMethod("SetScale",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new[] { typeof(float) }, null);
        if (setScale != null)
        {
            setScale.Invoke(window, new object[] { scale });
            return true;
        }

        FieldInfo scaleField = gameViewType.GetField("m_Scale",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (scaleField != null && scaleField.FieldType == typeof(float))
        {
            scaleField.SetValue(window, scale);
            return true;
        }

        object zoomArea = GetZoomArea(gameViewType, window);
        if (zoomArea != null)
        {
            FieldInfo scaleInZoom = zoomArea.GetType().GetField("m_Scale",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (scaleInZoom != null && scaleInZoom.FieldType == typeof(Vector2))
            {
                scaleInZoom.SetValue(zoomArea, new Vector2(scale, scale));
                return true;
            }
        }

        return false;
    }

    private static object GetZoomArea(Type gameViewType, EditorWindow window)
    {
        FieldInfo zoomField = gameViewType.GetField("m_ZoomArea",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return zoomField != null ? zoomField.GetValue(window) : null;
    }
}
