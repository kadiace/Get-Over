using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BubbleEmitterController : MonoBehaviour
{
    private enum SpawnShape
    {
        ColliderPlane,
        CircleOnWorldY,
    }

    [Header("Bubble FX")]
    [SerializeField] private string bubblePrefabPath = "Prefabs/TwoSidedDissolve";
    [SerializeField] private bool autoPlayOnEnable = false;
    [SerializeField] private int bubbleMinCount = 6;
    [SerializeField] private int bubbleMaxCount = 14;
    [SerializeField] private int bubbleAbsoluteMaxCount = 30;
    [SerializeField] private float bubbleDensityPerSquareUnit = 3.5f;
    [SerializeField] private float bubblePlaneOffset = 0.03f;
    [SerializeField] private float bubbleSpawnAreaPadding = 1f;
    [SerializeField] private float bubbleSpawnAreaExpand = 1f;
    [SerializeField] private Vector2 bubbleSpawnIntervalRange = new(0.12f, 0.24f);
    [SerializeField] private float bubbleStartScaleMultiplier = 0.22f;
    [SerializeField] private float bubbleMinScale = 0.1f;
    [SerializeField] private float bubbleMaxScale = 0.42f;
    [SerializeField] private Vector2 bubbleGrowDurationRange = new(0.08f, 0.22f);
    [SerializeField] private Vector2 bubbleLifetimeRange = new(0.65f, 1.45f);
    [SerializeField] private Vector2 fallbackPlaneSize = new(2f, 2f);
    [SerializeField] private SpawnShape spawnShape = SpawnShape.ColliderPlane;
    [SerializeField] private Vector3 circleWorldCenter = new(0f, 15f, 0f);
    [SerializeField] private float circleWorldRadius = 10f;
    [SerializeField] private float circleWorldY = 15f;
    [SerializeField] private bool circleEdgeOnly = true;
    [SerializeField] private float circleEdgeBandWidth = 0.4f;

    private MaterialPropertyBlock _bubblePropertyBlock;
    private Dictionary<int, int> _bubbleRunVersions;
    private HashSet<Poolable> _activeBubbles;
    private HashSet<int> _preparedBubbleIds;
    private GameObject _bubbleOriginal;
    private BoxCollider _boxCollider;
    private Coroutine _bubbleLoopCoroutine;
    private bool _isReturningActiveBubbles;

    private static readonly int DissolveThresholdId = Shader.PropertyToID("_DissolveThreshold");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");

    public void Configure(GameObject bubbleOriginal = null)
    {
        EnsureBubbleRuntimeState();
        CacheSpawnColliderIfNeeded();

        if (bubbleOriginal != null)
            _bubbleOriginal = bubbleOriginal;

        StartBubbleLoop();
    }

    public void ConfigureCircleWorldPlane(Vector3 center, float radius, float planeY)
    {
        spawnShape = SpawnShape.CircleOnWorldY;
        circleWorldCenter = center;
        circleWorldRadius = Mathf.Max(0.1f, radius);
        circleWorldY = planeY;
    }

    public void ConfigureCircleEdge(float bandWidth)
    {
        circleEdgeOnly = true;
        circleEdgeBandWidth = Mathf.Max(0.01f, bandWidth);
    }

    public void ConfigureEmissionDensity(int minCount, int maxCount, int absoluteMaxCount, float densityPerSquareUnit, Vector2 intervalRange)
    {
        bubbleMinCount = Mathf.Max(1, minCount);
        bubbleMaxCount = Mathf.Max(bubbleMinCount, maxCount);
        bubbleAbsoluteMaxCount = Mathf.Max(bubbleMaxCount, absoluteMaxCount);
        bubbleDensityPerSquareUnit = Mathf.Max(0f, densityPerSquareUnit);

        float minInterval = Mathf.Max(0.02f, intervalRange.x);
        float maxInterval = Mathf.Max(minInterval, intervalRange.y);
        bubbleSpawnIntervalRange = new Vector2(minInterval, maxInterval);
    }

    private void Awake()
    {
        EnsureBubbleRuntimeState();
        CacheSpawnColliderIfNeeded();
    }

    private void OnEnable()
    {
        EnsureBubbleRuntimeState();
        CacheSpawnColliderIfNeeded();

        if (autoPlayOnEnable)
            StartBubbleLoop();
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        if (_isReturningActiveBubbles)
            return;

        if (_bubbleLoopCoroutine != null)
        {
            StopCoroutine(_bubbleLoopCoroutine);
            _bubbleLoopCoroutine = null;
        }

        if (_activeBubbles == null || _bubbleRunVersions == null)
            return;

        if (_activeBubbles.Count == 0)
            return;

        Poolable[] activeSnapshot = new Poolable[_activeBubbles.Count];
        _activeBubbles.CopyTo(activeSnapshot);
        _activeBubbles.Clear();

        for (int i = 0; i < activeSnapshot.Length; i++)
        {
            Poolable bubble = activeSnapshot[i];
            if (bubble == null)
                continue;

            int bubbleId = bubble.GetInstanceID();
            InvalidateBubbleRun(bubbleId);
            if (bubble.IsUsing)
                Destroy(bubble.gameObject);
        }
    }

    public void ReturnAllActiveBubblesToPool()
    {
        if (!Application.isPlaying)
            return;

        EnsureBubbleRuntimeState();
        _isReturningActiveBubbles = true;

        if (_bubbleLoopCoroutine != null)
        {
            StopCoroutine(_bubbleLoopCoroutine);
            _bubbleLoopCoroutine = null;
        }

        if (_activeBubbles.Count > 0)
        {
            Poolable[] activeSnapshot = new Poolable[_activeBubbles.Count];
            _activeBubbles.CopyTo(activeSnapshot);
            _activeBubbles.Clear();

            for (int i = 0; i < activeSnapshot.Length; i++)
            {
                Poolable bubble = activeSnapshot[i];
                if (bubble == null)
                    continue;

                int bubbleId = bubble.GetInstanceID();
                InvalidateBubbleRun(bubbleId);
                if (bubble.IsUsing)
                    Managers.Pool.Push(bubble);
            }
        }

        _isReturningActiveBubbles = false;
    }

    private void CacheSpawnColliderIfNeeded()
    {
        if (_boxCollider != null)
            return;

        _boxCollider = GetComponentInChildren<BoxCollider>();
    }

    private void StartBubbleLoop()
    {
        if (_bubbleLoopCoroutine != null)
            StopCoroutine(_bubbleLoopCoroutine);

        SpawnBubblesFromPool(true);
        _bubbleLoopCoroutine = StartCoroutine(BubbleEmissionLoop());
    }

    private IEnumerator BubbleEmissionLoop()
    {
        while (isActiveAndEnabled)
        {
            float minInterval = Mathf.Max(0.02f, bubbleSpawnIntervalRange.x);
            float maxInterval = Mathf.Max(minInterval, bubbleSpawnIntervalRange.y);
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            if (!isActiveAndEnabled)
                yield break;

            SpawnBubblesFromPool(false);
        }
    }

    private void SpawnBubblesFromPool(bool initialBurst)
    {
        EnsureBubbleRuntimeState();

        if (_bubbleOriginal == null)
            _bubbleOriginal = Managers.Resource.Load<GameObject>(bubblePrefabPath);

        if (_bubbleOriginal == null)
            return;

        int minCount = Mathf.Max(1, bubbleMinCount);
        int maxCount = Mathf.Max(minCount, bubbleMaxCount);
        int randomCount = Random.Range(minCount, maxCount + 1);
        float spawnPlaneArea = ResolveSpawnPlaneArea();
        int densityCount = Mathf.RoundToInt(spawnPlaneArea * Mathf.Max(0f, bubbleDensityPerSquareUnit));
        int targetCount = Mathf.Max(randomCount, densityCount);
        int absoluteMaxCount = Mathf.Max(1, bubbleAbsoluteMaxCount);
        int spawnCount = Mathf.Clamp(targetCount, minCount, absoluteMaxCount);
        if (initialBurst)
            spawnCount = Mathf.Clamp(Mathf.RoundToInt(spawnCount * 1.35f), minCount, absoluteMaxCount);

        float areaScale = Mathf.Clamp(bubbleSpawnAreaPadding * bubbleSpawnAreaExpand, 0.1f, 3f);
        float safePlaneOffset = Mathf.Max(0f, bubblePlaneOffset);
        float minScale = Mathf.Max(0.05f, bubbleMinScale);
        float maxScale = Mathf.Max(minScale, bubbleMaxScale);
        float minGrow = Mathf.Max(0.01f, bubbleGrowDurationRange.x);
        float maxGrow = Mathf.Max(minGrow, bubbleGrowDurationRange.y);
        float minLifetime = Mathf.Max(minGrow, bubbleLifetimeRange.x);
        float maxLifetime = Mathf.Max(minLifetime, bubbleLifetimeRange.y);
        float startScaleMultiplier = Mathf.Clamp(bubbleStartScaleMultiplier, 0.01f, 1f);

        for (int i = 0; i < spawnCount; i++)
        {
            Poolable pooledBubble = Managers.Pool.Pop(_bubbleOriginal, transform);
            if (pooledBubble == null)
                continue;

            Transform bubbleTransform = pooledBubble.transform;
            Vector3 worldPosition = ResolveSpawnWorldPosition(areaScale, safePlaneOffset);
            bubbleTransform.localPosition = transform.InverseTransformPoint(worldPosition);
            bubbleTransform.localRotation = Quaternion.identity;

            float targetScale = Random.Range(minScale, maxScale);
            float startScale = targetScale * startScaleMultiplier;
            SetBubbleUniformWorldScale(bubbleTransform, startScale);

            PrepareBubbleForFx(pooledBubble.gameObject);
            ApplyBubbleMaterialVariation(pooledBubble.gameObject);

            int bubbleId = pooledBubble.GetInstanceID();
            int runVersion = GetNextBubbleRunVersion(bubbleId);
            _activeBubbles.Add(pooledBubble);

            float growDuration = Random.Range(minGrow, maxGrow);
            float lifetime = Random.Range(minLifetime, maxLifetime);
            StartCoroutine(GrowAndPopBubble(pooledBubble, bubbleId, runVersion, startScale, targetScale, growDuration, lifetime));
        }
    }

    private float ResolveSpawnPlaneArea()
    {
        if (spawnShape == SpawnShape.CircleOnWorldY)
        {
            float radius = Mathf.Max(0.1f, circleWorldRadius);
            return Mathf.PI * radius * radius;
        }

        if (_boxCollider != null)
        {
            float worldWidth = Mathf.Abs(_boxCollider.size.x * _boxCollider.transform.lossyScale.x);
            float worldHeight = Mathf.Abs(_boxCollider.size.y * _boxCollider.transform.lossyScale.y);
            return Mathf.Max(0.001f, worldWidth * worldHeight);
        }

        Vector3 lossyScale = transform.lossyScale;
        float width = Mathf.Max(0.1f, fallbackPlaneSize.x) * Mathf.Max(0.001f, Mathf.Abs(lossyScale.x));
        float height = Mathf.Max(0.1f, fallbackPlaneSize.y) * Mathf.Max(0.001f, Mathf.Abs(lossyScale.y));
        return Mathf.Max(0.001f, width * height);
    }

    private Vector3 ResolveSpawnWorldPosition(float areaScale, float safePlaneOffset)
    {
        if (spawnShape == SpawnShape.CircleOnWorldY)
        {
            float radius = Mathf.Max(0.1f, circleWorldRadius) * Mathf.Clamp(areaScale, 0.1f, 3f);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float spawnRadius = radius;

            if (!circleEdgeOnly)
            {
                spawnRadius = Random.value * radius;
            }
            else
            {
                float halfBand = Mathf.Max(0.005f, circleEdgeBandWidth * 0.5f);
                float minRadius = Mathf.Max(0.001f, radius - halfBand);
                float maxRadius = radius + halfBand;
                spawnRadius = Random.Range(minRadius, maxRadius);
            }

            Vector2 circlePoint = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
            return new Vector3(
                circleWorldCenter.x + circlePoint.x,
                circleWorldY,
                circleWorldCenter.z + circlePoint.y);
        }

        if (_boxCollider != null)
        {
            float localX = (Random.value - 0.5f) * _boxCollider.size.x * areaScale;
            float localY = (Random.value - 0.5f) * _boxCollider.size.y * areaScale;
            float localZ = _boxCollider.size.z * 0.5f + safePlaneOffset;
            Vector3 localOnTopPlane = _boxCollider.center + new Vector3(localX, localY, localZ);
            return _boxCollider.transform.TransformPoint(localOnTopPlane);
        }

        float fallbackWidth = Mathf.Max(0.1f, fallbackPlaneSize.x) * areaScale;
        float fallbackHeight = Mathf.Max(0.1f, fallbackPlaneSize.y) * areaScale;
        float offsetX = (Random.value - 0.5f) * fallbackWidth;
        float offsetY = (Random.value - 0.5f) * fallbackHeight;
        return transform.position
            + transform.right * offsetX
            + transform.up * offsetY
            + transform.forward * safePlaneOffset;
    }

    private void ApplyBubbleMaterialVariation(GameObject bubble)
    {
        EnsureBubbleRuntimeState();
        Renderer[] renderers = bubble.GetComponentsInChildren<Renderer>(true);
        float dissolveThreshold = Random.value;
        float seed = Random.Range(0f, 100f);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(_bubblePropertyBlock);
            _bubblePropertyBlock.SetFloat(DissolveThresholdId, dissolveThreshold);
            _bubblePropertyBlock.SetFloat(SeedId, seed);
            renderer.SetPropertyBlock(_bubblePropertyBlock);
            _bubblePropertyBlock.Clear();
        }
    }

    private int GetNextBubbleRunVersion(int bubbleId)
    {
        EnsureBubbleRuntimeState();
        int nextVersion = 1;
        if (_bubbleRunVersions.TryGetValue(bubbleId, out int currentVersion))
            nextVersion = currentVersion + 1;

        _bubbleRunVersions[bubbleId] = nextVersion;
        return nextVersion;
    }

    private void InvalidateBubbleRun(int bubbleId)
    {
        EnsureBubbleRuntimeState();
        if (_bubbleRunVersions.TryGetValue(bubbleId, out int currentVersion))
            _bubbleRunVersions[bubbleId] = currentVersion + 1;
        else
            _bubbleRunVersions[bubbleId] = 1;
    }

    private void EnsureBubbleRuntimeState()
    {
        _bubblePropertyBlock ??= new MaterialPropertyBlock();
        _bubbleRunVersions ??= new Dictionary<int, int>();
        _activeBubbles ??= new HashSet<Poolable>();
        _preparedBubbleIds ??= new HashSet<int>();
    }

    private void SetBubbleUniformWorldScale(Transform bubbleTransform, float targetWorldScale)
    {
        Transform parent = bubbleTransform.parent;
        Vector3 parentLossyScale = parent != null ? parent.lossyScale : Vector3.one;
        float safeX = Mathf.Max(0.001f, Mathf.Abs(parentLossyScale.x));
        float safeY = Mathf.Max(0.001f, Mathf.Abs(parentLossyScale.y));
        float safeZ = Mathf.Max(0.001f, Mathf.Abs(parentLossyScale.z));

        bubbleTransform.localScale = new Vector3(
            targetWorldScale / safeX,
            targetWorldScale / safeY,
            targetWorldScale / safeZ);
    }

    private void PrepareBubbleForFx(GameObject bubble)
    {
        if (bubble == null)
            return;

        EnsureBubbleRuntimeState();

        int bubbleId = bubble.GetInstanceID();
        if (_preparedBubbleIds.Contains(bubbleId))
            return;

        Rigidbody[] rigidbodies = bubble.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
        {
            Rigidbody body = rigidbodies[i];
            body.isKinematic = true;
            body.detectCollisions = false;
            Destroy(body);
        }

        Collider[] colliders = bubble.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            collider.enabled = false;
            Destroy(collider);
        }

        Rigidbody2D[] rigidbodies2D = bubble.GetComponentsInChildren<Rigidbody2D>(true);
        for (int i = 0; i < rigidbodies2D.Length; i++)
        {
            Rigidbody2D body2D = rigidbodies2D[i];
            body2D.simulated = false;
            Destroy(body2D);
        }

        Collider2D[] colliders2D = bubble.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders2D.Length; i++)
        {
            Collider2D collider2D = colliders2D[i];
            collider2D.enabled = false;
            Destroy(collider2D);
        }

        _preparedBubbleIds.Add(bubbleId);
    }

    private bool IsBubbleRunValid(Poolable pooledBubble, int bubbleId, int runVersion)
    {
        if (pooledBubble == null || !pooledBubble.IsUsing)
            return false;

        return _bubbleRunVersions.TryGetValue(bubbleId, out int activeRunVersion) && activeRunVersion == runVersion;
    }

    private IEnumerator GrowAndPopBubble(Poolable pooledBubble, int bubbleId, int runVersion, float startScale, float targetScale, float growDuration, float lifetime)
    {
        if (pooledBubble == null)
            yield break;

        Transform bubbleTransform = pooledBubble.transform;
        float elapsed = 0f;

        while (elapsed < growDuration)
        {
            if (!IsBubbleRunValid(pooledBubble, bubbleId, runVersion))
                yield break;

            elapsed += Time.deltaTime;
            float t = growDuration > Define.epsilon ? Mathf.Clamp01(elapsed / growDuration) : 1f;
            SetBubbleUniformWorldScale(bubbleTransform, Mathf.Lerp(startScale, targetScale, t));
            yield return null;
        }

        if (!IsBubbleRunValid(pooledBubble, bubbleId, runVersion))
            yield break;

        SetBubbleUniformWorldScale(bubbleTransform, targetScale);
        float restDuration = Mathf.Max(0f, lifetime - growDuration);
        if (restDuration > 0f)
            yield return new WaitForSeconds(restDuration);

        if (!IsBubbleRunValid(pooledBubble, bubbleId, runVersion))
            yield break;

        _activeBubbles.Remove(pooledBubble);
        InvalidateBubbleRun(bubbleId);
        Managers.Pool.Push(pooledBubble);
    }
}
