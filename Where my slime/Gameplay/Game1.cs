using Where_My_Slime.Battle;
using Where_My_Slime.Manager;
using Where_My_Slime.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;

namespace Where_My_Slime
{
    internal class FloatingText
    {
        public Vector2 Position;
        public string Text;
        public float Timer;
        public Color Color;
        public float Scale;
    }

    public enum AppState
    {
        Logo,
        Intro,
        MainMenu,
        ModeSelect,
        Settings,
        Credits,
        TeamSetup,
        Battle,
        Pause,
        Upgrade,
        ItemSelect,
        Shop,
        Rest,
        Map,
        StoryModeSelect
    }

    // Game1 เหลือหน้าที่แค่ "คนกลาง" (orchestrator): โหลดรีซอร์สที่ใช้ร่วมกัน,
    // ประกอบหน้าจอ (Screen) แต่ละคลาสเข้าด้วยกันผ่าน callback, แล้ว Update/Draw
    // หน้าจอที่กำลัง active อยู่ตาม _appState — ตรรกะจริงของแต่ละหน้าอยู่ในคลาส *Screen.cs แยกไฟล์
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Texture2D _pixel;
        private Texture2D _battleBackground;
        private Texture2D _introBackground;
        private Texture2D _logoBackground;
        private SpriteFont _font;
        private SpriteFont _menuFont;
        private readonly Random _rng = new Random();

        private Texture2D _aliceIdleTex;
        private Texture2D _aliceAttackTex;
        private Texture2D _elmaIdleTex;
        private Texture2D _elmaAttackTex;
        private Texture2D _elmaSkill01Tex;
        private Texture2D _elmaSkill02Tex;
        private Texture2D _lumiIdleTex;
        private Texture2D _lumiAttackTex;
        private Texture2D _lumiSkillBoomTex;
        private Texture2D _boomFxTex;
        private Texture2D _titleTex;
        private Texture2D _testCharacterTex;
        private Texture2D _deadEffectTex;
        private Texture2D _testEnemyTex;
        private Texture2D _orcWarriorTex;
        private Texture2D _goblinArcherTex;
        private Texture2D _darkMageTex;
        private Texture2D _ghostHealerTex;
        private Texture2D _stoneGiantTex;
        private Texture2D _beeIdleTex;
        private Texture2D _beeAttackTex;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        private AppState _appState = AppState.Logo;
        private AppState _settingsReturnState = AppState.MainMenu;
        private MouseState _previousMouseState;
        private KeyboardState _previousKeyboardState;

        private readonly GameSession _session = new GameSession();

        // --- หน้าจอต่างๆ (แต่ละหน้าแยกเป็นคลาสของตัวเอง เหมือน BattleScreen) ---
        private BattleScreen _battleScreen;
        private LogoScreen _logoScreen;
        private IntroScreen _introScreen;
        private MainMenuScreen _mainMenuScreen;
        private ModeSelectScreen _modeSelectScreen;
        private StoryModeSelectScreen _storyModeSelectScreen;
        private TeamSetupScreen _teamSetupScreen;
        private SettingsScreen _settingsScreen;
        private PauseScreen _pauseScreen;
        private UpgradeScreen _upgradeScreen;
        private ItemSelectScreen _itemSelectScreen;
        private ShopScreen _shopScreen;
        private RestScreen _restScreen;
        private MapScreen _mapScreen;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = ScreenWidth;
            _graphics.PreferredBackBufferHeight = ScreenHeight;
            _graphics.IsFullScreen = false;
            _graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            Window.AllowUserResizing = true;
            Window.IsBorderless = false;
            base.Initialize();
        }

        private void PlayUIClick()
        {
            AudioManager.PlaySfx("Audio/UI_Click");
        }

        private void EnterMapScreen()
        {
            _appState = AppState.Map;
            _session.IsShopOnMap = _rng.NextDouble() < 0.5;
        }

        private void StartNewRunForChallenge()
        {
            _session.IsStoryMode = false;
            _session.StoryStageIndex = 0;
            _session.TeamSetupOrigin = TeamSetupOrigin.ChallengeStart;
            _session.CurrentStageIsHard = false;
            _session.IsNextBattleBoss = false;
            _session.Coins = 0;
            _session.Inventory.Clear();
            _upgradeScreen.EnterFresh();

            _session.AllHeroes = BattleManager.CreateStartingParty();
            Array.Clear(_session.FormationSlots, 0, _session.FormationSlots.Length);

            for (int i = 0; i < Math.Min(_session.AllHeroes.Count, 5); i++)
            {
                _session.FormationSlots[i] = _session.AllHeroes[i];
            }

            _appState = AppState.TeamSetup;
        }

        private void StartNewBattleForParty(bool isHard)
        {
            _session.Party = new List<Character>();
            foreach (var c in _session.FormationSlots)
            {
                if (c != null) _session.Party.Add(c);
            }
            if (_session.Party.Count == 0 && _session.AllHeroes.Count > 0)
            {
                _session.Party.Add(_session.AllHeroes[0]);
            }

            _session.CurrentStageIsHard = isHard;

            bool isBoss = !_session.IsStoryMode && _session.IsNextBattleBoss;
            _battleScreen.StartNewBattle(_session.Party, _session.FormationSlots, isHard, _session.IsStoryMode, _session.StoryStageIndex, ref _session.Coins, _session.Inventory, _session.ManualModePreference, _session.CameraShakeEnabled, isBossStage: isBoss);
            _appState = AppState.Battle;

            AudioManager.PlayBgm("Audio/battle_theme");
        }

        private void GoBackFromTeamSetup()
        {
            if (_session.IsStoryMode) _appState = AppState.StoryModeSelect;
            else if (_session.TeamSetupOrigin == TeamSetupOrigin.ChallengeStart) _appState = AppState.ModeSelect;
            else _appState = AppState.Map;
        }

        private void StartBattleFromTeamSetup()
        {
            StartNewBattleForParty(_session.TeamSetupOrigin == TeamSetupOrigin.ChallengeMap && _session.CurrentStageIsHard);
        }

        private Texture2D LoadTextureHelper(string folderName, string fileName)
        {
            try
            {
                string current = AppDomain.CurrentDomain.BaseDirectory;
                string foundPath = null;

                for (int i = 0; i < 5; i++)
                {
                    string p1 = Path.Combine(current, folderName, fileName + ".png");
                    string p2 = Path.Combine(current, "Content", folderName, fileName + ".png");
                    string p3 = Path.Combine(current, fileName + ".png");

                    if (File.Exists(p1)) { foundPath = p1; break; }
                    if (File.Exists(p2)) { foundPath = p2; break; }
                    if (File.Exists(p3)) { foundPath = p3; break; }

                    var parent = Directory.GetParent(current);
                    if (parent == null) break;
                    current = parent.FullName;
                }

                if (foundPath != null)
                {
                    using (var stream = File.OpenRead(foundPath))
                    {
                        return Texture2D.FromStream(GraphicsDevice, stream);
                    }
                }
                else
                {
                    return Content.Load<Texture2D>($"{folderName}/{fileName}");
                }
            }
            catch
            {
                try { return Content.Load<Texture2D>(fileName); } catch { return null; }
            }
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            AudioManager.LoadAll(
                Content,
                new string[] { "Audio/UI_Click", "Audio/Deadsound" },
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

            // ฟอนต์เฉพาะหน้าเมนูหลัก (Quicksand) — ถ้าหาไฟล์ไม่เจอจะ fallback ไปใช้ฟอนต์หลักแทน
            string[] menuFontCandidates = { "Quicksand-VariableFont_wght", "Fonts/Quicksand-VariableFont_wght" };
            foreach (var name in menuFontCandidates)
            {
                try
                {
                    _menuFont = Content.Load<SpriteFont>(name);
                    break;
                }
                catch
                {
                    _menuFont = null;
                }
            }
            if (_menuFont == null) _menuFont = _font;

            try { _battleBackground = Content.Load<Texture2D>("bg_Battle_01"); } catch { _battleBackground = null; }
            try { _introBackground = Content.Load<Texture2D>("Intro_BG_Test001"); } catch { _introBackground = null; }
            try { _logoBackground = Content.Load<Texture2D>("LogoTest1"); } catch { _logoBackground = null; }

            try { _elmaIdleTex = Content.Load<Texture2D>("char_Elma_Idle"); } catch { _elmaIdleTex = null; }
            try { _elmaAttackTex = Content.Load<Texture2D>("char_Elma_Attack"); } catch { _elmaAttackTex = null; }

            _elmaSkill01Tex = LoadTextureHelper("Charater", "char_Elma_Skill01");
            _elmaSkill02Tex = LoadTextureHelper("Charater", "char_Elma_Skill02");

            _aliceIdleTex = LoadTextureHelper("Charater", "char_Alice_Idle");
            _aliceAttackTex = LoadTextureHelper("Charater", "char_Alice_Attack");

            _lumiIdleTex = LoadTextureHelper("Charater", "Char_Lumi_IDLE");
            _lumiAttackTex = LoadTextureHelper("Charater", "Char_Lumi_ATTACK");

            _lumiSkillBoomTex = LoadTextureHelper("Bloom_Effect", "skill_lumi_Boom");
            _boomFxTex = LoadTextureHelper("Bloom_Effect", "Boom_Fx");

            _deadEffectTex = LoadTextureHelper("Effect", "Dead_Effect");

            // รูปไตเติ้ล "Where my Slime" ที่ใช้แทนตัวหนังสือบนหน้าเมนูหลัก
            // ถ้าไฟล์อยู่คนละโฟลเดอร์ ให้บอกชื่อโฟลเดอร์มาเพิ่มใน LoadTextureHelper ตรงนี้ได้เลย
            _titleTex = LoadTextureHelper("", "Tilte_0.01");

            try
            {
                string current = AppDomain.CurrentDomain.BaseDirectory;
                string foundPath = null;

                for (int i = 0; i < 5; i++)
                {
                    string p1 = Path.Combine(current, "Charater", "Test_Charater.png");
                    string p2 = Path.Combine(current, "Content", "Charater", "Test_Charater.png");
                    string p3 = Path.Combine(current, "Test_Charater.png");
                    string p4 = Path.Combine(current, "Character", "Test_Charater.png");

                    if (File.Exists(p1)) { foundPath = p1; break; }
                    if (File.Exists(p2)) { foundPath = p2; break; }
                    if (File.Exists(p3)) { foundPath = p3; break; }
                    if (File.Exists(p4)) { foundPath = p4; break; }

                    var parent = Directory.GetParent(current);
                    if (parent == null) break;
                    current = parent.FullName;
                }

                if (foundPath != null)
                {
                    using (var stream = File.OpenRead(foundPath))
                    {
                        _testCharacterTex = Texture2D.FromStream(GraphicsDevice, stream);
                    }
                }
                else
                {
                    _testCharacterTex = Content.Load<Texture2D>("Test_Charater");
                }
            }
            catch
            {
                _testCharacterTex = null;
            }

            _testEnemyTex = LoadTextureHelper("Enemy", "Test_Enemy");
            _orcWarriorTex = LoadTextureHelper("Enemy", "orc_warrior");
            _goblinArcherTex = LoadTextureHelper("Enemy", "goblin_archer");
            _darkMageTex = LoadTextureHelper("Enemy", "dark_mage");
            _ghostHealerTex = LoadTextureHelper("Enemy", "ghost_healer");
            _stoneGiantTex = LoadTextureHelper("Enemy", "stone_giant");

            _beeIdleTex = LoadTextureHelper("Enemy", "enemyMySlive1_Bee_idle") ?? LoadTextureHelper("Enemy", "enemyMySlime1_Bee_idle");
            _beeAttackTex = LoadTextureHelper("Enemy", "enemyMySlive1_Bee_attack") ?? LoadTextureHelper("Enemy", "enemyMySlime1_Bee_attack");

            var texDict = new Dictionary<string, Texture2D>
            {
                { "ElmaIdle", _elmaIdleTex },
                { "ElmaAttack", _elmaAttackTex },
                { "char_Elma_Skill01", _elmaSkill01Tex },
                { "char_Elma_Skill02", _elmaSkill02Tex },
                { "Alice", _aliceIdleTex },
                { "Swordman", _aliceIdleTex },

                { "char_Alice_Idle", _aliceIdleTex },
                { "char_Alice_Attack", _aliceAttackTex },

                { "Char_Lumi_IDLE", _lumiIdleTex },
                { "Char_Lumi_ATTACK", _lumiAttackTex },
                { "skill_lumi_Boom", _lumiSkillBoomTex },
                { "Boom_Fx", _boomFxTex },
                { "Bloom_Effect/Boom_Fx", _boomFxTex },

                { "Giant_Hornet", _beeIdleTex },
                { "enemyMySlime1_Bee", _beeIdleTex },
                { "enemyMySlime1_Bee_idle", _beeIdleTex },
                { "enemyMySlime1_Bee_attack", _beeAttackTex },
                { "GiantHornet", _beeIdleTex },
                { "Bee", _beeIdleTex },

                { "TestChar", _testCharacterTex },
                { "DeadEffect", _deadEffectTex },
                { "TestEnemy", _testEnemyTex },
                { "Orc", _orcWarriorTex },
                { "Goblin", _goblinArcherTex },
                { "DarkMage", _darkMageTex },
                { "Ghost", _ghostHealerTex },
                { "StoneGiant", _stoneGiantTex }
            };

            _battleScreen = new BattleScreen(
                GraphicsDevice,
                _spriteBatch,
                _font,
                _pixel,
                _battleBackground,
                texDict,
                _rng,
                state => _appState = state,
                () => {
                    _settingsReturnState = AppState.Battle;
                    _appState = AppState.Settings;
                },
                PlayUIClick,
                () => { _upgradeScreen.EnterFresh(); _appState = AppState.Upgrade; },
                StartNewRunForChallenge,
                () => { _appState = AppState.Pause; }
            );

            _logoScreen = new LogoScreen(
                _spriteBatch, _font, _pixel, _logoBackground,
                goToIntro: () => { _appState = AppState.MainMenu; }); // ตัดหน้า Intro ออก ข้ามจาก Logo ไป MainMenu โดยตรง

            _introScreen = new IntroScreen(
                _spriteBatch, _font, _pixel, _introBackground,
                goToMainMenu: () => _appState = AppState.MainMenu);

            _mainMenuScreen = new MainMenuScreen(
                _spriteBatch, _menuFont, _pixel, _titleTex,
                goToModeSelect: () => _appState = AppState.ModeSelect,
                openSettings: () => { _settingsReturnState = AppState.MainMenu; _appState = AppState.Settings; },
                goToCredits: () => _appState = AppState.Credits,
                exitGame: () => Exit(),
                playUIClick: PlayUIClick);

            _modeSelectScreen = new ModeSelectScreen(
                _spriteBatch, _font, _pixel,
                goToMainMenu: () => _appState = AppState.MainMenu,
                goToStoryModeSelect: () => _appState = AppState.StoryModeSelect,
                startNewRunForChallenge: StartNewRunForChallenge,
                playUIClick: PlayUIClick);

            _storyModeSelectScreen = new StoryModeSelectScreen(
                _spriteBatch, _font, _pixel, _session,
                goToModeSelect: () => _appState = AppState.ModeSelect,
                goToTeamSetup: () => _appState = AppState.TeamSetup,
                playUIClick: PlayUIClick);

            _teamSetupScreen = new TeamSetupScreen(
                _spriteBatch, _font, _pixel, _session,
                goBack: GoBackFromTeamSetup,
                startBattle: StartBattleFromTeamSetup,
                playUIClick: PlayUIClick);

            _settingsScreen = new SettingsScreen(
                _spriteBatch, _font, _pixel, _session,
                close: () => _appState = _settingsReturnState,
                onToggleCameraShake: enabled => _battleScreen.SetCameraShakeEnabled(enabled),
                playUIClick: PlayUIClick);

            _pauseScreen = new PauseScreen(
                _spriteBatch, _font, _pixel,
                goToMainMenu: () => _appState = AppState.MainMenu,
                restart: () =>
                {
                    if (_session.IsStoryMode)
                    {
                        StartNewBattleForParty(_session.CurrentStageIsHard);
                        _appState = AppState.Battle;
                    }
                    else
                    {
                        StartNewRunForChallenge();
                    }
                },
                continueBattle: () => _appState = AppState.Battle,
                playUIClick: PlayUIClick);

            _upgradeScreen = new UpgradeScreen(
                _spriteBatch, _font, _pixel, _session,
                _elmaIdleTex, _lumiIdleTex, _aliceIdleTex, _testCharacterTex,
                onDone: EnterMapScreen,
                playUIClick: PlayUIClick);

            _itemSelectScreen = new ItemSelectScreen(
                _spriteBatch, _font, _pixel, _session, _rng,
                onDone: () => { _upgradeScreen.EnterFresh(); _appState = AppState.Upgrade; },
                playUIClick: PlayUIClick);

            _shopScreen = new ShopScreen(
                _spriteBatch, _font, _pixel, _session,
                onDone: () => { _upgradeScreen.EnterFresh(); _appState = AppState.Upgrade; },
                playUIClick: PlayUIClick);

            _restScreen = new RestScreen(
                _spriteBatch, _font, _pixel, _session,
                continueToMap: EnterMapScreen,
                playUIClick: PlayUIClick);

            _mapScreen = new MapScreen(
                _spriteBatch, _font, _pixel, _session,
                chooseStage: isHard =>
                {
                    _session.CurrentStageIsHard = isHard;
                    _session.IsNextBattleBoss = false;
                    _session.TeamSetupOrigin = TeamSetupOrigin.ChallengeMap;
                    _session.StoryStageIndex++;

                    Array.Clear(_session.FormationSlots, 0, _session.FormationSlots.Length);
                    for (int i = 0; i < Math.Min(_session.Party.Count, 5); i++) _session.FormationSlots[i] = _session.Party[i];
                    _appState = AppState.TeamSetup;
                },
                chooseShop: scrollVal =>
                {
                    _shopScreen.EnterFresh(_rng, scrollVal);
                    _appState = AppState.Shop;
                },
                chooseBoss: () =>
                {
                    _session.CurrentStageIsHard = false;
                    _session.IsNextBattleBoss = true;
                    _session.TeamSetupOrigin = TeamSetupOrigin.ChallengeMap;
                    _session.StoryStageIndex++;

                    Array.Clear(_session.FormationSlots, 0, _session.FormationSlots.Length);
                    for (int i = 0; i < Math.Min(_session.Party.Count, 5); i++) _session.FormationSlots[i] = _session.Party[i];
                    _appState = AppState.TeamSetup;
                },
                playUIClick: PlayUIClick);
        }

        protected override void Update(GameTime gameTime)
        {
            var mouse = Mouse.GetState();
            var keyboard = Keyboard.GetState();
            bool leftClicked = mouse.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released;

            switch (_appState)
            {
                case AppState.Logo:
                    _logoScreen.Update(gameTime, keyboard, _previousKeyboardState, leftClicked);
                    break;
                case AppState.Intro:
                    _introScreen.Update(gameTime, keyboard, _previousKeyboardState, leftClicked);
                    break;
                case AppState.MainMenu:
                    _mainMenuScreen.Update(mouse, leftClicked, keyboard);
                    break;
                case AppState.ModeSelect:
                    _modeSelectScreen.Update(mouse, leftClicked, keyboard);
                    break;
                case AppState.StoryModeSelect:
                    _storyModeSelectScreen.Update(mouse, leftClicked, keyboard);
                    break;
                case AppState.TeamSetup:
                    _teamSetupScreen.Update(gameTime, mouse, leftClicked, keyboard);
                    break;
                case AppState.Settings:
                    _settingsScreen.Update(mouse, leftClicked, keyboard);
                    break;
                case AppState.Credits:
                    if (leftClicked || keyboard.IsKeyDown(Keys.Escape))
                    {
                        if (leftClicked) PlayUIClick();
                        _appState = AppState.MainMenu;
                    }
                    break;
                case AppState.Battle:
                    _battleScreen.Update(gameTime, keyboard, mouse, leftClicked, ref _session.ManualModePreference, ref _session.Coins, _session.Inventory);
                    break;
                case AppState.Pause:
                    _pauseScreen.Update(mouse, leftClicked, keyboard);
                    break;
                case AppState.Upgrade:
                    _upgradeScreen.Update(mouse, leftClicked, keyboard, _previousKeyboardState);
                    break;
                case AppState.ItemSelect:
                    _itemSelectScreen.Update(mouse, leftClicked);
                    break;
                case AppState.Shop:
                    _shopScreen.Update(mouse, leftClicked);
                    break;
                case AppState.Rest:
                    _restScreen.Update(mouse, leftClicked);
                    break;
                case AppState.Map:
                    _mapScreen.Update(mouse, leftClicked);
                    break;
            }

            _previousMouseState = mouse;
            _previousKeyboardState = keyboard;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            Color clearColor = (_appState == AppState.Logo || _appState == AppState.Intro) ? Color.Black : new Color(30, 30, 45);
            GraphicsDevice.Clear(clearColor);

            _spriteBatch.Begin();

            switch (_appState)
            {
                case AppState.Logo: _logoScreen.Draw(); break;
                case AppState.Intro: _introScreen.Draw(); break;
                case AppState.MainMenu: _mainMenuScreen.Draw(); break;
                case AppState.ModeSelect: _modeSelectScreen.Draw(); break;
                case AppState.StoryModeSelect: _storyModeSelectScreen.Draw(); break;
                case AppState.TeamSetup: _teamSetupScreen.Draw(); break;
                case AppState.Settings:
                    if (_settingsReturnState == AppState.Battle)
                    {
                        _spriteBatch.End();
                        _spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(_battleScreen.GetShakeOffset().X, _battleScreen.GetShakeOffset().Y, 0f));
                        _battleScreen.Draw();
                        _spriteBatch.End();
                        _spriteBatch.Begin();
                    }
                    else
                    {
                        _mainMenuScreen.Draw();
                    }
                    _settingsScreen.Draw();
                    break;
                case AppState.Credits: DrawPlaceholderScreen("Credit", "Made with MonoGame - click anywhere or press Esc to go back"); break;
                case AppState.Battle:
                    _spriteBatch.End();
                    _spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(_battleScreen.GetShakeOffset().X, _battleScreen.GetShakeOffset().Y, 0f));
                    _battleScreen.Draw();
                    _spriteBatch.End();
                    _spriteBatch.Begin();
                    break;
                case AppState.Pause:
                    _spriteBatch.End();
                    _spriteBatch.Begin(transformMatrix: Matrix.CreateTranslation(_battleScreen.GetShakeOffset().X, _battleScreen.GetShakeOffset().Y, 0f));
                    _battleScreen.Draw();
                    _spriteBatch.End();
                    _spriteBatch.Begin();
                    _pauseScreen.Draw();
                    break;
                case AppState.Upgrade: _upgradeScreen.Draw(); break;
                case AppState.ItemSelect: _itemSelectScreen.Draw(); break;
                case AppState.Shop: _shopScreen.Draw(); break;
                case AppState.Rest: _restScreen.Draw(); break;
                case AppState.Map: _mapScreen.Draw(); break;
            }

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        private void DrawPlaceholderScreen(string title, string subtitle)
        {
            DrawTextCentered(title, new Vector2(ScreenWidth / 2f, ScreenHeight / 2f - 40), Color.Gold, 2.4f);
            DrawTextCentered(subtitle, new Vector2(ScreenWidth / 2f, ScreenHeight / 2f + 30), Color.White, 1f);
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