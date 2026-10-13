using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Where_My_Slime
{
    // หน้าเปิดเรื่อง: ค่อยๆ ไล่ข้อความทีละตัวอักษร กดอะไรก็ได้เพื่อเร่ง/ข้าม แล้วไปหน้า MainMenu
    public class IntroScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly Texture2D _introBackground;

        private readonly Action _goToMainMenu;

        private float _introTimer;
        private const string IntroText = "Test 1 2 3 4 5 4RindaHAHAHA\n Tomyamkung";
        private const float IntroCharsPerSecond = 30f;
        private const float IntroInputGracePeriod = 0.2f;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;
        private const int IntroTextBoxHeight = 200;
        private const int IntroDividerThickness = 6;
        private const float IntroTextScale = 2.5f;

        public IntroScreen(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, Texture2D introBackground, Action goToMainMenu)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _introBackground = introBackground;
            _goToMainMenu = goToMainMenu;
        }

        public void Reset()
        {
            _introTimer = 0f;
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, KeyboardState previousKeyboard, bool leftClicked)
        {
            _introTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

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

            bool inputPressed = leftClicked || anyNewKeyPressed;
            bool pastGracePeriod = _introTimer >= IntroInputGracePeriod;

            int totalChars = IntroText.Length;
            float fullRevealTime = totalChars / IntroCharsPerSecond;
            bool fullyRevealed = _introTimer >= fullRevealTime;

            if (inputPressed && pastGracePeriod)
            {
                if (!fullyRevealed)
                    _introTimer = fullRevealTime;
                else
                    _goToMainMenu?.Invoke();
            }
        }

        public void Draw()
        {
            int lineY = ScreenHeight - IntroTextBoxHeight - IntroDividerThickness;

            if (_introBackground != null)
                _spriteBatch.Draw(_introBackground, new Rectangle(0, 0, ScreenWidth, lineY), Color.White);

            var lineRect = new Rectangle(0, lineY, ScreenWidth, IntroDividerThickness);
            DrawRect(lineRect, Color.Black);

            var textBoxRect = new Rectangle(0, lineY + IntroDividerThickness, ScreenWidth, IntroTextBoxHeight);
            DrawRect(textBoxRect, new Color(0, 0, 0, 210));

            int totalChars = IntroText.Length;
            int revealedChars = Math.Min(totalChars, (int)(_introTimer * IntroCharsPerSecond));
            string revealedText = IntroText.Substring(0, revealedChars);
            DrawText(revealedText, new Vector2(60, textBoxRect.Y + 30), Color.White, IntroTextScale);

            bool fullyRevealed = revealedChars >= totalChars;
            if (fullyRevealed)
                DrawTextCentered("Click or press any key to continue", new Vector2(ScreenWidth / 2f, textBoxRect.Bottom - 30), Color.LightGray, 0.8f);
        }

        private void DrawRect(Rectangle rect, Color color) => _spriteBatch.Draw(_pixel, rect, color);

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