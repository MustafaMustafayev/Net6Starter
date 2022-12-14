using DAL.GenericRepositories.Abstract;
using ENTITIES.Entities;

namespace DAL.Abstract;

public interface IAuthRepository : IGenericRepository<User>
{
}