using System.Collections.Generic;

[System.Serializable]
public class PlayerTechData
{
    public List<string> unlockedNodeIDs = new List<string>();   // Only store IDs
    public int techPointsAvailable = 5;                         // Points player can spend
    public int totalTechPointsEarned = 5;                       // For progression
}