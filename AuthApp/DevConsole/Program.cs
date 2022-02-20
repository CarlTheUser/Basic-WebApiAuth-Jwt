// See https://aka.ms/new-console-template for more information
using Access;
using Data.Sql;
using Data.Sql.Provider;
using Infrastructure.Data.Access;


const string localConnectionString = @"Data Source='DESKTOP-U64U4KB\SQLEXPRESS';Initial Catalog=UserAccessManagementDb;User ID=sa;Password=password;";

static void InitializeUserAccess()
{
    Role administrator = new Role(GetAdministratorRole(), "User Administrator");

    string serverPeanuts = "long vanguard map intermediary address sun honky mason";

    UserAccess userAccess = UserAccess.New(
        email: "qazzqc@foo.company",
        role: administrator,
        password: new SecurePassword(
            password: serverPeanuts + "K1mD4hYun0528<3"));

    var repository = new UserAccessRepository(localConnectionString);

    repository.SaveAsync(userAccess, CancellationToken.None).Wait();
}

static Guid GetAdministratorRole()
{
    SqlServerProvider sqlServerProvider = new(localConnectionString);

    ISqlCaller caller = new SqlCaller(sqlServerProvider);

    var guid = caller.ExecuteScalar("Select Id From AccessRoles Where [Description] = 'User Administrator' ");

    return (Guid)(guid ?? throw new Exception("Run UserAccessManagementDb Creation.sql first"));
}

Console.WriteLine("Hello, World!");

InitializeUserAccess();