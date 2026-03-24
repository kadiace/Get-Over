using UnityEngine;

[RequireComponent(typeof(Renderer))]
public sealed class TwoSidedDissolveParticleController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private string dissolveMaterialPath = "Arts/Scenes/Materials/TwoSidedDissolve";
    [SerializeField] private string contactObjectName = "WaterfallBottomDisk";
    [SerializeField] private string particleObjectName = "TwoSidedDissolveParticles";

    [Header("Spawn")]
    [SerializeField] private float emissionRate = 24f;
    [SerializeField] private float contactLineOffset = 0.03f;
    [SerializeField] private float radiusScale = 1.02f;

    private ParticleSystem _particleSystem;

    private void Awake()
    {
        Configure();
    }

    public void Configure()
    {
        Renderer waterfallRenderer = GetComponent<Renderer>();
        if (waterfallRenderer == null)
            return;

        EnsureParticleSystem();
        if (_particleSystem == null)
            return;

        ConfigurePlacement(waterfallRenderer);
        ConfigureParticleSystem(_particleSystem);
        _particleSystem.Play(true);
    }

    private void EnsureParticleSystem()
    {
        if (_particleSystem != null)
            return;

        Transform child = transform.Find(particleObjectName);
        if (child == null)
        {
            GameObject go = new(particleObjectName);
            go.transform.SetParent(transform, false);
            child = go.transform;
        }

        _particleSystem = child.GetComponent<ParticleSystem>();
        if (_particleSystem == null)
            _particleSystem = child.gameObject.AddComponent<ParticleSystem>();
    }

    private void ConfigurePlacement(Renderer waterfallRenderer)
    {
        Bounds waterfallBounds = waterfallRenderer.bounds;
        float radius = Mathf.Max(waterfallBounds.extents.x, waterfallBounds.extents.z) * radiusScale;

        float lineY = waterfallBounds.min.y;
        GameObject contactObject = GameObject.Find(contactObjectName);
        if (contactObject != null)
        {
            Renderer contactRenderer = contactObject.GetComponent<Renderer>();
            if (contactRenderer != null)
            {
                lineY = Mathf.Clamp(contactRenderer.bounds.max.y + contactLineOffset, waterfallBounds.min.y, waterfallBounds.max.y);
            }
        }

        Vector3 worldCenter = new(waterfallBounds.center.x, lineY, waterfallBounds.center.z);
        Transform particleTransform = _particleSystem.transform;
        particleTransform.position = worldCenter;
        particleTransform.rotation = Quaternion.identity;

        ParticleSystem.ShapeModule shape = _particleSystem.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = radius;
        shape.radiusThickness = 0f;
        shape.arc = 360f;
        shape.arcMode = ParticleSystemShapeMultiModeValue.Random;
    }

    private void ConfigureParticleSystem(ParticleSystem particleSystem)
    {
        ParticleSystem.MainModule main = particleSystem.main;
        main.loop = true;
        main.playOnAwake = true;
        main.maxParticles = 320;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.42f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.55f, 1.05f);
        main.gravityModifier = 0f;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = Color.white;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient dissolveGradient = new();
        dissolveGradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f),
            },
            new[]
            {
                new GradientAlphaKey(0.08f, 0f),
                new GradientAlphaKey(0.32f, 0.45f),
                new GradientAlphaKey(0.58f, 1f),
            });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(dissolveGradient);

        ParticleSystem.VelocityOverLifetimeModule velocity = particleSystem.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.18f, 0.65f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);

        ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.alignment = ParticleSystemRenderSpace.Local;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        Material dissolveMaterial = Resources.Load<Material>(dissolveMaterialPath);
        if (dissolveMaterial != null)
            renderer.sharedMaterial = dissolveMaterial;

        GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        MeshFilter meshFilter = tempQuad.GetComponent<MeshFilter>();
        if (meshFilter != null)
            renderer.mesh = meshFilter.sharedMesh;
        Destroy(tempQuad);
    }
}
