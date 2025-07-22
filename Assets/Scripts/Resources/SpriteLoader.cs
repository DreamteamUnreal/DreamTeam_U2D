// SpriteLoader.cs
using UnityEngine;
using System.Collections.Generic;

public class SpriteLoader : MonoBehaviour
{
    private static Dictionary<string, Sprite> loadedSprites = new Dictionary<string, Sprite>();

    /// <summary>
    /// Loads a sprite from a Resources folder and caches it.
    /// </summary>
    /// <param name="spriteName">The name of the sprite file (without extension) in a Resources folder.</param>
    /// <returns>The loaded Sprite, or null if not found.</returns>
    public static Sprite GetSprite(string spriteName)
    {
        if (loadedSprites.ContainsKey(spriteName))
        {
            return loadedSprites[spriteName];
        }

        Sprite loadedSprite = Resources.Load<Sprite>(spriteName);
        if (loadedSprite == null)
        {
            Debug.LogError($"SpriteLoader: Sprite '{spriteName}' not found in Resources folder.");
            return null;
        }

        loadedSprites.Add(spriteName, loadedSprite);
        return loadedSprite;
    }
}