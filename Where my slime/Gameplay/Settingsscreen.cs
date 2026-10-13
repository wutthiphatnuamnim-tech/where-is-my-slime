using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Where_My_Slime
{
    // หน้า Setting: วาดทับหน้าจอเดิม (MainMenu หรือ Battle) เป็นหน้าต่าง popup เล็กๆ
    public class SettingsScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GameSession _session;

        private readonly Action _close;
        private readonly Action<bool> _onToggleCameraShake;
        private readonly Action _playUIClick;

        private bool _hoveredSettingsClose;
        private bool _hoveredSettingsToggle;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public SettingsScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            GameSession session,
            Action close,
            Action<bool> onToggleCameraShake,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _session = session;
            _close = close;
            _onToggleCameraShake = onToggleCameraShake;
            _playUIClick = playUIClick;
        }

        private Rectangle GetSettingsWindowRect() => new Rectangle(ScreenWidth / 2 - 450, ScreenHeight / 2 - 275, 900, 550);
        private Rectangle GetSettingsCloseButtonRect() => new Rectangle(GetSettingsWindowRect().Right - 70, GetSettingsWindowRect().Y + 20, 50, 50);
        private Rectangle GetSettingsToggleRect() => new Rectangle(GetSettingsWindowRect().X + 620, GetSettingsWindowRect().Y + 170, 140, 45);

        public void Update(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                _playUIClick?.Invoke();
                _close?.Invoke();
                return;
            }

            var closeRect = GetSettingsCloseButtonRect();
            var toggleRect = GetSettingsToggleRect();

            _hoveredSettingsClose = closeRect.Contains(mouse.Position);
            _hoveredSettingsToggle = toggleRect.Contains(mouse.Position);

            if (leftClicked)
            {
                if (_hoveredSettingsClose)
                {
                    _playUIClick?.Invoke();
                    _close?.Invoke();
                }
                else if (_hoveredSettingsToggle)
                {
                    _playUIClick?.Invoke();
                    _session.CameraShakeEnabled = !_session.CameraShakeEnabled;
                    _onToggleCameraShake?.Invoke(_session.CameraShakeEnabled);
                }
            }
        }

        public void Draw()
        {
            DrawRect(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 150));

            var windowRect = GetSettingsWindowRect();
            DrawRect(windowRect, Color.White);
            DrawRectBorder(windowRect, Color.Black, 4);

            DrawTextCentered("Setting", new Vector2(windowRect.Center.X, windowRect.Y + 50), Color.Black, 2.2f);

            DrawText("Camera Shake", new Vector2(windowRect.X + 80, windowRect.Y + 175), Color.Black, 1.5f);

            var toggleRect = GetSettingsToggleRect();
            DrawRect(toggleRect, new Color(220, 220, 220));
            DrawRectBorder(toggleRect, Color.Black, 2);

            DrawTextCentered("ON", new Vector2(toggleRect.X + 35, toggleRect.Center.Y), _session.CameraShakeEnabled ? Color.Transparent : Color.Gray, 1.1f);
            DrawTextCentered("OFF", new Vector2(toggleRect.Right - 35, toggleRect.Center.Y), !_session.CameraShakeEnabled ? Color.Transparent : Color.Gray, 1.1f);

            int boxWidth = 75;
            int boxX = _session.CameraShakeEnabled ? toggleRect.X : toggleRect.Right - boxWidth;
            var sliderBox = new Rectangle(boxX, toggleRect.Y, boxWidth, toggleRect.Height);

            Color sliderColor = _session.CameraShakeEnabled ? new Color(40, 160, 60) : new Color(180, 50, 50);
            string sliderText = _session.CameraShakeEnabled ? "ON" : "OFF";

            DrawRect(sliderBox, sliderColor);
            DrawRectBorder(sliderBox, Color.Black, 2);
            DrawTextCentered(sliderText, new Vector2(sliderBox.Center.X, sliderBox.Center.Y), Color.White, 1.1f);

            var closeRect = GetSettingsCloseButtonRect();
            DrawRect(closeRect, _hoveredSettingsClose ? Color.DarkRed : new Color(220, 40, 40));
            DrawRectBorder(closeRect, Color.Black, 3);
            DrawTextCentered("X", new Vector2(closeRect.Center.X, closeRect.Center.Y - 2), Color.White, 1.8f);
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

        private void DrawText(string text, Vector2 position, Color color, float scale = 1f)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;
            try { _spriteBatch.DrawString(_font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f); }
            catch (ArgumentException) { _spriteBatch.DrawString(_font, GetSafeText(text), position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f); }
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