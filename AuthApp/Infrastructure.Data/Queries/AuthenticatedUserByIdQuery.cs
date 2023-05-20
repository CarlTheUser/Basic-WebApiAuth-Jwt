using Application;
using Dapper;
using Data.Common.Contracts;
using System.Data;
using System.Data.SqlClient;

namespace Infrastructure.Data.Queries
{
    public class AuthenticatedUserByIdQuery : IAsyncQuery<AuthenticatedUser?, Guid>
    {
        private readonly string _connectionString;
        private readonly int _commandTimeout;

        public AuthenticatedUserByIdQuery(string connectionString, int commandTimeout)
        {
            _connectionString = connectionString;
            _commandTimeout = commandTimeout;
        }

        public async Task<AuthenticatedUser?> ExecuteAsync(Guid parameter, CancellationToken cancellationToken = default)
        {
            using var connection = new SqlConnection(connectionString: _connectionString);

            DataHolder? dataHolder = await connection.QueryFirstOrDefaultAsync<DataHolder?>(
                sql: "Select UA.Id, UA.Email, AR.[Description] As [Role] From UserAccesses UA Join AccessRoles AR On UA.[Role] = AR.Id Where UA.Id = @Id",
                param: new
                {
                    Id = parameter,
                },
                transaction: null,
                commandTimeout: _commandTimeout,
                commandType: CommandType.Text);

            if (dataHolder != null)
            {
                return new AuthenticatedUser(
                    Id: dataHolder.Id,
                    Email: dataHolder.Email,
                    Role: dataHolder.Role);
            }

            return null;
        }

        private class DataHolder
        {
            public Guid Id { get; set; }
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
        }
    }
}