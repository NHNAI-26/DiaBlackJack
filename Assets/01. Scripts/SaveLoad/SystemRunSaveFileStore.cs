using System;
using System.IO;
using UnityEngine;

namespace Border.SaveLoad
{
    public sealed class SystemRunSaveFileStore : IRunSaveFileStore
    {
        private readonly string _rootPath;

        public SystemRunSaveFileStore()
            : this(Application.persistentDataPath)
        {
        }

        internal SystemRunSaveFileStore(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new ArgumentException("Run save root path cannot be empty.", nameof(rootPath));
            }

            _rootPath = Path.GetFullPath(rootPath);
        }

        public bool Exists(string fileName)
        {
            return TryResolve(fileName, out string fullPath) && File.Exists(fullPath);
        }

        public bool TryRead(string fileName, out string contents)
        {
            contents = null;
            if (!TryResolve(fileName, out string fullPath) || !File.Exists(fullPath))
            {
                return false;
            }

            try
            {
                contents = File.ReadAllText(fullPath);
                return true;
            }
            catch (Exception)
            {
                contents = null;
                return false;
            }
        }

        public bool TryWrite(string fileName, string contents)
        {
            if (contents == null || !TryResolve(fileName, out string fullPath))
            {
                return false;
            }

            try
            {
                Directory.CreateDirectory(_rootPath);
                File.WriteAllText(fullPath, contents);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool TryDelete(string fileName)
        {
            if (!TryResolve(fileName, out string fullPath))
            {
                return false;
            }

            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool TryMove(
            string sourceFileName,
            string destinationFileName,
            bool overwrite)
        {
            if (!TryResolve(sourceFileName, out string sourcePath) ||
                !TryResolve(destinationFileName, out string destinationPath) ||
                string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(sourcePath))
            {
                return false;
            }

            try
            {
                if (File.Exists(destinationPath))
                {
                    if (!overwrite)
                    {
                        return false;
                    }

                    File.Replace(sourcePath, destinationPath, null);
                }
                else
                {
                    File.Move(sourcePath, destinationPath);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool TryResolve(string fileName, out string fullPath)
        {
            fullPath = null;
            if (string.IsNullOrWhiteSpace(fileName) ||
                Path.IsPathRooted(fileName) ||
                !string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal) ||
                fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                return false;
            }

            try
            {
                string candidate = Path.GetFullPath(Path.Combine(_rootPath, fileName));
                string rootPrefix = _rootPath.TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                if (!candidate.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                fullPath = candidate;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
