using UnityEngine;
using IGameInterface;

public interface ITargetPredictionService
{
    Vector3 GetPredictedPosition(EnemyInfo enemy, float delay, float predictionMultiplier, float maxPredictionDistance);
}
