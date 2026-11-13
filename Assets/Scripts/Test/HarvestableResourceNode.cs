using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class HarvestableResourceNode : MonoBehaviour, IHarvestableResource
{
    [SerializeField] [Tooltip("이 자원의 표시 이름입니다.")]
    [FormerlySerializedAs("resourceName")]
    private string resName = "Wood";
    [SerializeField, Min(0.1f)] [Tooltip("자원을 채취하는 데 걸리는 기본 시간입니다.")]
    [FormerlySerializedAs("harvestDuration")]
    private float harvestSec = 5f;
    [SerializeField, Min(1)] [Tooltip("채취 완료 시 생성할 자원 개수입니다.")]
    [FormerlySerializedAs("dropAmount")]
    private int dropCount = 3;
    [SerializeField, Min(1)]
    [Tooltip("채취 속도 증가에 반영할 작업자 수의 상한입니다.")]
    [FormerlySerializedAs("maximumHarvesters")]
    private int maxWorkers = 3;
    [SerializeField] [Tooltip("채취 완료 시 생성할 드롭 자원 프리팹입니다.")]
    [FormerlySerializedAs("dropPrefab")]
    private GameObject prefab;
    [SerializeField] [Tooltip("채취 완료 후 자원 오브젝트에 적용할 스프라이트입니다.")]
    [FormerlySerializedAs("depletedSprite")]
    private Sprite emptySprite;
    [SerializeField] [Tooltip("자원 오브젝트의 스프라이트 렌더러입니다.")]
    [FormerlySerializedAs("spriteRenderer")]
    private SpriteRenderer sprite;
    [SerializeField] [Tooltip("개별 채취 진행 표시 컴포넌트입니다.")]
    [FormerlySerializedAs("progressBar")]
    private HarvestProgressBarUI bar;
    [SerializeField, Min(0f)] [Tooltip("드롭 자원이 흩어질 최대 반경입니다.")]
    [FormerlySerializedAs("dropScatterRadius")]
    private float scatter = 0.35f;

    private readonly HashSet<GoblinResourceHarvester> harvesters = new HashSet<GoblinResourceHarvester>();
    private float elapsedHarvestTime;
    private bool isDepleted;

    public bool IsDepleted => isDepleted;
    public bool IsHarvesting => harvesters.Count > 0;
    public int MaxConcurrentWorkers => Mathf.Max(1, maxWorkers);
    public float Progress => harvestSec <= 0f ? 1f : Mathf.Clamp01(elapsedHarvestTime / harvestSec);
    public string ResourceName => resName;

    public event Action<IHarvestableResource> HarvestStarted;
    public event Action<IHarvestableResource, float> ProgressChanged;
    public event Action<IHarvestableResource> HarvestStopped;
    public event Action<IHarvestableResource> HarvestCompleted;

    private void Reset() => CacheComponents();

    private void Awake()
    {
        CacheComponents();
    }

    private void Start()
    {
        CacheComponents();
        if (bar != null) bar.Bind(this, sprite);
    }

    private void Update()
    {
        if (harvesters.Count == 0 || isDepleted) return;

        elapsedHarvestTime += Time.deltaTime * Mathf.Min(harvesters.Count, maxWorkers);
        ProgressChanged?.Invoke(this, Progress);

        if (Progress >= 1f) CompleteHarvest();
    }

    public bool TryAddHarvester(GoblinResourceHarvester harvester)
    {
        if (harvester == null || isDepleted || harvesters.Contains(harvester))
            return false;

        var wasIdle = harvesters.Count == 0;
        harvesters.Add(harvester);
        if (wasIdle) HarvestStarted?.Invoke(this);
        ProgressChanged?.Invoke(this, Progress);
        return true;
    }

    public void RemoveHarvester(GoblinResourceHarvester harvester)
    {
        if (harvester == null || !harvesters.Remove(harvester)) return;
        if (harvesters.Count == 0 && !isDepleted) HarvestStopped?.Invoke(this);
    }

    private void CompleteHarvest()
    {
        elapsedHarvestTime = harvestSec;
        isDepleted = true;
        harvesters.Clear();

        foreach (var obstacle in GetComponents<Collider2D>())
            obstacle.enabled = false;

        if (sprite != null && emptySprite != null)
            sprite.sprite = emptySprite;

        SpawnDrops();
        ProgressChanged?.Invoke(this, 1f);
        HarvestCompleted?.Invoke(this);
    }

    private void SpawnDrops()
    {
        if (prefab == null) return;

        var origin = transform.position;
        var sourceRenderer = sprite;
        for (var i = 0; i < dropCount; i++)
        {
            var offset = UnityEngine.Random.insideUnitCircle * scatter;
            var groundPosition = origin + new Vector3(offset.x, offset.y, 0f);
            var drop = Instantiate(prefab, groundPosition, Quaternion.identity);
            var dropRenderer = drop.GetComponent<SpriteRenderer>();
            var landingPosition = groundPosition;
            if (sourceRenderer != null && dropRenderer != null)
            {
                dropRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
                dropRenderer.sortingOrder = sourceRenderer.sortingOrder + 1;
            }

            if (dropRenderer != null)
            {
                var localBottomOffset = new Vector3(0f, -dropRenderer.localBounds.min.y, 0f);
                landingPosition += dropRenderer.transform.TransformVector(localBottomOffset);
            }

            var animation = drop.GetComponent<ResourceDropLandingAnimation>();
            if (animation == null) animation = drop.AddComponent<ResourceDropLandingAnimation>();
            animation.Play(landingPosition);
        }
    }

    private void CacheComponents()
    {
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (bar == null) bar = GetComponentInChildren<HarvestProgressBarUI>(true);
    }
}
