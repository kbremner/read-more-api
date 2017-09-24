using System;
using System.Threading.Tasks;

namespace PocketLib
{
    public interface IHttpRequestHandler : IDisposable
    {
        Task<T> PostAsync<T>(string path, object reqParams);
    }
}
