using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class SpriteBoingPlayer : Singleton<SpriteBoingPlayer>
{
    [SerializeField] private float stretchX = 1.2f;
    [SerializeField] private float squashY = 0.8f;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private int tweenCapacity = 256;
    [SerializeField] private int sequenceCapacity = 32;

    private readonly Dictionary<Transform, Vector3> _baseScales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Tweener> _activeTweens = new Dictionary<Transform, Tweener>();

    protected override void Awake()
    {
        base.Awake();
        DOTween.SetTweensCapacity(tweenCapacity, sequenceCapacity);
    }

    public static void PlayOnce(Transform target, float customDuration = -1f)
    {
        if (target == null || Instance == null) return;
        Instance.PlayInternal(target, customDuration > 0f ? customDuration : Instance.duration);
    }

    private void PlayInternal(Transform target, float playDuration)
    {
        if (_activeTweens.TryGetValue(target, out var existingTween))
        {
            existingTween.Kill();
            _activeTweens.Remove(target);
        }

        if (!_baseScales.TryGetValue(target, out var baseScale))
        {
            baseScale = target.localScale;
            _baseScales[target] = baseScale;
        }
        else
        {
            target.localScale = baseScale;
        }

        Vector3 targetScale = new Vector3(
            baseScale.x * stretchX,
            baseScale.y * squashY,
            baseScale.z
        );

        var tween = target
            .DOScale(targetScale, playDuration * 0.5f)
            .SetEase(Ease.OutQuad)
            .SetLoops(2, LoopType.Yoyo)
            .SetLink(target.gameObject)
            .OnComplete(() => CleanupTarget(target))
            .OnKill(() => CleanupTarget(target));

        _activeTweens[target] = tween;
    }

    private void CleanupTarget(Transform target)
    {
        if (target != null && _baseScales.TryGetValue(target, out var baseScale))
        {
            target.localScale = baseScale;
        }

        _activeTweens.Remove(target);
    }

    public void ReleaseTarget(Transform target)
    {
        if (target == null) return;

        if (_activeTweens.TryGetValue(target, out var tween))
        {
            tween.Kill();
            _activeTweens.Remove(target);
        }

        _baseScales.Remove(target);
    }

    private void OnDestroy()
    {
        foreach (var pair in _activeTweens)
        {
            pair.Value?.Kill();
        }

        _activeTweens.Clear();
        _baseScales.Clear();
    }
}