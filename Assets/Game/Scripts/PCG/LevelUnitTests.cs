/*---------------------------
File: LevelUnitTests.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    // Unit test script to test Level functionality
    // Attach to a GameObject in the scene and results are outputted to the console.
    public class LevelUnitTests : MonoBehaviour
    {
        [ContextMenu("Run Level Unit Tests")]
        public void RunTests()
        {
            Debug.Log("Starting Level unit tests...");

            // Test 1: Basic construction and properties
            Level level = new();
            Debug.Assert(level.Width == 64, "Width should be 64");
            Debug.Assert(level.Length == 64, "Length should be 64");
            Debug.Log("Test 1 passed: Basic construction and properties");

            // Test 2: GetTileType/SetTileType operations
            level.SetTileType(5, 4, TileType.kFloor);
            TileType tile = level.GetTileType(5, 4);
            Debug.Assert(tile == TileType.kFloor, "Tile at (5,4) should be kFloor");
            Debug.Log("Test 2 passed: GetTileType/SetTileType operations");

            // Test 3: TryGetTileType
            bool success = level.TryGetTileType(5, 4, out TileType retrievedTile);
            Debug.Assert(success, "TryGetTileType should return true for valid coordinates");
            Debug.Assert(retrievedTile == TileType.kFloor, "TryGetTileType should return correct tile");
            success = level.TryGetTileType(100, 100, out _);
            Debug.Assert(!success, "TryGetTileType should return false for out of bounds coordinates");
            Debug.Log("Test 3 passed: TryGetTileType");

            // Test 4: InBounds
            Debug.Assert(level.InBounds(0, 0), "Origin should be in bounds");
            Debug.Assert(level.InBounds(level.Width - 1, level.Length - 1), "Max valid coordinate should be in bounds");
            Debug.Assert(!level.InBounds(level.Width, level.Length), "Out of bounds coordinate should return false");
            Debug.Assert(!level.InBounds(-1, -1), "Negative coordinate should return false");
            Debug.Log("Test 4 passed: InBounds");

            // Test 5: Fill
            level.Fill(TileType.kWall);
            for (int x = 0; x < level.Width; x++)
            {
                for (int y = 0; y < level.Length; y++)
                {
                    Debug.Assert(level.GetTileType(x, y) == TileType.kWall, $"Tile at ({x},{y}) should be kWall after Fill");
                }
            }
            Debug.Log("Test 5 passed: Fill");

            // Test 6: DrawRect
            level.Fill(TileType.kEmpty);
            level.DrawRect(new RectInt(2, 2, 3, 3), TileType.kFloor);
            Debug.Assert(level.GetTileType(2, 2) == TileType.kFloor, "Rectangle corner should be kFloor");
            Debug.Assert(level.GetTileType(4, 4) == TileType.kFloor, "Rectangle opposite corner should be kFloor");
            Debug.Assert(level.GetTileType(0, 0) == TileType.kEmpty, "Outside rectangle should remain kEmpty");
            Debug.Log("Test 6 passed: DrawRect");

            // Test 7: DrawLine
            level.Fill(TileType.kEmpty);
            level.DrawLine(new Vector2Int(0, 0), new Vector2Int(5, 5), TileType.kFloor);
            Debug.Assert(level.GetTileType(0, 0) == TileType.kFloor, "Line start should be kFloor");
            Debug.Assert(level.GetTileType(5, 5) == TileType.kFloor, "Line end should be kFloor");
            Debug.Log("Test 7 passed: DrawLine");

            // Test 8: ToArray
            TileType[,] array = level.ToArray();
            Debug.Assert(array.GetLength(0) == level.Width, "Array width should match level width");
            Debug.Assert(array.GetLength(1) == level.Length, "Array length should match level length");
            Debug.Log("Test 8 passed: ToArray");

            // Test 9: ToTexture
            Texture2D texture = level.ToTexture(tileType => tileType.GetColor());
            Debug.Assert(texture != null, "ToTexture should return a valid texture");
            Debug.Assert(texture.width == level.Width, "Texture width should match level width");
            Debug.Assert(texture.height == level.Length, "Texture height should match level length");
            Debug.Log("Test 9 passed: ToTexture");

            Debug.Log("All Level unit tests passed!");
        }
    }
}