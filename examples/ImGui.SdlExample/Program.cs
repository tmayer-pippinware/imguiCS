using ImGui;
using ImGui.Backends.Sdl;
using SDL2;
using ImGuiApi = ImGui.ImGui;

// SDL2-based example that opens a native window, processes SDL events, and feeds them into imguiCS.
// Rendering uses SDL's 2D renderer to visualize draw geometry (colors only; text stays stubbed).

ImGuiApi.CreateContext();
SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_TIMER);
SDL.SDL_SetHint("SDL_MOUSE_FOCUS_CLICKTHROUGH", "1");

const int width = 1280;
const int height = 720;
IntPtr window = SDL.SDL_CreateWindow(
    "imguiCS SDL2 example",
    SDL.SDL_WINDOWPOS_CENTERED,
    SDL.SDL_WINDOWPOS_CENTERED,
    width,
    height,
    SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);

var platform = new ImGuiSDL2Platform();
platform.Init(window, new ImGuiSDL2PlatformOptions
{
    UseNativeBackend = true,
    InitialDisplaySize = new ImVec2(width, height)
});

var renderer = new ImGuiSDLRenderer();
renderer.Init(window, new ImGuiSDLRendererOptions { UseSDLRenderer = true });

bool done = false;
while (!done)
{
    while (SDL.SDL_PollEvent(out SDL.SDL_Event e) != 0)
    {
        if (e.type == SDL.SDL_EventType.SDL_QUIT)
            done = true;
        platform.ProcessEvent(ref e);
    }

    platform.NewFrame();
    renderer.NewFrame();

    ImGuiApi.NewFrame();
    ImGuiApi.Begin("SDL2 + imguiCS");
    ImGuiApi.Text("SDL2 backend running via SDL2-CS bindings.");
    ImGuiApi.Text($"Display size: {ImGuiApi.GetIO().DisplaySize.x} x {ImGuiApi.GetIO().DisplaySize.y}");
    if (ImGuiApi.Button("Quit"))
        done = true;
    ImGuiApi.End();
    ImGuiApi.Render();

    renderer.RenderDrawData(ImGuiApi.GetDrawData());
    SDL.SDL_Delay(1);
}

renderer.Shutdown();
platform.Shutdown();
SDL.SDL_DestroyWindow(window);
SDL.SDL_Quit();
