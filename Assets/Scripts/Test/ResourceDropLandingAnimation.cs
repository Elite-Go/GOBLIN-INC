using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class ResourceDropLandingAnimation : MonoBehaviour
{
    [SerializeField, Min(0.05f)] [Tooltip("자원이 떨어지는 연출의 지속 시간입니다.")]
    [FormerlySerializedAs("fallDuration")]
    private float fallSec = 0.45f;
    [SerializeField, Min(0f)] [Tooltip("자원이 떨어지기 시작하는 높이입니다.")]
    [FormerlySerializedAs("fallHeight")]
    private float fallH = 0.7f;
    [SerializeField, Range(0.1f, 1f)] [Tooltip("낙하 시작 시 자원의 크기 비율입니다.")]
    [FormerlySerializedAs("initialScale")]
    private float startScale = 0.72f;

    private Coroutine animationRoutine;
    private Vector3 originalScale;

    private void Awake() => originalScale = transform.localScale;

    public void Play(Vector3 landingPosition)
    {
        if (animationRoutine != null) StopCoroutine(animationRoutine);
        originalScale = transform.localScale;
        animationRoutine = StartCoroutine(AnimateFall(landingPosition));
    }

    private IEnumerator AnimateFall(Vector3 landingPosition)
    {
        var startPosition = landingPosition + Vector3.up * fallH;
        transform.position = startPosition;
        transform.localScale = originalScale * startScale;
        var elapsed = 0f;

        while (elapsed < fallSec)
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / fallSec);
            transform.position = Vector3.Lerp(startPosition, landingPosition, progress);
            transform.localScale = Vector3.Lerp(originalScale * startScale, originalScale, progress);
            yield return null;
        }

        transform.position = landingPosition;
        transform.localScale = originalScale;
        animationRoutine = null;
    }
}
