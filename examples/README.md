# ImGui Examples (SDL path)

This folder mirrors the upstream `examples/` layout with a focus on SDL2. The current sample is a stub that exercises the managed backend scaffolding without opening a real window.

- `ImGui.SdlExample`: console entry point wiring the SDL platform/renderer shims and submitting a single ImGui frame.
- `libs/`: placeholder for any SDL-only helper libs needed by future examples.

When SDL2 bindings are available, swap the placeholder backend with real SDL window creation and event processing, then forward draw data to your renderer of choice (OpenGL or SDLRenderer).
