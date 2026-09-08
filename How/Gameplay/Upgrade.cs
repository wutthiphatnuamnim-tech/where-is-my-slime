using How.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace How.Gameplay
{
    /// <summary>
    /// การ์ดหลังอัพสเตตัส: แต่ละใบให้ผลบวกพร้อมแลกกับผลลบ (trade-off) แบบเกม roguelite ทั่วไป
    /// Apply จะปรับสเตตัส "ฐาน" ของตัวละครทุกตัวในปาร์ตี้ถาวร (ติดตัวไปทุกด่านที่เหลือ)
    /// </summary>
    public class UpgradeCard
    {
        public string Title;
        public string Description;
        public Action<List<Character>> Apply;
    }

    public static class UpgradeCardPool
    {
        // พูล Card ทั้งหมด — ปรับตัวเลข/เพิ่มใบใหม่ได้ตรงนี้จุดเดียว
        public static List<UpgradeCard> GetAllCards()
        {
            return new List<UpgradeCard>
            {
                new UpgradeCard
                {
                    Title = "Power Surge",
                    Description = "+20% P.ATK   /   -30 Max HP",
                    Apply = party =>
                    {
                        foreach (var c in party)
                        {
                            c.ModifyBaseStat(StatKind.PAtk, 20f, isPercent: true);
                            c.ModifyBaseStat(StatKind.MaxHp, -30f, isPercent: false);
                        }
                    }
                },
                new UpgradeCard
                {
                    Title = "Berserker's Edge",
                    Description = "+15 ALL.ATK   /   -10 Speed",
                    Apply = party =>
                    {
                        foreach (var c in party)
                        {
                            c.ModifyBaseStat(StatKind.AllAtk, 15f, isPercent: false);
                            c.ModifyBaseStat(StatKind.Speed, -10f, isPercent: false);
                        }
                    }
                },
                new UpgradeCard
                {
                    Title = "Lucky Strike",
                    Description = "+20% Crit Rate   /   -15% Max HP",
                    Apply = party =>
                    {
                        foreach (var c in party)
                        {
                            c.ModifyBaseStat(StatKind.CritRate, 20f, isPercent: true);
                            c.ModifyBaseStat(StatKind.MaxHp, -15f, isPercent: true);
                        }
                    }
                },
                new UpgradeCard
                {
                    Title = "Glass Cannon",
                    Description = "-90% Max HP   /   +100% Crit Rate",
                    Apply = party =>
                    {
                        foreach (var c in party)
                        {
                            c.ModifyBaseStat(StatKind.MaxHp, -90f, isPercent: true);
                            c.ModifyBaseStat(StatKind.CritRate, 100f, isPercent: true);
                        }
                    }
                },
                new UpgradeCard
                {
                    Title = "Iron Will",
                    Description = "+25% P.DEF & M.DEF   /   -10% P.ATK & M.ATK",
                    Apply = party =>
                    {
                        foreach (var c in party)
                        {
                            c.ModifyBaseStat(StatKind.PDef, 25f, isPercent: true);
                            c.ModifyBaseStat(StatKind.MDef, 25f, isPercent: true);
                            c.ModifyBaseStat(StatKind.AllAtk, -10f, isPercent: true);
                        }
                    }
                },
                new UpgradeCard
                {
                    Title = "Swift Strikes",
                    Description = "+15 Speed   /   -10% Max HP",
                    Apply = party =>
                    {
                        foreach (var c in party)
                        {
                            c.ModifyBaseStat(StatKind.Speed, 15f, isPercent: false);
                            c.ModifyBaseStat(StatKind.MaxHp, -10f, isPercent: true);
                        }
                    }
                },
                new UpgradeCard
                {
                    Title = "Precision",
                    Description = "+25 Accuracy   /   -10% Crit Damage",
                    Apply = party =>
                    {
                        foreach (var c in party)
                        {
                            c.ModifyBaseStat(StatKind.Accuracy, 25f, isPercent: false);
                            c.ModifyBaseStat(StatKind.CritDamage, -10f, isPercent: true);
                        }
                    }
                },
            };
        }

        // สุ่มการ์ด count ใบแบบไม่ซ้ำกัน ให้ผู้เล่นเลือก
        public static List<UpgradeCard> GetRandomCards(int count, Random rng)
        {
            return GetAllCards().OrderBy(_ => rng.Next()).Take(count).ToList();
        }
    }
}