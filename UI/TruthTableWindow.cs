using System;
using System.Collections.Generic;
using System.Linq;
using CPUgame.Components;
using CPUgame.Core;
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
    public bool IsSimulating { get; private set; }

    private Point _windowDragOffset;
    private List<TruthTableRow> _truthTableRows = new();
    private int _totalInputBits;
    private int _totalOutputBits;
    private List<string> _inputLabels = new();
    private List<string> _outputLabels = new();

    // Scrolling
    private int _scrollOffset;
    private int _maxVisibleRows = 16;
    private bool _isScrollbarDragging;
    private int _scrollbarDragStartY;
    private int _scrollbarDragStartOffset;

    // UI Constants
    private const int TitleHeight = 28;
    private const int HeaderHeight = 26;
    private const int RowHeight = 22;
    private const int ButtonHeight = 30;
    private const int Padding = 8;
    private const int CellWidth = 24;
    private const int MinWidth = 200;
    private const int ScrollbarWidth = 14;

    // Button bounds
    private Rectangle _simulateButtonRect;
    private bool _isSimulateButtonHovered;

    // Colors
    private static readonly Color BackgroundColor = new(45, 45, 55, 245);
    private static readonly Color TitleColor = new(55, 55, 65);
    private static readonly Color BorderColor = new(80, 80, 100);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color HeaderColor = new(50, 50, 65);
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
        _simulateButtonRect = new Rectangle(
            Bounds.X + Padding,
            Bounds.Bottom - Padding - ButtonHeight,
            Bounds.Width - Padding * 2,
            ButtonHeight);
    }

    public void SetPosition(int x, int y)
    {
        UpdateBounds(x, y, Bounds.Width, Bounds.Height);
    }

    /// <summary>
    /// Calculate window size based on input/output counts
    /// </summary>
    public void RecalculateSize(Circuit circuit)
    {
        CountBuses(circuit);

        int tableWidth = (_totalInputBits + _totalOutputBits) * CellWidth + Padding * 2 + ScrollbarWidth;
        int width = Math.Max(MinWidth, tableWidth + Padding * 2);

        // Height: title + header + rows + button + padding
        int totalRows = _truthTableRows.Count;
        int visibleRows = Math.Min(totalRows, _maxVisibleRows);
        int tableHeight = HeaderHeight + visibleRows * RowHeight;
        int height = TitleHeight + tableHeight + ButtonHeight + Padding * 4;

        UpdateBounds(Bounds.X, Bounds.Y, width, height);
    }

    private void CountBuses(Circuit circuit)
    {
        _totalInputBits = 0;
        _totalOutputBits = 0;
        _inputLabels.Clear();
        _outputLabels.Clear();

        // Count input pins from BusInput components
        foreach (var component in circuit.Components.OfType<BusInput>().OrderBy(c => c.Y).ThenBy(c => c.X))
        {
            for (int i = component.BitCount - 1; i >= 0; i--)
            {
                _inputLabels.Add($"{component.Title ?? component.Name}[{i}]");
                _totalInputBits++;
            }
        }

        // Count output pins from BusOutput components
        foreach (var component in circuit.Components.OfType<BusOutput>().OrderBy(c => c.Y).ThenBy(c => c.X))
        {
            for (int i = component.BitCount - 1; i >= 0; i--)
            {
                _outputLabels.Add($"{component.Title ?? component.Name}[{i}]");
                _totalOutputBits++;
            }
        }
    }

    /// <summary>
    /// Run simulation for all input combinations
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

        // Limit input bits to prevent excessive computation
        int effectiveInputBits = Math.Min(_totalInputBits, 16);
        int totalCombinations = 1 << effectiveInputBits;

        // Get input and output components
        var inputBuses = circuit.Components.OfType<BusInput>().OrderBy(c => c.Y).ThenBy(c => c.X).ToList();
        var outputBuses = circuit.Components.OfType<BusOutput>().OrderBy(c => c.Y).ThenBy(c => c.X).ToList();

        // Store original input values to restore later
        var originalValues = inputBuses.Select(b => b.Value).ToList();

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

    public void Update(Point mousePos, bool mousePressed, bool mouseJustPressed, bool mouseJustReleased, int scrollDelta)
    {
        if (!IsVisible)
        {
            return;
        }

        _isSimulateButtonHovered = _simulateButtonRect.Contains(mousePos);

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

    public bool HandleSimulateClick(Point mousePos, bool mouseJustPressed)
    {
        if (!IsVisible)
        {
            return false;
        }

        if (mouseJustPressed && _simulateButtonRect.Contains(mousePos))
        {
            return true;
        }

        return false;
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
        return Bounds.Height - TitleHeight - HeaderHeight - ButtonHeight - Padding * 4;
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
            Bounds.Y + TitleHeight + HeaderHeight + thumbY,
            ScrollbarWidth,
            thumbHeight);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Point mousePos)
    {
        if (!IsVisible)
        {
            return;
        }

        // Background
        spriteBatch.Draw(pixel, Bounds, BackgroundColor);

        // Title bar
        var titleBar = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, TitleHeight);
        spriteBatch.Draw(pixel, titleBar, TitleColor);

        // Title text
        var titleText = LocalizationManager.Get("truthtable.title");
        var titleSize = font.MeasureString(titleText);
        spriteBatch.DrawString(font, titleText,
            new Vector2(Bounds.X + (Bounds.Width - titleSize.X) / 2, Bounds.Y + (TitleHeight - titleSize.Y) / 2),
            TextColor);

        // Border
        DrawBorder(spriteBatch, pixel, Bounds, BorderColor, 2);

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
        else if (_truthTableRows.Count == 0)
        {
            // Show message to click simulate
            var clickSimText = LocalizationManager.Get("truthtable.click_simulate");
            var clickSimSize = font.MeasureString(clickSimText);
            spriteBatch.DrawString(font, clickSimText,
                new Vector2(Bounds.X + (Bounds.Width - clickSimSize.X) / 2, contentY + 40),
                new Color(150, 150, 170));
        }
        else
        {
            // Draw table header
            DrawHeader(spriteBatch, pixel, font, Bounds.X + Padding, contentY, tableWidth);

            // Draw table rows
            int rowsY = contentY + HeaderHeight;
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

        // Draw simulate button
        DrawButton(spriteBatch, pixel, font, _simulateButtonRect, LocalizationManager.Get("truthtable.simulate"), _isSimulateButtonHovered);
    }

    private void DrawHeader(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, int x, int y, int width)
    {
        // Header background
        spriteBatch.Draw(pixel, new Rectangle(x, y, width, HeaderHeight), HeaderColor);

        // Input header
        int inputWidth = _totalInputBits * CellWidth;
        var inputText = LocalizationManager.Get("truthtable.inputs");
        var inputSize = font.MeasureString(inputText);
        spriteBatch.DrawString(font, inputText,
            new Vector2(x + (inputWidth - inputSize.X) / 2, y + (HeaderHeight - inputSize.Y) / 2),
            TextColor);

        // Separator
        int separatorX = x + inputWidth;
        spriteBatch.Draw(pixel, new Rectangle(separatorX, y, 2, HeaderHeight), SeparatorColor);

        // Output header
        int outputWidth = _totalOutputBits * CellWidth;
        var outputText = LocalizationManager.Get("truthtable.outputs");
        var outputSize = font.MeasureString(outputText);
        spriteBatch.DrawString(font, outputText,
            new Vector2(separatorX + 2 + (outputWidth - outputSize.X) / 2, y + (HeaderHeight - outputSize.Y) / 2),
            TextColor);
    }

    private void DrawRow(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, int x, int y, int width, int rowIndex, TruthTableRow row)
    {
        // Row background
        var rowColor = rowIndex % 2 == 0 ? RowEvenColor : RowOddColor;
        spriteBatch.Draw(pixel, new Rectangle(x, y, width, RowHeight), rowColor);

        // Draw input values
        int cellX = x;
        foreach (var value in row.InputValues)
        {
            var cellText = value ? "1" : "0";
            var cellSize = font.MeasureString(cellText);
            var cellColor = value ? HighColor : LowColor;
            spriteBatch.DrawString(font, cellText,
                new Vector2(cellX + (CellWidth - cellSize.X) / 2, y + (RowHeight - cellSize.Y) / 2),
                cellColor);
            cellX += CellWidth;
        }

        // Separator
        spriteBatch.Draw(pixel, new Rectangle(cellX, y, 2, RowHeight), SeparatorColor);
        cellX += 2;

        // Draw output values
        foreach (var value in row.OutputValues)
        {
            var cellText = value ? "1" : "0";
            var cellSize = font.MeasureString(cellText);
            var cellColor = value ? HighColor : LowColor;
            spriteBatch.DrawString(font, cellText,
                new Vector2(cellX + (CellWidth - cellSize.X) / 2, y + (RowHeight - cellSize.Y) / 2),
                cellColor);
            cellX += CellWidth;
        }
    }

    private void DrawScrollbar(SpriteBatch spriteBatch, Texture2D pixel, Point mousePos)
    {
        int trackX = Bounds.Right - ScrollbarWidth - Padding;
        int trackY = Bounds.Y + TitleHeight + HeaderHeight;
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

    private void DrawButton(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, Rectangle rect, string text, bool isHovered)
    {
        var buttonColor = isHovered ? ButtonHoverColor : ButtonColor;
        spriteBatch.Draw(pixel, rect, buttonColor);
        DrawBorder(spriteBatch, pixel, rect, BorderColor, 1);

        var textSize = font.MeasureString(text);
        spriteBatch.DrawString(font, text,
            new Vector2(rect.X + (rect.Width - textSize.X) / 2, rect.Y + (rect.Height - textSize.Y) / 2),
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
