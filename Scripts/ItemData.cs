using System;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Scriptable Objects/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemID; // Unique ID like "Apple", "Berry", "Mushroom"
    public string itemName;
    public Sprite icon;
    // Add more properties like description, stack size, etc.
}