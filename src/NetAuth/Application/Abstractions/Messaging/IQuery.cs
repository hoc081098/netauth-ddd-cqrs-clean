using LanguageExt;
using MediatR;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Either<DomainError, TResponse>>;