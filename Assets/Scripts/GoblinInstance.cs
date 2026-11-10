using System;
using UnityEngine;

namespace GoblinGame.Goblins
{
    /// <summary>
    /// 게임에서 소유하는 개별 고블린의 데이터.
    /// GameObject 풀링과 별개로 유지되며, Actor가 풀에 반환되어도 이 데이터는 삭제되지 않는다.
    /// </summary>
    [Serializable]
    public sealed class GoblinInstance
    {
        [Header("Identity")]
        [SerializeField] private string instanceId;
        [SerializeField] private string definitionId;

        [Header("Progress")]
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0f)] private float currentHealth;

        [Header("Last Known Location")]
        [SerializeField] private string mapId = "home";
        [SerializeField] private Vector2 savedPosition;

        public string InstanceId => instanceId;
        public string DefinitionId => definitionId;
        public int Level => Mathf.Max(1, level);
        public float CurrentHealth => Mathf.Max(0f, currentHealth);
        public bool IsAlive => CurrentHealth > 0f;
        public string MapId => mapId;
        public Vector2 SavedPosition => savedPosition;

        internal event Action<GoblinInstance> Changed;

        // Unity 직렬화 및 역직렬화를 위한 생성자
        public GoblinInstance() { }

        private GoblinInstance(string newInstanceId, string newDefinitionId, float startingHealth)
        {
            instanceId = newInstanceId;
            definitionId = newDefinitionId;
            level = 1;
            currentHealth = Mathf.Max(0f, startingHealth);
            mapId = "home";
            savedPosition = Vector2.zero;
        }

        internal static GoblinInstance Create(GoblinDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            return new GoblinInstance(
                Guid.NewGuid().ToString("N"),
                definition.Id,
                definition.BaseStats.MaxHealth);
        }

        /// <summary>세이브 데이터에서 기존 고유 ID와 개별 상태를 유지한 채 복원한다.</summary>
        internal static GoblinInstance Restore(
            string restoredInstanceId,
            string restoredDefinitionId,
            int restoredLevel,
            float restoredCurrentHealth,
            string restoredMapId,
            Vector2 restoredPosition)
        {
            if (string.IsNullOrWhiteSpace(restoredInstanceId))
                throw new ArgumentException("InstanceId는 비어 있을 수 없습니다.", nameof(restoredInstanceId));

            if (string.IsNullOrWhiteSpace(restoredDefinitionId))
                throw new ArgumentException("DefinitionId는 비어 있을 수 없습니다.", nameof(restoredDefinitionId));

            if (string.IsNullOrWhiteSpace(restoredMapId))
                throw new ArgumentException("MapId는 비어 있을 수 없습니다.", nameof(restoredMapId));

            if (float.IsNaN(restoredCurrentHealth) || float.IsInfinity(restoredCurrentHealth) || restoredCurrentHealth < 0f)
                throw new ArgumentOutOfRangeException(nameof(restoredCurrentHealth));

            GoblinInstance instance = new GoblinInstance();
            instance.instanceId = restoredInstanceId.Trim();
            instance.definitionId = restoredDefinitionId.Trim();
            instance.level = Mathf.Max(1, restoredLevel);
            instance.currentHealth = restoredCurrentHealth;
            instance.mapId = restoredMapId.Trim();
            instance.savedPosition = restoredPosition;
            return instance;
        }

        /// <summary>씬에서 마지막으로 확인된 맵 ID와 위치를 데이터에 저장한다.</summary>
        internal void SetLocation(string newMapId, Vector2 position)
        {
            if (string.IsNullOrWhiteSpace(newMapId))
                throw new ArgumentException("MapId는 비어 있을 수 없습니다.", nameof(newMapId));

            mapId = newMapId.Trim();
            savedPosition = position;
            Changed?.Invoke(this);
        }

        internal float ApplyDamage(float damage)
        {
            if (!IsAlive || !IsValidPositiveNumber(damage))
                return 0f;

            float before = currentHealth;
            currentHealth = Mathf.Max(0f, currentHealth - damage);
            float appliedDamage = before - currentHealth;

            if (appliedDamage > 0f)
                Changed?.Invoke(this);

            return appliedDamage;
        }

        internal float ApplyHealing(float amount, float maxHealth)
        {
            if (!IsAlive || !IsValidPositiveNumber(amount))
                return 0f;

            maxHealth = IsValidPositiveNumber(maxHealth) ? maxHealth : 1f;
            float before = currentHealth;
            currentHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
            float healedAmount = currentHealth - before;

            if (healedAmount > 0f)
                Changed?.Invoke(this);

            return healedAmount;
        }

        private static bool IsValidPositiveNumber(float value)
        {
            return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
