using System.Collections.Generic;
using UnityEngine;
using EnemyAI;

// The contract between enemies and the planet / towers.
// Enemies only talk to IPlanetDefense, so towers and the planet can be built however the team likes
// (UI canvas or world space) as long as something implements this interface.
//
// Angles: degrees, counter-clockwise from +X (right), in the planet root's local space.
// Distances: planet radii (1 = planet surface).

/// <summary>Anything an enemy can hit: a tower or the planet itself.</summary>
public interface IDamageable
{
    bool IsAlive { get; }

    /// <summary>Applies damage and returns how much was actually dealt (used to teach the enemy AI).</summary>
    float ReceiveDamage(float amount);
}

/// <summary>Optional for towers: lets Restraining Buck freeze them.</summary>
public interface IFreezable
{
    void Freeze(float seconds);
}

public interface IPlanetDefense
{
    /// <summary>Enemies are parented here; their positions are in this transform's local space.</summary>
    Transform Root { get; }

    /// <summary>Planet radius in Root local units (UI pixels on a canvas, world units otherwise).</summary>
    float Radius { get; }

    /// <summary>The tower in the slot under this angle, or the planet if the slot is empty.</summary>
    IDamageable GetTargetBelow(float angleDegrees);

    /// <summary>Everything an AoE attack at this angle hits: towers within spreadDegrees, plus the planet if the centre slot is empty.</summary>
    List<IDamageable> GetTargetsNear(float angleDegrees, float spreadDegrees);

    /// <summary>Freezes the tower nearest to this angle. Returns false if there was nothing to freeze.</summary>
    bool FreezeNearestTower(float angleDegrees, float seconds);

    /// <summary>What the enemy AI reads: every tower's kind, current angle and HP, plus the fuel balance.</summary>
    DefenseSnapshot BuildSnapshot();
}

public static class PlanetMath
{
    public static Vector3 ToLocal(float angleDegrees, float distanceRadii, float planetRadius)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        float r = distanceRadii * planetRadius;
        return new Vector3(Mathf.Cos(rad) * r, Mathf.Sin(rad) * r, 0f);
    }

    public static float Normalize(float angleDegrees)
    {
        angleDegrees %= 360f;
        return angleDegrees < 0f ? angleDegrees + 360f : angleDegrees;
    }

    public static float Distance(float a, float b)
    {
        return AdaptiveWaveDirector.AngleDistance(a, b);
    }
}
