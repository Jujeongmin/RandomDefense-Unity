using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace DefenseBattle.Server.Ugs
{
    /// <summary>Cloud Code 모듈 의존성 등록</summary>
    public sealed class ModuleConfig : ICloudCodeSetup
    {
        public void Setup(ICloudCodeConfig config)
        {
            config.AddGameApiClient();
        }
    }
}
