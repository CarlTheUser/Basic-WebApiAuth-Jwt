using Access;
using Data.Common.Contracts;

namespace Application.Repositories
{
    public interface IUserAccessRepository :IAsyncRepository<UserAccess>
    {
        public record IdSpecification(Guid Id) : ISpecification;

        public record EmailSpecification(string Email) : ISpecification;
    }
}
