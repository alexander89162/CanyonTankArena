using System.Collections.Generic;

[System.Serializable]
public class PlayerSaveData
{
    //saves players items
    public List<ItemInstance> inventoryItems = new List<ItemInstance>();

    //saves players highscore
    public int highScore = 0;
}