using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Where_My_Slime
{
    // หน้าแผนที่ระหว่างด่าน (Challenge Mode): เลือกไปด่านธรรมดา/ด่านยาก หรือเข้าร้านค้า (ถ้ามี)
    public class MapScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GameSession _session;

        private readonly Action<bool> _chooseStage; // isHard
        private readonly Action<int> _chooseShop;    // currentScrollWheelValue
        private readonly Action _chooseBoss;
        private readonly Action _playUIClick;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public MapScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            GameSession session,
            Action<bool> chooseStage,
            Action<int> chooseShop,
            Action chooseBoss,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _session = session;
            _chooseStage = chooseStage;
            _chooseShop = chooseShop;
            _chooseBoss = chooseBoss;
            _playUIClick = playUIClick;
        }

        // ทุกๆ 5 ชั้น (5, 10, 15, ...) จะมีให้เลือกแค่เส้นทางเดียวคือ Boss
        private bool IsNextFloorBoss()
        {
            int nextFloorNumber = _session.StoryStageIndex + 2;
            return nextFloorNumber % 5 == 0;
        }

        private Rectangle GetMapButtonRect(int i, int totalButtons)
        {
            int width = 450;
            int height = 280;
            int spacing = 60;
            int totalWidth = totalButtons * width + (totalButtons - 1) * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            return new Rectangle(startX + i * (width + spacing), (ScreenHeight - height) / 2 + 20, width, height);
        }

        public void Update(MouseState mouse, bool leftClicked)
        {
            if (IsNextFloorBoss())
            {
                var bossRect = GetMapButtonRect(0, 1);
                bool hoveredBoss = bossRect.Contains(mouse.Position);
                if (leftClicked && hoveredBoss)
                {
                    _playUIClick?.Invoke();
                    _chooseBoss?.Invoke();
                }
                return;
            }

            int totalButtons = _session.IsShopOnMap ? 3 : 2;
            int hoveredMapChoice = -1;

            for (int i = 0; i < totalButtons; i++)
            {
                if (GetMapButtonRect(i, totalButtons).Contains(mouse.Position)) hoveredMapChoice = i;
            }

            if (leftClicked && hoveredMapChoice >= 0)
            {
                _playUIClick?.Invoke();
                if (hoveredMapChoice == 0 || hoveredMapChoice == 1)
                {
                    _chooseStage?.Invoke(hoveredMapChoice == 1);
                }
                else if (hoveredMapChoice == 2 && _session.IsShopOnMap)
                {
                    _chooseShop?.Invoke(mouse.ScrollWheelValue);
                }
            }
        }

        public void Draw()
        {
            DrawTextCentered("Choose Your Path", new Vector2(ScreenWidth / 2f, 120), Color.Gold, 2.8f);

            if (IsNextFloorBoss())
            {
                DrawTextCentered("A powerful Boss blocks the way - only one path forward", new Vector2(ScreenWidth / 2f, 170), Color.LightGray, 1.1f);

                var bossRect = GetMapButtonRect(0, 1);
                bool hoveredBoss = bossRect.Contains(Mouse.GetState().Position);

                Color bossBg = hoveredBoss ? new Color(80, 20, 20) : new Color(45, 12, 12);
                DrawRect(bossRect, bossBg);
                DrawRectBorder(bossRect, hoveredBoss ? Color.Gold : new Color(220, 60, 60), hoveredBoss ? 5 : 3);

                DrawTextCentered("BOSS", new Vector2(bossRect.Center.X, bossRect.Y + 45), new Color(255, 80, 80), 2.2f);
                DrawLine(new Vector2(bossRect.X + 40, bossRect.Y + 85), new Vector2(bossRect.Right - 40, bossRect.Y + 85), Color.Gray, 1);

                DrawTextCentered("A single, much stronger enemy", new Vector2(bossRect.Center.X, bossRect.Y + 115), Color.LightGray, 1f);
                DrawTextCentered("Reward: +10 Points", new Vector2(bossRect.Center.X, bossRect.Center.Y + 35), Color.LimeGreen, 1.1f);
                DrawTextCentered("Reward Coins: 125 G", new Vector2(bossRect.Center.X, bossRect.Center.Y + 75), Color.Gold, 1.1f);

                if (hoveredBoss) DrawTextCentered("- Click to Select -", new Vector2(bossRect.Center.X, bossRect.Bottom - 35), Color.Gold, 0.9f);
                return;
            }

            DrawTextCentered("Select your next battle stage", new Vector2(ScreenWidth / 2f, 170), Color.LightGray, 1.1f);

            int totalButtons = _session.IsShopOnMap ? 3 : 2;

            for (int i = 0; i < totalButtons; i++)
            {
                var rect = GetMapButtonRect(i, totalButtons);
                bool hovered = rect.Contains(Mouse.GetState().Position);

                if (i == 0 || i == 1)
                {
                    bool isHard = (i == 1);
                    Color cardBg = hovered ? (isHard ? new Color(75, 35, 45) : new Color(50, 70, 110))
                                           : (isHard ? new Color(40, 20, 25) : new Color(30, 30, 45));

                    DrawRect(rect, cardBg);
                    DrawRectBorder(rect, hovered ? Color.Gold : (isHard ? new Color(200, 50, 50) : Color.White), hovered ? 4 : 2);

                    string stageTitle = isHard ? "Hard Stage" : "Normal Stage";
                    Color titleColor = isHard ? new Color(255, 90, 90) : Color.Gold;
                    DrawTextCentered(stageTitle, new Vector2(rect.Center.X, rect.Y + 45), titleColor, 1.8f);
                    DrawLine(new Vector2(rect.X + 40, rect.Y + 85), new Vector2(rect.Right - 40, rect.Y + 85), Color.Gray, 1);

                    string difficultyDesc = isHard ? "Enemy HP / ATK +150%" : "Standard enemy difficulty";
                    DrawTextCentered(difficultyDesc, new Vector2(rect.Center.X, rect.Y + 115), Color.LightGray, 1f);

                    string rewardPoints = isHard ? "Reward: +5 Points" : "Reward: +3 Points";
                    string rewardCoins = isHard ? "Reward Coins: 20 G" : "Reward Coins: 10 G";

                    DrawTextCentered(rewardPoints, new Vector2(rect.Center.X, rect.Center.Y + 35), Color.LimeGreen, 1.1f);
                    DrawTextCentered(rewardCoins, new Vector2(rect.Center.X, rect.Center.Y + 75), Color.Gold, 1.1f);
                }
                else if (i == 2 && _session.IsShopOnMap)
                {
                    Color shopBg = hovered ? new Color(60, 50, 90) : new Color(40, 30, 60);
                    DrawRect(rect, shopBg);
                    DrawRectBorder(rect, hovered ? Color.Gold : Color.MediumPurple, hovered ? 4 : 2);

                    DrawTextCentered("Merchant Shop", new Vector2(rect.Center.X, rect.Y + 45), Color.MediumPurple, 1.8f);
                    DrawLine(new Vector2(rect.X + 40, rect.Y + 85), new Vector2(rect.Right - 40, rect.Y + 85), Color.Gray, 1);

                    DrawTextCentered("Buy powerful items", new Vector2(rect.Center.X, rect.Y + 115), Color.LightGray, 1f);
                    DrawTextCentered("Need: Coins", new Vector2(rect.Center.X, rect.Center.Y + 55), Color.Gold, 1.1f);
                }

                if (hovered) DrawTextCentered("- Click to Select -", new Vector2(rect.Center.X, rect.Bottom - 35), Color.Gold, 0.9f);
            }
        }

        private void DrawRect(Rectangle rect, Color color) => _spriteBatch.Draw(_pixel, rect, color);

        private void DrawRectBorder(Rectangle rect, Color color, int thickness)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
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