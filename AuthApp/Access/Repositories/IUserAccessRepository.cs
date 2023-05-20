using Access.Models.Entities;
using Access.Models.Primitives;
using Data.Common.Contracts.SpecificationRepositories;

namespace Access.Repositories
{
    #region Specification Definitions

    public record UserAccessByUserAccessIdSpecification(UserAccessId UserAccessId) : ISpecification<UserAccess?>;

    public record UserAccessByEmailSpecification(Email Email) : ISpecification<UserAccess?>;

    #endregion

    public interface IUserAccessRepository : 
        IAsyncRepository<UserAccess>,
        IHandlesSpecificationAsync<UserAccessByUserAccessIdSpecification, UserAccess?>,
        IHandlesSpecificationAsync<UserAccessByEmailSpecification, UserAccess?>
    {

    }
}
