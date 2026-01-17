using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using StbTrueTypeSharp;

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
    public Func<string>? GetClipboardTextFn;
    public Action<string>? SetClipboardTextFn;
    public IntPtr ImeWindowHandle;
    public Action<int, int>? SetPlatformImeDataFn;

    // Input state
    public ImVec2 MousePos;
    public ImVec2 MousePosPrev;
    public ImGuiMouseSource MouseSource;
    public float[] MouseDownDuration;
    public float[] MouseDownDurationPrev;
    public bool[] MouseClicked;
    public bool[] MouseReleased;
    public ImGuiKeyData[] KeysData;
    public bool AppAcceptingEvents;
    public ushort InputQueueSurrogate;
    public bool[] MouseDown;
    public float MouseWheel;
    public float MouseWheelH;
    public bool KeyCtrl;
    public bool KeyShift;
    public bool KeyAlt;
    public bool KeySuper;
    public int KeyMods;
    public bool WantCaptureMouse;
    public bool WantCaptureKeyboard;
    public bool WantTextInput;
    public bool WantSetMousePos;
    public bool WantSaveIniSettings;
    public bool NavActive;
    public bool NavVisible;
    public float Framerate;
    public int MetricsRenderVertices;
    public int MetricsRenderIndices;
    public int MetricsRenderWindows;
    public int MetricsActiveWindows;
    public ImVec2 MouseDelta;
    public bool AppFocusLost;
    public bool WantCaptureMouseUnlessPopupClose;
    public List<uint> InputQueueCharacters;

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
        FontGlobalScale = 1.0f;
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
        MouseClicked = new bool[mouseCount];
        MouseReleased = new bool[mouseCount];
        MouseDown = new bool[mouseCount];

        int keyCount = (int)ImGuiKey.ImGuiKey_NamedKey_COUNT;
        KeysData = new ImGuiKeyData[keyCount];
        for (int i = 0; i < keyCount; i++)
        {
            KeysData[i].DownDuration = -1.0f;
            KeysData[i].DownDurationPrev = -1.0f;
        }

        AppAcceptingEvents = true;
        InputQueueCharacters = new List<uint>(8);
        MouseDelta = ImVec2.Zero;
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

    public void AddKeyEvent(ImGuiKey key, bool down)
    {
        var ctx = Context;
        if (ctx == null || !AppAcceptingEvents)
            return;
        ImGuiInputEvent evt = new()
        {
            Type = ImGuiInputEventType.Key,
            Source = ImGuiInputSource.Keyboard,
            EventId = ctx.InputEventsNextEventId++,
            Key = new ImGuiInputEventKey { Key = key, Down = down, AnalogValue = down ? 1.0f : 0.0f }
        };
        ctx.EnqueueInputEvent(evt);
    }

    public void AddMousePosEvent(float x, float y)
    {
        var ctx = Context;
        if (ctx == null || !AppAcceptingEvents)
            return;
        ImGuiInputEvent evt = new()
        {
            Type = ImGuiInputEventType.MousePos,
            Source = ImGuiInputSource.Mouse,
            EventId = ctx.InputEventsNextEventId++,
            MousePos = new ImGuiInputEventMousePos { Pos = new ImVec2(x, y) }
        };
        ctx.EnqueueInputEvent(evt);
    }

    public void AddMouseButtonEvent(int button, bool down)
    {
        var ctx = Context;
        if (ctx == null || !AppAcceptingEvents)
            return;
        ImGuiInputEvent evt = new()
        {
            Type = ImGuiInputEventType.MouseButton,
            Source = ImGuiInputSource.Mouse,
            EventId = ctx.InputEventsNextEventId++,
            MouseButton = new ImGuiInputEventMouseButton { Button = button, Down = down }
        };
        ctx.EnqueueInputEvent(evt);
    }

    public void AddMouseWheelEvent(float wheelX, float wheelY)
    {
        var ctx = Context;
        if (ctx == null || !AppAcceptingEvents)
            return;
        ImGuiInputEvent evt = new()
        {
            Type = ImGuiInputEventType.MouseWheel,
            Source = ImGuiInputSource.Mouse,
            EventId = ctx.InputEventsNextEventId++,
            MouseWheel = new ImGuiInputEventMouseWheel { WheelX = wheelX, WheelY = wheelY }
        };
        ctx.EnqueueInputEvent(evt);
    }

    public void AddMouseSourceEvent(ImGuiMouseSource source)
    {
        var ctx = Context;
        if (ctx == null || !AppAcceptingEvents)
            return;
        ImGuiInputEvent evt = new()
        {
            Type = ImGuiInputEventType.MouseSource,
            Source = ImGuiInputSource.Mouse,
            EventId = ctx.InputEventsNextEventId++,
            MouseSource = source
        };
        ctx.EnqueueInputEvent(evt);
    }

    public void AddFocusEvent(bool focused)
    {
        var ctx = Context;
        if (ctx == null)
            return;
        ImGuiInputEvent evt = new()
        {
            Type = ImGuiInputEventType.Focus,
            Source = ImGuiInputSource.None,
            EventId = ctx.InputEventsNextEventId++,
            Focus = new ImGuiInputEventFocus { Focused = focused }
        };
        ctx.EnqueueInputEvent(evt);
    }

    public void AddInputCharactersUTF8(ReadOnlySpan<byte> utf8)
    {
        if (!AppAcceptingEvents)
            return;
        int i = 0;
        while (i < utf8.Length)
        {
            uint codepoint;
            int consumed = Utf8Decode(utf8.Slice(i), out codepoint);
            if (consumed <= 0)
                break;
            AddInputCharacter(codepoint);
            i += consumed;
        }
    }

    public void ClearEventsQueue()
    {
        Context?.InputEventsQueue.Clear();
    }

    public void ClearInputKeys()
    {
        for (int i = 0; i < KeysData.Length; i++)
        {
            KeysData[i].Down = false;
            KeysData[i].DownDuration = -1.0f;
            KeysData[i].DownDurationPrev = -1.0f;
        }
        KeyCtrl = KeyShift = KeyAlt = KeySuper = false;
        KeyMods = 0;
        InputQueueCharacters.Clear();
        WantTextInput = false;
    }

    public void ClearInputMouse()
    {
        for (int i = 0; i < MouseDown.Length; i++)
        {
            MouseDown[i] = false;
            MouseDownDuration[i] = -1.0f;
            MouseDownDurationPrev[i] = -1.0f;
            MouseClicked[i] = false;
            MouseReleased[i] = false;
        }
        MouseWheel = 0;
        MouseWheelH = 0;
        MousePos = new ImVec2(-float.MaxValue, -float.MaxValue);
        MousePosPrev = new ImVec2(-float.MaxValue, -float.MaxValue);
    }

    public void SetAppAcceptingEvents(bool acceptingEvents)
    {
        AppAcceptingEvents = acceptingEvents;
    }

    private static int Utf8Decode(ReadOnlySpan<byte> data, out uint codepoint)
    {
        codepoint = 0;
        if (data.IsEmpty)
            return 0;
        byte b0 = data[0];
        if (b0 < 0x80)
        {
            codepoint = b0;
            return 1;
        }
        if ((b0 & 0xE0) == 0xC0 && data.Length >= 2)
        {
            codepoint = (uint)(((b0 & 0x1F) << 6) | (data[1] & 0x3F));
            return 2;
        }
        if ((b0 & 0xF0) == 0xE0 && data.Length >= 3)
        {
            codepoint = (uint)(((b0 & 0x0F) << 12) | ((data[1] & 0x3F) << 6) | (data[2] & 0x3F));
            return 3;
        }
        if ((b0 & 0xF8) == 0xF0 && data.Length >= 4)
        {
            codepoint = (uint)(((b0 & 0x07) << 18) | ((data[1] & 0x3F) << 12) | ((data[2] & 0x3F) << 6) | (data[3] & 0x3F));
            return 4;
        }
        return 0;
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
public sealed class ImFontAtlas
{
    public ImTextureID TexID { get; private set; }
    public int TexWidth { get; set; }
    public int TexHeight { get; set; }
    public byte[]? TexPixelsRGBA32 { get; set; }
    public stbtt_bakedchar[]? BakedChars { get; set; }

    public void SetTexID(ImTextureID texId)
    {
        TexID = texId;
    }
}

public sealed class ImFont { }
