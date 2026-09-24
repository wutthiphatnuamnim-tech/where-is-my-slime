using How.Battle;
using How.Data;
using How.Manager;
using How.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace How
{
    internal class FloatingText
    {
        public Vector2 Position;
        public string Text;
        public float Timer;
        public Color Color;
        public float Scale;
    }

    internal enum AppState
    {
        Logo,
        Intro,
        MainMenu,
        ModeSelect,
        Settings,
        Credits,
        TeamSetup,
        Battle,
        Upgrade,
        ItemSelect,
        Rest,
        Map,
        StoryModeSelect
    }

    internal enum TeamSetupOrigin
    {
        Story,
        ChallengeStart,
        ChallengeMap
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private BattleManager _battle;
        private Texture2D _pixel;
        private Texture2D _battleBackground;
        private Texture2D _introBackground;
        private Texture2D _logoBackground;
        private SpriteFont _font;
        private readonly Random _rng = new Random();

        // ตัวแปรอนิเมชั่น Elma แบบดั้งเดิม
        private Texture2D _elmaIdleTex;
        private Texture2D _elmaAttackTex;
        private float _elmaAnimTimer;
        private int _elmaFrameIndex;

        // ตัวแปรควบคุมอนิเมชั่นโจมตี Elma ปรับเวลาเหลือ 0.5 วินาที พร้อมบล็อกเทิร์น
        private bool _elmaIsAnimatingAttack;
        private float _elmaAttackHoldTimer;
        private Character _elmaStoredTarget;
        private int _elmaCustomFrameIndex;
        private float _elmaCustomAnimTimer;
        private bool _elmaHasAnimatedThisTurn;

        private readonly List<FloatingText> _floatingTexts = new List<FloatingText>();
        private const float FloatingTextDuration = 0.8f;
        private const float FloatingTextRiseSpeed = 45f;
        private const float DamageTextScale = 2.2f;
        private const float CritTextScale = 2.8f;
        private const float MissTextScale = 1.8f;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        private AppState _appState = AppState.Logo;
        private MouseState _previousMouseState;
        private KeyboardState _previousKeyboardState;

        private float _logoTimer;
        private const float LogoDuration = 2f;

        private float _introTimer;
        private const string IntroText = "Test 1 2 3 4 5 4RindaHAHAHA\n Tomyamkung";
        private const float IntroCharsPerSecond = 30f;
        private const float IntroInputGracePeriod = 0.2f;
        private int _hoveredMenuIndex = -1;
        private Character _hoveredEnemyTarget;
        private bool _hoveredAutoToggle;

        private List<Character> _party;
        private List<Character> _allHeroes = new List<Character>();
        private Character[] _formationSlots = new Character[5];
        private TeamSetupOrigin _teamSetupOrigin;
        private bool _currentStageIsHard;
        private bool _manualModePreference = true;
        private int _upgradeSelectedIndex = -1;
        private List<Item> _currentItemChoices;
        private List<Item> _inventory = new List<Item>();
        private int _restSelectedIndex = -1;
        private int _coins;

        private int _hoveredUpgradeIcon = -1;
        private int _hoveredStatPlusRow = -1;
        private int _hoveredStatMinusRow = -1;
        private bool _hoveredConfirmButton;
        private bool _hoveredPanelClose;
        private bool _hoveredContinue;
        private int _hoveredItemCard = -1;
        private bool _hoveredItemSkip;
        private int _hoveredMapChoice = -1;
        private int _hoveredRestIcon = -1;
        private int _hoveredRestItemRow = -1;
        private bool _hoveredRestUnequip;
        private bool _hoveredRestPanelClose;
        private bool _hoveredRestContinue;

        private bool _isStoryMode;
        private bool _hoveredStoryWinContinue;
        private bool _hoveredStoryWinMenu;

        private bool _hoveredLoseRestart;
        private bool _hoveredLoseMenu;

        private int _storyStageIndex = 0;
        private const int MaxStoryStages = 5;
        private bool _hoveredStoryLeft;
        private bool _hoveredStoryRight;
        private bool _hoveredStoryCenter;
        private bool _hoveredStoryBack;

        private bool _hoveredModeStory;
        private bool _hoveredModeChallenge;
        private bool _hoveredModeBack;

        private bool _hoveredTeamStartButton;
        private bool _hoveredTeamBackButton;
        private int _hoveredSlotIndex = -1;
        private int _hoveredHeroTrayIndex = -1;

        private static readonly GrowthStat[] GrowthStats =
            { GrowthStat.STR, GrowthStat.INT, GrowthStat.VIT, GrowthStat.DEX, GrowthStat.LUX, GrowthStat.CRT };
        private int[] _pendingAllocations = new int[GrowthStats.Length];

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

        private void PlayUIClick()
        {
            AudioManager.PlaySfx("Audio/UI_Click");
        }

        private void StartNewRunForChallenge()
        {
            _isStoryMode = false;
            _teamSetupOrigin = TeamSetupOrigin.ChallengeStart;
            _coins = 0;
            _inventory.Clear();
            _upgradeSelectedIndex = -1;

            _allHeroes = BattleManager.CreateStartingParty();
            Array.Clear(_formationSlots, 0, _formationSlots.Length);

            for (int i = 0; i < Math.Min(_allHeroes.Count, 5); i++)
            {
                _formationSlots[i] = _allHeroes[i];
            }

            _appState = AppState.TeamSetup;
        }

        private void StartNewBattleForParty(bool isHard)
        {
            if (_battle != null)
                _battle.OnAttackResolved -= HandleAttackResolved;

            _party = new List<Character>();
            foreach (var c in _formationSlots)
            {
                if (c != null) _party.Add(c);
            }
            if (_party.Count == 0 && _allHeroes.Count > 0)
            {
                _party.Add(_allHeroes[0]);
            }

            _currentStageIsHard = isHard;
            _battle = new BattleManager(_party, isHard);

            if (_isStoryMode && _storyStageIndex == 0)
            {
                if (_battle.Enemies != null && _battle.Enemies.Count > 1)
                {
                    _battle.Enemies.RemoveRange(1, _battle.Enemies.Count - 1);
                }
            }

            _battle.SetManualMode(_manualModePreference);
            _battle.OnAttackResolved += HandleAttackResolved;
            _floatingTexts.Clear();

            // เล่นเพลงต่อสู้เมื่อเข้าฉาก Battle
            AudioManager.PlayBgm("Audio/battle_theme");
        }

        private void HandleAttackResolved(Character target, AttackResult result)
        {
            var rect = GetAvatarHomeRect(target);
            Vector2 pos = new Vector2(rect.X + rect.Width / 2f, rect.Y - 15f);

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

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            // โหลด Sound Effects และ BGM พร้อมระบุโฟลเดอร์ Audio ให้ถูกต้อง
            AudioManager.LoadAll(
                Content,
                new string[] { "Audio/UI_Click" },
                new string[] { "Audio/menu_theme", "Audio/battle_theme" }
            );

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

            try { _battleBackground = Content.Load<Texture2D>("bg_Battle_01"); } catch { _battleBackground = null; }
            try { _introBackground = Content.Load<Texture2D>("Intro_BG_Test001"); } catch { _introBackground = null; }
            try { _logoBackground = Content.Load<Texture2D>("LogoTest1"); } catch { _logoBackground = null; }

            try { _elmaIdleTex = Content.Load<Texture2D>("char_Elma_Idle"); } catch { _elmaIdleTex = null; }
            try { _elmaAttackTex = Content.Load<Texture2D>("char_Elma_Attack"); } catch { _elmaAttackTex = null; }
        }

        protected override void Update(GameTime gameTime)
        {
            var mouse = Mouse.GetState();
            var keyboard = Keyboard.GetState();
            bool leftClicked = mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;

            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _elmaAnimTimer += delta;
            if (_elmaAnimTimer >= 0.12f)
            {
                _elmaAnimTimer = 0f;
                _elmaFrameIndex++;
            }

            // จัดการอนิเมชั่นและบล็อกเทิร์นของ Elma (ปรับเวลาเหลือ 0.5 วินาที)
            if (_battle != null && _appState == AppState.Battle)
            {
                bool isElmaAttacking = (_battle.ActiveAttacker != null && _battle.ActiveAttacker.Name == "Elma");

                if (isElmaAttacking)
                {
                    if (!_elmaHasAnimatedThisTurn)
                    {
                        _elmaIsAnimatingAttack = true;
                        _elmaAttackHoldTimer = 0.5f;
                        _elmaStoredTarget = _battle.ActiveTarget;
                        _elmaCustomFrameIndex = 0;
                        _elmaCustomAnimTimer = 0f;
                        _elmaHasAnimatedThisTurn = true;
                    }
                }
                else
                {
                    _elmaHasAnimatedThisTurn = false;
                }

                if (_elmaIsAnimatingAttack)
                {
                    _elmaAttackHoldTimer -= delta;
                    if (_elmaAttackTex != null)
                    {
                        int frameWidth = 320;
                        int totalFrames = Math.Max(1, _elmaAttackTex.Width / frameWidth);
                        float timePerFrame = 0.5f / totalFrames;
                        _elmaCustomAnimTimer += delta;
                        if (_elmaCustomAnimTimer >= timePerFrame)
                        {
                            _elmaCustomAnimTimer = 0f;
                            if (_elmaCustomFrameIndex < totalFrames - 1)
                            {
                                _elmaCustomFrameIndex++;
                            }
                        }
                    }

                    if (_elmaAttackHoldTimer <= 0f)
                    {
                        _elmaIsAnimatingAttack = false;
                        _elmaStoredTarget = null;
                    }
                }
                else
                {
                    _battle.Update(delta);
                }
            }

            UpdateFloatingTexts(delta);

            switch (_appState)
            {
                case AppState.Logo:
                    UpdateLogo(gameTime, keyboard, leftClicked);
                    break;

                case AppState.Intro:
                    UpdateIntro(gameTime, keyboard, leftClicked);
                    break;

                case AppState.MainMenu:
                    UpdateMainMenu(mouse, leftClicked, keyboard);
                    break;

                case AppState.ModeSelect:
                    UpdateModeSelect(mouse, leftClicked, keyboard);
                    break;

                case AppState.StoryModeSelect:
                    UpdateStoryModeSelect(mouse, leftClicked, keyboard);
                    break;

                case AppState.TeamSetup:
                    UpdateTeamSetup(mouse, leftClicked, keyboard);
                    break;

                case AppState.Settings:
                case AppState.Credits:
                    if (leftClicked || keyboard.IsKeyDown(Keys.Escape))
                    {
                        if (leftClicked) PlayUIClick();
                        _appState = AppState.MainMenu;
                    }
                    break;

                case AppState.Battle:
                    UpdateBattle(gameTime, keyboard, mouse, leftClicked);
                    break;

                case AppState.Upgrade:
                    UpdateUpgrade(mouse, leftClicked);
                    break;

                case AppState.ItemSelect:
                    UpdateItemSelect(mouse, leftClicked);
                    break;

                case AppState.Rest:
                    UpdateRest(mouse, leftClicked);
                    break;

                case AppState.Map:
                    UpdateMap(mouse, leftClicked);
                    break;
            }

            _previousMouseState = mouse;
            _previousKeyboardState = keyboard;
            base.Update(gameTime);
        }

        private void UpdateBattle(GameTime gameTime, KeyboardState keyboard, MouseState mouse, bool leftClicked)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            {
                PlayUIClick();
                _appState = AppState.MainMenu;
                return;
            }

            _hoveredAutoToggle = GetAutoToggleButtonRect().Contains(mouse.Position);
            if (leftClicked && _hoveredAutoToggle)
            {
                PlayUIClick();
                _battle.SetManualMode(!_battle.ManualMode);
                _manualModePreference = _battle.ManualMode;
            }

            _hoveredEnemyTarget = null;
            if (_battle.PendingAttacker != null)
            {
                foreach (var enemy in _battle.Enemies)
                {
                    if (!enemy.IsAlive) continue;
                    if (GetAvatarHomeRect(enemy).Contains(mouse.Position)) { _hoveredEnemyTarget = enemy; break; }
                }

                if (leftClicked && _hoveredEnemyTarget != null)
                {
                    _battle.TrySelectTarget(_hoveredEnemyTarget);
                    _hoveredEnemyTarget = null;
                }
            }

            if (_battle.State == BattleState.AllyWin)
            {
                if (!_isStoryMode)
                {
                    foreach (var c in _party) c.UpgradePoints += _battle.RewardPoints;
                    _coins += _battle.RewardCoins;
                    _upgradeSelectedIndex = -1;
                    _appState = AppState.Upgrade;
                }
                else
                {
                    _hoveredStoryWinContinue = GetStoryWinContinueRect().Contains(mouse.Position);
                    _hoveredStoryWinMenu = GetStoryWinMenuRect().Contains(mouse.Position);

                    if (leftClicked)
                    {
                        if (_hoveredStoryWinContinue) { PlayUIClick(); _appState = AppState.StoryModeSelect; }
                        else if (_hoveredStoryWinMenu) { PlayUIClick(); _appState = AppState.MainMenu; }
                    }
                }
            }
            else if (_battle.State == BattleState.EnemyWin)
            {
                _hoveredLoseRestart = GetLoseRestartRect().Contains(mouse.Position);
                _hoveredLoseMenu = GetLoseMenuRect().Contains(mouse.Position);

                if (leftClicked)
                {
                    if (_hoveredLoseRestart)
                    {
                        PlayUIClick();
                        if (_isStoryMode)
                        {
                            StartNewBattleForParty(_currentStageIsHard);
                            _appState = AppState.Battle;
                        }
                        else
                        {
                            StartNewRunForChallenge();
                        }
                    }
                    else if (_hoveredLoseMenu)
                    {
                        PlayUIClick();
                        _appState = AppState.MainMenu;
                    }
                }
            }
        }

        private void UpdateLogo(GameTime gameTime, KeyboardState keyboard, bool leftClicked)
        {
            _logoTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            bool anyNewKeyPressed = false;
            var pressedKeys = keyboard.GetPressedKeys();
            for (int i = 0; i < pressedKeys.Length; i++)
            {
                if (!_previousKeyboardState.IsKeyDown(pressedKeys[i]))
                {
                    anyNewKeyPressed = true;
                    break;
                }
            }

            if (_logoTimer >= LogoDuration || leftClicked || anyNewKeyPressed)
            {
                _appState = AppState.Intro;
                _introTimer = 0f;
            }
        }

        private void UpdateIntro(GameTime gameTime, KeyboardState keyboard, bool leftClicked)
        {
            _introTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            bool anyNewKeyPressed = false;
            var pressedKeys = keyboard.GetPressedKeys();
            for (int i = 0; i < pressedKeys.Length; i++)
            {
                if (!_previousKeyboardState.IsKeyDown(pressedKeys[i]))
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
                    _appState = AppState.MainMenu;
            }
        }

        private void UpdateMainMenu(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            AudioManager.PlayBgm("Audio/menu_theme");

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
                Exit();

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
                PlayUIClick();
                switch (_hoveredMenuIndex)
                {
                    case 0: _appState = AppState.ModeSelect; break;
                    case 1: _appState = AppState.Settings; break;
                    case 2: _appState = AppState.Credits; break;
                }
            }
        }

        private Rectangle GetModeSelectStoryCardRect() => new Rectangle(ScreenWidth / 2 - 440, ScreenHeight / 2 - 250, 400, 500);
        private Rectangle GetModeSelectChallengeCardRect() => new Rectangle(ScreenWidth / 2 + 40, ScreenHeight / 2 - 250, 400, 500);
        private Rectangle GetModeSelectBackRect() => new Rectangle(40, 40, 150, 50);

        private void UpdateModeSelect(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape))
            {
                PlayUIClick();
                _appState = AppState.MainMenu;
                return;
            }

            _hoveredModeStory = GetModeSelectStoryCardRect().Contains(mouse.Position);
            _hoveredModeChallenge = GetModeSelectChallengeCardRect().Contains(mouse.Position);
            _hoveredModeBack = GetModeSelectBackRect().Contains(mouse.Position);

            if (leftClicked)
            {
                if (_hoveredModeBack) { PlayUIClick(); _appState = AppState.MainMenu; }
                else if (_hoveredModeStory) { PlayUIClick(); _appState = AppState.StoryModeSelect; }
                else if (_hoveredModeChallenge) { PlayUIClick(); StartNewRunForChallenge(); }
            }
        }

        private Rectangle GetStoryCenterCardRect() => new Rectangle(ScreenWidth / 2 - 180, ScreenHeight / 2 - 250, 360, 500);
        private Rectangle GetStoryLeftCardRect() => new Rectangle(ScreenWidth / 2 - 420, ScreenHeight / 2 - 200, 300, 400);
        private Rectangle GetStoryRightCardRect() => new Rectangle(ScreenWidth / 2 + 120, ScreenHeight / 2 - 200, 300, 400);
        private Rectangle GetStoryLeftArrowRect() => new Rectangle(ScreenWidth / 2 - 600, ScreenHeight / 2 - 50, 60, 100);
        private Rectangle GetStoryRightArrowRect() => new Rectangle(ScreenWidth / 2 + 540, ScreenHeight / 2 - 50, 60, 100);
        private Rectangle GetStoryBackButtonRect() => new Rectangle(40, 40, 150, 50);
        private Rectangle GetStoryWinContinueRect() => new Rectangle(ScreenWidth / 2 - 250, ScreenHeight / 2 - 30, 220, 60);
        private Rectangle GetStoryWinMenuRect() => new Rectangle(ScreenWidth / 2 + 30, ScreenHeight / 2 - 30, 220, 60);

        private Rectangle GetLoseRestartRect() => new Rectangle(ScreenWidth / 2 - 250, ScreenHeight / 2 - 30, 220, 60);
        private Rectangle GetLoseMenuRect() => new Rectangle(ScreenWidth / 2 + 30, ScreenHeight / 2 - 30, 220, 60);

        private Rectangle GetTeamSetupStartButtonRect() => new Rectangle(ScreenWidth / 2 - 110, ScreenHeight - 250, 220, 50);
        private Rectangle GetTeamSetupBackButtonRect() => new Rectangle(40, 40, 150, 50);

        private Rectangle GetFormationSlotRect(int i)
        {
            const int size = 120;
            const int gap = 25;
            int startX = ScreenWidth / 2 - 200;
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
            int totalWidth = _allHeroes.Count * w + (_allHeroes.Count - 1) * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            int y = ScreenHeight - 175;
            return new Rectangle(startX + i * (w + spacing), y, w, h);
        }

        private void UpdateStoryModeSelect(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape)) { PlayUIClick(); _appState = AppState.ModeSelect; return; }

            _hoveredStoryCenter = GetStoryCenterCardRect().Contains(mouse.Position);
            _hoveredStoryLeft = _storyStageIndex > 0 && GetStoryLeftArrowRect().Contains(mouse.Position);
            _hoveredStoryRight = _storyStageIndex < MaxStoryStages - 1 && GetStoryRightArrowRect().Contains(mouse.Position);
            _hoveredStoryBack = GetStoryBackButtonRect().Contains(mouse.Position);

            if (leftClicked)
            {
                if (_hoveredStoryLeft) { PlayUIClick(); _storyStageIndex--; }
                else if (_hoveredStoryRight) { PlayUIClick(); _storyStageIndex++; }
                else if (_hoveredStoryBack) { PlayUIClick(); _appState = AppState.ModeSelect; }
                else if (_hoveredStoryCenter)
                {
                    PlayUIClick();
                    _isStoryMode = true;
                    _teamSetupOrigin = TeamSetupOrigin.Story;
                    _allHeroes = BattleManager.CreateStartingParty();
                    Array.Clear(_formationSlots, 0, _formationSlots.Length);

                    if (_storyStageIndex == 0)
                    {
                        Character mage = null;
                        foreach (var c in _allHeroes) { if (c.Name == "Mage") { mage = c; break; } }
                        if (mage == null && _allHeroes.Count > 0) mage = _allHeroes[0];
                        _formationSlots[1] = mage;
                    }
                    else
                    {
                        for (int i = 0; i < Math.Min(_allHeroes.Count, 5); i++) _formationSlots[i] = _allHeroes[i];
                    }
                    _appState = AppState.TeamSetup;
                }
            }
        }

        private void UpdateTeamSetup(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape)) { PlayUIClick(); GoBackFromTeamSetup(); return; }

            _hoveredTeamStartButton = GetTeamSetupStartButtonRect().Contains(mouse.Position);
            _hoveredTeamBackButton = GetTeamSetupBackButtonRect().Contains(mouse.Position);

            _hoveredSlotIndex = -1;
            for (int i = 0; i < 5; i++)
            {
                if (GetFormationSlotRect(i).Contains(mouse.Position)) _hoveredSlotIndex = i;
            }

            _hoveredHeroTrayIndex = -1;
            for (int i = 0; i < _allHeroes.Count; i++)
            {
                if (GetHeroTrayRect(i).Contains(mouse.Position)) _hoveredHeroTrayIndex = i;
            }

            if (leftClicked)
            {
                if (_hoveredTeamBackButton) { PlayUIClick(); GoBackFromTeamSetup(); }
                else if (_hoveredTeamStartButton) { PlayUIClick(); StartBattleFromTeamSetup(); }
                else if (_hoveredSlotIndex >= 0)
                {
                    PlayUIClick();
                    if (!(_isStoryMode && _storyStageIndex == 0)) { _formationSlots[_hoveredSlotIndex] = null; }
                }
                else if (_hoveredHeroTrayIndex >= 0)
                {
                    PlayUIClick();
                    if (!(_isStoryMode && _storyStageIndex == 0))
                    {
                        var clickedHero = _allHeroes[_hoveredHeroTrayIndex];
                        int existingSlot = -1;
                        for (int i = 0; i < 5; i++) { if (_formationSlots[i] == clickedHero) { existingSlot = i; break; } }

                        if (existingSlot >= 0)
                        {
                            int activeCount = 0;
                            for (int i = 0; i < 5; i++) if (_formationSlots[i] != null) activeCount++;
                            if (activeCount > 1) _formationSlots[existingSlot] = null;
                        }
                        else
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                if (_formationSlots[i] == null) { _formationSlots[i] = clickedHero; break; }
                            }
                        }
                    }
                }
            }
        }

        private void GoBackFromTeamSetup()
        {
            if (_isStoryMode) _appState = AppState.StoryModeSelect;
            else if (_teamSetupOrigin == TeamSetupOrigin.ChallengeStart) _appState = AppState.ModeSelect;
            else _appState = AppState.Map;
        }

        private void StartBattleFromTeamSetup()
        {
            StartNewBattleForParty(_teamSetupOrigin == TeamSetupOrigin.ChallengeMap && _currentStageIsHard);
            _appState = AppState.Battle;
        }

        private void UpdateFloatingTexts(float delta)
        {
            for (int i = _floatingTexts.Count - 1; i >= 0; i--)
            {
                var ft = _floatingTexts[i];
                ft.Timer -= delta;
                ft.Position.Y -= FloatingTextRiseSpeed * delta;
                if (ft.Timer <= 0f) _floatingTexts.RemoveAt(i);
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

            if (leftClicked && _hoveredUpgradeIcon >= 0 && _hoveredUpgradeIcon != _upgradeSelectedIndex)
            {
                PlayUIClick();
                _upgradeSelectedIndex = _hoveredUpgradeIcon;
                ResetPendingAllocations();
            }

            _hoveredStatPlusRow = -1;
            _hoveredStatMinusRow = -1;
            _hoveredPanelClose = false;
            _hoveredConfirmButton = false;
            if (_upgradeSelectedIndex >= 0)
            {
                var selected = _party[_upgradeSelectedIndex];
                int pendingUsed = SumPendingAllocations();
                int remaining = selected.UpgradePoints - pendingUsed;

                for (int row = 0; row < GrowthStats.Length; row++)
                {
                    if (GetStatRowPlusButtonRect(row).Contains(mouse.Position)) _hoveredStatPlusRow = row;
                    if (GetStatRowMinusButtonRect(row).Contains(mouse.Position)) _hoveredStatMinusRow = row;
                }
                _hoveredPanelClose = GetPanelCloseButtonRect().Contains(mouse.Position);
                _hoveredConfirmButton = pendingUsed > 0 && GetConfirmButtonRect().Contains(mouse.Position);

                if (leftClicked)
                {
                    if (_hoveredStatPlusRow >= 0 && remaining > 0) { PlayUIClick(); _pendingAllocations[_hoveredStatPlusRow]++; }
                    else if (_hoveredStatMinusRow >= 0 && _pendingAllocations[_hoveredStatMinusRow] > 0) { PlayUIClick(); _pendingAllocations[_hoveredStatMinusRow]--; }
                    else if (_hoveredConfirmButton)
                    {
                        PlayUIClick();
                        for (int row = 0; row < GrowthStats.Length; row++)
                        {
                            for (int n = 0; n < _pendingAllocations[row]; n++) selected.ApplyGrowthPoint(GrowthStats[row]);
                        }
                        selected.UpgradePoints -= pendingUsed;
                        ResetPendingAllocations();
                    }
                    else if (_hoveredPanelClose)
                    {
                        PlayUIClick();
                        ResetPendingAllocations();
                        _upgradeSelectedIndex = -1;
                    }
                }
            }

            _hoveredContinue = GetContinueButtonRect().Contains(mouse.Position);
            if (leftClicked && _hoveredContinue)
            {
                PlayUIClick();
                _currentItemChoices = ItemPool.GetRandomItems(3, _rng);
                _hoveredItemCard = -1;
                _upgradeSelectedIndex = -1;
                ResetPendingAllocations();
                _appState = AppState.ItemSelect;
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
        }

        private void UpdateItemSelect(MouseState mouse, bool leftClicked)
        {
            _hoveredItemCard = -1;
            for (int i = 0; i < _currentItemChoices.Count; i++)
            {
                if (GetItemCardRect(i).Contains(mouse.Position)) _hoveredItemCard = i;
            }

            if (leftClicked && _hoveredItemCard >= 0)
            {
                PlayUIClick();
                var item = _currentItemChoices[_hoveredItemCard];
                if (_coins >= item.Price)
                {
                    _coins -= item.Price;
                    _inventory.Add(item);
                    _restSelectedIndex = -1;
                    _appState = AppState.Rest;
                }
            }

            _hoveredItemSkip = GetItemSkipButtonRect().Contains(mouse.Position);
            if (leftClicked && _hoveredItemSkip)
            {
                PlayUIClick();
                _restSelectedIndex = -1;
                _appState = AppState.Rest;
            }
        }

        private void UpdateRest(MouseState mouse, bool leftClicked)
        {
            _hoveredRestIcon = -1;
            for (int i = 0; i < _party.Count; i++)
            {
                if (GetRestIconRect(i).Contains(mouse.Position) || GetRestButtonRect(i).Contains(mouse.Position))
                    _hoveredRestIcon = i;
            }

            if (leftClicked && _hoveredRestIcon >= 0) { PlayUIClick(); _restSelectedIndex = _hoveredRestIcon; }

            _hoveredRestItemRow = -1;
            _hoveredRestUnequip = false;
            _hoveredRestPanelClose = false;
            if (_restSelectedIndex >= 0)
            {
                var selected = _party[_restSelectedIndex];
                for (int row = 0; row < _inventory.Count; row++)
                {
                    if (GetRestItemRowButtonRect(row).Contains(mouse.Position)) _hoveredRestItemRow = row;
                }
                _hoveredRestUnequip = selected.EquippedItem != null && GetRestUnequipButtonRect().Contains(mouse.Position);
                _hoveredRestPanelClose = GetRestPanelCloseButtonRect().Contains(mouse.Position);

                if (leftClicked)
                {
                    if (_hoveredRestItemRow >= 0)
                    {
                        PlayUIClick();
                        var newItem = _inventory[_hoveredRestItemRow];
                        var previousItem = selected.EquippedItem;
                        selected.EquipItem(newItem);
                        _inventory.RemoveAt(_hoveredRestItemRow);
                        if (previousItem != null) _inventory.Add(previousItem);
                        _hoveredRestItemRow = -1;
                    }
                    else if (_hoveredRestUnequip)
                    {
                        PlayUIClick();
                        var removedItem = selected.EquippedItem;
                        selected.UnequipItem();
                        if (removedItem != null) _inventory.Add(removedItem);
                    }
                    else if (_hoveredRestPanelClose) { PlayUIClick(); _restSelectedIndex = -1; }
                }
            }

            _hoveredRestContinue = GetRestContinueButtonRect().Contains(mouse.Position);
            if (leftClicked && _hoveredRestContinue)
            {
                PlayUIClick();
                _restSelectedIndex = -1;
                _appState = AppState.Map;
            }
        }

        private void UpdateMap(MouseState mouse, bool leftClicked)
        {
            _hoveredMapChoice = -1;
            for (int i = 0; i < 2; i++)
            {
                if (GetMapButtonRect(i).Contains(mouse.Position)) _hoveredMapChoice = i;
            }

            if (leftClicked && _hoveredMapChoice >= 0)
            {
                PlayUIClick();
                _currentStageIsHard = (_hoveredMapChoice == 1);
                _teamSetupOrigin = TeamSetupOrigin.ChallengeMap;
                Array.Clear(_formationSlots, 0, _formationSlots.Length);
                for (int i = 0; i < Math.Min(_party.Count, 5); i++) _formationSlots[i] = _party[i];
                _appState = AppState.TeamSetup;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            Color clearColor = (_appState == AppState.Logo || _appState == AppState.Intro) ? Color.Black : new Color(30, 30, 45);
            GraphicsDevice.Clear(clearColor);

            _spriteBatch.Begin();

            switch (_appState)
            {
                case AppState.Logo:
                    DrawLogo();
                    break;

                case AppState.Intro:
                    DrawIntro();
                    break;

                case AppState.MainMenu:
                    DrawMainMenu();
                    break;

                case AppState.ModeSelect:
                    DrawModeSelect();
                    break;

                case AppState.StoryModeSelect:
                    DrawStoryModeSelect();
                    break;

                case AppState.TeamSetup:
                    DrawTeamSetup();
                    break;

                case AppState.Settings:
                    DrawPlaceholderScreen("Setting", "Coming soon - click anywhere or press Esc to go back");
                    break;

                case AppState.Credits:
                    DrawPlaceholderScreen("Credit", "Made with MonoGame - click anywhere or press Esc to go back");
                    break;

                case AppState.Battle:
                    DrawBattleBackground();
                    DrawTeam(_battle.Allies, isEnemyRow: false);
                    DrawTeam(_battle.Enemies, isEnemyRow: true);
                    DrawFloatingTexts();
                    DrawTurnArrows();
                    DrawResultBanner();
                    DrawAutoToggleButton();
                    break;

                case AppState.Upgrade:
                    DrawUpgrade();
                    break;

                case AppState.ItemSelect:
                    DrawItemSelect();
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

        private void DrawLogo()
        {
            if (_logoBackground != null)
            {
                _spriteBatch.Draw(_logoBackground, new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White);
            }
            else
            {
                DrawTextCentered("LOGO", new Vector2(ScreenWidth / 2f, ScreenHeight / 2f), Color.White, 3f);
            }
        }

        private const string GameTitle = "Where is my slime";
        private const int TitleY = 220;
        private static readonly string[] MenuLabels = { "Play", "Setting", "Credit" };
        private static readonly bool[] MenuEnabled = { true, true, true };
        private const int MenuButtonSpacing = 90;
        private const float MenuButtonScale = 1.6f;
        private const float TitleScale = 3f;
        private const int MenuRightMargin = 80;

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

            int centerX = ScreenWidth - MenuRightMargin - (int)(scaledSize.X / 2f);
            int centerY = TitleY + 100 + index * MenuButtonSpacing;

            var rect = new Rectangle(
                (int)(centerX - scaledSize.X / 2f - 24),
                (int)(centerY - scaledSize.Y / 2f - 10),
                (int)scaledSize.X + 48,
                (int)scaledSize.Y + 20);

            return new MenuButtonLayout { Rect = rect, Center = new Vector2(centerX, centerY), TextSize = rawSize };
        }

        private const int IntroTextBoxHeight = 200;
        private const int IntroDividerThickness = 6;
        private const float IntroTextScale = 2.5f;

        private void DrawIntro()
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

        private void DrawMainMenu()
        {
            if (_font != null)
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
                    float scaledHalfHeight = (layout.TextSize.Y * MenuButtonScale) / 2f;
                    int barWidth = (int)(layout.TextSize.X * MenuButtonScale) + 24;
                    int barY = (int)(layout.Center.Y + scaledHalfHeight + 8);
                    var barRect = new Rectangle((int)(layout.Center.X - barWidth / 2f), barY, barWidth, 4);
                    DrawRect(barRect, Color.Gold);
                }
            }
        }

        private void DrawModeSelect()
        {
            DrawTextCentered("Select Game Mode", new Vector2(ScreenWidth / 2f, 120), Color.Gold, 2.8f);

            var storyRect = GetModeSelectStoryCardRect();
            DrawRect(storyRect, _hoveredModeStory ? new Color(50, 50, 75) : new Color(30, 30, 45));
            DrawRectBorder(storyRect, _hoveredModeStory ? Color.Gold : Color.White, _hoveredModeStory ? 5 : 3);
            DrawTextCentered("Story Mode", new Vector2(storyRect.Center.X, storyRect.Center.Y), Color.White, 2f);

            var challengeRect = GetModeSelectChallengeCardRect();
            DrawRect(challengeRect, _hoveredModeChallenge ? new Color(50, 50, 75) : new Color(30, 30, 45));
            DrawRectBorder(challengeRect, _hoveredModeChallenge ? Color.Gold : Color.White, _hoveredModeChallenge ? 5 : 3);
            DrawTextCentered("Challenge mode", new Vector2(challengeRect.Center.X, challengeRect.Center.Y), Color.White, 2f);

            var backRect = GetModeSelectBackRect();
            DrawRect(backRect, _hoveredModeBack ? Color.DarkRed : new Color(50, 30, 30));
            DrawRectBorder(backRect, Color.White, 2);
            DrawTextCentered("Back", new Vector2(backRect.Center.X, backRect.Center.Y), Color.White, 1f);
        }

        private void DrawPlaceholderScreen(string title, string subtitle)
        {
            DrawTextCentered(title, new Vector2(ScreenWidth / 2f, ScreenHeight / 2f - 40), Color.Gold, 2.4f);
            DrawTextCentered(subtitle, new Vector2(ScreenWidth / 2f, ScreenHeight / 2f + 30), Color.White, 1f);
        }

        private void DrawStoryModeSelect()
        {
            DrawTextCentered("Story Mode", new Vector2(ScreenWidth / 2f, 100), Color.Gold, 2.5f);

            if (_storyStageIndex > 0)
            {
                var leftRect = GetStoryLeftCardRect();
                DrawRect(leftRect, new Color(20, 20, 25));
                DrawRectBorder(leftRect, Color.Gray, 3);
                DrawTextCentered($"1-{_storyStageIndex}", new Vector2(ScreenWidth / 2 - 300, leftRect.Bottom - 60), Color.Gray, 1.2f);
            }

            if (_storyStageIndex < MaxStoryStages - 1)
            {
                var rightRect = GetStoryRightCardRect();
                DrawRect(rightRect, new Color(20, 20, 25));
                DrawRectBorder(rightRect, Color.Gray, 3);
                DrawTextCentered($"1-{_storyStageIndex + 2}", new Vector2(ScreenWidth / 2 + 300, rightRect.Bottom - 60), Color.Gray, 1.2f);
            }

            var centerRect = GetStoryCenterCardRect();
            DrawRect(centerRect, _hoveredStoryCenter ? new Color(50, 50, 70) : new Color(30, 30, 45));
            DrawRectBorder(centerRect, _hoveredStoryCenter ? Color.Gold : Color.White, 5);

            DrawTextCentered($"1-{_storyStageIndex + 1}", new Vector2(centerRect.Center.X, centerRect.Bottom - 80), Color.White, 3f);

            if (_hoveredStoryCenter)
                DrawTextCentered("Click to Play", new Vector2(centerRect.Center.X, centerRect.Center.Y), Color.Gold, 1.5f);

            if (_storyStageIndex > 0)
                DrawArrow(GetStoryLeftArrowRect(), true, _hoveredStoryLeft ? Color.Gold : Color.White, 8);

            if (_storyStageIndex < MaxStoryStages - 1)
                DrawArrow(GetStoryRightArrowRect(), false, _hoveredStoryRight ? Color.Gold : Color.White, 8);

            var backRect = GetStoryBackButtonRect();
            DrawRect(backRect, _hoveredStoryBack ? Color.DarkRed : new Color(50, 30, 30));
            DrawRectBorder(backRect, Color.White, 2);
            DrawTextCentered("Back", new Vector2(backRect.Center.X, backRect.Center.Y), Color.White, 1f);
        }

        private void DrawTeamSetup()
        {
            string modeTitle = _isStoryMode ? $"Story Mode (1-{_storyStageIndex + 1})" : "Challenge Mode";
            DrawTextCentered($"Team Setup - {modeTitle}", new Vector2(ScreenWidth / 2f, 50), Color.Gold, 2.2f);

            for (int i = 0; i < 5; i++)
            {
                var slotRect = GetFormationSlotRect(i);
                bool hovered = _hoveredSlotIndex == i;
                Character c = _formationSlots[i];

                Color bgColor = c != null ? new Color(50, 100, 180) : (hovered ? Color.DarkSlateGray : new Color(35, 35, 50));
                DrawRect(slotRect, bgColor);
                DrawRectBorder(slotRect, hovered ? Color.Gold : Color.White, hovered ? 3 : 2);

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

            for (int i = 0; i < _allHeroes.Count; i++)
            {
                var hero = _allHeroes[i];
                var trayRect = GetHeroTrayRect(i);
                bool hovered = _hoveredHeroTrayIndex == i;

                bool inTeam = false;
                for (int s = 0; s < 5; s++) { if (_formationSlots[s] == hero) { inTeam = true; break; } }

                Color heroBg = inTeam ? new Color(60, 60, 60) : (hovered ? new Color(70, 70, 100) : new Color(30, 30, 45));
                DrawRect(trayRect, heroBg);
                DrawRectBorder(trayRect, inTeam ? Color.Gray : (hovered ? Color.Gold : Color.White), hovered ? 3 : 2);

                DrawTextCentered(hero.Name, new Vector2(trayRect.Center.X, trayRect.Y + 30), inTeam ? Color.Gray : Color.Gold, 1.1f);
                DrawTextCentered($"HP:{hero.MaxHp}", new Vector2(trayRect.Center.X, trayRect.Y + 70), Color.White, 0.8f);

                string badge = inTeam ? "[In Team]" : "[Available]";
                DrawTextCentered(badge, new Vector2(trayRect.Center.X, trayRect.Bottom - 25), inTeam ? Color.DimGray : Color.LightGreen, 0.75f);
            }

            var startRect = GetTeamSetupStartButtonRect();
            DrawRect(startRect, _hoveredTeamStartButton ? Color.Gold : new Color(50, 150, 50));
            DrawRectBorder(startRect, Color.White, 3);
            DrawTextCentered("Start Battle", new Vector2(startRect.Center.X, startRect.Center.Y), Color.White, 1.1f);

            var backRect = GetTeamSetupBackButtonRect();
            DrawRect(backRect, _hoveredTeamBackButton ? Color.DarkRed : new Color(50, 30, 30));
            DrawRectBorder(backRect, Color.White, 2);
            DrawTextCentered("Back", new Vector2(backRect.Center.X, backRect.Center.Y), Color.White, 1f);
        }

        private void DrawArrow(Rectangle rect, bool isLeft, Color color, int thickness)
        {
            Vector2 top = isLeft ? new Vector2(rect.Right, rect.Top) : new Vector2(rect.Left, rect.Top);
            Vector2 mid = isLeft ? new Vector2(rect.Left, rect.Center.Y) : new Vector2(rect.Right, rect.Center.Y);
            Vector2 bot = isLeft ? new Vector2(rect.Right, rect.Bottom) : new Vector2(rect.Left, rect.Bottom);

            DrawLine(top, mid, color, thickness);
            DrawLine(mid, bot, color, thickness);
        }

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

        private Rectangle GetUpgradePanelRect() => new Rectangle(ScreenWidth / 2 - 260, 520, 520, 440);
        private Rectangle GetStatRowMinusButtonRect(int rowIndex) => new Rectangle(GetUpgradePanelRect().Right - 170, GetUpgradePanelRect().Y + 90 + rowIndex * 46, 40, 38);
        private Rectangle GetStatRowPlusButtonRect(int rowIndex) => new Rectangle(GetUpgradePanelRect().Right - 90, GetUpgradePanelRect().Y + 90 + rowIndex * 46, 40, 38);
        private Rectangle GetConfirmButtonRect() => new Rectangle(GetUpgradePanelRect().X + 20, GetUpgradePanelRect().Y + 368, GetUpgradePanelRect().Width - 40, 44);
        private Rectangle GetPanelCloseButtonRect() => new Rectangle(GetUpgradePanelRect().Right - 90, GetUpgradePanelRect().Y + 12, 70, 36);
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
            DrawTextCentered($"Coins: {_coins}", new Vector2(ScreenWidth / 2f, 195), Color.White, 1.3f);

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
                DrawTextCentered($"Pts: {c.UpgradePoints}", new Vector2(icon.Center.X, icon.Y - 45), Color.LightGreen, 0.9f);

                var btnRect = GetUpgradeButtonRect(i);
                DrawRect(btnRect, hovered ? new Color(90, 150, 230) : new Color(50, 90, 150));
                DrawRectBorder(btnRect, Color.White, 2);
                DrawTextCentered("Upgrade", new Vector2(btnRect.Center.X, btnRect.Center.Y), Color.White, 0.85f);
            }

            if (_upgradeSelectedIndex >= 0) DrawUpgradePanel(_party[_upgradeSelectedIndex]);

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

            int pendingUsed = SumPendingAllocations();
            int remaining = selected.UpgradePoints - pendingUsed;
            DrawTextCentered($"Points: {remaining} / {selected.UpgradePoints}", new Vector2(panel.Center.X, panel.Y + 62), Color.LightGreen, 0.95f);

            for (int row = 0; row < GrowthStats.Length; row++)
            {
                var stat = GrowthStats[row];
                int baseValue = GetGrowthValue(selected, stat);
                int pending = _pendingAllocations[row];
                var minusRect = GetStatRowMinusButtonRect(row);
                var plusRect = GetStatRowPlusButtonRect(row);

                string valueText = pending > 0 ? $"{stat}: {baseValue} (+{pending})" : $"{stat}: {baseValue}";
                DrawText(valueText, new Vector2(panel.X + 30, minusRect.Y + 8), pending > 0 ? Color.Gold : Color.White);

                DrawRect(minusRect, !pending.Equals(0) ? (_hoveredStatMinusRow == row ? Color.Gold : new Color(150, 50, 50)) : Color.DarkGray);
                DrawRectBorder(minusRect, Color.White, 2);
                DrawTextCentered("-", new Vector2(minusRect.Center.X, minusRect.Center.Y), Color.White, 1.3f);

                DrawRect(plusRect, remaining > 0 ? (_hoveredStatPlusRow == row ? Color.Gold : new Color(50, 150, 50)) : Color.DarkGray);
                DrawRectBorder(plusRect, Color.White, 2);
                DrawTextCentered("+", new Vector2(plusRect.Center.X, plusRect.Center.Y), Color.White, 1.3f);
            }

            var confirmRect = GetConfirmButtonRect();
            DrawRect(confirmRect, pendingUsed > 0 ? (_hoveredConfirmButton ? Color.Gold : new Color(50, 120, 50)) : new Color(60, 60, 60));
            DrawRectBorder(confirmRect, Color.White, 3);
            DrawTextCentered(pendingUsed > 0 ? $"Confirm ({pendingUsed} pt)" : "Confirm", new Vector2(confirmRect.Center.X, confirmRect.Center.Y), Color.White, 1f);

            var closeRect = GetPanelCloseButtonRect();
            DrawRect(closeRect, _hoveredPanelClose ? Color.Gold : new Color(150, 50, 50));
            DrawRectBorder(closeRect, Color.White, 2);
            DrawTextCentered("Close", new Vector2(closeRect.Center.X, closeRect.Center.Y), Color.White, 0.85f);
        }

        private Rectangle GetItemCardRect(int i)
        {
            const int cardWidth = 400;
            const int cardHeight = 460;
            const int spacing = 50;
            int totalWidth = 3 * cardWidth + 2 * spacing;
            int startX = (ScreenWidth - totalWidth) / 2;
            int y = (ScreenHeight - cardHeight) / 2 + 20;
            return new Rectangle(startX + i * (cardWidth + spacing), y, cardWidth, cardHeight);
        }

        private Rectangle GetItemSkipButtonRect() => new Rectangle(ScreenWidth / 2 - 110, 830, 220, 56);

        private void DrawItemSelect()
        {
            DrawTextCentered("Choose an Item", new Vector2(ScreenWidth / 2f, 140), Color.Gold, 2.6f);
            DrawTextCentered($"Coins: {_coins}", new Vector2(ScreenWidth / 2f, 195), Color.Gold, 1.3f);

            for (int i = 0; i < _currentItemChoices.Count; i++)
            {
                var rect = GetItemCardRect(i);
                var item = _currentItemChoices[i];
                bool hovered = _hoveredItemCard == i;
                bool affordable = _coins >= item.Price;
                Color rarityColor = GetRarityColor(item.Rarity);

                Color baseCardBg = item.Rarity switch
                {
                    ItemRarity.Common => new Color(30, 30, 45),
                    ItemRarity.Uncommon => new Color(20, 45, 30),
                    ItemRarity.Rare => new Color(20, 35, 55),
                    ItemRarity.Epic => new Color(40, 25, 55),
                    ItemRarity.Legendary => new Color(50, 40, 20),
                    ItemRarity.Mythic => new Color(55, 20, 20),
                    _ => new Color(30, 30, 45)
                };

                DrawRect(rect, hovered && affordable ? new Color(60, 60, 95) : baseCardBg);
                DrawRectBorder(rect, !affordable ? Color.DarkGray : hovered ? Color.Gold : rarityColor, hovered && affordable ? 4 : 3);

                DrawTextCentered(item.Name, new Vector2(rect.Center.X, rect.Y + 60), !affordable ? Color.Gray : rarityColor, 1.3f);
                DrawTextCentered($"[{item.Rarity.ToString().ToUpper()}]", new Vector2(rect.Center.X, rect.Y + 110), rarityColor, 0.95f);
                DrawTextCentered(item.Description, new Vector2(rect.Center.X, rect.Y + 185), Color.White, 0.9f);
                DrawTextCentered($"Price: {item.Price} Coin", new Vector2(rect.Center.X, rect.Y + 245), affordable ? Color.LightGreen : Color.OrangeRed, 1f);
            }

            var skipRect = GetItemSkipButtonRect();
            DrawRect(skipRect, _hoveredItemSkip ? Color.Gold : new Color(80, 80, 80));
            DrawRectBorder(skipRect, Color.White, 3);
            DrawTextCentered("Skip ->", new Vector2(skipRect.Center.X, skipRect.Center.Y), Color.White, 1.1f);
        }

        private const int RestIconSize = 140;
        private const int RestIconSpacing = 40;
        private const int RestIconY = 320;

        private Rectangle GetRestIconRect(int i) => new Rectangle(((ScreenWidth - (_party.Count * RestIconSize + (_party.Count - 1) * RestIconSpacing)) / 2) + i * (RestIconSize + RestIconSpacing), RestIconY, RestIconSize, RestIconSize);
        private Rectangle GetRestButtonRect(int i) => new Rectangle(GetRestIconRect(i).X, GetRestIconRect(i).Bottom + 12, RestIconSize, 40);
        private Rectangle GetRestPanelRect() => new Rectangle(ScreenWidth / 2 - 260, 520, 520, Math.Min(100 + Math.Max(_inventory.Count, 1) * 88 + 70, 430));
        private Rectangle GetRestItemRowRect(int rowIndex) => new Rectangle(GetRestPanelRect().X + 20, GetRestPanelRect().Y + 100 + rowIndex * 88, GetRestPanelRect().Width - 40, 78);
        private Rectangle GetRestItemRowButtonRect(int rowIndex) => new Rectangle(GetRestItemRowRect(rowIndex).Right - 116, GetRestItemRowRect(rowIndex).Y + GetRestItemRowRect(rowIndex).Height / 2 - 20, 100, 40);
        private Rectangle GetRestUnequipButtonRect() => new Rectangle(GetRestPanelRect().X + 20, GetRestPanelRect().Y + 100 + Math.Max(_inventory.Count, 1) * 88 + 8, GetRestPanelRect().Width - 40, 44);
        private Rectangle GetRestPanelCloseButtonRect() => new Rectangle(GetRestPanelRect().Right - 90, GetRestPanelRect().Y + 12, 70, 36);
        private Rectangle GetRestContinueButtonRect() => new Rectangle(ScreenWidth - 280, ScreenHeight - 100, 220, 64);

        private void DrawRest()
        {
            DrawTextCentered("Rest Camp", new Vector2(ScreenWidth / 2f, 120), Color.Gold, 3f);
            DrawTextCentered($"Coins: {_coins}     |     Inventory: {_inventory.Count} item(s) available", new Vector2(ScreenWidth / 2f, 195), Color.White, 1.2f);

            for (int i = 0; i < _party.Count; i++)
            {
                var icon = GetRestIconRect(i);
                var c = _party[i];
                bool hovered = _hoveredRestIcon == i;
                bool selected = _restSelectedIndex == i;

                DrawRect(icon, new Color(60, 120, 200));
                DrawRectBorder(icon, hovered ? Color.Gold : selected ? Color.LightBlue : Color.White, selected ? 4 : 3);
                DrawTextCentered(c.Name, new Vector2(icon.Center.X, icon.Y - 24), Color.White, 1f);

                var btnRect = GetRestButtonRect(i);
                DrawRect(btnRect, hovered ? new Color(90, 150, 230) : new Color(50, 90, 150));
                DrawRectBorder(btnRect, Color.White, 2);
                DrawTextCentered("Equip", new Vector2(btnRect.Center.X, btnRect.Center.Y), Color.White, 0.85f);

                // ปรับสีชื่อไอเทมที่สวมใส่ตามระดับ Rarity ของไอเทม
                string equippedName = c.EquippedItem != null ? c.EquippedItem.Name : "(empty)";
                Color equippedColor = c.EquippedItem != null ? GetRarityColor(c.EquippedItem.Rarity) : Color.Gray;
                DrawTextCentered(equippedName, new Vector2(icon.Center.X, btnRect.Bottom + 22), equippedColor, 0.8f);
            }

            if (_restSelectedIndex >= 0) DrawRestPanel(_party[_restSelectedIndex]);

            var contRect = GetRestContinueButtonRect();
            DrawRect(contRect, _hoveredRestContinue ? Color.Gold : new Color(50, 120, 50));
            DrawRectBorder(contRect, Color.White, 3);
            DrawTextCentered("Continue ->", new Vector2(contRect.Center.X, contRect.Center.Y), Color.White, 1.1f);
        }

        private void DrawRestPanel(Character selected)
        {
            var panel = GetRestPanelRect();
            DrawRect(panel, new Color(10, 10, 15, 235));
            DrawRectBorder(panel, Color.Gold, 3);
            DrawTextCentered($"{selected.Name} - Equip Item", new Vector2(panel.Center.X, panel.Y + 35), Color.Gold, 1.4f);

            string equippedName = selected.EquippedItem != null ? selected.EquippedItem.Name : "None";
            Color equippedColor = selected.EquippedItem != null ? GetRarityColor(selected.EquippedItem.Rarity) : Color.LightGreen;
            DrawTextCentered($"Currently Equipped: {equippedName}", new Vector2(panel.Center.X, panel.Y + 72), equippedColor, 0.95f);

            if (_inventory.Count == 0) DrawTextCentered("Inventory is empty", new Vector2(panel.Center.X, panel.Y + 140), Color.Gray, 1f);

            for (int row = 0; row < _inventory.Count; row++)
            {
                var item = _inventory[row];
                var rowRect = GetRestItemRowRect(row);
                bool hoveredRow = _hoveredRestItemRow == row;
                Color rarityColor = GetRarityColor(item.Rarity);

                DrawRectBorder(rowRect, hoveredRow ? Color.Gold : rarityColor, hoveredRow ? 3 : 1);
                DrawText(item.Name, new Vector2(rowRect.X + 16, rowRect.Y + 10), rarityColor);
                DrawText($"[{item.Rarity.ToString().ToUpper()}]", new Vector2(rowRect.X + 220, rowRect.Y + 12), rarityColor, 0.85f);
                DrawText(item.Description, new Vector2(rowRect.X + 16, rowRect.Y + 42), Color.White);

                var btnRect = GetRestItemRowButtonRect(row);
                DrawRect(btnRect, hoveredRow ? Color.Gold : new Color(50, 90, 150));
                DrawRectBorder(btnRect, Color.White, 2);
                DrawTextCentered("Equip", new Vector2(btnRect.Center.X, btnRect.Center.Y), Color.White, 0.8f);
            }

            var unequipRect = GetRestUnequipButtonRect();
            bool hasItem = selected.EquippedItem != null;
            DrawRect(unequipRect, !hasItem ? new Color(60, 60, 60) : (_hoveredRestUnequip ? Color.Gold : new Color(150, 50, 50)));
            DrawRectBorder(unequipRect, Color.White, 2);
            DrawTextCentered(hasItem ? "Unequip" : "No Item Equipped", new Vector2(unequipRect.Center.X, unequipRect.Center.Y), Color.White, 0.9f);

            var closeRect = GetRestPanelCloseButtonRect();
            DrawRect(closeRect, _hoveredRestPanelClose ? Color.Gold : new Color(150, 50, 50));
            DrawRectBorder(closeRect, Color.White, 2);
            DrawTextCentered("Close", new Vector2(closeRect.Center.X, closeRect.Center.Y), Color.White, 0.85f);
        }

        private Rectangle GetMapButtonRect(int i) => new Rectangle(((ScreenWidth - (2 * 560 + 80)) / 2) + i * (560 + 80), (ScreenHeight - 280) / 2 + 20, 560, 280);

        private void DrawMap()
        {
            DrawTextCentered("Choose Your Path", new Vector2(ScreenWidth / 2f, 140), Color.Gold, 2.6f);
            for (int i = 0; i < 2; i++)
            {
                var rect = GetMapButtonRect(i);
                bool hovered = _hoveredMapChoice == i;
                DrawRect(rect, hovered ? new Color(70, 70, 110) : new Color(30, 30, 45));
                DrawRectBorder(rect, hovered ? Color.Gold : Color.White, hovered ? 4 : 2);
                DrawTextCentered(i == 0 ? "Normal Stage" : "Hard Stage", new Vector2(rect.Center.X, rect.Y + 60), Color.Gold, 1.6f);
                DrawTextCentered(i == 0 ? "Reward: +5 Points" : "Enemy HP / ATK +150%\nReward: +10 Points", new Vector2(rect.Center.X, rect.Center.Y + 30), Color.White, 1f);
            }
        }

        private const int AvatarSize = 110;
        private const int RowSpacing = 165;
        private const int FormationTop = 330;
        private const int FrontRowCount = 3;
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
            int y = isFront ? FormationTop + rowSlot * RowSpacing : FormationTop + RowSpacing / 2 + rowSlot * RowSpacing;
            int x = unit.IsEnemy ? (isFront ? EnemyFrontX : EnemyBackX) : (isFront ? AllyFrontX : AllyBackX);
            return new Rectangle(x, y, AvatarSize, AvatarSize);
        }

        private Rectangle GetAvatarDisplayRect(Character unit)
        {
            var home = GetAvatarHomeRect(unit);

            
            if (unit.Name == "Elma" && _elmaIsAnimatingAttack && _elmaStoredTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_elmaStoredTarget);
                int offsetX = unit.IsEnemy ? 110 : -110;
                return new Rectangle(targetRect.X + offsetX, targetRect.Y, targetRect.Width, targetRect.Height);
            }
            else if (_battle.ActiveAttacker == unit && _battle.ActiveTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_battle.ActiveTarget);
                int offsetX = unit.IsEnemy ? 110 : -110;
                return new Rectangle(targetRect.X + offsetX, targetRect.Y, targetRect.Width, targetRect.Height);
            }

            return home;
        }

        private void DrawBattleBackground() { if (_battleBackground != null) _spriteBatch.Draw(_battleBackground, new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White); }

        private void DrawTeam(List<Character> team, bool isEnemyRow)
        {
            foreach (var unit in team)
            {
                bool isActive = _battle.ActiveAttacker == unit || _battle.ActiveTarget == unit;
                bool isHoveredTarget = _hoveredEnemyTarget == unit;
                Color themeColor = isEnemyRow ? new Color(180, 60, 60) : new Color(60, 120, 200);

                var avatarRect = GetAvatarDisplayRect(unit);

                if (unit.Name == "Elma")
                {
                    float baseScale = ((float)avatarRect.Height / 320f) * 1.8f;

                    if (unit.Name == "Elma" && _elmaIsAnimatingAttack)
                    {
                        if (_elmaAttackTex != null)
                        {
                            int frameWidth = 320;
                            int frameHeight = 320;
                            int totalFrames = Math.Max(1, _elmaAttackTex.Width / frameWidth);
                            int currentFrame = Math.Min(_elmaCustomFrameIndex, totalFrames - 1);
                            Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                            float scaleX = baseScale;
                            float scaleY = baseScale;

                            float drawW = frameWidth * scaleX;
                            float drawH = frameHeight * scaleY;
                            Vector2 drawPos = new Vector2(
                                avatarRect.X + (avatarRect.Width - drawW) / 2f,
                                avatarRect.Bottom - drawH
                            );

                            _spriteBatch.Draw(_elmaAttackTex, drawPos, sourceRect, Color.White, 0f, Vector2.Zero, new Vector2(scaleX, scaleY), SpriteEffects.None, 0f);
                        }
                        else
                        {
                            DrawRect(avatarRect, themeColor);
                            DrawRectBorder(avatarRect, Color.White, 3);
                        }
                    }
                    else if (_elmaIdleTex != null)
                    {
                        int frameWidth = 160;
                        int frameHeight = 320;
                        int totalFrames = Math.Max(1, _elmaIdleTex.Width / frameWidth);
                        int currentFrame = _elmaFrameIndex % totalFrames;
                        Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                        float scale = baseScale;
                        float drawW = frameWidth * scale;
                        float drawH = frameHeight * scale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );

                        _spriteBatch.Draw(_elmaIdleTex, drawPos, sourceRect, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                    }
                    else
                    {
                        DrawRect(avatarRect, themeColor);
                        DrawRectBorder(avatarRect, Color.White, 3);
                    }
                }
                else
                {
                    DrawRect(avatarRect, !unit.IsAlive ? new Color(50, 50, 50) : themeColor);
                    DrawRectBorder(avatarRect, !unit.IsAlive ? Color.DarkGray : isHoveredTarget ? Color.Orange : isActive ? Color.Gold : themeColor, isHoveredTarget ? 4 : 3);
                }

                var home = GetAvatarHomeRect(unit);
                float hpBarY;
                if (unit.Name == "Elma")
                {
                    float baseScale = ((float)home.Height / 320f) * 1.8f;
                    float drawH = 320f * baseScale;
                    float spriteTop = home.Bottom - drawH;
                    hpBarY = spriteTop - HpBarGap - HpBarHeight;
                }
                else
                {
                    hpBarY = home.Y - HpBarGap - HpBarHeight;
                }

                var hpBg = new Rectangle(home.X, (int)hpBarY, home.Width, HpBarHeight);
                DrawRect(hpBg, new Color(60, 20, 20));

                float hpRatio = unit.MaxHp > 0 ? (float)unit.Hp / unit.MaxHp : 0f;
                DrawRect(new Rectangle(hpBg.X, hpBg.Y, (int)(hpBg.Width * hpRatio), hpBg.Height), hpRatio > 0.5f ? Color.LimeGreen : hpRatio > 0.2f ? Color.Orange : Color.Red);
                DrawRectBorder(hpBg, Color.Black, 1);
            }
        }

        private void DrawFloatingTexts()
        {
            foreach (var ft in _floatingTexts) DrawTextCentered(ft.Text, ft.Position, ft.Color * MathHelper.Clamp(ft.Timer / FloatingTextDuration, 0f, 1f), ft.Scale);
        }

        private void DrawTurnArrows()
        {
            if (_battle.PendingAttacker != null) DrawDownArrow(new Vector2(GetAvatarHomeRect(_battle.PendingAttacker).X + AvatarSize / 2f, GetAvatarHomeRect(_battle.PendingAttacker).Y - 15f), 16, Color.Yellow);
            if (_hoveredEnemyTarget != null) DrawDownArrow(new Vector2(GetAvatarHomeRect(_hoveredEnemyTarget).X + AvatarSize / 2f, GetAvatarHomeRect(_hoveredEnemyTarget).Y - 15f), 16, Color.Orange);
        }

        private void DrawDownArrow(Vector2 tip, float size, Color color)
        {
            DrawLine(tip + new Vector2(-size, -size), tip, color, 5);
            DrawLine(tip + new Vector2(size, -size), tip, color, 5);
            DrawLine(tip + new Vector2(0, -size * 2.2f), tip + new Vector2(0, -size), color, 5);
        }

        private Rectangle GetAutoToggleButtonRect() => new Rectangle(ScreenWidth - 220, 20, 180, 50);

        private void DrawAutoToggleButton()
        {
            var rect = GetAutoToggleButtonRect();
            bool isManual = _battle.ManualMode;
            DrawRect(rect, _hoveredAutoToggle ? Color.Gold : isManual ? new Color(50, 90, 150) : new Color(50, 120, 50));
            DrawRectBorder(rect, Color.White, 2);
            DrawTextCentered(isManual ? "Auto:Off" : "Auto:ON", new Vector2(rect.Center.X, rect.Center.Y), Color.White, 0.9f);
        }

        private void DrawResultBanner()
        {
            if (_battle.State == BattleState.Running) return;
            if (_battle.State == BattleState.AllyWin && _isStoryMode)
            {
                var rect = new Rectangle(ScreenWidth / 2 - 340, ScreenHeight / 2 - 150, 680, 80);
                DrawRect(rect, new Color(0, 0, 0, 210));
                DrawRectBorder(rect, Color.Gold, 3);
                DrawTextCentered($"Stage 1-{_storyStageIndex + 1} Cleared!", new Vector2(rect.Center.X, rect.Center.Y), Color.Gold, 2f);

                var btnContinue = GetStoryWinContinueRect();
                DrawRect(btnContinue, _hoveredStoryWinContinue ? Color.Gold : new Color(50, 120, 50));
                DrawRectBorder(btnContinue, Color.White, 2);
                DrawTextCentered("Continue", new Vector2(btnContinue.Center.X, btnContinue.Center.Y), Color.White, 1.1f);

                var btnMenu = GetStoryWinMenuRect();
                DrawRect(btnMenu, _hoveredStoryWinMenu ? Color.Gold : new Color(150, 50, 50));
                DrawRectBorder(btnMenu, Color.White, 2);
                DrawTextCentered("Main Menu", new Vector2(btnMenu.Center.X, btnMenu.Center.Y), Color.White, 1.1f);
            }
            else if (_battle.State == BattleState.EnemyWin)
            {
                var rect = new Rectangle(ScreenWidth / 2 - 340, ScreenHeight / 2 - 150, 680, 80);
                DrawRect(rect, new Color(0, 0, 0, 210));
                DrawRectBorder(rect, Color.Red, 3);
                DrawTextCentered("You Lose!", new Vector2(rect.Center.X, rect.Center.Y), Color.Red, 2f);

                var btnRestart = GetLoseRestartRect();
                DrawRect(btnRestart, _hoveredLoseRestart ? Color.Gold : new Color(50, 120, 50));
                DrawRectBorder(btnRestart, Color.White, 2);
                DrawTextCentered("Restart", new Vector2(btnRestart.Center.X, btnRestart.Center.Y), Color.White, 1.1f);

                var btnMenu = GetLoseMenuRect();
                DrawRect(btnMenu, _hoveredLoseMenu ? Color.Gold : new Color(150, 50, 50));
                DrawRectBorder(btnMenu, Color.White, 2);
                DrawTextCentered("Main Menu", new Vector2(btnMenu.Center.X, btnMenu.Center.Y), Color.White, 1.1f);
            }
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
    }
}