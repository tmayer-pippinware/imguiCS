using ImGui;
using ImGui.Backends.Sdl;

// Minimal placeholder example for SDL2 integration. This does not open a real SDL window yet,
// but it shows how to wire the backend shims and render a frame.

var ctx = ImGui.CreateContext();
var platform = new ImGuiSDL2Platform();
var renderer = new ImGuiSDLRenderer();

platform.Init(IntPtr.Zero);
renderer.Init(IntPtr.Zero);
platform.UpdateDisplaySize(800, 600);

// Single frame demo content
ImGui.NewFrame();
ImGui.Begin("Hello SDL");
ImGui.Text("ImGui SDL example stub");
ImGui.Button("Click me");
ImGui.End();
ImGui.Render();

// Would forward to GPU in a real backend
renderer.RenderDrawData(ImGui.GetDrawData());
renderer.Shutdown();
platform.Shutdown();
