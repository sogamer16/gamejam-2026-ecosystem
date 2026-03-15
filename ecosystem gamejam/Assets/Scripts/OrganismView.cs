using UnityEngine;

public class OrganismView : MonoBehaviour
{
    private Transform cachedTransform;
    private Transform modelRoot;
    private Renderer[] renderers;
    private Vector3 baseLocalPosition;
    private Vector3 moveTarget;
    private float bobSeed;
    private float swimSpeed;
    private float turnSpeed;
    private float verticalAmplitude;
    private float fishSwimBandY;
    private Vector3 fishSwimExtents = new Vector3(0.95f, 1.35f, 0.95f);
    private float fishSurfaceY = 1.45f;
    private float fishFloorY = -1.55f;
    private float baseVisualScale = 1f;
    private Color baseColor;
    private Quaternion visualRotationOffset = Quaternion.identity;
    private MaterialPropertyBlock propertyBlock;

    public SpeciesType Species { get; private set; }
    public float Health { get; private set; }
    public float ReproductionProgress { get; set; }

    public void Initialize(
        SpeciesType species,
        string displayName,
        Color color,
        Transform parent,
        Vector3 localPosition,
        GameObject modelPrefab,
        float visualScale)
    {
        Species = species;
        Health = 1f;
        baseColor = color;
        bobSeed = Random.Range(0f, 10f);
        cachedTransform = transform;
        cachedTransform.SetParent(parent, false);
        cachedTransform.localPosition = localPosition;
        cachedTransform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        cachedTransform.localScale = Vector3.one;
        baseLocalPosition = localPosition;
        fishSwimBandY = Mathf.Clamp(localPosition.y, -0.95f, 0.95f);
        moveTarget = PickTarget();
        swimSpeed = species == SpeciesType.Fish ? Random.Range(0.72f, 1.25f) : species == SpeciesType.Snail ? Random.Range(0.12f, 0.22f) : Random.Range(0.05f, 0.12f);
        turnSpeed = species == SpeciesType.Fish ? 3.2f : 2.4f;
        verticalAmplitude = species == SpeciesType.Fish ? 0.01f : species == SpeciesType.Snail ? 0.01f : 0.03f;

        modelRoot = new GameObject("ModelRoot").transform;
        modelRoot.SetParent(cachedTransform, false);

        if (modelPrefab != null)
        {
            GameObject model = Instantiate(modelPrefab, modelRoot);
            model.name = displayName + "Model";
            model.transform.localPosition = Vector3.zero;
            visualRotationOffset = GetVisualRotationOffset(species);
            model.transform.localRotation = visualRotationOffset;
            model.transform.localScale = Vector3.one * visualScale;
            baseVisualScale = visualScale;
            renderers = model.GetComponentsInChildren<Renderer>(true);
        }
        else
        {
            GameObject fallback = CreateFallbackModel(species, visualScale);
            fallback.name = displayName + "Fallback";
            fallback.transform.SetParent(modelRoot, false);
            baseVisualScale = visualScale;
            renderers = fallback.GetComponentsInChildren<Renderer>(true);
        }

        RefreshHealthVisual();
    }

    public void AdjustHealth(float delta)
    {
        Health = Mathf.Clamp01(Health + delta);
        RefreshHealthVisual();
    }

    public void ConfigureFishSwimArea(Vector3 extents, float floorY, float surfaceY)
    {
        fishSwimExtents = new Vector3(Mathf.Max(0.2f, extents.x), Mathf.Max(0.2f, extents.y), Mathf.Max(0.2f, extents.z));
        fishFloorY = floorY;
        fishSurfaceY = surfaceY;
        fishSwimBandY = Mathf.Clamp(fishSwimBandY, fishFloorY + 0.1f, fishSurfaceY - 0.1f);
        moveTarget = PickTarget();
        if (Species == SpeciesType.Fish && cachedTransform != null)
        {
            cachedTransform.localPosition = ClampFishPosition(cachedTransform.localPosition);
        }
    }

    public void SetBasePosition(Vector3 position)
    {
        baseLocalPosition = position;
    }

    public void SetVisualScaleMultiplier(float multiplier)
    {
        if (modelRoot == null)
        {
            return;
        }

        float clamped = Mathf.Max(0.1f, multiplier);
        modelRoot.localScale = Vector3.one * clamped;
    }

    public float GetBaseVisualScale()
    {
        return baseVisualScale;
    }

    public bool IsDead()
    {
        return Health <= 0.01f;
    }

    private void Update()
    {
        if (cachedTransform == null)
        {
            return;
        }

        float delta = Application.isPlaying ? Time.deltaTime : 0.016f;
        if (Species == SpeciesType.Algae)
        {
            float sway = Mathf.Sin((Time.time * 1.6f) + bobSeed) * 0.08f;
            cachedTransform.localPosition = Vector3.Lerp(cachedTransform.localPosition, baseLocalPosition + new Vector3(sway, Mathf.Cos((Time.time * 1.1f) + bobSeed) * 0.06f, 0f), delta * 2f);
            if (modelRoot != null) modelRoot.localRotation = Quaternion.Euler(0f, Mathf.Sin((Time.time * 0.8f) + bobSeed) * 18f, sway * 40f);
            return;
        }

        if (Vector3.Distance(cachedTransform.localPosition, moveTarget) < 0.22f)
        {
            moveTarget = PickTarget();
        }

        Vector3 targetPosition = Vector3.MoveTowards(cachedTransform.localPosition, moveTarget, swimSpeed * delta);
        if (Species == SpeciesType.Fish)
        {
            targetPosition.y = Mathf.Lerp(targetPosition.y, fishSwimBandY, delta * 2.2f);
            targetPosition = ClampFishPosition(targetPosition);
        }
        else
        {
            targetPosition += new Vector3(0f, Mathf.Sin((Time.time * 1.7f) + bobSeed) * verticalAmplitude, 0f);
        }
        cachedTransform.localPosition = targetPosition;

        Vector3 direction = moveTarget - cachedTransform.localPosition;
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            cachedTransform.localRotation = Quaternion.Slerp(cachedTransform.localRotation, targetRotation, delta * turnSpeed);
        }

        if (modelRoot != null && Species == SpeciesType.Fish)
        {
            Quaternion swimMotion = Quaternion.Euler(
                Mathf.Sin((Time.time * 6.2f) + bobSeed) * 8f,
                Mathf.Sin((Time.time * 2.4f) + bobSeed) * 3f,
                Mathf.Sin((Time.time * 5.1f) + bobSeed) * 7f);
            modelRoot.localRotation = visualRotationOffset * swimMotion;
        }
        else if (modelRoot != null && Species == SpeciesType.Snail)
        {
            modelRoot.localRotation = Quaternion.Euler(0f, 180f, Mathf.Sin((Time.time * 3f) + bobSeed) * 6f);
        }
    }

    private void RefreshHealthVisual()
    {
        if (renderers == null)
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        Color faded = Color.Lerp(baseColor, new Color(0.78f, 0.8f, 0.78f, 1f), 1f - Health);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            propertyBlock.Clear();
            renderer.GetPropertyBlock(propertyBlock);
            Material sharedMaterial = renderer.sharedMaterial;
            if (sharedMaterial != null && sharedMaterial.HasProperty("_BaseColor"))
            {
                propertyBlock.SetColor("_BaseColor", faded);
            }
            else if (sharedMaterial != null && sharedMaterial.HasProperty("_Color"))
            {
                propertyBlock.SetColor("_Color", faded);
            }

            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private Vector3 PickTarget()
    {
        if (Species == SpeciesType.Snail)
        {
            return new Vector3(Random.Range(-1.15f, 1.15f), Random.Range(-1.45f, -0.35f), Random.Range(-1.15f, 1.15f));
        }

        if (Species == SpeciesType.Algae)
        {
            return baseLocalPosition;
        }

        return PickFishTarget();
    }

    private Vector3 PickFishTarget()
    {
        Vector2 circle = Random.insideUnitCircle * 0.92f;
        float targetY = Mathf.Clamp(fishSwimBandY + Random.Range(-0.18f, 0.18f), fishFloorY + 0.25f, fishSurfaceY - 0.25f);
        Vector3 target = new Vector3(circle.x * fishSwimExtents.x, targetY, circle.y * fishSwimExtents.z);
        return ClampFishPosition(target);
    }

    private Vector3 ClampFishPosition(Vector3 position)
    {
        position.y = Mathf.Clamp(position.y, fishFloorY, fishSurfaceY);

        Vector2 planar = new Vector2(position.x / fishSwimExtents.x, position.z / fishSwimExtents.z);
        float magnitude = planar.magnitude;
        if (magnitude > 1f)
        {
            planar /= magnitude;
            position.x = planar.x * fishSwimExtents.x;
            position.z = planar.y * fishSwimExtents.z;
        }

        return position;
    }

    private static Quaternion GetVisualRotationOffset(SpeciesType species)
    {
        if (species == SpeciesType.Fish) return Quaternion.Euler(0f, -90f, 180f);
        if (species == SpeciesType.Snail) return Quaternion.Euler(0f, 180f, 0f);
        return Quaternion.identity;
    }

    private static GameObject CreateFallbackModel(SpeciesType species, float visualScale)
    {
        GameObject root = new GameObject(species + "FallbackRoot");
        if (species == SpeciesType.Snail)
        {
            GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shell.transform.SetParent(root.transform, false);
            shell.transform.localScale = new Vector3(0.9f, 0.75f, 0.9f) * visualScale;
            shell.transform.localPosition = new Vector3(0f, 0.12f, -0.04f);

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = new Vector3(0.5f, 0.28f, 0.9f) * visualScale;
            body.transform.localPosition = new Vector3(0f, -0.08f, 0.12f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(root.transform, false);
            head.transform.localScale = Vector3.one * (0.28f * visualScale);
            head.transform.localPosition = new Vector3(0f, 0.02f, 0.42f);
        }
        else if (species == SpeciesType.Algae)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject stalk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stalk.transform.SetParent(root.transform, false);
                stalk.transform.localScale = new Vector3(0.12f, 0.5f + (i * 0.08f), 0.12f) * visualScale;
                stalk.transform.localPosition = new Vector3((i - 1) * 0.12f * visualScale, 0f, Random.Range(-0.08f, 0.08f));
                stalk.transform.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), 0f, Random.Range(-12f, 12f));
            }
        }
        else
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.transform.SetParent(root.transform, false);
            body.transform.localScale = Vector3.one * visualScale;
        }

        return root;
    }
}
