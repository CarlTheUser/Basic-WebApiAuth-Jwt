using Application;
using Data.Common.Contracts;

namespace Infrastructure.Data.Application
{
    public record class RefreshTokenByUserValueParameter(Guid User, string Value);

    public class RefreshTokenByUserValueQuery : IAsyncQuery<RefreshToken?, RefreshTokenByUserValueParameter>
    {
        private readonly string _connection;

        public RefreshTokenByUserValueQuery(string connection)
        {
            _connection = connection;
        }

        public async Task<RefreshToken?> ExecuteAsync(RefreshTokenByUserValueParameter parameter, CancellationToken token)
        {
            return (await new RefreshTokenSqlQuery(_connection)
                .Filter(RefreshTokenSqlQuery.UserFilter(parameter.User).And(RefreshTokenSqlQuery.ValueFilter(parameter.Value)))
                .ExecuteAsync(token))
                .FirstOrDefault();
        }
    }
}
