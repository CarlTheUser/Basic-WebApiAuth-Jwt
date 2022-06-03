using Access;
using Application.Repositories;
using Data.Common.Contracts;
using Data.Sql;
using Data.Sql.Provider;
using System.Data;
using System.Data.Common;
using static Application.Repositories.IUserAccessRepository;

namespace Infrastructure.Data.Access
{
    public class UserAccessRepository : IUserAccessRepository
    {
        

        private readonly ISqlProvider _provider;
        private readonly ISqlCaller _caller;

        public UserAccessRepository(string connection)
        {
            _caller = new SqlCaller(_provider = new SqlServerProvider(connection));
        }

        public async Task<UserAccess?> FindAsync(ISpecification specs, CancellationToken token)
        {
            UserAccessSqlQuery query = new(_provider, _caller);

            return specs switch
            {
                IdSpecification ids => (await query.Filter(UserAccessSqlQuery.IdFilter(ids.Id))
                                        .ExecuteAsync(token))
                                        .FirstOrDefault(),
                EmailSpecification es => (await query.Filter(UserAccessSqlQuery.EmailFilter(es.Email))
                                        .ExecuteAsync(token))
                                        .FirstOrDefault(),
                _ => null,
            };
        }

        public async Task SaveAsync(UserAccess item, CancellationToken token)
        {
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

                throw;
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
    }
}
