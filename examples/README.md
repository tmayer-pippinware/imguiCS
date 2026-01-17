# ImGui Examples (SDL path)

This folder mirrors the upstream `examples/` layout with a focus on SDL2.

- `ImGui.SdlExample`: opens an SDL2 window, processes SDL events through the managed backend, and renders via SDL's 2D renderer.
- `SDL2.dll`: Windows native binary for SDL2 copied to the example output directory.
- `libs/`: placeholder for helper libraries or alternate platform binaries (drop macOS/Linux SDL2 dylib/so files here and mark CopyToOutputDirectory in the project if needed).

Run the example with `dotnet run --project ImGui.SdlExample`. The window demonstrates the backend wiring; rendering is color-only until the renderer gains full draw list translation.
