using System;

namespace How.Data
{
    public enum AttackType
    {
        Physical, // ใช้ P.Atk 
        Magic     // ใช้ M.Atk 
    }

    // สเตตัสโตที่ผู้เล่นแจกแต้มได้ (STR/INT/VIT/DEX/LUX/CRT)
    public enum GrowthStat
    {
        STR, // เพิ่ม P.Atk
        INT, // เพิ่ม M.Atk และ M.Def
        VIT, // เพิ่ม HP และ P.Def
        DEX, // เพิ่มความแม่นยำ (Accuracy)
        LUX, // เพิ่มอัตราคริติคอล (CritRate)
        CRT  // เพิ่มดาเมจคริติคอล (CritDamage)
    }

    // สเตตัสหลักที่การ์ด (UpgradeCard) และไอเทม (Item) สามารถปรับได้ ทั้งแบบ % และแบบหน่วยตรงๆ
    public enum StatKind
    {
        PAtk,
        MAtk,
        AllAtk,   // ปรับทั้ง PAtk และ MAtk พร้อมกัน (การ์ด "เพิ่ม ALL.ATK")
        MaxHp,
        Speed,
        CritRate,
        CritDamage,
        Accuracy,
        PDef,
        MDef
    }

    public struct AttackResult
    {
        public int Damage;
        public bool IsCrit;
        public bool IsMiss;
    }

    /// <summary>
    /// ตัวละครหนึ่งตัว (ทั้งฝั่งพันธมิตรและศัตรู)
    /// ใช้ระบบ Speed Gauge แบบ ATB คล้าย 7 Knights
    /// สเตตัสสุดท้าย (Final Stat) = สเตตัสฐาน (Base) + โบนัสจากแต้มโต (Growth) แล้วค่อยรวมโบนัสจาก Card/Item ทับบน Base อีกที
    /// Card (UpgradeCard) ปรับ Base แบบถาวร ส่วน Item (จากหน้า Rest) ปรับ Base แบบถอดออกได้ (Equip/Unequip)
    /// </summary>
    public class Character
    {
        public string Name;
        public bool IsEnemy;

        // ---------- สเตตัสฐาน (Base) : ปรับถาวรได้จาก Card, ใช้เป็นฐานคำนวณ Final Stat ทุกครั้ง ----------
        private int _baseMaxHp;
        private int _baseSpeed;
        private int _basePAtk;
        private int _baseMAtk;
        private int _basePDef;
        private int _baseMDef;
        private float _baseAccuracy;   // % โอกาสตีโดน (0-100)
        private float _baseCritRate;   // % โอกาสคริติคอล (0-100)
        private float _baseCritDamage; // % ดาเมจโบนัสตอนคริ (เช่น 50 = คริแล้วแรงขึ้น 50%)

        // ---------- แต้มโต (Growth) : ผู้เล่นแจกแต้มหลังผ่านด่านได้ ----------
        public int STR;
        public int INT;
        public int VIT;
        public int DEX;
        public int LUX;
        public int CRT;

        // ---------- Level/Exp: ตัวละครแต่ละคนโตแยกกัน จบด่านได้ Exp เข้าตัวเอง เลเวลอัพแล้วได้แต้มโตของตัวเอง ----------
        public int Level { get; private set; } = 1;
        public int Exp { get; private set; }
        public int UpgradePoints { get; private set; } // แต้มโตที่ยังไม่ได้ใช้ (เฉพาะของตัวละครนี้คนเดียว)

        public const int ExpPerLevel = 100;   // Exp ที่ต้องใช้เพื่อเลเวลอัพ 1 ระดับ
        public const int PointsPerLevelUp = 3; // แต้มโตที่ได้ต่อการเลเวลอัพ 1 ครั้ง

        // ---------- สเตตัสสุดท้าย (คำนวณจาก Base + Growth ทุกครั้งที่ Recalculate) ----------
        public int MaxHp { get; private set; }
        public int Speed { get; private set; }
        public int PAtk { get; private set; }
        public int MAtk { get; private set; }
        public int PDef { get; private set; }
        public int MDef { get; private set; }
        public float Accuracy { get; private set; }
        public float CritRate { get; private set; }
        public float CritDamage { get; private set; }

        public int Hp; // เลือดปัจจุบัน

        public AttackType Type;

        public float Gauge;          // เกจสะสมความเร็ว
        public const float GaugeMax = 1000f; // เกณฑ์ที่ต้องถึงเพื่อโจมตี

        // ---------- ไอเทมที่สวมใส่อยู่ (หน้า Rest) : สวมได้ทีละ 1 ชิ้น ถอดแล้วสเตตัสกลับเหมือนก่อนสวม ----------
        public Item EquippedItem { get; private set; }
        private StatKind _equippedItemStat;
        private float _equippedItemDelta; // เก็บ "ค่าจริง" ที่ถูกบวกเข้าไปตอนสวม (หน่วยตรงๆ) ไว้หักออกให้แม่นยำตอนถอด

        // ตัวคูณสเตตัสต่อ 1 แต้มโต (ปรับ balance ได้ตรงนี้จุดเดียว)
        private const float StrToPAtk = 4f;
        private const float IntToMAtk = 4f;
        private const float IntToMDef = 2f;
        private const float VitToHp = 12f;
        private const float VitToPDef = 2f;
        private const float DexToAccuracy = 1.2f;
        private const float LuxToCritRate = 1f;
        private const float CrtToCritDamage = 2f;

        public Character(string name, int hp, int speed, int pAtk, int mAtk, int pDef, AttackType type, bool isEnemy,
            int mDef = 0, float accuracy = 90f, float critRate = 5f, float critDamage = 50f)
        {
            Name = name;
            _baseMaxHp = hp;
            _baseSpeed = speed;
            _basePAtk = pAtk;
            _baseMAtk = mAtk;
            _basePDef = pDef;
            _baseMDef = mDef == 0 ? pDef : mDef; // ถ้าไม่ระบุ M.Def ให้ใช้ค่าเดียวกับ P.Def ไปก่อน
            _baseAccuracy = accuracy;
            _baseCritRate = critRate;
            _baseCritDamage = critDamage;

            Type = type;
            IsEnemy = isEnemy;
            Gauge = 0f;

            RecalculateStats();
            Hp = MaxHp;
        }

        public bool IsAlive => Hp > 0;

        /// <summary>
        /// อัพเดตเกจความเร็วต่อเฟรม คืนค่า true ถ้าเกจเต็มแล้ว (พร้อมโจมตี)
        /// </summary>
        public bool TickGauge(float deltaSeconds)
        {
            if (!IsAlive) return false;
            Gauge += Speed * deltaSeconds * 20f;
            if (Gauge >= GaugeMax)
            {
                Gauge = 0f;
                return true;
            }
            return false;
        }

        // คำนวณสเตตัสสุดท้ายใหม่ทั้งหมดจาก Base + แต้มโต เรียกทุกครั้งที่ Base หรือ Growth เปลี่ยน
        public void RecalculateStats()
        {
            MaxHp = _baseMaxHp + (int)(VIT * VitToHp);
            Speed = _baseSpeed;
            PAtk = _basePAtk + (int)(STR * StrToPAtk);
            MAtk = _baseMAtk + (int)(INT * IntToMAtk);
            PDef = _basePDef + (int)(VIT * VitToPDef);
            MDef = _baseMDef + (int)(INT * IntToMDef);
            Accuracy = Math.Min(100f, _baseAccuracy + DEX * DexToAccuracy);
            CritRate = Math.Min(100f, _baseCritRate + LUX * LuxToCritRate);
            CritDamage = _baseCritDamage + CRT * CrtToCritDamage;

            if (MaxHp < 1) MaxHp = 1;
            if (Hp > MaxHp) Hp = MaxHp;
        }

        // เพิ่ม Exp ให้ตัวละครนี้ (เรียกตอนจบด่าน) เลเวลอัพได้หลายระดับพร้อมกันถ้า Exp เกินหลายเท่า
        public void AddExp(int amount)
        {
            if (amount <= 0) return;
            Exp += amount;
            while (Exp >= ExpPerLevel)
            {
                Exp -= ExpPerLevel;
                Level++;
                UpgradePoints += PointsPerLevelUp;
            }
        }

        // ใช้แต้มโต 1 แต้มลงสเตตัสที่เลือก แล้วคำนวณใหม่ทันที ต้องมีแต้มโตของตัวเองเหลือพอ (คืน false ถ้าไม่มีแต้ม)
        public bool ApplyGrowthPoint(GrowthStat stat)
        {
            if (UpgradePoints <= 0) return false;

            switch (stat)
            {
                case GrowthStat.STR: STR++; break;
                case GrowthStat.INT: INT++; break;
                case GrowthStat.VIT: VIT++; break;
                case GrowthStat.DEX: DEX++; break;
                case GrowthStat.LUX: LUX++; break;
                case GrowthStat.CRT: CRT++; break;
            }
            UpgradePoints--;
            RecalculateStats();
            return true;
        }

        // ใช้โดย Card/Item: ปรับสเตตัส "ฐาน" (ทั้งแบบ % และแบบหน่วยตรงๆ) แล้วคำนวณใหม่
        // isPercent=true คือปรับเป็น % ของค่าฐานปัจจุบัน (เช่น +15% หรือ -20%), false คือบวก/ลบเป็นหน่วยตรงๆ
        public void ModifyBaseStat(StatKind kind, float amount, bool isPercent)
        {
            switch (kind)
            {
                case StatKind.PAtk:
                    _basePAtk = isPercent ? (int)(_basePAtk * (1 + amount / 100f)) : _basePAtk + (int)amount;
                    break;
                case StatKind.MAtk:
                    _baseMAtk = isPercent ? (int)(_baseMAtk * (1 + amount / 100f)) : _baseMAtk + (int)amount;
                    break;
                case StatKind.AllAtk:
                    _basePAtk = isPercent ? (int)(_basePAtk * (1 + amount / 100f)) : _basePAtk + (int)amount;
                    _baseMAtk = isPercent ? (int)(_baseMAtk * (1 + amount / 100f)) : _baseMAtk + (int)amount;
                    break;
                case StatKind.MaxHp:
                    _baseMaxHp = isPercent ? (int)(_baseMaxHp * (1 + amount / 100f)) : _baseMaxHp + (int)amount;
                    if (_baseMaxHp < 1) _baseMaxHp = 1;
                    break;
                case StatKind.Speed:
                    _baseSpeed = isPercent ? (int)(_baseSpeed * (1 + amount / 100f)) : _baseSpeed + (int)amount;
                    if (_baseSpeed < 1) _baseSpeed = 1;
                    break;
                case StatKind.CritRate:
                    _baseCritRate = isPercent ? _baseCritRate * (1 + amount / 100f) : _baseCritRate + amount;
                    break;
                case StatKind.CritDamage:
                    _baseCritDamage = isPercent ? _baseCritDamage * (1 + amount / 100f) : _baseCritDamage + amount;
                    break;
                case StatKind.Accuracy:
                    _baseAccuracy = isPercent ? _baseAccuracy * (1 + amount / 100f) : _baseAccuracy + amount;
                    break;
                case StatKind.PDef:
                    _basePDef = isPercent ? (int)(_basePDef * (1 + amount / 100f)) : _basePDef + (int)amount;
                    break;
                case StatKind.MDef:
                    _baseMDef = isPercent ? (int)(_baseMDef * (1 + amount / 100f)) : _baseMDef + (int)amount;
                    break;
            }

            RecalculateStats();
        }

        // อ่านค่าสเตตัส "ฐาน" ดิบๆ ตาม StatKind (ใช้ภายในสำหรับคำนวณ delta ตอนสวม/ถอดไอเทม)
        private float GetBaseStatValue(StatKind kind)
        {
            switch (kind)
            {
                case StatKind.PAtk: return _basePAtk;
                case StatKind.MAtk: return _baseMAtk;
                case StatKind.MaxHp: return _baseMaxHp;
                case StatKind.Speed: return _baseSpeed;
                case StatKind.CritRate: return _baseCritRate;
                case StatKind.CritDamage: return _baseCritDamage;
                case StatKind.Accuracy: return _baseAccuracy;
                case StatKind.PDef: return _basePDef;
                case StatKind.MDef: return _baseMDef;
                default: return 0f; // AllAtk ไม่รองรับสำหรับไอเทม (ใช้ได้เฉพาะการ์ด)
            }
        }

        // สวมไอเทม 1 ชิ้น (ถอดของเดิมออกก่อนอัตโนมัติถ้ามี) แล้วบวกโบนัสเข้า Base
        // เก็บ delta จริงที่ถูกบวกไว้ เพื่อให้ถอดออกภายหลังได้แม่นยำ ไม่ว่าไอเทมจะเป็น % หรือหน่วยตรงๆ
        public void EquipItem(Item item)
        {
            if (item == null) return;
            if (EquippedItem != null)
                UnequipItem();

            float before = GetBaseStatValue(item.Stat);
            ModifyBaseStat(item.Stat, item.Amount, item.IsPercent);
            float after = GetBaseStatValue(item.Stat);

            _equippedItemStat = item.Stat;
            _equippedItemDelta = after - before;
            EquippedItem = item;
        }

        // ถอดไอเทมที่สวมอยู่ออก คืนสเตตัสฐานกลับไปเท่าก่อนสวม
        public void UnequipItem()
        {
            if (EquippedItem == null) return;
            ModifyBaseStat(_equippedItemStat, -_equippedItemDelta, isPercent: false);
            EquippedItem = null;
            _equippedItemDelta = 0f;
        }

        // ลด HP ปัจจุบันเป็น % (ใช้โดยการ์ดที่แลกเลือดกับพลัง เช่น "ลด HP 90%")
        public void ReduceCurrentHpPercent(float percent)
        {
            int amount = (int)(MaxHp * (percent / 100f));
            Hp = Math.Max(1, Hp - amount); // กันไม่ให้การ์ดฆ่าตัวละครตายทันที เหลืออย่างน้อย 1 HP
        }

        /// <summary>
        /// คำนวณผลการโจมตีใส่เป้าหมาย: เช็คโอกาสพลาด (Accuracy) ก่อน แล้วค่อยเช็คคริติคอล (CritRate/CritDamage)
        /// </summary>
        public AttackResult CalculateAttack(Character target, Random rng)
        {
            if (rng.NextDouble() * 100 > Accuracy)
                return new AttackResult { Damage = 0, IsCrit = false, IsMiss = true };

            int atkStat = Type == AttackType.Physical ? PAtk : MAtk;
            int defStat = Type == AttackType.Physical ? target.PDef : target.MDef;

            int baseDamage = atkStat - (int)(defStat * 0.5f);
            if (baseDamage < 1) baseDamage = 1;

            float variance = 0.9f + (float)rng.NextDouble() * 0.2f;
            float damage = baseDamage * variance;

            bool isCrit = rng.NextDouble() * 100 < CritRate;
            if (isCrit)
                damage *= 1f + CritDamage / 100f;

            int finalDamage = Math.Max(1, (int)damage);
            return new AttackResult { Damage = finalDamage, IsCrit = isCrit, IsMiss = false };
        }

        public void TakeDamage(int amount)
        {
            Hp -= amount;
            if (Hp < 0) Hp = 0;
        }
    }
}