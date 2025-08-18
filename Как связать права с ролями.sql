-- Назначение прав роли "Admin" (ID=1)
INSERT INTO RolePermissions (RoleId, PermissionId) VALUES 
    (1, 1),  -- ViewDashboard
    (1, 2),  -- EditDocument
    (1, 3),  -- DeleteDocument
    (1, 4),  -- ViewReports
    (1, 5),  -- ManageUsers
    (1, 6);  -- ConfigureSystem
	
-- Назначение прав роли "User" (ID=2)
INSERT INTO RolePermissions (RoleId, PermissionId) VALUES 
    (2, 1),  -- ViewDashboard
	(2, 4);  -- ViewReports
	
--Как проверить, какие права есть у роли?
-- Для роли с ID=1 (Admin)
SELECT p.PermissionName 
FROM RolePermissions rp
JOIN Permissions p ON rp.PermissionId = p.PermissionId
WHERE rp.RoleId = 1;