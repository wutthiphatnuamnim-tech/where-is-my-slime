using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace How.Manager
{
    // ตัวกลางจัดการเสียงทั้งหมดของเกม: เสียงสั้น (SoundEffect) และเพลงประกอบ (Song ผ่าน MediaPlayer)
    public static class AudioManager
    {
        private static readonly Dictionary<string, SoundEffect> _sfx = new Dictionary<string, SoundEffect>();
        private static readonly Dictionary<string, Song> _songs = new Dictionary<string, Song>();
        private static string _currentSongName; // เพลงที่กำลังเล่นอยู่ตอนนี้ กันไม่ให้ restart ซ้ำถ้าขอเพลงเดิม

        public static float SfxVolume = 0.8f;
        public static float MusicVolume = 0.2f;
        public static bool MusicEnabled = true;
        public static bool SfxEnabled = true;

        public static void LoadAll(ContentManager content, string[] sfxNames, string[] songNames)
        {
            foreach (var name in sfxNames)
                TryLoadSfx(content, name);

            foreach (var name in songNames)
                TryLoadSong(content, name);
        }

        private static void TryLoadSfx(ContentManager content, string name)
        {
            try
            {
                _sfx[name] = content.Load<SoundEffect>(name);
            }
            catch
            {
            }
        }

        private static void TryLoadSong(ContentManager content, string name)
        {
            try
            {
                _songs[name] = content.Load<Song>(name);
            }
            catch
            {
            }
        }

        public static void PlaySfx(string name, float volumeScale = 1f)
        {
            if (!SfxEnabled) return;
            if (_sfx.TryGetValue(name, out var sfx))
                sfx.Play(SfxVolume * volumeScale, 0f, 0f);
        }

        public static void PlayMusic(string name, bool loop = true)
        {
            if (!MusicEnabled) return;
            if (!_songs.TryGetValue(name, out var song)) return;

            if (_currentSongName == name && MediaPlayer.State == MediaState.Playing)
                return;

            MediaPlayer.IsRepeating = loop;
            MediaPlayer.Volume = MusicVolume;
            MediaPlayer.Play(song);
            _currentSongName = name;
        }

        // เพิ่มเมธอด PlayBgm เพื่อรองรับการเรียกใช้งานจาก Game1.cs
        public static void PlayBgm(string name, bool loop = true)
        {
            PlayMusic(name, loop);
        }

        public static void StopMusic()
        {
            MediaPlayer.Stop();
            _currentSongName = null;
        }

        public static void SetMusicEnabled(bool enabled)
        {
            MusicEnabled = enabled;
            if (!enabled) MediaPlayer.Stop();
        }

        public static void SetSfxEnabled(bool enabled) => SfxEnabled = enabled;
    }
}