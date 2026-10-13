using System;
using System.Collections.Generic;
using System.Linq;
using Where_My_Slime.Data;
using Where_My_Slime.Models;

namespace Where_My_Slime.Battle
{
    public enum BattleState
    {
        Running,
        AllyWin,
        EnemyWin
    }

    public class BattleManager
    {
        public List<Character> Allies { get; private set; }
        public List<Character> Enemies { get; private set; }
        public List<string> Log { get; private set; } = new List<string>();
        public BattleState State { get; private set; } = BattleState.Running;

        public bool IsHardStage { get; private set; }
        public bool IsBossStage { get; private set; }
        public int RewardPoints { get; private set; }
        public int RewardCoins { get; private set; }

        private readonly Random _rng = new Random();
        private const int MaxLogLines = 8;
        private readonly Action _onShakeCamera;

        public Character ActiveAttacker { get; private set; }
        public Character ActiveTarget { get; private set; }
        public float AttackAnimTimer { get; private set; }
        public const float AttackAnimDuration = 0.35f;

        public bool ManualMode { get; private set; } = true;
        public Character PendingAttacker { get; private set; }

        public event Action<Character, AttackResult> OnAttackResolved;

        public delegate void HealResolvedEventHandler(Character target, int amount);
        public event HealResolvedEventHandler OnHealResolved;

        public bool IsUsingUltimate = false;
        public Character UltimateCaster = null;

        // เพิ่มตัวแปรสำหรับจับเวลาอนิเมชัน Ultimate ให้เล่นจนจบก่อนคำนวณผล
        public float UltimateAnimTimer { get; private set; }
        public const float UltimateAnimDuration = 1.1f;

        public BattleManager(List<Character> allyParty, bool isHardStage = false, Action onShakeCamera = null, int floorsCleared = 0, bool isBossStage = false)
        {
            Allies = allyParty;
            foreach (var ally in Allies)
            {
                ally.Hp = ally.MaxHp;
                ally.InitializeSP();
            }

            IsHardStage = isHardStage;
            IsBossStage = isBossStage;

            // รีวอร์ด: Boss ให้คงที่ ไม่สนใจ Hard/Normal เพราะ Boss เป็นหมวดของตัวเอง
            RewardPoints = isBossStage ? 10 : (isHardStage ? 5 : 3);
            RewardCoins = isBossStage ? 125 : (isHardStage ? 20 : 10);
            _onShakeCamera = onShakeCamera;

            Enemies = CreateEnemies(isHardStage, floorsCleared, isBossStage);
        }

        public static List<Character> CreateStartingParty()
        {
            return CharacterIndex.CreateStartingParty();
        }

        private List<Character> CreateEnemies(bool isHardStage, int floorsCleared, bool isBossStage)
        {
            List<Character> enemies = isBossStage
                ? new List<Character> { CharacterIndex.CreateEnemy("boss_slime_king") }
                : CharacterIndex.CreateEnemyWave();

            if (isHardStage)
            {
                foreach (var e in enemies)
                {
                    e.ModifyBaseStat(StatKind.MaxHp, 150f, isPercent: true);
                    e.ModifyBaseStat(StatKind.AllAtk, 150f, isPercent: true);
                    e.Hp = e.MaxHp;
                }
            }

            // Challenge Mode: ศัตรูแข็งแกร่งขึ้นทบต้น x1.2 ต่อจำนวนชั้นที่ผ่านมาแล้ว (Stack ตามจำนวนครั้งที่ชนะ)
            if (floorsCleared > 0)
            {
                float percentIncrease = ((float)Math.Pow(1.2, floorsCleared) - 1f) * 100f;
                foreach (var e in enemies)
                {
                    e.ModifyBaseStat(StatKind.MaxHp, percentIncrease, isPercent: true);
                    e.ModifyBaseStat(StatKind.AllAtk, percentIncrease, isPercent: true);
                    e.Hp = e.MaxHp;
                }
            }

            return enemies;
        }

        public IEnumerable<Character> AllUnits => Allies.Concat(Enemies);

        public void Update(float deltaSeconds)
        {
            if (State != BattleState.Running) return;

            // จัดการเวลาอนิเมชัน Ultimate ให้เล่นจนครบกำหนดเวลาก่อนค่อยประมวลผล
            if (IsUsingUltimate)
            {
                UltimateAnimTimer += deltaSeconds;
                if (UltimateAnimTimer >= UltimateAnimDuration)
                {
                    ResolveUltimate();
                    IsUsingUltimate = false;
                    UltimateCaster = null;
                    UltimateAnimTimer = 0f;
                }
                return;
            }

            if (ActiveAttacker != null)
            {
                float currentAnimDuration = AttackAnimDuration;

                string attackerName = ActiveAttacker.Name ?? "";
                bool isHornet = attackerName.IndexOf("bee", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                attackerName.IndexOf("hornet", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isHornet)
                {
                    currentAnimDuration = 1.0f;
                }

                AttackAnimTimer += deltaSeconds;
                if (AttackAnimTimer >= currentAnimDuration)
                {
                    ResolveAttack();
                    ActiveAttacker = null;
                    ActiveTarget = null;
                    AttackAnimTimer = 0f;
                }
                return;
            }

            if (PendingAttacker != null) return;

            Character readyUnit = null;
            foreach (var unit in AllUnits)
            {
                bool ready = unit.TickGauge(deltaSeconds);
                if (ready && (readyUnit == null || unit.Speed > readyUnit.Speed))
                    readyUnit = unit;
            }

            if (readyUnit != null)
            {
                if (!readyUnit.IsEnemy && ManualMode)
                    PendingAttacker = readyUnit;
                else
                    StartAttack(readyUnit);
            }

            CheckBattleEnd();
        }

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

            Character target = null;

            if (attacker.IsEnemy && attacker.TauntDuration > 0)
            {
                target = aliveTargets.FirstOrDefault(t => t.Name == "Elma");
            }

            if (target == null)
            {
                target = aliveTargets[_rng.Next(aliveTargets.Count)];
            }

            ActiveAttacker = attacker;
            ActiveTarget = target;
            AttackAnimTimer = 0f;
        }

        private void ResolveAttack()
        {
            if (ActiveAttacker == null || ActiveTarget == null || !ActiveAttacker.IsAlive) return;
            if (!ActiveTarget.IsAlive) return;

            _onShakeCamera?.Invoke();

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

                ActiveAttacker.CurrentSP = Math.Min(ActiveAttacker.CurrentSP + 50, ActiveAttacker.MaxSP);
                ActiveTarget.CurrentSP = Math.Min(ActiveTarget.CurrentSP + 20, ActiveTarget.MaxSP);

                if (!ActiveTarget.IsAlive)
                    AddLog($"{ActiveTarget.Name} has been defeated!");
            }

            if (ActiveAttacker.SwordmanBuffDuration > 0) ActiveAttacker.SwordmanBuffDuration--;
            if (ActiveAttacker.TauntDuration > 0) ActiveAttacker.TauntDuration--;

            OnAttackResolved?.Invoke(ActiveTarget, result);
            CheckBattleEnd();
        }

        public void CastUltimate(Character caster, Character target)
        {
            if (caster == null) return;

            caster.CurrentSP = 0;
            IsUsingUltimate = true;
            UltimateCaster = caster;
            ActiveTarget = target;
            UltimateAnimTimer = 0f;

            // คำนวณดาเมจ/ฮีล ทันทีตอนนี้เลย เพราะฝั่ง BattleScreen เป็นคนคุมจังหวะอนิเมชัน
            // เองอยู่แล้ว (ถึงค่อยเรียก CastUltimate ตอนอนิเมชันเดินมาถึงจังหวะที่ควรลงดาเมจ)
            // ถ้าไม่ Resolve ที่นี่ จะไม่มีใครเรียก ResolveUltimate() ให้เลย เพราะ BattleScreen
            // จะไม่เรียก Update(delta) ของคลาสนี้ตลอดช่วงที่ IsUsingUltimate เป็น true
            ResolveUltimate();

            IsUsingUltimate = false;
            UltimateCaster = null;
        }

        private void ResolveUltimate()
        {
            if (UltimateCaster == null) return;
            var caster = UltimateCaster;
            var target = ActiveTarget;

            string name = caster.Name ?? "";

            if (name == "Elma")
            {
                if (target == null || !target.IsAlive) return;
                int damage = (int)(caster.CurrentPAtk * 4.0f);
                target.TakeDamage(damage);
                target.CurrentSP = Math.Min(target.CurrentSP + 20, target.MaxSP);
                OnAttackResolved?.Invoke(target, new AttackResult { Damage = damage, IsCrit = false, IsMiss = false });

                foreach (var enemy in Enemies)
                {
                    if (enemy.IsAlive)
                    {
                        enemy.TauntDuration = 2;
                    }
                }
            }
            else if (name == "Archer")
            {
                if (target == null || !target.IsAlive) return;
                int damage = (int)(caster.CurrentPAtk * 2.5f);
                target.TakeDamage(damage);
                target.CurrentSP = Math.Min(target.CurrentSP + 20, target.MaxSP);
                OnAttackResolved?.Invoke(target, new AttackResult { Damage = damage, IsCrit = false, IsMiss = false });
            }
            else if (name == "Cleric")
            {
                int casterShare = (int)(caster.MaxHp * 0.25f);
                foreach (var ally in Allies)
                {
                    if (ally != null && ally.IsAlive)
                    {
                        int healAmount = casterShare + (int)(ally.MaxHp * 0.02f);
                        ally.Hp = Math.Min(ally.MaxHp, ally.Hp + healAmount);
                        OnHealResolved?.Invoke(ally, healAmount);
                    }
                }
            }
            else if (name == "Mage" || name == "Lumi" || name.Contains("Lumi"))
            {
                int damage = (int)(caster.MAtk * 0.4f);

                foreach (var enemy in Enemies.ToList())
                {
                    if (enemy.IsAlive)
                    {
                        enemy.TakeDamage(damage);
                        enemy.CurrentSP = Math.Min(enemy.CurrentSP + 20, enemy.MaxSP);
                        OnAttackResolved?.Invoke(enemy, new AttackResult { Damage = damage, IsCrit = false, IsMiss = false });
                    }
                }
            }
            else if (name == "Swordman" || name == "Alice" || name.Contains("Alice"))
            {
                caster.SwordmanBuffDuration = 3;
                AddLog($"{caster.Name} uses Ultimate and gains self buff!");
            }

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