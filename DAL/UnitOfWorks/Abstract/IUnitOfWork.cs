using DAL.Abstract;

namespace DAL.UnitOfWorks.Abstract;

public interface IUnitOfWork : IAsyncDisposable, IDisposable
{
    public IUserRepository UserRepository { get; set; }

    public IAuthRepository AuthRepository { get; set; }

    public ILoggingRepository LoggingRepository { get; set; }

    public IRoleRepository RoleRepository { get; set; }

    public IOrganizationRepository OrganizationRepository { get; set; }
    public IPermissionRepository PermissionRepository { get; set; }
    public ITokenRepository TokenRepository { get; set; }

    public Task CommitAsync();
}