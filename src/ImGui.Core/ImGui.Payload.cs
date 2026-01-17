namespace ImGui;

public sealed class ImGuiPayload
{
    public string? DataType { get; internal set; }
    public int DataSize { get; internal set; }
    public bool DataFrameCount { get; internal set; }
}
