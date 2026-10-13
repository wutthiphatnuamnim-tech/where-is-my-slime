using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Where_My_Slime.Data;

namespace Where_My_Slime
{
    // หน้าเลือกไอเท็มรางวัลฟรี 1 ชิ้นจาก 3 ตัวเลือก
    public class ItemSelectScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GameSession _session;
        private readonly Random _rng;

        private readonly Action _onDone;
        private readonly Action _playUIClick;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public ItemSelectScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            GameSession session,
            Random rng,
            Action onDone,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _session = session;
            _rng = rng;
            _onDone = onDone;
            _playUIClick = playUIClick;
        }

        private Rectangle GetRewardItemRect(int index)
        {
            int width = 350;
            int height = 450;
            int spacing = 60;
            int totalWidth = 3 * width + 2 * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            int startY = (ScreenHeight - height) / 2;
            return new Rectangle(startX + index * (width + spacing), startY, width, height);
        }

        private Rectangle GetSkipButtonRect() => new Rectangle(ScreenWidth - 280, ScreenHeight - 100, 220, 60);

        public void Update(MouseState mouse, bool leftClicked)
        {
            if (_session.RewardItems.Count == 0)
            {
                _session.RewardItems = ItemPool.GetRandomItems(3, _rng);
            }

            for (int i = 0; i < _session.RewardItems.Count; i++)
            {
                var rect = GetRewardItemRect(i);
                if (rect.Contains(mouse.Position) && leftClicked)
                {
                    _playUIClick?.Invoke();
                    _session.Inventory.Add(_session.RewardItems[i]);
                    _session.RewardItems.Clear();
                    _onDone?.Invoke();
                    return;
                }
            }

            var skipRect = GetSkipButtonRect();
            if (skipRect.Contains(mouse.Position) && leftClicked)
            {
                _playUIClick?.Invoke();
                _session.RewardItems.Clear();
                _onDone?.Invoke();
            }
        }

        public void Draw()
        {
            DrawTextCentered("Victory Reward", new Vector2(ScreenWidth / 2f, 120), Color.Gold, 2.5f);
            DrawTextCentered("Choose 1 Free Item", new Vector2(ScreenWidth / 2f, 180), Color.White, 1.2f);

            for (int i = 0; i < _session.RewardItems.Count; i++)
            {
                var rect = GetRewardItemRect(i);
                bool hovered = rect.Contains(Mouse.GetState().Position);
                var item = _session.RewardItems[i];
                Color rarityColor = GetRarityColor(item.Rarity);

                DrawRect(rect, hovered ? new Color(60, 60, 95) : new Color(30, 30, 45));
                DrawRectBorder(rect, hovered ? Color.Gold : rarityColor, hovered ? 4 : 3);

                DrawTextCentered(item.Name, new Vector2(rect.Center.X, rect.Y + 60), rarityColor, 1.3f);
                DrawTextCentered($"[{item.Rarity}]", new Vector2(rect.Center.X, rect.Y + 110), rarityColor, 0.95f);
                DrawTextCentered(item.Description, new Vector2(rect.Center.X, rect.Y + 185), Color.White, 0.9f);

                DrawTextCentered("FREE", new Vector2(rect.Center.X, rect.Bottom - 40), Color.LimeGreen, 1.5f);
            }

            var skipRect = GetSkipButtonRect();
            bool skipHovered = skipRect.Contains(Mouse.GetState().Position);
            DrawRect(skipRect, skipHovered ? Color.Gold : new Color(150, 50, 50));
            DrawRectBorder(skipRect, Color.White, 3);
            DrawTextCentered("Skip ->", new Vector2(skipRect.Center.X, skipRect.Center.Y), Color.White, 1.2f);
        }

        private Color GetRarityColor(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Common => Color.White,
            ItemRarity.Uncommon => Color.LimeGreen,
            ItemRarity.Rare => Color.DeepSkyBlue,
            ItemRarity.Epic => Color.MediumPurple,
            ItemRarity.Legendary => Color.Gold,
            ItemRarity.Mythic => Color.Red,
            _ => Color.White
        };

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