namespace Faultline.Persistence
{
    using Faultline.Domain;

    /// <summary>
    /// Persistence service contract: save/load/reset scenario session state.
    ///
    /// Pure C# — no UnityEngine. The concrete file path is exposed as a
    /// string property so implementations can be unit-tested without Unity
    /// (the Unity-backed implementation points it at
    /// Application.persistentDataPath).
    ///
    /// All methods return <see cref="Faultline.Domain.Result{T}"/> so
    /// failures never leak raw exceptions across module boundaries.
    /// </summary>
    public interface IPersistenceService
    {
        /// <summary>
        /// Filesystem path the save file lives at. Abstracted as a string so
        /// the service is testable without UnityEngine.
        /// </summary>
        string SaveFilePath { get; }

        /// <summary>
        /// Persist the given session state atomically.
        /// </summary>
        /// <param name="state">Session state to save.</param>
        /// <returns>Success, or an FL-PS-* failure.</returns>
        Result<bool> Save(SessionState state);

        /// <summary>
        /// Load session state from disk. Returns default/empty state (never
        /// throws) when the file is missing or corrupted.
        /// </summary>
        /// <returns>Loaded state on success; default/empty state with a
        /// failure result when data is missing/corrupt.</returns>
        Result<SessionState> Load();

        /// <summary>
        /// Delete any persisted save data and return the service to a clean
        /// state.
        /// </summary>
        /// <returns>Success, or an FL-PS-* failure.</returns>
        Result<bool> Reset();

        /// <summary>
        /// Whether a valid save file exists on disk.
        /// </summary>
        bool HasSaveData();
    }
}
