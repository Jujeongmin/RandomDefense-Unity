using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace DefenseBattle.Server.Ugs
{
    /// <summary>
    /// IServerStore → UGS Cloud Save. 플레이어 값은 Protected(기기가 읽기만 가능, 서버만 씀),
    /// 공용 값은 Private Custom 데이터(기기는 못 봄) "defensebattle".
    /// </summary>
    public sealed class UgsStore : IServerStore
    {
        public const string CustomId = "defensebattle";

        readonly IGameApiClient m_api;
        readonly IExecutionContext m_ctx;

        public UgsStore(IGameApiClient api, IExecutionContext ctx)
        {
            m_api = api;
            m_ctx = ctx;
        }

        static StoredItem ToStored(List<Item> results, string key)
        {
            Item item = results?.FirstOrDefault(i => i.Key == key);
            if (item?.Value == null) return null;
            string json = item.Value is string s ? s : JsonConvert.SerializeObject(item.Value);
            return new StoredItem { Json = json, WriteLock = item.WriteLock };
        }

        public async Task<StoredItem> GetPlayer(string playerId, string key)
        {
            ApiResponse<GetItemsResponse> res = await m_api.CloudSaveData.GetProtectedItemsAsync(m_ctx, m_ctx.ServiceToken, m_ctx.ProjectId, playerId, new List<string> { key });
            return ToStored(res.Data?.Results, key);
        }

        public Task<string> SetPlayer(string playerId, string key, string json, string writeLock)
            => Write(key, () => m_api.CloudSaveData.SetProtectedItemAsync(m_ctx, m_ctx.ServiceToken, m_ctx.ProjectId, playerId, new SetItemBody(key, JToken.Parse(json), writeLock)));

        public async Task<StoredItem> GetShared(string key)
        {
            ApiResponse<GetItemsResponse> res = await m_api.CloudSaveData.GetPrivateCustomItemsAsync(m_ctx, m_ctx.ServiceToken, m_ctx.ProjectId, CustomId, new List<string> { key });
            return ToStored(res.Data?.Results, key);
        }

        public Task<string> SetShared(string key, string json, string writeLock)
            => Write(key, () => m_api.CloudSaveData.SetPrivateCustomItemAsync(m_ctx, m_ctx.ServiceToken, m_ctx.ProjectId, CustomId, new SetItemBody(key, JToken.Parse(json), writeLock)));

        static async Task<string> Write(string key, System.Func<Task<ApiResponse<SetItemResponse>>> call)
        {
            try
            {
                ApiResponse<SetItemResponse> res = await call();
                return res.Data?.WriteLock;
            }
            catch (ApiException e) when (e.Response != null && e.Response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new WriteConflictException(key); // 다른 호출이 먼저 씀 → 다시 읽고 하기
            }
        }
    }
}
