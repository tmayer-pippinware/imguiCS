namespace ImGui;

/// <summary>
/// Compile-time configuration mapped from upstream imconfig.h.
/// Override values by providing a partial <c>Customize</c> implementation in another partial class file.
/// </summary>
internal static partial class ImConfig
{
    internal static Options Current { get; }

    static ImConfig()
    {
        var options = Options.Default;
        Customize(ref options);
        Current = options;
    }

    /// <summary>
    /// Creates a copy of the default option set (useful for tests).
    /// </summary>
    internal static Options CreateDefault() => Options.Default;

    /// <summary>
    /// Implement in a sibling partial class to override compile-time toggles without modifying this file.
    /// </summary>
    /// <param name="options">Options instance to mutate.</param>
    static partial void Customize(ref Options options);

    internal readonly record struct Options
    {
        internal static Options Default => new()
        {
            Disable = false,
            DisableDemoWindows = false,
            DisableDebugTools = false,
            DisableObsoleteFunctions = false,
            DisableObsoleteKeyIO = false,
            DisableWin32DefaultClipboard = false,
            EnableWin32DefaultIme = true,
            DisableWin32DefaultIme = false,
            DisableWin32Functions = false,
            EnableOsxDefaultClipboard = false,
            DisableDefaultShellFunctions = false,
            DisableDefaultFormatFunctions = false,
            DisableDefaultMathFunctions = false,
            DisableFileFunctions = false,
            DisableDefaultFileFunctions = false,
            DisableDefaultAllocators = false,
            DisableDefaultFont = false,
            DisableSse = false,
            EnableTestEngine = false,
            IncludeImGuiUserH = false,
            UseBgraPackedColor = false,
            UseLegacyCrc32Adler = false,
            UseWchar32 = false,
            DefineMathOperators = false,
            UseStbSprintf = false,
            EnableFreeType = false,
            EnableFreeTypePlutoSvg = false,
            EnableFreeTypeLunaSvg = false,
            EnableStbTrueType = true,
            DisableStbTrueTypeImplementation = false,
            DisableStbRectPackImplementation = false,
            DisableStbSprintfImplementation = false,
            UseDebugParanoid = false
        };

        internal bool Disable { get; init; }
        internal bool DisableDemoWindows { get; init; }
        internal bool DisableDebugTools { get; init; }
        internal bool DisableObsoleteFunctions { get; init; }
        internal bool DisableObsoleteKeyIO { get; init; }

        internal bool DisableWin32DefaultClipboard { get; init; }
        internal bool EnableWin32DefaultIme { get; init; }
        internal bool DisableWin32DefaultIme { get; init; }
        internal bool DisableWin32Functions { get; init; }
        internal bool EnableOsxDefaultClipboard { get; init; }
        internal bool DisableDefaultShellFunctions { get; init; }
        internal bool DisableDefaultFormatFunctions { get; init; }
        internal bool DisableDefaultMathFunctions { get; init; }
        internal bool DisableFileFunctions { get; init; }
        internal bool DisableDefaultFileFunctions { get; init; }
        internal bool DisableDefaultAllocators { get; init; }
        internal bool DisableDefaultFont { get; init; }
        internal bool DisableSse { get; init; }
        internal bool EnableTestEngine { get; init; }
        internal bool IncludeImGuiUserH { get; init; }

        internal bool UseBgraPackedColor { get; init; }
        internal bool UseLegacyCrc32Adler { get; init; }
        internal bool UseWchar32 { get; init; }
        internal bool DefineMathOperators { get; init; }
        internal bool UseStbSprintf { get; init; }

        internal bool EnableFreeType { get; init; }
        internal bool EnableFreeTypePlutoSvg { get; init; }
        internal bool EnableFreeTypeLunaSvg { get; init; }
        internal bool EnableStbTrueType { get; init; }
        internal bool DisableStbTrueTypeImplementation { get; init; }
        internal bool DisableStbRectPackImplementation { get; init; }
        internal bool DisableStbSprintfImplementation { get; init; }

        internal bool UseDebugParanoid { get; init; }
    }
}
