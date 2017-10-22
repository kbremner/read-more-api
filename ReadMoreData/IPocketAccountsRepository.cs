using ReadMoreData.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReadMoreData
{
    public interface IPocketAccountsRepository
    {
        Task<PocketAccount> FindByIdAsync(Guid id);
        Task<PocketAccount> FindByUsernameAsync(string username);
        Task<PocketAccount> InsertAsync(PocketAccount account);
        Task UpdateAsync(PocketAccount account);
        Task DeleteAsync(PocketAccount account);
        Task<IEnumerable<FeatureToggle>> FindTogglesForAccountAsync(Guid id);
    }
}
