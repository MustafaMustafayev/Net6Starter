using DAL.GenericRepositories.Abstract;
using ENTITIES.Entities;

namespace DAL.Abstract;

public interface IRoleRepository : IGenericRepository<Role>
{
    Role UpdateRoleAsync(Role role);
}