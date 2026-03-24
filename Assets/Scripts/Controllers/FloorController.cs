using UnityEngine;

public sealed class FloorController : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private const float ForcedMinDespawnY = -30f;

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float despawnY = -30f;
    [SerializeField] private float darkenStartY = -10f;

    private Renderer[] _renderers;
    private Color[] _baseColors;
    private Color[] _legacyColors;
    private bool[] _hasBaseColor;
    private bool[] _hasLegacyColor;
    private MaterialPropertyBlock _propertyBlock;

    public void Configure(float speed, float targetDespawnY)
    {
        moveSpeed = speed;
        despawnY = Mathf.Min(targetDespawnY, ForcedMinDespawnY);
        CacheRendererDataIfNeeded();
        ApplyDarkness(0f);
    }

    private void Awake()
    {
        CacheRendererDataIfNeeded();
    }

    private void OnEnable()
    {
        CacheRendererDataIfNeeded();
        despawnY = Mathf.Min(despawnY, ForcedMinDespawnY);
        ApplyDarkness(0f);
    }

    private void Update()
    {
        transform.position += Vector3.down * (moveSpeed * Time.deltaTime);

        float darkness = GetDarknessByHeight(transform.position.y);
        ApplyDarkness(darkness);

        if (transform.position.y <= ForcedMinDespawnY)
            Managers.Resource.Destory(gameObject);
    }

    private void CacheRendererDataIfNeeded()
    {
        if (_renderers != null)
            return;

        _renderers = GetComponentsInChildren<Renderer>(true);
        _baseColors = new Color[_renderers.Length];
        _legacyColors = new Color[_renderers.Length];
        _hasBaseColor = new bool[_renderers.Length];
        _hasLegacyColor = new bool[_renderers.Length];
        _propertyBlock = new MaterialPropertyBlock();

        for (int i = 0; i < _renderers.Length; i++)
        {
            Material shared = _renderers[i].sharedMaterial;
            if (shared == null)
                continue;

            _hasBaseColor[i] = shared.HasProperty(BaseColorId);
            if (_hasBaseColor[i])
                _baseColors[i] = shared.GetColor(BaseColorId);

            _hasLegacyColor[i] = shared.HasProperty(ColorId);
            if (_hasLegacyColor[i])
                _legacyColors[i] = shared.GetColor(ColorId);
        }
    }

    private float GetDarknessByHeight(float currentY)
    {
        float fadeLength = Mathf.Max(0.001f, darkenStartY - despawnY);
        return Mathf.Clamp01((darkenStartY - currentY) / fadeLength);
    }

    private void ApplyDarkness(float darkness)
    {
        if (_renderers == null || _propertyBlock == null)
            return;

        for (int i = 0; i < _renderers.Length; i++)
        {
            Renderer renderer = _renderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(_propertyBlock);

            if (_hasBaseColor[i])
                _propertyBlock.SetColor(BaseColorId, Color.Lerp(_baseColors[i], Color.black, darkness));

            if (_hasLegacyColor[i])
                _propertyBlock.SetColor(ColorId, Color.Lerp(_legacyColors[i], Color.black, darkness));

            renderer.SetPropertyBlock(_propertyBlock);
            _propertyBlock.Clear();
        }
    }
}
