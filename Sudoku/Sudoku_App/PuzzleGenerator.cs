using System;
using System.Collections.Generic;
using System.Linq;

namespace Sudoku_App
{
    public static class PuzzleGenerator
    {
        private static Random random = new Random();
        
        /// <summary>
        /// Generate a Sudoku puzzle with a unique solution based on difficulty
        /// </summary>
        public static int[,] GeneratePuzzle(string difficulty)
        {
            // Use date-based seed for daily puzzles, or random
            int seed = DateTime.Now.Year * 10000 + DateTime.Now.Month * 100 + DateTime.Now.Day;
            return GeneratePuzzleWithSeed(difficulty, seed);
        }
        
        /// <summary>
        /// Generate a Sudoku puzzle with a specific seed for reproducibility
        /// </summary>
        public static int[,] GeneratePuzzleWithSeed(string difficulty, int seed)
        {
            random = new Random(seed);
            
            // First, generate a complete solved Sudoku
            int[,] solution = GenerateCompleteSudoku();
            
            // Then remove numbers based on difficulty (ensuring unique solution)
            int cellsToRemove = difficulty.ToLower() switch
            {
                "easy" => 30,      // Remove 30 cells (51 filled) - Very beginner friendly
                "medium" => 40,    // Remove 40 cells (41 filled) - Comfortable  
                "hard" => 50,      // Remove 50 cells (31 filled) - Challenging
                "expert" => 55,    // Remove 55 cells (26 filled) - Advanced
                "master" => 60,    // Remove 60 cells (21 filled) - Expert level
                "extreme" => 64,   // Remove 64 cells (17 filled) - Extreme difficulty
                _ => 40
            };
            
            int[,] puzzle = RemoveCellsWithUniqueSolution(solution, cellsToRemove);
            
            return puzzle;
        }
        
        /// <summary>
        /// Generate a complete, solved Sudoku grid
        /// </summary>
        private static int[,] GenerateCompleteSudoku()
        {
            int[,] grid = new int[9, 9];
            
            // Fill diagonal 3x3 boxes first (they don't depend on each other)
            FillDiagonalBoxes(grid);
            
            // Fill remaining cells using backtracking
            SolveSudoku(grid, 0, 0);
            
            return grid;
        }
        
        /// <summary>
        /// Fill the three 3x3 boxes along the diagonal
        /// </summary>
        private static void FillDiagonalBoxes(int[,] grid)
        {
            for (int box = 0; box < 9; box += 3)
            {
                FillBox(grid, box, box);
            }
        }
        
        /// <summary>
        /// Fill a single 3x3 box with random numbers 1-9
        /// </summary>
        private static void FillBox(int[,] grid, int row, int col)
        {
            List<int> numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            ShuffleList(numbers);
            
            int index = 0;
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    grid[row + r, col + c] = numbers[index++];
                }
            }
        }
        
        /// <summary>
        /// Solve Sudoku using backtracking algorithm
        /// </summary>
        private static bool SolveSudoku(int[,] grid, int row, int col)
        {
            // Move to next row if we've reached the end of current row
            if (col == 9)
            {
                row++;
                col = 0;
            }
            
            // If we've filled all rows, puzzle is solved
            if (row == 9)
                return true;
            
            // If cell is already filled, move to next cell
            if (grid[row, col] != 0)
                return SolveSudoku(grid, row, col + 1);
            
            // Try numbers 1-9 in random order
            List<int> numbers = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            ShuffleList(numbers);
            
            foreach (int num in numbers)
            {
                if (IsValidPlacement(grid, row, col, num))
                {
                    grid[row, col] = num;
                    
                    if (SolveSudoku(grid, row, col + 1))
                        return true;
                    
                    grid[row, col] = 0; // Backtrack
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Check if a number can be placed at a given position
        /// </summary>
        private static bool IsValidPlacement(int[,] grid, int row, int col, int num)
        {
            // Check row
            for (int c = 0; c < 9; c++)
            {
                if (grid[row, c] == num)
                    return false;
            }
            
            // Check column
            for (int r = 0; r < 9; r++)
            {
                if (grid[r, col] == num)
                    return false;
            }
            
            // Check 3x3 box
            int boxRow = (row / 3) * 3;
            int boxCol = (col / 3) * 3;
            
            for (int r = boxRow; r < boxRow + 3; r++)
            {
                for (int c = boxCol; c < boxCol + 3; c++)
                {
                    if (grid[r, c] == num)
                        return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Remove cells from a solved Sudoku while maintaining unique solution
        /// </summary>
        private static int[,] RemoveCellsWithUniqueSolution(int[,] solution, int cellsToRemove)
        {
            int[,] puzzle = (int[,])solution.Clone();
            
            // Create list of all cell positions
            List<(int row, int col)> positions = new List<(int, int)>();
            for (int r = 0; r < 9; r++)
            {
                for (int c = 0; c < 9; c++)
                {
                    positions.Add((r, c));
                }
            }
            
            // Shuffle positions for random removal
            ShuffleList(positions);
            
            int removed = 0;
            int attempts = 0;
            int maxAttempts = cellsToRemove * 3; // Allow some failed attempts
            
            foreach (var (row, col) in positions)
            {
                if (removed >= cellsToRemove || attempts >= maxAttempts)
                    break;
                
                attempts++;
                
                int backup = puzzle[row, col];
                puzzle[row, col] = 0;
                
                // Check if puzzle still has unique solution
                int[,] testPuzzle = (int[,])puzzle.Clone();
                int solutionCount = CountSolutions(testPuzzle, 0, 0, 0);
                
                if (solutionCount == 1)
                {
                    // Good! This removal maintains unique solution
                    removed++;
                }
                else
                {
                    // Multiple solutions or no solution - restore the cell
                    puzzle[row, col] = backup;
                }
            }
            
            return puzzle;
        }
        
        /// <summary>
        /// Count the number of solutions a puzzle has (max 2 for efficiency)
        /// </summary>
        private static int CountSolutions(int[,] grid, int row, int col, int count)
        {
            if (count > 1)
                return count; // Early exit if we find multiple solutions
            
            // Move to next row if we've reached the end of current row
            if (col == 9)
            {
                row++;
                col = 0;
            }
            
            // If we've filled all rows, we found a solution
            if (row == 9)
                return count + 1;
            
            // If cell is already filled, move to next cell
            if (grid[row, col] != 0)
                return CountSolutions(grid, row, col + 1, count);
            
            // Try numbers 1-9
            for (int num = 1; num <= 9; num++)
            {
                if (IsValidPlacement(grid, row, col, num))
                {
                    grid[row, col] = num;
                    count = CountSolutions(grid, row, col + 1, count);
                    grid[row, col] = 0; // Backtrack
                    
                    if (count > 1)
                        return count; // Early exit
                }
            }
            
            return count;
        }
        
        /// <summary>
        /// Shuffle a list using Fisher-Yates algorithm
        /// </summary>
        private static void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
