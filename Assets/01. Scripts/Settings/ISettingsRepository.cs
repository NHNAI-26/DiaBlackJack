namespace Border.Settings
{
    public interface ISettingsRepository
    {
        bool TryLoad(out GameSettingsSnapshot settings);
        bool TrySave(GameSettingsSnapshot settings);
    }
}
