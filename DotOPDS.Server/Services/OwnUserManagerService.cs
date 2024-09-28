using System.Threading.Tasks;

namespace DotOPDS.Server.Services
{
    public interface IOwnUserManagerService
    {
        Task<bool> DoLogin(string username, string password);
    }

    public class OwnUserManagerService: IOwnUserManagerService
    {
        public async Task<bool> DoLogin(string username, string password)
        {
            // TODO: REPLACE
            return await Task.FromResult(username == password);
        }
    }
}
