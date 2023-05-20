using Access.Models.Primitives;
using Data.Common.Contracts;

namespace Access.Events
{
    public class RefreshTokenConsumedDataEvent : DataEvent
    {
        public Guid RefreshTokenId { get; }

        public RefreshTokenConsumedDataEvent(RefreshTokenId refreshTokenId)
        {
            RefreshTokenId = refreshTokenId.Value;
        }
    }
}
