using Access.Models.Entities;
using Access.Models.Primitives;
using Data.Common.Contracts.SpecificationRepositories;

namespace Access.Repositories
{
    #region Specification Definitions

    public record RefreshTokenByRefreshTokenIdSpecification(
            RefreshTokenId RefreshTokenId) : ISpecification<RefreshToken?>;

    public record RefreshTokenByIssuedToAndTokenCodeSpecification(
        UserAccessId IssuedTo,
        TokenCode TokenCode) : ISpecification<RefreshToken?>;

    #endregion

    public interface IRefreshTokenRepository : 
        IAsyncRepository<RefreshToken>,
        IHandlesSpecificationAsync<RefreshTokenByRefreshTokenIdSpecification, RefreshToken?>,
        IHandlesSpecificationAsync<RefreshTokenByIssuedToAndTokenCodeSpecification, RefreshToken?>
    {

    }
}
