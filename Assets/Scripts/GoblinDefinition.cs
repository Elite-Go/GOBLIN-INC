using UnityEngine;

namespace GoblinGame.Goblins
{
    /// <summary>
    /// 고블린 '종류'의 고정 데이터.
    /// 일반 고블린, 발빠른 고블린, 전사 고블린마다 별도의 에셋을 만든다.
    /// 실제 개체의 현재 체력 등 변하는 값은 GoblinInstance에 저장한다.
    /// </summary>
    [CreateAssetMenu(fileName = "GoblinDefinition", menuName = "Goblin Game/Goblins/Goblin Definition")]
    public sealed class GoblinDefinition : ScriptableObject
    {
        [Header("식별 정보")]
        [SerializeField] private string id = "goblin.common";
        [SerializeField] private string displayName = "일반 고블린";
        [TextArea(2, 5)]
        [SerializeField] private string description;

        [Header("이미지 및 프리팹")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite worldSprite;
        [Tooltip("실제 맵에 배치할 프리팹. 프리팹 루트에 GoblinActor와 SpriteRenderer가 필요하다.")]
        [SerializeField] private GoblinActor actorPrefab;

        [Header("기본 능력치")]
        [SerializeField] private GoblinBaseStats baseStats = new GoblinBaseStats
        {
            maxHealth = 50f,
            moveSpeed = 2.5f,
            attackPower = 5f,
            defense = 1f,
            workSpeed = 1f,
            carryCapacity = 10
        };

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public Sprite WorldSprite => worldSprite;
        public GoblinActor ActorPrefab => actorPrefab;
        public GoblinBaseStats BaseStats => baseStats.Validated();

        private void OnValidate()
        {
            id = id == null ? string.Empty : id.Trim();
            displayName = displayName == null ? string.Empty : displayName.Trim();
            baseStats = baseStats.Validated();
        }

        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                error = $"{name}: 고블린 ID가 비어 있습니다.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                error = $"{name}: 표시 이름이 비어 있습니다.";
                return false;
            }

            error = null;
            return true;
        }
    }
}
