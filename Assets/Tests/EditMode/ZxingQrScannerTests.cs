using NUnit.Framework;
using UnityEngine;
using ZXing;
using ZXing.Common;
using ZXing.Rendering;
using CampusNav.Navigation;

namespace CampusNav.EditorTools.Tests
{
    public class ZxingQrScannerTests
    {
        // The QR payload for BLDG_A_ENTRANCE in sample_campus.json.
        private const string ExpectedPayload = "QR_BLDGA_ENTRANCE";

        [Test]
        public void TryDecodeLuminance_SampleQrImage_RaisesOnQrDecodedWithCorrectPayload()
        {
            byte[] luminance = GenerateQrLuminanceImage(ExpectedPayload, 200, 200, out int width, out int height);

            var go = new GameObject("ZxingQrScanner_Test");
            try
            {
                var scanner = go.AddComponent<ZxingQrScanner>();

                string decodedPayload = null;
                int eventFireCount = 0;
                scanner.OnQrDecoded += payload =>
                {
                    decodedPayload = payload;
                    eventFireCount++;
                };

                bool decoded = scanner.TryDecodeLuminance(luminance, width, height);

                Assert.IsTrue(decoded, "ZxingQrScanner failed to decode the generated QR image.");
                Assert.AreEqual(1, eventFireCount, "OnQrDecoded should fire exactly once.");
                Assert.AreEqual(ExpectedPayload, decodedPayload);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Encodes a QR code for <paramref name="text"/> using ZXing.Net's
        /// own writer, then flattens its BGRA pixel output into a
        /// single-channel grayscale luminance buffer — the same shape of
        /// data ZxingQrScanner.TryDecodeLuminance consumes from a real
        /// camera frame's Y plane. Since a QR render is pure black/white,
        /// any one BGRA channel already equals the luminance value.
        /// </summary>
        private static byte[] GenerateQrLuminanceImage(string text, int requestedWidth, int requestedHeight, out int width, out int height)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new EncodingOptions
                {
                    Width = requestedWidth,
                    Height = requestedHeight,
                },
            };

            PixelData qrImage = writer.Write(text);
            width = qrImage.Width;
            height = qrImage.Height;

            var luminance = new byte[width * height];
            byte[] bgra = qrImage.Pixels;
            for (int i = 0; i < luminance.Length; i++)
            {
                luminance[i] = bgra[i * 4];
            }
            return luminance;
        }
    }
}
