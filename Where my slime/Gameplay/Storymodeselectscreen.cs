using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Where_My_Slime.Battle;

namespace Where_My_Slime
{
    // หน้าเลือกด่านของ Story Mode (เลื่อนซ้าย/ขวาดูด่านอื่น แล้วกดการ์ดกลางเพื่อเริ่มด่านปัจจุบัน)
    public class StoryModeSelectScreen
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GameSession _session;

        private readonly Action _goToModeSelect;
        private readonly Action _goToTeamSetup;
        private readonly Action _playUIClick;

        private bool _hoveredStoryLeft;
        private bool _hoveredStoryRight;
        private bool _hoveredStoryCenter;
        private bool _hoveredStoryBack;

        public const int MaxStoryStages = 5;

        private const int ScreenWidth = 1920;
        private const int ScreenHeight = 1080;

        public StoryModeSelectScreen(
            SpriteBatch spriteBatch,
            SpriteFont font,
            Texture2D pixel,
            GameSession session,
            Action goToModeSelect,
            Action goToTeamSetup,
            Action playUIClick)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _session = session;
            _goToModeSelect = goToModeSelect;
            _goToTeamSetup = goToTeamSetup;
            _playUIClick = playUIClick;
        }

        private Rectangle GetStoryCenterCardRect() => new Rectangle(ScreenWidth / 2 - 180, ScreenHeight / 2 - 250, 360, 500);
        private Rectangle GetStoryLeftCardRect() => new Rectangle(ScreenWidth / 2 - 420, ScreenHeight / 2 - 200, 300, 400);
        private Rectangle GetStoryRightCardRect() => new Rectangle(ScreenWidth / 2 + 120, ScreenHeight / 2 - 200, 300, 400);
        private Rectangle GetStoryLeftArrowRect() => new Rectangle(ScreenWidth / 2 - 600, ScreenHeight / 2 - 50, 60, 100);
        private Rectangle GetStoryRightArrowRect() => new Rectangle(ScreenWidth / 2 + 540, ScreenHeight / 2 - 50, 60, 100);
        private Rectangle GetStoryBackButtonRect() => new Rectangle(40, 40, 150, 50);

        public void Update(MouseState mouse, bool leftClicked, KeyboardState keyboard)
        {
            if (keyboard.IsKeyDown(Keys.Escape)) { _playUIClick?.Invoke(); _goToModeSelect?.Invoke(); return; }

            _hoveredStoryCenter = GetStoryCenterCardRect().Contains(mouse.Position);
            _hoveredStoryLeft = _session.StoryStageIndex > 0 && GetStoryLeftArrowRect().Contains(mouse.Position);
            _hoveredStoryRight = _session.StoryStageIndex < MaxStoryStages - 1 && GetStoryRightArrowRect().Contains(mouse.Position);
            _hoveredStoryBack = GetStoryBackButtonRect().Contains(mouse.Position);

            if (leftClicked)
            {
                if (_hoveredStoryLeft) { _playUIClick?.Invoke(); _session.StoryStageIndex--; }
                else if (_hoveredStoryRight) { _playUIClick?.Invoke(); _session.StoryStageIndex++; }
                else if (_hoveredStoryBack) { _playUIClick?.Invoke(); _goToModeSelect?.Invoke(); }
                else if (_hoveredStoryCenter)
                {
                    _playUIClick?.Invoke();
                    _session.IsStoryMode = true;
                    _session.TeamSetupOrigin = TeamSetupOrigin.Story;
                    _session.AllHeroes = BattleManager.CreateStartingParty();
                    Array.Clear(_session.FormationSlots, 0, _session.FormationSlots.Length);

                    if (_session.StoryStageIndex == 0)
                    {
                        Where_My_Slime.Models.Character lumi = null;
                        foreach (var c in _session.AllHeroes) { if (c.Name == "Lumi") { lumi = c; break; } }
                        if (lumi == null && _session.AllHeroes.Count > 0) lumi = _session.AllHeroes[0];
                        _session.FormationSlots[1] = lumi;
                    }
                    else
                    {
                        for (int i = 0; i < Math.Min(_session.AllHeroes.Count, 5); i++) _session.FormationSlots[i] = _session.AllHeroes[i];
                    }
                    _goToTeamSetup?.Invoke();
                }
            }
        }

        public void Draw()
        {
            DrawTextCentered("Story Mode", new Vector2(ScreenWidth / 2f, 100), Color.Gold, 2.5f);

            if (_session.StoryStageIndex > 0)
            {
                var leftRect = GetStoryLeftCardRect();
                DrawRect(leftRect, new Color(20, 20, 25));
                DrawRectBorder(leftRect, Color.Gray, 3);
                DrawTextCentered($"1-{_session.StoryStageIndex}", new Vector2(ScreenWidth / 2 - 300, leftRect.Bottom - 60), Color.Gray, 1.2f);
            }

            if (_session.StoryStageIndex < MaxStoryStages - 1)
            {
                var rightRect = GetStoryRightCardRect();
                DrawRect(rightRect, new Color(20, 20, 25));
                DrawRectBorder(rightRect, Color.Gray, 3);
                DrawTextCentered($"1-{_session.StoryStageIndex + 2}", new Vector2(ScreenWidth / 2 + 300, rightRect.Bottom - 60), Color.Gray, 1.2f);
            }

            var centerRect = GetStoryCenterCardRect();
            DrawRect(centerRect, _hoveredStoryCenter ? new Color(50, 50, 70) : new Color(30, 30, 45));
            DrawRectBorder(centerRect, _hoveredStoryCenter ? Color.Gold : Color.White, 5);

            DrawTextCentered($"1-{_session.StoryStageIndex + 1}", new Vector2(centerRect.Center.X, centerRect.Bottom - 80), Color.White, 3f);

            if (_hoveredStoryCenter)
                DrawTextCentered("Click to Play", new Vector2(centerRect.Center.X, centerRect.Center.Y), Color.Gold, 1.5f);

            if (_session.StoryStageIndex > 0)
                DrawArrow(GetStoryLeftArrowRect(), true, _hoveredStoryLeft ? Color.Gold : Color.White, 8);

            if (_session.StoryStageIndex < MaxStoryStages - 1)
                DrawArrow(GetStoryRightArrowRect(), false, _hoveredStoryRight ? Color.Gold : Color.White, 8);

            var backRect = GetStoryBackButtonRect();
            DrawRect(backRect, _hoveredStoryBack ? Color.DarkRed : new Color(50, 30, 30));
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

        private void DrawLine(Vector2 start, Vector2 end, Color color, int thickness)
        {
            Vector2 edge = end - start;
            float length = edge.Length();
            if (length < 0.01f) return;
            _spriteBatch.Draw(_pixel, new Rectangle((int)start.X, (int)start.Y, (int)length, thickness), null, color, (float)Math.Atan2(edge.Y, edge.X), Vector2.Zero, SpriteEffects.None, 0);
        }

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