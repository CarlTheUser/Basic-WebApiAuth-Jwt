using Access;
using Data.Common.Contracts;
using Data.Sql;
using Data.Sql.Mapping;
using Data.Sql.Provider;
using System.Data;
using System.Data.Common;

namespace Infrastructure.Data.Access
{
    public class UserAccessRepository : IRepository<Guid, UserAccess?>, IQuery<UserAccess?, Guid>
    {
        private readonly ISqlProvider _provider;
        private readonly ISqlCaller _caller;

        public UserAccessRepository(string connection)
        {
            _caller = new SqlCaller(_provider = new SqlServerProvider(connection));
        }

        public UserAccess? Execute(Guid parameter)
        {
            return new UserAccessSqlQuery(_provider, _caller)
                .Filter(UserAccessSqlQuery.IdFilter(parameter))
                .Execute()
                .FirstOrDefault();
        }

        public UserAccess? Find(Guid key)
        {
            return new UserAccessSqlQuery(_provider, _caller)
               .Filter(UserAccessSqlQuery.IdFilter(key))
               .Execute()
               .FirstOrDefault();
        }

        public void Save(UserAccess? item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var events = item.ReleaseEvents();

            SqlTransaction transaction = _caller.CreateScopedTransaction(IsolationLevel.ReadCommitted);

            try
            {
                foreach(var @event in events)
                {
                    switch (@event)
                    {
                        case UserAccessCreatedDataEvent uacde:
                            WriteEvent(uacde, transaction);
                            break;
                        case PasswordChangedDataEvent pcde:
                            WriteEvent(pcde, transaction);
                            break;
                        case RoleChangedDataEvent rcde:
                            WriteEvent(rcde, transaction);
                            break;
                    }
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
            }
        }

        private void WriteEvent(UserAccessCreatedDataEvent @event, SqlTransaction transaction)
        {
            DbCommand command = _provider.CreateCommand(
                $"Insert Into UserAccesses(Id,Email,[Role],Salt,[Hash])Values('{@event.User}',@Email,'{@event.Role}',@Salt,@Hash) ",
                CommandType.Text,
                _provider.CreateInputParameters(
                    new
                    {
                        @event.Email,
                        @event.Salt,
                        @event.Hash
                    }, "@"));

            transaction.ExecuteNonQuery(command);
        }

        private void WriteEvent(PasswordChangedDataEvent @event, SqlTransaction transaction)
        {
            DbCommand command = _provider.CreateCommand(
                $"Update UserAccesses Set Salt=@Salt,[Hash]=@Hash Where Id='{@event.User}' ",
                CommandType.Text,
                _provider.CreateInputParameters(
                    new
                    {
                        @event.Salt,
                        @event.Hash
                    }, "@"));

            transaction.ExecuteNonQuery(command);
        }

        private void WriteEvent(RoleChangedDataEvent @event, SqlTransaction transaction)
        {
            transaction.ExecuteNonQuery($"Update UserAccesses Set [Role]='{@event.Role}' Where Id='{@event.User}' ");
        }

        private class DataHolder
        {
            public Guid Id { get; set; }
            public string Email { get; set; } = string.Empty;
            public Guid RoleId { get; set; }
            public string RoleDescription { get; set; } = string.Empty;
            public byte[] Salt { get; set; } = Array.Empty<byte>();
            public byte[] Hash { get; set; } = Array.Empty<byte>();
        }
    }
}
