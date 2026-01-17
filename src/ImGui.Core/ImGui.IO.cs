using System;
using System.Runtime.CompilerServices;

namespace ImGui;

public struct ImGuiIO
{
    internal ImGuiContext? Context;

    // Settings
    public ImGuiConfigFlags_ ConfigFlags;
    public ImGuiBackendFlags_ BackendFlags;
    public ImVec2 DisplaySize;
    public float DeltaTime;
    public float IniSavingRate;
    public string? IniFilename;
    public string? LogFilename;
    public object? UserData;

    // Fonts
    public ImFontAtlas? Fonts;
    public ImFont? FontDefault;
    public bool FontAllowUserScaling;
#pragma warning disable CS0618
    public float FontGlobalScale;
#pragma warning restore CS0618
    public ImVec2 DisplayFramebufferScale;

    // Navigation options
    public bool ConfigNavSwapGamepadButtons;
    public bool ConfigNavMoveSetMousePos;
    public bool ConfigNavCaptureKeyboard;
    public bool ConfigNavEscapeClearFocusItem;
    public bool ConfigNavEscapeClearFocusWindow;
    public bool ConfigNavCursorVisibleAuto;
    public bool ConfigNavCursorVisibleAlways;

    // Misc options
    public bool MouseDrawCursor;
    public bool ConfigMacOSXBehaviors;
    public bool ConfigInputTrickleEventQueue;
    public bool ConfigInputTextCursorBlink;
    public bool ConfigInputTextEnterKeepActive;
    public bool ConfigDragClickToInputText;
    public bool ConfigWindowsResizeFromEdges;
    public bool ConfigWindowsMoveFromTitleBarOnly;
    public bool ConfigWindowsCopyContentsWithCtrlC;
    public bool ConfigScrollbarScrollByPage;
    public float ConfigMemoryCompactTimer;
    public bool ConfigDebugIsDebuggerPresent;
    public bool ConfigDebugHighlightIdConflicts;
    public bool ConfigDebugHighlightIdConflictsShowItemPicker;
    public bool ConfigDebugBeginReturnValueOnce;
    public bool ConfigDebugBeginReturnValueLoop;
    public bool ConfigErrorRecovery;
    public bool ConfigErrorRecoveryEnableAssert;
    public bool ConfigErrorRecoveryEnableDebugLog;
    public bool ConfigErrorRecoveryEnableTooltip;

    // Inputs behavior
    public float MouseDoubleClickTime;
    public float MouseDoubleClickMaxDist;
    public float MouseDragThreshold;
    public float KeyRepeatDelay;
    public float KeyRepeatRate;

    // Platform
    public string? BackendPlatformName;
    public string? BackendRendererName;
    public object? BackendPlatformUserData;
    public object? BackendRendererUserData;
    public object? BackendLanguageUserData;

    // Input state
    public ImVec2 MousePos;
    public ImVec2 MousePosPrev;
    public ImGuiMouseSource MouseSource;
    public float[] MouseDownDuration;
    public float[] MouseDownDurationPrev;
    public ImGuiKeyData[] KeysData;
    public bool AppAcceptingEvents;
    public ushort InputQueueSurrogate;

    public ImGuiIO()
    {
        // zero-init
        this = default;

        ConfigFlags = ImGuiConfigFlags_.ImGuiConfigFlags_None;
        BackendFlags = ImGuiBackendFlags_.ImGuiBackendFlags_None;
        DisplaySize = new ImVec2(-1.0f, -1.0f);
        DeltaTime = 1.0f / 60.0f;
        IniSavingRate = 5.0f;
        IniFilename = "imgui.ini";
        LogFilename = "imgui_log.txt";
        DisplayFramebufferScale = new ImVec2(1.0f, 1.0f);

        ConfigNavSwapGamepadButtons = false;
        ConfigNavMoveSetMousePos = false;
        ConfigNavCaptureKeyboard = true;
        ConfigNavEscapeClearFocusItem = true;
        ConfigNavEscapeClearFocusWindow = false;
        ConfigNavCursorVisibleAuto = true;
        ConfigNavCursorVisibleAlways = false;

        MouseDrawCursor = false;
        ConfigMacOSXBehaviors = false;
        ConfigInputTrickleEventQueue = true;
        ConfigInputTextCursorBlink = true;
        ConfigInputTextEnterKeepActive = false;
        ConfigDragClickToInputText = false;
        ConfigWindowsResizeFromEdges = true;
        ConfigWindowsMoveFromTitleBarOnly = false;
        ConfigWindowsCopyContentsWithCtrlC = false;
        ConfigScrollbarScrollByPage = true;
        ConfigMemoryCompactTimer = 60.0f;
        ConfigDebugIsDebuggerPresent = false;
        ConfigDebugHighlightIdConflicts = true;
        ConfigDebugHighlightIdConflictsShowItemPicker = true;
        ConfigDebugBeginReturnValueOnce = false;
        ConfigDebugBeginReturnValueLoop = false;
        ConfigErrorRecovery = true;
        ConfigErrorRecoveryEnableAssert = true;
        ConfigErrorRecoveryEnableDebugLog = true;
        ConfigErrorRecoveryEnableTooltip = true;

        MouseDoubleClickTime = 0.30f;
        MouseDoubleClickMaxDist = 6.0f;
        MouseDragThreshold = 6.0f;
        KeyRepeatDelay = 0.275f;
        KeyRepeatRate = 0.050f;

        MousePos = new ImVec2(-float.MaxValue, -float.MaxValue);
        MousePosPrev = new ImVec2(-float.MaxValue, -float.MaxValue);
        MouseSource = ImGuiMouseSource.ImGuiMouseSource_Mouse;

        int mouseCount = (int)ImGuiMouseButton_.ImGuiMouseButton_COUNT;
        MouseDownDuration = CreateAndFill(mouseCount, -1.0f);
        MouseDownDurationPrev = CreateAndFill(mouseCount, -1.0f);

        int keyCount = (int)ImGuiKey.ImGuiKey_NamedKey_COUNT;
        KeysData = new ImGuiKeyData[keyCount];
        for (int i = 0; i < keyCount; i++)
        {
            KeysData[i].DownDuration = -1.0f;
            KeysData[i].DownDurationPrev = -1.0f;
        }

        AppAcceptingEvents = true;
    }

    public void AddInputCharacter(uint c)
    {
        var ctx = Context;
        if (ctx == null || c == 0 || !AppAcceptingEvents)
            return;

        ImGuiInputEvent e = new()
        {
            Type = ImGuiInputEventType.Text,
            Source = ImGuiInputSource.Keyboard,
            EventId = ctx.InputEventsNextEventId++,
            Text = new ImGuiInputEventText { Char = c },
        };
        ctx.EnqueueInputEvent(e);
    }

    public void AddInputCharacterUTF16(ushort c)
    {
        if ((c == 0 && InputQueueSurrogate == 0) || !AppAcceptingEvents)
            return;

        if ((c & 0xFC00) == 0xD800) // high surrogate
        {
            if (InputQueueSurrogate != 0)
                AddInputCharacter(0xFFFD);
            InputQueueSurrogate = c;
            return;
        }

        uint cp = c;
        if (InputQueueSurrogate != 0)
        {
            if ((c & 0xFC00) != 0xDC00)
            {
                AddInputCharacter(0xFFFD);
                InputQueueSurrogate = 0;
                return;
            }
            cp = (uint)(((InputQueueSurrogate - 0xD800) << 10) + (c - 0xDC00) + 0x10000);
            InputQueueSurrogate = 0;
        }

        AddInputCharacter(cp);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float[] CreateAndFill(int count, float value)
    {
        var arr = new float[count];
        for (int i = 0; i < count; i++)
            arr[i] = value;
        return arr;
    }
}

public struct ImGuiKeyData
{
    public bool Down;
    public float DownDuration;
    public float DownDurationPrev;
}

// Placeholders for future font bindings.
public sealed class ImFontAtlas { }
public sealed class ImFont { }
