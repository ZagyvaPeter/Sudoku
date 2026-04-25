using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sudoku_App
{
    public partial class MainPage : ContentPage
    {
        private SudokuGame game;
        private Label[,] cellLabels;
        private Frame[,] cellFrames;
        private int selectedRow = -1;
        private int selectedCol = -1;
        private HashSet<int>[,] cellNotes;  // For notes mode
        private Button[] numberButtons; // Store number buttons for easy access
        
        // Game state
        private Stack<(int row, int col, int oldValue, int newValue)> moveHistory;
        private Dictionary<string, HashSet<int>> notesBackup; // Backup notes for undo
        private bool notesMode = false;
        private int mistakes = 0;
        private int maxMistakes = 3;
        private int score = 400;
        private int hintsRemaining = 2;
        private DateTime startTime;
        private bool isPaused = false;
        private string currentDifficulty = "Hard";
        private int highlightedNumber = -1; // For highlighting same numbers
        private bool highlightMode = false; // Toggle mode for auto-placing highlighted number
        private bool isLongPressing = false; // Flag to prevent placement during long press
        private bool isDarkMode = false; // Track theme mode
        
        // Timer
        private IDispatcherTimer timer;
        private IDispatcherTimer longPressTimer;
        private IDispatcherTimer notesLongPressTimer;
        private Button currentlyPressedButton;

        public bool IsNumberDone(string number) 
            => cellLabels.Cast<Label>().Count(x => x.Text == number) == 9;

        public MainPage()
        {
            InitializeComponent();
            
            game = new SudokuGame();
            cellLabels = new Label[9, 9];
            cellFrames = new Frame[9, 9];
            moveHistory = new Stack<(int, int, int, int)>();
            notesBackup = new Dictionary<string, HashSet<int>>();
            cellNotes = new HashSet<int>[9, 9];
            numberButtons = new Button[9];
            
            // Initialize notes for all cells
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    cellNotes[i, j] = new HashSet<int>();
            
            InitializeGrid();
            InitializeTimer();
            LoadNewGame("hard");
        }

        public MainPage(string difficulty)
        {
            InitializeComponent();
            
            game = new SudokuGame();
            cellLabels = new Label[9, 9];
            cellFrames = new Frame[9, 9];
            moveHistory = new Stack<(int, int, int, int)>();
            notesBackup = new Dictionary<string, HashSet<int>>();
            cellNotes = new HashSet<int>[9, 9];
            numberButtons = new Button[9];
            
            // Initialize notes for all cells
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    cellNotes[i, j] = new HashSet<int>();
            
            InitializeGrid();
            InitializeTimer();
            LoadNewGame(difficulty);
        }

        public MainPage(bool loadSavedGame)
        {
            InitializeComponent();
            
            game = new SudokuGame();
            cellLabels = new Label[9, 9];
            cellFrames = new Frame[9, 9];
            moveHistory = new Stack<(int, int, int, int)>();
            notesBackup = new Dictionary<string, HashSet<int>>();
            cellNotes = new HashSet<int>[9, 9];
            numberButtons = new Button[9];
            
            // Initialize notes for all cells
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    cellNotes[i, j] = new HashSet<int>();
            
            InitializeGrid();
            InitializeTimer();
            
            if (loadSavedGame)
            {
                LoadSavedGame();
            }
            else
            {
                LoadNewGame("hard");
            }
        }
        
        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Store references to number buttons after UI is loaded
            StoreNumberButtonReferences();
        }
        
        private void StoreNumberButtonReferences()
        {
            // Find the number pad grid in the UI
            var mainGrid = this.Content as Grid;
            if (mainGrid != null)
            {
                var frame = mainGrid.Children.OfType<Frame>().FirstOrDefault();
                if (frame?.Content is Grid innerGrid)
                {
                    var numberPadGrid = innerGrid.Children.OfType<Grid>().Skip(3).FirstOrDefault();
                    if (numberPadGrid != null)
                    {
                        var buttons = numberPadGrid.Children.OfType<Button>().ToList();
                        for (int i = 0; i < Math.Min(9, buttons.Count); i++)
                        {
                            numberButtons[i] = buttons[i];
                        }
                    }
                }
            }
        }

        private void InitializeTimer()
        {
            timer = Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                if (!isPaused)
                {
                    var elapsed = DateTime.Now - startTime;
                    TimeLabel.Text = $"{(int)elapsed.TotalMinutes}:{elapsed.Seconds:D2}";
                }
            };
            timer.Start();
        }

        private void InitializeGrid()
        {
            // Create 9x9 grid
            for (int row = 0; row < 9; row++)
            {
                SudokuGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            }
            
            for (int col = 0; col < 9; col++)
            {
                SudokuGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            }

            // Create cells
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    // Determine background color (light blue for alternating 3x3 boxes)
                    Color bgColor = ((row / 3) + (col / 3)) % 2 == 0 
                        ? Color.FromRgb(250, 250, 255) 
                        : Colors.White;
                    
                    // Create frame for cell
                    var frame = new Frame
                    {
                        Padding = 3,
                        HasShadow = false,
                        CornerRadius = 0,
                        BackgroundColor = bgColor,
                        BorderColor = Color.FromRgb(200, 200, 200)
                    };
                    
                    // Thicker borders for 3x3 box boundaries
                    double topMargin = row % 3 == 0 && row != 0 ? 2 : 0.5;
                    double leftMargin = col % 3 == 0 && col != 0 ? 2 : 0.5;
                    frame.Margin = new Thickness(leftMargin, topMargin, 0, 0);
                    
                    // Create a grid to hold both main number and notes
                    var cellContent = new Grid();
                    
                    // Create 3x3 grid for notes (will be visible when needed)
                    var noteGrid = new Grid
                    {
                        RowDefinitions =
                        {
                            new RowDefinition { Height = GridLength.Star },
                            new RowDefinition { Height = GridLength.Star },
                            new RowDefinition { Height = GridLength.Star }
                        },
                        ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Star },
                            new ColumnDefinition { Width = GridLength.Star }
                        },
                        IsVisible = false
                    };
                    
                    // Create 9 small labels for notes (1-9)
                    for (int i = 0; i < 9; i++)
                    {
                        var noteLabel = new Label
                        {
                            Text = "",
                            FontSize = 11,
                            TextColor = Color.FromRgb(120, 120, 120),
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            HorizontalTextAlignment = TextAlignment.Center,
                            VerticalTextAlignment = TextAlignment.Center
                        };
                        
                        Grid.SetRow(noteLabel, i / 3);
                        Grid.SetColumn(noteLabel, i % 3);
                        noteGrid.Children.Add(noteLabel);
                    }
                    
                    // Create label for main cell value
                    var mainLabel = new Label
                    {
                        HorizontalOptions = LayoutOptions.Fill,
                        VerticalOptions = LayoutOptions.Fill,
                        HorizontalTextAlignment = TextAlignment.Center,
                        VerticalTextAlignment = TextAlignment.Center,
                        FontSize = 22,
                        TextColor = Colors.Black,
                        FontAttributes = FontAttributes.Bold,
                        IsVisible = true
                    };
                    
                    // Add both to cell content
                    cellContent.Children.Add(noteGrid);
                    cellContent.Children.Add(mainLabel);
                    
                    frame.Content = cellContent;
                    
                    // Add tap gesture
                    var tapGesture = new TapGestureRecognizer();
                    int r = row;
                    int c = col;
                    tapGesture.Tapped += (s, e) => OnCellTapped(r, c);
                    frame.GestureRecognizers.Add(tapGesture);
                    
                    // Store references - store the noteGrid and mainLabel separately
                    cellLabels[row, col] = mainLabel;
                    cellFrames[row, col] = frame;
                    
                    // Add to grid
                    Grid.SetRow(frame, row);
                    Grid.SetColumn(frame, col);
                    SudokuGrid.Children.Add(frame);
                }
            }
        }

        private async void LoadSavedGame()
        {
            var savedState = await GameState.LoadGame();
            if (savedState != null)
            {
                game.LoadPuzzleState(savedState.Board, savedState.IsFixed);
                cellNotes = savedState.Notes;
                currentDifficulty = savedState.Difficulty;
                mistakes = savedState.Mistakes;
                score = savedState.Score;
                hintsRemaining = savedState.HintsRemaining;
                
                // Restore move history
                moveHistory.Clear();
                if (savedState.MoveHistory != null)
                {
                    foreach (var move in savedState.MoveHistory)
                    {
                        moveHistory.Push((move.Row, move.Col, move.OldValue, move.NewValue));
                    }
                }
                
                // Restore timer
                startTime = DateTime.Now.AddSeconds(-savedState.ElapsedSeconds);
                
                Dispatcher.Dispatch(() =>
                {
                    UpdateStatsDisplay();
                    UpdateGrid();
                });
            }
        }

        private async void SaveCurrentGame()
        {
            var elapsedSeconds = (int)(DateTime.Now - startTime).TotalSeconds;
            
            var moveHistoryList = new List<MoveHistoryItem>();
            foreach (var move in moveHistory)
            {
                moveHistoryList.Add(new MoveHistoryItem
                {
                    Row = move.row,
                    Col = move.col,
                    OldValue = move.oldValue,
                    NewValue = move.newValue
                });
            }
            
            var gameState = new GameStateData
            {
                Board = game.Board,
                IsFixed = game.IsFixed,
                Notes = cellNotes,
                Difficulty = currentDifficulty,
                Mistakes = mistakes,
                Score = score,
                HintsRemaining = hintsRemaining,
                ElapsedSeconds = elapsedSeconds,
                SaveTime = DateTime.Now,
                MoveHistory = moveHistoryList
            };

            await GameState.SaveGame(gameState);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Save game when leaving the page
            if (!game.IsSolved())
            {
                SaveCurrentGame();
            }
        }

        private void LoadNewGame(string difficulty)
        {
            var puzzle = PuzzleGenerator.GeneratePuzzle(difficulty);
            game.LoadPuzzle(puzzle);
            
            // Reset game state
            moveHistory.Clear();
            mistakes = 0;
            score = 400;
            hintsRemaining = 2;
            startTime = DateTime.Now;
            isPaused = false;
            currentDifficulty = difficulty.Substring(0, 1).ToUpper() + difficulty.Substring(1);
            notesMode = false;
            highlightedNumber = -1;
            
            // Clear all notes
            for (int i = 0; i < 9; i++)
                for (int j = 0; j < 9; j++)
                    cellNotes[i, j].Clear();
            
            // Update UI
            Dispatcher.Dispatch(() =>
            {
                UpdateStatsDisplay();
                UpdateGrid();
            });
            
            selectedRow = -1;
            selectedCol = -1;
        }
        
        private void UpdateStatsDisplay()
        {
            if (DifficultyLabel != null)
                DifficultyLabel.Text = currentDifficulty;
            if (MistakesLabel != null)
                MistakesLabel.Text = $"{mistakes}/{maxMistakes}";
            if (ScoreLabel != null)
                ScoreLabel.Text = score.ToString();
        }

        private void UpdateGrid()
        {
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    int value = game.GetCell(row, col);
                    var mainLabel = cellLabels[row, col];
                    var frame = cellFrames[row, col];
                    
                    // Get the cell content grid and note grid
                    var cellContent = frame.Content as Grid;
                    var noteGrid = cellContent?.Children[0] as Grid;
                    
                    // Base background color (alternating boxes)
                    Color bgColor = ((row / 3) + (col / 3)) % 2 == 0 
                        ? Color.FromRgb(50, 57, 69) 
                        : Color.FromRgb(60, 67, 79);

                    if (!highlightMode)
                    {
                        // Highlight selected cell
                        if (row == selectedRow && col == selectedCol)
                        {
                            bgColor = Color.FromRgb(187, 222, 251);
                        }
                        // Highlight same row/column as selected
                        else if (selectedRow >= 0 && (row == selectedRow || col == selectedCol))
                        {
                            bgColor = Color.FromRgb(18, 20, 40);
                        }
                        // Highlight same 3x3 box
                        else if (selectedRow >= 0 &&
                                 (row / 3) == (selectedRow / 3) &&
                                 (col / 3) == (selectedCol / 3))
                        {
                            bgColor = Color.FromRgb(18, 20, 40);
                        }
                    }
                    

                    // If cell has a value, show it
                    if (value != 0)
                    {
                        mainLabel.Text = value.ToString();
                        mainLabel.FontSize = 22;
                        mainLabel.IsVisible = true;
                        if (noteGrid != null) noteGrid.IsVisible = false;
                        
                        // Style fixed cells (pre-filled numbers)
                        if (game.IsFixed[row, col])
                        {
                            mainLabel.TextColor = Color.FromRgb(200,200,200);
                            mainLabel.FontAttributes = FontAttributes.Bold;
                        }
                        else
                        {
                            // User-entered numbers in blue
                            mainLabel.TextColor = Color.FromRgb(74, 144, 226);
                            mainLabel.FontAttributes = FontAttributes.None;
                        }
                        
                        // Highlight conflicts in red
                        if (!game.IsValidPlacement(row, col, value))
                        {
                            mainLabel.TextColor = Color.FromRgb(255, 100, 100);
                        }
                        
                        // Highlight if same as highlighted number
                        if (value == highlightedNumber)
                        {
                            bgColor = Color.FromRgb(18, 20, 40);
                        }
                    }
                    else if (cellNotes[row, col].Count > 0)
                    {
                        // Show notes in 3x3 grid
                        mainLabel.IsVisible = false;
                        if (noteGrid != null)
                        {
                            noteGrid.IsVisible = true;
                            
                            // Update each note label
                            for (int i = 1; i <= 9; i++)
                            {
                                var noteLabel = noteGrid.Children[i - 1] as Label;
                                if (noteLabel != null)
                                {
                                    if (cellNotes[row, col].Contains(i))
                                    {
                                        noteLabel.Text = i.ToString();
                                        noteLabel.TextColor = Color.FromRgb(200, 200, 200);

                                        // Highlight notes matching highlighted number
                                        if (i == highlightedNumber)
                                        {
                                            noteLabel.TextColor = Color.FromRgb(255, 255, 255);
                                            noteLabel.FontAttributes = FontAttributes.Bold;
                                        }
                                        else
                                        {
                                            noteLabel.FontAttributes = FontAttributes.None;
                                        }
                                    }
                                    else
                                    {
                                        noteLabel.Text = "";
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        mainLabel.Text = "";
                        mainLabel.IsVisible = true;
                        if (noteGrid != null) noteGrid.IsVisible = false;
                    }
                    
                    
                    
                    frame.BackgroundColor = bgColor;
                }
            }
            
            // Check if solved
            if (game.IsSolved())
            {
                ShowWinDialog();
            }
        }
        
        private async void ShowGameOverDialog()
        {
            bool restart = await DisplayAlert("Game Over!", 
                $"You made {maxMistakes} mistakes!\n\nScore: {score}\nTime: {TimeLabel?.Text}", 
                "Restart", 
                "Main Menu");
            
            if (restart)
            {
                // Restart with same difficulty
                await GameState.ClearSavedGame();
                LoadNewGame(currentDifficulty.ToLower());
            }
            else
            {
                // Go back to main menu
                await Navigation.PopAsync();
            }
        }
        
        private async void ShowWinDialog()
        {
            bool restart = await DisplayAlert("🎉 Congratulations!", 
                $"You solved the puzzle!\n\nTime: {TimeLabel?.Text}\nScore: {score}\nMistakes: {mistakes}", 
                "Play Again", 
                "Main Menu");
            
            if (restart)
            {
                // Start new game with same difficulty
                await GameState.ClearSavedGame();
                LoadNewGame(currentDifficulty.ToLower());
            }
            else
            {
                // Go back to main menu
                await Navigation.PopAsync();
            }
        }

        private void OnCellTapped(int row, int col)
        {
            // If in highlight mode and cell is empty, auto-place the highlighted number
            if (highlightMode && highlightedNumber > 0 && !game.IsFixed[row, col])
            {
                selectedRow = row;
                selectedCol = col;
                PlaceNumber(highlightedNumber);
                return;
            }
            
            // Don't allow selecting fixed cells
            if (game.IsFixed[row, col])
                return;
            
            selectedRow = row;
            selectedCol = col;
            UpdateGrid();
        }

        private void OnNumberClicked(object sender, EventArgs e)
        {
            if (selectedRow == -1 || selectedCol == -1)
            {
                return; // No cell selected
            }
            
            // Handle both Label and TapGestureRecognizer sources
            int number = 0;
            
            if (sender is Label label && int.TryParse(label.Text, out number))
            {
                // Got number from label
            }
            else if (sender is TapGestureRecognizer tapGesture)
            {
                var parentLabel = (tapGesture.Parent as View)?.Parent as Label;
                if (parentLabel == null)
                    parentLabel = tapGesture.Parent as Label;
                
                if (parentLabel != null && int.TryParse(parentLabel.Text, out number))
                {
                    // Got number from tap gesture's parent label
                }
            }
            
            if (number < 1 || number > 9)
                return;
            
            PlaceNumber(number);
        }
        
        // Individual number handlers for reliability
        private void OnNumber1Clicked(object sender, EventArgs e)
        {
            // Don't place if we're just activating highlight mode
            if (!isLongPressing)
                PlaceNumber(1);
        }
        
        private void OnNumber2Clicked(object sender, EventArgs e)
        {
            if (!isLongPressing)
                PlaceNumber(2);
        }
        
        private void OnNumber3Clicked(object sender, EventArgs e)
        {
            if (!isLongPressing)
                PlaceNumber(3);
        }
        
        private void OnNumber4Clicked(object sender, EventArgs e)
        {
            if (!isLongPressing)
                PlaceNumber(4);
        }
        
        private void OnNumber5Clicked(object sender, EventArgs e)
        {
            if (!isLongPressing)
                PlaceNumber(5);
        }
        
        private void OnNumber6Clicked(object sender, EventArgs e)
        {
            if (!isLongPressing)
                PlaceNumber(6);
        }
        
        private void OnNumber7Clicked(object sender, EventArgs e)
        {
            if (!isLongPressing)
                PlaceNumber(7);
        }
        
        private void OnNumber8Clicked(object sender, EventArgs e)
        {
            if (!isLongPressing)
                PlaceNumber(8);
        }
        
        private void OnNumber9Clicked(object sender, EventArgs e)
        {
            if (!isLongPressing)
                PlaceNumber(9);
        }
        
        private void OnNumberPressed(object sender, EventArgs e)
        {
            if (sender is Button button && int.TryParse(button.Text, out int number))
            {
                currentlyPressedButton = button;
                isLongPressing = false;
                
                // If already in highlight mode with a different number, turn it off first
                if (highlightMode && highlightedNumber != number)
                {
                    // Clear previous highlight mode
                    highlightMode = false;
                    ResetAllNumberButtonColors();
                }
                highlightedNumber = number;
                UpdateGrid();
                
                // Start timer for long press (1 second)
                if (longPressTimer == null)
                {
                    longPressTimer = Dispatcher.CreateTimer();
                    longPressTimer.Interval = TimeSpan.FromSeconds(1);
                }
                
                longPressTimer.Tick -= OnLongPressTimerTick; // Remove old handler
                longPressTimer.Tick += OnLongPressTimerTick; // Add new handler
                longPressTimer.Start();
            }
        }
        
        private void OnLongPressTimerTick(object sender, EventArgs e)
        {
            longPressTimer.Stop();
            isLongPressing = true;
            
            if (currentlyPressedButton != null && int.TryParse(currentlyPressedButton.Text, out int number))
            {
                // Toggle highlight mode for this number
                if (highlightMode && highlightedNumber == number)
                {
                    // Turn off highlight mode
                    highlightMode = false;
                    highlightedNumber = -1;
                    ResetAllNumberButtonColors();
                    UpdateGrid();
                }
                else
                {
                    // Turn on highlight mode
                    highlightMode = true;
                    highlightedNumber = number;
                    ResetAllNumberButtonColors();
                    currentlyPressedButton.BackgroundColor = Color.FromRgb(255, 250, 205);
                    UpdateGrid();
                }
            }
        }
        
        private void ResetAllNumberButtonColors()
        {
            // Reset all number buttons to transparent
            if (numberButtons != null)
            {
                foreach (var button in numberButtons)
                {
                    if (button != null)
                    {
                        button.BackgroundColor = Colors.Transparent;
                        if (IsNumberDone(button.Text))
                        {
                            button.IsVisible = false;
                        }
                    }
                }
            }
        }
        
        private void OnNumberReleased(object sender, EventArgs e)
        {
            // Stop long press timer
            if (longPressTimer != null && longPressTimer.IsRunning)
            {
                longPressTimer.Stop();
            }
            
            // Reset flag after a short delay to allow click event to check it
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100), () =>
            {
                isLongPressing = false;
            });
            
            currentlyPressedButton = null;
        }
        
        private void PlaceNumber(int number)
        {
            if (selectedRow == -1 || selectedCol == -1)
            {
                return; // No cell selected
            }
            
            // Can't modify fixed cells
            if (game.IsFixed[selectedRow, selectedCol])
            {
                return;
            }
            
            if (notesMode)
            {
                // Toggle note
                if (cellNotes[selectedRow, selectedCol].Contains(number))
                {
                    cellNotes[selectedRow, selectedCol].Remove(number);
                }
                else
                {
                    cellNotes[selectedRow, selectedCol].Add(number);
                }
                UpdateGrid();
            }
            else
            {
                int oldValue = game.GetCell(selectedRow, selectedCol);
                
                // Save current state of all notes for undo
                string moveKey = $"{selectedRow},{selectedCol},{number}";
                SaveNotesForUndo(moveKey, selectedRow, selectedCol, number);
                
                // Clear notes when placing a number
                cellNotes[selectedRow, selectedCol].Clear();
                
                // Clear notes of this number from same row, column, and 3x3 box
                ClearNotesInRelatedCells(selectedRow, selectedCol, number);
                
                // Add to move history
                moveHistory.Push((selectedRow, selectedCol, oldValue, number));
                
                // Set the cell
                game.SetCell(selectedRow, selectedCol, number);
                
                // Check if it's a mistake
                if (!game.IsValidPlacement(selectedRow, selectedCol, number))
                {
                    mistakes++;
                    score = Math.Max(0, score - 10);
                    UpdateStatsDisplay();
                    
                    if (mistakes >= maxMistakes)
                    {
                        ShowGameOverDialog();
                    }
                }
                else
                {
                    // Valid move, add points
                    score += 5;
                    UpdateStatsDisplay();
                }
                
                UpdateGrid();
            }
        }
        
        private void SaveNotesForUndo(string moveKey, int row, int col, int number)
        {
            // Create a deep copy of all affected notes
            var savedNotes = new HashSet<int>();
            
            // Save all notes that will be cleared
            for (int c = 0; c < 9; c++)
            {
                if (cellNotes[row, c].Contains(number))
                    savedNotes.Add(row * 100 + c * 10 + number);
            }
            
            for (int r = 0; r < 9; r++)
            {
                if (cellNotes[r, col].Contains(number))
                    savedNotes.Add(r * 100 + col * 10 + number);
            }
            
            int boxRow = (row / 3) * 3;
            int boxCol = (col / 3) * 3;
            for (int r = boxRow; r < boxRow + 3; r++)
            {
                for (int c = boxCol; c < boxCol + 3; c++)
                {
                    if (cellNotes[r, c].Contains(number))
                        savedNotes.Add(r * 100 + c * 10 + number);
                }
            }
            
            notesBackup[moveKey] = savedNotes;
        }
        
        private void ClearNotesInRelatedCells(int placedRow, int placedCol, int number)
        {
            // Clear notes of this number from the same row
            for (int col = 0; col < 9; col++)
            {
                cellNotes[placedRow, col].Remove(number);
            }
            
            // Clear notes of this number from the same column
            for (int row = 0; row < 9; row++)
            {
                cellNotes[row, placedCol].Remove(number);
            }
            
            // Clear notes of this number from the same 3x3 box
            int boxRow = (placedRow / 3) * 3;
            int boxCol = (placedCol / 3) * 3;
            
            for (int row = boxRow; row < boxRow + 3; row++)
            {
                for (int col = boxCol; col < boxCol + 3; col++)
                {
                    cellNotes[row, col].Remove(number);
                }
            }
        }

        private void OnUndoClicked(object sender, EventArgs e)
        {
            if (moveHistory.Count == 0)
            {
                return;
            }
            
            var lastMove = moveHistory.Pop();
            game.SetCell(lastMove.row, lastMove.col, lastMove.oldValue);
            
            // Restore notes that were cleared
            string moveKey = $"{lastMove.row},{lastMove.col},{lastMove.newValue}";
            if (notesBackup.ContainsKey(moveKey))
            {
                foreach (var encoded in notesBackup[moveKey])
                {
                    int r = encoded / 100;
                    int c = (encoded % 100) / 10;
                    int num = encoded % 10;
                    cellNotes[r, c].Add(num);
                }
                notesBackup.Remove(moveKey);
            }
            
            selectedRow = lastMove.row;
            selectedCol = lastMove.col;
            
            UpdateGrid();
        }
        
        private void OnEraseClicked(object sender, EventArgs e)
        {
            if (selectedRow == -1 || selectedCol == -1)
            {
                return;
            }
            
            int oldValue = game.GetCell(selectedRow, selectedCol);
            moveHistory.Push((selectedRow, selectedCol, oldValue, 0));
            
            game.ClearCell(selectedRow, selectedCol);
            UpdateGrid();
        }
        
        private void OnThemeToggleClicked(object sender, EventArgs e)
        {
            isDarkMode = !isDarkMode;
            ApplyTheme();
        }
        
        private void ApplyTheme()
        {
            if (isDarkMode)
            {
                // Dark mode colors
                this.BackgroundColor = Color.FromRgb(30, 30, 30);
                // You can add more dark mode styling here
            }
            else
            {
                // Light mode colors  
                this.BackgroundColor = Color.FromRgb(74, 144, 226);
            }
        }

        private void OnNotesClicked(object sender, EventArgs e)
        {
            notesMode = !notesMode;
            UpdateNotesButtonAppearance();
        }
        
        private void UpdateNotesButtonAppearance()
        {
            // Update the notes button to show active state
            if (NotesButton != null)
            {
                if (notesMode)
                {
                    NotesButton.BackgroundColor = Color.FromRgb(200, 220, 255);
                    NotesButton.TextColor = Color.FromRgb(74, 144, 226);
                }
                else
                {
                    NotesButton.BackgroundColor = Colors.Transparent;
                    NotesButton.TextColor = Colors.Gray;
                }
            }
        }
        
        private void OnNotesPressed(object sender, EventArgs e)
        {
            // Start timer for long press (2 seconds)
            if (notesLongPressTimer == null)
            {
                notesLongPressTimer = Dispatcher.CreateTimer();
                notesLongPressTimer.Interval = TimeSpan.FromSeconds(1);
                notesLongPressTimer.Tick += (s, args) =>
                {
                    notesLongPressTimer.Stop();
                    AutoFillNotes();
                };
            }
            notesLongPressTimer.Start();
        }
        
        private void OnNotesReleased(object sender, EventArgs e)
        {
            if (notesLongPressTimer != null && notesLongPressTimer.IsRunning)
            {
                notesLongPressTimer.Stop();
            }
        }
        
        private void AutoFillNotes()
        {
            // Fill all empty cells with possible numbers
            for (int row = 0; row < 9; row++)
            {
                for (int col = 0; col < 9; col++)
                {
                    // Only fill notes for empty cells
                    if (game.GetCell(row, col) == 0 && !game.IsFixed[row, col])
                    {
                        cellNotes[row, col].Clear();
                        
                        // Try each number 1-9
                        for (int num = 1; num <= 9; num++)
                        {
                            if (game.IsValidPlacement(row, col, num))
                            {
                                cellNotes[row, col].Add(num);
                            }
                        }
                    }
                }
            }
            
            UpdateGrid();
        }
        
        private void OnHintClicked(object sender, EventArgs e)
        {
            if (hintsRemaining <= 0)
            {
                DisplayAlert("No Hints Left", "You've used all your hints!", "OK");
                return;
            }
            
            var hint = game.GetHint();
            if (hint.HasValue)
            {
                moveHistory.Push((hint.Value.row, hint.Value.col, 0, hint.Value.value));
                game.SetCell(hint.Value.row, hint.Value.col, hint.Value.value);
                hintsRemaining--;
                score = Math.Max(0, score - 20);
                UpdateStatsDisplay();
                UpdateGrid();
            }
            else
            {
                DisplayAlert("No Hints Available", "No empty cells found or puzzle is complete", "OK");
            }
        }
        
        private void OnPauseClicked(object sender, EventArgs e)
        {
            isPaused = !isPaused;
            
            if (isPaused)
            {
                // Hide the grid when paused
                SudokuGrid.Opacity = 0.3;
                DisplayAlert("Paused", "Game paused. Tap pause again to resume.", "OK");
            }
            else
            {
                SudokuGrid.Opacity = 1.0;
            }
        }
        
        private async void OnBackClicked(object sender, EventArgs e)
        {
            // Save game before going back
            SaveCurrentGame();
            await Navigation.PopAsync();
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            // This would open a settings page
            DisplayAlert("Settings", "Choose difficulty:\n\nEasy | Medium | Hard", "OK");
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            if (selectedRow == -1 || selectedCol == -1)
            {
                DisplayAlert("No Cell Selected", "Please select a cell first", "OK");
                return;
            }
            
            game.ClearCell(selectedRow, selectedCol);
            UpdateGrid();
        }

        private void OnNewEasyClicked(object sender, EventArgs e)
        {
            LoadNewGame("easy");
        }

        private void OnNewMediumClicked(object sender, EventArgs e)
        {
            LoadNewGame("medium");
        }

        private void OnNewHardClicked(object sender, EventArgs e)
        {
            LoadNewGame("hard");
        }

        private void OnResetClicked(object sender, EventArgs e)
        {
            game.Reset();
            moveHistory.Clear();
            selectedRow = -1;
            selectedCol = -1;
            startTime = DateTime.Now;
            UpdateGrid();
        }
    }
}
