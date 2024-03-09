public static class GameConstants
{
    // Camera
    public const float CameraHeight = -10;
    public const float BaseObserverCameraMovespeed = 50f;


    // Game space
    public const int MinHorizontal = -45;
    public const int MaxHorizontal = 45;
    public const int MinVertical = -45;
    public const int MaxVertical = 45;

    // Collectibles
    public const int CollectibleValue = 1;
    public const int NumberOfCollectibles = 80;

    // Challenges
    public const int ChallengeValue = 50;
    public const float PostChallengeInvincibilityTimeInSeconds = 3f;
    public const float PostChallengeSpeedMultiplier = 2f;
    public const float PlayerAlphaWhileInChallenge = 0.4f;
    public const float ChallengeWinnerTime = 3f;
    

    public enum ChallengeType
    {
        ButtonPress,
        KeyboardButtonPress,
        Simulation
    }

    // Layers
    public enum Layer
    {
        Player = 3,
        InChallengePlayer = 6
    }

    // Players
    public const int StartingLives = 3;
    public const int MaxLives = 5;
    public const float BaseMovespeed = 10f;

    // Game
    public const bool MaximumGamePointsCapped = false;
    public const int MaximumGamePointsCap = 5000;

    // Debug
    public const bool DebugMode = true;
    public const int ChallengeSimulationTimeInSeconds = 10;

    // Tags
    public const string PlayerTag = "Player";
    public const string CollectibleTag = "Collectible";
}