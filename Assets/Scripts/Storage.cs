//Storage.cs
using System.Collections.Generic;
using UnityEngine;

namespace HappyHarvest
{
    public class Storage
    {
        public List<InventorySlot> Content { get; private set; }

        public Storage()
        {
            Content = new List<InventorySlot>();
        }

        public void Store(InventorySlot entry)
        {
            //we won't have thousands of objects types stored, so there should be no performance problem on simply searching
            //for the key. But as usual : profile. If profiling show this to be massively underperformant, switch over to a
            //lookup data format like Dictionary.
            var idx = Content.FindIndex(inventoryEntry => inventoryEntry.Item.Key == entry.Item.Key);
            if (idx != -1)
            {
                Content[idx].StackSize += entry.StackSize;
            }
        }

        // Will return how much was actually retrieve (in case more than what's stored is asked)
        public int Retrieve(int contentIndex, int amount)
        {
            Debug.Assert(contentIndex < Content.Count, "Tried to retrieve a non existing entry from storage");

            int actualAmount = Mathf.Min(amount, Content[contentIndex].StackSize);

            Content[contentIndex].StackSize -= actualAmount;

            return actualAmount;
        }
    }
}
