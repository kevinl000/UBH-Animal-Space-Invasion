using System;
using UnityEngine;

/// <summary>Animal Ship (README, level 5): no attack. On death, releases 2 Healthy Skunks, 2 Healthy Geese, 5 Shielded Geese.</summary>
public class AnimalShipBoss : BossEnemy
{
    [Serializable]
    public struct Cargo
    {
        public EnemyData enemy;
        public int count;
    }

    [Tooltip("README: 2 Healthy Skunk, 2 Healthy Goose, 5 Shielded Goose.")]
    public Cargo[] cargo = new Cargo[0];
    [Tooltip("How far apart the released animals spread, degrees.")]
    public float releaseSpread = 60f;

    protected override void OnDeath()
    {
        int total = 0;
        foreach (Cargo c in cargo) total += Mathf.Max(0, c.count);
        int i = 0;
        foreach (Cargo c in cargo)
        {
            for (int k = 0; k < c.count; k++, i++)
            {
                float offset = total > 1 ? Mathf.Lerp(-releaseSpread / 2f, releaseSpread / 2f, i / (float)(total - 1)) : 0f;
                Summon(c.enemy, offset);
            }
        }
    }
}
