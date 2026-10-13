using System.Collections.Generic;
using Where_My_Slime.Models;

namespace Where_My_Slime.Data
{
    // "ต้นแบบ" (template) ของตัวละคร/ศัตรูหนึ่งตัว เก็บค่าสเตตัสตั้งต้นไว้ที่เดียว
    // แล้วค่อยสั่ง CreateInstance() เพื่อสร้าง Character จริงตอนเริ่มเกม/ด่านใหม่
    public class CharacterDefinition
    {
        public string Id;
        public string Name;
        public int Hp;
        public int Speed;
        public int PAtk;
        public int MAtk;
        public int PDef;
        public int MDef;
        public float Accuracy = 90f;
        public float CritRate = 5f;
        public float CritDamage = 50f;
        public AttackType Type;
        public bool IsEnemy;

        // --- เพิ่มตัวแปรจัดการ SP และ Ultimate ---
        public int CurrentSP { get; set; }
        public int MaxSP { get; set; }

        // --- ตัวแปรสำหรับสถานะ (Buff/Debuff) ---
        public int TauntDuration { get; set; } // จำนวนเทิร์นที่โดน Elma ยั่วยุ
        public int AliceBuffDuration { get; set; } // ระยะเวลาบัฟของ Alice (เปลี่ยนจาก Swordman)

        // ฟังก์ชันเซ็ตค่า SP เริ่มต้น (เรียกใช้ตอนสร้างตัวละคร)
        public void InitializeSP()
        {
            CurrentSP = 0;
            if (Name == "Elma") MaxSP = 100;
            else if (Name == "Cleric") MaxSP = 120;
            else if (Name == "Lumi") MaxSP = 150;
            else if (Name == "Archer") MaxSP = 200;
            else if (Name == "Alice") MaxSP = 80; // เปลี่ยนจาก Swordman เป็น Alice
            else MaxSP = 100; // ค่าเริ่มต้นเผื่อตัวอื่น
        }

        // --- ดึงสเตตัสจริงที่รวมบัฟของ Alice แล้ว ไปใช้คำนวณดาเมจในเกม ---
        public int CurrentPAtk => PAtk + (AliceBuffDuration > 0 ? (int)(PAtk * 0.3f) : 0);
        public int CurrentPDef => PDef + (AliceBuffDuration > 0 ? (int)(PDef * 0.3f) : 0);
        public int CurrentSpeed => Speed + (AliceBuffDuration > 0 ? (int)(Speed * 0.3f) : 0);
        public float CurrentCritRate => CritRate + (AliceBuffDuration > 0 ? 30f : 0f);
        public float CurrentCritDamage => CritDamage + (AliceBuffDuration > 0 ? 120f : 0f);

        // กำหนดชื่อเท็กซ์เจอร์เริ่มต้นให้ตรงกับไฟล์ภาพของ Lumi
        public string IdleTextureName = "Char_Lumi_IDLE";
        public string AttackTextureName = "Char_Lumi_ATTACK";

        public Character CreateInstance()
        {
            return new Character(Name, Hp, Speed, PAtk, MAtk, PDef, Type, IsEnemy, MDef, Accuracy, CritRate, CritDamage);
        }
    }

    // Index กลาง เก็บข้อมูลตัวละครพันธมิตรและศัตรูทั้งหมดของเกม
    public static class CharacterIndex
    {
        // ---------- ปาร์ตี้พันธมิตรเริ่มต้น (เปลี่ยน Swordman เป็น Alice) ----------
        public static readonly Dictionary<string, CharacterDefinition> Allies = new Dictionary<string, CharacterDefinition>
        {
            ["alice"] = new CharacterDefinition { Id = "alice", Name = "Alice", Hp = 1200, Speed = 95, PAtk = 320, MAtk = 20, PDef = 60, Type = AttackType.Physical, IsEnemy = false },
            ["archer"] = new CharacterDefinition { Id = "archer", Name = "Archer", Hp = 950, Speed = 115, PAtk = 400, MAtk = 30, PDef = 40, Type = AttackType.Physical, IsEnemy = false },
            ["lumi"] = new CharacterDefinition { Id = "lumi", Name = "Lumi", Hp = 850, Speed = 80, PAtk = 30, MAtk = 380, PDef = 35, Type = AttackType.Magic, IsEnemy = false, IdleTextureName = "Char_Lumi_IDLE", AttackTextureName = "Char_Lumi_ATTACK" },
            ["cleric"] = new CharacterDefinition { Id = "cleric", Name = "Cleric", Hp = 1000, Speed = 90, PAtk = 40, MAtk = 140, PDef = 50, Type = AttackType.Magic, IsEnemy = false },
            ["elma"] = new CharacterDefinition { Id = "elma", Name = "Elma", Hp = 1250, Speed = 70, PAtk = 200, MAtk = 120, PDef = 200, Type = AttackType.Physical, IsEnemy = false }
        };

        // ---------- ศัตรูทั้งหมดที่มีในเกม ----------
        public static readonly Dictionary<string, CharacterDefinition> Enemies = new Dictionary<string, CharacterDefinition>
        {
            ["orc_warrior"] = new CharacterDefinition { Id = "orc_warrior", Name = "Orc Warrior", Hp = 970, Speed = 85, PAtk = 170, MAtk = 20, PDef = 55, Type = AttackType.Physical, IsEnemy = true },
            ["giant_hornet"] = new CharacterDefinition { Id = "giant_hornet", Name = "Giant Hornet", Hp = 900, Speed = 110, PAtk = 150, MAtk = 25, PDef = 35, Type = AttackType.Physical, IsEnemy = true },
            ["dark_mage"] = new CharacterDefinition { Id = "dark_mage", Name = "Dark Mage", Hp = 800, Speed = 90, PAtk = 25, MAtk = 190, PDef = 30, Type = AttackType.Magic, IsEnemy = true },
            ["ghost_healer"] = new CharacterDefinition { Id = "ghost_healer", Name = "Ghost Healer", Hp = 950, Speed = 75, PAtk = 35, MAtk = 150, PDef = 45, Type = AttackType.Magic, IsEnemy = true },
            ["stone_giant"] = new CharacterDefinition { Id = "stone_giant", Name = "Stone Giant", Hp = 1600, Speed = 60, PAtk = 160, MAtk = 10, PDef = 95, Type = AttackType.Physical, IsEnemy = true },

            // บอสประจำ Challenge Mode: เจอทุกๆ 5 ชั้น เป็นศัตรูตัวเดียวแต่สเตตัสสูงกว่าศัตรูทั่วไปมาก
            ["boss_slime_king"] = new CharacterDefinition { Id = "boss_slime_king", Name = "Boss Slime King", Hp = 6000, Speed = 80, PAtk = 260, MAtk = 220, PDef = 110, MDef = 100, Type = AttackType.Physical, IsEnemy = true },
        };

        public static readonly string[] DefaultEnemyWave =
            { "orc_warrior", "giant_hornet", "dark_mage", "ghost_healer", "stone_giant" };

        public static Character CreateAlly(string id) => Allies[id].CreateInstance();

        public static Character CreateEnemy(string id) => Enemies[id].CreateInstance();

        public static List<Character> CreateStartingParty()
        {
            var party = new List<Character>();
            foreach (var def in Allies.Values)
                party.Add(def.CreateInstance());
            return party;
        }

        public static List<Character> CreateEnemyWave(string[] waveIds = null)
        {
            waveIds = waveIds ?? DefaultEnemyWave;
            var wave = new List<Character>();
            foreach (var id in waveIds)
                wave.Add(Enemies[id].CreateInstance());
            return wave;
        }
    }
}