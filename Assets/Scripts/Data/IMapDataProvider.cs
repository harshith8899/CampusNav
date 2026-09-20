namespace CampusNav.Data
{
    /// <summary>
    /// Everything downstream (search, pathfinding, mini-map) depends ONLY on
    /// this interface, never on how the data is actually loaded. Today it's
    /// bundled JSON (LocalJsonMapDataProvider). Later, swapping in a backend
    /// just means writing a RemoteApiMapDataProvider that implements this same
    /// interface — no other code changes.
    /// </summary>
    public interface IMapDataProvider
    {
        /// <summary>
        /// Loads (or returns cached) campus data. Synchronous for now since
        /// local JSON loads are fast; a remote provider would want an async
        /// version — see LoadAsync below for that future path.
        /// </summary>
        CampusData GetCampusData();

        /// <summary>
        /// Async variant. LocalJsonMapDataProvider just wraps the sync load;
        /// a future RemoteApiMapDataProvider would do a real network call
        /// here without changing any caller.
        /// </summary>
        System.Threading.Tasks.Task<CampusData> GetCampusDataAsync();
    }
}
