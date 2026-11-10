using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoblinGame.Goblins
{
    /// <summary>
    /// GoblinRoster(영구 소유 데이터)와 GoblinActorPool(화면 오브젝트 풀)을 연결한다.
    /// CreateAndSpawn: 새 데이터 생성 + Actor 대여
    /// Despawn: Actor만 풀에 반환하고 소유 데이터는 유지
    /// Remove: Actor 반환 + 소유 데이터 삭제
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GoblinSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GoblinRoster roster;
        [SerializeField] private GoblinActorPool actorPool;
        [SerializeField] private Transform activeActorRoot;

        [Header("Defaults")]
        [SerializeField] private string defaultMapId = "home";

        private readonly Dictionary<string, GoblinActor> activeActorsById =
            new Dictionary<string, GoblinActor>(StringComparer.Ordinal);

        private readonly List<string> idBuffer = new List<string>();

        public event Action<GoblinActor> ActorSpawned;
        public event Action<GoblinInstance> ActorDespawned;

        public int ActiveActorCount => activeActorsById.Count;

        private void OnEnable()
        {
            if (roster != null)
                roster.GoblinRemoved += HandleGoblinRemoved;
        }

        private void OnDisable()
        {
            if (roster != null)
                roster.GoblinRemoved -= HandleGoblinRemoved;
        }

        private void OnDestroy()
        {
            DespawnAll();
        }

        /// <summary>
        /// 새 고블린 소유 데이터를 만들고 지정한 위치에 GameObject를 소환한다.
        /// 소환에 실패하면 방금 추가한 소유 데이터도 롤백한다.
        /// </summary>
        public bool TryCreateAndSpawn(
            string definitionId,
            Vector3 position,
            out GoblinActor actor,
            out string error)
        {
            return TryCreateAndSpawn(definitionId, defaultMapId, position, out actor, out error);
        }

        public bool TryCreateAndSpawn(
            string definitionId,
            string mapId,
            Vector3 position,
            out GoblinActor actor,
            out string error)
        {
            actor = null;
            error = null;

            if (!ValidateReferences(out error))
                return false;

            if (string.IsNullOrWhiteSpace(mapId))
            {
                error = "MapId는 비어 있을 수 없습니다.";
                return false;
            }

            if (!roster.TryAddGoblin(definitionId, out GoblinInstance instance, out error))
                return false;

            instance.SetLocation(mapId, new Vector2(position.x, position.y));

            if (TrySpawnInstance(instance, position, out actor, out error))
                return true;

            // 데이터는 추가됐지만 화면 객체 소환에 실패한 경우 생성 직전 상태로 되돌린다.
            roster.TryRemoveGoblin(instance.InstanceId, out _);
            return false;
        }

        /// <summary>
        /// 이미 소유 중인 고블린을 지정한 위치에 다시 소환한다.
        /// 기존 InstanceId, 체력, 레벨 등의 데이터는 새로 만들지 않는다.
        /// </summary>
        public bool TrySpawnExisting(
            string instanceId,
            Vector3 position,
            out GoblinActor actor,
            out string error)
        {
            actor = null;
            error = null;

            if (!ValidateReferences(out error))
                return false;

            if (!roster.TryGetGoblin(instanceId, out GoblinInstance instance))
            {
                error = $"소유 목록에서 고블린을 찾을 수 없습니다: {instanceId}";
                return false;
            }

            if (!instance.IsAlive)
            {
                error = "사망한 고블린은 소환할 수 없습니다.";
                return false;
            }

            instance.SetLocation(instance.MapId, new Vector2(position.x, position.y));
            return TrySpawnInstance(instance, position, out actor, out error);
        }

        /// <summary>
        /// 저장된 MapId와 마지막 위치로 고블린을 소환한다.
        /// 현재 맵 ID가 다르면 소환하지 않는다.
        /// </summary>
        public bool TrySpawnSaved(
            string instanceId,
            string currentMapId,
            out GoblinActor actor,
            out string error)
        {
            actor = null;
            error = null;

            if (!ValidateReferences(out error))
                return false;

            if (string.IsNullOrWhiteSpace(currentMapId))
            {
                error = "현재 MapId가 비어 있습니다.";
                return false;
            }

            if (!roster.TryGetGoblin(instanceId, out GoblinInstance instance))
            {
                error = $"소유 목록에서 고블린을 찾을 수 없습니다: {instanceId}";
                return false;
            }

            if (!string.Equals(instance.MapId, currentMapId, StringComparison.Ordinal))
            {
                error = $"고블린의 저장 맵({instance.MapId})과 현재 맵({currentMapId})이 다릅니다.";
                return false;
            }

            Vector2 saved = instance.SavedPosition;
            return TrySpawnExisting(instanceId, new Vector3(saved.x, saved.y, 0f), out actor, out error);
        }

        /// <summary>
        /// 씬에 보이는 Actor만 풀에 반환한다. GoblinRoster의 소유 데이터는 유지한다.
        /// </summary>
        public bool TryDespawn(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId) ||
                !activeActorsById.TryGetValue(instanceId, out GoblinActor actor) || actor == null)
            {
                return false;
            }

            GoblinInstance instance = actor.Instance;
            if (instance != null)
            {
                Vector3 position = actor.transform.position;
                instance.SetLocation(instance.MapId, new Vector2(position.x, position.y));
            }

            if (actorPool == null || !actorPool.TryRelease(actor))
            {
                Debug.LogError($"GoblinActor를 풀에 반환하지 못했습니다. InstanceId: {instanceId}", this);
                return false;
            }

            activeActorsById.Remove(instanceId);

            if (instance != null)
                ActorDespawned?.Invoke(instance);

            return true;
        }

        /// <summary>고블린의 화면 객체를 반환한 뒤 소유 데이터까지 제거한다.</summary>
        public bool TryRemoveGoblin(string instanceId, out GoblinInstance removed)
        {
            removed = null;

            if (!ValidateReferences(out string error))
            {
                Debug.LogError(error, this);
                return false;
            }

            // 활성 Actor 반환에 실패하면 소유 데이터를 삭제하지 않아 참조가 끊기는 것을 방지한다.
            if (activeActorsById.ContainsKey(instanceId) && !TryDespawn(instanceId))
                return false;

            return roster.TryRemoveGoblin(instanceId, out removed);
        }

        /// <summary>
        /// 활성 Actor의 현재 위치를 소유 데이터에 복사한다.
        /// 저장 시스템을 붙일 때 파일 저장 직전에 호출하면 된다.
        /// </summary>
        public void CaptureActivePositions()
        {
            foreach (KeyValuePair<string, GoblinActor> pair in activeActorsById)
            {
                GoblinActor actor = pair.Value;
                if (actor == null || actor.Instance == null)
                    continue;

                Vector3 position = actor.transform.position;
                actor.Instance.SetLocation(
                    actor.Instance.MapId,
                    new Vector2(position.x, position.y));
            }
        }

        /// <summary>현재 활성화된 모든 Actor를 풀에 반환한다. 소유 데이터는 삭제하지 않는다.</summary>
        public void DespawnAll()
        {
            idBuffer.Clear();
            foreach (KeyValuePair<string, GoblinActor> pair in activeActorsById)
                idBuffer.Add(pair.Key);

            for (int i = 0; i < idBuffer.Count; i++)
                TryDespawn(idBuffer[i]);

            idBuffer.Clear();
        }

        public bool TryGetActor(string instanceId, out GoblinActor actor)
        {
            actor = null;
            if (string.IsNullOrWhiteSpace(instanceId))
                return false;

            return activeActorsById.TryGetValue(instanceId, out actor) && actor != null;
        }

        private bool TrySpawnInstance(
            GoblinInstance instance,
            Vector3 position,
            out GoblinActor actor,
            out string error)
        {
            actor = null;
            error = null;

            if (activeActorsById.ContainsKey(instance.InstanceId))
            {
                error = $"이미 씬에 소환된 고블린입니다: {instance.InstanceId}";
                return false;
            }

            if (!roster.TryGetDefinition(instance, out GoblinDefinition definition) || definition == null)
            {
                error = $"GoblinDefinition을 찾을 수 없습니다: {instance.DefinitionId}";
                return false;
            }

            if (definition.ActorPrefab == null)
            {
                error = $"{definition.DisplayName}의 ActorPrefab이 지정되지 않았습니다.";
                return false;
            }

            if (!actorPool.TryRent(
                definition.ActorPrefab,
                position,
                Quaternion.identity,
                activeActorRoot,
                out GoblinActor rentedActor,
                out error))
            {
                return false;
            }

            if (!rentedActor.Bind(instance, definition, out error))
            {
                actorPool.TryRelease(rentedActor);
                return false;
            }

            // OnEnable에서 자신을 조회하는 컴포넌트도 정상 동작하도록 활성화 전에 등록한다.
            activeActorsById.Add(instance.InstanceId, rentedActor);

            if (!actorPool.TryActivate(rentedActor, out error))
            {
                activeActorsById.Remove(instance.InstanceId);
                actorPool.TryRelease(rentedActor);
                return false;
            }

            actor = rentedActor;
            ActorSpawned?.Invoke(actor);
            return true;
        }

        private bool ValidateReferences(out string error)
        {
            if (roster == null)
            {
                error = "GoblinSpawner에 GoblinRoster가 연결되지 않았습니다.";
                return false;
            }

            if (actorPool == null)
            {
                error = "GoblinSpawner에 GoblinActorPool이 연결되지 않았습니다.";
                return false;
            }

            if (roster.Database == null)
            {
                error = "GoblinRoster에 GoblinDatabase가 연결되지 않았습니다.";
                return false;
            }

            error = null;
            return true;
        }

        private void HandleGoblinRemoved(GoblinInstance removed)
        {
            if (removed == null)
                return;

            // 다른 시스템이 Roster에서 직접 제거해도 연결된 Actor가 풀에 남지 않도록 정리한다.
            TryDespawn(removed.InstanceId);
        }
    }
}
