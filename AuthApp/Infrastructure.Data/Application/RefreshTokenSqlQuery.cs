using Application;
using Data.Sql;
using Data.Sql.Mapping;
using Data.Sql.Provider;
using Data.Sql.Querying;
using System.Data;
using System.Data.Common;

namespace Infrastructure.Data.Application
{
    internal class RefreshTokenSqlQuery : AsyncSqlQuery<RefreshToken>
    {
        public static QueryFilter IdFilter(Guid id) => new _IdFilter(id);

        public static QueryFilter UserFilter(Guid user) => new _UserFilter(user);

        public static QueryFilter ValueFilter(string value) => new _ValueFilter(value);

        private const string BASE_QUERY = "Select R.Id, R.[User], R.Token, R.Issued, R.Expiry From CurrentRefreshTokens R ";

        private readonly ISqlProvider _provider;

        private readonly ISqlCaller _caller;

        public RefreshTokenSqlQuery(string connection)
        {
            _caller = new SqlCaller(_provider = new SqlServerProvider(connection));
        }

        public RefreshTokenSqlQuery(ISqlProvider provider, ISqlCaller caller)
        {
            _provider = provider;
            _caller = caller;
        }

        public override async Task<IEnumerable<RefreshToken>> ExecuteAsync(CancellationToken token)
        {
            string query = BASE_QUERY;

            bool usesParameter = false;

            DbParameter[] parameters = Array.Empty<DbParameter>();

            if (_filter != null)
            {
                usesParameter = _filter.UsesParameter;

                query += $"Where {_filter.ToSqlClause()} ";

                parameters = _filter.GetParameters();
            }

            DbCommand command = _provider.CreateCommand(query, CommandType.Text, parameters);

            IEnumerable<DataHolder> items = await _caller.GetAsync(new ReflectionDataMapper<DataHolder>(), command, token);

            return from item in items
                   select new RefreshToken(
                       id: item.Id,
                       issuedTo: item.User,
                       value: item.Token,
                       issued: item.Issued,
                       expiry: item.Expiry);
        }

        private class DataHolder
        {
            public Guid Id { get; set; }
            public Guid User { get; set; }
            public string Token { get; set; } = string.Empty;
            public DateTime Issued { get; set; } 
            public DateTime Expiry { get; set; } 
        }

        private class _IdFilter : QueryFilter
        {
            public Guid Id { get; set; }

            public _IdFilter()
            {

            }

            public _IdFilter(Guid id)
            {
                Id = id;
            }

            public override string ToSqlClause()
            {
                return $"R.Id='{Id}' ";
            }
        }

        private class _UserFilter : QueryFilter
        {
            public Guid User { get; set; } 

            public _UserFilter()
            {

            }

            public _UserFilter(Guid user)
            {
                User = user;
            }

            public override string ToSqlClause()
            {
                return $"R.[User]='{User}' ";
            }

        }

        private class _ValueFilter : QueryFilter
        {
            public string Value { get; set; } = string.Empty;

            public _ValueFilter()
            {
                UsesParameter = true;
            }

            public _ValueFilter(string value) : this()
            {
                Value = value;
            }

            public override string ToSqlClause()
            {
                return $"R.Token=@Token";
            }

            public override DbParameter[] GetParameters()
            {
                return new DbParameter[] { new SqlServerProvider().CreateInputParameter("@Token", Value, DbType.String) };
            }
        }
    }
}
