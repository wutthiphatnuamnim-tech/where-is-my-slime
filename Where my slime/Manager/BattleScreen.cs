using Where_My_Slime.Battle;
using Where_My_Slime.Data;
using Where_My_Slime.Manager;
using Where_My_Slime.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Where_My_Slime
{
    public class BattleScreen
    {
        private GraphicsDevice _graphicsDevice;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private Texture2D _pixel;
        private Texture2D _battleBackground;

        private Texture2D _elmaIdleTex;
        private Texture2D _elmaAttackTex;
        private Texture2D _elmaSkill01Tex;
        private Texture2D _elmaSkill02Tex;

        private Texture2D _lumiIdleTex;
        private Texture2D _lumiAttackTex;
        private Texture2D _lumiSkillBoomTex;
        private Texture2D _boomFxTex; //[cite: 7]

        // --- Texture ของ Alice / Swordman ---
        private Texture2D _aliceIdleTex;
        private Texture2D _aliceAttackTex;

        private Texture2D _testCharacterTex;
        private Texture2D _deadEffectTex;

        private Texture2D _testEnemyTex;
        private Texture2D _orcWarriorTex;
        private Texture2D _giantHornetIdleTex;
        private Texture2D _giantHornetAttackTex;
        private Texture2D _darkMageTex;
        private Texture2D _ghostHealerTex;
        private Texture2D _stoneGiantTex;

        private BattleManager _battle;
        private List<Character> _party;
        private Character[] _formationSlots;
        private List<Item> _inventory;
        private List<Item> _currentItemChoices;
        private int _coins;
        private bool _isStoryMode;
        private int _storyStageIndex;
        private bool _currentStageIsHard;
        private bool _currentStageIsBoss;
        private bool _cameraShakeEnabled;
        private readonly Random _rng;

        private List<Character> _turnQueue = new List<Character>();
        private Character _lastTurnUnit = null;

        private float _elmaAnimTimer;
        private int _elmaFrameIndex;
        private bool _elmaIsAnimatingAttack;
        private float _elmaAttackHoldTimer;
        private Character _elmaStoredTarget;
        private int _elmaCustomFrameIndex;
        private float _elmaCustomAnimTimer;
        private bool _elmaHasAnimatedThisTurn;

        private float _lumiAnimTimer;
        private int _lumiFrameIndex;
        private bool _lumiIsAnimatingAttack;
        private float _lumiAttackHoldTimer;
        private Character _lumiStoredTarget;
        private int _lumiCustomFrameIndex;
        private float _lumiCustomAnimTimer;
        private bool _lumiHasAnimatedThisTurn;
        private Character _lumiCasterRef;

        // --- ตัวแปรควบคุมอนิเมชันสกิล Lumi (โปรเจกต์ไทล์พุ่งไปหาเป้าหมาย + ระเบิดใส่ศัตรูทุกตัว) ---
        private bool _lumiIsUsingUltimateAnim = false;
        private bool _lumiIsProjectileMoving = false;
        private Vector2 _lumiProjPos;
        private Vector2 _lumiProjTargetPos;
        private float _lumiProjSpeed = 1200f;
        private float _lumiUltimateTimer = 0f;
        private bool _lumiHasDealtDamage = false;
        private Character _lumiUltimateTarget;

        // --- ตัวแปรจัดการอนิเมชันของ Alice / Swordman ---
        private float _aliceAnimTimer;
        private int _aliceFrameIndex;
        private bool _aliceIsAnimatingAttack;
        private float _aliceAttackHoldTimer;
        private Character _aliceStoredTarget;
        private int _aliceCustomFrameIndex;
        private float _aliceCustomAnimTimer;
        private bool _aliceHasAnimatedThisTurn;
        private Character _aliceCasterRef;

        private float _hornetAnimTimer;
        private int _hornetFrameIndex;

        private bool _elmaIsUsingUltimateAnim = false;
        private float _elmaUltimateTimer = 0f;
        private bool _elmaHasDealtDamage = false;
        private Character _elmaUltimateTarget;
        private Character _elmaCasterRef;

        private float _shakeTimer;
        private float _shakeOffsetX;
        private float _battleEndDelayTimer = 0f;

        private bool _isWaitingForUltimateTarget = false;

        private readonly Dictionary<Character, float> _deathFadeAlphas = new Dictionary<Character, float>();
        private readonly Dictionary<Character, float> _deathTimers = new Dictionary<Character, float>();
        private readonly HashSet<Character> _playedDeathSound = new HashSet<Character>();

        private readonly List<FloatingText> _floatingTexts = new List<FloatingText>();
        private const float FloatingTextDuration = 0.8f;
        private const float FloatingTextRiseSpeed = 45f;
        private const float DamageTextScale = 2.2f;
        private const float CritTextScale = 2.8f;
        private const float MissTextScale = 1.8f;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        private Character _hoveredEnemyTarget;
        private bool _hoveredPauseButton;
        private bool _hoveredAutoToggle;
        private bool _hoveredBattleSettings;
        private int _hoveredItemCard = -1;
        private bool _hoveredItemSkip;
        private bool _hoveredStoryWinContinue;
        private bool _hoveredStoryWinMenu;
        private bool _hoveredLoseRestart;
        private bool _hoveredLoseMenu;

        private readonly Action<AppState> _changeState;
        private readonly Action _openSettings;
        private readonly Action _playUIClick;
        private readonly Action _goToUpgrade;
        private readonly Action _startNewRunForChallenge;
        private readonly Action _openPause;

        public BattleScreen(
            GraphicsDevice graphicsDevice,
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            Texture2D battleBg,
            Dictionary<string, Texture2D> textures,
            Random rng,
            Action<AppState> changeState,
            Action openSettings,
            Action playUIClick,
            Action goToUpgrade,
            Action startNewRunForChallenge,
            Action openPause)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _battleBackground = battleBg;
            _rng = rng;
            _changeState = changeState;
            _openSettings = openSettings;
            _playUIClick = playUIClick;
            _goToUpgrade = goToUpgrade;
            _startNewRunForChallenge = startNewRunForChallenge;
            _openPause = openPause;

            _elmaIdleTex = textures.GetValueOrDefault("ElmaIdle") ?? textures.GetValueOrDefault("elma_idle") ?? textures.GetValueOrDefault("char_elma_idle");
            _elmaAttackTex = textures.GetValueOrDefault("ElmaAttack") ?? textures.GetValueOrDefault("elma_attack") ?? textures.GetValueOrDefault("char_elma_attack");

            _elmaSkill01Tex = textures.GetValueOrDefault("ElmaSkill01") ?? textures.GetValueOrDefault("elma_skill01") ?? textures.GetValueOrDefault("Elma_Skill01") ??
                    textures.GetValueOrDefault("char_Elma_Skill01") ?? textures.GetValueOrDefault("char_elma_skill01") ??
                    textures.GetValueOrDefault("char_Elma_Skill01.png") ?? textures.GetValueOrDefault("char_elma_skill01.png");

            _elmaSkill02Tex = textures.GetValueOrDefault("ElmaSkill02") ?? textures.GetValueOrDefault("elma_skill02") ?? textures.GetValueOrDefault("Elma_Skill02") ??
                    textures.GetValueOrDefault("char_Elma_Skill02") ?? textures.GetValueOrDefault("char_elma_skill02") ??
                    textures.GetValueOrDefault("char_Elma_Skill02.png") ?? textures.GetValueOrDefault("char_elma_skill02.png");

            _lumiIdleTex = textures.GetValueOrDefault("Charater/Char_Lumi_IDLE") ?? textures.GetValueOrDefault("Char_Lumi_IDLE") ?? textures.GetValueOrDefault("CharLumiIdle") ?? textures.GetValueOrDefault("char_lumi_idle") ?? textures.GetValueOrDefault("LumiIdle");
            _lumiAttackTex = textures.GetValueOrDefault("Charater/Char_Lumi_ATTACK") ?? textures.GetValueOrDefault("Char_Lumi_ATTACK") ?? textures.GetValueOrDefault("CharLumiAttack") ?? textures.GetValueOrDefault("char_lumi_attack") ?? textures.GetValueOrDefault("LumiAttack");

            _lumiSkillBoomTex = textures.GetValueOrDefault("skill_lumi_Boom") ?? textures.GetValueOrDefault("Bloom_Effect/skill_lumi_Boom");

            _boomFxTex = textures.GetValueOrDefault("Bloom_Effect/Boom_Fx") ??
                      textures.GetValueOrDefault("Boom_Fx") ??
                      textures.GetValueOrDefault("Bloom_Effect/Boom_Fx.png") ??
                      textures.GetValueOrDefault("Boom_Fx.png");

            _aliceIdleTex = textures.GetValueOrDefault("Charater/char_Alice_Idle") ??
                            textures.GetValueOrDefault("Charater/Char_Alice_Idle") ??
                            textures.GetValueOrDefault("char_Alice_Idle") ??
                            textures.GetValueOrDefault("Alice") ??
                            textures.GetValueOrDefault("Swordman") ??
                            textures.GetValueOrDefault("alice_idle");

            _aliceAttackTex = textures.GetValueOrDefault("Charater/char_Alice_Attack") ??
                            textures.GetValueOrDefault("Charater/Char_Alice_Attack") ??
                            textures.GetValueOrDefault("char_Alice_Attack") ??
                            textures.GetValueOrDefault("AliceAttack") ??
                            textures.GetValueOrDefault("alice_attack");

            _testCharacterTex = textures.GetValueOrDefault("TestChar");
            _deadEffectTex = textures.GetValueOrDefault("DeadEffect") ?? textures.GetValueOrDefault("Effect/Dead_Effect");
            _testEnemyTex = textures.GetValueOrDefault("TestEnemy");
            _orcWarriorTex = textures.GetValueOrDefault("Orc");

            _giantHornetIdleTex = textures.GetValueOrDefault("Giant_Hornet") ?? textures.GetValueOrDefault("Enemy/enemyMySlime1_Bee_idle") ?? textures.GetValueOrDefault("enemyMySlime1_Bee_idle") ?? textures.GetValueOrDefault("enemyMySlime1_Bee_idle.png");
            _giantHornetAttackTex = textures.GetValueOrDefault("Enemy/enemyMySlime1_Bee_attack") ?? textures.GetValueOrDefault("enemyMySlime1_Bee_attack") ?? textures.GetValueOrDefault("enemyMySlime1_Bee_attack.png");

            _darkMageTex = textures.GetValueOrDefault("DarkMage");
            _ghostHealerTex = textures.GetValueOrDefault("Ghost");
            _stoneGiantTex = textures.GetValueOrDefault("StoneGiant");
        }

        public void StartNewBattle(List<Character> party, Character[] formationSlots, bool isHard, bool isStoryMode, int storyStageIndex, ref int coins, List<Item> inventory, bool manualModePreference, bool cameraShakeEnabled, bool isBossStage = false)
        {
            _party = party;
            _formationSlots = formationSlots;
            _isStoryMode = isStoryMode;
            _storyStageIndex = storyStageIndex;
            _currentStageIsHard = isHard;
            _currentStageIsBoss = isBossStage;
            _cameraShakeEnabled = cameraShakeEnabled;
            _isWaitingForUltimateTarget = false;

            _elmaIsUsingUltimateAnim = false;
            _elmaUltimateTarget = null;
            _elmaCasterRef = null;

            _lumiIsUsingUltimateAnim = false;
            _lumiIsProjectileMoving = false;
            _lumiUltimateTarget = null;
            _lumiCasterRef = null;

            _aliceCasterRef = null;

            if (_party != null)
            {
                foreach (var c in _party)
                {
                    if (c != null)
                    {
                        c.Hp = c.MaxHp;
                        c.InitializeSP();
                    }
                }
            }

            if (_battle != null)
            {
                _battle.OnAttackResolved -= HandleAttackResolved;
                _battle.OnHealResolved -= HandleHealResolved;
            }

            // สเกลความยากตามจำนวนชั้นที่ผ่านมาแล้ว ใช้เฉพาะ Challenge Mode เท่านั้น (Story Mode ไม่ต้องสเกล)
            int floorsClearedForScaling = isStoryMode ? 0 : storyStageIndex;
            _battle = new BattleManager(_party, isHard, floorsCleared: floorsClearedForScaling, isBossStage: isBossStage);
            _currentItemChoices = null;
            _deathFadeAlphas.Clear();
            _deathTimers.Clear();
            _playedDeathSound.Clear();
            _battleEndDelayTimer = 0f;
            ResetTurnQueue();

            if (_isStoryMode && _storyStageIndex == 0)
            {
                if (_battle.Enemies != null && _battle.Enemies.Count > 1)
                {
                    _battle.Enemies.RemoveRange(1, _battle.Enemies.Count - 1);
                }
            }

            _battle.SetManualMode(manualModePreference);
            _battle.OnAttackResolved += HandleAttackResolved;
            _battle.OnHealResolved += HandleHealResolved;
            _floatingTexts.Clear();

            AudioManager.PlayBgm("Audio/battle_theme");
        }

        private void ResetTurnQueue()
        {
            _turnQueue.Clear();
            if (_battle == null) return;
            if (_battle.Allies != null)
            {
                foreach (var a in _battle.Allies) if (a != null && a.IsAlive) _turnQueue.Add(a);
            }
            if (_battle.Enemies != null)
            {
                foreach (var e in _battle.Enemies) if (e != null && e.IsAlive) _turnQueue.Add(e);
            }
            _lastTurnUnit = null;
        }

        private void UpdateTurnQueue()
        {
            if (_battle == null) return;

            if (_turnQueue.Count == 0)
            {
                ResetTurnQueue();
            }

            _turnQueue.RemoveAll(c => c == null || !c.IsAlive);

            var allAlive = new List<Character>();
            if (_battle.Allies != null)
            {
                foreach (var a in _battle.Allies) if (a != null && a.IsAlive) allAlive.Add(a);
            }
            if (_battle.Enemies != null)
            {
                foreach (var e in _battle.Enemies) if (e != null && e.IsAlive) allAlive.Add(e);
            }

            foreach (var c in allAlive)
            {
                if (!_turnQueue.Contains(c))
                {
                    _turnQueue.Add(c);
                }
            }

            Character currentUnit = _battle.PendingAttacker ?? _battle.ActiveAttacker;

            if (currentUnit != null && currentUnit.IsAlive)
            {
                if (_lastTurnUnit != null && _lastTurnUnit != currentUnit && _lastTurnUnit.IsAlive)
                {
                    _turnQueue.Remove(_lastTurnUnit);
                    if (!_turnQueue.Contains(_lastTurnUnit))
                    {
                        _turnQueue.Add(_lastTurnUnit);
                    }
                }

                _lastTurnUnit = currentUnit;

                if (_turnQueue.Contains(currentUnit))
                {
                    _turnQueue.Remove(currentUnit);
                }
                _turnQueue.Insert(0, currentUnit);
            }
        }

        public void SetCameraShakeEnabled(bool enabled)
        {
            _cameraShakeEnabled = enabled;
        }

        public Vector2 GetShakeOffset()
        {
            if (_shakeTimer > 0f)
            {
                _shakeOffsetX = (float)(_rng.Next(-5, 6));
                return new Vector2(_shakeOffsetX, 0f);
            }
            return Vector2.Zero;
        }

        private void HandleAttackResolved(Character target, AttackResult result)
        {
            if (_cameraShakeEnabled)
            {
                _shakeTimer = 0.25f;
            }

            var rect = GetAvatarHomeRect(target);
            float offsetX = (float)(_rng.Next(-30, 31));
            float offsetY = (float)(_rng.Next(-10, 11));
            Vector2 pos = new Vector2(rect.X + rect.Width / 2f + offsetX, (rect.Y - 15f) + offsetY);

            if (result.IsMiss)
            {
                _floatingTexts.Add(new FloatingText { Position = pos, Text = "MISS", Timer = FloatingTextDuration, Color = Color.LightGray, Scale = MissTextScale });
                return;
            }

            string text = result.IsCrit ? $"-{result.Damage}!" : $"-{result.Damage}";
            Color color = result.IsCrit ? Color.Red : Color.OrangeRed;
            float scale = result.IsCrit ? CritTextScale : DamageTextScale;
            _floatingTexts.Add(new FloatingText { Position = pos, Text = text, Timer = FloatingTextDuration, Color = color, Scale = scale });

            if ((!target.IsAlive || target.Hp <= 0) && !_playedDeathSound.Contains(target))
            {
                _playedDeathSound.Add(target);
                AudioManager.PlaySfx("Audio/Deadsound");
            }
        }

        private void HandleHealResolved(Character target, int amount)
        {
            var rect = GetAvatarHomeRect(target);
            Vector2 pos = new Vector2(rect.X + rect.Width / 2f, rect.Y - 15f);
            _floatingTexts.Add(new FloatingText { Position = pos, Text = $"+{amount}", Timer = FloatingTextDuration, Color = Color.LimeGreen, Scale = DamageTextScale });
        }

        public void Update(GameTime gameTime, KeyboardState keyboard, MouseState mouse, bool leftClicked, ref bool manualModePreference, ref int coins, List<Item> inventory)
        {
            _coins = coins;
            _inventory = inventory;

            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_shakeTimer > 0f) _shakeTimer -= delta;

            if (_elmaIsUsingUltimateAnim)
            {
                _elmaIsAnimatingAttack = false;
                _elmaUltimateTimer += delta;

                if (_elmaUltimateTimer >= 0.8f && !_elmaHasDealtDamage)
                {
                    _elmaHasDealtDamage = true;
                    if (_battle != null && _elmaCasterRef != null && _elmaUltimateTarget != null)
                    {
                        _battle.CastUltimate(_elmaCasterRef, _elmaUltimateTarget);
                    }
                }

                if (_elmaUltimateTimer >= 1.5f)
                {
                    _elmaIsUsingUltimateAnim = false;
                    _elmaUltimateTarget = null;
                    _elmaCasterRef = null;
                    _battle.IsUsingUltimate = false;
                }
                UpdateFloatingTexts(delta);
                return;
            }

            // --- อัปเดตการเคลื่อนที่โปรเจกต์ไทล์และอนิเมชันระเบิดของ Lumi ---
            if (_lumiIsProjectileMoving)
            {
                Vector2 direction = _lumiProjTargetPos - _lumiProjPos;
                float distance = direction.Length();
                if (distance > 15f)
                {
                    direction.Normalize();
                    _lumiProjPos += direction * _lumiProjSpeed * delta;
                }
                else
                {
                    _lumiIsProjectileMoving = false;
                    _lumiIsUsingUltimateAnim = true;
                    _lumiUltimateTimer = 0f;
                    _lumiHasDealtDamage = false;
                }
                UpdateFloatingTexts(delta);
                return;
            }

            if (_lumiIsUsingUltimateAnim)
            {
                _lumiIsAnimatingAttack = false;
                _lumiUltimateTimer += delta;

                float totalDuration = 0.8f;
                if (_lumiUltimateTimer >= totalDuration && !_lumiHasDealtDamage)
                {
                    _lumiHasDealtDamage = true;
                    if (_battle != null && _lumiCasterRef != null)
                    {
                        // สกิล Lumi เป็น AoE ใส่ศัตรูทุกตัวอยู่แล้วภายใน ResolveUltimate()
                        // เรียกแค่ครั้งเดียวพอ ไม่งั้นดาเมจจะโดนคูณซ้ำตามจำนวนศัตรูที่ยังไม่ตาย
                        _battle.CastUltimate(_lumiCasterRef, _lumiUltimateTarget);
                    }
                }

                if (_lumiUltimateTimer >= 1.2f)
                {
                    _lumiIsUsingUltimateAnim = false;
                    _lumiUltimateTarget = null;
                    _lumiCasterRef = null;
                    _battle.IsUsingUltimate = false;
                }
                UpdateFloatingTexts(delta);
                return;
            }

            _elmaAnimTimer += delta;
            if (_elmaAnimTimer >= 0.12f)
            {
                _elmaAnimTimer = 0f;
                _elmaFrameIndex++;
            }

            _lumiAnimTimer += delta;
            if (_lumiAnimTimer >= 0.12f)
            {
                _lumiAnimTimer = 0f;
                _lumiFrameIndex++;
            }

            _aliceAnimTimer += delta;
            if (_aliceAnimTimer >= 0.12f)
            {
                _aliceAnimTimer = 0f;
                _aliceFrameIndex++;
            }

            _hornetAnimTimer += delta;
            if (_hornetAnimTimer >= 0.12f)
            {
                _hornetAnimTimer = 0f;
                _hornetFrameIndex++;
            }

            if (_battle != null)
            {
                UpdateDeathFades(delta);
                UpdateTurnQueue();

                if (_battle.State != BattleState.Running)
                {
                    _battleEndDelayTimer += delta;
                }

                bool isElmaAttacking = (_battle.ActiveAttacker != null && _battle.ActiveAttacker == _elmaCasterRef && !_elmaIsUsingUltimateAnim);
                if (!isElmaAttacking && _battle.ActiveAttacker != null && !string.IsNullOrEmpty(_battle.ActiveAttacker.Name))
                {
                    if (_battle.ActiveAttacker.Name.IndexOf("elma", StringComparison.OrdinalIgnoreCase) >= 0 && !_elmaIsUsingUltimateAnim)
                    {
                        isElmaAttacking = true;
                    }
                }

                if (isElmaAttacking)
                {
                    if (!_elmaHasAnimatedThisTurn)
                    {
                        _elmaIsAnimatingAttack = true;
                        _elmaAttackHoldTimer = 1.0f;
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
                        float timePerFrame = 1.0f / totalFrames;
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

                bool isLumiAttacking = (_battle.ActiveAttacker != null && (IsLumi(_battle.ActiveAttacker)));
                if (isLumiAttacking)
                {
                    if (!_lumiHasAnimatedThisTurn)
                    {
                        _lumiIsAnimatingAttack = true;
                        _lumiAttackHoldTimer = 1.0f;
                        _lumiStoredTarget = _battle.ActiveTarget;
                        _lumiCustomFrameIndex = 0;
                        _lumiCustomAnimTimer = 0f;
                        _lumiHasAnimatedThisTurn = true;
                    }
                }
                else
                {
                    _lumiHasAnimatedThisTurn = false;
                }

                if (_lumiIsAnimatingAttack)
                {
                    _lumiAttackHoldTimer -= delta;
                    if (_lumiAttackTex != null)
                    {
                        int frameWidth = 320;
                        int totalFrames = Math.Max(1, _lumiAttackTex.Width / frameWidth);
                        float timePerFrame = 1.0f / totalFrames;
                        _lumiCustomAnimTimer += delta;
                        if (_lumiCustomAnimTimer >= timePerFrame)
                        {
                            _lumiCustomAnimTimer = 0f;
                            if (_lumiCustomFrameIndex < totalFrames - 1)
                            {
                                _lumiCustomFrameIndex++;
                            }
                        }
                    }

                    if (_lumiAttackHoldTimer <= 0f)
                    {
                        _lumiIsAnimatingAttack = false;
                        _lumiStoredTarget = null;
                    }
                }

                bool isAliceAttacking = (_battle.ActiveAttacker != null && IsAlice(_battle.ActiveAttacker));
                if (isAliceAttacking)
                {
                    if (!_aliceHasAnimatedThisTurn)
                    {
                        _aliceIsAnimatingAttack = true;
                        _aliceAttackHoldTimer = 1.0f;
                        _aliceStoredTarget = _battle.ActiveTarget;
                        _aliceCustomFrameIndex = 0;
                        _aliceCustomAnimTimer = 0f;
                        _aliceHasAnimatedThisTurn = true;
                    }
                }
                else
                {
                    _aliceHasAnimatedThisTurn = false;
                }

                if (_aliceIsAnimatingAttack)
                {
                    _aliceAttackHoldTimer -= delta;
                    if (_aliceAttackTex != null)
                    {
                        int frameWidth = 480;
                        int totalFrames = 6;
                        float timePerFrame = 1.0f / totalFrames;
                        _aliceCustomAnimTimer += delta;
                        if (_aliceCustomAnimTimer >= timePerFrame)
                        {
                            _aliceCustomAnimTimer = 0f;
                            if (_aliceCustomFrameIndex < totalFrames - 1)
                            {
                                _aliceCustomFrameIndex++;
                            }
                        }
                    }

                    if (_aliceAttackHoldTimer <= 0f)
                    {
                        _aliceIsAnimatingAttack = false;
                        _aliceStoredTarget = null;
                    }
                }

                if (!_elmaIsAnimatingAttack && !_lumiIsAnimatingAttack && !_aliceIsAnimatingAttack)
                {
                    if (!_isWaitingForUltimateTarget && !_battle.IsUsingUltimate)
                    {
                        _battle.Update(delta);
                    }
                }
            }

            UpdateFloatingTexts(delta);

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || keyboard.IsKeyDown(Keys.Escape))
            {
                _playUIClick();
                _changeState(AppState.MainMenu);
                return;
            }

            if (_battle.State != BattleState.Running && _battleEndDelayTimer < 1.3f) return;

            UpdateUltimates(mouse, leftClicked);

            if (_isWaitingForUltimateTarget && mouse.RightButton == ButtonState.Pressed)
            {
                _isWaitingForUltimateTarget = false;
                _battle.UltimateCaster = null;
                _battle.IsUsingUltimate = false;
                _elmaCasterRef = null;
                _lumiCasterRef = null;
            }

            if (_battle.State == BattleState.AllyWin && !_isStoryMode)
            {
                if (_currentItemChoices == null || _currentItemChoices.Count == 0)
                {
                    foreach (var c in _party) c.UpgradePoints += _battle.RewardPoints;
                    coins += _battle.RewardCoins;
                    _currentItemChoices = ItemPool.GetRandomItems(3, _rng);
                }

                _hoveredItemCard = -1;
                for (int i = 0; i < _currentItemChoices.Count; i++)
                {
                    if (GetItemCardRect(i).Contains(mouse.Position)) _hoveredItemCard = i;
                }

                if (leftClicked && _hoveredItemCard >= 0)
                {
                    _playUIClick();
                    var item = _currentItemChoices[_hoveredItemCard];
                    _inventory.Add(item);
                    _currentItemChoices = null;
                    _goToUpgrade();
                }

                _hoveredItemSkip = GetItemSkipButtonRect().Contains(mouse.Position);
                if (leftClicked && _hoveredItemSkip)
                {
                    _playUIClick();
                    _currentItemChoices = null;
                    _goToUpgrade();
                }
                return;
            }

            _hoveredPauseButton = GetPauseButtonRect().Contains(mouse.Position);
            _hoveredBattleSettings = GetBattleSettingsButtonRect().Contains(mouse.Position);
            _hoveredAutoToggle = GetAutoToggleButtonRect().Contains(mouse.Position);

            if (leftClicked)
            {
                if (_hoveredPauseButton)
                {
                    _playUIClick();
                    _openPause?.Invoke();
                }
                else if (_hoveredBattleSettings)
                {
                    _playUIClick();
                    _openSettings();
                }
                else if (_hoveredAutoToggle)
                {
                    _playUIClick();
                    _battle.SetManualMode(!_battle.ManualMode);
                    manualModePreference = _battle.ManualMode;
                }
            }

            _hoveredEnemyTarget = null;
            Character hoveredAllyTarget = null;

            if (_battle.PendingAttacker != null || _isWaitingForUltimateTarget)
            {
                bool isClericCaster = _battle.UltimateCaster != null && (_battle.UltimateCaster.Name?.IndexOf("cleric", StringComparison.OrdinalIgnoreCase) >= 0);

                if (_isWaitingForUltimateTarget && isClericCaster)
                {
                    foreach (var ally in _battle.Allies)
                    {
                        if (!ally.IsAlive) continue;
                        if (GetAvatarHomeRect(ally).Contains(mouse.Position)) { hoveredAllyTarget = ally; break; }
                    }

                    if (leftClicked && hoveredAllyTarget != null)
                    {
                        _playUIClick();
                        _battle.CastUltimate(_battle.UltimateCaster, hoveredAllyTarget);
                        _isWaitingForUltimateTarget = false;
                        hoveredAllyTarget = null;
                    }
                }
                else
                {
                    foreach (var enemy in _battle.Enemies)
                    {
                        if (!enemy.IsAlive) continue;
                        if (GetAvatarHomeRect(enemy).Contains(mouse.Position)) { _hoveredEnemyTarget = enemy; break; }
                    }

                    if (leftClicked && _hoveredEnemyTarget != null)
                    {
                        if (_isWaitingForUltimateTarget)
                        {
                            _playUIClick();

                            bool isElmaCaster = _battle.UltimateCaster != null &&
                                                (_battle.UltimateCaster == _elmaCasterRef ||
                                                 _battle.UltimateCaster.Name?.IndexOf("elma", StringComparison.OrdinalIgnoreCase) >= 0);

                            bool isLumiCaster = _battle.UltimateCaster != null &&
                                                (_battle.UltimateCaster == _lumiCasterRef ||
                                                 _battle.UltimateCaster.Name?.IndexOf("lumi", StringComparison.OrdinalIgnoreCase) >= 0);

                            if (isElmaCaster)
                            {
                                _elmaCasterRef = _battle.UltimateCaster;
                                _elmaIsUsingUltimateAnim = true;
                                _elmaUltimateTimer = 0f;
                                _elmaHasDealtDamage = false;
                                _elmaUltimateTarget = _hoveredEnemyTarget;
                            }
                            else if (isLumiCaster)
                            {
                                _lumiCasterRef = _battle.UltimateCaster;
                                var casterRect = GetAvatarHomeRect(_lumiCasterRef);
                                var targetRect = GetAvatarHomeRect(_hoveredEnemyTarget);

                                _lumiProjPos = new Vector2(casterRect.X + casterRect.Width / 2f, casterRect.Y + casterRect.Height / 2f);
                                _lumiProjTargetPos = new Vector2(targetRect.X + targetRect.Width / 2f, targetRect.Y + targetRect.Height / 2f);

                                _lumiIsProjectileMoving = true;
                                _lumiUltimateTarget = _hoveredEnemyTarget;
                            }
                            else
                            {
                                _battle.CastUltimate(_battle.UltimateCaster, _hoveredEnemyTarget);
                            }
                            _isWaitingForUltimateTarget = false;
                        }
                        else
                        {
                            _battle.TrySelectTarget(_hoveredEnemyTarget);
                        }
                        _hoveredEnemyTarget = null;
                    }
                }
            }

            if (_battle.State == BattleState.AllyWin)
            {
                if (_isStoryMode)
                {
                    _hoveredStoryWinContinue = GetStoryWinContinueRect().Contains(mouse.Position);
                    _hoveredStoryWinMenu = GetStoryWinMenuRect().Contains(mouse.Position);

                    if (leftClicked)
                    {
                        if (_hoveredStoryWinContinue) { _playUIClick(); _changeState(AppState.StoryModeSelect); }
                        else if (_hoveredStoryWinMenu) { _playUIClick(); _changeState(AppState.MainMenu); }
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
                        _playUIClick();
                        if (_isStoryMode) StartNewBattle(_party, _formationSlots, _currentStageIsHard, _isStoryMode, _storyStageIndex, ref coins, inventory, manualModePreference, _cameraShakeEnabled);
                        else _startNewRunForChallenge();
                    }
                    else if (_hoveredLoseMenu)
                    {
                        _playUIClick();
                        _changeState(AppState.MainMenu);
                    }
                }
            }
        }

        private void UpdateUltimates(MouseState mouse, bool leftClicked)
        {
            if (_battle.State != BattleState.Running) return;

            int size = 90;
            int spacing = 35;
            int totalWidth = (5 * size) + (4 * spacing);

            int startX = ((ScreenWidth - totalWidth) / 2) + 200;
            int yPos = ScreenHeight - 190;

            for (int i = 0; i < 5; i++)
            {
                var c = _formationSlots[i];
                if (c != null && c.IsAlive && c.CurrentSP >= c.MaxSP)
                {
                    Vector2 center = new Vector2(startX + i * (size + spacing) + (size / 2f), yPos);
                    Rectangle hitBox = new Rectangle((int)center.X - size / 2, (int)center.Y - size / 2, size, size);

                    if (hitBox.Contains(mouse.Position) && leftClicked && !_battle.IsUsingUltimate && !_isWaitingForUltimateTarget)
                    {
                        _playUIClick();
                        _isWaitingForUltimateTarget = true;
                        _battle.UltimateCaster = c;
                        _battle.IsUsingUltimate = true;

                        if (c.Name?.IndexOf("elma", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _elmaCasterRef = c;
                        }
                        else if (c.Name?.IndexOf("lumi", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _lumiCasterRef = c;
                        }
                    }
                }
            }
        }

        private void UpdateDeathFades(float delta)
        {
            if (_battle == null) return;

            var allUnits = new List<Character>();
            if (_battle.Allies != null) allUnits.AddRange(_battle.Allies);
            if (_battle.Enemies != null) allUnits.AddRange(_battle.Enemies);

            foreach (var unit in allUnits)
            {
                if (!unit.IsAlive || unit.Hp <= 0)
                {
                    if (!_deathFadeAlphas.ContainsKey(unit))
                    {
                        _deathFadeAlphas[unit] = 1.0f;
                        _deathTimers[unit] = 0f;
                    }
                    else
                    {
                        _deathTimers[unit] += delta;
                    }

                    if (_deathFadeAlphas[unit] > 0f)
                    {
                        _deathFadeAlphas[unit] -= delta * 1.6f;
                        if (_deathFadeAlphas[unit] < 0f) _deathFadeAlphas[unit] = 0f;
                    }
                }
                else
                {
                    _deathFadeAlphas[unit] = 1.0f;
                    _deathTimers[unit] = 0f;
                }
            }
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

        public void Draw()
        {
            DrawBattleBackground();
            DrawTurnQueue();
            DrawTeam(_battle.Allies, isEnemyRow: false);
            DrawTeam(_battle.Enemies, isEnemyRow: true);

            // --- วาดโปรเจกต์ไทล์ skill_lumi_Boom (160x160, 11 เฟรม) กำลังเคลื่อนที่ไปหาเป้าหมาย ---
            if (_lumiIsProjectileMoving && _lumiSkillBoomTex != null)
            {
                int fw = 160;
                int fh = 160;
                int totalFrames = 11;
                int cols = Math.Max(1, _lumiSkillBoomTex.Width / fw);

                // คำนวณเฟรมตามเวลาเคลื่อนที่
                int currentFrame = (int)(_lumiProjPos.X / 20f) % totalFrames;
                int col = currentFrame % cols;
                int row = currentFrame / cols;

                Rectangle src = new Rectangle(col * fw, row * fh, fw, fh);
                Vector2 origin = new Vector2(fw / 2f, fh / 2f);

                _spriteBatch.Draw(_lumiSkillBoomTex, _lumiProjPos, src, Color.White, 0f, origin, 1.5f, SpriteEffects.None, 0f);
            }

            DrawFloatingTexts();
            DrawTurnArrows();

            DrawFloorIndicator();

            DrawUltimates();

            if (_battleEndDelayTimer >= 1.3f)
            {
                DrawResultBanner();
            }

            DrawPauseButton();
            DrawBattleSettingsButton();
            DrawAutoToggleButton();

            if (_isWaitingForUltimateTarget)
            {
                bool isClericCasting = _battle.UltimateCaster != null && (_battle.UltimateCaster.Name?.IndexOf("cleric", StringComparison.OrdinalIgnoreCase) >= 0);
                string targetTypeMsg = isClericCasting ? "an ally target" : "an enemy target";
                string casterName = _battle.UltimateCaster?.Name ?? "Character";
                DrawTextCentered($"Select {targetTypeMsg} for {casterName}'s Ultimate! (Right-Click to Cancel)", new Vector2(ScreenWidth / 2f, ScreenHeight - 290), Color.Gold, 1.3f);
            }
        }

        private bool IsAlice(Character unit)
        {
            if (unit == null) return false;
            if (unit == _aliceCasterRef) return true;
            string name = unit.Name ?? "";
            return name == "Alice" || name == "Swordman" || name.Contains("Alice") || name.Contains("Swordman");
        }

        private void GetUnitPortraitInfo(Character unit, out Texture2D tex, out Rectangle? sourceRect)
        {
            if (IsElma(unit) && _elmaIdleTex != null)
            {
                tex = _elmaIdleTex;
                sourceRect = new Rectangle(0, 0, 160, 320);
                return;
            }

            if (IsLumi(unit) && _lumiIdleTex != null)
            {
                tex = _lumiIdleTex;
                sourceRect = new Rectangle(0, 0, 240, 320);
                return;
            }

            if (IsAlice(unit) && _aliceIdleTex != null)
            {
                tex = _aliceIdleTex;
                sourceRect = new Rectangle(0, 0, 480, 480);
                return;
            }

            if (!unit.IsEnemy)
            {
                tex = _testCharacterTex;
                sourceRect = null;
                return;
            }

            bool isGiantHornet = !string.IsNullOrEmpty(unit.Name) &&
                (unit.Name.IndexOf("bee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 unit.Name.IndexOf("hornet", StringComparison.OrdinalIgnoreCase) >= 0);

            if (isGiantHornet && _giantHornetIdleTex != null)
            {
                tex = _giantHornetIdleTex;
                sourceRect = new Rectangle(0, 0, 192, 192);
                return;
            }

            tex = GetEnemyTexture(unit);
            sourceRect = null;
        }

        private void DrawTurnQueue()
        {
            if (_battle == null || _turnQueue == null) return;

            _turnQueue.RemoveAll(c => c == null || !c.IsAlive);

            int startX = 50;
            int startY = 160;
            int boxWidth = 75;
            int boxHeight = 70;
            int spacing = 12;
            int maxShow = 6;

            Character activeUnit = _battle.PendingAttacker ?? _battle.ActiveAttacker;

            var drawnUnits = new HashSet<Character>();
            int count = 0;

            for (int i = 0; i < _turnQueue.Count && count < maxShow; i++)
            {
                var unit = _turnQueue[i];
                if (unit == null || !unit.IsAlive || drawnUnits.Contains(unit)) continue;
                drawnUnits.Add(unit);

                Rectangle rect = new Rectangle(startX, startY + count * (boxHeight + spacing), boxWidth, boxHeight);

                Color bgColor = unit.IsEnemy ? new Color(140, 40, 40, 220) : new Color(40, 90, 160, 220);
                bool isCurrentTurn = (unit == activeUnit);
                if (isCurrentTurn)
                {
                    bgColor = new Color(255, 215, 0, 240);
                }

                DrawRect(rect, bgColor);

                GetUnitPortraitInfo(unit, out Texture2D tex, out Rectangle? srcRect);
                if (tex != null)
                {
                    float srcW = srcRect.HasValue ? srcRect.Value.Width : tex.Width;
                    float srcH = srcRect.HasValue ? srcRect.Value.Height : tex.Height;

                    float scaleX = (float)(rect.Width - 6) / srcW;
                    float scaleY = (float)(rect.Height - 6) / srcH;
                    float scale = Math.Min(scaleX, scaleY);

                    Vector2 drawPos = new Vector2(
                        rect.Center.X - (srcW * scale) / 2f,
                        rect.Center.Y - (srcH * scale) / 2f
                    );

                    SpriteEffects effect = unit.IsEnemy ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    _spriteBatch.Draw(tex, drawPos, srcRect, Color.White, 0f, Vector2.Zero, scale, effect, 0f);
                }

                Color borderColor = isCurrentTurn ? Color.White : Color.DarkGray;
                int borderWidth = isCurrentTurn ? 3 : 2;
                DrawRectBorder(rect, borderColor, borderWidth);

                count++;
            }
        }

        private void DrawUltimates()
        {
            int size = 90;
            int spacing = 35;
            int totalWidth = (5 * size) + (4 * spacing);

            int startX = ((ScreenWidth - totalWidth) / 2) + 200;
            int yPos = ScreenHeight - 190;

            for (int i = 0; i < 5; i++)
            {
                var c = _formationSlots[i];
                Vector2 center = new Vector2(startX + i * (size + spacing) + (size / 2f), yPos);

                if (c != null && c.IsAlive)
                {
                    float progress = c.MaxSP > 0 ? MathHelper.Clamp((float)c.CurrentSP / c.MaxSP, 0f, 1f) : 0f;
                    bool isReady = c.CurrentSP >= c.MaxSP;
                    DrawUltimateSquare(center, size, progress, isReady, c.Name);
                }
                else
                {
                    DrawUltimateSquare(center, size, 0f, false, "");
                }
            }
        }

        private void DrawUltimateSquare(Vector2 center, int size, float progressPercent, bool isReady, string name)
        {
            Color bgColor = new Color(50, 50, 50, 200);
            Color fillColor = new Color(30, 144, 255, 220);
            if (isReady) fillColor = new Color(255, 215, 0, 220);

            Rectangle bgRect = new Rectangle((int)center.X - size / 2, (int)center.Y - size / 2, size, size);

            DrawRect(bgRect, bgColor);

            int fillHeight = (int)(size * progressPercent);
            int fillY = bgRect.Bottom - fillHeight;
            Rectangle fillRect = new Rectangle(bgRect.X, fillY, size, fillHeight);
            DrawRect(fillRect, fillColor);

            Color borderColor = isReady ? Color.Gold : Color.DarkGray;
            DrawRectBorder(bgRect, borderColor, 3);

            if (isReady)
                DrawTextCentered("MAX", new Vector2(center.X, center.Y), Color.White, 1.2f);
            else if (!string.IsNullOrEmpty(name))
                DrawTextCentered($"{(int)(progressPercent * 100)}%", new Vector2(center.X, center.Y), Color.LightGray, 0.9f);
        }

        private void DrawBattleBackground()
        {
            if (_battleBackground != null)
                _spriteBatch.Draw(_battleBackground, new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White);
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
            int index;

            if (unit.IsEnemy)
            {
                index = team.IndexOf(unit);
            }
            else
            {
                index = Array.IndexOf(_formationSlots, unit);
                if (index < 0) index = team.IndexOf(unit);
            }

            if (index < 0) index = 0;

            // ด่าน Boss มีศัตรูแค่ตัวเดียว ให้วางไว้กึ่งกลางแนวตั้งของพื้นที่ฟอร์เมชันฝั่งศัตรู
            // แทนที่จะใช้ตำแหน่ง front-row ช่องแรก (บนสุด) ตามปกติ ซึ่งทำให้ดูเอียงไปด้านบน
            if (unit.IsEnemy && team.Count == 1)
            {
                int bossY = FormationTop + RowSpacing;
                return new Rectangle(EnemyFrontX, bossY, AvatarSize, AvatarSize);
            }

            bool isFront = index < FrontRowCount;
            int rowSlot = isFront ? index : index - FrontRowCount;
            int y = isFront ? FormationTop + rowSlot * RowSpacing : FormationTop + RowSpacing / 2 + rowSlot * RowSpacing;
            int x = unit.IsEnemy ? (isFront ? EnemyFrontX : EnemyBackX) : (isFront ? AllyFrontX : AllyBackX);
            return new Rectangle(x, y, AvatarSize, AvatarSize);
        }

        private bool IsElma(Character unit)
        {
            if (unit == null) return false;
            if (unit == _elmaCasterRef) return true;
            return !string.IsNullOrEmpty(unit.Name) && unit.Name.IndexOf("elma", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsLumi(Character unit)
        {
            if (unit == null) return false;
            if (unit == _lumiCasterRef) return true;
            return !string.IsNullOrEmpty(unit.Name) && unit.Name.IndexOf("lumi", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Rectangle GetAvatarDisplayRect(Character unit)
        {
            var home = GetAvatarHomeRect(unit);
            bool isElma = IsElma(unit);
            bool isLumi = IsLumi(unit);
            bool isAlice = IsAlice(unit);

            if (isElma && _elmaIsUsingUltimateAnim && _elmaUltimateTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_elmaUltimateTarget);
                return new Rectangle(targetRect.X - 240, targetRect.Y, targetRect.Width, targetRect.Height);
            }
            else if (isLumi && _lumiIsUsingUltimateAnim)
            {
                return GetAvatarHomeRect(unit);
            }
            else if (isElma && _elmaIsAnimatingAttack && _elmaStoredTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_elmaStoredTarget);
                int offsetX = unit.IsEnemy ? 110 : -110;
                return new Rectangle(targetRect.X + offsetX, targetRect.Y, targetRect.Width, targetRect.Height);
            }
            else if (isLumi && _lumiIsAnimatingAttack && _lumiStoredTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_lumiStoredTarget);
                int offsetX = unit.IsEnemy ? 110 : -110;
                return new Rectangle(targetRect.X + offsetX, targetRect.Y, targetRect.Width, targetRect.Height);
            }
            else if (isAlice && _aliceIsAnimatingAttack && _aliceStoredTarget != null)
            {
                var targetRect = GetAvatarHomeRect(_aliceStoredTarget);
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

        private Texture2D GetEnemyTexture(Character unit)
        {
            string name = unit.Name?.ToLower() ?? "";
            Texture2D tex = null;
            if (name.Contains("orc") || name.Contains("orc_warrior")) tex = _orcWarriorTex;
            else if (name.Contains("bee") || name.Contains("hornet")) tex = _giantHornetIdleTex;
            else if (name.Contains("dark") || name.Contains("dark_mage")) tex = _darkMageTex;
            else if (name.Contains("ghost") || name.Contains("ghost_healer")) tex = _ghostHealerTex;
            else if (name.Contains("stone") || name.Contains("stone_giant")) tex = _stoneGiantTex;

            return tex ?? _testEnemyTex;
        }

        private void DrawTeam(List<Character> team, bool isEnemyRow)
        {
            foreach (var unit in team)
            {
                bool isActive = _battle.ActiveAttacker == unit || _battle.ActiveTarget == unit;
                bool isHoveredTarget = _hoveredEnemyTarget == unit;
                Color themeColor = isEnemyRow ? new Color(180, 60, 60) : new Color(60, 120, 200);

                float alpha = 1.0f;
                if (_deathFadeAlphas.ContainsKey(unit))
                {
                    alpha = _deathFadeAlphas[unit];
                }
                else if (!unit.IsAlive || unit.Hp <= 0)
                {
                    alpha = 0f;
                }

                var avatarRect = GetAvatarDisplayRect(unit);
                bool isElma = IsElma(unit);
                bool isLumi = IsLumi(unit);
                bool isAlice = IsAlice(unit);

                if (isElma && _elmaIsUsingUltimateAnim && _elmaUltimateTarget != null)
                {
                    float customX = avatarRect.X;
                    float customY = avatarRect.Y;

                    if (_elmaUltimateTimer < 0.8f)
                    {
                        if (_elmaSkill01Tex != null)
                        {
                            int totalFrames = 18;
                            int cols = 6;
                            int rows = 3;

                            int fw = _elmaSkill01Tex.Width / cols;
                            int fh = _elmaSkill01Tex.Height / rows;

                            float subProgress = _elmaUltimateTimer / 0.8f;
                            int frameIndex = Math.Min(totalFrames - 1, (int)(subProgress * totalFrames));

                            int col = frameIndex % cols;
                            int row = frameIndex / cols;

                            Rectangle srcRect = new Rectangle(col * fw, row * fh, fw, fh);

                            float scale = ((float)avatarRect.Height / fh) * 2.5f;
                            float drawW = fw * scale;
                            float drawH = fh * scale;
                            Vector2 drawPos = new Vector2(
                                customX + (avatarRect.Width - drawW) / 2f,
                                customY + avatarRect.Height - drawH
                            );

                            _spriteBatch.Draw(_elmaSkill01Tex, drawPos, srcRect, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                        }
                    }
                    else
                    {
                        if (_elmaSkill02Tex != null)
                        {
                            int totalFrames = 8;
                            int cols = 4;
                            int rows = 2;

                            int fw = _elmaSkill02Tex.Width / cols;
                            int fh = _elmaSkill02Tex.Height / rows;

                            float subProgress = (_elmaUltimateTimer - 0.8f) / (1.5f - 0.8f);
                            int frameIndex = Math.Min(totalFrames - 1, (int)(subProgress * totalFrames));

                            int col = frameIndex % cols;
                            int row = frameIndex / cols;

                            Rectangle srcRect = new Rectangle(col * fw, row * fh, fw, fh);

                            float scale = ((float)avatarRect.Height / fh) * 2.5f;
                            float drawW = fw * scale;
                            float drawH = fh * scale;
                            Vector2 drawPos = new Vector2(
                                customX + (avatarRect.Width - drawW) / 2f,
                                customY + avatarRect.Height - drawH
                            );

                            _spriteBatch.Draw(_elmaSkill02Tex, drawPos, srcRect, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                        }
                    }
                }
                else if (isElma)
                {
                    float baseScale = ((float)avatarRect.Height / 320f) * 1.8f;

                    if (_elmaIsAnimatingAttack)
                    {
                        if (_elmaAttackTex != null)
                        {
                            int frameWidth = 320;
                            int frameHeight = 320;
                            int totalFrames = Math.Max(1, _elmaAttackTex.Width / frameWidth);
                            int currentFrame = Math.Min(_elmaCustomFrameIndex, totalFrames - 1);
                            Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                            float drawW = frameWidth * baseScale;
                            float drawH = frameHeight * baseScale;
                            Vector2 drawPos = new Vector2(
                                avatarRect.X + (avatarRect.Width - drawW) / 2f,
                                avatarRect.Bottom - drawH
                            );

                            _spriteBatch.Draw(_elmaAttackTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                        }
                    }
                    else if (_elmaIdleTex != null)
                    {
                        int frameWidth = 160;
                        int frameHeight = 320;
                        int totalFrames = Math.Max(1, _elmaIdleTex.Width / frameWidth);
                        int currentFrame = _elmaFrameIndex % totalFrames;
                        Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                        float drawW = frameWidth * baseScale;
                        float drawH = frameHeight * baseScale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );

                        _spriteBatch.Draw(_elmaIdleTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                    }
                }
                else if (isLumi)
                {
                    float baseScale = ((float)avatarRect.Height / 320f) * 1.8f;

                    if (_lumiIsAnimatingAttack)
                    {
                        if (_lumiAttackTex != null)
                        {
                            int frameWidth = 320;
                            int frameHeight = 320;
                            int totalFrames = Math.Max(1, _lumiAttackTex.Width / frameWidth);
                            int currentFrame = Math.Min(_lumiCustomFrameIndex, totalFrames - 1);
                            Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                            float drawW = frameWidth * baseScale;
                            float drawH = frameHeight * baseScale;
                            Vector2 drawPos = new Vector2(
                                avatarRect.X + (avatarRect.Width - drawW) / 2f,
                                avatarRect.Bottom - drawH
                            );

                            _spriteBatch.Draw(_lumiAttackTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                        }
                    }
                    else if (_lumiIdleTex != null)
                    {
                        int frameWidth = 240;
                        int frameHeight = 320;
                        int totalFrames = 16;
                        int currentFrame = _lumiFrameIndex % totalFrames;
                        Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                        float drawW = frameWidth * baseScale;
                        float drawH = frameHeight * baseScale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );

                        _spriteBatch.Draw(_lumiIdleTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                    }
                }
                else if (isAlice)
                {
                    float baseScale = ((float)avatarRect.Height / 480f) * 2.2f;

                    if (_aliceIsAnimatingAttack)
                    {
                        if (_aliceAttackTex != null)
                        {
                            int frameWidth = 480;
                            int frameHeight = 480;
                            int totalFrames = 6;
                            int currentFrame = Math.Min(_aliceCustomFrameIndex, totalFrames - 1);
                            Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                            float drawW = frameWidth * baseScale;
                            float drawH = frameHeight * baseScale;
                            Vector2 drawPos = new Vector2(
                                avatarRect.X + (avatarRect.Width - drawW) / 2f,
                                avatarRect.Bottom - drawH
                            );

                            _spriteBatch.Draw(_aliceAttackTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                        }
                    }
                    else if (_aliceIdleTex != null)
                    {
                        int frameWidth = 480;
                        int frameHeight = 480;
                        int totalFrames = 4;
                        int currentFrame = _aliceFrameIndex % totalFrames;
                        Rectangle sourceRect = new Rectangle(currentFrame * frameWidth, 0, frameWidth, frameHeight);

                        float drawW = frameWidth * baseScale;
                        float drawH = frameHeight * baseScale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );

                        _spriteBatch.Draw(_aliceIdleTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                    }
                }
                else if (unit.IsEnemy)
                {
                    bool isGiantHornet = !string.IsNullOrEmpty(unit.Name) &&
                        (unit.Name.IndexOf("bee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         unit.Name.IndexOf("hornet", StringComparison.OrdinalIgnoreCase) >= 0);

                    if (isGiantHornet)
                    {
                        bool isHornetAttacking = (_battle.ActiveAttacker == unit && _giantHornetAttackTex != null);
                        Texture2D hornetTex = isHornetAttacking ? _giantHornetAttackTex : _giantHornetIdleTex;

                        if (hornetTex != null)
                        {
                            int frameWidth = 192;
                            int frameHeight = 192;
                            int totalFrames = isHornetAttacking ? 10 : 7;
                            int cols = isHornetAttacking ? 4 : Math.Max(1, hornetTex.Width / frameWidth);
                            int currentFrame = _hornetFrameIndex % totalFrames;

                            int col = currentFrame % cols;
                            int row = currentFrame / cols;

                            Rectangle sourceRect = new Rectangle(col * frameWidth, row * frameHeight, frameWidth, frameHeight);
                            float baseScale = ((float)avatarRect.Height / frameHeight) * 0.65f;
                            float drawW = frameWidth * baseScale;
                            float drawH = frameHeight * baseScale;
                            Vector2 drawPos = new Vector2(
                                avatarRect.X + (avatarRect.Width - drawW) / 2f,
                                avatarRect.Bottom - drawH
                            );

                            _spriteBatch.Draw(hornetTex, drawPos, sourceRect, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                        }
                    }
                    else
                    {
                        Texture2D enemyTex = GetEnemyTexture(unit);
                        if (enemyTex != null)
                        {
                            float baseScale = ((float)avatarRect.Height / enemyTex.Height) * 1.4f;
                            float drawW = enemyTex.Width * baseScale;
                            float drawH = enemyTex.Height * baseScale;
                            Vector2 drawPos = new Vector2(
                                avatarRect.X + (avatarRect.Width - drawW) / 2f,
                                avatarRect.Bottom - drawH
                            );

                            _spriteBatch.Draw(enemyTex, drawPos, null, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.FlipHorizontally, 0f);
                        }
                    }
                }
                else
                {
                    if (_testCharacterTex != null)
                    {
                        float baseScale = ((float)avatarRect.Height / _testCharacterTex.Height) * 1.4f;
                        float drawW = _testCharacterTex.Width * baseScale;
                        float drawH = _testCharacterTex.Height * baseScale;
                        Vector2 drawPos = new Vector2(
                            avatarRect.X + (avatarRect.Width - drawW) / 2f,
                            avatarRect.Bottom - drawH
                        );

                        _spriteBatch.Draw(_testCharacterTex, drawPos, null, Color.White * alpha, 0f, Vector2.Zero, baseScale, SpriteEffects.None, 0f);
                    }
                }

                // --- แสดงผลเอฟเฟกต์ Boom_Fx (ขนาด 160x160 มี 8 เฟรม) บน Enemy ทุกตัว เมื่อโปรเจกต์ไทล์พุ่งถึงเป้าหมาย ---
                if (_lumiIsUsingUltimateAnim && _boomFxTex != null && unit.IsEnemy && unit.IsAlive)
                {
                    float totalDuration = 0.8f;
                    float progress = MathHelper.Clamp(_lumiUltimateTimer / totalDuration, 0f, 1f);
                    int boomFrameIndex = (int)(progress * 8f);
                    if (boomFrameIndex > 7) boomFrameIndex = 7;

                    int fw = 160;
                    int fh = 160;
                    int cols = Math.Max(1, _boomFxTex.Width / fw);
                    int col = boomFrameIndex % cols;
                    int row = boomFrameIndex / cols;

                    Rectangle boomSrc = new Rectangle(col * fw, row * fh, fw, fh);
                    float boomScale = ((float)avatarRect.Height / fh) * 1.5f;
                    float boomW = fw * boomScale;
                    float boomH = fh * boomScale;
                    Vector2 finalPos = new Vector2(
                        avatarRect.X + (avatarRect.Width - boomW) / 2f,
                        avatarRect.Bottom - boomH
                    );

                    _spriteBatch.Draw(_boomFxTex, finalPos, boomSrc, Color.White * alpha, 0f, Vector2.Zero, boomScale, SpriteEffects.None, 0f);
                }

                // --- ป้องกัน Dead Effect วาดทับซ้อนขณะที่สกิล Lumi กำลังเล่นอนิเมชันอยู่ ---
                if ((!unit.IsAlive || unit.Hp <= 0) && _deadEffectTex != null && !_lumiIsUsingUltimateAnim && !_lumiIsProjectileMoving)
                {
                    int effFrameWidth = 160;
                    int effFrameHeight = 320;
                    int totalEffFrames = 4;
                    float deathTime = _deathTimers.ContainsKey(unit) ? _deathTimers[unit] : 0f;
                    int currentEffFrame = Math.Min(totalEffFrames - 1, (int)(deathTime / 0.15f));

                    Rectangle effSourceRect = new Rectangle(currentEffFrame * effFrameWidth, 0, effFrameWidth, effFrameHeight);
                    float effScale = ((float)avatarRect.Height / effFrameHeight) * 1.8f;
                    float effW = effFrameWidth * effScale;
                    float effH = effFrameHeight * effScale;
                    Vector2 effPos = new Vector2(
                        avatarRect.X + (avatarRect.Width - effW) / 2f,
                        avatarRect.Bottom - effH
                    );

                    _spriteBatch.Draw(_deadEffectTex, effPos, effSourceRect, Color.White * alpha, 0f, Vector2.Zero, effScale, SpriteEffects.None, 0f);
                }

                var home = GetAvatarHomeRect(unit);
                float hpBarY;
                int hpBarX = home.X;

                if (isElma && _elmaIsUsingUltimateAnim && _elmaUltimateTarget != null)
                {
                    hpBarX = avatarRect.X;
                    hpBarY = avatarRect.Y - HpBarGap - HpBarHeight;
                }
                else if (isElma || isLumi)
                {
                    float baseScale = ((float)home.Height / 320f) * 1.8f;
                    float drawH = 320f * baseScale;
                    hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                }
                else if (isAlice)
                {
                    float baseScale = ((float)home.Height / 480f) * 1.4f;
                    float drawH = 480f * baseScale;
                    hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                }
                else if (!unit.IsEnemy && _testCharacterTex != null)
                {
                    float baseScale = ((float)home.Height / _testCharacterTex.Height) * 1.4f;
                    float drawH = _testCharacterTex.Height * baseScale;
                    hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                }
                else if (unit.IsEnemy)
                {
                    bool isGiantHornet = !string.IsNullOrEmpty(unit.Name) &&
                        (unit.Name.IndexOf("bee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         unit.Name.IndexOf("hornet", StringComparison.OrdinalIgnoreCase) >= 0);

                    if (isGiantHornet)
                    {
                        float baseScale = ((float)home.Height / 192f) * 0.65f;
                        float drawH = 192f * baseScale;
                        hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                    }
                    else
                    {
                        var enemyTex = GetEnemyTexture(unit);
                        float baseScale = ((float)home.Height / enemyTex.Height) * 1.4f;
                        float drawH = enemyTex.Height * baseScale;
                        hpBarY = (home.Bottom - drawH) - HpBarGap - HpBarHeight;
                    }
                }
                else
                {
                    hpBarY = home.Y - HpBarGap - HpBarHeight;
                }

                var hpBg = new Rectangle(hpBarX, (int)hpBarY, home.Width, HpBarHeight);
                DrawRect(hpBg, new Color(60, 20, 20) * alpha);

                float hpRatio = unit.MaxHp > 0 ? (float)unit.Hp / unit.MaxHp : 0f;
                DrawRect(new Rectangle(hpBg.X, hpBg.Y, (int)(hpBg.Width * hpRatio), hpBg.Height), (hpRatio > 0.5f ? Color.LimeGreen : hpRatio > 0.2f ? Color.Orange : Color.Red) * alpha);
                DrawRectBorder(hpBg, Color.Black * alpha, 1);
            }
        }

        private Vector2 GetUnitArrowTipPosition(Character unit)
        {
            var home = GetAvatarHomeRect(unit);
            float topY = home.Y;
            bool isElma = IsElma(unit);
            bool isLumi = IsLumi(unit);
            bool isAlice = IsAlice(unit);

            if (isElma || isLumi)
            {
                float baseScale = ((float)home.Height / 320f) * 1.8f;
                topY = home.Bottom - (320f * baseScale);
            }
            else if (isAlice)
            {
                float baseScale = ((float)home.Height / 480f) * 1.4f;
                topY = home.Bottom - (480f * baseScale);
            }
            else if (!unit.IsEnemy && _testCharacterTex != null)
            {
                float baseScale = ((float)home.Height / _testCharacterTex.Height) * 1.4f;
                topY = home.Bottom - (_testCharacterTex.Height * baseScale);
            }
            else if (unit.IsEnemy)
            {
                bool isGiantHornet = !string.IsNullOrEmpty(unit.Name) &&
                    (unit.Name.IndexOf("bee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     unit.Name.IndexOf("hornet", StringComparison.OrdinalIgnoreCase) >= 0);

                if (isGiantHornet)
                {
                    float baseScale = ((float)home.Height / 192f) * 0.65f;
                    topY = home.Bottom - (192f * baseScale);
                }
                else
                {
                    Texture2D enemyTex = GetEnemyTexture(unit);
                    if (enemyTex != null)
                    {
                        float baseScale = ((float)home.Height / enemyTex.Height) * 1.4f;
                        topY = home.Bottom - (enemyTex.Height * baseScale);
                    }
                }
            }

            float hpBarY = topY - HpBarGap - HpBarHeight;
            return new Vector2(home.X + home.Width / 2f, hpBarY - 15f);
        }

        private void DrawFloatingTexts()
        {
            foreach (var ft in _floatingTexts)
                DrawTextCentered(ft.Text, ft.Position, ft.Color * MathHelper.Clamp(ft.Timer / FloatingTextDuration, 0f, 1f), ft.Scale);
        }

        private void DrawTurnArrows()
        {
            Character activeUnit = _battle.PendingAttacker ?? _battle.ActiveAttacker;
            if (activeUnit != null)
                DrawDownArrow(GetUnitArrowTipPosition(activeUnit), 16, Color.Yellow);
            if (_hoveredEnemyTarget != null)
                DrawDownArrow(GetUnitArrowTipPosition(_hoveredEnemyTarget), 16, Color.Orange);
        }

        private void DrawDownArrow(Vector2 tip, float size, Color color)
        {
            DrawLine(tip + new Vector2(-size, -size), tip, color, 5);
            DrawLine(tip + new Vector2(size, -size), tip, color, 5);
            DrawLine(tip + new Vector2(0, -size * 2.2f), tip + new Vector2(0, -size), color, 5);
        }

        private Rectangle GetPauseButtonRect() => new Rectangle(ScreenWidth - 280, 30, 70, 70);
        private Rectangle GetBattleSettingsButtonRect() => new Rectangle(ScreenWidth - 190, 30, 70, 70);
        private Rectangle GetAutoToggleButtonRect() => new Rectangle(ScreenWidth - 100, 30, 70, 70);

        private void DrawPauseButton()
        {
            var rect = GetPauseButtonRect();
            Color buttonColor = _hoveredPauseButton ? Color.DeepSkyBlue : Color.White;

            Vector2 center = new Vector2(rect.Center.X, rect.Center.Y);
            float radius = rect.Width / 2f;

            for (float angle = 0; angle < MathHelper.TwoPi; angle += 0.03f)
            {
                int bx = (int)(center.X + (radius - 2) * (float)Math.Cos(angle));
                int by = (int)(center.Y + (radius - 2) * (float)Math.Sin(angle));
                _spriteBatch.Draw(_pixel, new Rectangle(bx, by, 3, 3), buttonColor);
            }

            DrawTextCentered("||", center, buttonColor, 1.4f);
        }

        private void DrawAutoToggleButton()
        {
            var rect = GetAutoToggleButtonRect();
            bool isManual = _battle.ManualMode;

            Color buttonColor = Color.White;
            if (_hoveredAutoToggle) buttonColor = Color.DeepSkyBlue;
            else if (!isManual) buttonColor = Color.Gold;

            Vector2 center = new Vector2(rect.Center.X, rect.Center.Y);
            float radius = rect.Width / 2f;

            for (float angle = 0; angle < MathHelper.TwoPi; angle += 0.03f)
            {
                int bx = (int)(center.X + (radius - 2) * (float)Math.Cos(angle));
                int by = (int)(center.Y + (radius - 2) * (float)Math.Sin(angle));
                _spriteBatch.Draw(_pixel, new Rectangle(bx, by, 3, 3), buttonColor);
            }

            DrawTextCentered("A", center, buttonColor, 1.8f);
        }

        private void DrawBattleSettingsButton()
        {
            var rect = GetBattleSettingsButtonRect();
            Color buttonColor = _hoveredBattleSettings ? Color.DeepSkyBlue : Color.White;

            Vector2 center = new Vector2(rect.Center.X, rect.Center.Y);
            float radius = rect.Width / 2f;

            for (float angle = 0; angle < MathHelper.TwoPi; angle += 0.03f)
            {
                int bx = (int)(center.X + (radius - 2) * (float)Math.Cos(angle));
                int by = (int)(center.Y + (radius - 2) * (float)Math.Sin(angle));
                _spriteBatch.Draw(_pixel, new Rectangle(bx, by, 3, 3), buttonColor);
            }

            DrawTextCentered("S", center, buttonColor, 1.8f);
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
            else if (_battle.State == BattleState.AllyWin && !_isStoryMode)
            {
                if (_currentItemChoices != null)
                {
                    DrawRect(new Rectangle(0, 0, ScreenWidth, ScreenHeight), new Color(0, 0, 0, 200));

                    DrawTextCentered("Victory!", new Vector2(ScreenWidth / 2f, 105), Color.Gold, 3.0f);
                    DrawTextCentered("Choose an Item", new Vector2(ScreenWidth / 2f, 160), Color.White, 1.6f);

                    for (int i = 0; i < _currentItemChoices.Count; i++)
                    {
                        var rect = GetItemCardRect(i);
                        var item = _currentItemChoices[i];
                        bool hovered = _hoveredItemCard == i;
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

                        DrawRect(rect, hovered ? new Color(60, 60, 95) : baseCardBg);
                        DrawRectBorder(rect, hovered ? Color.Gold : rarityColor, hovered ? 4 : 3);

                        DrawTextCentered(item.Name, new Vector2(rect.Center.X, rect.Y + 60), rarityColor, 1.3f);
                        DrawTextCentered($"[{item.Rarity.ToString().ToUpper()}]", new Vector2(rect.Center.X, rect.Y + 110), rarityColor, 0.95f);
                        DrawTextCentered(item.Description, new Vector2(rect.Center.X, rect.Y + 185), Color.White, 0.9f);

                        DrawTextCentered("FREE", new Vector2(rect.Center.X, rect.Y + 245), Color.LimeGreen, 1f);
                    }

                    var skipRect = GetItemSkipButtonRect();
                    DrawRect(skipRect, _hoveredItemSkip ? Color.Gold : new Color(80, 80, 80));
                    DrawRectBorder(skipRect, Color.White, 3);
                    DrawTextCentered("Skip ->", new Vector2(skipRect.Center.X, skipRect.Center.Y), Color.White, 1.1f);
                }
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
        private Rectangle GetStoryWinContinueRect() => new Rectangle(ScreenWidth / 2 - 250, ScreenHeight / 2 - 30, 220, 60);
        private Rectangle GetStoryWinMenuRect() => new Rectangle(ScreenWidth / 2 + 30, ScreenHeight / 2 - 30, 220, 60);
        private Rectangle GetLoseRestartRect() => new Rectangle(ScreenWidth / 2 - 250, ScreenHeight / 2 - 30, 220, 60);
        private Rectangle GetLoseMenuRect() => new Rectangle(ScreenWidth / 2 + 30, ScreenHeight / 2 - 30, 220, 60);

        private void DrawRect(Rectangle rect, Color color) => _spriteBatch.Draw(_pixel, rect, color);

        private void DrawRectBorder(Rectangle rect, Color color, int thickness)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawFloorIndicator()
        {
            int currentFloor = _storyStageIndex + 1;
            string difficultyStr = _currentStageIsBoss ? "Boss" : (_currentStageIsHard ? "Hard" : "Normal");
            string floorText = $"Floor {currentFloor} {difficultyStr}";
            Vector2 position = new Vector2(ScreenWidth / 2f, 75f);
            DrawTextCentered(floorText, position, Color.White, 2.2f);
        }

        private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float length = edge.Length();
            if (length < 0.01f) return;
            _spriteBatch.Draw(_pixel, new Rectangle((int)start.X, (int)start.Y, (int)length, thickness), null, color, (float)Math.Atan2(edge.Y, edge.X), Vector2.Zero, SpriteEffects.None, 0);
        }

        private string GetSafeTest(string text)
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
            try { size = _font.MeasureString(text); } catch (ArgumentException) { text = GetSafeTest(text); size = _font.MeasureString(text); }
            try { _spriteBatch.DrawString(_font, text, center, color, 0f, size / 2f, scale, SpriteEffects.None, 0f); } catch (ArgumentException) { }
        }
    }
}