-- 1. Создаем временную таблицу с новой структурой
CREATE TABLE AttachedFiles_temp (
    FileId INTEGER PRIMARY KEY AUTOINCREMENT,
    EventId INTEGER NOT NULL,
    FileCategory TEXT NOT NULL DEFAULT 'document',  -- Новый столбец
    FileName TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    FileSize INTEGER NOT NULL,
    UploadDate TEXT NOT NULL DEFAULT (datetime('now', 'localtime')),
    FileType TEXT NOT NULL,
    Description TEXT, 
    UploadedBy TEXT,
    FOREIGN KEY (EventId) REFERENCES Events(id) ON DELETE CASCADE
);

-- 2. Копируем данные из старой таблицы во временную
INSERT INTO AttachedFiles_temp (
    FileId, EventId, FileName, FilePath, 
    FileSize, UploadDate, FileType, Description, UploadedBy
)
SELECT 
    FileId, EventId, FileName, FilePath, 
    FileSize, UploadDate, FileType, Description, UploadedBy
FROM AttachedFiles;

-- 3. Удаляем старую таблицу
DROP TABLE AttachedFiles;

-- 4. Переименовываем временную таблицу
ALTER TABLE AttachedFiles_temp RENAME TO AttachedFiles;
