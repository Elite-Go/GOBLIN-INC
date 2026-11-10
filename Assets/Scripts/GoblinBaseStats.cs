using System;
using UnityEngine;

namespace GoblinGame.Goblins
{
    /// <summary>
    /// 고블린 종류가 기본적으로 가지는 능력치 데이터.
    /// ScriptableObject의 종류 데이터와 런타임 개체가 공통으로 사용한다.
    /// </summary>
    [Serializable]
    public struct GoblinBaseStats
    {
        [Min(1f)] public float maxHealth;
        [Min(0f)] public float moveSpeed;
        [Min(0f)] public float attackPower;
        [Min(0f)] public float defense;
        [Min(0f)] public float workSpeed;
        [Min(0)] public int carryCapacity;

        public float MaxHealth => Mathf.Max(1f, SafeNonNegative(maxHealth));
        public float MoveSpeed => SafeNonNegative(moveSpeed);
        public float AttackPower => SafeNonNegative(attackPower);
        public float Defense => SafeNonNegative(defense);
        public float WorkSpeed => SafeNonNegative(workSpeed);
        public int CarryCapacity => Mathf.Max(0, carryCapacity);

        /// <summary>Inspector 또는 외부 데이터에서 잘못된 값이 들어오는 것을 방지한다.</summary>
        public GoblinBaseStats Validated()
        {
            GoblinBaseStats result = this;
            result.maxHealth = Mathf.Max(1f, SafeNonNegative(maxHealth));
            result.moveSpeed = SafeNonNegative(moveSpeed);
            result.attackPower = SafeNonNegative(attackPower);
            result.defense = SafeNonNegative(defense);
            result.workSpeed = SafeNonNegative(workSpeed);
            result.carryCapacity = Mathf.Max(0, carryCapacity);
            return result;
        }

        private static float SafeNonNegative(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return 0f;

            return Mathf.Max(0f, value);
        }
    }
}
