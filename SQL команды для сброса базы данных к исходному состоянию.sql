-- Удаляем все записи
DELETE FROM AttachedFiles;
-- Сбрасываем счетчик автоинкремента
DELETE FROM sqlite_sequence WHERE name='AttachedFiles';

DELETE FROM EventUnits;
DELETE FROM sqlite_sequence WHERE name='EventUnits';

DELETE FROM Events;
DELETE FROM sqlite_sequence WHERE name='Events';

DELETE FROM Units;
DELETE FROM sqlite_sequence WHERE name='Units';

DELETE FROM Category;
DELETE FROM sqlite_sequence WHERE name='Category';

DELETE FROM Users;
DELETE FROM sqlite_sequence WHERE name='Users';

-- Вставляем поьлзователя Administrator с паролем 123456
INSERT INTO "main"."Users" ("UserName", "DomainName", "DisplayName", "Email", "RoleId", "IsAllowed", "Password", "Salt", "IsLocal") VALUES ('Administrator', '', 'Administrator', '', 3, 1, '2o+mvkl2vsUfrPpv1INnBqDPAXIH7ZkYmGAYeyrWHbc=', 'LqDYl2e0BUuTFZF2knc9fw==', 1);
