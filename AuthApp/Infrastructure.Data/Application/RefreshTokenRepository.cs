using Application;
using Data.Common.Contracts;
using Data.Sql;
using Data.Sql.Provider;
using System.Data;
using System.Data.Common;

namespace Infrastructure.Data.Application
{
    public class RefreshTokenRepository : IAsyncRepository<Guid, RefreshToken>
    {
        private readonly ISqlProvider _provider;

        private readonly ISqlCaller _caller;

        public RefreshTokenRepository(string connection)
        {
            _caller = new SqlCaller(_provider = new SqlServerProvider(connection));
        }

        public async Task<RefreshToken?> FindAsync(Guid key, CancellationToken token)
        {
            return (await new RefreshTokenSqlQuery(_provider, _caller)
                .Filter(RefreshTokenSqlQuery.IdFilter(key))
                .ExecuteAsync(token))
                .FirstOrDefault();
        }

        public async Task SaveAsync(RefreshToken item, CancellationToken token)
        {
            var events = item.ReleaseEvents();

            SqlTransaction transaction = _caller.CreateScopedTransaction(IsolationLevel.ReadCommitted);

            try
            {
                foreach (var @event in events)
                {
                    switch (@event)
                    {
                        case RefreshTokenIssuedDataEvent rtide:
                            await WriteEvent(rtide, transaction, token);
                            break;
                        case RefreshTokenConsumedDataEvent rtcde:
                            await WriteEvent(rtcde, transaction, token);
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

        private async Task WriteEvent(RefreshTokenIssuedDataEvent @event, SqlTransaction transaction, CancellationToken token)
        {
            DbCommand command = _provider.CreateCommand(
                $"Insert Into CurrentRefreshTokens(Id,[User],Token,Issued,Expiry)Values('{@event.Id}','{@event.IssuedTo}',@Token,@Issued,@Expiry) ",
                CommandType.Text,
                _provider.CreateInputParameters(
                    new
                    {
                        Token = @event.Value,
                        @event.Issued,
                        @event.Expiry
                    }, "@"));

            await transaction.ExecuteNonQueryAsync(command, token);
        }

        private async Task WriteEvent(RefreshTokenConsumedDataEvent @event, SqlTransaction transaction, CancellationToken token)
        {
            await transaction.ExecuteNonQueryAsync($"Delete From CurrentRefreshTokens Where Id='{@event.Id}' ", token);
        }
    }
}
