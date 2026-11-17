using UnityEngine;
using System.Collections.Generic;

public class HoverManager : MonoBehaviour
{
    [SerializeField] private LayerMask hoverLayerMask;
    [SerializeField] private Material hoverMaterial;

    private Camera cachedCamera;
    private SpriteRenderer currentRenderer;
    
    private readonly Dictionary<SpriteRenderer, Material> originalMaterials = new Dictionary<SpriteRenderer, Material>();

    private void Awake()
    {
        cachedCamera = Camera.main;
        if (cachedCamera == null)
        {
            Debug.LogError("SpriteHoverManager: Main Camera를 찾을 수 없습니다.");
        }
    }

    private void Update()
    {
        if (cachedCamera == null) return;

        // 마우스의 스크린 좌표를 2D 월드 좌표로 변환합니다.
        Vector2 mouseWorldPos = cachedCamera.ScreenToWorldPoint(Input.mousePosition);

        // 2D 전용 오버랩 포인트를 사용하여 마우스 위치에 있는 콜라이더를 정확히 판정합니다.
        Collider2D hitCollider = Physics2D.OverlapPoint(mouseWorldPos, hoverLayerMask);

        SpriteRenderer hitRenderer = null;
        if (hitCollider != null)
        {
            // 콜라이더에서 SpriteRenderer를 가져옵니다. (없으면 부모/자식 구조일 수 있으니 GetComponent 사용)
            hitRenderer = hitCollider.GetComponent<SpriteRenderer>();
        }

        // 대상이 변경되었을 때만 머티리얼 교체 로직을 수행합니다.
        if (hitRenderer != currentRenderer)
        {
            // 1. 이전 오브젝트의 머티리얼 복원
            RestorePrevious();

            // 2. 새로운 오브젝트 처리
            if (hitRenderer != null && hoverMaterial != null)
            {
                currentRenderer = hitRenderer;

                // 최초 접근인 경우에만 원래 머티리얼 캐싱 (sharedMaterial 대신 material 접근 시 인스턴스화됨)
                if (!originalMaterials.ContainsKey(currentRenderer))
                {
                    originalMaterials[currentRenderer] = currentRenderer.material;
                }

                // 호버 머티리얼 적용
                currentRenderer.material = hoverMaterial;
            }
        }
    }

    private void RestorePrevious()
    {
        if (currentRenderer != null)
        {
            if (originalMaterials.TryGetValue(currentRenderer, out Material originalMat))
            {
                currentRenderer.material = originalMat;
            }
            currentRenderer = null;
        }
    }
}