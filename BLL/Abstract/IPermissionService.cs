using DAL.Utility;
using DTO.Permission;
using DTO.Responses;

namespace BLL.Abstract;

public interface IPermissionService
{
    Task<IDataResult<List<PermissionToListDto>>> GetAsync();

    Task<IDataResult<PaginatedList<PermissionToListDto>>> GetAsPaginatedListAsync(int pageIndex,
        int pageSize);

    Task<IDataResult<PermissionToListDto>> GetAsync(int id);

    Task<IResult> AddAsync(PermissionToAddDto dto);

    Task<IResult> UpdateAsync(int permissionId, PermissionToUpdateDto dto);

    Task<IResult> SoftDeleteAsync(int id);
}