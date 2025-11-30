using LanguageExt;
using MediatR;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Abstractions.Messaging;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Either<DomainError, TResponse>>
    where TQuery : IQuery<TResponse>;