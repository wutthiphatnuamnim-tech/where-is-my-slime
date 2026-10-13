using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Where_My_Slime
{
    // หน้าโลโก้เปิดเกม: โชว์ภาพโลโก้ไว้ช่วงเวลาสั้นๆ แล้วไปหน้า Intro
    public class LogoScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly Texture2D _logoBackground;

        private readonly Action _goToIntro;

        private float _logoTimer;
        private const float LogoDuration = 2f;
        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public LogoScreen(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, Texture2D logoBackground, Action goToIntro)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _logoBackground = logoBackground;
            _goToIntro = goToIntro;
        }

        public void Reset()
        {
            _logoTimer = 0f;
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, KeyboardState previousKeyboard, bool leftClicked)
        {
            _logoTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            bool anyNewKeyPressed = false;
            var pressedKeys = keyboard.GetPressedKeys();
            for (int i = 0; i < pressedKeys.Length; i++)
            {
                if (!previousKeyboard.IsKeyDown(pressedKeys[i]))
                {
                    anyNewKeyPressed = true;
                    break;
                }
            }

            if (_logoTimer >= LogoDuration || leftClicked || anyNewKeyPressed)
            {
                _goToIntro?.Invoke();
            }
        }

        public void Draw()
        {
            if (_logoBackground != null)
                _spriteBatch.Draw(_logoBackground, new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White);
            else
                DrawTextCentered("LOGO", new Vector2(ScreenWidth / 2f, ScreenHeight / 2f), Color.White, 3f);
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