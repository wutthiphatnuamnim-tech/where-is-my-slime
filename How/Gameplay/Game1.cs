using How.Battle;
using How.Data;
using How.Gameplay;
using How.Manager;
using How.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace How
{
    // เลขดาเมจ/MISS/CRIT ที่ลอยขึ้นเหนือหัวตัวละครที่โดนตี แล้วค่อยๆ จางหายไป
    internal class FloatingText
    {
        public Vector2 Position;
        public string Text;
        public float Timer;
        public Color Color;
        public float Scale;
    }

    // สถานะหน้าจอปัจจุบันของเกม
    internal enum AppState
    {
        MainMenu,
        Settings,
        Credits,
        Battle,
        Upgrade,    // หน้าแจกแต้มสเตตัสหลังชนะด่าน
        CardSelect, // หน้าเลือกการ์ด 3 ใบ (เลือก "อาวุธ"/บัฟถาวร)
        Rest,       // หน้าพัก: เลือกไอเทมสวมใส่ให้ตัวละคร หลังจากเลือกการ์ดเสร็จ
        Map         // หน้าเลือกด่านถัดไป (ธรรมดา / ยาก)
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private BattleManager _battle;
        private Texture2D _pixel;      // 1x1 texture ใช้วาดกรอบ/แถบเลือด/เส้น
        private SpriteFont _font;      // ต้องเพิ่มไฟล์ฟอนต์ใน Content (ดูคำแนะนำท้ายไฟล์)
        private readonly Random _rng = new Random();

        private readonly List<FloatingText> _floatingTexts = new List<FloatingText>();
        private const float FloatingTextDuration = 0.8f; // ระยะเวลาที่เลขดาเมจแสดงอยู่บนจอ (วินาที)
        private const float FloatingTextRiseSpeed = 45f;  // ความเร็วที่เลขดาเมจลอยขึ้น (พิกเซล/วินาที)
        private const float DamageTextScale = 2.2f;
        private const float CritTextScale = 2.8f;
        private const float MissTextScale = 1.8f;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        private AppState _appState = AppState.MainMenu;
        private MouseState _previousMouseState;
        private int _hoveredMenuIndex = -1;

        // ---------- Roguelite run state: ปาร์ตี้คงอยู่ข้ามด่าน จนกว่าจะแพ้หรือกลับเมนูหลัก ----------
        private List<Character> _party;
        private bool _currentStageIsHard;
        private int _upgradePointsRemaining;
        private int _upgradeSelectedIndex = -1; // -1 = ยังไม่ได้เลือกตัวละครในหน้า Upgrade
        private List<UpgradeCard> _currentCardChoices;
        private List<Item> _currentItemChoices;   // ไอเทม 3 ชิ้นให้เลือกในหน้า Rest
        private int _restSelectedIndex = -1;      // -1 = ยังไม่ได้เลือกตัวละครในหน้า Rest

        private int _hoveredUpgradeIcon = -1;
        private int _hoveredStatRow = -1;
        private bool _hoveredPanelClose;
        private bool _hoveredContinue;
        private int _hoveredCard = -1;
        private int _hoveredMapChoice = -1;
        private int _hoveredRestIcon = -1;
        private int _hoveredRestItem = -1;
        private bool _hoveredRestUnequip;
        private bool _hoveredRestContinue;

        private static readonly GrowthStat[] GrowthStats =
            { GrowthStat.STR, GrowthStat.INT, GrowthStat.VIT, GrowthStat.DEX, GrowthStat.LUX, GrowthStat.CRT };

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = ScreenWidth;
            _graphics.PreferredBackBufferHeight = ScreenHeight;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        // เริ่มเกมใหม่ทั้งหมด: ปาร์ตี้ใหม่ (แต้มโตเป็น 0, ไม่มีไอเทมสวม), ด่านแรกเป็นด่านธรรมดาเสมอ
        private void StartNewRun()
        {
            _party = BattleManager.CreateStartingParty();
            _upgradePointsRemaining = 0;
            _upgradeSelectedIndex = -1;
            _restSelectedIndex = -1;
            StartNewBattleForParty(isHard: false);
            _appState = AppState.Battle;
        }

        // เริ่มด่านใหม่โดยใช้ปาร์ตี้เดิม (คงสเตตัสที่อัพ/การ์ด/ไอเทมที่เลือกไว้) — ใช้ทั้งตอนเริ่มเกมและตอนเลือกด่านจากแมพ
        private void StartNewBattleForParty(bool isHard)
        {
            if (_battle != null)
                _battle.OnAttackResolved -= HandleAttackResolved;

            _currentStageIsHard = isHard;
            _battle = new BattleManager(_party, isHard);
            _battle.OnAttackResolved += HandleAttackResolved;
            _floatingTexts.Clear();
        }

        private void HandleAttackResolved(Character target, AttackResult result)
        {
            var rect = GetAvatarHomeRect(target); // ใช้ตำแหน่ง "บ้าน" เพราะเป้าหมายไม่ได้ขยับตอนโดนตี
            Vector2 pos = new Vector2(rect.X + rect.Width / 2f, rect.Y - 15f); // ลอยขึ้นเหนือหัวตัวละคร (Y - 15)

            if (result.IsMiss)
            {
                _floatingTexts.Add(new FloatingText { Position = pos, Text = "MISS", Timer = FloatingTextDuration, Color = Color.LightGray, Scale = MissTextScale });
                return;
            }

            string text = result.IsCrit ? $"-{result.Damage}!" : $"-{result.Damage}";
            Color color = result.IsCrit ? Color.Red : Color.OrangeRed;
            float scale = result.IsCrit ? CritTextScale : DamageTextScale;
            _floatingTexts.Add(new FloatingText { Position = pos, Text = text, Timer = FloatingTextDuration, Color = color, Scale = scale });
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // สร้าง texture 1x1 สีขาว ไว้ใช้วาดสี่เหลี่ยม/แถบเลือดโดยไม่ต้องใช้ไฟล์รูปภายนอก
            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // ลองโหลดฟอนต์จากชื่อที่เป็นไปได้หลายแบบ เผื่อชื่อไฟล์ใน Content.mgcb ไม่ตรงกับ "DefaultFont"
            string[] fontCandidates = { "DefaultFont", "SpriteFont", "Font", "Fonts/DefaultFont" };
            foreach (var name in fontCandidates)
            {
                try
                {
                    _font = Content.Load<SpriteFont>(name);
                    break;
                }
                catch
                {
                    _font = null;
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {
            var mouse = Mouse.GetState();
            var keyboard = Keyboard.GetState();
            bool leftClicked = mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;

            switch (_appState)
            {
                case AppState.MainMenu:
                    UpdateMainMenu(mouse, leftClicked, keyboard);
                    break;

                case AppState.Settings:
                case AppState.Credits:
                    if (leftClicked || keyboard.IsKeyDown(Keys.Escape))
                        _appState = AppState.MainMenu;
                    break;

                case AppState.Battle:
                    UpdateBattle(gameTime, keyboard);
                    break;

                case AppState.Upgrade:
                    UpdateUpgrade(mouse, leftClicked);
                    break;

                case AppState.CardSelect:
                    UpdateCardSelect(mouse, leftClicked);
                    break;

                case AppState.Rest:
                    UpdateRest(mouse, leftClicked);
                    break;

                case AppState.Map:
                    UpdateMap(mouse, leftClicked);
                    break;
            }

            _previousMouseState = mouse;
            base.Update(gameTime);
        }

        private void UpdateMainMenu(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
                Exit();

            _hoveredMenuIndex = -1;
            for (int i = 0; i < MenuLabels.Length; i++)
            {
                var layout = GetMenuButtonLayout(i);
                if (layout.Rect.Contains(mouse.Position))
                    _hoveredMenuIndex = i;
            }

            if (leftClicked && _hoveredMenuIndex >= 0)
            {
                switch (_hoveredMenuIndex)
                {
                    case 0: StartNewRun(); break;                      // Start
                    case 1: _appState = AppState.Settings; break;       // Setting
                    case 2: _appState = AppState.Credits; break;        // Credit
                }
            }
        }

        private void UpdateBattle(GameTime gameTime, KeyboardState keyboard)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                Exit();

            if (keyboard.IsKeyDown(Keys.Escape))
            {
                _appState = AppState.MainMenu;
                return;
            }

            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _battle.Update(delta);
            UpdateFloatingTexts(delta);

            // ชนะ: ให้แต้มอัพสเตตัสตามด่าน แล้วไปหน้า Upgrade ทันที
            if (_battle.State == BattleState.AllyWin)
            {
                _upgradePointsRemaining += _battle.RewardPoints;
                _upgradeSelectedIndex = -1;
                _appState = AppState.Upgrade;
                return;
            }

            // แพ้: กด R เพื่อเริ่มรันใหม่ทั้งหมด
            if (_battle.State == BattleState.EnemyWin)
            {
                if (keyboard.IsKeyDown(Keys.R))
                    StartNewRun();
            }
        }

        private void UpdateFloatingTexts(float delta)
        {
            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                var ft = _floatingTexts[i];
                ft.Timer -= delta;
                ft.Position.Y -= FloatingTextRiseSpeed * delta;
                if (ft.Timer <= 0f)
                    _floatingTexts.RemoveAt(i);
            }
        }

        private void UpdateUpgrade(MouseState mouse, bool leftClicked)
        {
            _hoveredUpgradeIcon = -1;
            for (int i = 0; i < _party.Count; i++)
            {
                if (GetUpgradeIconRect(i).Contains(mouse.Position) || GetUpgradeButtonRect(i).Contains(mouse.Position))
                    _hoveredUpgradeIcon = i;
            }

            if (leftClicked && _hoveredUpgradeIcon >= 0)
                _upgradeSelectedIndex = _hoveredUpgradeIcon;

            _hoveredStatRow = -1;
            _hoveredPanelClose = false;
            if (_upgradeSelectedIndex >= 0)
            {
                for (int row = 0; row < GrowthStats.Length; row++)
                {
                    if (GetStatRowPlusButtonRect(row).Contains(mouse.Position))
                        _hoveredStatRow = row;
                }
                _hoveredPanelClose = GetPanelCloseButtonRect().Contains(mouse.Position);

                if (leftClicked)
                {
                    if (_hoveredStatRow >= 0 && _upgradePointsRemaining > 0)
                    {
                        _party[_upgradeSelectedIndex].ApplyGrowthPoint(GrowthStats[_hoveredStatRow]);
                        _upgradePointsRemaining--;
                    }
                    else if (_hoveredPanelClose)
                    {
                        _upgradeSelectedIndex = -1;
                    }
                }
            }

            _hoveredContinue = GetContinueButtonRect().Contains(mouse.Position);
            if (leftClicked && _hoveredContinue)
            {
                _currentCardChoices = UpgradeCardPool.GetRandomCards(3, _rng);
                _upgradeSelectedIndex = -1;
                _appState = AppState.CardSelect;
            }
        }

        private void UpdateCardSelect(MouseState mouse, bool leftClicked)
        {
            _hoveredCard = -1;
            for (int i = 0; i < _currentCardChoices.Count; i++)
            {
                if (GetCardRect(i).Contains(mouse.Position))
                    _hoveredCard = i;
            }

            if (leftClicked && _hoveredCard >= 0)
            {
                _currentCardChoices[_hoveredCard].Apply(_party);

                // หลังเลือกการ์ด (อาวุธ) แล้ว แวะหน้าพักให้เลือกไอเทมสวมใส่ก่อนไปแมพเลือกด่านถัดไป
                _currentItemChoices = ItemPool.GetRandomItems(3, _rng);
                _restSelectedIndex = -1;
                _appState = AppState.Rest;
            }
        }

        private void UpdateRest(MouseState mouse, bool leftClicked)
        {
            _hoveredRestIcon = -1;
            for (int i = 0; i < _party.Count; i++)
            {
                if (GetRestIconRect(i).Contains(mouse.Position))
                    _hoveredRestIcon = i;
            }

            if (leftClicked && _hoveredRestIcon >= 0)
                _restSelectedIndex = _hoveredRestIcon;

            _hoveredRestItem = -1;
            _hoveredRestUnequip = false;
            if (_restSelectedIndex >= 0)
            {
                for (int i = 0; i < _currentItemChoices.Count; i++)
                {
                    if (GetRestItemRect(i).Contains(mouse.Position))
                        _hoveredRestItem = i;
                }
                _hoveredRestUnequip = GetRestUnequipButtonRect().Contains(mouse.Position);

                if (leftClicked)
                {
                    var selected = _party[_restSelectedIndex];
                    if (_hoveredRestItem >= 0)
                    {
                        selected.EquipItem(_currentItemChoices[_hoveredRestItem]);
                    }
                    else if (_hoveredRestUnequip)
                    {
                        selected.UnequipItem();
                    }
                }
            }

            _hoveredRestContinue = GetRestContinueButtonRect().Contains(mouse.Position);
            if (leftClicked && _hoveredRestContinue)
            {
                _restSelectedIndex = -1;
                _appState = AppState.Map;
            }
        }

        private void UpdateMap(MouseState mouse, bool leftClicked)
        {
            _hoveredMapChoice = -1;
            for (int i = 0; i < 2; i++)
            {
                if (GetMapButtonRect(i).Contains(mouse.Position))
                    _hoveredMapChoice = i;
            }

            if (leftClicked && _hoveredMapChoice >= 0)
            {
                StartNewBattleForParty(isHard: _hoveredMapChoice == 1);
                _appState = AppState.Battle;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(30, 30, 45));

            _spriteBatch.Begin();

            switch (_appState)
            {
                case AppState.MainMenu:
                    DrawMainMenu();
                    break;

                case AppState.Settings:
                    DrawPlaceholderScreen("Setting", "Coming soon - click anywhere or press Esc to go back");
                    break;

                case AppState.Credits:
                    DrawPlaceholderScreen("Credit", "Made with MonoGame - click anywhere or press Esc to go back");
                    break;

                case AppState.Battle:
                    DrawTeam(_battle.Allies, isEnemyRow: false);
                    DrawTeam(_battle.Enemies, isEnemyRow: true);
                    DrawAttackLine();
                    DrawFloatingTexts();
                    DrawResultBanner();
                    break;

                case AppState.Upgrade:
                    DrawUpgrade();
                    break;

                case AppState.CardSelect:
                    DrawCardSelect();
                    break;

                case AppState.Rest:
                    DrawRest();
                    break;

                case AppState.Map:
                    DrawMap();
                    break;
            }

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        // ---------- Main Menu ----------
        private const string GameTitle = "Lost Crystal";
        private const int TitleY = 220;
        private static readonly string[] MenuLabels = { "Start", "Setting", "Credit" };
        private const int MenuButtonSpacing = 90;
        private const float MenuButtonScale = 1.6f;
        private const float TitleScale = 3f;

        private struct MenuButtonLayout
        {
            public Rectangle Rect;
            public Vector2 Center;
            public Vector2 TextSize;
        }

        private MenuButtonLayout GetMenuButtonLayout(int index)
        {
            if (_font == null)
                return new MenuButtonLayout { Rect = Rectangle.Empty, Center = Vector2.Zero, TextSize = Vector2.Zero };

            string label = MenuLabels[index];
            Vector2 rawSize = _font.MeasureString(label);
            Vector2 scaledSize = rawSize * MenuButtonScale;

            int centerX = ScreenWidth / 2;
            int centerY = TitleY + 100 + index * MenuButtonSpacing;

            var rect = new Rectangle(
                (int)(centerX - scaledSize.X / 2f - 24),
                (int)(centerY - scaledSize.Y / 2f - 10),
                (int)scaledSize.X + 48,
                (int)scaledSize.Y + 20);

            return new MenuButtonLayout { Rect = rect, Center = new Vector2(centerX, centerY), TextSize = rawSize };
        }

        private void DrawMainMenu()
        {
            DrawTextCentered(GameTitle, new Vector2(ScreenWidth / 2f, TitleY), Color.Gold, TitleScale);

            for (int i = 0; i < MenuLabels.Length; i++)
            {
                var layout = GetMenuButtonLayout(i);
                bool hovered = _hoveredMenuIndex == i;
                Color color = hovered ? Color.Gold : Color.White;

                DrawTextCentered(MenuLabels[i], layout.Center, color, MenuButtonScale);

                if (hovered)
                {
                    float scaledHalfHeight = (layout.TextSize.Y * MenuButtonScale) / 2f;
                    int barWidth = (int)(layout.TextSize.X * MenuButtonScale) + 24;
                    int barY = (int)(layout.Center.Y + scaledHalfHeight + 8);
                    var barRect = new Rectangle((int)(layout.Center.X - barWidth / 2f), barY, barWidth, 4);
                    DrawRect(barRect, Color.Gold);
                }
            }
        }

        private void DrawPlaceholderScreen(string title, string subtitle)
        {
            DrawTextCentered(title, new Vector2(ScreenWidth / 2f, ScreenHeight / 2f - 40), Color.Gold, 2.4f);
            DrawTextCentered(subtitle, new Vector2(ScreenWidth / 2f, ScreenHeight / 2f + 30), Color.White, 1f);
        }

        // ---------- Upgrade Screen ----------
        private const int UpgradeIconSize = 140;
        private const int UpgradeIconSpacing = 40;
        private const int UpgradeIconY = 320;

        private Rectangle GetUpgradeIconRect(int i)
        {
            int totalWidth = _party.Count * UpgradeIconSize + (_party.Count - 1) * UpgradeIconSpacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            return new Rectangle(startX + i * (UpgradeIconSize + UpgradeIconSpacing), UpgradeIconY, UpgradeIconSize, UpgradeIconSize);
        }

        private Rectangle GetUpgradeButtonRect(int i)
        {
            var icon = GetUpgradeIconRect(i);
            return new Rectangle(icon.X, icon.Bottom + 12, icon.Width, 40);
        }

        private Rectangle GetUpgradePanelRect() => new Rectangle(ScreenWidth / 2 - 260, 470, 520, 430);

        private Rectangle GetStatRowPlusButtonRect(int rowIndex)
        {
            var panel = GetUpgradePanelRect();
            int y = panel.Y + 80 + rowIndex * 56;
            return new Rectangle(panel.Right - 90, y, 56, 42);
        }

        private Rectangle GetPanelCloseButtonRect()
        {
            var panel = GetUpgradePanelRect();
            return new Rectangle(panel.Right - 90, panel.Y + 12, 70, 36);
        }

        private Rectangle GetContinueButtonRect() => new Rectangle(ScreenWidth - 280, ScreenHeight - 100, 220, 64);

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

        private void DrawUpgrade()
        {
            DrawTextCentered("Upgrade", new Vector2(ScreenWidth / 2f, 120), Color.Gold, 3f);
            DrawTextCentered($"Points Remaining: {_upgradePointsRemaining}", new Vector2(ScreenWidth / 2f, 195), Color.White, 1.3f);

            for (int i = 0; i < _party.Count; i++)
            {
                var icon = GetUpgradeIconRect(i);
                var c = _party[i];
                bool hovered = _hoveredUpgradeIcon == i;
                bool selected = _upgradeSelectedIndex == i;
                Color borderColor = hovered ? Color.Gold : selected ? Color.LightBlue : Color.White;

                DrawRect(icon, new Color(60, 120, 200));
                DrawRectBorder(icon, borderColor, selected ? 4 : 3);
                DrawTextCentered(c.Name, new Vector2(icon.Center.X, icon.Y - 24), Color.White, 1f);

                var btnRect = GetUpgradeButtonRect(i);
                DrawRect(btnRect, hovered ? new Color(90, 150, 230) : new Color(50, 90, 150));
                DrawRectBorder(btnRect, Color.White, 2);
                DrawTextCentered("Upgrade", new Vector2(btnRect.Center.X, btnRect.Center.Y), Color.White, 0.85f);
            }

            if (_upgradeSelectedIndex >= 0)
                DrawUpgradePanel(_party[_upgradeSelectedIndex]);

            var contRect = GetContinueButtonRect();
            DrawRect(contRect, _hoveredContinue ? Color.Gold : new Color(50, 120, 50));
            DrawRectBorder(contRect, Color.White, 3);
            DrawTextCentered("Continue ->", new Vector2(contRect.Center.X, contRect.Center.Y), Color.White, 1.1f);
        }

        private void DrawUpgradePanel(Character selected)
        {
            var panel = GetUpgradePanelRect();
            DrawRect(panel, new Color(10, 10, 15, 235));
            DrawRectBorder(panel, Color.Gold, 3);
            DrawTextCentered($"{selected.Name} - Stats", new Vector2(panel.Center.X, panel.Y + 35), Color.Gold, 1.4f);

            for (int row = 0; row < GrowthStats.Length; row++)
            {
                var stat = GrowthStats[row];
                int value = GetGrowthValue(selected, stat);
                var plusRect = GetStatRowPlusButtonRect(row);

                DrawText($"{stat}: {value}", new Vector2(panel.X + 30, plusRect.Y + 10), Color.White);

                bool canSpend = _upgradePointsRemaining > 0;
                bool hoveredRow = _hoveredStatRow == row;
                Color plusColor = !canSpend ? Color.DarkGray : hoveredRow ? Color.Gold : new Color(50, 150, 50);
                DrawRect(plusRect, plusColor);
                DrawRectBorder(plusRect, Color.White, 2);
                DrawTextCentered("+", new Vector2(plusRect.Center.X, plusRect.Center.Y), Color.White, 1.3f);
            }

            var closeRect = GetPanelCloseButtonRect();
            DrawRect(closeRect, _hoveredPanelClose ? Color.Gold : new Color(150, 50, 50));
            DrawRectBorder(closeRect, Color.White, 2);
            DrawTextCentered("Close", new Vector2(closeRect.Center.X, closeRect.Center.Y), Color.White, 0.85f);
        }

        // ---------- Card Select Screen ----------
        private Rectangle GetCardRect(int i)
        {
            const int cardWidth = 400;
            const int cardHeight = 460;
            const int spacing = 50;
            int totalWidth = 3 * cardWidth + 2 * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            int y = (ScreenHeight - cardHeight) / 2 + 20;
            return new Rectangle(startX + i * (cardWidth + spacing), y, cardWidth, cardHeight);
        }

        private void DrawCardSelect()
        {
            DrawTextCentered("Choose a Card", new Vector2(ScreenWidth / 2f, 140), Color.Gold, 2.6f);

            for (int i = 0; i < _currentCardChoices.Count; i++)
            {
                var rect = GetCardRect(i);
                var card = _currentCardChoices[i];
                bool hovered = _hoveredCard == i;

                DrawRect(rect, hovered ? new Color(60, 60, 95) : new Color(25, 25, 40));
                DrawRectBorder(rect, hovered ? Color.Gold : Color.White, hovered ? 4 : 2);

                DrawTextCentered(card.Title, new Vector2(rect.Center.X, rect.Y + 70), Color.Gold, 1.3f);

                var parts = card.Description.Split(new[] { "   /   " }, StringSplitOptions.None);
                for (int j = 0; j < parts.Length; j++)
                {
                    DrawTextCentered(parts[j], new Vector2(rect.Center.X, rect.Y + 170 + j * 45), Color.White, 0.9f);
                }
            }
        }

        // ---------- Rest Screen (เลือกไอเทมสวมใส่หลังเลือกการ์ด) ----------
        private const int RestIconSize = 140;
        private const int RestIconSpacing = 40;
        private const int RestIconY = 300;

        private Rectangle GetRestIconRect(int i)
        {
            int totalWidth = _party.Count * RestIconSize + (_party.Count - 1) * RestIconSpacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            return new Rectangle(startX + i * (RestIconSize + RestIconSpacing), RestIconY, RestIconSize, RestIconSize);
        }

        private Rectangle GetRestUnequipButtonRect() => new Rectangle(ScreenWidth / 2 - 110, 570, 220, 46);

        private Rectangle GetRestItemRect(int i)
        {
            const int cardWidth = 360;
            const int cardHeight = 180;
            const int spacing = 40;
            int totalWidth = 3 * cardWidth + 2 * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            const int y = 650;
            return new Rectangle(startX + i * (cardWidth + spacing), y, cardWidth, cardHeight);
        }

        private Rectangle GetRestContinueButtonRect() => new Rectangle(ScreenWidth - 280, ScreenHeight - 100, 220, 64);

        private void DrawRest()
        {
            DrawTextCentered("Rest Camp", new Vector2(ScreenWidth / 2f, 120), Color.Gold, 3f);
            DrawTextCentered("Pick a party member, then equip an item", new Vector2(ScreenWidth / 2f, 195), Color.White, 1.1f);

            for (int i = 0; i < _party.Count; i++)
            {
                var icon = GetRestIconRect(i);
                var c = _party[i];
                bool hovered = _hoveredRestIcon == i;
                bool selected = _restSelectedIndex == i;
                Color borderColor = hovered ? Color.Gold : selected ? Color.LightBlue : Color.White;

                DrawRect(icon, new Color(60, 120, 200));
                DrawRectBorder(icon, borderColor, selected ? 4 : 3);
                DrawTextCentered(c.Name, new Vector2(icon.Center.X, icon.Y - 24), Color.White, 1f);

                string equippedLabel = c.EquippedItem != null ? c.EquippedItem.Name : "(empty)";
                Color equippedColor = c.EquippedItem != null ? Color.LightGreen : Color.Gray;
                DrawTextCentered(equippedLabel, new Vector2(icon.Center.X, icon.Bottom + 22), equippedColor, 0.8f);
            }

            if (_restSelectedIndex >= 0)
            {
                var selected = _party[_restSelectedIndex];
                bool hasItem = selected.EquippedItem != null;

                var unequipRect = GetRestUnequipButtonRect();
                Color unequipColor = !hasItem ? new Color(60, 60, 60) : _hoveredRestUnequip ? Color.Gold : new Color(150, 50, 50);
                DrawRect(unequipRect, unequipColor);
                DrawRectBorder(unequipRect, Color.White, 2);
                DrawTextCentered(hasItem ? "Unequip" : "No Item", new Vector2(unequipRect.Center.X, unequipRect.Center.Y), Color.White, 0.9f);

                for (int i = 0; i < _currentItemChoices.Count; i++)
                {
                    var rect = GetRestItemRect(i);
                    var item = _currentItemChoices[i];
                    bool hovered = _hoveredRestItem == i;
                    bool isEquipped = selected.EquippedItem == item;

                    DrawRect(rect, hovered ? new Color(60, 60, 95) : new Color(25, 25, 40));
                    DrawRectBorder(rect, isEquipped ? Color.LightGreen : hovered ? Color.Gold : Color.White, (isEquipped || hovered) ? 4 : 2);

                    DrawTextCentered(item.Name, new Vector2(rect.Center.X, rect.Y + 50), Color.Gold, 1.1f);
                    DrawTextCentered(item.Description, new Vector2(rect.Center.X, rect.Y + 110), Color.White, 0.9f);
                }
            }

            var contRect = GetRestContinueButtonRect();
            DrawRect(contRect, _hoveredRestContinue ? Color.Gold : new Color(50, 120, 50));
            DrawRectBorder(contRect, Color.White, 3);
            DrawTextCentered("Continue ->", new Vector2(contRect.Center.X, contRect.Center.Y), Color.White, 1.1f);
        }

        // ---------- Map Screen ----------
        private Rectangle GetMapButtonRect(int i)
        {
            const int w = 560;
            const int h = 280;
            const int spacing = 80;
            int totalWidth = 2 * w + spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            int y = (ScreenHeight - h) / 2 + 20;
            return new Rectangle(startX + i * (w + spacing), y, w, h);
        }

        private void DrawMap()
        {
            DrawTextCentered("Choose Your Path", new Vector2(ScreenWidth / 2f, 140), Color.Gold, 2.6f);

            for (int i = 0; i < 2; i++)
            {
                var rect = GetMapButtonRect(i);
                bool hovered = _hoveredMapChoice == i;

                DrawRect(rect, hovered ? new Color(70, 70, 110) : new Color(30, 30, 45));
                DrawRectBorder(rect, hovered ? Color.Gold : Color.White, hovered ? 4 : 2);

                string title = i == 0 ? "Normal Stage" : "Hard Stage";
                string desc = i == 0 ? "Reward: +5 Points" : "Enemy HP / ATK +150%\nReward: +10 Points";

                DrawTextCentered(title, new Vector2(rect.Center.X, rect.Y + 60), Color.Gold, 1.6f);
                DrawTextCentered(desc, new Vector2(rect.Center.X, rect.Center.Y + 30), Color.White, 1f);
            }
        }

        // ---------- Layout constants: จัดทัพหน้า 3 หลัง 2 (ฟอร์เมชันแบบ 7 Knights) ----------
        private const int AvatarSize = 110;
        private const int RowSpacing = 165;
        private const int FormationTop = 230;
        private const int FrontRowCount = 3;
        private const int LungeDistance = 110;

        private const int HpBarGap = 6;
        private const int HpBarHeight = 12;

        private const int AllyFrontX = 650;
        private const int AllyBackX = 430;

        private static int EnemyFrontX => ScreenWidth - AllyFrontX - AvatarSize;
        private static int EnemyBackX => ScreenWidth - AllyBackX - AvatarSize;

        private Rectangle GetAvatarHomeRect(Character unit)
        {
            var team = unit.IsEnemy ? _battle.Enemies : _battle.Allies;
            int index = team.IndexOf(unit);

            bool isFront = index < FrontRowCount;
            int rowSlot = isFront ? index : index - FrontRowCount;

            int y = isFront
                ? FormationTop + rowSlot * RowSpacing
                : FormationTop + RowSpacing / 2 + rowSlot * RowSpacing;

            int x;
            if (unit.IsEnemy)
                x = isFront ? EnemyFrontX : EnemyBackX;
            else
                x = isFront ? AllyFrontX : AllyBackX;

            return new Rectangle(x, y, AvatarSize, AvatarSize);
        }

        private Rectangle GetAvatarDisplayRect(Character unit)
        {
            var home = GetAvatarHomeRect(unit);

            if (_battle.ActiveAttacker != unit || _battle.ActiveTarget == null)
                return home;

            Vector2 fromCenter = new Vector2(home.X + home.Width / 2f, home.Y + home.Height / 2f);
            Rectangle targetHome = GetAvatarHomeRect(_battle.ActiveTarget);
            Vector2 toCenter = new Vector2(targetHome.X + targetHome.Width / 2f, targetHome.Y + targetHome.Height / 2f);

            Vector2 direction = toCenter - fromCenter;
            float distance = direction.Length();
            if (distance > 0.01f) direction /= distance;

            float progress = MathHelper.Clamp(_battle.AttackAnimTimer / BattleManager.AttackAnimDuration, 0f, 1f);
            float lungeAmount = (float)Math.Sin(progress * Math.PI) * Math.Min(LungeDistance, distance * 0.5f);

            Vector2 offset = direction * lungeAmount;
            return new Rectangle((int)(home.X + offset.X), (int)(home.Y + offset.Y), home.Width, home.Height);
        }

        private void DrawTeam(List<Character> team, bool isEnemyRow)
        {
            foreach (var unit in team)
            {
                bool isActive = _battle.ActiveAttacker == unit || _battle.ActiveTarget == unit;
                Color themeColor = isEnemyRow ? new Color(180, 60, 60) : new Color(60, 120, 200);
                Color borderColor = !unit.IsAlive ? Color.DarkGray : isActive ? Color.Gold : themeColor;

                var avatarRect = GetAvatarDisplayRect(unit);
                Color avatarColor = !unit.IsAlive ? new Color(50, 50, 50) : themeColor;
                DrawRect(avatarRect, avatarColor);
                DrawRectBorder(avatarRect, borderColor, 3);

                var home = GetAvatarHomeRect(unit);
                var hpBg = new Rectangle(home.X, home.Y - HpBarGap - HpBarHeight, home.Width, HpBarHeight);
                DrawRect(hpBg, new Color(60, 20, 20));

                float hpRatio = unit.MaxHp > 0 ? (float)unit.Hp / unit.MaxHp : 0f;
                Color hpColor = hpRatio > 0.5f ? Color.LimeGreen : hpRatio > 0.2f ? Color.Orange : Color.Red;
                DrawRect(new Rectangle(hpBg.X, hpBg.Y, (int)(hpBg.Width * hpRatio), hpBg.Height), hpColor);
                DrawRectBorder(hpBg, Color.Black, 1);
            }
        }

        private void DrawAttackLine()
        {
            if (_battle.ActiveAttacker == null || _battle.ActiveTarget == null) return;

            Vector2 from = GetUnitCenter(_battle.ActiveAttacker);
            Vector2 to = GetUnitCenter(_battle.ActiveTarget);
            DrawLine(from, to, Color.Yellow, 3);
        }

        private Vector2 GetUnitCenter(Character unit)
        {
            var rect = GetAvatarDisplayRect(unit);
            return new Vector2(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
        }

        private void DrawFloatingTexts()
        {
            foreach (var ft in _floatingTexts)
            {
                float alpha = MathHelper.Clamp(ft.Timer / FloatingTextDuration, 0f, 1f);
                DrawTextCentered(ft.Text, ft.Position, ft.Color * alpha, ft.Scale);
            }
        }

        private void DrawResultBanner()
        {
            if (_battle.State == BattleState.Running) return;

            string text = _battle.State == BattleState.AllyWin
                ? "You Win!"
                : "You Lose! press R to restart run, Esc for menu";

            var rect = new Rectangle(ScreenWidth / 2 - 340, 20, 680, 40);
            DrawRect(rect, new Color(0, 0, 0, 200));
            DrawRectBorder(rect, Color.Gold, 2);
            DrawText(text, new Vector2(rect.X + 20, rect.Y + 10), Color.Gold);
        }

        // ---------- Helper drawing methods (ไม่ต้องพึ่งไฟล์ภาพภายนอก) ----------

        private void DrawRect(Rectangle rect, Color color)
        {
            _spriteBatch.Draw(_pixel, rect, color);
        }

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
            float angle = (float)Math.Atan2(edge.Y, edge.X);

            _spriteBatch.Draw(_pixel, new Rectangle((int)start.X, (int)start.Y, (int)length, thickness),
                null, color, angle, Vector2.Zero, SpriteEffects.None, 0);
        }

        private void DrawText(string text, Vector2 position, Color color)
        {
            if (_font != null)
                _spriteBatch.DrawString(_font, text, position, color);
        }

        private void DrawTextCentered(string text, Vector2 center, Color color, float scale = 1f)
        {
            if (_font == null) return;
            Vector2 size = _font.MeasureString(text);
            Vector2 origin = size / 2f;
            _spriteBatch.DrawString(_font, text, center, color, 0f, origin, scale, SpriteEffects.None, 0f);
        }
    }
}