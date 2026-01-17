using ImGui;
using Xunit;

namespace ImGui.Core.Tests;

public class WidgetTests
{
    [Fact]
    public void Button_returns_true_on_click()
    {
        ImGui.CreateContext();
        ImGui.Begin("Test");
        var cursor = ImGui.GetCursorScreenPos();
        ImGui.AddMousePosEvent(cursor.x + 1, cursor.y + 1);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        var pressed = ImGui.Button("Btn");
        Assert.True(pressed);
        ImGui.End();
    }

    [Fact]
    public void SmallButton_press()
    {
        ImGui.CreateContext();
        ImGui.Begin("Buttons");
        var pos = ImGui.GetCursorScreenPos();
        ImGui.AddMousePosEvent(pos.x + 1, pos.y + 1);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        Assert.True(ImGui.SmallButton("Small"));
        ImGui.End();
    }

    [Fact]
    public void ArrowButton_press()
    {
        ImGui.CreateContext();
        ImGui.Begin("Buttons");
        ImGui.AddMousePosEvent(ImGui.GetCursorScreenPos().x + 1, ImGui.GetCursorScreenPos().y + 1);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        Assert.True(ImGui.ArrowButton("Arrow", ImGuiDir.ImGuiDir_Right));
        ImGui.End();
    }

    [Fact]
    public void InvisibleButton_registers_click_without_draw()
    {
        ImGui.CreateContext();
        ImGui.Begin("Invis");
        var pos = ImGui.GetCursorScreenPos();
        ImGui.AddMousePosEvent(pos.x + 1, pos.y + 1);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        var pressed = ImGui.InvisibleButton("area", new ImVec2(20, 10));
        Assert.True(pressed);
        Assert.Empty(ImGui.GetWindowDrawList().VtxBuffer);
        ImGui.End();
    }

    [Fact]
    public void Button_uses_visible_label_with_hash_suffix()
    {
        ImGui.CreateContext();
        ImGui.Begin("Button");
        ImGui.Button("Click##Hidden");
        var drawList = ImGui.GetWindowDrawList();
        Assert.Contains(drawList.TextBuffer, t => t.Text == "Click");
        var idWithSuffix = ImGui.GetID("Click##Hidden");
        var idWithoutSuffix = ImGui.GetID("Click");
        Assert.NotEqual(idWithoutSuffix, idWithSuffix);
        ImGui.End();
    }

    [Fact]
    public void PushID_changes_GetID()
    {
        ImGui.CreateContext();
        ImGui.Begin("Test");
        var baseId = ImGui.GetID("Label");
        ImGui.PushID("Scope");
        var scopedId = ImGui.GetID("Label");
        Assert.NotEqual(baseId, scopedId);
        ImGui.PopID();
        ImGui.End();
    }

    [Fact]
    public void Text_records_draw_command()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("Test");
        ImGui.Text("Hello");
        ImGui.End();
        ImGui.Render();

        var dd = ImGui.GetDrawData();
        Assert.Equal(2, dd.CmdLists.Count);
        var windowList = dd.CmdLists[0];
        Assert.Single(windowList.TextBuffer);
        Assert.Equal("Hello", windowList.TextBuffer[0].Text);
    }

    [Fact]
    public void Text_hides_id_suffix_when_rendered()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("Text");
        ImGui.Text("Hello##Hidden");
        ImGui.End();
        ImGui.Render();

        var dd = ImGui.GetDrawData();
        Assert.Equal("Hello", dd.CmdLists[0].TextBuffer[0].Text);
    }

    [Fact]
    public void SameLine_places_items_horizontally()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("Test");
        ImGui.Button("A");
        ImGui.SameLine();
        ImGui.Button("B");
        ImGui.End();
        ImGui.Render();

        var dl = ImGui.GetDrawData().CmdLists[0];
        Assert.True(dl.VtxBuffer.Count >= 8);
        var firstMin = dl.VtxBuffer[0].pos;
        var firstMax = dl.VtxBuffer[2].pos;
        var secondMin = dl.VtxBuffer[4].pos;
        var style = ImGui.GetStyle();
        Assert.Equal(firstMin.y, secondMin.y);
        Assert.InRange(secondMin.x, firstMax.x + style.ItemSpacing.x - 0.01f, firstMax.x + style.ItemSpacing.x + 0.01f);
    }

    [Fact]
    public void NextWindow_pos_and_size_apply_once()
    {
        ImGui.CreateContext();
        ImGui.SetNextWindowPos(new ImVec2(100, 50));
        ImGui.SetNextWindowSize(new ImVec2(300, 200));
        ImGui.Begin("Layout");
        Assert.Equal(new ImVec2(100, 50), ImGui.GetWindowPos());
        Assert.Equal(new ImVec2(300, 200), ImGui.GetWindowSize());
        ImGui.End();
    }

    [Fact]
    public void Cursor_and_content_region_respect_padding_and_advances()
    {
        ImGui.CreateContext();
        var style = ImGui.GetStyle();
        ImGui.SetNextWindowPos(new ImVec2(10, 20));
        ImGui.SetNextWindowSize(new ImVec2(120, 80));
        ImGui.Begin("Cursor");
        var screenPos = ImGui.GetCursorScreenPos();
        Assert.Equal(new ImVec2(10 + style.WindowPadding.x, 20 + style.WindowPadding.y), screenPos);
        var avail = ImGui.GetContentRegionAvail();
        ImGui.Dummy(new ImVec2(10, 10));
        var availAfter = ImGui.GetContentRegionAvail();
        Assert.Equal(avail.x, availAfter.x);
        Assert.True(availAfter.y < avail.y);
        ImGui.End();
    }

    [Fact]
    public void SetCursorPosX_and_Y_move_cursor()
    {
        ImGui.CreateContext();
        ImGui.Begin("Cursor");
        ImGui.SetCursorPosX(20);
        ImGui.SetCursorPosY(30);
        var pos = ImGui.GetCursorPos();
        Assert.Equal(20, pos.x);
        Assert.Equal(30, pos.y);
        ImGui.End();
    }

    [Fact]
    public void AlignTextToFramePadding_shifts_cursor()
    {
        ImGui.CreateContext();
        ImGui.Begin("Align");
        var start = ImGui.GetCursorPos();
        ImGui.AlignTextToFramePadding();
        var after = ImGui.GetCursorPos();
        Assert.True(after.y >= start.y + ImGui.GetStyle().FramePadding.y);
        ImGui.End();
    }

    [Fact]
    public void PushStyleVar_float_restores_alpha()
    {
        ImGui.CreateContext();
        var before = ImGui.GetStyle().Alpha;
        ImGui.PushStyleVar(ImGuiStyleVar_.ImGuiStyleVar_Alpha, 0.5f);
        Assert.Equal(0.5f, ImGui.GetStyle().Alpha);
        ImGui.PopStyleVar();
        Assert.Equal(before, ImGui.GetStyle().Alpha);
    }

    [Fact]
    public void Separator_adds_draw_command()
    {
        ImGui.CreateContext();
        ImGui.Begin("Lines");
        var dl = ImGui.GetWindowDrawList();
        int idxBefore = dl.IdxBuffer.Count;
        ImGui.Separator();
        Assert.True(dl.IdxBuffer.Count >= idxBefore + 6);
        ImGui.End();
    }

    [Fact]
    public void Hover_and_active_state_track_last_item()
    {
        ImGui.CreateContext();
        ImGui.Begin("Hover");
        var pos = ImGui.GetCursorScreenPos();
        ImGui.AddMousePosEvent(pos.x + 1, pos.y + 1);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        ImGui.Button("X");
        Assert.True(ImGui.IsItemHovered());
        Assert.True(ImGui.IsItemActive());
        Assert.Equal(ImGui.GetItemID(), ImGui.GetCurrentContext()!.ActiveId);
        ImGui.End();
    }

    [Fact]
    public void GetItemRectSize_matches_drawn_widget()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("Size");
        ImGui.Button("SizeBtn");
        var size = ImGui.GetItemRectSize();
        Assert.True(size.x > 0);
        Assert.True(size.y > 0);
        ImGui.End();
    }

    [Fact]
    public void Checkbox_toggles_value_on_click()
    {
        ImGui.CreateContext();
        bool value = false;
        ImGui.Begin("Check");
        var pos = ImGui.GetCursorScreenPos();
        ImGui.AddMousePosEvent(pos.x + 2, pos.y + 2);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        var pressed = ImGui.Checkbox("Enable", ref value);
        Assert.True(pressed);
        Assert.True(value);
        ImGui.End();
    }

    [Fact]
    public void RadioButton_sets_active_on_click()
    {
        ImGui.CreateContext();
        bool active = false;
        ImGui.Begin("Radio");
        var pos = ImGui.GetCursorScreenPos();
        ImGui.AddMousePosEvent(pos.x + 2, pos.y + 2);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        var pressed = ImGui.RadioButton("Choice", ref active);
        Assert.True(pressed);
        Assert.True(active);
        ImGui.End();
    }

    [Fact]
    public void TextColored_uses_temporary_style_color()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("Text");
        ImGui.TextColored(new ImVec4(1, 0, 0, 1), "Color");
        ImGui.End();
        ImGui.Render();
        var dd = ImGui.GetDrawData();
        Assert.Equal(2, dd.CmdLists.Count);
        var drawList = dd.CmdLists[0];
        Assert.Single(drawList.TextBuffer);
        Assert.Equal("Color", drawList.TextBuffer[0].Text);
    }

    [Fact]
    public void PushStyleVar_overrides_and_restores_spacing()
    {
        ImGui.CreateContext();
        var style = ImGui.GetStyle();
        var original = style.ItemSpacing;
        ImGui.PushStyleVar(ImGuiStyleVar_.ImGuiStyleVar_ItemSpacing, new ImVec2(10, 10));
        Assert.Equal(new ImVec2(10, 10), ImGui.GetStyle().ItemSpacing);
        ImGui.PopStyleVar();
        Assert.Equal(original, ImGui.GetStyle().ItemSpacing);
    }

    [Fact]
    public void PushPopClipRect_changes_clipping()
    {
        ImGui.CreateContext();
        ImGui.Begin("Clip");
        var dl = ImGui.GetWindowDrawList();
        int before = dl.CmdBuffer.Count;
        ImGui.PushClipRect(new ImVec2(0, 0), new ImVec2(10, 10));
        ImGui.PopClipRect();
        Assert.True(dl.CmdBuffer.Count > before);
        ImGui.End();
    }

    [Fact]
    public void TextDisabled_uses_disabled_color()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("Text");
        ImGui.TextDisabled("Muted");
        ImGui.End();
        ImGui.Render();
        var cmd = ImGui.GetDrawData().CmdLists[0].TextBuffer[0];
        Assert.Equal("Muted", cmd.Text);
        Assert.Equal(ImGui.GetColorU32(ImGuiCol_.ImGuiCol_TextDisabled), cmd.Color);
    }

    [Fact]
    public void SliderFloat_updates_value_on_click()
    {
        ImGui.CreateContext();
        float val = 0.0f;
        ImGui.Begin("Slider");
        var pos = ImGui.GetCursorScreenPos();
        ImGui.AddMousePosEvent(pos.x + 75, pos.y + 1);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        var changed = ImGui.SliderFloat("Volume", ref val, 0.0f, 1.0f);
        Assert.True(changed);
        Assert.InRange(val, 0.4f, 0.6f);
        ImGui.End();
    }

    [Fact]
    public void DragFloat_updates_value_on_drag()
    {
        ImGui.CreateContext();
        ImGui.Begin("Drag");
        var pos = ImGui.GetCursorScreenPos();
        ImGui.AddMousePosEvent(pos.x + 1, pos.y + 1);
        ImGui.AddMousePosEvent(pos.x + 20, pos.y + 1);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        float v = 0;
        var changed = ImGui.DragFloat("Drag", ref v, 1.0f, -10.0f, 10.0f);
        Assert.True(changed);
        Assert.True(v > 0);
        ImGui.End();
    }

    [Fact]
    public void InputText_accepts_characters_and_backspace()
    {
        ImGui.CreateContext();
        ImGui.Begin("Input");
        var pos = ImGui.GetCursorScreenPos();
        ImGui.AddMousePosEvent(pos.x + 1, pos.y + 1);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.AddInputCharacter('X');
        ImGui.AddKeyEvent(ImGuiKey.ImGuiKey_Backspace, true);
        ImGui.NewFrame();
        string text = "Hi";
        bool changed = ImGui.InputText("Name", ref text);
        ImGui.End();
        Assert.True(changed);
        Assert.Equal("Hi", text);
    }

    [Fact]
    public void Combo_cycles_selection_on_activation()
    {
        ImGui.CreateContext();
        ImGui.Begin("Combo");
        var pos = ImGui.GetCursorScreenPos();
        ImGui.AddMousePosEvent(pos.x + 1, pos.y + 1);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        int current = 0;
        var items = new[] { "A", "B", "C" };
        bool changed = ImGui.Combo("Options", ref current, items);
        ImGui.End();
        Assert.True(changed);
        Assert.Equal(1, current);
    }

    [Fact]
    public void Selectable_toggles_selection_on_click()
    {
        ImGui.CreateContext();
        ImGui.Begin("Sel");
        var pos = ImGui.GetCursorScreenPos();
        ImGui.AddMousePosEvent(pos.x + 1, pos.y + 1);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        bool selected = false;
        var pressed = ImGui.Selectable("Item", ref selected);
        Assert.True(pressed);
        Assert.True(selected);
        ImGui.End();
    }

    [Fact]
    public void Menu_bar_and_menu_item_activate()
    {
        ImGui.CreateContext();
        var io = ImGui.GetIO();
        io.DisplaySize = new ImVec2(200, 100);
        ImGui.AddMousePosEvent(10, 10);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        Assert.True(ImGui.BeginMainMenuBar());
        Assert.True(ImGui.BeginMenuBar());
        var open = ImGui.BeginMenu("File");
        Assert.True(open);
        var clicked = ImGui.MenuItem("New");
        Assert.True(clicked);
        ImGui.EndMenu();
        ImGui.EndMenuBar();
        ImGui.EndMainMenuBar();
    }

    [Fact]
    public void Popup_open_and_close_cycle()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("PopupOwner");
        ImGui.OpenPopup("P");
        bool shown = ImGui.BeginPopup("P");
        Assert.True(shown);
        ImGui.MenuItem("Inside");
        ImGui.CloseCurrentPopup();
        ImGui.EndPopup();
        ImGui.End();

        ImGui.NewFrame();
        ImGui.Begin("PopupOwner");
        Assert.False(ImGui.BeginPopup("P"));
        ImGui.End();
    }

    [Fact]
    public void DragDrop_payload_delivered()
    {
        ImGui.CreateContext();
        ImGui.AddMousePosEvent(5, 5);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        ImGui.Begin("DragDrop");
        ImGui.Button("Source");
        Assert.True(ImGui.BeginDragDropSource());
        ImGui.SetDragDropPayload("TEXT", System.Text.Encoding.UTF8.GetBytes("hello"));
        ImGui.EndDragDropSource();

        ImGui.Button("Target");
        Assert.True(ImGui.BeginDragDropTarget());
        var payload = ImGui.AcceptDragDropPayload("TEXT");
        ImGui.EndDragDropTarget();
        ImGui.End();
        Assert.NotNull(payload);
        Assert.True(payload!.IsDelivery || payload.IsPreview);
    }

    [Fact]
    public void CollapsingHeader_toggles_and_persists_state()
    {
        ImGui.CreateContext();
        ImGui.SetNextWindowPos(new ImVec2(0, 0));
        ImGui.NewFrame();
        ImGui.Begin("Tree");
        bool open = true;
        var firstOpen = ImGui.CollapsingHeader("Section", ref open);
        Assert.True(firstOpen);
        Assert.True(open);
        ImGui.End();

        // State persists without reinitializing 'open'
        ImGui.NewFrame();
        ImGui.Begin("Tree");
        var persisted = ImGui.CollapsingHeader("Section", ref open);
        Assert.True(persisted);
        Assert.True(open);
        ImGui.End();
    }

    [Fact]
    public void TreeNode_reports_toggled_open()
    {
        ImGui.CreateContext();
        ImGui.AddMousePosEvent(10, 10);
        ImGui.AddMouseButtonEvent(0, true);
        ImGui.NewFrame();
        ImGui.Begin("Tree");
        var open = ImGui.TreeNode("Node");
        Assert.True(open);
        Assert.True(ImGui.IsItemToggledOpen());
        ImGui.TreePop();
        ImGui.End();
    }

    [Fact]
    public void TreeNodeEx_respects_default_open_and_leaf()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("Tree");
        bool open = ImGui.TreeNodeEx("DefaultOpen", ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_DefaultOpen);
        Assert.True(open);
        ImGui.TreePop();
        ImGui.NewFrame();
        ImGui.Begin("Tree");
        bool leafOpen = ImGui.TreeNodeEx("Leaf", ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_Leaf);
        Assert.True(leafOpen);
        Assert.False(ImGui.IsItemToggledOpen());
        ImGui.End();
    }

    [Fact]
    public void TreePush_and_TreePop_adjust_indent()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("Indent");
        var start = ImGui.GetCursorPos();
        ImGui.TreePush("scope");
        var after = ImGui.GetCursorPos();
        ImGui.TreePop();
        var end = ImGui.GetCursorPos();
        Assert.True(after.x > start.x);
        Assert.Equal(start.x, end.x);
        ImGui.End();
    }

    [Fact]
    public void SetItemDefaultFocus_sets_nav_id()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("Focus");
        ImGui.Button("Btn");
        ImGui.SetItemDefaultFocus();
        Assert.True(ImGui.IsItemFocused());
        ImGui.End();
    }

    [Fact]
    public void BulletText_and_TextWrapped_render_lines()
    {
        ImGui.CreateContext();
        ImGui.NewFrame();
        ImGui.Begin("Wrap");
        ImGui.BulletText("Bullet");
        ImGui.SetNextWindowSize(new ImVec2(50, 100));
        ImGui.TextWrapped("This is a wrapped line of text");
        ImGui.End();
        ImGui.Render();
        var dd = ImGui.GetDrawData();
        Assert.Equal(2, dd.CmdLists.Count);
        Assert.True(dd.CmdLists[0].TextBuffer.Count >= 2);
    }

    [Fact]
    public void TextWrapped_strips_id_suffix_and_wraps_lines()
    {
        ImGui.CreateContext();
        ImGui.SetNextWindowSize(new ImVec2(50, 100));
        ImGui.NewFrame();
        ImGui.Begin("Wrap");
        ImGui.TextWrapped("Word Word Word##Hidden");
        ImGui.End();
        ImGui.Render();
        var texts = ImGui.GetDrawData().CmdLists[0].TextBuffer;
        Assert.All(texts, t => Assert.DoesNotContain("##", t.Text));
        Assert.True(texts.Count >= 2);
    }

    [Fact]
    public void Indent_and_unindent_move_cursor()
    {
        ImGui.CreateContext();
        ImGui.Begin("Indent");
        var start = ImGui.GetCursorPos();
        ImGui.Indent();
        var afterIndent = ImGui.GetCursorPos();
        ImGui.Unindent();
        var afterUnindent = ImGui.GetCursorPos();
        Assert.True(afterIndent.x > start.x);
        Assert.Equal(start.x, afterUnindent.x);
        ImGui.End();
    }

    [Fact]
    public void BeginGroup_and_EndGroup_wrap_items()
    {
        ImGui.CreateContext();
        ImGui.Begin("Group");
        var start = ImGui.GetCursorPos();
        ImGui.BeginGroup();
        ImGui.Button("Grouped");
        ImGui.EndGroup();
        var after = ImGui.GetCursorPos();
        Assert.Equal(start.x, after.x);
        Assert.True(ImGui.GetItemRectSize().y > 0);
        ImGui.End();
    }

    [Fact]
    public void BeginChild_advances_parent_cursor()
    {
        ImGui.CreateContext();
        ImGui.Begin("Parent");
        var startY = ImGui.GetCursorPos().y;
        ImGui.BeginChild("Child", new ImVec2(40, 30), false);
        ImGui.Button("Inside");
        ImGui.EndChild();
        var afterY = ImGui.GetCursorPos().y;
        Assert.True(afterY > startY);
        var childSize = ImGui.GetItemRectSize();
        Assert.InRange(childSize.x, 39.9f, 40.1f);
        Assert.InRange(childSize.y, 29.9f, 30.1f);
        ImGui.End();
    }
}
