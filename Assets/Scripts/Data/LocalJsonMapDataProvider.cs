using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace CampusNav.Data
{
    /// <summary>
    /// Loads campus map data from a JSON file bundled in StreamingAssets.
    /// This is the only place in the project that knows the data comes from
    /// a local file — everything else talks to IMapDataProvider.
    ///
    /// Usage:
    ///   var provider = new LocalJsonMapDataProvider("sample_campus.json");
    ///   CampusData data = provider.GetCampusData();
    ///
    /// NOTE: On Android, files inside StreamingAssets live inside the .apk
    /// and can't be read with System.IO directly — they must go through
    /// UnityWebRequest. This class handles both cases so it behaves the same
    /// in the Editor, on iOS, and on Android.
    /// </summary>
    public class LocalJsonMapDataProvider : IMapDataProvider
    {
        private readonly string _fileName;
        private CampusData _cache;

        public LocalJsonMapDataProvider(string fileName)
        {
            _fileName = fileName;
        }

        public CampusData GetCampusData()
        {
            if (_cache != null) return _cache;

            string path = Path.Combine(Application.streamingAssetsPath, _fileName);

            string json;
            if (path.Contains("://") || path.Contains(":///"))
            {
                // Android (and WebGL) route: streamingAssetsPath is a URL,
                // must be read via UnityWebRequest. Blocking-wait here is
                // acceptable only for small JSON files at startup; prefer
                // GetCampusDataAsync in real game code.
                json = ReadViaUnityWebRequestSync(path);
            }
            else
            {
                json = File.ReadAllText(path);
            }

            _cache = JsonUtility.FromJson<CampusData>(json);
            if (_cache == null)
            {
                throw new Exception($"CampusNav: failed to parse map data from '{_fileName}'.");
            }
            return _cache;
        }

        public async Task<CampusData> GetCampusDataAsync()
        {
            if (_cache != null) return _cache;

            string path = Path.Combine(Application.streamingAssetsPath, _fileName);
            string json;

            using (var request = UnityWebRequest.Get(path))
            {
                var op = request.SendWebRequest();
                while (!op.isDone) await Task.Yield();

#if UNITY_2020_1_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
                if (request.isNetworkError || request.isHttpError)
#endif
                {
                    throw new Exception($"CampusNav: failed to load map data '{_fileName}': {request.error}");
                }
                json = request.downloadHandler.text;
            }

            _cache = JsonUtility.FromJson<CampusData>(json);
            return _cache;
        }

        private static string ReadViaUnityWebRequestSync(string url)
        {
            // Simple synchronous-looking wrapper for Editor/dev convenience.
            // On Android in production, prefer GetCampusDataAsync instead —
            // this blocks the main thread until the local file read completes.
            using (var request = UnityWebRequest.Get(url))
            {
                var op = request.SendWebRequest();
                while (!op.isDone) { /* spin — local file, resolves quickly */ }

#if UNITY_2020_1_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
                if (request.isNetworkError || request.isHttpError)
#endif
                {
                    throw new Exception($"CampusNav: failed to load map data: {request.error}");
                }
                return request.downloadHandler.text;
            }
        }
    }
}
