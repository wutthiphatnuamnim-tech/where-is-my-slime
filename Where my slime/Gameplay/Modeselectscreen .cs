using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Where_My_Slime
{
    // หน้าเลือกโหมด: Story Mode / Challenge Mode
    public class ModeSelectScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        private readonly Action _goToMainMenu;
        private readonly Action _goToStoryModeSelect;
        private readonly Action _startNewRunForChallenge;
        private readonly Action _playUIClick;

        private bool _hoveredModeStory;
        private bool _hoveredModeChallenge;
        private bool _hoveredModeBack;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public ModeSelectScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            Action goToMainMenu,
            Action goToStoryModeSelect,
            Action startNewRunForChallenge,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _goToMainMenu = goToMainMenu;
            _goToStoryModeSelect = goToStoryModeSelect;
            _startNewRunForChallenge = startNewRunForChallenge;
            _playUIClick = playUIClick;
        }

        private Rectangle GetModeSelectStoryCardRect() => new Rectangle(ScreenWidth / 2 - 440, ScreenHeight / 2 - 250, 400, 500);
        private Rectangle GetModeSelectChallengeCardRect() => new Rectangle(ScreenWidth / 2 + 40, ScreenHeight / 2 - 250, 400, 500);
        private Rectangle GetModeSelectBackRect() => new Rectangle(40, 40, 150, 50);

        public void Update(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                _playUIClick?.Invoke();
                _goToMainMenu?.Invoke();
                return;
            }

            // Story Mode ปิดไว้ชั่วคราว กดไม่ได้ จึงไม่ต้องเช็ค hover/คลิกของการ์ดนี้เลย
            _hoveredModeStory = false;
            _hoveredModeChallenge = GetModeSelectChallengeCardRect().Contains(mouse.Position);
            _hoveredModeBack = GetModeSelectBackRect().Contains(mouse.Position);

            if (leftClicked)
            {
                if (_hoveredModeBack) { _playUIClick?.Invoke(); _goToMainMenu?.Invoke(); }
                else if (_hoveredModeChallenge) { _playUIClick?.Invoke(); _startNewRunForChallenge?.Invoke(); }
            }
        }

        public void Draw()
        {
            DrawTextCentered("Select Game Mode", new Vector2(ScreenWidth / 2f, 120), Color.Gold, 2.8f);

            var storyRect = GetModeSelectStoryCardRect();
            DrawRect(storyRect, new Color(22, 22, 30));
            DrawRectBorder(storyRect, Color.DimGray, 3);
            DrawTextCentered("Story Mode", new Vector2(storyRect.Center.X, storyRect.Center.Y - 25), Color.Gray, 2f);
            DrawTextCentered("(Locked)", new Vector2(storyRect.Center.X, storyRect.Center.Y + 35), Color.DarkGray, 1.4f);

            var challengeRect = GetModeSelectChallengeCardRect();
            DrawRect(challengeRect, _hoveredModeChallenge ? new Color(50, 50, 75) : new Color(30, 30, 45));
            DrawRectBorder(challengeRect, _hoveredModeChallenge ? Color.Gold : Color.White, _hoveredModeChallenge ? 5 : 3);
            DrawTextCentered("Challenge mode", new Vector2(challengeRect.Center.X, challengeRect.Center.Y), Color.White, 2f);

            var backRect = GetModeSelectBackRect();
            DrawRect(backRect, _hoveredModeBack ? Color.DarkRed : new Color(50, 30, 30));
            DrawRectBorder(backRect, Color.White, 2);
            DrawTextCentered("Back", new Vector2(backRect.Center.X, backRect.Center.Y), Color.White, 1f);
        }

        private void DrawRect(Rectangle rect, Color color) => _spriteBatch.Draw(_pixel, rect, color);

        private void DrawRectBorder(Rectangle rect, Color color, int thickness)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private string GetSafeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            char[] chars = text.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] > 126 || chars[i] < 32)
                {
                    if (chars[i] != '\n' && chars[i] != '\r' && chars[i] != '\t') chars[i] = '?';
                }
            }
            return new string(chars);
        }

        private void DrawTextCentered(string text, Vector2 center, Color color, float scale = 1f)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;
            Vector2 size;
            try { size = _font.MeasureString(text); } catch (ArgumentException) { text = GetSafeText(text); size = _font.MeasureString(text); }
            try { _spriteBatch.DrawString(_font, text, center, color, 0f, size / 2f, scale, SpriteEffects.None, 0f); } catch (ArgumentException) { }
        }
    }
}