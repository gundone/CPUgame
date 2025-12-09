using System;
using System.Collections.Generic;
using System.Linq;
using CPUgame.Core.Circuit;
using CPUgame.Core.Components;
using CPUgame.Core.Levels;
using CPUgame.Core.Localization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

/// <summary>
/// Floating window that displays truth table for the current circuit.
/// Shows all input combinations and their corresponding output values.
/// </summary>
public class TruthTableWindow
{
    public Rectangle Bounds { get; private set; }
    public bool IsVisible { get; set; }
    public bool IsDraggingWindow { get; private set; }
    public bool IsSimulating { get; set; }
    public bool IsLevelPassed { get; set; }
    public List<TruthTableRow> TruthTableRows => _truthTableRows;

    private Point _windowDragOffset;
    private List<TruthTableRow> _truthTableRows = new();
    private int _totalInputBits;
    private int _totalOutputBits;
    private List<string> _inputLabels = new();
    private List<string> _outputLabels = new();

    // Level mode
    private GameLevel? _currentLevel;
    private List<bool> _rowMatchStatus = new(); // Track which rows match the expected output

    // Bus info for header rows
    private List<BusHeaderInfo> _inputBuses = new();
    private List<BusHeaderInfo> _outputBuses = new();

    // Calculated column widths (accounting for header text)
    private int _inputColumnWidth;
    private int _outputColumnWidth;
    private int _expectedOutputColumnWidth;
    private List<int> _inputBusWidths = new();
    private List<int> _outputBusWidths = new();
    private SpriteFont? _font;

    // Animation for simulation indicator
    private double _pulseTimer;
    private const double PulseSpeed = 3.0;

    // Scrolling
    private int _scrollOffset;
    private int _maxVisibleRows = 16;
    private bool _isScrollbarDragging;
    private int _scrollbarDragStartY;
    private int _scrollbarDragStartOffset;

    // UI Constants
    private const int TitleHeight = 28;
    private const int HeaderHeight = 26;
    private const int BusTitleRowHeight = 22;
    private const int PinNumberRowHeight = 20;
    private const int RowHeight = 22;
    private const int ButtonRowHeight = 36;
    private const int ButtonSize = 28;
    private const int ButtonSpacing = 8;
    private const int Padding = 8;
    private const int CellWidth = 24;
    private const int MinWidth = 200;
    private const int ScrollbarWidth = 14;

    // Button bounds
    private Rectangle _playButtonRect;
    private Rectangle _stopButtonRect;
    private Rectangle _clearButtonRect;
    private bool _isPlayButtonHovered;
    private bool _isStopButtonHovered;
    private bool _isClearButtonHovered;

    // Tooltip
    private string? _hoveredTooltip;
    private Point _tooltipPosition;

    // Colors
    private static readonly Color BackgroundColor = new(45, 45, 55, 245);
    private static readonly Color TitleColor = new(55, 55, 65);
    private static readonly Color BorderColor = new(80, 80, 100);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color HeaderColor = new(50, 50, 65);
    private static readonly Color TableHeaderRowColor = new(55, 55, 70);
    private static readonly Color RowEvenColor = new(40, 40, 50);
    private static readonly Color RowOddColor = new(45, 45, 55);
    private static readonly Color SeparatorColor = new(70, 70, 90);
    private static readonly Color HighColor = new(50, 200, 100);
    private static readonly Color LowColor = new(120, 120, 130);
    private static readonly Color ButtonColor = new(60, 100, 140);
    private static readonly Color ButtonHoverColor = new(80, 120, 160);
    private static readonly Color ScrollbarBgColor = new(35, 35, 45);
    private static readonly Color ScrollbarThumbColor = new(80, 80, 100);
    private static readonly Color ScrollbarThumbHoverColor = new(100, 100, 120);
    private static readonly Color RowMatchColor = new(40, 70, 50);
    private static readonly Color RowMismatchColor = new(70, 40, 40);
    private static readonly Color MatchIndicatorColor = new(80, 200, 120);
    private static readonly Color MismatchIndicatorColor = new(200, 80, 80);
    private static readonly Color LevelPassedColor = new(50, 180, 100);

    public TruthTableWindow(int x, int y)
    {
        UpdateBounds(x, y, MinWidth, 300);
    }

    private void UpdateBounds(int x, int y, int width, int height)
    {
        Bounds = new Rectangle(x, y, width, height);
        UpdateButtonRect();
    }

    private void UpdateButtonRect()
    {
        int buttonY = Bounds.Bottom - Padding - ButtonSize;
        int totalButtonsWidth = ButtonSize * 3 + ButtonSpacing * 2;
        int startX = Bounds.X + (Bounds.Width - totalButtonsWidth) / 2;

        _playButtonRect = new Rectangle(startX, buttonY, ButtonSize, ButtonSize);
        _stopButtonRect = new Rectangle(startX + ButtonSize + ButtonSpacing, buttonY, ButtonSize, ButtonSize);
        _clearButtonRect = new Rectangle(startX + (ButtonSize + ButtonSpacing) * 2, buttonY, ButtonSize, ButtonSize);
    }

    public void SetPosition(int x, int y)
    {
        UpdateBounds(x, y, Bounds.Width, Bounds.Height);
    }

    /// <summary>
    /// Calculate window size based on input/output counts
    /// </summary>
    public void RecalculateSize(Circuit circuit, SpriteFont? font = null)
    {
        if (font != null)
        {
            _font = font;
        }

        CountBuses(circuit);
        CalculateColumnWidths();

        // Calculate table width: inputs + outputs + expected (if in level mode) + separators + scrollbar
        int tableWidth = _inputColumnWidth + _outputColumnWidth + 2 + ScrollbarWidth; // +2 for separator
        if (_currentLevel != null)
        {
            tableWidth += _expectedOutputColumnWidth + 2; // +2 for separator
        }
        int width = Math.Max(MinWidth, tableWidth + Padding * 2);

        // Height: title + header + bus titles + pin numbers + rows + button + padding
        int totalRows = _truthTableRows.Count;
        if (_currentLevel != null && totalRows == 0)
        {
            // In level mode, show expected rows even if not simulated yet
            totalRows = _currentLevel.TruthTable.Count;
        }
        int visibleRows = Math.Min(totalRows, _maxVisibleRows);
        int tableHeight = HeaderHeight + BusTitleRowHeight + PinNumberRowHeight + visibleRows * RowHeight;
        int height = TitleHeight + tableHeight + ButtonRowHeight + Padding * 3;

        UpdateBounds(Bounds.X, Bounds.Y, width, height);
    }

    private void CalculateColumnWidths()
    {
        _inputBusWidths.Clear();
        _outputBusWidths.Clear();

        // Calculate minimum width for input column based on cells
        int inputCellsWidth = _totalInputBits * CellWidth;

        // Calculate minimum width for output column based on cells
        int outputCellsWidth = _totalOutputBits * CellWidth;

        // Expected output column width (same as output, only used in level mode)
        _expectedOutputColumnWidth = 0;

        // If we have a font, also consider header text widths, bus titles, and pin numbers
        if (_font != null)
        {
            // Main header text (Inputs / Outputs / Expected)
            var inputHeaderText = LocalizationManager.Get("truthtable.inputs");
            var outputHeaderText = LocalizationManager.Get("truthtable.outputs");
            var expectedHeaderText = LocalizationManager.Get("truthtable.expected");

            int inputHeaderWidth = (int)_font.MeasureString(inputHeaderText).X + Padding * 2;
            int outputHeaderWidth = (int)_font.MeasureString(outputHeaderText).X + Padding * 2;
            int expectedHeaderWidth = (int)_font.MeasureString(expectedHeaderText).X + Padding * 2;

            // Calculate minimum width needed for each input bus
            int inputBusTitlesWidth = 0;
            foreach (var bus in _inputBuses)
            {
                int titleWidth = (int)_font.MeasureString(bus.Title).X + Padding;
                int minBusWidth = Math.Max(titleWidth, bus.BitCount * CellWidth);
                _inputBusWidths.Add(minBusWidth);
                inputBusTitlesWidth += minBusWidth;
            }

            // Calculate minimum width needed for each output bus
            int outputBusTitlesWidth = 0;
            foreach (var bus in _outputBuses)
            {
                int titleWidth = (int)_font.MeasureString(bus.Title).X + Padding;
                int minBusWidth = Math.Max(titleWidth, bus.BitCount * CellWidth);
                _outputBusWidths.Add(minBusWidth);
                outputBusTitlesWidth += minBusWidth;
            }

            _inputColumnWidth = Math.Max(inputCellsWidth, Math.Max(inputHeaderWidth, inputBusTitlesWidth));
            _outputColumnWidth = Math.Max(outputCellsWidth, Math.Max(outputHeaderWidth, outputBusTitlesWidth));

            // Expected output column (only in level mode)
            if (_currentLevel != null)
            {
                int expectedBits = _currentLevel.OutputCount;
                _expectedOutputColumnWidth = Math.Max(expectedBits * CellWidth, expectedHeaderWidth);
            }
        }
        else
        {
            _inputColumnWidth = inputCellsWidth;
            _outputColumnWidth = outputCellsWidth;

            // Default bus widths based on bit count
            foreach (var bus in _inputBuses)
            {
                _inputBusWidths.Add(bus.BitCount * CellWidth);
            }
            foreach (var bus in _outputBuses)
            {
                _outputBusWidths.Add(bus.BitCount * CellWidth);
            }

            if (_currentLevel != null)
            {
                _expectedOutputColumnWidth = _currentLevel.OutputCount * CellWidth;
            }
        }
    }

    private void CountBuses(Circuit circuit)
    {
        _totalInputBits = 0;
        _totalOutputBits = 0;
        _inputLabels.Clear();
        _outputLabels.Clear();
        _inputBuses.Clear();
        _outputBuses.Clear();

        // Count input pins from BusInput components
        foreach (var component in circuit.Components.OfType<BusInput>().OrderBy(c => c.Y).ThenBy(c => c.X))
        {
            _inputBuses.Add(new BusHeaderInfo(component.Title ?? component.Name, component.BitCount));
            for (int i = component.BitCount - 1; i >= 0; i--)
            {
                _inputLabels.Add($"{component.Title ?? component.Name}[{i}]");
                _totalInputBits++;
            }
        }

        // Count output pins from BusOutput components
        foreach (var component in circuit.Components.OfType<BusOutput>().OrderBy(c => c.Y).ThenBy(c => c.X))
        {
            _outputBuses.Add(new BusHeaderInfo(component.Title ?? component.Name, component.BitCount));
            for (int i = component.BitCount - 1; i >= 0; i--)
            {
                _outputLabels.Add($"{component.Title ?? component.Name}[{i}]");
                _totalOutputBits++;
            }
        }
    }

    /// <summary>
    /// Run simulation for all input combinations (sandbox) or level-defined combinations (level mode)
    /// </summary>
    public void SimulateTruthTable(Circuit circuit)
    {
        IsSimulating = true;
        _truthTableRows.Clear();
        _scrollOffset = 0;

        CountBuses(circuit);

        if (_totalInputBits == 0 || _totalOutputBits == 0)
        {
            IsSimulating = false;
            RecalculateSize(circuit);
            return;
        }

        // Get input and output components
        var inputBuses = circuit.Components.OfType<BusInput>().OrderBy(c => c.Y).ThenBy(c => c.X).ToList();
        var outputBuses = circuit.Components.OfType<BusOutput>().OrderBy(c => c.Y).ThenBy(c => c.X).ToList();

        // Store original input values to restore later
        var originalValues = inputBuses.Select(b => b.Value).ToList();

        // In level mode, use only level-defined input combinations
        if (_currentLevel != null)
        {
            SimulateLevelCombinations(circuit, inputBuses, outputBuses);
        }
        else
        {
            SimulateAllCombinations(circuit, inputBuses, outputBuses);
        }

        // Restore original input values
        for (int i = 0; i < inputBuses.Count; i++)
        {
            inputBuses[i].Value = originalValues[i];
        }

        // Re-simulate to restore circuit state
        circuit.Simulate();

        IsSimulating = false;
        RecalculateSize(circuit);
    }

    /// <summary>
    /// Simulate only the input combinations defined in the current level's truth table
    /// </summary>
    private void SimulateLevelCombinations(Circuit circuit, List<BusInput> inputBuses, List<BusOutput> outputBuses)
    {
        if (_currentLevel == null)
        {
            return;
        }

        foreach (var levelEntry in _currentLevel.TruthTable)
        {
            // Set input values from level truth table entry
            int bitOffset = 0;
            foreach (var inputBus in inputBuses)
            {
                int busValue = 0;
                for (int bit = inputBus.BitCount - 1; bit >= 0; bit--)
                {
                    if (bitOffset < levelEntry.Inputs.Count && levelEntry.Inputs[bitOffset])
                    {
                        busValue |= (1 << bit);
                    }
                    bitOffset++;
                }
                inputBus.Value = busValue;
            }

            // Simulate the circuit
            circuit.Simulate();

            // Read input values (from level entry)
            var inputValues = new List<bool>(levelEntry.Inputs);

            // Read output values from circuit
            var outputValues = new List<bool>();
            foreach (var outputBus in outputBuses)
            {
                for (int bit = outputBus.BitCount - 1; bit >= 0; bit--)
                {
                    int inputIndex = outputBus.BitCount - 1 - bit;
                    if (inputIndex < outputBus.Inputs.Count)
                    {
                        outputValues.Add(outputBus.Inputs[inputIndex].Value == Signal.High);
                    }
                    else
                    {
                        outputValues.Add(false);
                    }
                }
            }

            _truthTableRows.Add(new TruthTableRow(inputValues, outputValues));
        }
    }

    /// <summary>
    /// Simulate all possible input combinations (sandbox mode)
    /// </summary>
    private void SimulateAllCombinations(Circuit circuit, List<BusInput> inputBuses, List<BusOutput> outputBuses)
    {
        // Limit input bits to prevent excessive computation
        int effectiveInputBits = Math.Min(_totalInputBits, 16);
        int totalCombinations = 1 << effectiveInputBits;

        for (int combo = 0; combo < totalCombinations; combo++)
        {
            // Set input values for this combination
            int bitOffset = 0;
            foreach (var inputBus in inputBuses)
            {
                int busValue = 0;
                for (int bit = inputBus.BitCount - 1; bit >= 0; bit--)
                {
                    int globalBit = effectiveInputBits - 1 - bitOffset;
                    if (globalBit >= 0 && (combo & (1 << globalBit)) != 0)
                    {
                        busValue |= (1 << bit);
                    }
                    bitOffset++;
                }
                inputBus.Value = busValue;
            }

            // Simulate the circuit
            circuit.Simulate();

            // Read output values
            var inputValues = new List<bool>();
            var outputValues = new List<bool>();

            bitOffset = 0;
            foreach (var inputBus in inputBuses)
            {
                for (int bit = inputBus.BitCount - 1; bit >= 0; bit--)
                {
                    int globalBit = effectiveInputBits - 1 - bitOffset;
                    inputValues.Add(globalBit >= 0 && (combo & (1 << globalBit)) != 0);
                    bitOffset++;
                }
            }

            foreach (var outputBus in outputBuses)
            {
                for (int bit = outputBus.BitCount - 1; bit >= 0; bit--)
                {
                    int inputIndex = outputBus.BitCount - 1 - bit;
                    if (inputIndex < outputBus.Inputs.Count)
                    {
                        outputValues.Add(outputBus.Inputs[inputIndex].Value == Signal.High);
                    }
                    else
                    {
                        outputValues.Add(false);
                    }
                }
            }

            _truthTableRows.Add(new TruthTableRow(inputValues, outputValues));
        }
    }

    public void Update(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased, int scrollDelta, double deltaTime)
    {
        if (!IsVisible)
        {
            return;
        }

        // Update pulse animation
        if (IsSimulating)
        {
            _pulseTimer += deltaTime * PulseSpeed;
        }

        // Update button hover states and tooltip
        _isPlayButtonHovered = _playButtonRect.Contains(mousePos);
        _isStopButtonHovered = _stopButtonRect.Contains(mousePos);
        _isClearButtonHovered = _clearButtonRect.Contains(mousePos);

        _hoveredTooltip = null;
        if (_isPlayButtonHovered)
        {
            _hoveredTooltip = LocalizationManager.Get("truthtable.simulate");
            _tooltipPosition = mousePos;
        }
        else if (_isStopButtonHovered)
        {
            _hoveredTooltip = LocalizationManager.Get("truthtable.stop");
            _tooltipPosition = mousePos;
        }
        else if (_isClearButtonHovered)
        {
            _hoveredTooltip = LocalizationManager.Get("truthtable.clear");
            _tooltipPosition = mousePos;
        }

        // Handle scrollbar dragging
        if (_isScrollbarDragging)
        {
            if (mousePressed)
            {
                int scrollableHeight = GetTableHeight() - _maxVisibleRows * RowHeight;
                int scrollbarTrackHeight = GetScrollbarTrackHeight();
                if (scrollableHeight > 0 && scrollbarTrackHeight > 0)
                {
                    int deltaY = mousePos.Y - _scrollbarDragStartY;
                    int maxScroll = _truthTableRows.Count - _maxVisibleRows;
                    _scrollOffset = _scrollbarDragStartOffset + (int)((float)deltaY / scrollbarTrackHeight * maxScroll);
                    _scrollOffset = Math.Max(0, Math.Min(_scrollOffset, maxScroll));
                }
            }
            else
            {
                _isScrollbarDragging = false;
            }
            return;
        }

        // Handle scroll wheel
        if (Bounds.Contains(mousePos) && scrollDelta != 0)
        {
            int maxScroll = Math.Max(0, _truthTableRows.Count - _maxVisibleRows);
            _scrollOffset -= scrollDelta / 120; // Standard mouse wheel delta
            _scrollOffset = Math.Max(0, Math.Min(_scrollOffset, maxScroll));
        }

        // Handle scrollbar click
        var scrollbarRect = GetScrollbarThumbRect();
        if (mouseJustPressed && scrollbarRect.Contains(mousePos))
        {
            _isScrollbarDragging = true;
            _scrollbarDragStartY = mousePos.Y;
            _scrollbarDragStartOffset = _scrollOffset;
            return;
        }

        var titleBar = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, TitleHeight);

        // Handle dragging the window
        if (mouseJustPressed && titleBar.Contains(mousePos))
        {
            IsDraggingWindow = true;
            _windowDragOffset = new Point(mousePos.X - Bounds.X, mousePos.Y - Bounds.Y);
        }

        if (IsDraggingWindow)
        {
            if (mousePressed)
            {
                UpdateBounds(mousePos.X - _windowDragOffset.X, mousePos.Y - _windowDragOffset.Y, Bounds.Width, Bounds.Height);
            }
            else
            {
                IsDraggingWindow = false;
            }
        }
    }

    public TruthTableAction HandleButtonClick(Point mousePos, bool mouseJustPressed)
    {
        if (!IsVisible)
        {
            return TruthTableAction.None;
        }

        if (mouseJustPressed)
        {
            if (_playButtonRect.Contains(mousePos))
            {
                return TruthTableAction.Play;
            }
            if (_stopButtonRect.Contains(mousePos))
            {
                return TruthTableAction.Stop;
            }
            if (_clearButtonRect.Contains(mousePos))
            {
                return TruthTableAction.Clear;
            }
        }

        return TruthTableAction.None;
    }

    public void ClearTable()
    {
        _truthTableRows.Clear();
        _rowMatchStatus.Clear();
        _scrollOffset = 0;
        IsSimulating = false;
        IsLevelPassed = false;
    }

    public void SetCurrentLevel(GameLevel? level)
    {
        _currentLevel = level;
        _rowMatchStatus.Clear();
        IsLevelPassed = false;

        // Initialize column widths based on level
        if (level != null)
        {
            _totalInputBits = level.InputCount;
            _totalOutputBits = level.OutputCount;
            CalculateColumnWidths();

            // Calculate new window size
            int tableWidth = _inputColumnWidth + _outputColumnWidth + _expectedOutputColumnWidth + 4 + ScrollbarWidth;
            int width = Math.Max(MinWidth, tableWidth + Padding * 2);
            int totalRows = level.TruthTable.Count;
            int visibleRows = Math.Min(totalRows, _maxVisibleRows);
            int tableHeight = HeaderHeight + BusTitleRowHeight + PinNumberRowHeight + visibleRows * RowHeight;
            int height = TitleHeight + tableHeight + ButtonRowHeight + Padding * 3;

            UpdateBounds(Bounds.X, Bounds.Y, width, height);
        }
    }

    public void UpdateRowMatchStatus()
    {
        _rowMatchStatus.Clear();

        if (_currentLevel == null || _truthTableRows.Count == 0)
        {
            return;
        }

        bool allMatch = true;

        for (int i = 0; i < _truthTableRows.Count; i++)
        {
            if (i < _currentLevel.TruthTable.Count)
            {
                var simRow = _truthTableRows[i];
                var levelRow = _currentLevel.TruthTable[i];

                bool rowMatches = true;

                // Check if outputs match
                if (simRow.OutputValues.Count == levelRow.Outputs.Count)
                {
                    for (int j = 0; j < simRow.OutputValues.Count; j++)
                    {
                        if (simRow.OutputValues[j] != levelRow.Outputs[j])
                        {
                            rowMatches = false;
                            break;
                        }
                    }
                }
                else
                {
                    rowMatches = false;
                }

                _rowMatchStatus.Add(rowMatches);
                if (!rowMatches)
                {
                    allMatch = false;
                }
            }
            else
            {
                _rowMatchStatus.Add(false);
                allMatch = false;
            }
        }

        IsLevelPassed = allMatch && _truthTableRows.Count == _currentLevel.TruthTable.Count;
    }

    public bool ContainsPoint(Point p)
    {
        if (!IsVisible)
        {
            return false;
        }

        return Bounds.Contains(p);
    }

    private int GetTableHeight()
    {
        return _truthTableRows.Count * RowHeight;
    }

    private int GetScrollbarTrackHeight()
    {
        return Bounds.Height - TitleHeight - HeaderHeight - BusTitleRowHeight - PinNumberRowHeight - ButtonRowHeight - Padding * 3;
    }

    private Rectangle GetScrollbarThumbRect()
    {
        if (_truthTableRows.Count <= _maxVisibleRows)
        {
            return Rectangle.Empty;
        }

        int trackHeight = GetScrollbarTrackHeight();
        int thumbHeight = Math.Max(20, trackHeight * _maxVisibleRows / _truthTableRows.Count);
        int maxScroll = _truthTableRows.Count - _maxVisibleRows;
        int thumbY = (int)((float)_scrollOffset / maxScroll * (trackHeight - thumbHeight));

        return new Rectangle(
            Bounds.Right - ScrollbarWidth - Padding,
            Bounds.Y + TitleHeight + HeaderHeight + BusTitleRowHeight + PinNumberRowHeight + thumbY,
            ScrollbarWidth,
            thumbHeight);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Point mousePos)
    {
        if (!IsVisible)
        {
            return;
        }

        // Store font reference for size calculations
        _font = font;

        // Background
        spriteBatch.Draw(pixel, Bounds, BackgroundColor);

        // Title bar - pulse green when simulating, show passed state
        var titleBar = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, TitleHeight);
        Color titleBarColor = TitleColor;
        if (IsLevelPassed)
        {
            titleBarColor = LevelPassedColor;
        }
        else if (IsSimulating)
        {
            float pulse = (float)(Math.Sin(_pulseTimer) * 0.5 + 0.5); // 0 to 1
            titleBarColor = Color.Lerp(TitleColor, new Color(40, 80, 60), pulse * 0.5f);
        }
        spriteBatch.Draw(pixel, titleBar, titleBarColor);

        // Title text - show level name if in level mode
        string titleText;
        if (_currentLevel != null)
        {
            titleText = _currentLevel.Name;
            if (IsLevelPassed)
            {
                titleText += " [OK]";
            }
        }
        else
        {
            titleText = LocalizationManager.Get("truthtable.title");
        }
        var titleSize = font.MeasureString(titleText);
        spriteBatch.DrawString(font, titleText,
            new Vector2(Bounds.X + (Bounds.Width - titleSize.X) / 2, Bounds.Y + (TitleHeight - titleSize.Y) / 2),
            IsLevelPassed ? new Color(30, 30, 40) : TextColor);

        // Simulation indicator dot
        if (IsSimulating)
        {
            float pulse = (float)(Math.Sin(_pulseTimer) * 0.5 + 0.5);
            Color dotColor = Color.Lerp(new Color(50, 150, 80), HighColor, pulse);
            int dotSize = 8;
            int dotX = Bounds.X + Bounds.Width - dotSize - 8;
            int dotY = Bounds.Y + (TitleHeight - dotSize) / 2;
            spriteBatch.Draw(pixel, new Rectangle(dotX, dotY, dotSize, dotSize), dotColor);
        }

        // Border - pulse when simulating
        Color borderColor = BorderColor;
        if (IsSimulating)
        {
            float pulse = (float)(Math.Sin(_pulseTimer) * 0.5 + 0.5);
            borderColor = Color.Lerp(BorderColor, HighColor, pulse * 0.4f);
        }
        DrawBorder(spriteBatch, pixel, Bounds, borderColor, 2);

        // Content area
        int contentY = Bounds.Y + TitleHeight;
        int tableWidth = Bounds.Width - Padding * 2 - ScrollbarWidth;

        if (_totalInputBits == 0 || _totalOutputBits == 0)
        {
            // Show message when no inputs/outputs
            var noDataText = LocalizationManager.Get("truthtable.no_buses");
            var noDataSize = font.MeasureString(noDataText);
            spriteBatch.DrawString(font, noDataText,
                new Vector2(Bounds.X + (Bounds.Width - noDataSize.X) / 2, contentY + 40),
                new Color(150, 150, 170));
        }
        else if (_truthTableRows.Count == 0 && _currentLevel == null)
        {
            // Show message to click simulate (sandbox mode only)
            var clickSimText = LocalizationManager.Get("truthtable.click_simulate");
            var clickSimSize = font.MeasureString(clickSimText);
            spriteBatch.DrawString(font, clickSimText,
                new Vector2(Bounds.X + (Bounds.Width - clickSimSize.X) / 2, contentY + 40),
                new Color(150, 150, 170));
        }
        else if (_truthTableRows.Count == 0 && _currentLevel != null)
        {
            // In level mode without simulation, show expected table
            DrawHeader(spriteBatch, pixel, font, Bounds.X + Padding, contentY, tableWidth);

            int busTitleY = contentY + HeaderHeight;
            DrawBusTitlesRow(spriteBatch, pixel, font, Bounds.X + Padding, busTitleY, tableWidth);

            int pinNumberY = busTitleY + BusTitleRowHeight;
            DrawPinNumbersRow(spriteBatch, pixel, font, Bounds.X + Padding, pinNumberY, tableWidth);

            // Draw expected rows with "?" for outputs
            int rowsY = pinNumberY + PinNumberRowHeight;
            int totalRows = _currentLevel.TruthTable.Count;
            int visibleRows = Math.Min(_maxVisibleRows, totalRows - _scrollOffset);

            for (int i = 0; i < visibleRows; i++)
            {
                int rowIndex = i + _scrollOffset;
                if (rowIndex < _currentLevel.TruthTable.Count)
                {
                    DrawLevelOnlyRow(spriteBatch, pixel, font, Bounds.X + Padding, rowsY + i * RowHeight, tableWidth, rowIndex);
                }
            }

            if (totalRows > _maxVisibleRows)
            {
                DrawScrollbar(spriteBatch, pixel, mousePos);
            }
        }
        else
        {
            // Draw table header (Inputs / Outputs)
            DrawHeader(spriteBatch, pixel, font, Bounds.X + Padding, contentY, tableWidth);

            // Draw bus titles row
            int busTitleY = contentY + HeaderHeight;
            DrawBusTitlesRow(spriteBatch, pixel, font, Bounds.X + Padding, busTitleY, tableWidth);

            // Draw pin numbers row
            int pinNumberY = busTitleY + BusTitleRowHeight;
            DrawPinNumbersRow(spriteBatch, pixel, font, Bounds.X + Padding, pinNumberY, tableWidth);

            // Draw table rows
            int rowsY = pinNumberY + PinNumberRowHeight;
            int visibleRows = Math.Min(_maxVisibleRows, _truthTableRows.Count - _scrollOffset);

            for (int i = 0; i < visibleRows; i++)
            {
                int rowIndex = i + _scrollOffset;
                if (rowIndex < _truthTableRows.Count)
                {
                    DrawRow(spriteBatch, pixel, font, Bounds.X + Padding, rowsY + i * RowHeight, tableWidth, rowIndex, _truthTableRows[rowIndex]);
                }
            }

            // Draw scrollbar if needed
            if (_truthTableRows.Count > _maxVisibleRows)
            {
                DrawScrollbar(spriteBatch, pixel, mousePos);
            }
        }

        // Draw button row
        DrawIconButton(spriteBatch, pixel, font, _playButtonRect, ">", _isPlayButtonHovered, IsSimulating);
        DrawIconButton(spriteBatch, pixel, font, _stopButtonRect, "||", _isStopButtonHovered, false);
        DrawIconButton(spriteBatch, pixel, font, _clearButtonRect, "X", _isClearButtonHovered, false);

        // Draw tooltip if hovering
        if (_hoveredTooltip != null)
        {
            DrawTooltip(spriteBatch, pixel, font, _hoveredTooltip, _tooltipPosition);
        }
    }

    private void DrawHeader(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, int x, int y, int width)
    {
        // Header background
        spriteBatch.Draw(pixel, new Rectangle(x, y, width, HeaderHeight), HeaderColor);

        // Input header - use calculated column width
        var inputText = LocalizationManager.Get("truthtable.inputs");
        var inputSize = font.MeasureString(inputText);
        spriteBatch.DrawString(font, inputText,
            new Vector2(x + (_inputColumnWidth - inputSize.X) / 2, y + (HeaderHeight - inputSize.Y) / 2),
            TextColor);

        // Separator between inputs and expected
        int separatorX = x + _inputColumnWidth;
        spriteBatch.Draw(pixel, new Rectangle(separatorX, y, 2, HeaderHeight), SeparatorColor);

        // Expected output header (only in level mode)
        if (_currentLevel != null)
        {
            var expectedText = LocalizationManager.Get("truthtable.expected");
            var expectedSize = font.MeasureString(expectedText);
            spriteBatch.DrawString(font, expectedText,
                new Vector2(separatorX + 2 + (_expectedOutputColumnWidth - expectedSize.X) / 2, y + (HeaderHeight - expectedSize.Y) / 2),
                TextColor);

            // Separator between expected and actual output
            separatorX += 2 + _expectedOutputColumnWidth;
            spriteBatch.Draw(pixel, new Rectangle(separatorX, y, 2, HeaderHeight), SeparatorColor);
        }

        // Output header - use calculated column width
        var outputText = LocalizationManager.Get("truthtable.outputs");
        var outputSize = font.MeasureString(outputText);
        spriteBatch.DrawString(font, outputText,
            new Vector2(separatorX + 2 + (_outputColumnWidth - outputSize.X) / 2, y + (HeaderHeight - outputSize.Y) / 2),
            TextColor);
    }

    private void DrawRow(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, int x, int y, int width, int rowIndex, TruthTableRow row)
    {
        // Row background - use match/mismatch colors when in level mode
        Color rowColor;
        if (_currentLevel != null && rowIndex < _rowMatchStatus.Count)
        {
            rowColor = _rowMatchStatus[rowIndex] ? RowMatchColor : RowMismatchColor;
        }
        else
        {
            rowColor = rowIndex % 2 == 0 ? RowEvenColor : RowOddColor;
        }
        spriteBatch.Draw(pixel, new Rectangle(x, y, width, RowHeight), rowColor);

        // Draw input values using calculated bus widths
        int cellX = x;
        int valueIdx = 0;
        for (int busIdx = 0; busIdx < _inputBuses.Count; busIdx++)
        {
            var bus = _inputBuses[busIdx];
            int busWidth = busIdx < _inputBusWidths.Count ? _inputBusWidths[busIdx] : bus.BitCount * CellWidth;
            int pinCellWidth = busWidth / bus.BitCount;

            for (int i = 0; i < bus.BitCount; i++)
            {
                if (valueIdx < row.InputValues.Count)
                {
                    var value = row.InputValues[valueIdx];
                    var cellText = value ? "1" : "0";
                    var cellSize = font.MeasureString(cellText);
                    var cellColor = value ? HighColor : LowColor;
                    spriteBatch.DrawString(font, cellText,
                        new Vector2(cellX + (pinCellWidth - cellSize.X) / 2, y + (RowHeight - cellSize.Y) / 2),
                        cellColor);
                }
                cellX += pinCellWidth;
                valueIdx++;
            }
        }

        // Separator at end of input column
        int separatorX = x + _inputColumnWidth;
        spriteBatch.Draw(pixel, new Rectangle(separatorX, y, 2, RowHeight), SeparatorColor);

        // Draw expected output values (only in level mode)
        if (_currentLevel != null && rowIndex < _currentLevel.TruthTable.Count)
        {
            cellX = separatorX + 2;
            var expectedRow = _currentLevel.TruthTable[rowIndex];
            int pinCellWidth = _expectedOutputColumnWidth / Math.Max(1, expectedRow.Outputs.Count);

            for (int i = 0; i < expectedRow.Outputs.Count; i++)
            {
                var value = expectedRow.Outputs[i];
                var cellText = value ? "1" : "0";
                var cellSize = font.MeasureString(cellText);
                var cellColor = value ? HighColor : LowColor;
                spriteBatch.DrawString(font, cellText,
                    new Vector2(cellX + (pinCellWidth - cellSize.X) / 2, y + (RowHeight - cellSize.Y) / 2),
                    cellColor);
                cellX += pinCellWidth;
            }

            // Separator after expected column
            separatorX += 2 + _expectedOutputColumnWidth;
            spriteBatch.Draw(pixel, new Rectangle(separatorX, y, 2, RowHeight), SeparatorColor);
        }

        // Draw output values using calculated bus widths
        cellX = separatorX + 2;
        valueIdx = 0;
        for (int busIdx = 0; busIdx < _outputBuses.Count; busIdx++)
        {
            var bus = _outputBuses[busIdx];
            int busWidth = busIdx < _outputBusWidths.Count ? _outputBusWidths[busIdx] : bus.BitCount * CellWidth;
            int pinCellWidth = busWidth / bus.BitCount;

            for (int i = 0; i < bus.BitCount; i++)
            {
                if (valueIdx < row.OutputValues.Count)
                {
                    var value = row.OutputValues[valueIdx];
                    var cellText = value ? "1" : "0";
                    var cellSize = font.MeasureString(cellText);
                    var cellColor = value ? HighColor : LowColor;
                    spriteBatch.DrawString(font, cellText,
                        new Vector2(cellX + (pinCellWidth - cellSize.X) / 2, y + (RowHeight - cellSize.Y) / 2),
                        cellColor);
                }
                cellX += pinCellWidth;
                valueIdx++;
            }
        }
    }

    private void DrawLevelOnlyRow(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, int x, int y, int width, int rowIndex)
    {
        if (_currentLevel == null || rowIndex >= _currentLevel.TruthTable.Count)
        {
            return;
        }

        var levelRow = _currentLevel.TruthTable[rowIndex];

        // Row background
        var rowColor = rowIndex % 2 == 0 ? RowEvenColor : RowOddColor;
        spriteBatch.Draw(pixel, new Rectangle(x, y, width, RowHeight), rowColor);

        // Draw input values
        int cellX = x;
        int pinCellWidth = _inputColumnWidth / Math.Max(1, levelRow.Inputs.Count);
        for (int i = 0; i < levelRow.Inputs.Count; i++)
        {
            var value = levelRow.Inputs[i];
            var cellText = value ? "1" : "0";
            var cellSize = font.MeasureString(cellText);
            var cellColor = value ? HighColor : LowColor;
            spriteBatch.DrawString(font, cellText,
                new Vector2(cellX + (pinCellWidth - cellSize.X) / 2, y + (RowHeight - cellSize.Y) / 2),
                cellColor);
            cellX += pinCellWidth;
        }

        // Separator
        int separatorX = x + _inputColumnWidth;
        spriteBatch.Draw(pixel, new Rectangle(separatorX, y, 2, RowHeight), SeparatorColor);

        // Draw expected output values
        cellX = separatorX + 2;
        pinCellWidth = _expectedOutputColumnWidth / Math.Max(1, levelRow.Outputs.Count);
        for (int i = 0; i < levelRow.Outputs.Count; i++)
        {
            var value = levelRow.Outputs[i];
            var cellText = value ? "1" : "0";
            var cellSize = font.MeasureString(cellText);
            var cellColor = value ? HighColor : LowColor;
            spriteBatch.DrawString(font, cellText,
                new Vector2(cellX + (pinCellWidth - cellSize.X) / 2, y + (RowHeight - cellSize.Y) / 2),
                cellColor);
            cellX += pinCellWidth;
        }

        // Separator after expected
        separatorX += 2 + _expectedOutputColumnWidth;
        spriteBatch.Draw(pixel, new Rectangle(separatorX, y, 2, RowHeight), SeparatorColor);

        // Draw "?" for actual outputs (not simulated yet)
        cellX = separatorX + 2;
        pinCellWidth = _outputColumnWidth / Math.Max(1, levelRow.Outputs.Count);
        for (int i = 0; i < levelRow.Outputs.Count; i++)
        {
            var cellText = "?";
            var cellSize = font.MeasureString(cellText);
            spriteBatch.DrawString(font, cellText,
                new Vector2(cellX + (pinCellWidth - cellSize.X) / 2, y + (RowHeight - cellSize.Y) / 2),
                new Color(150, 150, 170));
            cellX += pinCellWidth;
        }
    }

    private void DrawBusTitlesRow(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, int x, int y, int width)
    {
        // Row background - slightly highlighted
        spriteBatch.Draw(pixel, new Rectangle(x, y, width, BusTitleRowHeight), TableHeaderRowColor);

        // Draw input bus titles
        int cellX = x;
        for (int i = 0; i < _inputBuses.Count; i++)
        {
            var bus = _inputBuses[i];
            int busWidth = i < _inputBusWidths.Count ? _inputBusWidths[i] : bus.BitCount * CellWidth;
            var titleSize = font.MeasureString(bus.Title);

            spriteBatch.DrawString(font, bus.Title,
                new Vector2(cellX + (busWidth - titleSize.X) / 2, y + (BusTitleRowHeight - titleSize.Y) / 2),
                TextColor);
            cellX += busWidth;
        }

        // Separator
        int separatorX = x + _inputColumnWidth;
        spriteBatch.Draw(pixel, new Rectangle(separatorX, y, 2, BusTitleRowHeight), SeparatorColor);

        // Expected output title (in level mode)
        if (_currentLevel != null)
        {
            // Just draw "-" as placeholder for expected column title
            var expectedTitle = "-";
            var expectedSize = font.MeasureString(expectedTitle);
            spriteBatch.DrawString(font, expectedTitle,
                new Vector2(separatorX + 2 + (_expectedOutputColumnWidth - expectedSize.X) / 2, y + (BusTitleRowHeight - expectedSize.Y) / 2),
                new Color(150, 150, 170));

            separatorX += 2 + _expectedOutputColumnWidth;
            spriteBatch.Draw(pixel, new Rectangle(separatorX, y, 2, BusTitleRowHeight), SeparatorColor);
        }

        // Draw output bus titles
        cellX = separatorX + 2;
        for (int i = 0; i < _outputBuses.Count; i++)
        {
            var bus = _outputBuses[i];
            int busWidth = i < _outputBusWidths.Count ? _outputBusWidths[i] : bus.BitCount * CellWidth;
            var titleSize = font.MeasureString(bus.Title);

            spriteBatch.DrawString(font, bus.Title,
                new Vector2(cellX + (busWidth - titleSize.X) / 2, y + (BusTitleRowHeight - titleSize.Y) / 2),
                TextColor);
            cellX += busWidth;
        }
    }

    private void DrawPinNumbersRow(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, int x, int y, int width)
    {
        // Row background - slightly highlighted
        spriteBatch.Draw(pixel, new Rectangle(x, y, width, PinNumberRowHeight), TableHeaderRowColor);

        // Draw input pin numbers using calculated bus widths
        int cellX = x;
        for (int busIdx = 0; busIdx < _inputBuses.Count; busIdx++)
        {
            var bus = _inputBuses[busIdx];
            int busWidth = busIdx < _inputBusWidths.Count ? _inputBusWidths[busIdx] : bus.BitCount * CellWidth;
            int pinCellWidth = busWidth / bus.BitCount;

            for (int i = bus.BitCount - 1; i >= 0; i--)
            {
                var pinText = i.ToString();
                var pinSize = font.MeasureString(pinText);
                spriteBatch.DrawString(font, pinText,
                    new Vector2(cellX + (pinCellWidth - pinSize.X) / 2, y + (PinNumberRowHeight - pinSize.Y) / 2),
                    new Color(150, 150, 170));
                cellX += pinCellWidth;
            }
        }

        // Separator
        int separatorX = x + _inputColumnWidth;
        spriteBatch.Draw(pixel, new Rectangle(separatorX, y, 2, PinNumberRowHeight), SeparatorColor);

        // Draw expected output pin numbers (in level mode)
        if (_currentLevel != null)
        {
            int pinCellWidth = _expectedOutputColumnWidth / Math.Max(1, _currentLevel.OutputCount);
            for (int i = _currentLevel.OutputCount - 1; i >= 0; i--)
            {
                var pinText = i.ToString();
                var pinSize = font.MeasureString(pinText);
                spriteBatch.DrawString(font, pinText,
                    new Vector2(separatorX + 2 + (_currentLevel.OutputCount - 1 - i) * pinCellWidth + (pinCellWidth - pinSize.X) / 2,
                        y + (PinNumberRowHeight - pinSize.Y) / 2),
                    new Color(150, 150, 170));
            }

            separatorX += 2 + _expectedOutputColumnWidth;
            spriteBatch.Draw(pixel, new Rectangle(separatorX, y, 2, PinNumberRowHeight), SeparatorColor);
        }

        // Draw output pin numbers using calculated bus widths
        cellX = separatorX + 2;
        for (int busIdx = 0; busIdx < _outputBuses.Count; busIdx++)
        {
            var bus = _outputBuses[busIdx];
            int busWidth = busIdx < _outputBusWidths.Count ? _outputBusWidths[busIdx] : bus.BitCount * CellWidth;
            int pinCellWidth = busWidth / bus.BitCount;

            for (int i = bus.BitCount - 1; i >= 0; i--)
            {
                var pinText = i.ToString();
                var pinSize = font.MeasureString(pinText);
                spriteBatch.DrawString(font, pinText,
                    new Vector2(cellX + (pinCellWidth - pinSize.X) / 2, y + (PinNumberRowHeight - pinSize.Y) / 2),
                    new Color(150, 150, 170));
                cellX += pinCellWidth;
            }
        }
    }

    private void DrawScrollbar(SpriteBatch spriteBatch, Texture2D pixel, Point mousePos)
    {
        int trackX = Bounds.Right - ScrollbarWidth - Padding;
        int trackY = Bounds.Y + TitleHeight + HeaderHeight + BusTitleRowHeight + PinNumberRowHeight;
        int trackHeight = GetScrollbarTrackHeight();

        // Track background
        spriteBatch.Draw(pixel, new Rectangle(trackX, trackY, ScrollbarWidth, trackHeight), ScrollbarBgColor);

        // Thumb
        var thumbRect = GetScrollbarThumbRect();
        if (thumbRect != Rectangle.Empty)
        {
            var thumbColor = thumbRect.Contains(mousePos) || _isScrollbarDragging ? ScrollbarThumbHoverColor : ScrollbarThumbColor;
            spriteBatch.Draw(pixel, thumbRect, thumbColor);
        }
    }

    private void DrawIconButton(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Rectangle rect, string icon, bool isHovered, bool isActive)
    {
        Color buttonColor;
        if (isActive)
        {
            buttonColor = HighColor;
        }
        else if (isHovered)
        {
            buttonColor = ButtonHoverColor;
        }
        else
        {
            buttonColor = ButtonColor;
        }

        spriteBatch.Draw(pixel, rect, buttonColor);
        DrawBorder(spriteBatch, pixel, rect, BorderColor, 1);

        var iconSize = font.MeasureString(icon);
        spriteBatch.DrawString(font, icon,
            new Vector2(rect.X + (rect.Width - iconSize.X) / 2, rect.Y + (rect.Height - iconSize.Y) / 2),
            isActive ? new Color(30, 30, 40) : TextColor);
    }

    private void DrawTooltip(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, string text, Point position)
    {
        var textSize = font.MeasureString(text);
        int tooltipPadding = 4;
        int tooltipWidth = (int)textSize.X + tooltipPadding * 2;
        int tooltipHeight = (int)textSize.Y + tooltipPadding * 2;

        // Position tooltip above and to the right of cursor
        int tooltipX = position.X + 10;
        int tooltipY = position.Y - tooltipHeight - 5;

        var tooltipRect = new Rectangle(tooltipX, tooltipY, tooltipWidth, tooltipHeight);

        spriteBatch.Draw(pixel, tooltipRect, new Color(60, 60, 70, 240));
        DrawBorder(spriteBatch, pixel, tooltipRect, BorderColor, 1);
        spriteBatch.DrawString(font, text,
            new Vector2(tooltipX + tooltipPadding, tooltipY + tooltipPadding),
            TextColor);
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}

/// <summary>
/// Represents a single row in the truth table
/// </summary>
public class TruthTableRow
{
    public List<bool> InputValues { get; }
    public List<bool> OutputValues { get; }

    public TruthTableRow(List<bool> inputValues, List<bool> outputValues)
    {
        InputValues = new List<bool>(inputValues);
        OutputValues = new List<bool>(outputValues);
    }
}

/// <summary>
/// Stores information about a bus for header display
/// </summary>
public class BusHeaderInfo
{
    public string Title { get; }
    public int BitCount { get; }

    public BusHeaderInfo(string title, int bitCount)
    {
        Title = title;
        BitCount = bitCount;
    }
}

/// <summary>
/// Actions that can be triggered from truth table buttons
/// </summary>
public enum TruthTableAction
{
    None,
    Play,
    Stop,
    Clear
}
