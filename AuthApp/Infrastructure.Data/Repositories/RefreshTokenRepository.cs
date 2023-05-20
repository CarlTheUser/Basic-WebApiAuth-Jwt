using Access.Events;
using Access.Models.Entities;
using Access.Models.Primitives;
using Access.Repositories;
using Dapper;
using Data.Common.Contracts;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Infrastructure.Data.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly string _connectionString;
        private readonly int _commandTimeout;

        public RefreshTokenRepository(string connectionString, int commandTimeout)
        {
            _connectionString = connectionString;
            _commandTimeout = commandTimeout;
        }

        public async Task<RefreshToken?> FindAsync(RefreshTokenByRefreshTokenIdSpecification specification, CancellationToken cancellationToken = default)
        {
            var commandDefinition = new CommandDefinition(
                commandText: "Select R.Id, R.[User], R.Token, R.Issued, R.Expiry From CurrentRefreshTokens R Where R.[Id] = @Id",
                parameters: new
                {
                    Id = specification.RefreshTokenId.Value,
                },
                transaction: null,
                commandTimeout: _commandTimeout,
                commandType: CommandType.Text,
                cancellationToken: cancellationToken);

            using var connection = new SqlConnection(connectionString: _connectionString);

            DataHolder? dataHolder = await connection.QueryFirstOrDefaultAsync<DataHolder?>(commandDefinition);

            if (dataHolder != null)
            {
                return new RefreshToken(
                    id: new RefreshTokenId(dataHolder.Id),
                    issuedTo: new UserAccessId(dataHolder.User),
                    code: new TokenCode(dataHolder.Token),
                    issued: dataHolder.Issued,
                    expiry: dataHolder.Expiry);
            }

            return null;
        }

        public async Task<RefreshToken?> FindAsync(RefreshTokenByIssuedToAndTokenCodeSpecification specification, CancellationToken cancellationToken = default)
        {
            var commandDefinition = new CommandDefinition(
                commandText: "Select R.Id, R.[User], R.Token, R.Issued, R.Expiry From CurrentRefreshTokens R Where R.[User] = @User And R.Token = @Token ",
                parameters: new
                {
                    User = specification.IssuedTo.Value,
                    Token = specification.TokenCode.Value
                },
                transaction: null,
                commandTimeout: _commandTimeout,
                commandType: CommandType.Text,
                cancellationToken: cancellationToken);
                
            using var connection = new SqlConnection(connectionString: _connectionString);

            DataHolder? dataHolder = await connection.QueryFirstOrDefaultAsync<DataHolder?>(commandDefinition);


            if (dataHolder != null)
            {
                return new RefreshToken(
                    id: new RefreshTokenId(dataHolder.Id),
                    issuedTo: new UserAccessId(dataHolder.User),
                    code: new TokenCode(dataHolder.Token),
                    issued: dataHolder.Issued,
                    expiry: dataHolder.Expiry);
            }

            return null;
        }

        public async Task SaveAsync(RefreshToken item, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(connectionString: _connectionString);

            await connection.OpenAsync(cancellationToken);

            using var transaction = await connection.BeginTransactionAsync(
                isolationLevel: IsolationLevel.ReadCommitted, 
                cancellationToken: cancellationToken);

            try
            {
                DataEvent? @event;

                while ((@event = item.DequeueDataEvent()) != null)
                {
                    switch (@event)
                    {
                        case RefreshTokenIssuedDataEvent rtide:
                            await WriteEvent(rtide, transaction, cancellationToken);
                            break;
                        case RefreshTokenConsumedDataEvent rtcde:
                            await WriteEvent(rtcde, transaction, cancellationToken);
                            break;
                    }
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);

                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        #region Map Object Structure

        private class DataHolder
        {
            public Guid Id { get; set; }
            public Guid User { get; set; }
            public string Token { get; set; } = string.Empty;
            public DateTime Issued { get; set; }
            public DateTime Expiry { get; set; }
        }

        #endregion

        #region Write Event to Database

        private async Task WriteEvent(RefreshTokenIssuedDataEvent @event, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            var commandDefinition = new CommandDefinition(
                commandText: "Insert Into CurrentRefreshTokens(Id,[User],Token,Issued,Expiry)Values(@Id,@User,@Token,@Issued,@Expiry) ",
                parameters: new
                {
                    Id = @event.RefreshTokenId,
                    User = @event.IssuedTo,
                    Token = @event.TokenCode,
                    @event.Issued,
                    @event.Expiry,

                },
                transaction: transaction,
                commandTimeout: _commandTimeout,
                commandType: CommandType.Text,
                cancellationToken: cancellationToken);

            _ = await transaction.Connection.ExecuteAsync(commandDefinition);
        }

        private async Task WriteEvent(RefreshTokenConsumedDataEvent @event, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            var commandDefinition = new CommandDefinition(
                commandText: $"Delete From CurrentRefreshTokens Where Id = @Id",
                parameters: new
                {
                    Id = @event.RefreshTokenId.ToString(),

                },
                transaction: transaction,
                commandTimeout: _commandTimeout,
                commandType: CommandType.Text,
                cancellationToken: cancellationToken);

            _ = await transaction.Connection.ExecuteAsync(commandDefinition);
        }

        #endregion
    }
}
