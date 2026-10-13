using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Where_My_Slime
{
    // หน้าพักค่าย (ปัจจุบันเป็นหน้าทางผ่านง่ายๆ ก่อนไปหน้า Map)
    public class RestScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GameSession _session;

        private readonly Action _continueToMap;
        private readonly Action _playUIClick;

        private bool _hoveredRestContinue;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public RestScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            GameSession session,
            Action continueToMap,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _session = session;
            _continueToMap = continueToMap;
            _playUIClick = playUIClick;
        }

        private Rectangle GetRestContinueButtonRect() => new Rectangle(ScreenWidth - 280, ScreenHeight - 100, 220, 64);

        public void Update(MouseState mouse, bool leftClicked)
        {
            _hoveredRestContinue = GetRestContinueButtonRect().Contains(mouse.Position);
            if (leftClicked && _hoveredRestContinue)
            {
                _playUIClick?.Invoke();
                _continueToMap?.Invoke();
            }
        }

        public void Draw()
        {
            DrawTextCentered("Rest Camp", new Vector2(ScreenWidth / 2f, 120), Color.Gold, 3f);
            DrawTextCentered($"Coins: {_session.Coins}     |     Inventory: {_session.Inventory.Count} item(s) available", new Vector2(ScreenWidth / 2f, 195), Color.White, 1.2f);

            var contRect = GetRestContinueButtonRect();
            DrawRect(contRect, _hoveredRestContinue ? Color.Gold : new Color(50, 120, 50));
            DrawRectBorder(contRect, Color.White, 3);
            DrawTextCentered("Continue ->", new Vector2(contRect.Center.X, contRect.Center.Y), Color.White, 1.1f);
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