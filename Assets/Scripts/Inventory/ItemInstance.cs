using UnityEngine;

[System.Serializable]
public class ItemInstance
{
    public string itemID;
    public int quantity = 1;

    public ItemInstance(string id, int qty = 1) { 
        itemID = id; 
        quantity = Mathf.Max(1, qty); 
        }
    public ItemInstance(ItemSO so, int qty = 1) : this(so.itemID, qty) { }

    public ItemSO GetItemSO() => ItemDatabase.Instance.GetItemByID(itemID);
}
