using Access;
using Data.Common.Contracts;

namespace Infrastructure.Data.Access
{
    public class UserAccessByEmailQuery : IQuery<UserAccess?, string>
    {
        private readonly string _connection;

        public UserAccessByEmailQuery(string connection)
        {
            _connection = connection;
        }

        public UserAccess? Execute(string parameter)
        {
            return new UserAccessSqlQuery(_connection)
               .Filter(UserAccessSqlQuery.EmailFilter(parameter))
               .Execute()
               .FirstOrDefault();
        }
    }
}
