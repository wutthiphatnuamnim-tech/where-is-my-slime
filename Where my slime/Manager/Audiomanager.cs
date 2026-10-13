using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace Where_My_Slime.Manager
{
    public static class AudioManager
    {
        private static readonly Dictionary<string, SoundEffect> _sfx = new Dictionary<string, SoundEffect>();
        private static readonly Dictionary<string, Song> _songs = new Dictionary<string, Song>();
        private static string _currentSongName;

        public static float SfxVolume = 1f;
        public static float MusicVolume = 0.5f;
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
            // ปล่อยให้โหลดตรงๆ ไม่มี try-catch ครอบ เพื่อให้เกมฟ้อง Error ทันทีถ้าโหลดไฟล์ไม่ได้
            _sfx[name] = content.Load<SoundEffect>(name);
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