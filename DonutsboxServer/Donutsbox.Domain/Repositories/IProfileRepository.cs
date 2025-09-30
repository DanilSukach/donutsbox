namespace Donutsbox.Domain.Repositories;

public interface IProfileRepository<TUserData, Tuser>
{
    Task<(TUserData?, Tuser?)> GetUserDataByIdAsync(Guid id);
}
