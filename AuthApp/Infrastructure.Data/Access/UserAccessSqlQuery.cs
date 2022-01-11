using Access;
using Data.Sql;
using Data.Sql.Mapping;
using Data.Sql.Provider;
using Data.Sql.Querying;
using System.Data;
using System.Data.Common;

namespace Infrastructure.Data.Access
{
    internal class UserAccessSqlQuery : SqlQuery<UserAccess>
    {
        public static QueryFilter IdFilter(Guid id) => new _IdFilter(id);

        public static QueryFilter EmailFilter(string email) => new _EmailFilter(email);

        private const string BASE_QUERY = "Select UA.Id, UA.Email, UA.[Role] As RoleId, AR.[Description] As RoleDescription, UA.Salt, UA.[Hash] From UserAccesses UA Join AccessRoles AR On UA.[Role] = AR.Id ";

        private readonly ISqlProvider _provider;

        private readonly ISqlCaller _caller;

        public UserAccessSqlQuery(string connection)
        {
            _caller = new SqlCaller(_provider = new SqlServerProvider(connection));
        }

        public UserAccessSqlQuery(ISqlProvider provider, ISqlCaller caller)
        {
            _provider = provider;
            _caller = caller;
        }

        public override IEnumerable<UserAccess> Execute()
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

            IEnumerable<DataHolder> items = _caller.Get(new ReflectionDataMapper<DataHolder>(), command);

            return from item in items
                   select UserAccess.Existing(
                       item.Id,
                       item.Email,
                       new Role(
                           item.RoleId,
                           item.RoleDescription),
                       new SecurePassword(
                           item.Salt,
                           item.Hash));
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

        public class _IdFilter : QueryFilter
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
                return $"UA.Id='{Id}' ";
            }
        }

        public class _EmailFilter : QueryFilter
        {
            public string Email { get; set; } = string.Empty;

            public _EmailFilter()
            {
                UsesParameter = true;
            }

            public _EmailFilter(string email) : this()
            {
                Email = email;
            }

            public override string ToSqlClause()
            {
                return $"UA.Email=@Email ";
            }

            public override DbParameter[] GetParameters()
            {
                return new DbParameter[] { new SqlServerProvider().CreateInputParameter("@Email", Email, DbType.String) };
            }
        }
    }
}
