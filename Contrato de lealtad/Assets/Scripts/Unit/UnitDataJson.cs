using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UnitDataJson
{
    [System.Serializable]
    public class Unit
    {
        public string unitName;
        public string unitClass;
        public BaseStats baseStats;
        public GrowthRates growthRates;
    }

    [System.Serializable]
    public class BaseStats
    {
        public int hp;
        public int attack;
        public int defense;
        public int magic;
        public int speed;
    }

    [System.Serializable]
    public class GrowthRates
    {
        public float hpGrowth;
        public float attackGrowth;
        public float defenseGrowth;
        public float magicGrowth;
        public float speedGrowth;
    }
}