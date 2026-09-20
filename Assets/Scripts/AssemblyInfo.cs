using System.Runtime.CompilerServices;

// Lets CampusNav.EditModeTests exercise internal test seams (e.g.
// ZxingQrScanner.TryDecodeLuminance) that intentionally aren't part of the
// public API surface.
[assembly: InternalsVisibleTo("CampusNav.EditModeTests")]
