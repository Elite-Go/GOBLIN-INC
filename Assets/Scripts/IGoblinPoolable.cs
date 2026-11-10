namespace GoblinGame.Goblins
{
    /// <summary>
    /// GoblinActor 프리팹의 컴포넌트가 풀 재사용 시 상태를 초기화할 수 있도록 제공하는 계약.
    /// 이동/애니메이션/전투 컴포넌트가 추가되면 이 인터페이스를 구현하는 것을 권장한다.
    /// </summary>
    public interface IGoblinPoolable
    {
        /// <summary>데이터가 Bind된 후 GameObject가 활성화되기 직전에 호출된다.</summary>
        void OnRentFromPool();

        /// <summary>GameObject가 비활성화되기 직전에 호출된다.</summary>
        void OnReturnToPool();
    }
}
