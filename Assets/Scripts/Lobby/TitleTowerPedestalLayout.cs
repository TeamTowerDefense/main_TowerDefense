using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class TitleTowerPedestalLayout : MonoBehaviour
{
    [SerializeField] List<Sprite> pedestalSprites = new();
    [SerializeField] List<Transform> towerAnchors = new();
    [SerializeField] List<Vector2> pedestalSizes = new();
    [SerializeField] Vector2 defaultPedestalSize = new(3.25f, 1.5f);
    [SerializeField] float pedestalWorldY = 0.08f;
    [SerializeField] float pedestalWorldZ = 1.5f;
    [SerializeField] int sortingOrder = -10;

    readonly List<Transform> pedestals = new();
    bool rebuildRequested = true;

    void OnEnable()
    {
        pedestals.Clear();
        rebuildRequested = true;
    }

    void OnValidate()
    {
        pedestals.Clear();
        rebuildRequested = true;
    }

    void LateUpdate()
    {
        if (rebuildRequested || pedestals.Count != towerAnchors.Count)
        {
            EnsurePedestals();
            rebuildRequested = false;
        }

        for (int i = 0; i < pedestals.Count; i++)
        {
            if (towerAnchors[i] == null || pedestals[i] == null) continue;
            pedestals[i].position = new Vector3(towerAnchors[i].position.x, pedestalWorldY, pedestalWorldZ);
        }
    }

    void EnsurePedestals()
    {
        if (pedestalSprites.Count == 0) return;

        while (pedestals.Count < towerAnchors.Count)
        {
            int index = pedestals.Count;
            Transform existing = transform.Find($"Tower Pedestal {index + 1}");

            if (existing == null)
            {
                GameObject pedestalObject = new($"Tower Pedestal {index + 1}");
                existing = pedestalObject.transform;
                existing.SetParent(transform, true);
            }

            pedestals.Add(existing);
        }

        for (int i = 0; i < pedestals.Count; i++)
        {
            Transform pedestal = pedestals[i];
            Sprite sprite = pedestalSprites[Mathf.Min(i, pedestalSprites.Count - 1)];
            if (pedestal == null || sprite == null) continue;

            SpriteRenderer renderer = pedestal.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = pedestal.gameObject.AddComponent<SpriteRenderer>();

            if (renderer.sprite != sprite)
                renderer.sprite = sprite;

            if (renderer.sortingOrder != sortingOrder)
                renderer.sortingOrder = sortingOrder;

            Vector2 targetSize = i < pedestalSizes.Count ? pedestalSizes[i] : defaultPedestalSize;
            Vector2 spriteSize = sprite.bounds.size;
            pedestal.localScale = new Vector3(
                spriteSize.x > 0f ? targetSize.x / spriteSize.x : 1f,
                spriteSize.y > 0f ? targetSize.y / spriteSize.y : 1f,
                1f);
        }
    }
}
