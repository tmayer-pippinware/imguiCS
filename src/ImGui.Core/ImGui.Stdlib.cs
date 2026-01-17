using System.Text;

namespace ImGui;

public static partial class ImGui
{
    public static bool InputText(string label, StringBuilder buf, ImGuiInputTextFlags_ flags = ImGuiInputTextFlags_.ImGuiInputTextFlags_None)
    {
        string str = buf.ToString();
        bool changed = InputText(label, ref str, flags);
        if (changed)
        {
            buf.Clear();
            buf.Append(str);
        }
        return changed;
    }
}
