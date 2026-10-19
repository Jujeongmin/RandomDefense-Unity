using Newtonsoft.Json;

namespace DefenseBattle.Server
{
    /// <summary>저장·응답 JSON 형식 (클라이언트도 같은 Newtonsoft 기본값으로 읽는다)</summary>
    public static class Json
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            ObjectCreationHandling = ObjectCreationHandling.Replace, // 기본값이 든 리스트·사전을 덧붙이지 않고 바꾼다
        };

        public static string Write(object value) => JsonConvert.SerializeObject(value, Settings);

        public static T Read<T>(string json) where T : class
            => string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<T>(json, Settings);

        /// <summary>깊은 복사 (응답으로 내보낼 때 저장 객체와 분리)</summary>
        public static T Clone<T>(T value) where T : class => Read<T>(Write(value));
    }
}
