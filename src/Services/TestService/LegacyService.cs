using System;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using Microsoft.TestService.Services;

namespace Microsoft.TestService.Services
{
    /// <summary>
    /// WCF Service for legacy operations
    /// </summary>
    [ServiceContract]
    public interface ILegacyService
    {
        [OperationContract]
        [WebGet]
        string GetLegacyData(string id);
    }

    public class LegacyService : ILegacyService
    {
        public string GetLegacyData(string id)
        {
            return $"Legacy data for ID: {id}";
        }
    }
}
