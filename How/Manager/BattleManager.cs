using How.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace How.Manager
{
    public enum BattleState
    {
        Running,
        AllyWin,
        EnemyWin
    }

    /// <summary>
    /// จัดการ Battle field ทั้งหมด: ลำดับการโจมตีอัตโนมัติแบบ ATB (คล้าย 7 Knights)
    /// รับปาร์ตี้พันธมิตรที่คงอยู่ข้ามด่าน (ผ่านการอัพสเตตัส/การ์ด/ไอเทมมาแล้ว) และสร้างศัตรูใหม่ทุกด่าน
    /// ข้อมูลตั้งต้นของตัวละครและศัตรูทั้งหมดมาจาก How.Data.CharacterIndex (index กลาง)
    /// ถ้าเป็นด่านยาก (isHardStage) ศัตรูจะมี HP และ ATK เพิ่มขึ้น 150% (คูณ 2.5 เท่า)
    /// </summary>
    public class BattleManager
    {
        public List<Character> Allies { get; private set; }
        public List<Character> Enemies { get; private set; }
        public List<string> Log { get; private set; } = new List<string>();
        public BattleState State { get; private set; } = BattleState.Running;

        public bool IsHardStage { get; private set; }
        public int RewardExp { get; private set; }   // Exp ที่พันธมิตรแต่ละคนจะได้รับถ้าชนะด่านนี้ (ปกติ 100, ด่านยาก 325)
        public int RewardCoins { get; private set; } // Coin ที่จะได้รับถ้าชนะด่านนี้ (ปกติ 10, ด่านยาก 25)

        private readonly Random _rng = new Random();
        private const int MaxLogLines = 8;

        // ตัวละครที่กำลังเล่นอนิเมชันโจมตีอยู่ (ไว้ใช้ตอน Draw ให้ขยับเข้าหาเป้าหมาย)
        public Character ActiveAttacker { get; private set; }
        public Character ActiveTarget { get; private set; }
        public float AttackAnimTimer { get; private set; }
        public const float AttackAnimDuration = 0.35f; // ระยะเวลาที่ตัวละครขยับไปหาเป้าหมายแล้วกลับ (วินาที)

        // โหมดต่อสู้: true = พันธมิตรต้องให้ผู้เล่นคลิกเลือกเป้าหมายเอง (ศัตรูสุ่มเป้าหมายเองเสมอไม่ว่าโหมดไหน)
        //             false = พันธมิตรสุ่มเป้าหมายอัตโนมัติเหมือนเดิมทั้งหมด
        public bool ManualMode { get; private set; } = true;

        // ตัวละครพันธมิตรที่ถึงตาแล้วและกำลังรอผู้เล่นคลิกเลือกเป้าหมาย (มีค่าเฉพาะตอน ManualMode = true)
        public Character PendingAttacker { get; private set; }

        // ยิง event ทุกครั้งที่มีการโจมตีจริง (เป้าหมาย, ผลการโจมตี) ให้ฝั่งแสดงผลเอาไปโชว์เลขดาเมจ/MISS/CRIT ลอยได้
        public event Action<Character, AttackResult> OnAttackResolved;

        // สร้างการต่อสู้ใหม่โดยใช้ปาร์ตี้เดิมที่ส่งเข้ามา (คงสเตตัสที่อัพ/ไอเทมที่สวมไว้) แล้วฮีลเต็มก่อนเริ่มด่านใหม่เสมอ
        public BattleManager(List<Character> allyParty, bool isHardStage = false)
        {
            Allies = allyParty;
            foreach (var ally in Allies)
                ally.Hp = ally.MaxHp; // ฮีลเต็มทุกครั้งที่เริ่มด่านใหม่

            IsHardStage = isHardStage;
            RewardExp = isHardStage ? 325 : 100;
            RewardCoins = isHardStage ? 25 : 10;

            Enemies = CreateEnemies(isHardStage);
        }

        // ปาร์ตี้พันธมิตรเริ่มต้นสำหรับเกมใหม่ (ก่อนอัพสเตตัสใดๆ) — ดึงมาจาก CharacterIndex
        public static List<Character> CreateStartingParty()
        {
            return CharacterIndex.CreateStartingParty();
        }

        private List<Character> CreateEnemies(bool isHardStage)
        {
            var enemies = CharacterIndex.CreateEnemyWave();

            if (isHardStage)
            {
                // ด่านยาก: HP และ ATK (ทั้ง P.ATK และ M.ATK) เพิ่มขึ้น 150% เท่ากับคูณ 2.5 เท่าของค่าเดิม
                foreach (var e in enemies)
                {
                    e.ModifyBaseStat(StatKind.MaxHp, 150f, isPercent: true);
                    e.ModifyBaseStat(StatKind.AllAtk, 150f, isPercent: true);
                    e.Hp = e.MaxHp;
                }
            }

            return enemies;
        }

        public IEnumerable<Character> AllUnits => Allies.Concat(Enemies);

        public void Update(float deltaSeconds)
        {
            if (State != BattleState.Running) return;

            // ระหว่างอนิเมชันโจมตี ให้หยุดสะสมเกจตัวอื่นชั่วครู่ เพื่อให้ดูเป็นจังหวะ (คล้ายเกมจริง)
            if (ActiveAttacker != null)
            {
                AttackAnimTimer += deltaSeconds;
                if (AttackAnimTimer >= AttackAnimDuration)
                {
                    ResolveAttack();
                    ActiveAttacker = null;
                    ActiveTarget = null;
                    AttackAnimTimer = 0f;
                }
                return;
            }

            // ถึงตาพันธมิตรแล้วแต่รอผู้เล่นคลิกเลือกเป้าหมายอยู่ (ManualMode) หยุดเดินเกจตัวอื่นไว้ก่อนเหมือนตอนอนิเมชันโจมตี
            if (PendingAttacker != null)
                return;

            // สะสมเกจให้ทุกตัวที่ยังมีชีวิต หาตัวที่เกจเต็มก่อน (ถ้าเต็มพร้อมกันหลายตัว เลือกตัว Speed สูงสุด)
            Character readyUnit = null;
            foreach (var unit in AllUnits)
            {
                bool ready = unit.TickGauge(deltaSeconds);
                if (ready && (readyUnit == null || unit.Speed > readyUnit.Speed))
                    readyUnit = unit;
            }

            if (readyUnit != null)
            {
                // ศัตรูสุ่มเป้าหมายเองเสมอ ส่วนพันธมิตรสุ่มเองเฉพาะตอนปิด ManualMode เท่านั้น
                if (!readyUnit.IsEnemy && ManualMode)
                    PendingAttacker = readyUnit; // รอผู้เล่นคลิกเลือกเป้าหมาย
                else
                    StartAttack(readyUnit);
            }

            CheckBattleEnd();
        }

        // สลับโหมด Manual/Auto ระหว่างเกม ถ้ากำลังรอผู้เล่นคลิกอยู่แล้วสลับไป Auto ให้สุ่มเป้าหมายให้ทันที กันค้าง
        public void SetManualMode(bool manual)
        {
            ManualMode = manual;
            if (!manual && PendingAttacker != null)
            {
                var attacker = PendingAttacker;
                PendingAttacker = null;
                StartAttack(attacker);
            }
        }

        // ผู้เล่นคลิกเลือกเป้าหมาย (ต้องเป็นฝั่งศัตรูที่ยังมีชีวิตเท่านั้น) ให้ตัวละครที่กำลังรออยู่ (PendingAttacker) เริ่มโจมตี
        public bool TrySelectTarget(Character target)
        {
            if (PendingAttacker == null) return false;
            if (target == null || !target.IsAlive || !target.IsEnemy) return false;

            ActiveAttacker = PendingAttacker;
            ActiveTarget = target;
            AttackAnimTimer = 0f;
            PendingAttacker = null;
            return true;
        }

        private void StartAttack(Character attacker)
        {
            var targets = attacker.IsEnemy ? Allies : Enemies;
            var aliveTargets = targets.Where(t => t.IsAlive).ToList();
            if (aliveTargets.Count == 0) return;

            var target = aliveTargets[_rng.Next(aliveTargets.Count)];
            ActiveAttacker = attacker;
            ActiveTarget = target;
            AttackAnimTimer = 0f;
        }

        private void ResolveAttack()
        {
            if (ActiveAttacker == null || ActiveTarget == null || !ActiveAttacker.IsAlive) return;
            if (!ActiveTarget.IsAlive) return;

            var result = ActiveAttacker.CalculateAttack(ActiveTarget, _rng);

            if (result.IsMiss)
            {
                AddLog($"{ActiveAttacker.Name} misses {ActiveTarget.Name}!");
            }
            else
            {
                ActiveTarget.TakeDamage(result.Damage);
                string critText = result.IsCrit ? " (CRIT!)" : "";
                AddLog($"{ActiveAttacker.Name} hits {ActiveTarget.Name} for {result.Damage} damage{critText}");

                if (!ActiveTarget.IsAlive)
                    AddLog($"{ActiveTarget.Name} has been defeated!");
            }

            OnAttackResolved?.Invoke(ActiveTarget, result);
            CheckBattleEnd();
        }

        private void CheckBattleEnd()
        {
            if (Allies.All(a => !a.IsAlive))
            {
                State = BattleState.EnemyWin;
                AddLog("Lose!");
            }
            else if (Enemies.All(e => !e.IsAlive))
            {
                State = BattleState.AllyWin;
                AddLog("Win!");
            }
        }

        private void AddLog(string message)
        {
            Log.Add(message);
            if (Log.Count > MaxLogLines)
                Log.RemoveAt(0);
        }
    }
}