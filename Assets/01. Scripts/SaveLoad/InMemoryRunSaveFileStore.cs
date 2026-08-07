using System.Collections.Generic;

namespace Border.SaveLoad
{
    /// <summary>
    /// A throwaway, process-local <see cref="IRunSaveFileStore"/> — never touches disk.
    /// Used by <c>StageProgressionRuntime.CreateTutorialInstance</c> so the scripted
    /// tutorial's reservation/checkpoint writes can never collide with or overwrite the
    /// player's real save file (which lives at a fixed path shared by every
    /// <see cref="SystemRunSaveFileStore"/> instance).
    /// </summary>
    internal sealed class InMemoryRunSaveFileStore : IRunSaveFileStore
    {
        private readonly Dictionary<string, string> _files =
            new Dictionary<string, string>();

        public bool Exists(string fileName)
        {
            return fileName != null && _files.ContainsKey(fileName);
        }

        public bool TryRead(string fileName, out string contents)
        {
            if (fileName == null)
            {
                contents = null;
                return false;
            }

            return _files.TryGetValue(fileName, out contents);
        }

        public bool TryWrite(string fileName, string contents)
        {
            if (fileName == null || contents == null)
            {
                return false;
            }

            _files[fileName] = contents;
            return true;
        }

        public bool TryDelete(string fileName)
        {
            if (fileName == null)
            {
                return false;
            }

            _files.Remove(fileName);
            return true;
        }

        public bool TryMove(
            string sourceFileName,
            string destinationFileName,
            bool overwrite)
        {
            if (sourceFileName == null ||
                destinationFileName == null ||
                !_files.TryGetValue(sourceFileName, out string contents))
            {
                return false;
            }

            if (!overwrite && _files.ContainsKey(destinationFileName))
            {
                return false;
            }

            _files.Remove(sourceFileName);
            _files[destinationFileName] = contents;
            return true;
        }
    }
}
