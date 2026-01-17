namespace ImGui;

public struct ImGuiTableColumnSortSpecs
{
    public int ColumnIndex;
    public ImGuiID ColumnUserID;
    public ImGuiSortDirection SortDirection;
    public short SortOrder;
}

public struct ImGuiTableSortSpecs
{
    public ImGuiTableColumnSortSpecs[]? Specs;
    public int SpecsCount;
    public bool SpecsDirty;
}
