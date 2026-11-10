using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoblinGame.Goblins
{
    /// <summary>
    /// 플레이어가 소유한 고블린 목록을 관리한다.
    /// 씬에 배치된 GoblinActor와 별개이므로 맵/씬이 바뀌어도 소유 데이터는 유지할 수 있다.
    /// </summary>
    public sealed class GoblinRoster : MonoBehaviour
    {
        [Header("데이터베이스")]
        [SerializeField] private GoblinDatabase database;

        [Header("현재 소유 고블린")]
        [SerializeField] private List<GoblinInstance> ownedGoblins = new List<GoblinInstance>();

        private Dictionary<string, GoblinInstance> goblinsByInstanceId;
        private bool indexInitialized;

        public event Action<GoblinInstance> GoblinAdded;
        public event Action<GoblinInstance> GoblinRemoved;
        public event Action<GoblinInstance> GoblinChanged;

        public int Count => ownedGoblins == null ? 0 : ownedGoblins.Count;
        public IReadOnlyList<GoblinInstance> OwnedGoblins => ownedGoblins;
        public GoblinDatabase Database => database;

        private void Awake()
        {
            EnsureIndex();
        }

        private void OnDestroy()
        {
            if (ownedGoblins == null)
                return;

            for (int i = 0; i < ownedGoblins.Count; i++)
            {
                if (ownedGoblins[i] != null)
                    ownedGoblins[i].Changed -= HandleGoblinChanged;
            }
        }

        /// <summary>고블린 종류 ID를 사용해 새 개체를 소유 목록에 추가한다.</summary>
        public bool TryAddGoblin(string definitionId, out GoblinInstance created, out string error)
        {
            created = null;
            error = null;

            if (database == null)
            {
                error = "GoblinRoster에 GoblinDatabase가 연결되지 않았습니다.";
                return false;
            }

            if (!database.TryGetDefinition(definitionId, out GoblinDefinition definition))
            {
                error = $"등록되지 않은 고블린 종류입니다: {definitionId}";
                return false;
            }

            return TryAddGoblin(definition, out created, out error);
        }

        /// <summary>고블린 정의 에셋을 사용해 새 개체를 소유 목록에 추가한다.</summary>
        public bool TryAddGoblin(GoblinDefinition definition, out GoblinInstance created, out string error)
        {
            created = null;
            error = null;
            EnsureIndex();

            if (definition == null)
            {
                error = "추가할 GoblinDefinition이 null입니다.";
                return false;
            }

            if (database == null || !database.TryGetDefinition(definition.Id, out GoblinDefinition registeredDefinition))
            {
                error = $"데이터베이스에 등록되지 않은 고블린 종류입니다: {definition.Id}";
                return false;
            }

            // ID만 같은 다른 에셋이 잘못 전달되는 상황도 막는다.
            if (registeredDefinition != definition)
            {
                error = $"고블린 ID '{definition.Id}'가 데이터베이스의 다른 에셋과 중복됩니다.";
                return false;
            }

            created = GoblinInstance.Create(definition);

            if (goblinsByInstanceId.ContainsKey(created.InstanceId))
            {
                created = null;
                error = "고블린 인스턴스 ID 중복이 발생했습니다.";
                return false;
            }

            ownedGoblins.Add(created);
            goblinsByInstanceId.Add(created.InstanceId, created);
            created.Changed += HandleGoblinChanged;

            GoblinAdded?.Invoke(created);
            return true;
        }

        /// <summary>고유 InstanceId를 기준으로 소유 고블린 한 마리를 제거한다.</summary>
        public bool TryRemoveGoblin(string instanceId, out GoblinInstance removed)
        {
            removed = null;
            EnsureIndex();

            if (string.IsNullOrWhiteSpace(instanceId) ||
                !goblinsByInstanceId.TryGetValue(instanceId, out GoblinInstance target))
            {
                return false;
            }

            goblinsByInstanceId.Remove(instanceId);
            ownedGoblins.Remove(target);
            target.Changed -= HandleGoblinChanged;

            removed = target;
            GoblinRemoved?.Invoke(removed);
            return true;
        }

        /// <summary>
        /// 세이브 파일에서 복원한 전체 목록으로 현재 소유 목록을 교체한다.
        /// 데이터 전체를 먼저 검증하므로 잘못된 항목이 있으면 기존 목록을 변경하지 않는다.
        /// </summary>
        public bool TryReplaceAll(IList<GoblinInstance> restoredGoblins, out string error)
        {
            error = null;
            EnsureIndex();

            if (restoredGoblins == null)
            {
                error = "복원할 고블린 목록이 null입니다.";
                return false;
            }

            Dictionary<string, GoblinInstance> validatedIndex =
                new Dictionary<string, GoblinInstance>(StringComparer.Ordinal);

            for (int i = 0; i < restoredGoblins.Count; i++)
            {
                GoblinInstance goblin = restoredGoblins[i];
                if (goblin == null || string.IsNullOrWhiteSpace(goblin.InstanceId))
                {
                    error = $"세이브 데이터의 {i}번째 고블린 데이터가 유효하지 않습니다.";
                    return false;
                }

                if (validatedIndex.ContainsKey(goblin.InstanceId))
                {
                    error = $"중복된 고블린 InstanceId가 있습니다: {goblin.InstanceId}";
                    return false;
                }

                if (database == null || !database.TryGetDefinition(goblin.DefinitionId, out _))
                {
                    error = $"GoblinDefinition을 찾을 수 없습니다: {goblin.DefinitionId}";
                    return false;
                }

                validatedIndex.Add(goblin.InstanceId, goblin);
            }

            // 기존 이벤트 구독 및 외부 참조를 정리한다.
            for (int i = 0; i < ownedGoblins.Count; i++)
            {
                GoblinInstance oldGoblin = ownedGoblins[i];
                if (oldGoblin == null)
                    continue;

                oldGoblin.Changed -= HandleGoblinChanged;
                GoblinRemoved?.Invoke(oldGoblin);
            }

            ownedGoblins.Clear();
            goblinsByInstanceId.Clear();

            for (int i = 0; i < restoredGoblins.Count; i++)
            {
                GoblinInstance goblin = restoredGoblins[i];
                ownedGoblins.Add(goblin);
                goblinsByInstanceId.Add(goblin.InstanceId, goblin);
                goblin.Changed -= HandleGoblinChanged;
                goblin.Changed += HandleGoblinChanged;
                GoblinAdded?.Invoke(goblin);
            }

            return true;
        }

        /// <summary>현재 소유 고블린 데이터를 전부 제거한다. 세이브 파일은 삭제하지 않는다.</summary>
        public void ClearAll()
        {
            EnsureIndex();

            for (int i = ownedGoblins.Count - 1; i >= 0; i--)
            {
                GoblinInstance goblin = ownedGoblins[i];
                if (goblin == null)
                    continue;

                goblin.Changed -= HandleGoblinChanged;
                GoblinRemoved?.Invoke(goblin);
            }

            ownedGoblins.Clear();
            goblinsByInstanceId.Clear();
        }

        public bool TryGetGoblin(string instanceId, out GoblinInstance goblin)
        {
            EnsureIndex();
            goblin = null;

            if (string.IsNullOrWhiteSpace(instanceId))
                return false;

            return goblinsByInstanceId.TryGetValue(instanceId, out goblin);
        }

        public bool TryGetDefinition(GoblinInstance goblin, out GoblinDefinition definition)
        {
            definition = null;

            if (goblin == null || database == null)
                return false;

            return database.TryGetDefinition(goblin.DefinitionId, out definition);
        }

        /// <summary>고블린의 마지막으로 확인된 맵 ID와 좌표를 갱신한다.</summary>
        public bool TrySetLocation(string instanceId, string mapId, Vector2 position)
        {
            if (!TryGetGoblin(instanceId, out GoblinInstance goblin))
                return false;

            if (string.IsNullOrWhiteSpace(mapId))
                return false;

            goblin.SetLocation(mapId, position);
            return true;
        }

        /// <summary>고블린에게 피해를 적용한다. 체력이 0이 되어도 자동으로 목록에서 제거하지 않는다.</summary>
        public bool TryApplyDamage(string instanceId, float damage, out float appliedDamage)
        {
            appliedDamage = 0f;

            if (!TryGetGoblin(instanceId, out GoblinInstance goblin))
                return false;

            appliedDamage = goblin.ApplyDamage(damage);
            return appliedDamage > 0f;
        }

        /// <summary>고블린을 회복한다. 최대 체력은 종류의 기본 능력치를 기준으로 한다.</summary>
        public bool TryHeal(string instanceId, float amount, out float healedAmount)
        {
            healedAmount = 0f;

            if (!TryGetGoblin(instanceId, out GoblinInstance goblin))
                return false;

            if (!TryGetDefinition(goblin, out GoblinDefinition definition))
                return false;

            healedAmount = goblin.ApplyHealing(amount, definition.BaseStats.MaxHealth);
            return healedAmount > 0f;
        }

        private void EnsureIndex()
        {
            if (indexInitialized)
                return;

            if (ownedGoblins == null)
                ownedGoblins = new List<GoblinInstance>();

            if (goblinsByInstanceId == null)
                goblinsByInstanceId = new Dictionary<string, GoblinInstance>(StringComparer.Ordinal);
            else
                goblinsByInstanceId.Clear();

            for (int i = 0; i < ownedGoblins.Count; i++)
            {
                GoblinInstance goblin = ownedGoblins[i];

                if (goblin == null || string.IsNullOrWhiteSpace(goblin.InstanceId))
                {
                    Debug.LogError($"[{name}] 유효하지 않은 고블린 데이터가 소유 목록에 있습니다. 목록 인덱스: {i}", this);
                    continue;
                }

                if (goblinsByInstanceId.ContainsKey(goblin.InstanceId))
                {
                    Debug.LogError($"[{name}] 중복된 고블린 InstanceId가 있습니다: {goblin.InstanceId}", this);
                    continue;
                }

                goblinsByInstanceId.Add(goblin.InstanceId, goblin);
                goblin.Changed -= HandleGoblinChanged;
                goblin.Changed += HandleGoblinChanged;

                if (database == null || !database.TryGetDefinition(goblin.DefinitionId, out _))
                {
                    Debug.LogError($"[{name}] 종류 데이터를 찾을 수 없는 고블린입니다. DefinitionId: {goblin.DefinitionId}", this);
                }
            }

            indexInitialized = true;
        }

        private void HandleGoblinChanged(GoblinInstance goblin)
        {
            GoblinChanged?.Invoke(goblin);
        }
    }
}
