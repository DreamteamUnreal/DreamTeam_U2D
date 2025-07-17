// LevelGenerator.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class LevelGenerator : MonoBehaviour
{
    // Assign these references in the Inspector if they exist beforehand,
    // otherwise, the script will create them.
    public Tilemap groundTilemap;
    public Tilemap obstacleTilemap;
    public Tilemap interactableTilemap;

    [Header("Tile Definitions (Sprite names from Resources)")]
    public string grassTileSpriteName = "grass_tile";
    public string dirtTileSpriteName = "dirt_tile";
    public string wallTileSpriteName = "wall_tile";
    public string berryBushSpriteName = "berry_bush_tile"; // Example for interactable

    // Keep track of the actual Tile assets created at runtime
    private Tile grassTile;
    private Tile dirtTile;
    private Tile wallTile;
    private Tile berryBushTile;

    [Header("Level Dimensions")]
    public int levelWidth = 20;
    public int levelHeight = 15;

    [Header("Game Object Prefabs (Assign in Inspector)")]
    public GameObject playerPrefab;
    public GameObject squirrelPrefab;
    public GameObject applePrefab; // If apples are separate GameObjects, not tiles

    // A simple 2D array to define your level layout using characters
    // 'G' for Grass, 'D' for Dirt, 'W' for Wall, 'B' for Berry Bush, 'A' for Apple (if a tile), 'P' for Player Start
    private string[] levelLayout = new string[]
    {
        "WWWWWWWWWWWWWWWWWWWW",
        "WGGGGGGGGGGGGGGGGGGW",
        "WGDGGGAWWWWWWWWWGGDW",
        "WGGDGGBGGGGGGGGWAGGW",
        "WGGGGGGGGGGGGGGWGGGW",
        "WGGGGGGGGGGGGGGWGGGW",
        "WGGWGGGGGGGGGGGWGGGW",
        "WGGWGGGGGGGGGGGWGGGW",
        "WGGDGGBGGGGGGGGWGGGW",
        "WGGGGGGGGGGGGGGWGGGW",
        "WGGGGGGGGGGGGGGWGGGW",
        "WGDGGGAWWWWWWWWWGGDW",
        "WGGGGGGGGGGGGGGGGGGW",
        "WPGGGGGGGGGGGGGGGGGW", // P for Player Start
        "WWWWWWWWWWWWWWWWWWWW"
    };

    void Start()
    {
        // 1. Create Tilemaps if they don't exist
        SetupTilemaps();

        // 2. Load/Create Tile assets from sprites
        LoadTileAssets();

        // 3. Generate the level based on the layout array
        GenerateLevel();

        // 4. Optionally initialize other game elements
        SpawnPlayerAndAnimals();

		// For A* pathfinding manager
		// Ensure TileInteractionManager is aware of these newly created tilemaps
#pragma warning disable CS0618 // Type or member is obsolete
		TileInteractionManager tileInteractionManager = FindObjectOfType<TileInteractionManager>();
#pragma warning restore CS0618 // Type or member is obsolete
		if (tileInteractionManager != null)
        {
            tileInteractionManager.groundTilemap = groundTilemap;
            tileInteractionManager.obstacleTilemap = obstacleTilemap;
            tileInteractionManager.interactableTilemap = interactableTilemap;
            Debug.Log("LevelGenerator: TileInteractionManager updated with generated tilemaps.");
        }
        else
        {
            Debug.LogError("LevelGenerator: TileInteractionManager not found in scene. A* pathfinding may not work correctly.");
        }
    }

    void SetupTilemaps()
    {
        GameObject gridObject = GameObject.Find("Grid");
        if (gridObject == null)
        {
            gridObject = new GameObject("Grid");
            gridObject.AddComponent<Grid>();
        }

        // Create ground tilemap
        if (groundTilemap == null)
        {
            GameObject tmObject = new GameObject("GroundTilemap");
            tmObject.transform.SetParent(gridObject.transform);
            groundTilemap = tmObject.AddComponent<Tilemap>();
            groundTilemap.gameObject.AddComponent<TilemapRenderer>();
            // Renderer properties (sorting order, material, etc.)
            TilemapRenderer renderer = groundTilemap.GetComponent<TilemapRenderer>();
            renderer.sortingLayerName = "Ground"; // Make sure you have a "Ground" sorting layer
            renderer.sortingOrder = 0;
        }

        // Create obstacle tilemap
        if (obstacleTilemap == null)
        {
            GameObject tmObject = new GameObject("ObstacleTilemap");
            tmObject.transform.SetParent(gridObject.transform);
            obstacleTilemap = tmObject.AddComponent<Tilemap>();
            obstacleTilemap.gameObject.AddComponent<TilemapRenderer>();
            TilemapRenderer renderer = obstacleTilemap.GetComponent<TilemapRenderer>();
            renderer.sortingLayerName = "Obstacles"; // Make sure you have an "Obstacles" sorting layer
            renderer.sortingOrder = 1; // Render above ground
            // Add a TilemapCollider2D for obstacles (important for A* IsCellPassable if it checks colliders)
            // Might manage this differently if IsCellPassable only checks HasTile on obstacleTilemap
            // obstacleTilemap.gameObject.AddComponent<TilemapCollider2D>();
        }

        // Create interactable tilemap (for tiles like berry bushes)
        if (interactableTilemap == null)
        {
            GameObject tmObject = new GameObject("InteractableTilemap");
            tmObject.transform.SetParent(gridObject.transform);
            interactableTilemap = tmObject.AddComponent<Tilemap>();
            interactableTilemap.gameObject.AddComponent<TilemapRenderer>();
            TilemapRenderer renderer = interactableTilemap.GetComponent<TilemapRenderer>();
            renderer.sortingLayerName = "Interactables"; // Make sure you have an "Interactables" sorting layer
            renderer.sortingOrder = 2; // Render above ground and obstacles
        }

        Debug.Log("LevelGenerator: Tilemaps setup complete.");
    }

    void LoadTileAssets()
    {
        // Grass Tile
        Sprite grassSprite = SpriteLoader.GetSprite(grassTileSpriteName);
        if (grassSprite != null)
        {
            grassTile = ScriptableObject.CreateInstance<Tile>();
            grassTile.sprite = grassSprite;
        }

        // Dirt Tile
        Sprite dirtSprite = SpriteLoader.GetSprite(dirtTileSpriteName);
        if (dirtSprite != null)
        {
            dirtTile = ScriptableObject.CreateInstance<Tile>();
            dirtTile.sprite = dirtSprite;
        }

        // Wall Tile
        Sprite wallSprite = SpriteLoader.GetSprite(wallTileSpriteName);
        if (wallSprite != null)
        {
            wallTile = ScriptableObject.CreateInstance<Tile>();
            wallTile.sprite = wallSprite;
            // Optionally set wall tiles to block pathfinding directly if they are on obstacle map
            // wallTile.colliderType = Tile.ColliderType.Grid; // If you add TilemapCollider2D
        }

        // Berry Bush Tile
        Sprite berryBushSprite = SpriteLoader.GetSprite(berryBushSpriteName);
        if (berryBushSprite != null)
        {
            berryBushTile = ScriptableObject.CreateInstance<Tile>();
            berryBushTile.sprite = berryBushSprite;
        }

        Debug.Log("LevelGenerator: Tile assets loaded/created.");
    }

    void GenerateLevel()
    {
        for (int y = 0; y < levelHeight; y++)
        {
            // Reverse y for array indexing vs. Unity's bottom-left origin for tilemaps
            string row = levelLayout[levelHeight - 1 - y]; 
            for (int x = 0; x < levelWidth; x++)
            {
                Vector3Int cellPosition = new Vector3Int(x, y, 0);
                char tileChar = row[x];

                // Default to grass for any unhandled character or background
                groundTilemap.SetTile(cellPosition, grassTile);

                switch (tileChar)
                {
                    case 'D':
                        groundTilemap.SetTile(cellPosition, dirtTile);
                        break;
                    case 'W':
                        obstacleTilemap.SetTile(cellPosition, wallTile);
                        break;
                    case 'B':
                        interactableTilemap.SetTile(cellPosition, berryBushTile);
                        // Make sure berry bush tiles are passable for movement
                        // For gameplay, you might want to remove them after interaction
                        break;
                    case 'P':
                        // Player will be spawned here
                        // Set ground tile under player start
                        groundTilemap.SetTile(cellPosition, grassTile);
                        break;
                    // Add more cases for other tile types (e.g., A for Apple if it's a tile)
                }
            }
        }
        Debug.Log("LevelGenerator: Level tiles generated.");
    }

    void SpawnPlayerAndAnimals()
    {
        bool playerSpawned = false;
        for (int y = 0; y < levelHeight; y++)
        {
            string row = levelLayout[levelHeight - 1 - y]; // Reverse y for array indexing
            for (int x = 0; x < levelWidth; x++)
            {
                char tileChar = row[x];
                Vector3 worldPos = groundTilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));

                if (tileChar == 'P' && playerPrefab != null && !playerSpawned)
                {
                    Instantiate(playerPrefab, worldPos, Quaternion.identity);
                    playerSpawned = true;
                    Debug.Log($"LevelGenerator: Player spawned at {worldPos}");
                }
                // Example: Spawn a squirrel on a grass tile near the start
                else if (tileChar == 'G' && squirrelPrefab != null && Random.value < 0.05f) // 5% chance on grass
                {
                    Instantiate(squirrelPrefab, worldPos, Quaternion.identity);
                }
                 // Example: Spawn an apple if it's a separate GameObject
                else if (tileChar == 'A' && applePrefab != null && Random.value < 0.5f) // 50% chance on 'A'
                {
                    Instantiate(applePrefab, worldPos, Quaternion.identity);
                }
            }
        }
        if (!playerSpawned) Debug.LogWarning("LevelGenerator: Player spawn point 'P' not found or player prefab not assigned!");
        Debug.Log("LevelGenerator: Player and animals spawned.");
    }
}