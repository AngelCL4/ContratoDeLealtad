using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*public class UnitInstance
{
    public string unitName;
    public Class unitClass;
    public int currentLevel;
    public int currentExp;
    public int currentHP;
    public int maxHP;
    public int power;
    public int skill;
    public int speed;
    public int luck;
    public int defense;
    public int resistance;
    public int movement;

    public int hpGrowth;
    public int powerGrowth;
    public int skillGrowth;
    public int speedGrowth;
    public int luckGrowth;
    public int defenseGrowth;
    public int resistanceGrowth;

    public UnitInstance(UnitDataJson.Unit data)
    {
        currentLevel = data.level;
        maxHP = data.maxHP + data.unitClass.bonusHP;
        currentHP = maxHP;
        power = data.power + data.unitClass.bonusPower;
        skill = data.skill + data.unitClass.bonusSkill;
        speed = data.speed + data.unitClass.bonusSpeed;
        luck = data.luck + data.unitClass.bonusLuck;
        defense = data.defense + data.unitClass.bonusDefense;
        resistance = data.resistance + data.unitClass.bonusResistance;
        movement = data.movement + data.unitClass.bonusMovement;
        currentExp = 0;
    }

    public void GainExp(int amount)
    {
        currentExp += amount;
        while (currentExp >= 100)
        {
            currentExp -= 100;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        currentLevel++;
        power += CheckStatIncrease(powerGrowth);
        skill += CheckStatIncrease(skillGrowth);
        speed += CheckStatIncrease(speedGrowth);
        luck += CheckStatIncrease(luckGrowth);
        defense += CheckStatIncrease(defenseGrowth);
        resistance += CheckStatIncrease(resistanceGrowth);
        maxHP += CheckStatIncrease(hpGrowth);
        currentHP = maxHP;
        Debug.Log($"{unitName} subió de nivel.");
    }

    private int CheckStatIncrease(int growth)
    {
        int increases = 0;
        if (growth > 100)
        {
            increases++;
            int overflowChance = growth - 100;
            if (Random.Range(1, 101) <= overflowChance)
            {
                increases++;
            }
        }
        else
        {
            if (Random.Range(1, 101) <= growth)
            {
                increases++;
            }
        }
        return increases;
    }
}
*/