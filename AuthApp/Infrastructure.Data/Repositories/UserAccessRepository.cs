using Access.Events;
using Access.Models.Entities;
using Access.Models.Primitives;
using Access.Models.ValueObjects;
using Access.Repositories;
using Dapper;
using Data.Common.Contracts;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;

namespace Infrastructure.Data.Repositories
{
    public class UserAccessRepository : IUserAccessRepository
    {
        private readonly string _connectionString;
        private readonly int _commandTimeout;

        public UserAccessRepository(string connectionString, int commandTimeout)
        {
            _connectionString = connectionString;
            _commandTimeout = commandTimeout;
        }

        public async Task<UserAccess?> FindAsync(UserAccessByUserAccessIdSpecification specification, CancellationToken cancellationToken = default)
        {
            var commandDefinition = new CommandDefinition(
               commandText: "Select UA.Id, UA.Email, UA.[Role] As RoleId, UA.Salt, UA.[Hash] From UserAccesses UA Where UA.Id = @Id",
               parameters: new
               {
                   Id = specification.UserAccessId.Value,
               },
               transaction: null,
               commandTimeout: _commandTimeout,
               commandType: CommandType.Text,
               cancellationToken: cancellationToken);

            using var connection = new SqlConnection(connectionString: _connectionString);

            DataHolder? dataHolder = await connection.QueryFirstOrDefaultAsync<DataHolder?>(commandDefinition);

            if (dataHolder != null)
            {
                return UserAccess.Existing(
                    userAccessId: dataHolder.Id,
                    email: dataHolder.Email,
                    roleId: new RoleId(dataHolder.Id),
                    password: new SecurePassword(
                        salt: dataHolder.Salt,
                        value: dataHolder.Hash));
            }

            return null;
        }

        public async Task<UserAccess?> FindAsync(UserAccessByEmailSpecification specification, CancellationToken cancellationToken = default)
        {
            var commandDefinition = new CommandDefinition(
               commandText: "Select UA.Id, UA.Email, UA.[Role] As RoleId, UA.Salt, UA.[Hash] From UserAccesses UA Where UA.Email = @Email",
               parameters: new
               {
                   Email = specification.Email.Value,
               },
               transaction: null,
               commandTimeout: _commandTimeout,
               commandType: CommandType.Text,
               cancellationToken: cancellationToken);

            using var connection = new SqlConnection(connectionString: _connectionString);

            DataHolder? dataHolder = await connection.QueryFirstOrDefaultAsync<DataHolder?>(commandDefinition);

            if(dataHolder != null)
            {
                return UserAccess.Existing(
                    userAccessId: dataHolder.Id,
                    email: dataHolder.Email,
                    roleId: new RoleId(dataHolder.Id),
                    password: new SecurePassword(
                        salt: dataHolder.Salt,
                        value: dataHolder.Hash));
            }

            return null;
        }

        public async Task SaveAsync(UserAccess item, CancellationToken cancellationToken = default)
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
                        case UserAccessCreatedDataEvent uacde:
                            await WriteEvent(uacde, transaction, cancellationToken);
                            break;
                        case PasswordChangedDataEvent pcde:
                            await WriteEvent(pcde, transaction, cancellationToken);
                            break;
                        case RoleChangedDataEvent rcde:
                            await WriteEvent(rcde, transaction, cancellationToken);
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
            public string Email { get; set; } = string.Empty;
            public Guid RoleId { get; set; }
            public byte[] Salt { get; set; } = Array.Empty<byte>();
            public byte[] Hash { get; set; } = Array.Empty<byte>();
        }

        #endregion

        #region Write Event to Database

        private async Task WriteEvent(UserAccessCreatedDataEvent @event, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            var commandDefinition = new CommandDefinition(
               commandText: "Insert Into UserAccesses(Id,Email,[Role],Salt,[Hash])Values(@Id,@Email,@Role,@Salt,@Hash)",
               parameters: new
               {
                   Id = @event.User,
                   @event.Email,
                   @event.Role,
                   @event.Salt, 
                   @event.Hash
               },
               transaction: transaction,
               commandTimeout: _commandTimeout,
               commandType: CommandType.Text,
               cancellationToken: cancellationToken);

            _ = await transaction.Connection.ExecuteAsync(commandDefinition);
        }

        private async Task WriteEvent(PasswordChangedDataEvent @event, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            var commandDefinition = new CommandDefinition(
               commandText: "Update UserAccesses Set Salt=@Salt,[Hash]=@Hash Where Id=@Id",
               parameters: new
               {
                   Id = @event.User,
                   @event.Salt,
                   @event.Hash
               },
               transaction: transaction,
               commandTimeout: _commandTimeout,
               commandType: CommandType.Text,
               cancellationToken: cancellationToken);

            _ = await transaction.Connection.ExecuteAsync(commandDefinition);
        }

        private async Task WriteEvent(RoleChangedDataEvent @event, DbTransaction transaction, CancellationToken cancellationToken = default)
        {
            var commandDefinition = new CommandDefinition(
               commandText: "Update UserAccesses Set [Role]=@.Role Where Id=@Id",
               parameters: new
               {
                   Id = @event.User,
                   @event.Role
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
