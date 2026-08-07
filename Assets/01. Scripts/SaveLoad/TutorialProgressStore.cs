using System;

namespace Border.SaveLoad
{
    /// <summary>
    /// Tracks whether the player has ever started the scripted first-play tutorial,
    /// persisted across runs — unlike <see cref="RunSaveEnvelope"/>/<see cref="RunReservation"/>,
    /// which are scoped to a single run and cleared whenever a new run starts. Presence of the
    /// marker file is the flag itself; there is no content to parse for a single boolean.
    /// </summary>
    public sealed class TutorialProgressStore
    {
        private const string FileName = "tutorial_progress.marker";

        private readonly IRunSaveFileStore _fileStore;

        public TutorialProgressStore(IRunSaveFileStore fileStore)
        {
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        }

        public bool HasSeenTutorial => _fileStore.Exists(FileName);

        public bool TryMarkSeen()
        {
            return _fileStore.TryWrite(FileName, "1");
        }
    }
}
