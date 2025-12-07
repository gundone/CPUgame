using System;
using System.Collections.Generic;
using System.Linq;
using CPUgame.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

/// <summary>
/// Modal popup for level selection with tier-based progression
/// </summary>
public class LevelSelectionPopup
{
    public bool IsVisible { get; set; }

    private readonly ILevelService _levelService;
    private readonly IProfileService _profileService;
    private List<LevelTier> _tiers = new();
    private int _hoveredLevelIndex = -1;
    private int _hoveredTierIndex = -1;

    private const int PopupWidth = 400;
    private const int PopupHeight = 350;
    private const int TitleHeight = 32;
    private const int TierHeaderHeight = 28;
    private const int LevelItemHeight = 36;
    private const int Padding = 12;
    private const int CloseButtonSize = 24;
    private const int CheckMarkSize = 18;

    private static readonly Color OverlayColor = new(0, 0, 0, 180);
    private static readonly Color BackgroundColor = new(45, 45, 55);
    private static readonly Color TitleColor = new(55, 55, 65);
    private static readonly Color BorderColor = new(80, 80, 100);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color TierHeaderColor = new(50, 50, 60);
    private static readonly Color LevelItemColor = new(55, 55, 70);
    private static readonly Color LevelItemHoverColor = new(70, 80, 100);
    private static readonly Color LevelLockedColor = new(40, 40, 50);
    private static readonly Color LevelCompletedColor = new(50, 70, 55);
    private static readonly Color CheckMarkColor = new(80, 200, 120);
    private static readonly Color LockedTextColor = new(100, 100, 110);
    private static readonly Color CloseButtonColor = new(180, 60, 60);
    private static readonly Color CloseButtonHoverColor = new(220, 80, 80);

    private Rectangle _bounds;
    private Rectangle _closeButtonRect;
    private bool _closeButtonHovered;
    private int _scrollOffset;
    private int _contentHeight;

    public event Action<int>? OnLevelSelected;

    public LevelSelectionPopup(ILevelService levelService, IProfileService profileService)
    {
        _levelService = levelService;
        _profileService = profileService;
    }

    public void Show()
    {
        IsVisible = true;
        _scrollOffset = 0;
        _hoveredLevelIndex = -1;
        _hoveredTierIndex = -1;
        BuildTierStructure();
    }

    public void Hide()
    {
        IsVisible = false;
    }

    private void BuildTierStructure()
    {
        _tiers.Clear();

        // Group levels by tier
        var tierGroups = _levelService.Levels
            .GroupBy(l => l.Tier)
            .OrderBy(g => g.Key);

        foreach (var group in tierGroups)
        {
            var tier = new LevelTier
            {
                TierNumber = group.Key,
                IsUnlocked = _profileService.IsTierUnlocked(group.Key, _levelService),
                Levels = group.ToList()
            };
            _tiers.Add(tier);
        }

        // Calculate content height
        _contentHeight = 0;
        foreach (var tier in _tiers)
        {
            _contentHeight += TierHeaderHeight + tier.Levels.Count * LevelItemHeight + Padding;
        }
    }

    public void Update(Point mousePos, bool mouseJustPressed, int scrollDelta, int screenWidth, int screenHeight)
    {
        if (!IsVisible)
        {
            return;
        }

        // Calculate popup position (centered)
        int popupX = (screenWidth - PopupWidth) / 2;
        int popupY = (screenHeight - PopupHeight) / 2;
        _bounds = new Rectangle(popupX, popupY, PopupWidth, PopupHeight);

        // Close button
        _closeButtonRect = new Rectangle(
            _bounds.Right - CloseButtonSize - Padding,
            _bounds.Y + (TitleHeight - CloseButtonSize) / 2,
            CloseButtonSize,
            CloseButtonSize);
        _closeButtonHovered = _closeButtonRect.Contains(mousePos);

        if (mouseJustPressed && _closeButtonHovered)
        {
            Hide();
            return;
        }

        // Click outside to close
        if (mouseJustPressed && !_bounds.Contains(mousePos))
        {
            Hide();
            return;
        }

        // Handle scroll
        if (_bounds.Contains(mousePos) && scrollDelta != 0)
        {
            int maxScroll = Math.Max(0, _contentHeight - (PopupHeight - TitleHeight - Padding * 2));
            _scrollOffset -= scrollDelta / 120 * LevelItemHeight;
            _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
        }

        // Check level hover and click
        _hoveredLevelIndex = -1;
        _hoveredTierIndex = -1;

        int y = _bounds.Y + TitleHeight + Padding - _scrollOffset;
        int levelIndex = 0;

        for (int tierIdx = 0; tierIdx < _tiers.Count; tierIdx++)
        {
            var tier = _tiers[tierIdx];
            y += TierHeaderHeight;

            foreach (var level in tier.Levels)
            {
                var levelRect = new Rectangle(_bounds.X + Padding, y, PopupWidth - Padding * 2, LevelItemHeight);

                if (levelRect.Contains(mousePos) && levelRect.Bottom > _bounds.Y + TitleHeight && levelRect.Y < _bounds.Bottom - Padding)
                {
                    _hoveredTierIndex = tierIdx;
                    _hoveredLevelIndex = levelIndex;

                    if (mouseJustPressed && tier.IsUnlocked)
                    {
                        int globalIndex = _levelService.Levels.IndexOf(level);
                        if (globalIndex >= 0)
                        {
                            OnLevelSelected?.Invoke(globalIndex);
                            Hide();
                        }
                    }
                }

                y += LevelItemHeight;
                levelIndex++;
            }

            y += Padding;
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, int screenWidth, int screenHeight)
    {
        if (!IsVisible)
        {
            return;
        }

        // Draw overlay
        spriteBatch.Draw(pixel, new Rectangle(0, 0, screenWidth, screenHeight), OverlayColor);

        // Draw popup background
        spriteBatch.Draw(pixel, _bounds, BackgroundColor);
        DrawBorder(spriteBatch, pixel, _bounds, BorderColor, 2);

        // Draw title bar
        var titleBar = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, TitleHeight);
        spriteBatch.Draw(pixel, titleBar, TitleColor);

        var titleText = LocalizationManager.Get("levelselect.title");
        var titleSize = font.MeasureString(titleText);
        spriteBatch.DrawString(font, titleText,
            new Vector2(_bounds.X + Padding, _bounds.Y + (TitleHeight - titleSize.Y) / 2),
            TextColor);

        // Draw close button
        spriteBatch.Draw(pixel, _closeButtonRect, _closeButtonHovered ? CloseButtonHoverColor : CloseButtonColor);
        var xText = "X";
        var xSize = font.MeasureString(xText);
        spriteBatch.DrawString(font, xText,
            new Vector2(_closeButtonRect.X + (_closeButtonRect.Width - xSize.X) / 2,
                       _closeButtonRect.Y + (_closeButtonRect.Height - xSize.Y) / 2),
            TextColor);

        // Set up scissor rectangle for content clipping
        var contentArea = new Rectangle(_bounds.X, _bounds.Y + TitleHeight, _bounds.Width, _bounds.Height - TitleHeight);

        // Draw tiers and levels
        int y = _bounds.Y + TitleHeight + Padding - _scrollOffset;
        int levelIndex = 0;

        for (int tierIdx = 0; tierIdx < _tiers.Count; tierIdx++)
        {
            var tier = _tiers[tierIdx];

            // Tier header
            if (y + TierHeaderHeight > _bounds.Y + TitleHeight && y < _bounds.Bottom)
            {
                var tierHeaderRect = new Rectangle(_bounds.X + Padding, y, PopupWidth - Padding * 2, TierHeaderHeight);
                spriteBatch.Draw(pixel, tierHeaderRect, TierHeaderColor);

                var tierName = LocalizationManager.Get($"levelselect.tier{tier.TierNumber}");
                if (string.IsNullOrEmpty(tierName) || tierName.StartsWith("levelselect."))
                {
                    tierName = $"Part {tier.TierNumber}";
                }

                if (!tier.IsUnlocked)
                {
                    tierName += " [" + LocalizationManager.Get("levelselect.locked") + "]";
                }

                var tierSize = font.MeasureString(tierName);
                spriteBatch.DrawString(font, tierName,
                    new Vector2(tierHeaderRect.X + Padding, tierHeaderRect.Y + (TierHeaderHeight - tierSize.Y) / 2),
                    tier.IsUnlocked ? TextColor : LockedTextColor);
            }

            y += TierHeaderHeight;

            // Levels in this tier
            foreach (var level in tier.Levels)
            {
                var levelRect = new Rectangle(_bounds.X + Padding, y, PopupWidth - Padding * 2, LevelItemHeight);

                if (levelRect.Bottom > _bounds.Y + TitleHeight && levelRect.Y < _bounds.Bottom - Padding)
                {
                    bool isCompleted = _profileService.IsLevelCompleted(level.Id);
                    bool isHovered = tierIdx == _hoveredTierIndex && levelIndex == _hoveredLevelIndex;

                    Color bgColor;
                    if (!tier.IsUnlocked)
                    {
                        bgColor = LevelLockedColor;
                    }
                    else if (isCompleted)
                    {
                        bgColor = isHovered ? LevelItemHoverColor : LevelCompletedColor;
                    }
                    else
                    {
                        bgColor = isHovered ? LevelItemHoverColor : LevelItemColor;
                    }

                    spriteBatch.Draw(pixel, levelRect, bgColor);
                    DrawBorder(spriteBatch, pixel, levelRect, BorderColor, 1);

                    // Level name - use localized name if available
                    var levelNameKey = $"level.{level.Id}.name";
                    var levelName = LocalizationManager.Get(levelNameKey);
                    if (levelName == levelNameKey)
                    {
                        levelName = level.Name;
                    }
                    var levelSize = font.MeasureString(levelName);
                    spriteBatch.DrawString(font, levelName,
                        new Vector2(levelRect.X + Padding, levelRect.Y + (LevelItemHeight - levelSize.Y) / 2),
                        tier.IsUnlocked ? TextColor : LockedTextColor);

                    // Completion checkmark
                    if (isCompleted)
                    {
                        var checkRect = new Rectangle(
                            levelRect.Right - CheckMarkSize - Padding,
                            levelRect.Y + (LevelItemHeight - CheckMarkSize) / 2,
                            CheckMarkSize,
                            CheckMarkSize);

                        // Draw checkmark (V)
                        var checkText = "V";
                        var checkSize = font.MeasureString(checkText);
                        spriteBatch.DrawString(font, checkText,
                            new Vector2(checkRect.X + (checkRect.Width - checkSize.X) / 2,
                                       checkRect.Y + (checkRect.Height - checkSize.Y) / 2),
                            CheckMarkColor);
                    }
                }

                y += LevelItemHeight;
                levelIndex++;
            }

            y += Padding;
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }
}

internal class LevelTier
{
    public int TierNumber { get; set; }
    public bool IsUnlocked { get; set; }
    public List<GameLevel> Levels { get; set; } = new();
}
