using System.Collections.Generic;

[System.Serializable]
public class PlayerSaveData
{
    //saves players items
    public List<ItemInstance> inventoryItems = new List<ItemInstance>();

    //saves players highscore
    public int highScore = 0;

    //saves players unlocked tech nodes
    public PlayerTechData techData = new PlayerTechData();
}