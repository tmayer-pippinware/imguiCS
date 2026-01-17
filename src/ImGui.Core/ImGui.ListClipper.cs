using System;

namespace ImGui;

public sealed class ImGuiListClipper
{
    public int DisplayStart { get; private set; }
    public int DisplayEnd { get; private set; }
    public float ItemsHeight { get; private set; }
    public float StartPosY { get; private set; }
    private int _itemsCount;
    private int _step;

    public void Begin(int itemsCount, float itemsHeight = -1.0f)
    {
        _itemsCount = itemsCount;
        ItemsHeight = itemsHeight;
        DisplayStart = DisplayEnd = 0;
        _step = 0;
        StartPosY = 0f;
        if (ItemsHeight <= 0f)
            ItemsHeight = 1f;
    }

    public bool Step()
    {
        if (_step == 0)
        {
            DisplayStart = 0;
            DisplayEnd = Math.Min(_itemsCount, (int)(10)); // placeholder window height of ~10 items
            _step++;
            return true;
        }
        if (DisplayEnd >= _itemsCount)
            return false;
        DisplayStart = DisplayEnd;
        DisplayEnd = Math.Min(_itemsCount, DisplayStart + (int)(10));
        _step++;
        return DisplayStart < DisplayEnd;
    }

    public void End()
    {
        _itemsCount = 0;
    }
}
