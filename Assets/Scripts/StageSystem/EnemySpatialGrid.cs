using IGameInterface;
using System.Collections.Generic;
using UnityEngine;

sealed class EnemySpatialGrid
{
    readonly Dictionary<Vector2Int, List<EnemyInfo>> cells = new();
    readonly Dictionary<EnemyInfo, Vector2Int> enemyCells = new();

    float cellSize = 5f;

    public void SetCellSize(float value) => cellSize = Mathf.Max(0.1f, value);
    public void Clear()
    {
        cells.Clear();
        enemyCells.Clear();
    }

    public void AddOrUpdate(EnemyInfo enemy)
    {
        if (!IsValidTarget(enemy))
        {
            Remove(enemy);
            return;
        }

        Vector2Int newCell = GetCell(enemy.Position);

        if (enemyCells.TryGetValue(enemy, out Vector2Int oldCell))
        {
            if (oldCell == newCell) return;

            RemoveFromCell(enemy, oldCell);
            AddToCell(enemy, newCell);
            enemyCells[enemy] = newCell;
            return;
        }

        AddToCell(enemy, newCell);
        enemyCells.Add(enemy, newCell);
    }
    public void Remove(EnemyInfo enemy)
    {
        if (enemy == null || !enemyCells.TryGetValue(enemy, out Vector2Int cell)) return;

        RemoveFromCell(enemy, cell);
        enemyCells.Remove(enemy);
    }

    public bool TryGetEnemy(Vector3 origin, float range, EnemyTargetMode mode, out EnemyInfo enemy)
    {
        enemy = null;
        if (range <= 0f) return false;

        float rangeSqr = range * range;
        float bestScore = GetInitialScore(mode);

        Vector2Int min = GetCell(origin - new Vector3(range, 0f, range));
        Vector2Int max = GetCell(origin + new Vector3(range, 0f, range));

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                if (!cells.TryGetValue(new Vector2Int(x, y), out List<EnemyInfo> bucket)) continue;

                FindBestInBucket(bucket, origin, rangeSqr, mode, ref bestScore, ref enemy);
            }
        }

        return enemy != null;
    }

    void FindBestInBucket(List<EnemyInfo> bucket, Vector3 origin, float rangeSqr,
        EnemyTargetMode mode, ref float bestScore, ref EnemyInfo bestEnemy)
    {
        for (int i = bucket.Count - 1; i >= 0; i--)
        {
            EnemyInfo candidate = bucket[i];

            if (!IsValidTarget(candidate))
            {
                bucket.RemoveAt(i);
                enemyCells.Remove(candidate);
                continue;
            }

            Vector3 diff = candidate.Position - origin;
            diff.y = 0f;

            float distanceSqr = diff.sqrMagnitude;
            if (distanceSqr > rangeSqr) continue;

            float score = GetScore(candidate, distanceSqr, mode);
            if (!IsBetter(score, bestScore, mode)) continue;

            bestScore = score;
            bestEnemy = candidate;
            {

            }
        }
    }

    Vector2Int GetCell(Vector3 pos)
        => new Vector2Int(Mathf.FloorToInt(pos.x / cellSize), Mathf.FloorToInt(pos.z / cellSize));
    void AddToCell(EnemyInfo enemy, Vector2Int cell)
    {
        if (!cells.TryGetValue(cell, out List<EnemyInfo> bucket))
        {
            bucket = new List<EnemyInfo>();
            cells.Add(cell, bucket);
        }

        if (!bucket.Contains(enemy)) bucket.Add(enemy);
    }
    void RemoveFromCell(EnemyInfo enemy, Vector2Int cell)
    {
        if (!cells.TryGetValue(cell, out List<EnemyInfo> bucket)) return;

        bucket.Remove(enemy);

        if (bucket.Count <= 0) cells.Remove(cell);
    }

    static bool IsValidTarget(EnemyInfo enemy) => enemy != null && enemy.CanBeTargeted;
    static float GetInitialScore(EnemyTargetMode mode)
        => mode switch
        {
            EnemyTargetMode.ClosestToTower => float.MaxValue,
            EnemyTargetMode.FarthestFromTower => float.MinValue,
            EnemyTargetMode.FrontMost => float.MinValue,
            EnemyTargetMode.BackMost => float.MaxValue,
            _ => float.MaxValue
        };
    static float GetScore(EnemyInfo enemy, float distanceSqr, EnemyTargetMode mode)
        => mode switch
        {
            EnemyTargetMode.ClosestToTower => distanceSqr,
            EnemyTargetMode.FarthestFromTower => distanceSqr,
            EnemyTargetMode.FrontMost => enemy.PathProgress,
            EnemyTargetMode.BackMost => enemy.PathProgress,
            _ => distanceSqr
        };
    static bool IsBetter(float score, float bestScore, EnemyTargetMode mode)
        => mode switch
        {
            EnemyTargetMode.ClosestToTower => score < bestScore,
            EnemyTargetMode.FarthestFromTower => score > bestScore,
            EnemyTargetMode.FrontMost => score > bestScore,
            EnemyTargetMode.BackMost => score < bestScore,
            _ => score < bestScore
        };

}