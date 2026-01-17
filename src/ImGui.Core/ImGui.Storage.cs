using System.Collections.Generic;

namespace ImGui;

public sealed class ImGuiStorage
{
    private readonly Dictionary<ImGuiID, ImGuiStoragePair> _data = new();

    public void Clear() => _data.Clear();

    public void SetInt(ImGuiID key, int value) => _data[key] = new ImGuiStoragePair(value);
    public void SetBool(ImGuiID key, bool value) => _data[key] = new ImGuiStoragePair(value ? 1 : 0);
    public void SetFloat(ImGuiID key, float value) => _data[key] = new ImGuiStoragePair(value);
    public void SetVoidPtr(ImGuiID key, nint value) => _data[key] = new ImGuiStoragePair(value);

    public int GetInt(ImGuiID key, int defaultVal = 0) => _data.TryGetValue(key, out var val) ? val.Int : defaultVal;
    public bool GetBool(ImGuiID key, bool defaultVal = false) => _data.TryGetValue(key, out var val) ? val.Int != 0 : defaultVal;
    public float GetFloat(ImGuiID key, float defaultVal = 0f) => _data.TryGetValue(key, out var val) ? val.Float : defaultVal;
    public nint GetVoidPtr(ImGuiID key) => _data.TryGetValue(key, out var val) ? val.Ptr : 0;
}

public readonly struct ImGuiStoragePair
{
    public readonly ImGuiID Key;
    public readonly int Int;
    public readonly float Float;
    public readonly nint Ptr;

    public ImGuiStoragePair(int v) { Key = 0; Int = v; Float = 0; Ptr = 0; }
    public ImGuiStoragePair(float v) { Key = 0; Int = 0; Float = v; Ptr = 0; }
    public ImGuiStoragePair(nint v) { Key = 0; Int = 0; Float = 0; Ptr = v; }
}
