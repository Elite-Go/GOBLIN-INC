using System.Collections.Generic;
using UnityEngine;

namespace GoblinGame.Goblins
{
    /// <summary>
    /// GoblinActor 프리팹별 오브젝트 풀.
    /// UnityEngine.Pool에 의존하지 않는 간단한 Stack 기반 구현으로 Unity 버전 호환성을 높였다.
    /// Rent된 오브젝트는 Bind/위치 설정을 마친 뒤 TryActivate로 활성화해야 한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GoblinActorPool : MonoBehaviour
    {
        private sealed class PoolEntry
        {
            public GoblinActor Prefab;
            public readonly Stack<GoblinActor> Available = new Stack<GoblinActor>();
            public readonly HashSet<GoblinActor> AllActors = new HashSet<GoblinActor>();
        }

        private readonly Dictionary<int, PoolEntry> poolsByPrefabId = new Dictionary<int, PoolEntry>();
        private readonly Dictionary<GoblinActor, PoolEntry> leasedActors = new Dictionary<GoblinActor, PoolEntry>();

        private Transform inactiveRoot;

        public int LeasedCount => leasedActors.Count;

        private void Awake()
        {
            EnsureInactiveRoot();
        }

        /// <summary>
        /// 프리팹에 해당하는 Actor를 풀에서 대여한다.
        /// 반환된 Actor는 비활성 상태이며, 호출자가 Bind한 뒤 TryActivate를 호출해야 한다.
        /// </summary>
        public bool TryRent(
            GoblinActor prefab,
            Vector3 position,
            Quaternion rotation,
            Transform activeParent,
            out GoblinActor actor,
            out string error)
        {
            actor = null;
            error = null;

            if (prefab == null)
            {
                error = "GoblinDefinition에 ActorPrefab이 연결되지 않았습니다.";
                return false;
            }

            if (!prefab.gameObject.activeSelf)
            {
                error = $"{prefab.name} 프리팹의 루트 GameObject는 활성 상태여야 합니다. 풀에서 생성 직후 비활성화합니다.";
                return false;
            }

            EnsureInactiveRoot();
            PoolEntry entry = GetOrCreateEntry(prefab);
            actor = PopAvailable(entry);

            if (actor == null)
            {
                // 활성 매니저 아래에서 한 번 생성해 Awake를 완료한 뒤 즉시 비활성화한다.
                // 이후 대여 시에는 Bind와 풀 콜백을 마친 다음 다시 활성화한다.
                actor = Instantiate(prefab, transform, false);
                if (actor == null)
                {
                    error = $"{prefab.name} 프리팹 인스턴스 생성에 실패했습니다.";
                    return false;
                }

                actor.gameObject.SetActive(false);
                actor.transform.SetParent(inactiveRoot, false);
                entry.AllActors.Add(actor);
            }

            if (leasedActors.ContainsKey(actor))
            {
                error = "이미 대여 중인 GoblinActor가 풀에서 반환되었습니다.";
                actor = null;
                return false;
            }

            Transform parent = activeParent != null ? activeParent : transform;
            actor.transform.SetParent(parent, false);
            actor.transform.SetPositionAndRotation(position, rotation);
            actor.transform.localScale = actor.PrefabLocalScale;

            leasedActors.Add(actor, entry);
            return true;
        }

        /// <summary>Bind 완료된 Actor를 활성화한다.</summary>
        public bool TryActivate(GoblinActor actor, out string error)
        {
            error = null;

            if (actor == null || !leasedActors.ContainsKey(actor))
            {
                error = "현재 풀에서 대여한 GoblinActor가 아닙니다.";
                return false;
            }

            if (actor.gameObject.activeSelf)
            {
                error = "GoblinActor가 이미 활성화되어 있습니다.";
                return false;
            }

            if (!actor.PrepareForRent(out error))
                return false;

            actor.gameObject.SetActive(true);
            return true;
        }

        /// <summary>
        /// Actor를 풀에 반환한다. GoblinInstance는 제거하지 않는다.
        /// </summary>
        public bool TryRelease(GoblinActor actor)
        {
            if (actor == null || !leasedActors.TryGetValue(actor, out PoolEntry entry))
                return false;

            leasedActors.Remove(actor);

            actor.ResetForPool();
            actor.gameObject.SetActive(false);
            actor.transform.SetParent(inactiveRoot, false);
            actor.transform.localPosition = Vector3.zero;
            actor.transform.localRotation = Quaternion.identity;
            actor.transform.localScale = actor.PrefabLocalScale;

            entry.Available.Push(actor);
            return true;
        }

        /// <summary>지정한 프리팹의 비활성 인스턴스를 미리 생성한다.</summary>
        public bool Prewarm(GoblinActor prefab, int count, out string error)
        {
            error = null;

            if (prefab == null)
            {
                error = "Prewarm할 GoblinActor 프리팹이 null입니다.";
                return false;
            }

            if (count < 0)
            {
                error = "Prewarm 개수는 0 이상이어야 합니다.";
                return false;
            }

            EnsureInactiveRoot();
            PoolEntry entry = GetOrCreateEntry(prefab);

            for (int i = 0; i < count; i++)
            {
                GoblinActor created = Instantiate(prefab, transform, false);
                if (created == null)
                {
                    error = $"{prefab.name} 프리팹 사전 생성에 실패했습니다.";
                    return false;
                }

                created.gameObject.SetActive(false);
                created.transform.SetParent(inactiveRoot, false);
                entry.AllActors.Add(created);
                entry.Available.Push(created);
            }

            return true;
        }

        /// <summary>특정 프리팹 풀의 대기/대여 개수를 확인한다.</summary>
        public bool TryGetCounts(GoblinActor prefab, out int available, out int leased)
        {
            available = 0;
            leased = 0;

            if (prefab == null || !poolsByPrefabId.TryGetValue(prefab.GetInstanceID(), out PoolEntry entry))
                return false;

            available = entry.Available.Count;

            foreach (KeyValuePair<GoblinActor, PoolEntry> pair in leasedActors)
            {
                if (pair.Value == entry)
                    leased++;
            }

            return true;
        }

        private GoblinActor PopAvailable(PoolEntry entry)
        {
            while (entry.Available.Count > 0)
            {
                GoblinActor candidate = entry.Available.Pop();
                if (candidate != null)
                    return candidate;
            }

            return null;
        }

        private PoolEntry GetOrCreateEntry(GoblinActor prefab)
        {
            int prefabId = prefab.GetInstanceID();
            if (poolsByPrefabId.TryGetValue(prefabId, out PoolEntry entry))
                return entry;

            entry = new PoolEntry { Prefab = prefab };
            poolsByPrefabId.Add(prefabId, entry);
            return entry;
        }

        private void EnsureInactiveRoot()
        {
            if (inactiveRoot != null)
                return;

            GameObject root = new GameObject("GoblinActorPool_Inactive");
            root.transform.SetParent(transform, false);
            root.SetActive(false);
            inactiveRoot = root.transform;
        }

        private void OnDestroy()
        {
            // 풀 매니저가 파괴될 때 대기/대여 중인 풀 오브젝트도 함께 정리한다.
            foreach (PoolEntry entry in poolsByPrefabId.Values)
            {
                foreach (GoblinActor actor in entry.AllActors)
                {
                    if (actor != null)
                        Destroy(actor.gameObject);
                }
            }

            poolsByPrefabId.Clear();
            leasedActors.Clear();
        }
    }
}
