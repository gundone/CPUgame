using System;
using System.Collections.Generic;
using CPUgame.Core;
using CPUgame.Core.Input;
using CPUgame.Core.Localization;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CPUgame.UI;

/// <summary>
/// Dialog for profile selection and creation
/// </summary>
public class ProfileDialog
{
    public bool IsVisible { get; private set; }
    public bool IsCreatingProfile { get; private set; }

    private readonly IProfileService _profileService;
    private List<string> _profiles = new();
    private int _hoveredProfileIndex = -1;
    private bool _createButtonHovered;
    private bool _closeButtonHovered;
    private string _newProfileName = "";

    private const int DialogWidth = 350;
    private const int DialogHeight = 300;
    private const int TitleHeight = 32;
    private const int ProfileItemHeight = 36;
    private const int ButtonHeight = 32;
    private const int Padding = 12;
    private const int CloseButtonSize = 24;
    private const int InputHeight = 28;

    private static readonly Color OverlayColor = new(0, 0, 0, 200);
    private static readonly Color BackgroundColor = new(45, 45, 55);
    private static readonly Color TitleColor = new(55, 55, 65);
    private static readonly Color BorderColor = new(80, 80, 100);
    private static readonly Color TextColor = new(220, 220, 230);
    private static readonly Color ProfileItemColor = new(55, 55, 70);
    private static readonly Color ProfileItemHoverColor = new(70, 80, 100);
    private static readonly Color ButtonColor = new(60, 100, 140);
    private static readonly Color ButtonHoverColor = new(80, 120, 160);
    private static readonly Color InputBgColor = new(35, 35, 45);
    private static readonly Color CloseButtonColor = new(180, 60, 60);
    private static readonly Color CloseButtonHoverColor = new(220, 80, 80);
    private static readonly Color HintColor = new(150, 150, 170);

    private Rectangle _bounds;
    private Rectangle _closeButtonRect;
    private Rectangle _createButtonRect;
    private Rectangle _inputRect;

    public event Action? OnProfileSelected;
    public event Action? OnDialogClosed;

    public ProfileDialog(IProfileService profileService)
    {
        _profileService = profileService;
    }

    public void Show()
    {
        IsVisible = true;
        IsCreatingProfile = false;
        _newProfileName = "";
        _hoveredProfileIndex = -1;
        RefreshProfiles();
    }

    public void Hide()
    {
        IsVisible = false;
        IsCreatingProfile = false;
        OnDialogClosed?.Invoke();
    }

    public void StartCreatingProfile()
    {
        IsCreatingProfile = true;
        _newProfileName = "";
    }

    private void RefreshProfiles()
    {
        _profiles = _profileService.GetAvailableProfiles();
    }

    public void HandleInput(InputState input, IInputHandler inputHandler)
    {
        if (!IsVisible)
        {
            return;
        }

        // Handle escape to cancel or close
        if (input.EscapeCommand)
        {
            if (IsCreatingProfile)
            {
                IsCreatingProfile = false;
                inputHandler.EndTextInput();
            }
            else if (_profiles.Count > 0 || _profileService.HasProfile)
            {
                Hide();
            }
            return;
        }

        if (!IsCreatingProfile)
        {
            return;
        }

        // Handle enter to confirm
        if (input.EnterPressed && _newProfileName.Length > 0)
        {
            _profileService.CreateProfile(_newProfileName);
            IsCreatingProfile = false;
            inputHandler.EndTextInput();
            OnProfileSelected?.Invoke();
            Hide();
            return;
        }

        // Handle backspace
        if (input.BackspacePressed && _newProfileName.Length > 0)
        {
            _newProfileName = _newProfileName[..^1];
            return;
        }

        // Handle character input
        if (input.CharacterInput.HasValue && _newProfileName.Length < 20)
        {
            _newProfileName += input.CharacterInput.Value;
        }
    }

    public void Update(Point mousePos, bool mouseJustPressed, int screenWidth, int screenHeight, IInputHandler inputHandler)
    {
        if (!IsVisible)
        {
            return;
        }

        // Calculate dialog position (centered)
        int dialogX = (screenWidth - DialogWidth) / 2;
        int dialogY = (screenHeight - DialogHeight) / 2;
        _bounds = new Rectangle(dialogX, dialogY, DialogWidth, DialogHeight);

        // Close button (only show if we have profiles or are creating)
        _closeButtonRect = new Rectangle(
            _bounds.Right - CloseButtonSize - Padding,
            _bounds.Y + (TitleHeight - CloseButtonSize) / 2,
            CloseButtonSize,
            CloseButtonSize);
        _closeButtonHovered = _closeButtonRect.Contains(mousePos) && (_profiles.Count > 0 || _profileService.HasProfile);

        if (mouseJustPressed && _closeButtonHovered)
        {
            if (IsCreatingProfile)
            {
                IsCreatingProfile = false;
                inputHandler.EndTextInput();
            }
            else
            {
                Hide();
            }
            return;
        }

        if (IsCreatingProfile)
        {
            // Input field for new profile name
            _inputRect = new Rectangle(
                _bounds.X + Padding,
                _bounds.Y + TitleHeight + Padding * 2,
                DialogWidth - Padding * 2,
                InputHeight);

            // Create button
            _createButtonRect = new Rectangle(
                _bounds.X + Padding,
                _inputRect.Bottom + Padding,
                DialogWidth - Padding * 2,
                ButtonHeight);
            _createButtonHovered = _createButtonRect.Contains(mousePos);

            if (mouseJustPressed && _createButtonHovered && _newProfileName.Length > 0)
            {
                _profileService.CreateProfile(_newProfileName);
                IsCreatingProfile = false;
                inputHandler.EndTextInput();
                OnProfileSelected?.Invoke();
                Hide();
            }
        }
        else
        {
            // Profile list
            _hoveredProfileIndex = -1;
            int y = _bounds.Y + TitleHeight + Padding;

            for (int i = 0; i < _profiles.Count; i++)
            {
                var profileRect = new Rectangle(_bounds.X + Padding, y, DialogWidth - Padding * 2, ProfileItemHeight);
                if (profileRect.Contains(mousePos))
                {
                    _hoveredProfileIndex = i;

                    if (mouseJustPressed)
                    {
                        _profileService.LoadProfile(_profiles[i]);
                        OnProfileSelected?.Invoke();
                        Hide();
                        return;
                    }
                }
                y += ProfileItemHeight + 4;
            }

            // Create new profile button
            _createButtonRect = new Rectangle(
                _bounds.X + Padding,
                _bounds.Bottom - Padding - ButtonHeight,
                DialogWidth - Padding * 2,
                ButtonHeight);
            _createButtonHovered = _createButtonRect.Contains(mousePos);

            if (mouseJustPressed && _createButtonHovered)
            {
                StartCreatingProfile();
                inputHandler.BeginTextInput();
            }
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFontBase font, int screenWidth, int screenHeight)
    {
        if (!IsVisible)
        {
            return;
        }

        // Draw overlay
        spriteBatch.Draw(pixel, new Rectangle(0, 0, screenWidth, screenHeight), OverlayColor);

        // Draw dialog background
        spriteBatch.Draw(pixel, _bounds, BackgroundColor);
        DrawBorder(spriteBatch, pixel, _bounds, BorderColor, 2);

        // Draw title bar
        var titleBar = new Rectangle(_bounds.X, _bounds.Y, _bounds.Width, TitleHeight);
        spriteBatch.Draw(pixel, titleBar, TitleColor);

        var titleText = IsCreatingProfile
            ? LocalizationManager.Get("profile.create")
            : LocalizationManager.Get("profile.select");
        var titleSize = font.MeasureString(titleText);
        font.DrawText(spriteBatch, titleText,
            new Vector2(_bounds.X + Padding, _bounds.Y + (TitleHeight - titleSize.Y) / 2),
            TextColor);

        // Draw close button (if allowed)
        if (_profiles.Count > 0 || _profileService.HasProfile || IsCreatingProfile)
        {
            spriteBatch.Draw(pixel, _closeButtonRect, _closeButtonHovered ? CloseButtonHoverColor : CloseButtonColor);
            var xText = "X";
            var xSize = font.MeasureString(xText);
            font.DrawText(spriteBatch, xText,
                new Vector2(_closeButtonRect.X + (_closeButtonRect.Width - xSize.X) / 2,
                           _closeButtonRect.Y + (_closeButtonRect.Height - xSize.Y) / 2),
                TextColor);
        }

        if (IsCreatingProfile)
        {
            // Draw input field
            spriteBatch.Draw(pixel, _inputRect, InputBgColor);
            DrawBorder(spriteBatch, pixel, _inputRect, BorderColor, 1);

            var displayText = _newProfileName + "_";
            var textSize = font.MeasureString(displayText);
            font.DrawText(spriteBatch, displayText,
                new Vector2(_inputRect.X + Padding, _inputRect.Y + (_inputRect.Height - textSize.Y) / 2),
                TextColor);

            // Draw hint
            var hintText = LocalizationManager.Get("profile.name_hint");
            var hintSize = font.MeasureString(hintText);
            font.DrawText(spriteBatch, hintText,
                new Vector2(_bounds.X + (_bounds.Width - hintSize.X) / 2, _inputRect.Bottom + (float)Padding / 2),
                HintColor);

            // Draw create button
            spriteBatch.Draw(pixel, _createButtonRect, _createButtonHovered && _newProfileName.Length > 0 ? ButtonHoverColor : ButtonColor);
            DrawBorder(spriteBatch, pixel, _createButtonRect, BorderColor, 1);

            var createText = LocalizationManager.Get("profile.confirm");
            var createSize = font.MeasureString(createText);
            font.DrawText(spriteBatch, createText,
                new Vector2(_createButtonRect.X + (_createButtonRect.Width - createSize.X) / 2,
                           _createButtonRect.Y + (_createButtonRect.Height - createSize.Y) / 2),
                TextColor);
        }
        else
        {
            // Draw profile list
            int y = _bounds.Y + TitleHeight + Padding;

            if (_profiles.Count == 0)
            {
                var noProfileText = LocalizationManager.Get("profile.no_profiles");
                var noProfileSize = font.MeasureString(noProfileText);
                font.DrawText(spriteBatch, noProfileText,
                    new Vector2(_bounds.X + (_bounds.Width - noProfileSize.X) / 2, y + Padding),
                    HintColor);
            }
            else
            {
                for (int i = 0; i < _profiles.Count; i++)
                {
                    var profileRect = new Rectangle(_bounds.X + Padding, y, DialogWidth - Padding * 2, ProfileItemHeight);
                    bool isHovered = i == _hoveredProfileIndex;

                    spriteBatch.Draw(pixel, profileRect, isHovered ? ProfileItemHoverColor : ProfileItemColor);
                    DrawBorder(spriteBatch, pixel, profileRect, BorderColor, 1);

                    var profileName = _profiles[i];
                    var profileSize = font.MeasureString(profileName);
                    font.DrawText(spriteBatch, profileName,
                        new Vector2(profileRect.X + Padding, profileRect.Y + (ProfileItemHeight - profileSize.Y) / 2),
                        TextColor);

                    y += ProfileItemHeight + 4;
                }
            }

            // Draw create new profile button
            spriteBatch.Draw(pixel, _createButtonRect, _createButtonHovered ? ButtonHoverColor : ButtonColor);
            DrawBorder(spriteBatch, pixel, _createButtonRect, BorderColor, 1);

            var newProfileText = LocalizationManager.Get("profile.new");
            var newProfileSize = font.MeasureString(newProfileText);
            font.DrawText(spriteBatch, newProfileText,
                new Vector2(_createButtonRect.X + (_createButtonRect.Width - newProfileSize.X) / 2,
                           _createButtonRect.Y + (_createButtonRect.Height - newProfileSize.Y) / 2),
                TextColor);
        }
    }

    private void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness)
    {
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
        spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
    }

    public bool ContainsPoint(Point p)
    {
        return IsVisible && _bounds.Contains(p);
    }
}
