using Access.Models.Entities;
using Access.Models.Primitives;
using Data.Common.Contracts.SpecificationRepositories;

namespace Access.Repositories
{
    #region Specification Definitions

    public record RoleByRoleIdSpecification(
            RoleId RoleId) : ISpecification<Role?>;

    #endregion

    public interface IRoleRepository :
        IAsyncRepository<Role>,
        IHandlesSpecificationAsync<RoleByRoleIdSpecification, Role?>
    {

    }
}
