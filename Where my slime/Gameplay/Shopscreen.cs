using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Where_My_Slime.Data;

namespace Where_My_Slime
{
    // หน้าร้านพ่อค้าเร่: ซื้อไอเท็มด้วยเหรียญที่สะสมมา
    public class ShopScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GameSession _session;

        private readonly Action _onDone;
        private readonly Action _playUIClick;

        private int _shopScrollY = 0;
        private int _previousScrollWheelValue;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public ShopScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            GameSession session,
            Action onDone,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _session = session;
            _onDone = onDone;
            _playUIClick = playUIClick;
        }

        // เรียกตอนเข้าหน้า Shop ใหม่จาก Map เพื่อสุ่มสินค้าและรีเซ็ตสกอร์ล
        public void EnterFresh(Random rng, int currentScrollWheelValue)
        {
            _session.ShopItems = ItemPool.GetRandomItems(9, rng);
            _shopScrollY = 0;
            _previousScrollWheelValue = currentScrollWheelValue;
        }

        private Rectangle GetShopItemRect(int index, int scrollY)
        {
            int col = index % 3;
            int row = index / 3;
            int width = 380;
            int height = 450;
            int spacingX = 50;
            int spacingY = 50;

            int totalWidth = 3 * width + 2 * spacingX;
            int startX = (ScreenWidth - totalWidth) / 2;
            int startY = 160;

            return new Rectangle(startX + col * (width + spacingX), startY + row * (height + spacingY) - scrollY, width, height);
        }

        private Rectangle GetShopNextButtonRect() => new Rectangle(ScreenWidth - 280, ScreenHeight - 100, 220, 60);

        public void Update(MouseState mouse, bool leftClicked)
        {
            int currentScrollWheel = mouse.ScrollWheelValue;
            if (currentScrollWheel > _previousScrollWheelValue) _shopScrollY -= 50;
            else if (currentScrollWheel < _previousScrollWheelValue) _shopScrollY += 50;
            _previousScrollWheelValue = currentScrollWheel;

            int maxScroll = Math.Max(0, (int)(Math.Ceiling(_session.ShopItems.Count / 3.0) * 500) - 800);
            if (_shopScrollY < 0) _shopScrollY = 0;
            if (_shopScrollY > maxScroll) _shopScrollY = maxScroll;

            for (int i = 0; i < _session.ShopItems.Count; i++)
            {
                var rect = GetShopItemRect(i, _shopScrollY);
                if (rect.Bottom > 0 && rect.Top < ScreenHeight && rect.Contains(mouse.Position) && leftClicked)
                {
                    if (_session.Coins >= _session.ShopItems[i].Price)
                    {
                        _playUIClick?.Invoke();
                        _session.Coins -= _session.ShopItems[i].Price;
                        _session.Inventory.Add(_session.ShopItems[i]);
                        _session.ShopItems.RemoveAt(i);
                        break;
                    }
                }
            }

            var nextRect = GetShopNextButtonRect();
            if (nextRect.Contains(mouse.Position) && leftClicked)
            {
                _playUIClick?.Invoke();
                _onDone?.Invoke();
            }
        }

        public void Draw()
        {
            for (int i = 0; i < _session.ShopItems.Count; i++)
            {
                var rect = GetShopItemRect(i, _shopScrollY);
                if (rect.Bottom < 0 || rect.Top > ScreenHeight) continue;

                var item = _session.ShopItems[i];
                bool hovered = rect.Contains(Mouse.GetState().Position);
                bool affordable = _session.Coins >= item.Price;
                Color rarityColor = GetRarityColor(item.Rarity);

                DrawRect(rect, hovered && affordable ? new Color(60, 60, 95) : new Color(30, 30, 45));
                DrawRectBorder(rect, !affordable ? Color.DarkGray : (hovered ? Color.Gold : rarityColor), hovered && affordable ? 4 : 3);

                DrawTextCentered(item.Name, new Vector2(rect.Center.X, rect.Y + 60), !affordable ? Color.Gray : rarityColor, 1.3f);
                DrawTextCentered($"[{item.Rarity}]", new Vector2(rect.Center.X, rect.Y + 110), rarityColor, 0.95f);
                DrawTextCentered(item.Description, new Vector2(rect.Center.X, rect.Y + 185), Color.White, 0.9f);
                DrawTextCentered($"Price: {item.Price} G", new Vector2(rect.Center.X, rect.Bottom - 40), affordable ? Color.LimeGreen : Color.Red, 1.2f);
            }

            DrawRect(new Rectangle(0, 0, ScreenWidth, 140), new Color(30, 30, 45));
            DrawTextCentered("Wandering Merchant", new Vector2(ScreenWidth / 2f, 60), Color.Gold, 2.5f);
            DrawTextCentered($"Coins: {_session.Coins}", new Vector2(ScreenWidth / 2f, 110), Color.Gold, 1.3f);

            var nextRect = GetShopNextButtonRect();
            bool nextHovered = nextRect.Contains(Mouse.GetState().Position);
            DrawRect(nextRect, nextHovered ? Color.Gold : new Color(50, 120, 50));
            DrawRectBorder(nextRect, Color.White, 3);
            DrawTextCentered("Next ->", new Vector2(nextRect.Center.X, nextRect.Center.Y), Color.White, 1.2f);
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