using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class HarvestProgressBarUI : MonoBehaviour
{
    [SerializeField] [Tooltip("진행 표시의 투명도와 상호작용을 제어하는 CanvasGroup입니다.")]
    [FormerlySerializedAs("canvasGroup")]
    private CanvasGroup group;
    [SerializeField] [Tooltip("진행 표시의 배경 이미지입니다.")]
    [FormerlySerializedAs("backgroundImage")]
    private Image back;
    [SerializeField] [Tooltip("남은 채취 시간을 표시하는 채움 이미지입니다.")]
    [FormerlySerializedAs("fillImage")]
    private Image fill;
    [SerializeField] [Tooltip("월드 공간 진행 표시의 Canvas입니다.")]
    [FormerlySerializedAs("worldCanvas")]
    private Canvas canvas;

    private IHarvestableResource currentResource;

    private void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (back != null) back.color = Color.white;
        if (fill != null)
        {
            fill.color = new Color(0.62f, 0.9f, 0.38f, 1f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        }
        SetVisible(false);
    }

    public void Bind(IHarvestableResource resource, SpriteRenderer sourceRenderer)
    {
        Unsubscribe();
        currentResource = resource;
        if (currentResource == null)
        {
            SetVisible(false);
            return;
        }

        currentResource.HarvestStarted += HandleHarvestStarted;
        currentResource.ProgressChanged += HandleProgressChanged;
        currentResource.HarvestStopped += HandleHarvestStopped;
        currentResource.HarvestCompleted += HandleHarvestCompleted;
        if (canvas != null && sourceRenderer != null)
        {
            canvas.overrideSorting = true;
            canvas.sortingLayerID = sourceRenderer.sortingLayerID;
            canvas.sortingOrder = sourceRenderer.sortingOrder + 20;
        }
        HandleProgressChanged(currentResource, currentResource.Progress);
        SetVisible(currentResource.IsHarvesting);
    }

    private void Update()
    {
        if (currentResource != null && currentResource.IsHarvesting)
            HandleProgressChanged(currentResource, currentResource.Progress);
    }

    private void HandleHarvestStarted(IHarvestableResource resource)
    {
        if (resource != currentResource) return;
        HandleProgressChanged(resource, resource.Progress);
        SetVisible(true);
    }

    private void HandleProgressChanged(IHarvestableResource resource, float progress)
    {
        if (resource != currentResource || fill == null) return;
        fill.fillAmount = 1f - Mathf.Clamp01(progress);
    }

    private void HandleHarvestStopped(IHarvestableResource resource)
    {
        if (resource == currentResource) SetVisible(false);
    }

    private void HandleHarvestCompleted(IHarvestableResource resource)
    {
        if (resource == currentResource) SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (group == null) return;
        group.alpha = visible ? 1f : 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void Unsubscribe()
    {
        if (currentResource == null) return;
        currentResource.HarvestStarted -= HandleHarvestStarted;
        currentResource.ProgressChanged -= HandleProgressChanged;
        currentResource.HarvestStopped -= HandleHarvestStopped;
        currentResource.HarvestCompleted -= HandleHarvestCompleted;
        currentResource = null;
    }

    private void OnDestroy() => Unsubscribe();
}
