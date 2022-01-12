using Access;
using Data.Common.Contracts;

namespace Infrastructure.Data.Access
{
    public class UserAccessByEmailQuery : IAsyncQuery<UserAccess?, string>
    {
        private readonly string _connection;

        public UserAccessByEmailQuery(string connection)
        {
            _connection = connection;
        }

        public async Task<UserAccess?> ExecuteAsync(string parameter, CancellationToken token)
        {
            return (await new UserAccessSqlQuery(_connection)
               .Filter(UserAccessSqlQuery.EmailFilter(parameter))
               .ExecuteAsync(token))
               .FirstOrDefault();
        }
    }
}
