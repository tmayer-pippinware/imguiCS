using System;

namespace ImGui;

/// <summary>
/// Holds Dear ImGui state for the current context.
/// </summary>
public sealed class ImGuiContext
{
    public ImGuiIO IO;
    public ImGuiStyle Style;
    public int FrameCount;
    public readonly List<ImGuiInputEvent> InputEventsQueue = new();
    internal uint InputEventsNextEventId = 1;
    public readonly ImDrawList ForegroundDrawList = new();
    internal readonly List<ImGuiWindow> Windows = new();
    internal ImGuiWindow? CurrentWindow;
    public Stack<ImGuiID> IDStack = new();
    public ImGuiID LastItemID;

    public ImGuiContext()
    {
        IO = new ImGuiIO();
        IO.Context = this;
        Style = new ImGuiStyle();
        FrameCount = 0;
        IDStack.Push(0);
    }

    internal void EnqueueInputEvent(in ImGuiInputEvent evt)
    {
        InputEventsQueue.Add(evt);
    }

    internal void ProcessInputEvents()
    {
        ref var io = ref IO;

        // Apply queued events.
        io.InputQueueCharacters.Clear();
        for (int i = 0; i < InputEventsQueue.Count; i++)
        {
            var evt = InputEventsQueue[i];
            switch (evt.Type)
            {
                case ImGuiInputEventType.MousePos:
                    io.MousePosPrev = io.MousePos;
                    io.MousePos = evt.MousePos.Pos;
                    io.MouseDelta = io.MousePos - io.MousePosPrev;
                    break;
                case ImGuiInputEventType.MouseButton:
                    if ((uint)evt.MouseButton.Button < (uint)io.MouseDown.Length)
                        io.MouseDown[evt.MouseButton.Button] = evt.MouseButton.Down;
                    break;
                case ImGuiInputEventType.MouseWheel:
                    io.MouseWheelH += evt.MouseWheel.WheelX;
                    io.MouseWheel += evt.MouseWheel.WheelY;
                    break;
                case ImGuiInputEventType.MouseSource:
                    io.MouseSource = evt.MouseSource;
                    break;
                case ImGuiInputEventType.Key:
                    ApplyKeyEvent(ref io, evt.Key);
                    break;
                case ImGuiInputEventType.Text:
                    io.InputQueueCharacters.Add(evt.Text.Char);
                    break;
                case ImGuiInputEventType.Focus:
                    io.AppFocusLost = !evt.Focus.Focused;
                    if (io.AppFocusLost)
                    {
                        io.ClearInputKeys();
                        io.ClearInputMouse();
                    }
                    break;
            }
        }

        InputEventsQueue.Clear();
        UpdateDurations(ref io);
        UpdateCaptureFlags(ref io);
    }

    private static void UpdateDurations(ref ImGuiIO io)
    {
        float dt = io.DeltaTime <= 0 ? 0 : io.DeltaTime;

        for (int i = 0; i < io.KeysData.Length; i++)
        {
            ref var key = ref io.KeysData[i];
            key.DownDurationPrev = key.DownDuration;
            key.DownDuration = key.Down ? (key.DownDuration < 0 ? 0 : key.DownDuration + dt) : -1.0f;
        }

        for (int i = 0; i < io.MouseDown.Length; i++)
        {
            io.MouseClicked[i] = false;
            io.MouseReleased[i] = false;
            io.MouseDownDurationPrev[i] = io.MouseDownDuration[i];
            io.MouseDownDuration[i] = io.MouseDown[i] ? (io.MouseDownDuration[i] < 0 ? 0 : io.MouseDownDuration[i] + dt) : -1.0f;
            if (io.MouseDown[i] && io.MouseDownDuration[i] == 0)
                io.MouseClicked[i] = true;
            if (!io.MouseDown[i] && io.MouseDownDurationPrev[i] >= 0)
                io.MouseReleased[i] = true;
        }

        io.Framerate = dt > 0 ? 1.0f / dt : 0;
    }

    private static void ApplyKeyEvent(ref ImGuiIO io, in ImGuiInputEventKey keyEvt)
    {
        int index = (int)keyEvt.Key - (int)ImGuiKey.ImGuiKey_NamedKey_BEGIN;
        if ((uint)index < (uint)io.KeysData.Length)
        {
            ref var key = ref io.KeysData[index];
            key.DownDurationPrev = key.DownDuration;
            key.Down = keyEvt.Down;
            key.DownDuration = keyEvt.Down ? (key.DownDuration < 0 ? 0 : key.DownDuration) : -1.0f;
        }

        switch (keyEvt.Key)
        {
            case ImGuiKey.ImGuiKey_LeftCtrl:
            case ImGuiKey.ImGuiKey_RightCtrl:
                io.KeyCtrl = keyEvt.Down;
                break;
            case ImGuiKey.ImGuiKey_LeftShift:
            case ImGuiKey.ImGuiKey_RightShift:
                io.KeyShift = keyEvt.Down;
                break;
            case ImGuiKey.ImGuiKey_LeftAlt:
            case ImGuiKey.ImGuiKey_RightAlt:
                io.KeyAlt = keyEvt.Down;
                break;
            case ImGuiKey.ImGuiKey_LeftSuper:
            case ImGuiKey.ImGuiKey_RightSuper:
                io.KeySuper = keyEvt.Down;
                break;
        }

        io.KeyMods = 0;
        if (io.KeyCtrl) io.KeyMods |= (int)ImGuiKey.ImGuiMod_Ctrl;
        if (io.KeyShift) io.KeyMods |= (int)ImGuiKey.ImGuiMod_Shift;
        if (io.KeyAlt) io.KeyMods |= (int)ImGuiKey.ImGuiMod_Alt;
        if (io.KeySuper) io.KeyMods |= (int)ImGuiKey.ImGuiMod_Super;
    }

    private static void UpdateCaptureFlags(ref ImGuiIO io)
    {
        bool anyMouseDown = false;
        for (int i = 0; i < io.MouseDown.Length; i++)
            anyMouseDown |= io.MouseDown[i];

        bool anyKeyDown = false;
        for (int i = 0; i < io.KeysData.Length; i++)
            anyKeyDown |= io.KeysData[i].Down;

        io.WantCaptureMouse = anyMouseDown || io.MouseWheel != 0 || io.MouseWheelH != 0;
        io.WantCaptureMouseUnlessPopupClose = io.WantCaptureMouse;
        io.WantCaptureKeyboard = anyKeyDown || io.InputQueueCharacters.Count > 0;
        io.WantTextInput = io.InputQueueCharacters.Count > 0;
        io.NavActive = io.WantCaptureKeyboard;
        io.NavVisible = io.WantCaptureKeyboard;
    }
}
