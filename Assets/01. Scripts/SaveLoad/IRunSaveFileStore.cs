namespace Border.SaveLoad
{
    public interface IRunSaveFileStore
    {
        bool Exists(string fileName);

        bool TryRead(string fileName, out string contents);

        bool TryWrite(string fileName, string contents);

        bool TryDelete(string fileName);

        bool TryMove(string sourceFileName, string destinationFileName, bool overwrite);
    }
}
