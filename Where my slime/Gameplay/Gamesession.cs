using System.Collections.Generic;
using Where_My_Slime.Data;
using Where_My_Slime.Models;

namespace Where_My_Slime
{
    // ที่มาของการเข้าหน้า TeamSetup แต่ละครั้ง ใช้ตัดสินใจว่าปุ่ม Back ควรพาไปหน้าไหน
    public enum TeamSetupOrigin
    {
        Story,
        ChallengeStart,
        ChallengeMap
    }

    // เก็บสถานะที่ใช้ร่วมกันข้ามหลายหน้าจอ (ปาร์ตี้, คลัง, เหรียญ, ฟอร์เมชัน ฯลฯ)
    // ส่งอ้างอิง (reference) ตัวเดียวกันนี้ให้ทุกคลาส Screen เพื่อให้ค่าที่แก้ไขในหน้าหนึ่งเห็นผลในหน้าอื่นทันที
    public class GameSession
    {
        public List<Character> Party;
        public List<Character> AllHeroes = new List<Character>();
        public Character[] FormationSlots = new Character[5];
        public TeamSetupOrigin TeamSetupOrigin;
        public bool CurrentStageIsHard;
        public bool IsNextBattleBoss; // ตั้งเป็น true ตอนเลือกเส้นทาง Boss จาก Map ใน Challenge Mode (ทุกๆ 5 ชั้น)
        public bool ManualModePreference = true;

        public List<Item> Inventory = new List<Item>();
        public int Coins;

        public bool IsStoryMode;
        public int StoryStageIndex = 0;

        public bool IsShopOnMap;
        public List<Item> ShopItems = new List<Item>();
        public List<Item> RewardItems = new List<Item>();

        public bool CameraShakeEnabled = true;
    }
}