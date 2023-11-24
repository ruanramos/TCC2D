using UnityEngine;

public static class GameConstants
{
    // Camera
    public const int PlayerCameraFieldOfView = 75;
    public const float BaseObserverCameraMovespeed = 50f;
    

    // Game space
    public const int MinHorizontal = -45;
    public const int MaxHorizontal = 45;
    public const int MinVertical = -30;
    public const int MaxVertical = 60;

    // Collectibles
    public const int CollectibleValue = 1;
    public const int NumberOfCollectibles = 80;

    // Challenges
    public const int ChallengeValue = 50;


    // Layers
    public const int PlayerLayerNumber = 3;
    public const int InChallengePlayerLayerNumber = 6;

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
}