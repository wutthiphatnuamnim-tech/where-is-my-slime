using System;
using System.Collections.Generic;
using System.Linq;

namespace How.Data
{
    // ไอเทมชิ้นเดียวที่ตัวละครสวมใส่ได้ 1 ชิ้นต่อคน (สวมทับของเดิมอัตโนมัติถ้าเลือกใหม่)
    // ต่างจาก UpgradeCard ตรงที่ถอดออกได้ (Unequip) แล้วสเตตัสจะกลับไปเหมือนก่อนสวม
    public class Item
    {
        public string Name;
        public string Description;
        public StatKind Stat;
        public float Amount;
        public bool IsPercent;
        public int Price; // ราคา Coin ที่ต้องจ่ายเพื่อซื้อไอเทมนี้ในหน้า ItemSelect
    }

    public static class ItemPool
    {
        // พูลไอเทมทั้งหมดที่อาจสุ่มออกมาให้เลือกในหน้าพัก (Rest) — เพิ่ม/ปรับได้ตรงนี้จุดเดียว
        // ราคาตั้งตามความแรงคร่าวๆ (10-25 Coin) ให้พอๆ กับรางวัลที่ได้ต่อด่าน (ปกติ 10 / ยาก 25)
        public static List<Item> GetAllItems()
        {
            return new List<Item>
            {
                new Item { Name = "Sharp Blade",    Description = "+15% P.ATK",     Stat = StatKind.PAtk,       Amount = 15f,  IsPercent = true,  Price = 20 },
                new Item { Name = "Arcane Orb",     Description = "+15% M.ATK",     Stat = StatKind.MAtk,       Amount = 15f,  IsPercent = true,  Price = 20 },
                new Item { Name = "Iron Plate",     Description = "+20% P.DEF",     Stat = StatKind.PDef,       Amount = 20f,  IsPercent = true,  Price = 15 },
                new Item { Name = "Ward Charm",     Description = "+20% M.DEF",     Stat = StatKind.MDef,       Amount = 20f,  IsPercent = true,  Price = 15 },
                new Item { Name = "Vitality Ring",  Description = "+150 Max HP",    Stat = StatKind.MaxHp,      Amount = 150f, IsPercent = false, Price = 20 },
                new Item { Name = "Swift Boots",    Description = "+20 Speed",      Stat = StatKind.Speed,      Amount = 20f,  IsPercent = false, Price = 15 },
                new Item { Name = "Hawk Eye",       Description = "+15 Accuracy",   Stat = StatKind.Accuracy,   Amount = 15f,  IsPercent = false, Price = 10 },
                new Item { Name = "Lucky Coin",     Description = "+15% Crit Rate", Stat = StatKind.CritRate,   Amount = 15f,  IsPercent = true,  Price = 25 },
                new Item { Name = "Berserk Fang",   Description = "+20% Crit Dmg",  Stat = StatKind.CritDamage, Amount = 20f,  IsPercent = true,  Price = 25 },
            };
        }

        // สุ่มไอเทม count ชิ้นแบบไม่ซ้ำกัน ให้ผู้เล่นเลือกในหน้าพัก
        public static List<Item> GetRandomItems(int count, Random rng)
        {
            return GetAllItems().OrderBy(_ => rng.Next()).Take(count).ToList();
        }
    }
}