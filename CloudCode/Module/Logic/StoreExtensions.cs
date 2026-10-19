using System;
using System.Threading.Tasks;

namespace DefenseBattle.Server
{
    /// <summary>
    /// 읽고-바꾸고-쓰기를 잠금 충돌 시 다시 하는 도우미 (원본 $lock 자리).
    /// fn 은 다시 불릴 수 있으니 바깥 상태를 바꾸지 말고 받은 값만 바꾼다.
    /// </summary>
    public static class StoreExtensions
    {
        const int MaxTries = 6;

        public static Task<TResult> MutatePlayer<T, TResult>(this IServerStore store, string playerId, string key,
            Func<T> create, Func<T, TResult> fn) where T : class
            => Mutate(() => store.GetPlayer(playerId, key), (json, wl) => store.SetPlayer(playerId, key, json, wl), key, create, fn);

        public static Task<TResult> MutateShared<T, TResult>(this IServerStore store, string key,
            Func<T> create, Func<T, TResult> fn) where T : class
            => Mutate(() => store.GetShared(key), (json, wl) => store.SetShared(key, json, wl), key, create, fn);

        public static async Task<T> ReadPlayer<T>(this IServerStore store, string playerId, string key) where T : class
            => Json.Read<T>((await store.GetPlayer(playerId, key))?.Json);

        public static async Task<T> ReadShared<T>(this IServerStore store, string key) where T : class
            => Json.Read<T>((await store.GetShared(key))?.Json);

        /// <summary>fn 이 false 를 주는 '바꾸지 않음' 은 Skip 결과로 표현 - 쓰기를 건너뛴다</summary>
        static async Task<TResult> Mutate<T, TResult>(Func<Task<StoredItem>> get, Func<string, string, Task<string>> set, string key,
            Func<T> create, Func<T, TResult> fn) where T : class
        {
            for (int attempt = 1; ; attempt++)
            {
                StoredItem item = await get();
                T value = Json.Read<T>(item?.Json) ?? create();
                TResult result = fn(value);
                if (result is IMutation m && !m.Changed) return result;
                try
                {
                    await set(Json.Write(value), item?.WriteLock);
                    return result;
                }
                catch (WriteConflictException) when (attempt < MaxTries)
                {
                    await Task.Delay(20 * attempt);
                }
            }
        }
    }

    /// <summary>결과가 이걸 구현하면 Changed=false 일 때 저장하지 않는다</summary>
    public interface IMutation
    {
        bool Changed { get; }
    }
}
