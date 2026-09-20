using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using ZXing.Common;

namespace CampusNav.Navigation
{
    /// <summary>
    /// Decodes QR codes using ZXing.Net, sourcing frames from the AR
    /// camera's own CPU image feed rather than a second, independent
    /// camera capture.
    ///
    /// WHY NOT WebCamTexture:
    /// On both Android (ARCore) and iOS (ARKit), the AR session takes
    /// exclusive ownership of the physical camera device for as long as
    /// ARSession is running. A second, independent UnityEngine.WebCamTexture
    /// request for that same camera fails to open — or opens but delivers a
    /// frozen/black texture — on real hardware, because the OS camera stack
    /// only grants one active client per device. That conflict isn't a rare
    /// edge case here: QR scanning exists specifically to (re-)localize the
    /// player indoors (see WaypointLocalizer), i.e. exactly while AR
    /// passthrough is active. So instead this reads through the same
    /// camera session AR Foundation already owns, via
    /// ARCameraManager.TryAcquireLatestCpuImage — the API AR Foundation
    /// itself documents for doing custom CPU-side image processing
    /// alongside AR (the same approach ML Kit-on-ARCore integrations use).
    /// The Y (luma) plane of that image is used directly as ZXing's
    /// grayscale luminance input, with no RGB conversion needed.
    ///
    /// MANUAL EDITOR STEP REQUIRED: assign _cameraManager to the
    /// ARCameraManager component on the scene's AR Camera (see
    /// NavigationScene), and assign this component to WaypointLocalizer's
    /// QR scanner field.
    /// </summary>
    public class ZxingQrScanner : IQrScannerBehaviour
    {
        [SerializeField] private ARCameraManager _cameraManager;

        [Header("Performance")]
        [Tooltip("Only attempt a decode once every N camera frames. QR decoding is comparatively expensive CPU work, and a code the user is holding the phone up to stays in frame for many frames in a row, so there's no benefit to decoding every single one.")]
        [SerializeField] private int _decodeEveryNFrames = 10;

        public override event Action<string> OnQrDecoded;

        private readonly BarcodeReaderGeneric _reader = new BarcodeReaderGeneric
        {
            AutoRotate = false,
            Options = new DecodingOptions
            {
                TryHarder = false,
                PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
            },
        };

        private bool _isScanning;
        private int _frameCounter;
        private byte[] _luminanceBuffer;

        public override void StartScanning()
        {
            _isScanning = true;
        }

        public override void StopScanning()
        {
            _isScanning = false;
        }

        private void OnEnable()
        {
            if (_cameraManager != null) _cameraManager.frameReceived += OnCameraFrameReceived;
        }

        private void OnDisable()
        {
            if (_cameraManager != null) _cameraManager.frameReceived -= OnCameraFrameReceived;
        }

        private void OnCameraFrameReceived(ARCameraFrameEventArgs args)
        {
            if (!_isScanning) return;

            _frameCounter++;
            if (_frameCounter % _decodeEveryNFrames != 0) return;

            TryDecodeLatestFrame();
        }

        private void TryDecodeLatestFrame()
        {
            if (_cameraManager == null || !_cameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                return;
            }

            using (image)
            {
                DecodeImage(image);
            }
        }

        private void DecodeImage(XRCpuImage image)
        {
            // Plane 0 of a YUV camera image is the Y (luma) plane — this
            // already IS grayscale luminance, exactly what a barcode reader
            // needs, so no RGB conversion is necessary.
            XRCpuImage.Plane yPlane = image.GetPlane(0);
            int width = image.width;
            int height = image.height;

            byte[] buffer = GetLuminanceBuffer(width * height);
            CopyPlaneToBuffer(yPlane, width, height, buffer);

            TryDecodeLuminance(buffer, width, height);
        }

        /// <summary>
        /// Decodes a single-channel grayscale luminance buffer and raises
        /// OnQrDecoded on success. Split out from DecodeImage so tests can
        /// drive the actual ZXing decode path with a synthetically
        /// generated QR image, without needing a live AR camera CPU image
        /// (XRCpuImage can't be constructed outside a running AR session).
        /// </summary>
        internal bool TryDecodeLuminance(byte[] luminance, int width, int height)
        {
            var luminanceSource = new PlanarYUVLuminanceSource(
                luminance, width, height, 0, 0, width, height, false);

            Result result = _reader.Decode(luminanceSource);
            if (result != null && !string.IsNullOrEmpty(result.Text))
            {
                OnQrDecoded?.Invoke(result.Text);
                return true;
            }
            return false;
        }

        private byte[] GetLuminanceBuffer(int size)
        {
            if (_luminanceBuffer == null || _luminanceBuffer.Length != size)
            {
                _luminanceBuffer = new byte[size];
            }
            return _luminanceBuffer;
        }

        private static void CopyPlaneToBuffer(XRCpuImage.Plane plane, int width, int height, byte[] destination)
        {
            int rowStride = plane.rowStride;
            NativeArray<byte> data = plane.data;

            if (rowStride == width)
            {
                // No per-row padding — one contiguous copy.
                NativeArray<byte>.Copy(data, destination, width * height);
            }
            else
            {
                // Row stride has padding beyond the actual pixel width;
                // copy row by row, skipping the padding bytes.
                for (int row = 0; row < height; row++)
                {
                    NativeArray<byte>.Copy(data, row * rowStride, destination, row * width, width);
                }
            }
        }
    }
}
