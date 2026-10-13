using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Where_My_Slime.Manager;

namespace Where_My_Slime
{
    // หน้าเมนูหลัก: Play / Setting / Credit / Exit
    public class MainMenuScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly Texture2D _titleTexture;

        private readonly Action _goToModeSelect;
        private readonly Action _openSettings;
        private readonly Action _goToCredits;
        private readonly Action _exitGame;
        private readonly Action _playUIClick;

        private int _hoveredMenuIndex = -1;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        private const string GameTitle = "Where is my slime";
        private const int TitleY = 230;
        private static readonly string[] MenuLabels = { "Play", "Setting", "Credit", "Exit" };
        private static readonly bool[] MenuEnabled = { true, true, true, true };

        private const int MenuButtonSpacing = 110;
        private const float MenuButtonScale = 2.4f;
        private const float TitleScale = 4.5f;
        private const int MenuRightMargin = 80;
        private const float TitleImageTargetWidth = 980f; // ความกว้างเป้าหมายของรูปไตเติ้ล (ย่อ/ขยายตามสัดส่วนเดิม)
        private const float TitleOffsetX = 220f; // ขยับรูปไตเติ้ลไปทางขวาเพิ่ม เพราะตัวไฟล์รูปมีพื้นที่โปร่งใสเหลือด้านขวาเยอะ ทำให้ขอบรูปจริง (ที่คำนวณชิดขวา) ดูไม่ชิดขวาจริงๆ

        private struct MenuButtonLayout
        {
            public Rectangle Rect;
            public Vector2 Center;
            public Vector2 TextSize;
        }

        public MainMenuScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            Texture2D titleTexture,
            Action goToModeSelect,
            Action openSettings,
            Action goToCredits,
            Action exitGame,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _titleTexture = titleTexture;
            _goToModeSelect = goToModeSelect;
            _openSettings = openSettings;
            _goToCredits = goToCredits;
            _exitGame = exitGame;
            _playUIClick = playUIClick;
        }

        private MenuButtonLayout GetMenuButtonLayout(int index)
        {
            if (_font == null)
                return new MenuButtonLayout { Rect = Rectangle.Empty, Center = Vector2.Zero, TextSize = Vector2.Zero };

            string label = MenuLabels[index];
            Vector2 rawSize = _font.MeasureString(label);
            Vector2 scaledSize = rawSize * MenuButtonScale;

            int centerX = ScreenWidth - MenuRightMargin - (int)(scaledSize.X / 2f);
            int centerY = TitleY + 190 + index * MenuButtonSpacing;

            var rect = new Rectangle(
                (int)(centerX - scaledSize.X / 2f - 30),
                (int)(centerY - scaledSize.Y / 2f - 12),
                (int)scaledSize.X + 60,
                (int)scaledSize.Y + 24);

            return new MenuButtonLayout { Rect = rect, Center = new Vector2(centerX, centerY), TextSize = rawSize };
        }

        public void Update(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            AudioManager.PlayBgm("Audio/menu_theme");

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
                _exitGame?.Invoke();

            _hoveredMenuIndex = -1;
            for (int i = 0; i < MenuLabels.Length; i++)
            {
                if (!MenuEnabled[i]) continue;
                var layout = GetMenuButtonLayout(i);
                if (layout.Rect.Contains(mouse.Position))
                    _hoveredMenuIndex = i;
            }

            if (leftClicked && _hoveredMenuIndex >= 0)
            {
                _playUIClick?.Invoke();
                switch (_hoveredMenuIndex)
                {
                    case 0: _goToModeSelect?.Invoke(); break;
                    case 1: _openSettings?.Invoke(); break;
                    case 2: _goToCredits?.Invoke(); break;
                    case 3: _exitGame?.Invoke(); break;
                }
            }
        }

        public void Draw()
        {
            if (_titleTexture != null)
            {
                float scale = TitleImageTargetWidth / _titleTexture.Width;
                float titleCenterX = ScreenWidth - MenuRightMargin - TitleImageTargetWidth / 2f + TitleOffsetX;
                Vector2 origin = new Vector2(_titleTexture.Width / 2f, _titleTexture.Height / 2f);
                _spriteBatch.Draw(_titleTexture, new Vector2(titleCenterX, TitleY), null, Color.White, 0f, origin, scale, SpriteEffects.None, 0f);
            }
            else if (_font != null)
            {
                Vector2 titleSize = _font.MeasureString(GameTitle) * TitleScale;
                float titleCenterX = ScreenWidth - MenuRightMargin - titleSize.X / 2f;
                DrawTextCentered(GameTitle, new Vector2(titleCenterX, TitleY), Color.Gold, TitleScale);
            }

            for (int i = 0; i < MenuLabels.Length; i++)
            {
                var layout = GetMenuButtonLayout(i);
                bool hovered = _hoveredMenuIndex == i;
                bool enabled = MenuEnabled[i];
                Color color = !enabled ? Color.DimGray : hovered ? Color.Gold : Color.White;

                DrawTextCentered(MenuLabels[i], layout.Center, color, MenuButtonScale);

                if (hovered && enabled)
                {
                    var arrowRect = new Rectangle(layout.Rect.X - 45, (int)(layout.Center.Y - 15), 25, 30);
                    DrawArrow(arrowRect, false, Color.Gold, 4);
                }
            }
        }

        private void DrawArrow(Rectangle rect, bool isLeft, Color color, int thickness)
        {
            Vector2 top = isLeft ? new Vector2(rect.Right, rect.Top) : new Vector2(rect.Left, rect.Top);
            Vector2 mid = isLeft ? new Vector2(rect.Left, rect.Center.Y) : new Vector2(rect.Right, rect.Center.Y);
            Vector2 bot = isLeft ? new Vector2(rect.Right, rect.Bottom) : new Vector2(rect.Left, rect.Bottom);

            DrawLine(top, mid, color, thickness);
            DrawLine(mid, bot, color, thickness);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float length = edge.Length();
            if (length < 0.01f) return;
            _spriteBatch.Draw(_pixel, new Rectangle((int)start.X, (int)start.Y, (int)length, thickness), null, color, (float)Math.Atan2(edge.Y, edge.X), Vector2.Zero, SpriteEffects.None, 0);
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