﻿namespace ENTITIES.Entities;

public class Permission : Auditable, IEntity
{
    public int PermissionId { get; set; }
    public required string Name { get; set; }
    public required string Key { get; set; }
    public virtual List<Role>? Roles { get; set; }
}