using Access;
using Data.Common.Contracts;
using Data.Sql;
using Data.Sql.Mapping;
using Data.Sql.Provider;
using System.Data;
using System.Data.Common;

namespace Infrastructure.Data.Access
{
    public class UserAccessRepository : IAsyncRepository<Guid, UserAccess?>
    {
        private readonly ISqlProvider _provider;
        private readonly ISqlCaller _caller;

        public UserAccessRepository(string connection)
        {
            _caller = new SqlCaller(_provider = new SqlServerProvider(connection));
        }

        public async Task<UserAccess?> FindAsync(Guid key, CancellationToken token)
        {
            string query = $"Select UA.Id, UA.Email, UA.[Role] As RoleId, AR.[Description] As RoleDescription, UA.Salt, UA.[Hash] From UserAccesses UA Join AccessRoles AR On UA.[Role] = AR.Id Where UA.Id='{key}' ";

            IEnumerable<DataHolder> items = await _caller.GetAsync(new ReflectionDataMapper<DataHolder>(), query, token);

            return (from item in items
                    select UserAccess.Existing(
                        item.Id,
                        item.Email,
                        new Role(
                            item.RoleId,
                            item.RoleDescription),
                        new SecurePassword(
                            item.Salt, 
                            item.Hash))).FirstOrDefault();
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
