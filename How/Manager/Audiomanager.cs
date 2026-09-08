using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace How.Manager
{
    // ตัวกลางจัดการเสียงทั้งหมดของเกม: เสียงสั้น (SoundEffect) และเพลงประกอบ (Song ผ่าน MediaPlayer)
    // โหลดแบบปลอดภัยเหมือน pattern โหลดฟอนต์ใน Game1 — ถ้ายังไม่มีไฟล์เสียงใน Content จะข้ามไปเงียบๆ ไม่ทำเกม crash
    // วิธีเพิ่มไฟล์เสียงจริง: เอาไฟล์ .wav/.mp3 ไปวางใน Content/Audio แล้วเปิด Content.mgcb ด้วย MGCB Editor
    //   - เสียงสั้น (SFX): Add Existing Item -> เลือกไฟล์ .wav -> Importer/Processor จะเป็น "Sound Effect" อัตโนมัติ
    //   - เพลงประกอบ (BGM): Add Existing Item -> เลือกไฟล์ .mp3/.ogg/.wav -> Importer/Processor จะเป็น "Song"
    // แล้วเรียก AudioManager.LoadAll(Content) ตอน LoadContent() ของ Game1 ครั้งเดียว
    public static class AudioManager
    {
        private static readonly Dictionary<string, SoundEffect> _sfx = new Dictionary<string, SoundEffect>();
        private static readonly Dictionary<string, Song> _songs = new Dictionary<string, Song>();
        private static string _currentSongName; // เพลงที่กำลังเล่นอยู่ตอนนี้ กันไม่ให้ restart ซ้ำถ้าขอเพลงเดิม

        public static float SfxVolume = 0.8f;
        public static float MusicVolume = 0.5f;
        public static bool MusicEnabled = true;
        public static bool SfxEnabled = true;

        // เรียกครั้งเดียวใน Game1.LoadContent() หลังโหลดฟอนต์
        // sfxNames / songNames คือชื่อไฟล์ (ไม่ต้องมีนามสกุล) ตาม path ที่ตั้งไว้ใน Content.mgcb เช่น "Audio/hit", "Audio/click"
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
                // ยังไม่มีไฟล์เสียงนี้ใน Content ก็ข้ามไปเงียบๆ (จะไม่มีเสียงเล่นตอนเรียก PlaySfx ด้วยชื่อนี้)
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
                // ยังไม่มีไฟล์เพลงนี้ใน Content ก็ข้ามไปเงียบๆ
            }
        }

        // เล่นเสียงสั้นหนึ่งครั้ง (ตี, คลิกปุ่ม, เลเวลอัพ ฯลฯ) — ไม่มีผลอะไรถ้ายังไม่ได้โหลดไฟล์ชื่อนี้
        public static void PlaySfx(string name, float volumeScale = 1f)
        {
            if (!SfxEnabled) return;
            if (_sfx.TryGetValue(name, out var sfx))
                sfx.Play(SfxVolume * volumeScale, 0f, 0f);
        }

        // เล่นเพลงประกอบ (วนลูปตามค่า loop) — ถ้าเพลงชื่อเดียวกันกำลังเล่นอยู่แล้วจะไม่ restart ซ้ำ (กันสะดุดตอนสลับหน้าจอถี่ๆ)
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

        public static void StopMusic()
        {
            MediaPlayer.Stop();
            _currentSongName = null;
        }

        // สลับเปิด/ปิดเพลงประกอบทั้งหมด (ปุ่ม mute ในเกม) — ปิดแล้วเพลงที่กำลังเล่นจะหยุดทันที
        public static void SetMusicEnabled(bool enabled)
        {
            MusicEnabled = enabled;
            if (!enabled) MediaPlayer.Stop();
        }

        public static void SetSfxEnabled(bool enabled) => SfxEnabled = enabled;
    }
}