-- Базовые права для системы
INSERT INTO Permissions (PermissionName) VALUES 
    ('ViewDashboard'),      -- Просмотр главной страницы
    ('EditDocument'),       -- Редактирование документов
    ('DeleteDocument'),     -- Удаление документов
    ('ViewReports'),        -- Просмотр отчетов
    ('ManageUsers'),        -- Управление пользователями
    ('ConfigureSystem');    -- Настройка системы

-- Специфичные права (если нужно)
INSERT INTO Permissions (PermissionName) VALUES 
    ('ExportData'),
    ('ImportData'),
    ('AuditLogs');