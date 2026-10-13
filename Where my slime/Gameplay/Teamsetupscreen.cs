using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Where_My_Slime.Models;

namespace Where_My_Slime
{
    // หน้าจัดทีม (Formation): ลากฮีโร่จาก Hero Tray ลงช่อง Front/Back 5 ช่อง ก่อนเริ่มต่อสู้
    public class TeamSetupScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GameSession _session;

        private readonly Action _goBack;
        private readonly Action _startBattle;
        private readonly Action _playUIClick;

        private bool _hoveredTeamStartButton;
        private bool _hoveredTeamBackButton;
        private int _hoveredSlotIndex = -1;
        private int _hoveredHeroTrayIndex = -1;
        private int _selectedHeroIndex = -1; // index ใน AllHeroes ของฮีโร่ที่กำลังถูกเลือกค้างไว้ รอให้กดเลือกช่องที่จะใส่

        private bool _showTeamWarning;
        private float _teamWarningTimer;

        private MouseState _previousMouseState;
        private Character _infoPanelCharacter; // ตัวละครที่กำลังโชว์ข้อมูล INFO ค้างไว้ (คลิกขวาเพื่อเปิด/ปิด)

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public TeamSetupScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            GameSession session,
            Action goBack,
            Action startBattle,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _session = session;
            _goBack = goBack;
            _startBattle = startBattle;
            _playUIClick = playUIClick;
        }

        private const int InfoPanelWidth = 380;
        private const int InfoPanelRightMargin = 60;
        private const int InfoPanelGap = 40;

        // ตอนแผง INFO เปิดอยู่ ดันแค่ช่อง Formation ไปทางซ้ายเล็กน้อย ให้มีระยะห่างจากแผงพอดีๆ ไม่ชิดไม่ห่างเกินไป
        private int ContentOffsetX => _infoPanelCharacter != null ? -120 : 0;

        private Rectangle GetTeamSetupStartButtonRect() => new Rectangle(ScreenWidth / 2 - 110, ScreenHeight - 250, 220, 50);
        private Rectangle GetTeamSetupBackButtonRect() => new Rectangle(40, 40, 150, 50);

        private Rectangle GetFormationSlotRect(int i)
        {
            const int size = 120;
            const int gap = 25;
            int startX = ScreenWidth / 2 - 200 + ContentOffsetX;
            int startY = ScreenHeight / 2 - 240;

            if (i < 3)
            {
                return new Rectangle(startX + size + 60, startY - 30 + i * (size + gap - 10), size, size);
            }
            else
            {
                int backIndex = i - 3;
                return new Rectangle(startX, startY + backIndex * (size + gap), size, size);
            }
        }

        private Rectangle GetHeroTrayRect(int i)
        {
            const int w = 110;
            const int h = 130;
            const int spacing = 20;
            int totalWidth = _session.AllHeroes.Count * w + (_session.AllHeroes.Count - 1) * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            int y = ScreenHeight - 175;
            return new Rectangle(startX + i * (w + spacing), y, w, h);
        }

        public void Update(GameTime gameTime, MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_showTeamWarning)
            {
                _teamWarningTimer -= delta;
                if (_teamWarningTimer <= 0f) _showTeamWarning = false;
            }

            if (keyboard.IsKeyDown(Keys.Escape)) { _playUIClick?.Invoke(); _selectedHeroIndex = -1; _infoPanelCharacter = null; _goBack?.Invoke(); return; }

            _hoveredTeamStartButton = GetTeamSetupStartButtonRect().Contains(mouse.Position);
            _hoveredTeamBackButton = GetTeamSetupBackButtonRect().Contains(mouse.Position);

            _hoveredSlotIndex = -1;
            for (int i = 0; i < 5; i++)
            {
                if (GetFormationSlotRect(i).Contains(mouse.Position)) _hoveredSlotIndex = i;
            }

            _hoveredHeroTrayIndex = -1;
            for (int i = 0; i < _session.AllHeroes.Count; i++)
            {
                if (GetHeroTrayRect(i).Contains(mouse.Position)) _hoveredHeroTrayIndex = i;
            }

            // คลิกขวาที่ตัวละคร (ใน Hero Tray หรือในช่อง Formation) เพื่อเปิด/ปิดแผง INFO เหมือนหน้า Upgrade
            bool rightClicked = mouse.RightButton == ButtonState.Pressed && _previousMouseState.RightButton == ButtonState.Released;
            if (rightClicked)
            {
                Character rightClickedChar = null;
                if (_hoveredHeroTrayIndex >= 0) rightClickedChar = _session.AllHeroes[_hoveredHeroTrayIndex];
                else if (_hoveredSlotIndex >= 0 && _session.FormationSlots[_hoveredSlotIndex] != null) rightClickedChar = _session.FormationSlots[_hoveredSlotIndex];

                _infoPanelCharacter = (rightClickedChar != null && _infoPanelCharacter == rightClickedChar) ? null : rightClickedChar;
            }
            _previousMouseState = mouse;

            if (leftClicked)
            {
                if (_hoveredTeamBackButton) { _playUIClick?.Invoke(); _selectedHeroIndex = -1; _infoPanelCharacter = null; _goBack?.Invoke(); }
                else if (_hoveredTeamStartButton)
                {
                    bool hasCharacter = false;
                    foreach (var c in _session.FormationSlots)
                    {
                        if (c != null) { hasCharacter = true; break; }
                    }

                    if (hasCharacter)
                    {
                        _playUIClick?.Invoke();
                        _selectedHeroIndex = -1;
                        _startBattle?.Invoke();
                    }
                    else
                    {
                        _playUIClick?.Invoke();
                        _showTeamWarning = true;
                        _teamWarningTimer = 2.0f;
                    }
                }
                else if (_hoveredSlotIndex >= 0)
                {
                    _playUIClick?.Invoke();
                    if (!(_session.IsStoryMode && _session.StoryStageIndex == 0))
                    {
                        if (_selectedHeroIndex >= 0)
                        {
                            // มีฮีโร่ที่เลือกค้างไว้จาก Hero Tray -> เอาไปใส่ในช่องที่เพิ่งกด
                            var heroToPlace = _session.AllHeroes[_selectedHeroIndex];

                            // ถ้าฮีโร่ตัวนี้อยู่ในช่องอื่นอยู่แล้ว ให้เอาออกจากช่องเดิมก่อน กันไม่ให้ซ้ำสองช่อง
                            for (int i = 0; i < 5; i++)
                            {
                                if (_session.FormationSlots[i] == heroToPlace) _session.FormationSlots[i] = null;
                            }

                            _session.FormationSlots[_hoveredSlotIndex] = heroToPlace;
                            _selectedHeroIndex = -1;
                        }
                        else
                        {
                            // ไม่มีฮีโร่ที่เลือกค้างไว้ -> กดช่องเพื่อเอาฮีโร่ในช่องนั้นออก (พฤติกรรมเดิม)
                            _session.FormationSlots[_hoveredSlotIndex] = null;
                        }
                    }
                }
                else if (_hoveredHeroTrayIndex >= 0)
                {
                    _playUIClick?.Invoke();
                    if (!(_session.IsStoryMode && _session.StoryStageIndex == 0))
                    {
                        if (_selectedHeroIndex == _hoveredHeroTrayIndex)
                        {
                            // กดตัวที่เลือกอยู่ซ้ำ -> ยกเลิกการเลือก
                            _selectedHeroIndex = -1;
                        }
                        else
                        {
                            // เลือกฮีโร่ตัวนี้ค้างไว้ก่อน รอให้กดเลือกช่องที่จะใส่ในขั้นต่อไป
                            _selectedHeroIndex = _hoveredHeroTrayIndex;
                        }
                    }
                }
            }
        }

        public void Draw()
        {
            string modeTitle = _session.IsStoryMode
                ? $"Story Mode (1-{_session.StoryStageIndex + 1})"
                : (_session.IsNextBattleBoss ? "Challenge Mode - BOSS" : "Challenge Mode");
            DrawTextCentered($"Team Setup - {modeTitle}", new Vector2(ScreenWidth / 2f, 50), Color.Gold, 2.2f);

            for (int i = 0; i < 5; i++)
            {
                var slotRect = GetFormationSlotRect(i);
                bool hovered = _hoveredSlotIndex == i;
                Character c = _session.FormationSlots[i];

                bool awaitingPlacement = _selectedHeroIndex >= 0;
                Color bgColor = c != null ? new Color(50, 100, 180) : (hovered ? Color.DarkSlateGray : (awaitingPlacement ? new Color(60, 55, 25) : new Color(35, 35, 50)));
                DrawRect(slotRect, bgColor);
                DrawRectBorder(slotRect, hovered ? Color.Gold : (awaitingPlacement ? new Color(200, 170, 80) : Color.White), hovered ? 3 : (awaitingPlacement ? 3 : 2));

                if (c != null)
                {
                    DrawTextCentered(c.Name, new Vector2(slotRect.Center.X, slotRect.Center.Y - 15), Color.White, 1f);
                    DrawTextCentered($"HP:{c.MaxHp}", new Vector2(slotRect.Center.X, slotRect.Center.Y + 15), Color.LightGreen, 0.8f);
                }
                else
                {
                    DrawTextCentered("+", new Vector2(slotRect.Center.X, slotRect.Center.Y), Color.LightGray, 2.5f);
                }
            }

            int lineY = ScreenHeight - 200;
            DrawLine(new Vector2(100, lineY), new Vector2(ScreenWidth - 100, lineY), Color.Gold, 3);
            DrawText("Hero Tray", new Vector2(120, lineY - 35), Color.Gold, 1.2f);

            if (_selectedHeroIndex >= 0 && _selectedHeroIndex < _session.AllHeroes.Count)
            {
                DrawTextCentered($"{_session.AllHeroes[_selectedHeroIndex].Name} selected - tap a slot to place", new Vector2(ScreenWidth / 2f, 110), Color.Gold, 1.1f);
            }

            for (int i = 0; i < _session.AllHeroes.Count; i++)
            {
                var hero = _session.AllHeroes[i];
                var trayRect = GetHeroTrayRect(i);
                bool hovered = _hoveredHeroTrayIndex == i;
                bool selected = _selectedHeroIndex == i;

                bool inTeam = false;
                for (int s = 0; s < 5; s++) { if (_session.FormationSlots[s] == hero) { inTeam = true; break; } }

                Color heroBg = selected ? new Color(90, 80, 30) : (inTeam ? new Color(60, 60, 60) : (hovered ? new Color(70, 70, 100) : new Color(30, 30, 45)));
                DrawRect(trayRect, heroBg);
                DrawRectBorder(trayRect, selected ? Color.Gold : (inTeam ? Color.Gray : (hovered ? Color.Gold : Color.White)), selected ? 4 : (hovered ? 3 : 2));

                DrawTextCentered(hero.Name, new Vector2(trayRect.Center.X, trayRect.Y + 30), inTeam ? Color.Gray : Color.Gold, 1.1f);
                DrawTextCentered($"HP:{hero.MaxHp}", new Vector2(trayRect.Center.X, trayRect.Y + 70), Color.White, 0.8f);
            }

            var startRect = GetTeamSetupStartButtonRect();
            DrawRect(startRect, _hoveredTeamStartButton ? Color.Gold : new Color(50, 150, 50));
            DrawRectBorder(startRect, Color.White, 3);
            DrawTextCentered("Start Battle", new Vector2(startRect.Center.X, startRect.Center.Y), Color.White, 1.1f);

            if (_showTeamWarning)
            {
                DrawTextCentered("Please choose a character to play", new Vector2(startRect.Center.X, startRect.Y - 30), Color.Red, 1.3f);
            }

            var backRect = GetTeamSetupBackButtonRect();
            DrawRect(backRect, _hoveredTeamBackButton ? Color.DarkRed : new Color(50, 30, 30));
            DrawRectBorder(backRect, Color.White, 2);
            DrawTextCentered("Back", new Vector2(backRect.Center.X, backRect.Center.Y), Color.White, 1f);

            DrawInfoPanel();
        }

        private Rectangle GetInfoPanelRect() => new Rectangle(ScreenWidth - InfoPanelRightMargin - InfoPanelWidth - 300, 160, InfoPanelWidth, 600);

        // แผง INFO แบบเดียวกับหน้า Upgrade เปิด/ปิดด้วยการคลิกขวาที่ตัวละคร
        private void DrawInfoPanel()
        {
            if (_infoPanelCharacter == null) return;
            var c = _infoPanelCharacter;

            var infoRect = GetInfoPanelRect();
            DrawRect(infoRect, new Color(20, 20, 30, 240));
            DrawRectBorder(infoRect, Color.Gold, 2);

            DrawText("INFO", new Vector2(infoRect.X + 15, infoRect.Y + 12), Color.Gold, 0.9f);
            DrawText(c.Name, new Vector2(infoRect.X + 25, infoRect.Y + 45), Color.White, 1.2f);

            int statStartY = infoRect.Y + 95;
            int rowGap = 38;
            int col2X = infoRect.X + 210;

            DrawText($"HP: {c.MaxHp}", new Vector2(infoRect.X + 25, statStartY + rowGap * 0), Color.White, 0.9f);
            DrawText($"P. DEF: {c.PDef}", new Vector2(col2X, statStartY + rowGap * 0), Color.White, 0.9f);

            DrawText($"P. ATK: {c.PAtk}", new Vector2(infoRect.X + 25, statStartY + rowGap * 1), Color.White, 0.9f);
            DrawText($"M. ATK: {c.MAtk}", new Vector2(col2X, statStartY + rowGap * 1), Color.White, 0.9f);

            DrawText($"M. DEF: {c.MDef}", new Vector2(infoRect.X + 25, statStartY + rowGap * 2), Color.White, 0.9f);
            DrawText($"CRIT RATE: {(int)c.CritRate}%", new Vector2(col2X, statStartY + rowGap * 2), Color.White, 0.9f);

            DrawText($"CRIT DMG: {(int)c.CritDamage}%", new Vector2(infoRect.X + 25, statStartY + rowGap * 3), Color.White, 0.9f);
            DrawText($"SPEED: {c.Speed}", new Vector2(col2X, statStartY + rowGap * 3), Color.White, 0.9f);

            DrawText($"ACC: {(int)c.Accuracy}%", new Vector2(infoRect.X + 25, statStartY + rowGap * 4), Color.White, 0.9f);

            int ultimateY = statStartY + rowGap * 5 + 20;
            DrawText("Ultimate Skill", new Vector2(infoRect.X + 25, ultimateY), Color.Gold, 0.95f);
            DrawWrappedText(GetUltimateSkillDescription(c.Name), new Vector2(infoRect.X + 25, ultimateY + 36), infoRect.Width - 50, Color.White, 0.8f);

            DrawText("(Right-click again to close)", new Vector2(infoRect.X + 15, infoRect.Bottom - 30), Color.Gray, 0.75f);
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