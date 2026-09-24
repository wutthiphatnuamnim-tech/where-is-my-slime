using System;
using System.Collections.Generic;
using How.Models;

namespace How.Data
{
    public enum ItemRarity
    {
        Common,     // สีขาว
        Uncommon,   // สีเขียว
        Rare,       // สีฟ้า
        Epic,       // สีม่วง
        Legendary,  // สีทอง
        Mythic      // สีแดง
    }

    public class Item
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ItemRarity Rarity { get; set; }
        public int Price { get; set; }

        // สเตตัสหลัก (Primary Stat)
        public StatKind Stat { get; set; }
        public float Amount { get; set; }
        public bool IsPercent { get; set; }

        // สเตตัสรอง (Secondary Stat - เช่น M.ATK ของ Necklace หรือ M.Def ของ Dragon Mail)
        public StatKind SecondaryStat { get; set; }
        public float SecondaryAmount { get; set; }
        public bool SecondaryIsPercent { get; set; }

        // สเตตัสที่สาม (Tertiary Stat - สำหรับไอเท็มระดับ Mythic)
        public StatKind TertiaryStat { get; set; }
        public float TertiaryAmount { get; set; }
        public bool TertiaryIsPercent { get; set; }
    }

    public static class ItemPool
    {
        public static List<Item> GetRandomItems(int count, Random rng)
        {
            List<Item> masterPool = new List<Item>()
            {
                // ---------- Common (ระดับ Common - สีขาว) ----------
                new Item { Name = "Broken Blade", Description = "P.ATK +4%", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.PAtk, Amount = 0.04f, IsPercent = true },
                new Item { Name = "Stick", Description = "M.ATK +4%", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.MAtk, Amount = 0.04f, IsPercent = true },
                new Item { Name = "Wood Plate", Description = "P.Def +8%", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.PDef, Amount = 0.08f, IsPercent = true },
                new Item { Name = "Broken Charm", Description = "M.Def +8%", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.MDef, Amount = 0.08f, IsPercent = true },
                new Item { Name = "Health Ring", Description = "HP +50", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.MaxHp, Amount = 50f, IsPercent = false },
                new Item { Name = "Fast Boots", Description = "Speed +8", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.Speed, Amount = 8f, IsPercent = false },
                new Item { Name = "Clover Leaf", Description = "Crit Rate +8%", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.CritRate, Amount = 0.08f, IsPercent = true },
                new Item { Name = "Broken Spirit", Description = "Crit DMG +12%", Rarity = ItemRarity.Common, Price = 10, Stat = StatKind.CritDamage, Amount = 0.12f, IsPercent = true },

                // ---------- Uncommon (ระดับ Uncommon - สีเขียว) ----------
                new Item { Name = "Sharp Blade", Description = "P.ATK +15%", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.PAtk, Amount = 0.15f, IsPercent = true },
                new Item { Name = "Arcane Orb", Description = "M.ATK +15%", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.MAtk, Amount = 0.15f, IsPercent = true },
                new Item { Name = "Iron Plate", Description = "P.Def +18%", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.PDef, Amount = 0.18f, IsPercent = true },
                new Item { Name = "Ward Charm", Description = "M.Def +18%", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.MDef, Amount = 0.18f, IsPercent = true },
                new Item { Name = "Vitality Ring", Description = "HP +150", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.MaxHp, Amount = 150f, IsPercent = false },
                new Item { Name = "Swift Boots", Description = "Speed +18", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.Speed, Amount = 18f, IsPercent = false },
                new Item { Name = "Hawk Eye", Description = "Crit Rate +18%", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.CritRate, Amount = 0.18f, IsPercent = true },
                new Item { Name = "Lucky Coin", Description = "Increase Coin Gain", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.PAtk, Amount = 1f, IsPercent = false },
                new Item { Name = "Berserk Fang", Description = "Crit DMG +35%", Rarity = ItemRarity.Uncommon, Price = 25, Stat = StatKind.CritDamage, Amount = 0.35f, IsPercent = true },

                // ---------- Rare (ระดับ Rare - สีฟ้า) ----------
                new Item { Name = "Twin Blade", Description = "P.ATK +30%", Rarity = ItemRarity.Rare, Price = 50, Stat = StatKind.PAtk, Amount = 0.30f, IsPercent = true },
                new Item { Name = "Magic Gloves", Description = "M.ATK +30%", Rarity = ItemRarity.Rare, Price = 50, Stat = StatKind.MAtk, Amount = 0.30f, IsPercent = true },
                new Item { Name = "Platinum Plate", Description = "P.Def +35%", Rarity = ItemRarity.Rare, Price = 50, Stat = StatKind.PDef, Amount = 0.35f, IsPercent = true },
                new Item { Name = "Tunic", Description = "M.Def +35%", Rarity = ItemRarity.Rare, Price = 50, Stat = StatKind.MDef, Amount = 0.35f, IsPercent = true },
                new Item {
                    Name = "Beast Ring",
                    Description = "Crit DMG +120% decrease MaxHP 80%",
                    Rarity = ItemRarity.Rare,
                    Price = 50,
                    Stat = StatKind.CritDamage, Amount = 1.20f, IsPercent = true,
                    SecondaryStat = StatKind.MaxHp, SecondaryAmount = -0.80f, SecondaryIsPercent = true
                },

                // ---------- Epic (ระดับ Epic - สีม่วง) ----------
                new Item { Name = "Ancient Katana", Description = "P.ATK +200 \n(P.ATK + 14% if STR > 19)", Rarity = ItemRarity.Epic, Price = 100, Stat = StatKind.PAtk, Amount = 200f, IsPercent = false },
                new Item { Name = "Ancient Staff", Description = "M.ATK +200 \n(M.ATK + 14% if INT > 19)", Rarity = ItemRarity.Epic, Price = 100, Stat = StatKind.MAtk, Amount = 200f, IsPercent = false },
                new Item { Name = "Demon Eye", Description = "Crit Rate +100% if CRT > 30", Rarity = ItemRarity.Epic, Price = 100, Stat = StatKind.CritRate, Amount = 1.0f, IsPercent = true },

                // ---------- Legendary (ระดับ Legendary - สีทอง) ----------
                new Item {
                    Name = "Strongest Protection Necklace",
                    Description = "increase All.ATK * VIT(VIT * 1.3%)",
                    Rarity = ItemRarity.Legendary,
                    Price = 200,
                    Stat = StatKind.PAtk, Amount = 0.013f, IsPercent = true,
                    SecondaryStat = StatKind.MAtk, SecondaryAmount = 0.013f, SecondaryIsPercent = true
                },
                new Item {
                    Name = "Dragon Mail",
                    Description = "Ignore All.Def + 90%, Evasion +40%",
                    Rarity = ItemRarity.Legendary,
                    Price = 200,
                    Stat = StatKind.PDef, Amount = 0.90f, IsPercent = true,
                    SecondaryStat = StatKind.MDef, SecondaryAmount = 0.90f, SecondaryIsPercent = true
                },

                // ---------- Mythic (ระดับ Mythic - สีแดง) ----------
                new Item {
                    Name = "Ring of Legendary Hero",
                    Description = "P.ATK, M.ATK + 50%, MaxHP +4000 \nincrease Crit Rate/DMG 50% if CRT > 50",
                    Rarity = ItemRarity.Mythic,
                    Price = 500,
                    Stat = StatKind.PAtk, Amount = 0.50f, IsPercent = true,
                    SecondaryStat = StatKind.MAtk, SecondaryAmount = 0.50f, SecondaryIsPercent = true,
                    TertiaryStat = StatKind.MaxHp, TertiaryAmount = 4000f, TertiaryIsPercent = false
                }
            };

            List<Item> results = new List<Item>();
            for (int i = 0; i < count; i++)
            {
                int index = rng.Next(masterPool.Count);
                results.Add(masterPool[index]);
            }

            return results;
        }
    }
}