using System;

namespace ImGui;

public struct ImGuiStyle
{
    public float FontSizeBase;
    public float FontScaleMain;
    public float FontScaleDpi;

    public float Alpha;
    public float DisabledAlpha;
    public ImVec2 WindowPadding;
    public float WindowRounding;
    public float WindowBorderSize;
    public float WindowBorderHoverPadding;
    public ImVec2 WindowMinSize;
    public ImVec2 WindowTitleAlign;
    public ImGuiDir WindowMenuButtonPosition;
    public float ChildRounding;
    public float ChildBorderSize;
    public float PopupRounding;
    public float PopupBorderSize;
    public ImVec2 FramePadding;
    public float FrameRounding;
    public float FrameBorderSize;
    public ImVec2 ItemSpacing;
    public ImVec2 ItemInnerSpacing;
    public ImVec2 CellPadding;
    public ImVec2 TouchExtraPadding;
    public float IndentSpacing;
    public float ColumnsMinSpacing;
    public float ScrollbarSize;
    public float ScrollbarRounding;
    public float ScrollbarPadding;
    public float GrabMinSize;
    public float GrabRounding;
    public float LogSliderDeadzone;
    public float ImageRounding;
    public float ImageBorderSize;
    public float TabRounding;
    public float TabBorderSize;
    public float TabMinWidthBase;
    public float TabMinWidthShrink;
    public float TabCloseButtonMinWidthSelected;
    public float TabCloseButtonMinWidthUnselected;
    public float TabBarBorderSize;
    public float TabBarOverlineSize;
    public float TableAngledHeadersAngle;
    public ImVec2 TableAngledHeadersTextAlign;
    public ImGuiTreeNodeFlags_ TreeLinesFlags;
    public float TreeLinesSize;
    public float TreeLinesRounding;
    public float DragDropTargetRounding;
    public float DragDropTargetBorderSize;
    public float DragDropTargetPadding;
    public float ColorMarkerSize;
    public ImGuiDir ColorButtonPosition;
    public ImVec2 ButtonTextAlign;
    public ImVec2 SelectableTextAlign;
    public float SeparatorTextBorderSize;
    public ImVec2 SeparatorTextAlign;
    public ImVec2 SeparatorTextPadding;
    public ImVec2 DisplayWindowPadding;
    public ImVec2 DisplaySafeAreaPadding;
    public float MouseCursorScale;
    public bool AntiAliasedLines;
    public bool AntiAliasedLinesUseTex;
    public bool AntiAliasedFill;
    public float CurveTessellationTol;
    public float CircleTessellationMaxError;
    public float HoverStationaryDelay;
    public float HoverDelayShort;
    public float HoverDelayNormal;
    public ImGuiHoveredFlags_ HoverFlagsForTooltipMouse;
    public ImGuiHoveredFlags_ HoverFlagsForTooltipNav;
    public float _MainScale;
    public float _NextFrameFontSizeBase;

    public ImVec4[] Colors;

    public ImGuiStyle()
    {
        FontSizeBase = 0.0f;
        FontScaleMain = 1.0f;
        FontScaleDpi = 1.0f;

        Alpha = 1.0f;
        DisabledAlpha = 0.60f;
        WindowPadding = new ImVec2(8, 8);
        WindowRounding = 0.0f;
        WindowBorderSize = 1.0f;
        WindowBorderHoverPadding = 4.0f;
        WindowMinSize = new ImVec2(32, 32);
        WindowTitleAlign = new ImVec2(0.0f, 0.5f);
        WindowMenuButtonPosition = ImGuiDir.ImGuiDir_Left;
        ChildRounding = 0.0f;
        ChildBorderSize = 1.0f;
        PopupRounding = 0.0f;
        PopupBorderSize = 1.0f;
        FramePadding = new ImVec2(4, 3);
        FrameRounding = 0.0f;
        FrameBorderSize = 0.0f;
        ItemSpacing = new ImVec2(8, 4);
        ItemInnerSpacing = new ImVec2(4, 4);
        CellPadding = new ImVec2(4, 2);
        TouchExtraPadding = new ImVec2(0, 0);
        IndentSpacing = 21.0f;
        ColumnsMinSpacing = 6.0f;
        ScrollbarSize = 14.0f;
        ScrollbarRounding = 9.0f;
        ScrollbarPadding = 2.0f;
        GrabMinSize = 12.0f;
        GrabRounding = 0.0f;
        LogSliderDeadzone = 4.0f;
        ImageRounding = 0.0f;
        ImageBorderSize = 0.0f;
        TabRounding = 5.0f;
        TabBorderSize = 0.0f;
        TabMinWidthBase = 1.0f;
        TabMinWidthShrink = 80.0f;
        TabCloseButtonMinWidthSelected = -1.0f;
        TabCloseButtonMinWidthUnselected = 0.0f;
        TabBarBorderSize = 1.0f;
        TabBarOverlineSize = 1.0f;
        TableAngledHeadersAngle = 35.0f * ((float)Math.PI / 180.0f);
        TableAngledHeadersTextAlign = new ImVec2(0.5f, 0.0f);
        TreeLinesFlags = ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_DrawLinesNone;
        TreeLinesSize = 1.0f;
        TreeLinesRounding = 0.0f;
        DragDropTargetRounding = 0.0f;
        DragDropTargetBorderSize = 2.0f;
        DragDropTargetPadding = 3.0f;
        ColorMarkerSize = 3.0f;
        ColorButtonPosition = ImGuiDir.ImGuiDir_Right;
        ButtonTextAlign = new ImVec2(0.5f, 0.5f);
        SelectableTextAlign = new ImVec2(0.0f, 0.0f);
        SeparatorTextBorderSize = 3.0f;
        SeparatorTextAlign = new ImVec2(0.0f, 0.5f);
        SeparatorTextPadding = new ImVec2(20.0f, 3.0f);
        DisplayWindowPadding = new ImVec2(19, 19);
        DisplaySafeAreaPadding = new ImVec2(3, 3);
        MouseCursorScale = 1.0f;
        AntiAliasedLines = true;
        AntiAliasedLinesUseTex = true;
        AntiAliasedFill = true;
        CurveTessellationTol = 1.25f;
        CircleTessellationMaxError = 0.30f;
        HoverStationaryDelay = 0.15f;
        HoverDelayShort = 0.15f;
        HoverDelayNormal = 0.40f;
        HoverFlagsForTooltipMouse = ImGuiHoveredFlags_.ImGuiHoveredFlags_Stationary | ImGuiHoveredFlags_.ImGuiHoveredFlags_DelayShort | ImGuiHoveredFlags_.ImGuiHoveredFlags_AllowWhenDisabled;
        HoverFlagsForTooltipNav = ImGuiHoveredFlags_.ImGuiHoveredFlags_NoSharedDelay | ImGuiHoveredFlags_.ImGuiHoveredFlags_DelayNormal | ImGuiHoveredFlags_.ImGuiHoveredFlags_AllowWhenDisabled;
        _MainScale = 1.0f;
        _NextFrameFontSizeBase = 0.0f;

        Colors = new ImVec4[(int)ImGuiCol_.ImGuiCol_COUNT];
        for (int i = 0; i < Colors.Length; i++)
            Colors[i] = new ImVec4(0, 0, 0, 1);
    }

    public static void StyleColorsDark(ref ImGuiStyle dst)
    {
        for (int i = 0; i < dst.Colors.Length; i++)
            dst.Colors[i] = new ImVec4(0, 0, 0, 1);
    }
}
