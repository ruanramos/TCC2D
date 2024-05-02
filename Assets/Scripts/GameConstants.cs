using UnityEngine;

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
    public const float ChallengeStartDelayInSeconds = 3f;
    public const float PostChallengeInvincibilityTimeInSeconds = 3f;
    public const float PostChallengeSpeedMultiplier = 2f;
    public const float PlayerAlphaWhileInChallenge = 0.4f;
    public const float ChallengeWinnerTime = 5f;
    public const int ChallengeTimeoutLimitInSeconds = 8;

    public static readonly Color ScreenFlashGreen = new(0f, 255f, 0f, 0.4f);
    public static readonly Color ScreenFlashRed = new(255f, 0f, 0f, 0.4f);
    public const float ColorFlashTime = 0.1f;
    public const float RandomTimeSugarWindow = 1.5f;
    public const int MaxPressesAllowed = 1;

    public const string InnerCanvasTitleKeyboardPressChallenge = "Press the space bar faster than your opponent!"; 
    public const string InnerCanvasTitleQuestionChallenge = "Answer the question faster than your opponent!"; 
    

    public enum ChallengeType
    {
        KeyboardButtonPress,
        QuestionChallenge,
        Random
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
    public const int MaximumPlayersToDisplayScore = 3;

    // Debug
    public const bool DebugMode = true;

    // Tags
    public const string PlayerTag = "Player";
    public const string CollectibleTag = "Collectible";
}