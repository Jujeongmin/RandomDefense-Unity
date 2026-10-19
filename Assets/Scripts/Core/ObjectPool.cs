using System;
using System.Collections.Generic;

namespace DefenseBattle.Core
{
    /// <summary>
    /// 단순 오브젝트 풀 (스택). 전투 중 초당 수백 개씩 생기고 사라지는 몹·이펙트를 새로 만들지 않고 재사용해
    /// GC 할당을 없앤다 (모바일에서 GC 스파이크 = 프레임 끊김).
    /// Get() 으로 받은 객체는 호출하는 쪽이 모든 필드를 다시 채워야 한다 (풀은 초기화하지 않음).
    /// </summary>
    public sealed class ObjectPool<T> where T : class
    {
        readonly Stack<T> m_free;
        readonly Func<T> m_create;
        readonly int m_maxSize;

        /// <param name="create">비었을 때 새로 만드는 함수</param>
        /// <param name="prewarm">미리 만들어 둘 개수 (판 시작 직후 할당 몰림 방지)</param>
        /// <param name="maxSize">풀에 보관할 최대 개수 (넘치면 버려서 메모리를 무한히 잡지 않게)</param>
        public ObjectPool(Func<T> create, int prewarm = 0, int maxSize = 1024)
        {
            m_create = create;
            m_maxSize = maxSize;
            m_free = new Stack<T>(Math.Max(prewarm, 16));
            for (int i = 0; i < prewarm; i++) m_free.Push(create());
        }

        /// <summary>지금 풀에 쉬고 있는 개수</summary>
        public int FreeCount => m_free.Count;

        public T Get() => m_free.Count > 0 ? m_free.Pop() : m_create();

        public void Release(T item)
        {
            if (item != null && m_free.Count < m_maxSize) m_free.Push(item);
        }
    }
}
