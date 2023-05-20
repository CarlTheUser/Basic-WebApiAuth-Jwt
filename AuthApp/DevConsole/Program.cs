// See https://aka.ms/new-console-template for more information
using Access.Models.Entities;
using Access.Models.Primitives;
using Access.Models.ValueObjects;
using Dapper;
using Infrastructure.Data.Repositories;
using System.Data.SqlClient;

//const string localConnectionString = @"Data Source='.\SQLEXPRESS';Initial Catalog=UserAccessManagementDB;User ID=sa;Password=P@$$W0RD;Encrypt=True;TrustServerCertificate=True;";
const string localConnectionString = @"Data Source='.\SQLEXPRESS';Initial Catalog=UserAccessManagement;User ID=sa;Password=P@$$W0RD;TrustServerCertificate=True;";
static void InitializeUserAccess()
{
    UserAccess userAccess = UserAccess.New(
        email: "qazzqc@foo.company",
        roleId: new RoleId(GetAdministratorRole()),
        password: new SecurePassword(password: "K1mD4hYun0528<3"));

    var repository = new UserAccessRepository(

        connectionString: localConnectionString,
        commandTimeout: 300);

    repository.SaveAsync(userAccess, CancellationToken.None).Wait();
}

static Guid GetAdministratorRole()
{
    using var connection = new SqlConnection(connectionString: localConnectionString);
    
    var guid = connection.ExecuteScalar<Guid?>(sql: "Select Id From AccessRoles Where [Description] = 'User Administrator'");

    return guid ?? throw new Exception("Run UserAccessManagementDb Creation.sql first");
}

Console.WriteLine("Hello, World!");

InitializeUserAccess();