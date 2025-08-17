using UnityEngine;

public static class ThreatAssessor
{
    // Simple heuristic: low hp or enemies >= allies*1.2 → DANGER
    public static bool IsDanger(float health01, int allyCount, int enemyCount, float ratioK=1.2f, float lowHp=0.35f)
    {
        if (health01 <= lowHp) return true;
        return enemyCount >= Mathf.CeilToInt(allyCount * ratioK);
    }
}