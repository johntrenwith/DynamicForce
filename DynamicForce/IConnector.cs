using Salesforce.Force;
using System.Threading.Tasks;

namespace DynamicForce
{
    public interface IConnector
    {
        Task<ForceClient> Authenticate();
        Task<string> DescribeObject(string url);
        string ApiVersion { get; }
    }
}

