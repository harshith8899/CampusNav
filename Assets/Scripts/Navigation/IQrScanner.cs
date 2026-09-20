using System;

namespace CampusNav.Navigation
{
    /// <summary>
    /// Abstraction over whatever QR-scanning plugin actually does the camera
    /// capture + decode. WaypointLocalizer depends only on this interface,
    /// so swapping the underlying scanning library later is a one-file
    /// change.
    ///
    /// You still need to install and wire up an actual scanning plugin (see
    /// project README for recommended options) and implement this interface
    /// against it — e.g. a ZxingQrScanner : IQrScanner that feeds camera
    /// frames to a ZXing.Net decoder and raises OnQrDecoded with the result.
    /// </summary>
    public interface IQrScanner
    {
        /// <summary>Raised with the decoded string payload whenever a QR code is read.</summary>
        event Action<string> OnQrDecoded;

        void StartScanning();
        void StopScanning();
    }
}
