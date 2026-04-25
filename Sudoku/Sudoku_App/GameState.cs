using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sudoku_App
{
    public class GameStateData
    {
        public int[,] Board { get; set; }
        public bool[,] IsFixed { get; set; }
        public HashSet<int>[,] Notes { get; set; }
        public string Difficulty { get; set; }
        public int Mistakes { get; set; }
        public int Score { get; set; }
        public int HintsRemaining { get; set; }
        public int ElapsedSeconds { get; set; }
        public DateTime SaveTime { get; set; }
        public List<MoveHistoryItem> MoveHistory { get; set; }
    }

    public class MoveHistoryItem
    {
        public int Row { get; set; }
        public int Col { get; set; }
        public int OldValue { get; set; }
        public int NewValue { get; set; }
    }

    public static class GameState
    {
        private static string GetSaveFilePath()
        {
            var appDataPath = FileSystem.AppDataDirectory;
            return Path.Combine(appDataPath, "sudoku_save.json");
        }

        public static async Task<bool> HasSavedGame()
        {
            try
            {
                var filePath = GetSaveFilePath();
                return File.Exists(filePath);
            }
            catch
            {
                return false;
            }
        }

        public static async Task SaveGame(GameStateData gameState)
        {
            try
            {
                var filePath = GetSaveFilePath();
                
                // Convert 2D arrays and HashSet arrays to serializable format
                var serializableState = new
                {
                    Board = ConvertToJaggedArray(gameState.Board),
                    IsFixed = ConvertToJaggedArray(gameState.IsFixed),
                    Notes = ConvertNotesToJaggedArray(gameState.Notes),
                    gameState.Difficulty,
                    gameState.Mistakes,
                    gameState.Score,
                    gameState.HintsRemaining,
                    gameState.ElapsedSeconds,
                    gameState.SaveTime,
                    gameState.MoveHistory
                };

                var json = JsonSerializer.Serialize(serializableState, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving game: {ex.Message}");
            }
        }

        public static async Task<GameStateData> LoadGame()
        {
            try
            {
                var filePath = GetSaveFilePath();
                
                if (!File.Exists(filePath))
                    return null;

                var json = await File.ReadAllTextAsync(filePath);
                var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var gameState = new GameStateData
                {
                    Board = ConvertToRectangularArray<int>(root.GetProperty("Board")),
                    IsFixed = ConvertToRectangularArray<bool>(root.GetProperty("IsFixed")),
                    Notes = ConvertToNotesArray(root.GetProperty("Notes")),
                    Difficulty = root.GetProperty("Difficulty").GetString(),
                    Mistakes = root.GetProperty("Mistakes").GetInt32(),
                    Score = root.GetProperty("Score").GetInt32(),
                    HintsRemaining = root.GetProperty("HintsRemaining").GetInt32(),
                    ElapsedSeconds = root.GetProperty("ElapsedSeconds").GetInt32(),
                    SaveTime = root.GetProperty("SaveTime").GetDateTime(),
                    MoveHistory = JsonSerializer.Deserialize<List<MoveHistoryItem>>(
                        root.GetProperty("MoveHistory").GetRawText())
                };

                return gameState;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading game: {ex.Message}");
                return null;
            }
        }

        public static async Task ClearSavedGame()
        {
            try
            {
                var filePath = GetSaveFilePath();
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error clearing saved game: {ex.Message}");
            }
        }

        // Helper methods for conversion
        private static T[][] ConvertToJaggedArray<T>(T[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            T[][] jagged = new T[rows][];

            for (int i = 0; i < rows; i++)
            {
                jagged[i] = new T[cols];
                for (int j = 0; j < cols; j++)
                {
                    jagged[i][j] = array[i, j];
                }
            }

            return jagged;
        }

        private static List<int>[][] ConvertNotesToJaggedArray(HashSet<int>[,] notes)
        {
            int rows = notes.GetLength(0);
            int cols = notes.GetLength(1);
            List<int>[][] jagged = new List<int>[rows][];

            for (int i = 0; i < rows; i++)
            {
                jagged[i] = new List<int>[cols];
                for (int j = 0; j < cols; j++)
                {
                    jagged[i][j] = new List<int>(notes[i, j]);
                }
            }

            return jagged;
        }

        private static T[,] ConvertToRectangularArray<T>(JsonElement element)
        {
            var jagged = JsonSerializer.Deserialize<T[][]>(element.GetRawText());
            int rows = jagged.Length;
            int cols = jagged[0].Length;
            T[,] rect = new T[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    rect[i, j] = jagged[i][j];
                }
            }

            return rect;
        }

        private static HashSet<int>[,] ConvertToNotesArray(JsonElement element)
        {
            var jagged = JsonSerializer.Deserialize<List<int>[][]>(element.GetRawText());
            int rows = jagged.Length;
            int cols = jagged[0].Length;
            HashSet<int>[,] notes = new HashSet<int>[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    notes[i, j] = new HashSet<int>(jagged[i][j]);
                }
            }

            return notes;
        }
    }
}
