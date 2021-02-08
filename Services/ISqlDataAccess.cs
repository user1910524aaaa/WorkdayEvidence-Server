using System.Collections.Generic;
using System.Threading.Tasks;

namespace Server.Services
{
    public interface ISqlDataAccess
    {
        string ConnectionStringName { get; set; }

        Task DeleteAsync<T>(string sql, T parameters);
        Task InsertAsync<T>(string sql, T parameters);
        Task<List<T>> SelectAsync<T, U>(string sql, U parameters);
        Task UpdateAsync<T>(string sql, T parameters);
    }
}