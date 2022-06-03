using Data.Common.Contracts;

namespace Application.Repositories
{
    public interface IRefreshTokenRepository : IAsyncRepository<RefreshToken>
    {
        public record IdSpecification(Guid Id) : ISpecification;

        public record UserWithRefreshTokenSpecification(Guid User, string RefreshToken) : ISpecification;
    }
}
