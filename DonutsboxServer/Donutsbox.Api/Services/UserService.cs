using Donutsbox.Api.Dto;
using Donutsbox.Domain.Repositories;
using Donutsbox.Domain.Entities;

namespace Donutsbox.Api.Services;

public class UserService(IEntityRepository<UserDTO, Guid> repository) : IEntityService<UserDTO, Guid>
{

}
