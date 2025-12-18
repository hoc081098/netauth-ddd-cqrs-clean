using LanguageExt;
using MediatR;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Abstractions.Messaging;

public interface ICommand<TResponse> : IRequest<Either<DomainError, TResponse>>;