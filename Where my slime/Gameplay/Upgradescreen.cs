using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Where_My_Slime.Models;

namespace Where_My_Slime
{
    // หน้าอัพเกรดตัวละคร: แจกแต้มสเตตัส (STR/INT/VIT/DEX/LUX/CRT) หรือใส่/ถอดไอเทม
    public class UpgradeScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GameSession _session;

        private readonly Texture2D _elmaIdleTex;
        private readonly Texture2D _lumiIdleTex;
        private readonly Texture2D _aliceIdleTex;
        private readonly Texture2D _testCharacterTex;

        private readonly Action _onDone;
        private readonly Action _playUIClick;

        private int _upgradeSelectedIndex = -1;
        private int _upgradeSelectedTab = 0;
        private int _inventoryScrollY = 0;
        private int _previousScrollWheelValue;

        private bool _hoveredStatTab;
        private bool _hoveredEquipTab;
        private int _hoveredHeroTrayIndex = -1;
        private int _hoveredStatPlusRow = -1;
        private int _hoveredStatMinusRow = -1;
        private bool _hoveredConfirmButton;
        private bool _hoveredContinue;
        private int _hoveredRestItemRow = -1;
        private bool _hoveredRestUnequip;

        private static readonly GrowthStat[] GrowthStats =
            { GrowthStat.STR, GrowthStat.INT, GrowthStat.VIT, GrowthStat.DEX, GrowthStat.LUX, GrowthStat.CRT };
        private int[] _pendingAllocations = new int[GrowthStats.Length];

        private bool _isInputActive = false;
        private string _inputBuffer = "1";

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public UpgradeScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            GameSession session,
            Texture2D elmaIdleTex,
            Texture2D lumiIdleTex,
            Texture2D aliceIdleTex,
            Texture2D testCharacterTex,
            Action onDone,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _session = session;
            _elmaIdleTex = elmaIdleTex;
            _lumiIdleTex = lumiIdleTex;
            _aliceIdleTex = aliceIdleTex;
            _testCharacterTex = testCharacterTex;
            _onDone = onDone;
            _playUIClick = playUIClick;
        }

        // เรียกก่อนเข้าหน้า Upgrade ใหม่ทุกครั้ง เพื่อล้างการเลือกตัวละคร/แท็บ/แต้มที่กำลังจัดสรรค้างอยู่
        public void EnterFresh()
        {
            _upgradeSelectedIndex = -1;
            _upgradeSelectedTab = 0;
            ResetPendingAllocations();
        }

        private Rectangle GetUpgradeStatTabRect() => new Rectangle(120, 320, 70, 70);
        private Rectangle GetUpgradeEquipTabRect() => new Rectangle(120, 410, 70, 70);
        private Rectangle GetUpgradeCharacterCardRect() => new Rectangle(230, 220, 280, 520);
        private Rectangle GetUpgradeInfoPanelRect() => new Rectangle(530, 220, 380, 520);
        private Rectangle GetUpgradeRightPanelRect() => new Rectangle(930, 220, 520, 520);

        private Rectangle GetStatRowMinusButtonRect(int rowIndex) => new Rectangle(GetUpgradeRightPanelRect().Right - 150, GetUpgradeRightPanelRect().Y + 90 + rowIndex * 60, 45, 42);
        private Rectangle GetStatRowPlusButtonRect(int rowIndex) => new Rectangle(GetUpgradeRightPanelRect().Right - 80, GetUpgradeRightPanelRect().Y + 90 + rowIndex * 60, 45, 42);
        private Rectangle GetUpgradeConfirmButtonRect() => new Rectangle(GetUpgradeRightPanelRect().X + 30, GetUpgradeRightPanelRect().Bottom - 80, GetUpgradeRightPanelRect().Width - 60, 50);

        private Rectangle GetUpgradePtsBoxRect()
        {
            var rightRect = GetUpgradeRightPanelRect();
            return new Rectangle(rightRect.Right - 130, rightRect.Y + 12, 105, 36);
        }

        private Rectangle GetEquipItemSlotRect() => new Rectangle(GetUpgradeRightPanelRect().Center.X - 50, GetUpgradeRightPanelRect().Y + 80, 100, 100);

        private Rectangle GetEquipInventoryItemRect(int rowIndex, int scrollY) =>
            new Rectangle(GetUpgradeRightPanelRect().X + 30, GetUpgradeRightPanelRect().Y + 280 + rowIndex * 90 - scrollY, GetUpgradeRightPanelRect().Width - 60, 75);

        private Rectangle GetEquipUnequipButtonRect() => new Rectangle(GetUpgradeRightPanelRect().Center.X - 100, GetUpgradeRightPanelRect().Y + 195, 200, 38);

        private Rectangle GetUpgradeHeroTrayRect(int i)
        {
            const int w = 110;
            const int h = 130;
            const int spacing = 20;
            int totalWidth = _session.Party.Count * w + (_session.Party.Count - 1) * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            int y = ScreenHeight - 175;
            return new Rectangle(startX + i * (w + spacing), y, w, h);
        }

        private Rectangle GetContinueButtonRect() => new Rectangle(ScreenWidth - 280, ScreenHeight - 100, 220, 64);

        public void Update(MouseState mouse, bool leftClicked, KeyboardState keyboard, KeyboardState previousKeyboard)
        {
            _hoveredStatTab = GetUpgradeStatTabRect().Contains(mouse.Position);
            _hoveredEquipTab = GetUpgradeEquipTabRect().Contains(mouse.Position);

            if (leftClicked)
            {
                var ptsBoxRect = GetUpgradePtsBoxRect();
                if (ptsBoxRect.Contains(mouse.Position))
                {
                    _isInputActive = true;
                }
                else
                {
                    _isInputActive = false;
                }
            }

            if (_isInputActive && _upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count)
            {
                var selected = _session.Party[_upgradeSelectedIndex];
                int pendingUsed = SumPendingAllocations();
                int maxAvailable = selected.UpgradePoints - pendingUsed;

                Keys[] pressedKeys = keyboard.GetPressedKeys();
                foreach (var key in pressedKeys)
                {
                    if (!previousKeyboard.IsKeyDown(key))
                    {
                        if (key >= Keys.D0 && key <= Keys.D9)
                        {
                            char digit = (char)('0' + (key - Keys.D0));
                            if (_inputBuffer == "0") _inputBuffer = digit.ToString();
                            else _inputBuffer += digit;
                        }
                        else if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
                        {
                            char digit = (char)('0' + (key - Keys.NumPad0));
                            if (_inputBuffer == "0") _inputBuffer = digit.ToString();
                            else _inputBuffer += digit;
                        }
                        else if (key == Keys.Back && _inputBuffer.Length > 0)
                        {
                            _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                            if (_inputBuffer == "") _inputBuffer = "0";
                        }
                    }
                }

                if (int.TryParse(_inputBuffer, out int val))
                {
                    if (val > maxAvailable) _inputBuffer = maxAvailable.ToString();
                }
                else
                {
                    _inputBuffer = "0";
                }
            }

            if (leftClicked && GetUpgradeCharacterCardRect().Contains(mouse.Position))
            {
                if (_upgradeSelectedIndex >= 0)
                {
                    _playUIClick?.Invoke();
                    _upgradeSelectedIndex = -1;
                    _upgradeSelectedTab = 0;
                    ResetPendingAllocations();
                }
            }

            if (leftClicked && _upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count)
            {
                if (_hoveredStatTab)
                {
                    _playUIClick?.Invoke();
                    if (_upgradeSelectedTab == 0) _upgradeSelectedTab = -1;
                    else { _upgradeSelectedTab = 0; ResetPendingAllocations(); }
                }
                else if (_hoveredEquipTab)
                {
                    _playUIClick?.Invoke();
                    if (_upgradeSelectedTab == 1) _upgradeSelectedTab = -1;
                    else { _upgradeSelectedTab = 1; ResetPendingAllocations(); }
                }
            }

            _hoveredHeroTrayIndex = -1;
            for (int i = 0; i < _session.Party.Count; i++)
            {
                if (GetUpgradeHeroTrayRect(i).Contains(mouse.Position)) _hoveredHeroTrayIndex = i;
            }

            if (leftClicked && _hoveredHeroTrayIndex >= 0 && _hoveredHeroTrayIndex != _upgradeSelectedIndex)
            {
                _playUIClick?.Invoke();
                _upgradeSelectedIndex = _hoveredHeroTrayIndex;
                if (_upgradeSelectedTab < 0) _upgradeSelectedTab = 0;
                ResetPendingAllocations();
            }

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count && _upgradeSelectedTab >= 0)
            {
                var selected = _session.Party[_upgradeSelectedIndex];

                if (_upgradeSelectedTab == 0)
                {
                    int pendingUsed = SumPendingAllocations();
                    int remaining = selected.UpgradePoints - pendingUsed;

                    _hoveredStatPlusRow = -1;
                    _hoveredStatMinusRow = -1;
                    _hoveredConfirmButton = false;

                    for (int row = 0; row < GrowthStats.Length; row++)
                    {
                        if (GetStatRowPlusButtonRect(row).Contains(mouse.Position)) _hoveredStatPlusRow = row;
                        if (GetStatRowMinusButtonRect(row).Contains(mouse.Position)) _hoveredStatMinusRow = row;
                    }
                    _hoveredConfirmButton = pendingUsed > 0 && GetUpgradeConfirmButtonRect().Contains(mouse.Position);

                    if (leftClicked)
                    {
                        if (_hoveredStatPlusRow >= 0 && remaining > 0)
                        {
                            _playUIClick?.Invoke();
                            int addAmount = 1;
                            if (int.TryParse(_inputBuffer, out int parsedInput) && parsedInput > 0)
                            {
                                addAmount = Math.Min(parsedInput, remaining);
                            }
                            _pendingAllocations[_hoveredStatPlusRow] += addAmount;
                        }
                        else if (_hoveredStatMinusRow >= 0 && _pendingAllocations[_hoveredStatMinusRow] > 0)
                        {
                            _playUIClick?.Invoke();
                            int subAmount = 1;
                            if (int.TryParse(_inputBuffer, out int parsedInput) && parsedInput > 0)
                            {
                                subAmount = Math.Min(parsedInput, _pendingAllocations[_hoveredStatMinusRow]);
                            }
                            _pendingAllocations[_hoveredStatMinusRow] -= subAmount;
                        }
                        else if (_hoveredConfirmButton)
                        {
                            _playUIClick?.Invoke();
                            for (int row = 0; row < GrowthStats.Length; row++)
                            {
                                for (int n = 0; n < _pendingAllocations[row]; n++) selected.ApplyGrowthPoint(GrowthStats[row]);
                            }
                            selected.UpgradePoints -= pendingUsed;
                            ResetPendingAllocations();
                        }
                    }
                }
                else if (_upgradeSelectedTab == 1)
                {
                    var rightRect = GetUpgradeRightPanelRect();

                    // สโครลด้วยล้อเมาส์เฉพาะตอนที่เมาส์อยู่เหนือกรอบ Equipment เท่านั้น กันไม่ให้
                    // สโครลไอเทมไปโดยไม่ตั้งใจตอนเลื่อนเมาส์ผ่านส่วนอื่นของหน้าจอ
                    int currentScrollWheel = mouse.ScrollWheelValue;
                    if (rightRect.Contains(mouse.Position))
                    {
                        if (currentScrollWheel > _previousScrollWheelValue) _inventoryScrollY -= 50;
                        else if (currentScrollWheel < _previousScrollWheelValue) _inventoryScrollY += 50;
                    }
                    _previousScrollWheelValue = currentScrollWheel;

                    int visibleHeight = GetUpgradeRightPanelRect().Height - 280;
                    int totalHeight = _session.Inventory.Count * 90;
                    int maxScroll = Math.Max(0, totalHeight - visibleHeight + 20);
                    if (_inventoryScrollY < 0) _inventoryScrollY = 0;
                    if (_inventoryScrollY > maxScroll) _inventoryScrollY = maxScroll;

                    _hoveredRestItemRow = -1;
                    _hoveredRestUnequip = false;

                    int clipTop = rightRect.Y + 270;
                    int clipBottom = rightRect.Bottom - 10;

                    for (int row = 0; row < _session.Inventory.Count; row++)
                    {
                        var itemRow = GetEquipInventoryItemRect(row, _inventoryScrollY);

                        if (itemRow.Top >= clipTop && itemRow.Bottom <= clipBottom)
                        {
                            if (itemRow.Contains(mouse.Position)) _hoveredRestItemRow = row;
                        }
                    }

                    _hoveredRestUnequip = selected.EquippedItem != null && GetEquipUnequipButtonRect().Contains(mouse.Position);

                    if (leftClicked)
                    {
                        if (_hoveredRestItemRow >= 0)
                        {
                            _playUIClick?.Invoke();
                            var newItem = _session.Inventory[_hoveredRestItemRow];
                            var previousItem = selected.EquippedItem;
                            selected.EquipItem(newItem);
                            _session.Inventory.RemoveAt(_hoveredRestItemRow);
                            if (previousItem != null) _session.Inventory.Add(previousItem);
                        }
                        else if (_hoveredRestUnequip)
                        {
                            _playUIClick?.Invoke();
                            var removedItem = selected.EquippedItem;
                            selected.UnequipItem();
                            if (removedItem != null) _session.Inventory.Add(removedItem);
                        }
                    }
                }
            }

            _hoveredContinue = GetContinueButtonRect().Contains(mouse.Position);
            if (leftClicked && _hoveredContinue)
            {
                _playUIClick?.Invoke();
                _upgradeSelectedIndex = -1;
                _upgradeSelectedTab = 0;
                ResetPendingAllocations();
                _onDone?.Invoke();
            }
        }

        private int SumPendingAllocations()
        {
            int sum = 0;
            for (int i = 0; i < _pendingAllocations.Length; i++) sum += _pendingAllocations[i];
            return sum;
        }

        private void ResetPendingAllocations()
        {
            for (int i = 0; i < _pendingAllocations.Length; i++) _pendingAllocations[i] = 0;
            _inventoryScrollY = 0;
            _inputBuffer = "1";
            _isInputActive = false;
        }

        private static int GetGrowthValue(Character c, GrowthStat stat)
        {
            switch (stat)
            {
                case GrowthStat.STR: return c.STR;
                case GrowthStat.INT: return c.INT;
                case GrowthStat.VIT: return c.VIT;
                case GrowthStat.DEX: return c.DEX;
                case GrowthStat.LUX: return c.LUX;
                case GrowthStat.CRT: return c.CRT;
                default: return 0;
            }
        }

        public void Draw()
        {
            DrawTextCentered("Upgrade", new Vector2(ScreenWidth / 2f, 80), Color.Gold, 2.6f);

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count)
            {
                var selectedCharForBadges = _session.Party[_upgradeSelectedIndex];

                // วงกลมแดงแจ้งเตือน: ไอคอน STAT เตือนเมื่อตัวละครยังมีแต้มเหลือให้อัพ,
                // ไอคอน Equip เตือนเมื่อมีไอเทมในคลังให้ใส่ และตัวละครยังไม่ได้ใส่ไอเทมอยู่
                bool hasPointsToSpend = (selectedCharForBadges.UpgradePoints - SumPendingAllocations()) > 0;
                bool hasItemToEquip = _session.Inventory.Count > 0 && selectedCharForBadges.EquippedItem == null;

                var statTabRect = GetUpgradeStatTabRect();
                DrawRect(statTabRect, _upgradeSelectedTab == 0 ? Color.Gold : (_hoveredStatTab ? new Color(70, 70, 100) : new Color(40, 40, 60)));
                DrawRectBorder(statTabRect, Color.White, 2);

                DrawRect(new Rectangle(statTabRect.X + 13, statTabRect.Y + 38, 10, 18), Color.Red);
                DrawRectBorder(new Rectangle(statTabRect.X + 13, statTabRect.Y + 38, 10, 18), Color.Black, 1);

                DrawRect(new Rectangle(statTabRect.X + 27, statTabRect.Y + 26, 10, 30), Color.LimeGreen);
                DrawRectBorder(new Rectangle(statTabRect.X + 27, statTabRect.Y + 26, 10, 30), Color.Black, 1);

                DrawRect(new Rectangle(statTabRect.X + 41, statTabRect.Y + 14, 10, 42), Color.DeepSkyBlue);
                DrawRectBorder(new Rectangle(statTabRect.X + 41, statTabRect.Y + 14, 10, 42), Color.Black, 1);

                if (hasPointsToSpend) DrawNotificationDot(new Vector2(statTabRect.Right - 4, statTabRect.Top - 4), 10f);

                var equipTabRect = GetUpgradeEquipTabRect();
                DrawRect(equipTabRect, _upgradeSelectedTab == 1 ? Color.Gold : (_hoveredEquipTab ? new Color(70, 70, 100) : new Color(40, 40, 60)));
                DrawRectBorder(equipTabRect, Color.White, 2);

                DrawRect(new Rectangle(equipTabRect.X + 31, equipTabRect.Y + 12, 8, 28), Color.White);
                DrawRectBorder(new Rectangle(equipTabRect.X + 31, equipTabRect.Y + 12, 8, 28), Color.Black, 1);
                DrawRect(new Rectangle(equipTabRect.X + 18, equipTabRect.Y + 36, 34, 8), Color.Black);
                DrawRectBorder(new Rectangle(equipTabRect.X + 18, equipTabRect.Y + 36, 34, 8), Color.White, 1);
                DrawRect(new Rectangle(equipTabRect.X + 31, equipTabRect.Y + 44, 8, 14), Color.Black);
                DrawRectBorder(new Rectangle(equipTabRect.X + 31, equipTabRect.Y + 44, 8, 14), Color.White, 1);

                if (hasItemToEquip) DrawNotificationDot(new Vector2(equipTabRect.Right - 4, equipTabRect.Top - 4), 10f);
            }

            var cardRect = GetUpgradeCharacterCardRect();
            DrawRect(cardRect, new Color(25, 25, 35));
            DrawRectBorder(cardRect, Color.White, 3);

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count)
            {
                var selectedChar = _session.Party[_upgradeSelectedIndex];
                DrawTextCentered(selectedChar.Name, new Vector2(cardRect.Center.X, cardRect.Bottom + 35), Color.Gold, 1.3f);

                if (selectedChar.Name == "Elma" && _elmaIdleTex != null)
                {
                    int frameWidth = 160;
                    int frameHeight = 320;
                    Rectangle sourceRect = new Rectangle(0, 0, frameWidth, frameHeight);
                    float scale = Math.Min((cardRect.Width - 40) / (float)frameWidth, (cardRect.Height - 40) / (float)frameHeight);
                    float drawW = frameWidth * scale;
                    float drawH = frameHeight * scale;
                    Vector2 drawPos = new Vector2(cardRect.Center.X - drawW / 2f, cardRect.Center.Y - drawH / 2f);
                    _spriteBatch.Draw(_elmaIdleTex, drawPos, sourceRect, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                else if (selectedChar.Name == "Lumi" && _lumiIdleTex != null)
                {
                    int frameWidth = 240;
                    int frameHeight = 320;
                    Rectangle sourceRect = new Rectangle(0, 0, frameWidth, frameHeight);

                    float scale = Math.Min((cardRect.Width - 40) / (float)frameWidth, (cardRect.Height - 40) / (float)frameHeight);
                    float drawW = frameWidth * scale;
                    float drawH = frameHeight * scale;
                    Vector2 drawPos = new Vector2(cardRect.Center.X - drawW / 2f, cardRect.Center.Y - drawH / 2f);
                    _spriteBatch.Draw(_lumiIdleTex, drawPos, sourceRect, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                else if ((selectedChar.Name == "Alice" || selectedChar.Name == "Swordman") && _aliceIdleTex != null)
                {
                    int frameWidth = 480;
                    int frameHeight = 480;
                    Rectangle sourceRect = new Rectangle(0, 0, frameWidth, frameHeight);

                    float scale = Math.Min((cardRect.Width - 40) / (float)frameWidth, (cardRect.Height - 40) / (float)frameHeight) * 2.4f;

                    float drawW = frameWidth * scale;
                    float drawH = frameHeight * scale;

                    Vector2 drawPos = new Vector2(cardRect.Center.X - drawW / 2f, (cardRect.Center.Y - drawH / 2f) - 30f);

                    _spriteBatch.Draw(_aliceIdleTex, drawPos, sourceRect, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
                else if (_testCharacterTex != null)
                {
                    float scale = Math.Min((cardRect.Width - 40) / (float)_testCharacterTex.Width, (cardRect.Height - 40) / (float)_testCharacterTex.Height);
                    float drawW = _testCharacterTex.Width * scale;
                    float drawH = _testCharacterTex.Height * scale;
                    Vector2 drawPos = new Vector2(cardRect.Center.X - drawW / 2f, cardRect.Center.Y - drawH / 2f);
                    _spriteBatch.Draw(_testCharacterTex, drawPos, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }
            else
            {
                DrawTextCentered("+", new Vector2(cardRect.Center.X, cardRect.Center.Y), Color.LightGray, 3f);
            }

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count)
            {
                var selectedChar = _session.Party[_upgradeSelectedIndex];
                var infoRect = GetUpgradeInfoPanelRect();
                DrawRect(infoRect, new Color(20, 20, 30, 240));
                DrawRectBorder(infoRect, Color.Gold, 2);

                DrawText("INFO", new Vector2(infoRect.X + 15, infoRect.Y + 12), Color.Gold, 0.9f);
                DrawText($"{selectedChar.Name}", new Vector2(infoRect.X + 25, infoRect.Y + 45), Color.White, 1.2f);

                int pendingStr = _pendingAllocations[0];
                int pendingInt = _pendingAllocations[1];
                int pendingVit = _pendingAllocations[2];
                int pendingDex = _pendingAllocations[3];
                int pendingLux = _pendingAllocations[4];
                int pendingCrt = _pendingAllocations[5];

                int bonusHp = pendingVit * 50;
                int bonusPAtk = pendingStr * 12;
                int bonusPDef = pendingVit * 7;
                int bonusMAtk = pendingInt * 12;
                int bonusMDef = (pendingInt * 9);

                float luxCritRate = pendingLux * 4f;
                float crtCritRate = pendingCrt * 0.25f;
                float totalBonusCritRate = luxCritRate + crtCritRate;
                string critRateBonusStr = totalBonusCritRate > 0 ? $" (+{totalBonusCritRate:0.##}%)" : "";

                int bonusCritDmg = pendingCrt * 5;
                string critDmgBonusStr = bonusCritDmg > 0 ? $" (+{bonusCritDmg}%)" : "";

                int bonusSpeed = pendingDex * 2;
                int bonusAccuracy = pendingDex * 1;

                int statStartY = infoRect.Y + 95;
                int rowGap = 38;
                int col2X = infoRect.X + 210;

                DrawStatWithBonus($"HP: {selectedChar.MaxHp}", bonusHp > 0 ? $" (+{bonusHp})" : "", new Vector2(infoRect.X + 25, statStartY + rowGap * 0), 0.9f);
                DrawStatWithBonus($"P. DEF: {selectedChar.PDef}", bonusPDef > 0 ? $" (+{bonusPDef})" : "", new Vector2(col2X, statStartY + rowGap * 0), 0.9f);

                DrawStatWithBonus($"P. ATK: {selectedChar.PAtk}", bonusPAtk > 0 ? $" (+{bonusPAtk})" : "", new Vector2(infoRect.X + 25, statStartY + rowGap * 1), 0.9f);
                DrawStatWithBonus($"M. ATK: {selectedChar.MAtk}", bonusMAtk > 0 ? $" (+{bonusMAtk})" : "", new Vector2(col2X, statStartY + rowGap * 1), 0.9f);

                DrawStatWithBonus($"M. DEF: {selectedChar.MDef}", bonusMDef > 0 ? $" (+{bonusMDef})" : "", new Vector2(infoRect.X + 25, statStartY + rowGap * 2), 0.9f);
                DrawStatWithBonus($"CRIT RATE: {(int)selectedChar.CritRate}%", critRateBonusStr, new Vector2(col2X, statStartY + rowGap * 2), 0.9f);

                DrawStatWithBonus($"CRIT DMG: {(int)selectedChar.CritDamage}%", critDmgBonusStr, new Vector2(infoRect.X + 25, statStartY + rowGap * 3), 0.9f);
                DrawStatWithBonus($"SPEED: {selectedChar.Speed}", bonusSpeed > 0 ? $" (+{bonusSpeed})" : "", new Vector2(col2X, statStartY + rowGap * 3), 0.9f);

                DrawStatWithBonus($"ACC: {(int)selectedChar.Accuracy}%", bonusAccuracy > 0 ? $" (+{bonusAccuracy}%)" : "", new Vector2(infoRect.X + 25, statStartY + rowGap * 4), 0.9f);

                int ultimateY = statStartY + rowGap * 5 + 20;
                DrawText("Ultimate Skill", new Vector2(infoRect.X + 25, ultimateY), Color.Gold, 0.95f);
                DrawWrappedText(GetUltimateSkillDescription(selectedChar.Name), new Vector2(infoRect.X + 25, ultimateY + 36), infoRect.Width - 50, Color.White, 0.8f);
            }

            if (_upgradeSelectedIndex >= 0 && _upgradeSelectedIndex < _session.Party.Count && _upgradeSelectedTab >= 0)
            {
                var selectedChar = _session.Party[_upgradeSelectedIndex];
                var rightRect = GetUpgradeRightPanelRect();
                DrawRect(rightRect, new Color(20, 20, 30, 240));
                DrawRectBorder(rightRect, Color.Gold, 2);

                if (_upgradeSelectedTab == 0)
                {
                    DrawText("STAT", new Vector2(rightRect.X + 20, rightRect.Y + 15), Color.Gold, 1.1f);

                    int pendingUsed = SumPendingAllocations();
                    int remainingPts = selectedChar.UpgradePoints - pendingUsed;

                    DrawText($"Point : {remainingPts}", new Vector2(rightRect.X + 150, rightRect.Y + 15), Color.LimeGreen, 1.1f);

                    var ptsBox = GetUpgradePtsBoxRect();
                    DrawRect(ptsBox, _isInputActive ? new Color(60, 60, 85) : new Color(40, 40, 55));
                    DrawRectBorder(ptsBox, _isInputActive ? Color.Gold : Color.LimeGreen, _isInputActive ? 3 : 2);
                    DrawTextCentered(_inputBuffer, new Vector2(ptsBox.Center.X, ptsBox.Center.Y), Color.LimeGreen, 1f);

                    for (int row = 0; row < GrowthStats.Length; row++)
                    {
                        var statName = GrowthStats[row].ToString();
                        int baseVal = GetGrowthValue(selectedChar, GrowthStats[row]);
                        int pending = _pendingAllocations[row];

                        var minusRect = GetStatRowMinusButtonRect(row);
                        var plusRect = GetStatRowPlusButtonRect(row);

                        string rowText = pending > 0 ? $"{statName} : {baseVal} (+{pending})" : $"{statName} : {baseVal}";
                        DrawText(rowText, new Vector2(rightRect.X + 30, minusRect.Y + 8), pending > 0 ? Color.Gold : Color.White, 1f);

                        DrawRect(minusRect, pending > 0 ? (_hoveredStatMinusRow == row ? Color.Gold : new Color(150, 50, 50)) : Color.DarkGray);
                        DrawRectBorder(minusRect, Color.White, 2);
                        DrawTextCentered("-", new Vector2(minusRect.Center.X, minusRect.Center.Y), Color.White, 1.2f);

                        DrawRect(plusRect, remainingPts > 0 ? (_hoveredStatPlusRow == row ? Color.Gold : new Color(50, 150, 50)) : Color.DarkGray);
                        DrawRectBorder(plusRect, Color.White, 2);
                        DrawTextCentered("+", new Vector2(plusRect.Center.X, plusRect.Center.Y), Color.White, 1.2f);
                    }

                    var confirmRect = GetUpgradeConfirmButtonRect();
                    DrawRect(confirmRect, pendingUsed > 0 ? (_hoveredConfirmButton ? Color.Gold : new Color(50, 120, 50)) : new Color(60, 60, 60));
                    DrawRectBorder(confirmRect, Color.White, 2);
                    DrawTextCentered(pendingUsed > 0 ? $"Confirm ({pendingUsed} pt)" : "Confirm", new Vector2(confirmRect.Center.X, confirmRect.Center.Y), Color.White, 1.1f);
                }
                else if (_upgradeSelectedTab == 1)
                {
                    DrawText("Equipment", new Vector2(rightRect.X + 20, rightRect.Y + 15), Color.Gold, 1.1f);

                    var equipSlot = GetEquipItemSlotRect();
                    DrawRect(equipSlot, new Color(40, 40, 60));
                    DrawRectBorder(equipSlot, selectedChar.EquippedItem != null ? Color.LimeGreen : Color.White, 2);

                    if (selectedChar.EquippedItem != null)
                    {
                        DrawTextCentered(selectedChar.EquippedItem.Name, new Vector2(equipSlot.Center.X, equipSlot.Center.Y - 10), Color.LimeGreen, 0.75f);
                        DrawTextCentered($"[{selectedChar.EquippedItem.Rarity}]", new Vector2(equipSlot.Center.X, equipSlot.Center.Y + 15), Color.White, 0.7f);
                    }
                    else
                    {
                        DrawTextCentered("+", new Vector2(equipSlot.Center.X, equipSlot.Center.Y), Color.Gray, 2f);
                    }

                    if (selectedChar.EquippedItem != null)
                    {
                        var unequipBtn = GetEquipUnequipButtonRect();
                        DrawRect(unequipBtn, _hoveredRestUnequip ? Color.Gold : new Color(150, 50, 50));
                        DrawRectBorder(unequipBtn, Color.White, 1);
                        DrawTextCentered("Unequip", new Vector2(unequipBtn.Center.X, unequipBtn.Center.Y), Color.White, 0.85f);
                    }

                    DrawText("Inventory Items:", new Vector2(rightRect.X + 30, rightRect.Y + 245), Color.White, 0.9f);
                    if (_session.Inventory.Count == 0)
                    {
                        DrawText("No items in inventory", new Vector2(rightRect.X + 30, rightRect.Y + 280), Color.Gray, 0.9f);
                    }

                    int clipTop = rightRect.Y + 275;
                    int clipBottom = rightRect.Bottom - 10;

                    for (int row = 0; row < _session.Inventory.Count; row++)
                    {
                        var itemRow = GetEquipInventoryItemRect(row, _inventoryScrollY);

                        if (itemRow.Top < clipTop || itemRow.Bottom > clipBottom) continue;

                        bool hoveredRow = _hoveredRestItemRow == row;
                        DrawRect(itemRow, hoveredRow ? new Color(50, 50, 80) : new Color(30, 30, 45));
                        DrawRectBorder(itemRow, hoveredRow ? Color.Gold : Color.White, 2);

                        var invItem = _session.Inventory[row];
                        DrawText(invItem.Name, new Vector2(itemRow.X + 15, itemRow.Y + 12), Color.Gold, 0.9f);
                        DrawText(invItem.Description, new Vector2(itemRow.X + 15, itemRow.Y + 40), Color.White, 0.75f);
                    }
                }
            }

            int trayLineY = ScreenHeight - 200;
            DrawLine(new Vector2(100, trayLineY), new Vector2(ScreenWidth - 100, trayLineY), Color.Gold, 3);
            DrawText("Hero Tray", new Vector2(120, trayLineY - 35), Color.Gold, 1.2f);

            for (int i = 0; i < _session.Party.Count; i++)
            {
                var hero = _session.Party[i];
                var trayRect = GetUpgradeHeroTrayRect(i);
                bool hovered = _hoveredHeroTrayIndex == i;
                bool selected = _upgradeSelectedIndex == i;

                Color heroBg = selected ? new Color(60, 100, 180) : (hovered ? new Color(70, 70, 100) : new Color(30, 30, 45));
                DrawRect(trayRect, heroBg);
                DrawRectBorder(trayRect, selected ? Color.Gold : Color.White, selected ? 3 : 2);

                DrawTextCentered(hero.Name, new Vector2(trayRect.Center.X, trayRect.Y + 30), Color.Gold, 1.1f);
                DrawTextCentered($"Pts: {hero.UpgradePoints}", new Vector2(trayRect.Center.X, trayRect.Y + 70), Color.LightGreen, 0.8f);
            }

            var contRect = GetContinueButtonRect();
            DrawRect(contRect, _hoveredContinue ? Color.Gold : new Color(50, 120, 50));
            DrawRectBorder(contRect, Color.White, 3);
            DrawTextCentered("Done ->", new Vector2(contRect.Center.X, contRect.Center.Y), Color.White, 1.1f);
        }

        private void DrawStatWithBonus(string mainText, string bonusText, Vector2 position, float scale)
        {
            if (_font == null) return;
            DrawText(mainText, position, Color.White, scale);
            if (!string.IsNullOrEmpty(bonusText))
            {
                float mainWidth = _font.MeasureString(mainText).X * scale;
                DrawText(bonusText, new Vector2(position.X + mainWidth, position.Y), Color.Gold, scale);
            }
        }

        // วาดวงกลมแดงแจ้งเตือนเล็กๆ ไว้มุมบนขวาของไอคอน (มีขอบดำบางๆ กันกลืนกับพื้นหลัง)
        private void DrawNotificationDot(Vector2 center, float radius)
        {
            DrawFilledCircle(center, radius + 2f, Color.Black);
            DrawFilledCircle(center, radius, Color.Red);
        }

        private void DrawFilledCircle(Vector2 center, float radius, Color color)
        {
            int r = (int)Math.Ceiling(radius);
            for (int y = -r; y <= r; y++)
            {
                float remaining = radius * radius - y * y;
                if (remaining < 0) continue;
                int dx = (int)Math.Sqrt(remaining);
                if (dx <= 0) continue;
                var rowRect = new Rectangle((int)(center.X - dx), (int)(center.Y + y), dx * 2, 1);
                _spriteBatch.Draw(_pixel, rowRect, color);
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

        private string GetUltimateSkillDescription(string characterName)
        {
            if (string.IsNullOrEmpty(characterName)) return "";

            if (characterName.Contains("Elma"))
                return "Strikes one enemy for 400% Physical ATK damage and forces every enemy to attack Elma for the next 2 turns.";
            if (characterName.Contains("Archer"))
                return "Fires a powerful, precise shot that deals 250% Physical ATK damage to a single enemy.";
            if (characterName.Contains("Cleric"))
                return "Channels healing light that restores 25% of the Cleric's own Max HP, plus 2% of each ally's own Max HP, to every ally at once.";
            if (characterName.Contains("Lumi"))
                return "Unleashes a wave of magic that deals 40% Magic ATK damage to every enemy on the battlefield.";
            if (characterName.Contains("Alice"))
                return "Enters a focused battle stance for 3 turns, boosting her own Physical ATK, Defense, and Speed by 30%, Crit Rate by 30%, and Crit Damage by 120%.";

            return "";
        }

        private void DrawWrappedText(string text, Vector2 position, float maxWidth, Color color, float scale = 1f, float lineHeight = 26f)
        {
            if (_font == null || string.IsNullOrEmpty(text)) return;

            string[] words = text.Split(' ');
            string line = "";
            float y = position.Y;

            foreach (var word in words)
            {
                string testLine = line.Length == 0 ? word : line + " " + word;
                float testWidth;
                try { testWidth = _font.MeasureString(GetSafeText(testLine)).X * scale; } catch (ArgumentException) { testWidth = 0; }

                if (testWidth > maxWidth && line.Length > 0)
                {
                    DrawText(line, new Vector2(position.X, y), color, scale);
                    line = word;
                    y += lineHeight;
                }
                else
                {
                    line = testLine;
                }
            }

            if (line.Length > 0)
                DrawText(line, new Vector2(position.X, y), color, scale);
        }
    }
}