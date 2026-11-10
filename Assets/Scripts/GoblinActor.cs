using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoblinGame.Goblins
{
    /// <summary>
    /// 씬에 보이는 고블린 GameObject.
    /// 영구 데이터는 GoblinInstance에 있고, Actor는 풀에서 재사용되는 표현 객체다.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class GoblinActor : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Sprite prefabDefaultSprite;
        private Vector3 prefabLocalScale;
        private List<IGoblinPoolable> poolableComponents;
        private int preparedPoolableCount;
        private bool componentsCached;

        public GoblinInstance Instance { get; private set; }
        public GoblinDefinition Definition { get; private set; }
        public bool IsBound => Instance != null && Definition != null;

        public Vector3 WorldPosition => transform.position;

        private void Awake()
        {
            CacheComponents();
        }

        /// <summary>풀에서 꺼낸 Actor에 개별 데이터와 종류 데이터를 연결한다.</summary>
        public bool Bind(GoblinInstance instance, GoblinDefinition definition, out string error)
        {
            error = null;
            CacheComponents();

            if (instance == null)
            {
                error = "GoblinInstance가 null입니다.";
                return false;
            }

            if (definition == null)
            {
                error = "GoblinDefinition이 null입니다.";
                return false;
            }

            if (instance.DefinitionId != definition.Id)
            {
                error = $"개체 종류와 정의 에셋이 일치하지 않습니다. Instance: {instance.DefinitionId}, Definition: {definition.Id}";
                return false;
            }

            if (spriteRenderer == null)
            {
                error = "SpriteRenderer를 찾을 수 없습니다.";
                return false;
            }

            if (IsBound)
            {
                error = "이미 다른 GoblinInstance가 연결된 Actor입니다. 먼저 풀에 반환해야 합니다.";
                return false;
            }

            Instance = instance;
            Definition = definition;
            spriteRenderer.sprite = definition.WorldSprite != null
                ? definition.WorldSprite
                : prefabDefaultSprite;

            gameObject.name = $"Goblin_{definition.Id}_{instance.InstanceId}";
            return true;
        }

        /// <summary>
        /// 풀에서 꺼내 활성화하기 직전에 부속 컴포넌트의 상태를 초기화한다.
        /// GoblinActorPool만 호출한다.
        /// </summary>
        internal bool PrepareForRent(out string error)
        {
            error = null;

            if (!IsBound)
            {
                error = "Bind되지 않은 GoblinActor는 활성화할 수 없습니다.";
                return false;
            }

            CacheComponents();
            preparedPoolableCount = 0;

            for (int i = 0; i < poolableComponents.Count; i++)
            {
                try
                {
                    poolableComponents[i].OnRentFromPool();
                    preparedPoolableCount++;
                }
                catch (Exception exception)
                {
                    error = $"{poolableComponents[i].GetType().Name}.OnRentFromPool 실행 실패: {exception.Message}";
                    ResetPreparedPoolables();
                    return false;
                }
            }

            return true;
        }

        /// <summary>풀에 반환되기 전 부속 컴포넌트를 정리하고 데이터 연결을 해제한다.</summary>
        internal void ResetForPool()
        {
            ResetPreparedPoolables();
            Unbind();
        }

        private void ResetPreparedPoolables()
        {
            for (int i = preparedPoolableCount - 1; i >= 0; i--)
            {
                try
                {
                    poolableComponents[i].OnReturnToPool();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }

            preparedPoolableCount = 0;
        }

        private void Unbind()
        {
            Instance = null;
            Definition = null;

            if (spriteRenderer != null)
                spriteRenderer.sprite = prefabDefaultSprite;

            gameObject.name = "Pooled_GoblinActor";
        }

        private void CacheComponents()
        {
            if (componentsCached)
                return;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (spriteRenderer != null)
                prefabDefaultSprite = spriteRenderer.sprite;

            prefabLocalScale = transform.localScale;

            MonoBehaviour[] behaviours = GetComponentsInChildren<MonoBehaviour>(true);
            poolableComponents = new List<IGoblinPoolable>(behaviours.Length);

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null || behaviours[i] == this)
                    continue;

                if (behaviours[i] is IGoblinPoolable poolable)
                    poolableComponents.Add(poolable);
            }

            componentsCached = true;
        }

        internal Vector3 PrefabLocalScale
        {
            get
            {
                CacheComponents();
                return prefabLocalScale;
            }
        }
    }
}
