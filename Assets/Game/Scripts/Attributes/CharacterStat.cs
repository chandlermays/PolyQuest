/*---------------------------
File: CharacterStat.cs
Author: Chandler Mays
----------------------------*/
namespace PolyQuest.Attributes
{
    public enum Stat
    {
        kHealth,
        kDamage,
        kDefense,
        kMana,
        kManaRegenRate,
        kExperienceReward,
        kExperienceToLevelUp,
        kTotalAttributePoints,
        //...
    }

    public enum Attribute
    {
        kStrength,          // Increase melee damage
        kDexterity,         // Increase ranged damage
        kVitality,          // Increase maximum health
        kMagic              // Increase magic damage and maximum mana
        //...
    }
}