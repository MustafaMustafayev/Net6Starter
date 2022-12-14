using DTO.Organization;
using DTO.Responses;
using MediatR;

namespace BLL.MediatR.OrganizationCQRS.Commands;

public record UpdateOrganizationCommand(int OrganizationId, OrganizationToUpdateDto Organization) : IRequest<IResult>;