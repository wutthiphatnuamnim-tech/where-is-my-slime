using System.Collections.Generic;

namespace How.Data
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

        public Character CreateInstance()
        {
            return new Character(Name, Hp, Speed, PAtk, MAtk, PDef, Type, IsEnemy, MDef, Accuracy, CritRate, CritDamage);
        }
    }

    // Index กลาง เก็บข้อมูลตัวละครพันธมิตรและศัตรูทั้งหมดของเกม
    // ต้องการเพิ่ม/แก้สเตตัสตัวละครหรือศัตรูตัวไหน แก้ที่นี่จุดเดียว ไม่ต้องไปตามหาใน BattleManager อีก
    public static class CharacterIndex
    {
        // ---------- ปาร์ตี้พันธมิตรเริ่มต้น (key = id ใช้อ้างอิงตอนสร้างตัวละคร) ----------
        public static readonly Dictionary<string, CharacterDefinition> Allies = new Dictionary<string, CharacterDefinition>
        {
            ["sworman"] = new CharacterDefinition { Id = "sworman", Name = "Sworman", Hp = 1200, Speed = 95, PAtk = 180, MAtk = 20, PDef = 60, Type = AttackType.Physical, IsEnemy = false },
            ["archer"] = new CharacterDefinition { Id = "archer", Name = "Archer", Hp = 950, Speed = 115, PAtk = 160, MAtk = 30, PDef = 40, Type = AttackType.Physical, IsEnemy = false },
            ["mage"] = new CharacterDefinition { Id = "mage", Name = "Mage", Hp = 850, Speed = 80, PAtk = 30, MAtk = 200, PDef = 35, Type = AttackType.Magic, IsEnemy = false },
            ["cleric"] = new CharacterDefinition { Id = "cleric", Name = "Cleric", Hp = 1000, Speed = 90, PAtk = 40, MAtk = 140, PDef = 50, Type = AttackType.Magic, IsEnemy = false },
            ["knight"] = new CharacterDefinition { Id = "knight", Name = "Knight", Hp = 1500, Speed = 70, PAtk = 150, MAtk = 20, PDef = 90, Type = AttackType.Physical, IsEnemy = false },
        };

        // ---------- ศัตรูทั้งหมดที่มีในเกม ----------
        public static readonly Dictionary<string, CharacterDefinition> Enemies = new Dictionary<string, CharacterDefinition>
        {
            ["orc_warrior"] = new CharacterDefinition { Id = "orc_warrior", Name = "Orc Warrior", Hp = 1100, Speed = 85, PAtk = 170, MAtk = 20, PDef = 55, Type = AttackType.Physical, IsEnemy = true },
            ["goblin_archer"] = new CharacterDefinition { Id = "goblin_archer", Name = "Goblin Archer", Hp = 900, Speed = 110, PAtk = 150, MAtk = 25, PDef = 35, Type = AttackType.Physical, IsEnemy = true },
            ["dark_mage"] = new CharacterDefinition { Id = "dark_mage", Name = "Dark Mage", Hp = 800, Speed = 90, PAtk = 25, MAtk = 190, PDef = 30, Type = AttackType.Magic, IsEnemy = true },
            ["ghost_healer"] = new CharacterDefinition { Id = "ghost_healer", Name = "Ghost Healer", Hp = 950, Speed = 75, PAtk = 35, MAtk = 150, PDef = 45, Type = AttackType.Magic, IsEnemy = true },
            ["stone_giant"] = new CharacterDefinition { Id = "stone_giant", Name = "Stone Giant", Hp = 1600, Speed = 60, PAtk = 160, MAtk = 10, PDef = 95, Type = AttackType.Physical, IsEnemy = true },
        };

        // ลำดับศัตรูที่จะพบในด่านปกติ (default wave) — เพิ่ม wave ใหม่ๆ ได้โดยสร้าง string[] ชุดใหม่แล้วส่งเข้า CreateEnemyWave
        public static readonly string[] DefaultEnemyWave =
            { "orc_warrior", "goblin_archer", "dark_mage", "ghost_healer", "stone_giant" };

        public static Character CreateAlly(string id) => Allies[id].CreateInstance();

        public static Character CreateEnemy(string id) => Enemies[id].CreateInstance();

        public static List<Character> CreateStartingParty()
        {
            var party = new List<Character>();
            foreach (var def in Allies.Values)
                party.Add(def.CreateInstance());
            return party;
        }

        // ไม่ส่ง waveIds มา = ใช้ wave เริ่มต้น (DefaultEnemyWave)
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