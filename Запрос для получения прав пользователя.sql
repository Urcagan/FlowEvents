Select r.RoleId, rp.PermissionId, p.PermissionName from Roles as r 
JOIN RolePermissions as rp on r.RoleId = rp.RoleId
JOIN Permissions as p on rp.PermissionId = p.PermissionId
where r.RoleName = "Manager_OC"