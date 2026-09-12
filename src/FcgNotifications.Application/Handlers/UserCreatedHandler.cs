using FcgNotifications.Application.Commands;
using FcgNotifications.Domain.Entities;
using FcgNotifications.Domain.Repositories;
using FcgUsers.Domain.ValueObjects;
using MediatR;
using OperationResult;

namespace FcgNotifications.Application.Handlers;

public sealed class CreateUserCommandHandler(
    IUserRepository repository)
: IRequestHandler<CreateUserCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var exists = await repository.ExistsAsync(request.UserId, cancellationToken);

        if (exists)
        {
            return Result.Success(true);
        }

        var email = Email.Create(request.Email);

        var user = new User(request.UserId, request.Name, email);

        repository.Add(user);

        await repository.SaveChangesAsync();

        return Result.Success(true);
    }
}
