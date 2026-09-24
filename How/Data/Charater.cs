using How.Data;
using System;

namespace How.Models
{
    public enum AttackType
    {
        Physical, // ใช้ P.Atk โจมตี (นักดาบ, นักธนู ฯลฯ)
        Magic     // ใช้ M.Atk โจมตี (นักเวท ฯลฯ)
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
        AllAtk,   // ปรับทั้ง PAtk และ MAtk พร้อมกัน
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

    public class Character
    {
        public string Name;
        public bool IsEnemy;

        // ---------- สเตตัสฐาน (Base) ----------
        private int _baseMaxHp;
        private int _baseSpeed;
        private int _basePAtk;
        private int _baseMAtk;
        private int _basePDef;
        private int _baseMDef;
        private float _baseAccuracy;
        private float _baseCritRate;
        private float _baseCritDamage;

        // ---------- แต้มโต (Growth) ----------
        public int STR;
        public int INT;
        public int VIT;
        public int DEX;
        public int LUX;
        public int CRT;

        public int UpgradePoints { get; set; }

        // ---------- สเตตัสสุดท้าย ----------
        public int MaxHp { get; private set; }
        public int Speed { get; private set; }
        public int PAtk { get; private set; }
        public int MAtk { get; private set; }
        public int PDef { get; private set; }
        public int MDef { get; private set; }
        public float Accuracy { get; private set; }
        public float CritRate { get; private set; }
        public float CritDamage { get; private set; }

        public int Hp;
        public AttackType Type;

        public float Gauge;
        public const float GaugeMax = 1000f;

        // ไอเท็มที่สวมใส่อยู่
        public Item EquippedItem { get; private set; }

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
            _baseMDef = mDef == 0 ? pDef : mDef;
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

        // คำนวณสเตตัสสุดท้ายใหม่ทั้งหมด
        public void RecalculateStats()
        {
            int hpCalc = _baseMaxHp + (int)(VIT * VitToHp);
            int speedCalc = _baseSpeed;
            int pAtkCalc = _basePAtk + (int)(STR * StrToPAtk);
            int mAtkCalc = _baseMAtk + (int)(INT * IntToMAtk);
            int pDefCalc = _basePDef + (int)(VIT * VitToPDef);
            int mDefCalc = _baseMDef + (int)(INT * IntToMDef);
            float accCalc = Math.Min(100f, _baseAccuracy + DEX * DexToAccuracy);
            float critRateCalc = Math.Min(100f, _baseCritRate + LUX * LuxToCritRate);
            float critDmgCalc = _baseCritDamage + CRT * CrtToCritDamage;

            // ประมวลผลโบนัสจากไอเท็มที่สวมใส่
            if (EquippedItem != null)
            {
                void ApplyStat(StatKind kind, float amount, bool isPercent)
                {
                    switch (kind)
                    {
                        case StatKind.PAtk: pAtkCalc = isPercent ? (int)(pAtkCalc * (1 + amount)) : pAtkCalc + (int)amount; break;
                        case StatKind.MAtk: mAtkCalc = isPercent ? (int)(mAtkCalc * (1 + amount)) : mAtkCalc + (int)amount; break;
                        case StatKind.MaxHp: hpCalc = isPercent ? (int)(hpCalc * (1 + amount)) : hpCalc + (int)amount; break;
                        case StatKind.Speed: speedCalc = isPercent ? (int)(speedCalc * (1 + amount)) : speedCalc + (int)amount; break;
                        case StatKind.CritRate: critRateCalc = isPercent ? critRateCalc * (1 + amount) : critRateCalc + amount; break;
                        case StatKind.CritDamage: critDmgCalc = isPercent ? critDmgCalc * (1 + amount) : critDmgCalc + amount; break;
                        case StatKind.Accuracy: accCalc = isPercent ? accCalc * (1 + amount) : accCalc + amount; break;
                        case StatKind.PDef: pDefCalc = isPercent ? (int)(pDefCalc * (1 + amount)) : pDefCalc + (int)amount; break;
                        case StatKind.MDef: mDefCalc = isPercent ? (int)(mDefCalc * (1 + amount)) : mDefCalc + (int)amount; break;
                    }
                }

                // 1. สเตตัสหลัก
                ApplyStat(EquippedItem.Stat, EquippedItem.Amount, EquippedItem.IsPercent);

                // 2. สเตตัสรอง
                if (EquippedItem.SecondaryAmount != 0 || EquippedItem.SecondaryStat != default(StatKind))
                {
                    ApplyStat(EquippedItem.SecondaryStat, EquippedItem.SecondaryAmount, EquippedItem.SecondaryIsPercent);
                }

                // 3. สเตตัสที่สาม
                if (EquippedItem.TertiaryAmount != 0 || EquippedItem.TertiaryStat != default(StatKind))
                {
                    ApplyStat(EquippedItem.TertiaryStat, EquippedItem.TertiaryAmount, EquippedItem.TertiaryIsPercent);
                }

                // เงื่อนไขพิเศษและโบนัสตามสเตตัส
                if (EquippedItem.Name == "Ancient Katana" && STR > 20)
                {
                    pAtkCalc = (int)(pAtkCalc * 1.14f);
                }
                else if (EquippedItem.Name == "Ancient Staff" && INT > 20)
                {
                    mAtkCalc = (int)(mAtkCalc * 1.14f);
                }
                else if (EquippedItem.Name == "Demon Eye" && critRateCalc > 30)
                {
                    critRateCalc += 100f;
                }
                else if (EquippedItem.Name == "Strongest Protection Necklace")
                {
                    float vitBonus = VIT * 0.013f;
                    pAtkCalc = (int)(pAtkCalc * (1 + vitBonus));
                    mAtkCalc = (int)(mAtkCalc * (1 + vitBonus));
                }
                else if (EquippedItem.Name == "Ring of Legendary Hero" && critRateCalc > 50)
                {
                    critRateCalc += 50f;
                    critDmgCalc += 50f;
                }
            }

            MaxHp = Math.Max(1, hpCalc);
            Speed = Math.Max(1, speedCalc);
            PAtk = pAtkCalc;
            MAtk = mAtkCalc;
            PDef = pDefCalc;
            MDef = mDefCalc;
            Accuracy = Math.Min(100f, accCalc);
            CritRate = Math.Min(100f, critRateCalc);
            CritDamage = critDmgCalc;

            if (Hp > MaxHp) Hp = MaxHp;
        }

        public void ApplyGrowthPoint(GrowthStat stat)
        {
            switch (stat)
            {
                case GrowthStat.STR: STR++; break;
                case GrowthStat.INT: INT++; break;
                case GrowthStat.VIT: VIT++; break;
                case GrowthStat.DEX: DEX++; break;
                case GrowthStat.LUX: LUX++; break;
                case GrowthStat.CRT: CRT++; break;
            }
            RecalculateStats();
        }

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

        public void EquipItem(Item item)
        {
            if (item == null) return;
            EquippedItem = item;
            RecalculateStats();
        }

        public void UnequipItem()
        {
            if (EquippedItem == null) return;
            EquippedItem = null;
            RecalculateStats();
        }

        public void ReduceCurrentHpPercent(float percent)
        {
            int amount = (int)(MaxHp * (percent / 100f));
            Hp = Math.Max(1, Hp - amount);
        }

        public AttackResult CalculateAttack(Character target, Random rng)
        {
            if (rng.NextDouble() * 100 > Accuracy)
                return new AttackResult { Damage = 0, IsCrit = false, IsMiss = true };

            int atkStat = Type == AttackType.Physical ? PAtk : MAtk;
            int defStat = Type == AttackType.Physical ? target.PDef : target.MDef;

            // ตรวจสอบเงื่อนไข Dragon Mail (Ignore Def 90%)
            if (EquippedItem != null && EquippedItem.Name == "Dragon Mail")
            {
                defStat = (int)(defStat * 0.1f);
            }

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