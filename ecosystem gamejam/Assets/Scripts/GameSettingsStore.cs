public static class GameSettingsStore
{
    public static bool HasSelection { get; private set; }
    public static int DifficultyIndex { get; private set; }
    public static int StartingJarIndex { get; private set; }
    public static int TemperatureLevel { get; private set; }

    public static void Save(int difficultyIndex, int startingJarIndex, int temperatureLevel)
    {
        HasSelection = true;
        DifficultyIndex = difficultyIndex;
        StartingJarIndex = startingJarIndex;
        TemperatureLevel = temperatureLevel;
    }
}
