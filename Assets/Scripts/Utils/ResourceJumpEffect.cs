using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class ResourceJumpEffect : Singleton<ResourceJumpEffect>
{
    private class TargetState
    {
        public double currentValue;
        public long targetValue;
        public Vector2 baseAnchoredPos;
        public Vector3 baseScale;
        public Color baseColor;
        public Tween countTween;
        public Tween moveTween;
        public Tween scaleTween;
        public Tween colorTween;
        public bool initialized;
        public long lastDisplayedValue;
        public bool hasDisplayedValue;
    }

    [Header("점프 연출")]
    [SerializeField] private float jumpHeight = 20f;
    [SerializeField] private float jumpDuration = 0.24f;
    [SerializeField] private float scaleMultiplier = 1.2f;

    [Header("색상 연출")]
    [SerializeField] private Color increaseFlashColor = new Color(0.4f, 1f, 0.5f, 1f);
    [SerializeField] private Color decreaseFlashColor = new Color(1f, 0.4f, 0.4f, 1f);
    [SerializeField] private float colorDuration = 0.22f;

    [Header("숫자 카운팅")]
    [SerializeField] private float countDuration = 0.2f;
    [SerializeField] private string numberFormat = "N0";

    private readonly Dictionary<TMP_Text, TargetState> _states = new Dictionary<TMP_Text, TargetState>();

    /// <param name="targetText">숫자를 표시할 TextMeshPro</param>
    /// <param name="newValue">변경될 최종 숫자</param>
    /// <param name="startValue">시작 숫자 지정 시 전달 (생략 시 현재 값 기준)</param>
    public void Play(TMP_Text targetText, long newValue, long? startValue = null)
    {
        if (targetText == null) return;

        RectTransform rect = targetText.rectTransform;
        TargetState state = GetOrCreateState(targetText, rect);

        if (startValue.HasValue && (state.countTween == null || !state.countTween.IsActive()))
        {
            state.currentValue = startValue.Value;
        }

        long prevTarget = state.targetValue;
        state.targetValue = newValue;
        bool isIncrease = newValue >= prevTarget && newValue >= (long)System.Math.Round(state.currentValue);

        KillTween(state.countTween);
        KillTween(state.moveTween);
        KillTween(state.scaleTween);
        KillTween(state.colorTween);

        rect.anchoredPosition = new Vector2(state.baseAnchoredPos.x, state.baseAnchoredPos.y);
        rect.localScale = state.baseScale;
        targetText.color = state.baseColor;

        if ((long)System.Math.Round(state.currentValue) == newValue && prevTarget == newValue)
        {
            state.currentValue = newValue;
            SetDisplayedValue(targetText, state, newValue);
            return;
        }

        state.countTween = DOTween.To(
            () => state.currentValue,
            v =>
            {
                state.currentValue = v;
                if (targetText != null)
                {
                    long shown = System.Math.Abs(state.targetValue - v) < 0.5
                        ? state.targetValue
                        : (long)System.Math.Round(v);
                    SetDisplayedValue(targetText, state, shown);
                }
            },
            (double)newValue,
            countDuration
        )
        .SetEase(Ease.OutCubic)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            state.currentValue = state.targetValue;
            if (targetText != null)
            {
                SetDisplayedValue(targetText, state, state.targetValue);
            }
        });

        float fixedX = state.baseAnchoredPos.x;
        float baseY = state.baseAnchoredPos.y;
        float peakY = baseY + jumpHeight;

        state.moveTween = DOTween.To(
            () => rect.anchoredPosition.y,
            y =>
            {
                if (rect != null)
                {
                    rect.anchoredPosition = new Vector2(fixedX, y);
                }
            },
            peakY,
            jumpDuration * 0.45f
        )
        .SetEase(Ease.OutQuad)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            if (rect == null) return;
            state.moveTween = DOTween.To(
                () => rect.anchoredPosition.y,
                y =>
                {
                    if (rect != null)
                    {
                        rect.anchoredPosition = new Vector2(fixedX, y);
                    }
                },
                baseY,
                jumpDuration * 0.55f
            )
            .SetEase(Ease.InQuad)
            .SetUpdate(true);
        });

        Vector3 peakScale = state.baseScale * scaleMultiplier;
        state.scaleTween = rect.DOScale(peakScale, jumpDuration * 0.45f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (rect == null) return;
                state.scaleTween = rect.DOScale(state.baseScale, jumpDuration * 0.55f)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true);
            });

        Color flashColor = isIncrease ? increaseFlashColor : decreaseFlashColor;
        state.colorTween = targetText.DOColor(flashColor, colorDuration * 0.35f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                if (targetText == null) return;
                state.colorTween = targetText.DOColor(state.baseColor, colorDuration * 0.65f)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true);
            });
    }

    /// <param name="targetText">숫자를 표시할 TextMeshPro</param>
    /// <param name="value">즉시 반영할 숫자</param>
    public void SetValueImmediate(TMP_Text targetText, long value)
    {
        if (targetText == null) return;

        RectTransform rect = targetText.rectTransform;
        TargetState state = GetOrCreateState(targetText, rect);

        KillAll(state);
        state.currentValue = value;
        state.targetValue = value;
        rect.anchoredPosition = new Vector2(state.baseAnchoredPos.x, state.baseAnchoredPos.y);
        rect.localScale = state.baseScale;
        targetText.color = state.baseColor;
        state.hasDisplayedValue = false;
        SetDisplayedValue(targetText, state, value);
    }

    private TargetState GetOrCreateState(TMP_Text targetText, RectTransform rect)
    {
        if (!_states.TryGetValue(targetText, out TargetState state))
        {
            long parsed = ParseInitialValue(targetText.text);
            state = new TargetState
            {
                currentValue = parsed,
                targetValue = parsed,
                baseAnchoredPos = rect.anchoredPosition,
                baseScale = rect.localScale,
                baseColor = targetText.color,
                initialized = true
            };
            _states[targetText] = state;
        }
        else if (!state.initialized)
        {
            state.baseAnchoredPos = rect.anchoredPosition;
            state.baseScale = rect.localScale;
            state.baseColor = targetText.color;
            state.initialized = true;
        }

        return state;
    }

    private static long ParseInitialValue(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return 0;

        var cleaned = new StringBuilder(raw.Length);
        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (char.IsDigit(c) || (c == '-' && cleaned.Length == 0))
            {
                cleaned.Append(c);
            }
        }

        return long.TryParse(cleaned.ToString(), out long val) ? val : 0;
    }

    private void SetDisplayedValue(TMP_Text targetText, TargetState state, long value)
    {
        if (state.hasDisplayedValue && state.lastDisplayedValue == value) return;

        state.lastDisplayedValue = value;
        state.hasDisplayedValue = true;
        targetText.text = value.ToString(numberFormat);
    }

    private static void KillTween(Tween tween)
    {
        if (tween != null && tween.IsActive())
        {
            tween.Kill(false);
        }
    }

    private static void KillAll(TargetState state)
    {
        KillTween(state.countTween);
        KillTween(state.moveTween);
        KillTween(state.scaleTween);
        KillTween(state.colorTween);
    }

    private void OnDestroy()
    {
        foreach (var kvp in _states)
        {
            KillAll(kvp.Value);
        }
        _states.Clear();
    }
}
