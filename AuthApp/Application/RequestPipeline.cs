using MediatR;

namespace Application
{
    public interface IPostRequestHandler<in TRequest, in TResponse> where TRequest : IRequest<TResponse>
    {
        Task Handle(TRequest request, TResponse response);
    }

    public class RequestPipeline<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private readonly IRequestHandler<TRequest, TResponse> _innerRequestHandler;
        private readonly IPostRequestHandler<TRequest, TResponse>[] _postRequestHandlers;

        public RequestPipeline(
            IRequestHandler<TRequest, TResponse> innerRequestHandler,
            IPostRequestHandler<TRequest, TResponse>[] postRequestHandlers)
        {
            _innerRequestHandler = innerRequestHandler;
            _postRequestHandlers = postRequestHandlers;
        }

        public async Task<TResponse> Handler(TRequest request, CancellationToken token)
        {
            var response = await _innerRequestHandler.Handle(request, token);

            foreach(var postHandler in _postRequestHandlers)
            {
                await postHandler.Handle(request, response);
            }

            return response;
        }
    }
}
