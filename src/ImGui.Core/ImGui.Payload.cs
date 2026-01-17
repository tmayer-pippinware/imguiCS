namespace ImGui;

public sealed class ImGuiPayload
{
    public string? DataType { get; internal set; }
    public int DataSize { get; internal set; }
    public byte[]? Data { get; internal set; }
    public bool IsDelivery { get; internal set; }
    public bool IsPreview { get; internal set; }

    internal void Clear()
    {
        DataType = null;
        DataSize = 0;
        Data = null;
        IsDelivery = false;
        IsPreview = false;
    }
}
