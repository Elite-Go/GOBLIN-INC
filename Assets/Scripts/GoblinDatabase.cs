using System;
using System.Collections.Generic;
using UnityEngine;

namespace GoblinGame.Goblins
{
    /// <summary>
    /// 프로젝트에 등록된 모든 고블린 종류를 ID로 검색하는 데이터베이스.
    /// 새 고블린 종류를 추가할 때는 GoblinDefinition 에셋을 만들고 이 목록에 등록한다.
    /// </summary>
    [CreateAssetMenu(fileName = "GoblinDatabase", menuName = "Goblin Game/Goblins/Goblin Database")]
    public sealed class GoblinDatabase : ScriptableObject
    {
        [SerializeField] private List<GoblinDefinition> definitions = new List<GoblinDefinition>();

        [NonSerialized]
        private Dictionary<string, GoblinDefinition> definitionsById;

        public IReadOnlyList<GoblinDefinition> Definitions => definitions;

        private void OnEnable()
        {
            RebuildIndex(false);
        }

        private void OnValidate()
        {
            RebuildIndex(true);
        }

        public bool TryGetDefinition(string definitionId, out GoblinDefinition definition)
        {
            definition = null;

            if (string.IsNullOrWhiteSpace(definitionId))
                return false;

            if (definitionsById == null)
                RebuildIndex(false);

            return definitionsById.TryGetValue(definitionId.Trim(), out definition);
        }

        /// <summary>에셋 목록을 다시 검사하고 ID 검색용 Dictionary를 재생성한다.</summary>
        public void RebuildIndex(bool logErrors = true)
        {
            if (definitionsById == null)
                definitionsById = new Dictionary<string, GoblinDefinition>(StringComparer.Ordinal);
            else
                definitionsById.Clear();

            if (definitions == null)
                return;

            for (int i = 0; i < definitions.Count; i++)
            {
                GoblinDefinition definition = definitions[i];

                if (definition == null)
                {
                    if (logErrors)
                        Debug.LogError($"[{name}] 고블린 데이터베이스의 {i}번 항목이 비어 있습니다.", this);
                    continue;
                }

                if (!definition.Validate(out string validationError))
                {
                    if (logErrors)
                        Debug.LogError(validationError, definition);
                    continue;
                }

                if (definitionsById.ContainsKey(definition.Id))
                {
                    if (logErrors)
                        Debug.LogError($"[{name}] 중복된 고블린 ID가 있습니다: {definition.Id}", definition);
                    continue;
                }

                definitionsById.Add(definition.Id, definition);
            }
        }
    }
}
