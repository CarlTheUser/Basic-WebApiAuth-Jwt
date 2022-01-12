using Access;
using Data.Common.Contracts;
using Data.Sql;
using Data.Sql.Mapping;
using Data.Sql.Provider;
using System.Data;
using System.Data.Common;

namespace Infrastructure.Data.Access
{
    public class UserAccessRepository : IAsyncRepository<Guid, UserAccess?>, IAsyncQuery<UserAccess?, Guid>
    {
        private readonly ISqlProvider _provider;
        private readonly ISqlCaller _caller;

        public UserAccessRepository(string connection)
        {
            _caller = new SqlCaller(_provider = new SqlServerProvider(connection));
        }

        public async Task<UserAccess?> ExecuteAsync(Guid parameter, CancellationToken token)
        {
            return (await new UserAccessSqlQuery(_provider, _caller)
                .Filter(UserAccessSqlQuery.IdFilter(parameter))
                .ExecuteAsync(token))
                .FirstOrDefault();
        }

        public async Task<UserAccess?> FindAsync(Guid key, CancellationToken token)
        {
            return (await new UserAccessSqlQuery(_provider, _caller)
               .Filter(UserAccessSqlQuery.IdFilter(key))
               .ExecuteAsync(token))
               .FirstOrDefault();
        }

        public async Task SaveAsync(UserAccess? item, CancellationToken token)
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
                            await WriteEvent(uacde, transaction, token);
                            break;
                        case PasswordChangedDataEvent pcde:
                            await WriteEvent(pcde, transaction, token);
                            break;
                        case RoleChangedDataEvent rcde:
                            await WriteEvent(rcde, transaction, token);
                            break;
                    }
                }

                await transaction.CommitAsync(token);
            }
            catch
            {
                await transaction.RollbackAsync(token);
            }
        }

        private async Task WriteEvent(UserAccessCreatedDataEvent @event, SqlTransaction transaction, CancellationToken token)
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

            await transaction.ExecuteNonQueryAsync(command, token);
        }

        private async Task WriteEvent(PasswordChangedDataEvent @event, SqlTransaction transaction, CancellationToken token)
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

            await transaction.ExecuteNonQueryAsync(command, token);
        }

        private async Task WriteEvent(RoleChangedDataEvent @event, SqlTransaction transaction, CancellationToken token)
        {
            await transaction.ExecuteNonQueryAsync($"Update UserAccesses Set [Role]='{@event.Role}' Where Id='{@event.User}' ", token);
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
