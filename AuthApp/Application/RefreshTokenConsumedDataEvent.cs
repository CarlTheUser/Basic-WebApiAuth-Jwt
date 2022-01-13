using Data.Common.Contracts;

namespace Application
{
    public class RefreshTokenConsumedDataEvent : DataEvent
    {
        public Guid Id { get; }

        public RefreshTokenConsumedDataEvent(Guid id)
        {
            Id = id;
        }
    }
}
