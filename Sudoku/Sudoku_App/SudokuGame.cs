using System;
using System.Collections.Generic;
using System.Linq;

namespace Sudoku_App
{
    public class SudokuGame
    {
        private int[,] board;
        private int[,] solution;
        private bool[,] isFixed; // tracks which cells are pre-filled
        
        public int[,] Board => board;
        public bool[,] IsFixed => isFixed;
        
        public SudokuGame()
        {
            board = new int[9, 9];
            solution = new int[9, 9];
            isFixed = new bool[9, 9];
        }
        
        // Initialize with a pre-made puzzle (you can expand this to generate puzzles)
        public void LoadPuzzle(int[,] puzzle)
        {
            if (puzzle.GetLength(0) != 9 || puzzle.GetLength(1) != 9)
                throw new ArgumentException("Puzzle must be 9x9");
            
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    board[row, col] = puzzle[row, col];
                    isFixed[row, col] = puzzle[row, col] != 0;
                }
            }
            
            // For now, we'll just copy the board as solution
            // In a real app, you'd solve the puzzle or store the solution
            Array.Copy(board, solution, board.Length);
        }

        // Load a saved game state
        public void LoadPuzzleState(int[,] boardState, bool[,] fixedState)
        {
            if (boardState.GetLength(0) != 9 || boardState.GetLength(1) != 9)
                throw new ArgumentException("Board must be 9x9");
            
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    board[row, col] = boardState[row, col];
                    isFixed[row, col] = fixedState[row, col];
                }
            }
            
            Array.Copy(board, solution, board.Length);
        }
        
        // Set a number at a specific cell
        public bool SetCell(int row, int col, int value)
        {
            if (row < 0 || row >= 9 || col < 0 || col >= 9)
                return false;
            
            if (isFixed[row, col])
                return false; // Can't change pre-filled cells
            
            if (value < 0 || value > 9)
                return false;
            
            board[row, col] = value;
            return true;
        }
        
        // Get the value at a specific cell
        public int GetCell(int row, int col)
        {
            if (row < 0 || row >= 9 || col < 0 || col >= 9)
                return 0;
            
            return board[row, col];
        }
        
        // Check if a number placement is valid (no conflicts)
        public bool IsValidPlacement(int row, int col, int value)
        {
            if (value == 0) return true; // Empty cell is always valid
            
            // Check row
            for (int c = 0; c < 9; c++)
            {
                if (c != col && board[row, c] == value)
                    return false;
            }
            
            // Check column
            for (int r = 0; r < 9; r++)
            {
                if (r != row && board[r, col] == value)
                    return false;
            }
            
            // Check 3x3 box
            int boxRow = (row / 3) * 3;
            int boxCol = (col / 3) * 3;
            
            for (int r = boxRow; r < boxRow + 3; r++)
            {
                for (int c = boxCol; c < boxCol + 3; c++)
                {
                    if ((r != row || c != col) && board[r, c] == value)
                        return false;
                }
            }
            
            return true;
        }
        
        // Get all cells that conflict with a specific cell
        public List<(int row, int col)> GetConflicts(int row, int col)
        {
            var conflicts = new List<(int, int)>();
            int value = board[row, col];
            
            if (value == 0) return conflicts;
            
            // Check row conflicts
            for (int c = 0; c < 9; c++)
            {
                if (c != col && board[row, c] == value)
                    conflicts.Add((row, c));
            }
            
            // Check column conflicts
            for (int r = 0; r < 9; r++)
            {
                if (r != row && board[r, col] == value)
                    conflicts.Add((r, col));
            }
            
            // Check 3x3 box conflicts
            int boxRow = (row / 3) * 3;
            int boxCol = (col / 3) * 3;
            
            for (int r = boxRow; r < boxRow + 3; r++)
            {
                for (int c = boxCol; c < boxCol + 3; c++)
                {
                    if ((r != row || c != col) && board[r, c] == value)
                    {
                        if (!conflicts.Contains((r, c)))
                            conflicts.Add((r, c));
                    }
                }
            }
            
            return conflicts;
        }
        
        // Check if the puzzle is completely solved
        public bool IsSolved()
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (board[row, col] == 0)
                        return false;
                    
                    if (!IsValidPlacement(row, col, board[row, col]))
                        return false;
                }
            }
            
            return true;
        }
        
        // Clear a cell (if it's not fixed)
        public bool ClearCell(int row, int col)
        {
            return SetCell(row, col, 0);
        }
        
        // Get a hint (find an empty cell and fill it with the correct answer)
        public (int row, int col, int value)? GetHint()
        {
            // Find first empty cell
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (board[row, col] == 0 && !isFixed[row, col])
                    {
                        // In a real implementation, you'd solve for this
                        // For now, we'll try values 1-9
                        for (int value = 1; value <= 9; value++)
                        {
                            if (IsValidPlacement(row, col, value))
                            {
                                return (row, col, value);
                            }
                        }
                    }
                }
            }
            
            return null;
        }
        
        // Reset the board to the original puzzle
        public void Reset()
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    if (!isFixed[row, col])
                    {
                        board[row, col] = 0;
                    }
                }
            }
        }
    }
}
