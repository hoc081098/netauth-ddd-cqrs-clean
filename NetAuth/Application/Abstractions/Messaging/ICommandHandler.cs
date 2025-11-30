using LanguageExt;
using MediatR;
using NetAuth.Domain.Core.Primitives;

namespace NetAuth.Application.Abstractions.Messaging;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Either<DomainError, TResponse>>
    where TCommand : ICommand<TResponse>;