using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Where_My_Slime
{
    // หน้า Pause ระหว่างต่อสู้: วาดทับ BattleScreen เป็นหน้าต่าง popup
    public class PauseScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;

        private readonly Action _goToMainMenu;
        private readonly Action _restart;
        private readonly Action _continueBattle;
        private readonly Action _playUIClick;

        private bool _hoveredPauseMainMenu;
        private bool _hoveredPauseRestart;
        private bool _hoveredPauseContinue;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public PauseScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            Action goToMainMenu,
            Action restart,
            Action continueBattle,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _goToMainMenu = goToMainMenu;
            _restart = restart;
            _continueBattle = continueBattle;
            _playUIClick = playUIClick;
        }

        private Rectangle GetPauseWindowRect() => new Rectangle(ScreenWidth / 2 - 450, ScreenHeight / 2 - 275, 900, 550);
        private Rectangle GetPauseTitleRect() => new Rectangle(ScreenWidth / 2 - 220, GetPauseWindowRect().Y - 40, 440, 75);
        private Rectangle GetPauseMainMenuButtonRect() => new Rectangle(GetPauseWindowRect().X + 60, GetPauseWindowRect().Bottom - 95, 220, 55);
        private Rectangle GetPauseRestartButtonRect() => new Rectangle(ScreenWidth / 2 - 110, GetPauseWindowRect().Bottom - 95, 220, 55);
        private Rectangle GetPauseContinueButtonRect() => new Rectangle(GetPauseWindowRect().Right - 280, GetPauseWindowRect().Bottom - 95, 220, 55);

        public void Update(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                _playUIClick?.Invoke();
                _continueBattle?.Invoke();
                return;
            }

            var mainMenuRect = GetPauseMainMenuButtonRect();
            var restartRect = GetPauseRestartButtonRect();
            var continueRect = GetPauseContinueButtonRect();

            _hoveredPauseMainMenu = mainMenuRect.Contains(mouse.Position);
            _hoveredPauseRestart = restartRect.Contains(mouse.Position);
            _hoveredPauseContinue = continueRect.Contains(mouse.Position);

            if (leftClicked)
            {
                if (_hoveredPauseMainMenu)
                {
                    _playUIClick?.Invoke();
                    _goToMainMenu?.Invoke();
                }
                else if (_hoveredPauseRestart)
                {
                    _playUIClick?.Invoke();
                    _restart?.Invoke();
                }
                else if (_hoveredPauseContinue)
                {
                    _playUIClick?.Invoke();
                    _continueBattle?.Invoke();
                }
            }
        }

        public void Draw()
        {
            DrawRect(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 120));

            var windowRect = GetPauseWindowRect();
            DrawRect(windowRect, new Color(173, 216, 230));
            DrawRectBorder(windowRect, Color.Black, 4);

            var titleRect = GetPauseTitleRect();
            DrawRect(titleRect, new Color(173, 216, 230));
            DrawRectBorder(titleRect, Color.Black, 4);
            DrawTextCentered("Game Pause", new Vector2(titleRect.Center.X, titleRect.Center.Y), Color.Black, 2.4f);

            var mainMenuRect = GetPauseMainMenuButtonRect();
            DrawRect(mainMenuRect, _hoveredPauseMainMenu ? new Color(255, 140, 0) : new Color(230, 115, 0));
            DrawRectBorder(mainMenuRect, Color.Black, 3);
            DrawTextCentered("Main Menu", new Vector2(mainMenuRect.Center.X, mainMenuRect.Center.Y), Color.Black, 1.1f);

            var restartRect = GetPauseRestartButtonRect();
            DrawRect(restartRect, _hoveredPauseRestart ? new Color(220, 50, 50) : new Color(190, 40, 40));
            DrawRectBorder(restartRect, Color.Black, 3);
            DrawTextCentered("Restart", new Vector2(restartRect.Center.X, restartRect.Center.Y), Color.White, 1.1f);

            var continueRect = GetPauseContinueButtonRect();
            DrawRect(continueRect, _hoveredPauseContinue ? new Color(60, 180, 60) : new Color(40, 150, 40));
            DrawRectBorder(continueRect, Color.Black, 3);
            DrawTextCentered("Continue", new Vector2(continueRect.Center.X, continueRect.Center.Y), Color.White, 1.1f);
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