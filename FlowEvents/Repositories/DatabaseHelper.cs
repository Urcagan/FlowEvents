// --------------------------------
// Класс для даботы с базой данных
// --------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;

namespace FlowEvents
{
    public class DatabaseHelper
    {
        private readonly CultureInfo culture = AppBaseConfig.culture;   // Культура русского языка
        private readonly string formatDate = AppBaseConfig.formatDate; //Формат даты в приложении
        private readonly string formatDateTime = AppBaseConfig.formatDateTime; // формат даты с временем

        private static SQLiteConnection _connection;
        private static readonly object _lock = new object();
        private static string _databasePath;

        public DatabaseHelper(string databasePath)
        {
            // Проверяем и корректируем путь
            _databasePath = AdjustNetworkPath(databasePath);

            InitializeConnection();
        }

        /// <summary>
        /// Корректирует путь, если он является сетевым.
        /// </summary>
        private string AdjustNetworkPath(string path)
        {
            return path.StartsWith("\\") ? @"\\" + path : path;
        }

        /// <summary>
        /// Инициализация строки подключения к БД
        /// </summary>
        private static void InitializeConnection()
        {
            if (_connection == null || GetDatabasePathFromConnectionString(_connection.ConnectionString) != _databasePath)
            {
                lock (_lock)
                {
                    if (_connection == null || GetDatabasePathFromConnectionString(_connection.ConnectionString) != _databasePath)
                    {
                        _connection?.Dispose();
                        _connection = new SQLiteConnection($"Data Source={_databasePath};Version=3;foreign keys=true;");
                        //          _connection.Open();
                    }
                }
            }

        }

        /// <summary>
        /// Метод закрывает соединение с базой данных и освобождает ресурсы.
        /// </summary>
        public static void CloseConnection()
        {
            if (_connection != null)
            {
                lock (_lock)
                {
                    if (_connection != null)
                    {
                        _connection.Close();
                        _connection.Dispose();
                        _connection = null;
                    }
                }
            }
        }

        /// <summary>
        /// Метод позволяет изменить путь к базе данных и обновить соединение с новым путем.
        /// </summary>
        /// <param name="newDatabasePath"> Указать путь к базе данных</param>
        public static void ChangeDatabasePath(string newDatabasePath)
        {
            //_databasePath = newDatabasePath;

            //Проверяем не является ли путь сетевым
            if (newDatabasePath.StartsWith("\\"))
            {
                _databasePath = "\\\\" + newDatabasePath;
            }
            else
            {
                _databasePath = newDatabasePath;
            }

            InitializeConnection();
        }

        /// <summary>
        /// Получить данные из БД (Необходимо задать SQL запрос)
        /// </summary>
        /// <param name="query">SQL запрос для получения даных формата SELECT</param>
        /// <returns></returns>
        public DataTable GetDataTable(string query)
        {
            try
            {
                //InitializeConnection();
                _connection.Open();
                using (var cmd = new SQLiteCommand(query, _connection))
                {
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        //_connection.Close();
                        return dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выполнении запроса: " + ex.Message, "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
                throw;
            }
            finally
            {
                _connection.Close();
            }
        }


        /// <summary>
        /// Параметризированный SQL запрос с параметрами sUnit - установка, sDate - дата, Remove - признак удаленной строки
        /// </summary>
        /// <param name="query"></param>
        /// <param name="sUnit"></param>
        /// <param name="sDate"></param>
        /// <param name="Remove"></param>
        /// <returns></returns>
        public DataTable GetDataTableParam(string query, string sUnit, string sDate, string Remove)
        {
            try
            {
                //InitializeConnection();
                _connection.Open();
                using (var cmd = new SQLiteCommand(query, _connection))
                {
                    // Добавление параметров
                    cmd.Parameters.AddWithValue("@sUnit", "%" + sUnit + "%"); // Параметр для sUnit
                    cmd.Parameters.AddWithValue("@sDate", "%" + sDate + "%"); // Параметр для sDate
                    cmd.Parameters.AddWithValue("@Remove", "%" + Remove + "%"); // Параметр для sDate

                    // Просмотр строки запроса после добавления параметров
                    Console.WriteLine("SQL запрос после добавления параметров: " + cmd.CommandText);

                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        //_connection.Close();
                        return dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выполнении запроса: " + ex.Message, "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
                throw;
            }
            finally
            {
                _connection.Close();
            }
        }

        /// <summary>
        /// Получение данных из БД и общего количества строк в таблице
        /// </summary>
        /// <param name="query">  SQL запрос</param>
        /// <returns>
        /// Возвращает результат запроса в виде объекта DataTable
        /// и количество строк во всей таблице в виде int
        /// из num вернет количество строк получившихся в результате выполнения запроса
        /// </returns>
        public (DataTable, int) GetTablePagination(string query)
        {
            try
            {
                //InitializeConnection();

                int count = 0;  // Количество записей о всей таблице
                var dataTable = new DataTable();

                _connection.Open();

                using (var cmd = new SQLiteCommand(query, _connection))
                {
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        adapter.Fill(dataTable);
                    }
                }

                // получаем строку SQL запроса для подсчета количества строк 
                string queryForCountRows = SQLqueryTrim.GetWhereClauseWithoutOrderBy(query);

                using (var cmd = new SQLiteCommand(queryForCountRows, _connection))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        count = Convert.ToInt32(result);
                    }
                }
                return (dataTable, count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выполнении запроса: " + ex.Message, "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
                throw;
            }
            finally
            {
                _connection.Close();
            }
        }

        /// <summary>
        /// Асинхронная версия метода GetTablePagination
        /// </summary>
        /// <param name="query"> SQL запрос </param>
        /// <returns> Возвращает объект DataTable </returns>
        public async Task<(DataTable, int)> GetTablePaginationAsync(string query)
        {
            try
            {
                int count = 0;
                var dataTable = new DataTable();

                await _connection.OpenAsync(); // Открываем подключение асинхронно

                using (var cmd = new SQLiteCommand(query, _connection))
                {
                    using (var adapter = new SQLiteDataAdapter(cmd))
                    {
                        await Task.Run(() => adapter.Fill(dataTable)); // Заполняем DataTable асинхронно
                    }
                }

                // Получаем строку SQL запроса для подсчета количества строк
                string queryForCountRows = SQLqueryTrim.GetWhereClauseWithoutOrderBy(query);

                using (var cmd = new SQLiteCommand(queryForCountRows, _connection))
                {
                    object result = await cmd.ExecuteScalarAsync(); // Получаем количество строк асинхронно
                    if (result != null)
                    {
                        count = Convert.ToInt32(result);
                    }
                }

                return (dataTable, count);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выполнении запроса: " + ex.Message, "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
                throw;
            }
            finally
            {
                _connection.Close();
            }
        }


        /// <summary>
        /// Считываем все роли пользователей и их права
        /// </summary>
        /// <returns> ictionary </returns>
        //public Dictionary<string, List<string>> LoadRolesAndPermissionsFromDatabase()
        //{
        //    Dictionary<string, List<string>> roles = new Dictionary<string, List<string>>();

        //    {
        //        string query = @"
        //                        SELECT r.RoleName, p.PermissionName
        //                        FROM Roles r
        //                        JOIN RolePermissions rp ON r.RoleId = rp.RoleId
        //                        JOIN Permissions p ON rp.PermissionId = p.PermissionId";

        //        SQLiteCommand command = new SQLiteCommand(query, _connection);
        //        _connection.Open();

        //        try
        //        {
        //            using (SQLiteDataReader reader = command.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    string roleName = reader["RoleName"].ToString();
        //                    string permissionName = reader["PermissionName"].ToString();

        //                    if (!roles.ContainsKey(roleName))
        //                    {
        //                        roles.Add(roleName, new List<string>());
        //                    }

        //                    roles[roleName].Add(permissionName);
        //                }
        //                return roles;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show("Ошибка при выполнении операци: " + ex.Message, "Сообщение", MessageBoxButtons.OK, MessageBoxImage.Information);
        //            return null;
        //        }
        //        finally
        //        {
        //            _connection.Close();
        //        }
        //    }
        //}

        /// <summary>
        /// этот метод будет возвращать список массивов объектов, где каждый массив представляет собой одну строку результата запроса, 
        /// а элементы массива будут иметь соответствующие типы, как в базе данных.
        /// </summary>
        /// <param name="query"> SQL запрос</param>
        /// <returns>Возвращает объект типа List </returns>
        public List<object[]> GetListObject(string query)
        {

            List<object[]> queryResult = new List<object[]>();
            //List<string> queryResult = new List<string>();
            _connection.Open();
            try
            {
                // Выполнение запроса и получение результата
                SQLiteCommand command = new SQLiteCommand(query, _connection);
                SQLiteDataReader reader = command.ExecuteReader();

                // Получение метаданных о столбцах
                DataTable schemaTable = reader.GetSchemaTable();

                // Чтение результатов запроса и добавление их в список
                while (reader.Read())
                {
                    object[] values = new object[reader.FieldCount];

                    // Получение значений всех столбцов текущей строки
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        Type dataType = (Type)schemaTable.Rows[i]["DataType"];  // Получаем тип данных  текущем индексе
                        values[i] = reader.IsDBNull(i) ? null : Convert.ChangeType(reader.GetValue(i), dataType);   // Записываем данные согластно их типа
                    }

                    // Добавление значений текущей строки в список результатов
                    queryResult.Add(values);
                }
                // Закрытие SQLiteDataReader
                reader.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выполнении операци: " + ex.Message, "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
                return null;
            }
            finally
            {
                _connection.Close();
            }
            return queryResult;

        }

        /// <summary>
        /// Запись строки в БД
        /// </summary>
        /// <param name="insertQuery"> SQL запрос на вставку стрроки формата INSERT </param>
        public void InsertData(string insertQuery)
        {
            try
            {
                _connection.Open();
                using (var cmd = new SQLiteCommand(insertQuery, _connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при создании записи: " + ex.Message, "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                _connection.Close();
            }

        }

        /// <summary>
        /// Обновление данных в БД 
        /// </summary>
        /// <param name="updateQuery">SQL запрос с обновлением</param>
        public void UpdateData(string updateQuery)
        {
            try
            {
                _connection.Open();
                using (var cmd = new SQLiteCommand(updateQuery, _connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выполнении обновлении записи: " + ex.Message, "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                _connection.Close();
            }

        }

        /// <summary>
        /// Удаление данных из БД
        /// </summary>
        /// <param name="deleteQuery">SQL запрос на удаление</param>
        public void DeleteData(string deleteQuery)
        {
            try
            {
                _connection.Open();
                using (var cmd = new SQLiteCommand(deleteQuery, _connection))
                {
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выполнении удаления: " + ex.Message, "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            finally
            {
                _connection.Close();
            }
        }


        /// <summary>
        /// Выполняет запрос, который возвращает одно значение (например, запрос с функцией агрегации) и возвращеет это значение.
        /// </summary>
        /// <param name="query">SQL запрос</param>
        /// <returns></returns>
        public object ExecuteScalar(string query)
        {
            try
            {
                _connection.Open();
                using (var cmd = new SQLiteCommand(query, _connection))
                {
                    return cmd.ExecuteScalar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при выполнении запроса: " + ex.Message, "Сообщение", MessageBoxButton.OK, MessageBoxImage.Information);
                return -1;
            }
            finally
            {
                _connection.Close();
            }


        }

        /// <summary>
        /// Получение роли пользователя
        /// </summary>
        /// <param name="name">Имя пользователя</param>
        /// <returns></returns>
        public string GetRoleByName(string name)
        {
            var result = ExecuteScalar($"SELECT Role FROM Users WHERE LOWER(User) = LOWER('{name}')");
            if (result != null)
            {
                //MessageBox.Show("Имя: "+ name +" Роль: " + result.ToString());
                //Global_Var.User_Role = result.ToString();
                return result.ToString();
            }
            else
            {
                // Если пользователя с таким именем не существует
                MessageBox.Show("Пользователь '" + name + "' не имеет прав доступа! \n Обратитесь к администратору");
                return null;
            }
        }

        /// <summary>
        /// Получение id по названию установки
        /// </summary>
        /// <param name="Unit"> Назвние установки </param>
        /// <returns></returns>
        public string GetUnitIDByName(string Unit)
        {
            var result = ExecuteScalar($"SELECT id FROM Units WHERE Unit = '{Unit}'");
            if (result != null)
            {
                return result.ToString();
            }
            else
            {
                MessageBox.Show("Объект '" + Unit + "' не найден!");
                return "-1";
            }
        }

        /// <summary>
        /// Выполнение запроса транзакцией 
        /// </summary>
        /// <param name="query">SQL запрос </param>
        public void ExecuteTransaction(string query)
        {
            _connection.Open();
            using (SQLiteTransaction transaction = _connection.BeginTransaction())
            {
                // Удаляем запись из таблицы Units
                using (SQLiteCommand deleteCommand = new SQLiteCommand(query, _connection, transaction))
                {
                    try
                    {
                        deleteCommand.ExecuteNonQuery();
                        // Если всё прошло успешно, фиксируем транзакцию
                        transaction.Commit();
                    }
                    //catch (Exception ex)
                    catch (SQLiteException ex)
                    {
                        // Откат транзакции при исключении
                        transaction.Rollback();

                        string customErrorMessage;
                        string mes = ex.Message;
                        if (ex.Message == "constraint failed\r\nFOREIGN KEY constraint failed")
                        {
                            customErrorMessage = "Объект невозможно удалить, он используется в записях задач !";
                        }
                        else
                        {
                            customErrorMessage = $"Произошла ошибка: {ex.Message}";
                        }
                        //string customErrorMessage = $"Произошла ошибка при удалении записи: {ex.Message}";
                        MessageBox.Show(customErrorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

            }
            _connection.Close();
        }


        /// <summary>
        /// Проверка наличия таблицы в БД 
        /// </summary>
        /// <param name="tableName">Имя таблиый наличие которой нужно проверить</param>
        /// <returns></returns>
        public bool CheckTableName(string tableName)
        {
            // Формируем SQL-запрос для проверки наличия таблицы
            string query = $"SELECT name FROM sqlite_master WHERE type = 'table' AND name = '{tableName}';";

            // Получаем результат запроса в виде DataTable
            DataTable result = GetDataTable(query);

            // Если результат содержит хотя бы одну строку, то таблица существует
            return result.Rows.Count > 0;
        }

        /// <summary>
        /// Проверка на наличие строки с параметром VersionDB 
        /// </summary>
        /// <returns> true/ false </returns>
        public bool CheckRow(string tableName)
        {
            string query = $"Select * From {tableName} WHERE Parameter = 'VersionDB'";
            DataTable result = GetDataTable(query);
            return result.Rows.Count > 0;
        }

        /// <summary>
        /// Проверка на соответствие версии базы данных, версии приложения 
        /// </summary>
        /// <param name="verProg"> Версия программы </param>
        /// <returns> true/false </returns>
        public bool CheckVer(string tableName, string verProg)
        {
            // Формируем SQL-запрос для выбора строки, где Parameter = "Version"
            string query = $"SELECT Value FROM {tableName} WHERE Parameter = 'VersionDB'";
            
            DataTable result = GetDataTable(query);     // Получаем DataTable с результатом запроса

            // Проверяем, есть ли данные в результате
            if (result.Rows.Count > 0)
            {
                
                DataRow firstRow = result.Rows[0];                      // Берем первую строку (если Parameter уникальный, она будет единственной)
                
                string versionFromDb = firstRow["Value"].ToString();    // Получаем значение из столбца "Value"

                return verProg == versionFromDb;                        // Сравниваем значение из базы данных с verProg
            }

            return false; // Если строка с Parameter = "Version" не найдена, возвращаем false
        }

        /// <summary>
        /// Метод, который выполняет проверку наличия записи в таблице базы данных 
        /// </summary>
        /// <param name="tableName"> Имя таблицы в которой нужно выполнит проверку </param>
        /// <param name="Column"> Имя колонки в которой надо искать </param>
        /// <param name="record"> СОдержимое которое надо искать </param> 
        /// <returns> true/false </returns>
        public bool isRecordPresent(string tableName, string column, string record)
        {
            string query = $"SELECT COUNT(*) FROM {tableName} WHERE {column} = '{record}';";
            DataTable result = GetDataTable(query);
            return result.Rows.Count > 0;
        }

        /// <summary>
        /// Возвращает путь к базе данных из строки подключения 
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns> Путьк БД </returns>
        private static string GetDatabasePathFromConnectionString(string connectionString)
        {
            string[] parts = connectionString.Split(';');
            if (parts.Length > 0)
            {
                var path = parts[0].Split('=')[1];
                return path;
            }
            return null;
        }

        /// <summary>
        /// Добавление новой строки в таблицу задач 
        /// </summary>
        /// <param name="dateTask"></param>
        /// <param name="load"></param>
        /// <param name="quality"></param>
        /// <param name="routeFlow"></param>
        /// <param name="currentTask"></param>
        /// <param name="technologist"></param>
        /// <param name="unitIds"></param>
        public void AddTask(string dateTask, string load, string quality, string routeFlow, string currentTask, string technologist, List<long> unitIds)
        {
            _connection.Open();
            using (SQLiteTransaction transaction = _connection.BeginTransaction())
            {
                try
                {
                    // Выполнение вставки записи в таблицу Tasks
                    long taskId = InsertTask(dateTask, load, quality, routeFlow, currentTask);

                    // Добавление записей в таблицу TaskUnits для связывания с элементами ListView
                    foreach (long unitId in unitIds)
                    {
                        InsertTaskUnit(taskId, unitId);
                    }


                    // Новая запись технологических замечаний если пользователь с ролью технолога
                    if (AppBaseConfig.User_Role == "Technologist" || AppBaseConfig.User_Role == "Administrator" || AppBaseConfig.User_Role == "Editor")
                    {
                        InsertTechnologistComent(taskId, technologist, "Technologists");
                    }

                    // Подтвердить транзакцию
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    // В случае ошибки откатить транзакцию
                    transaction.Rollback();
                    MessageBox.Show("Произошла ошибка при добавлении задачи: " + ex.Message);
                }


            }
            _connection.Close();
        }

        public void EditTask(string dateTask, string load, string quality, string routeFlow, string currentTask, string technologist, List<long> unitIds, long selectedRowId)
        {
            _connection.Open();
            using (SQLiteTransaction transaction = _connection.BeginTransaction())
            {
                try
                {
                    if (AppBaseConfig.User_Role != "Technologist")
                    {
                        // Выполнение вставки записи в таблицу Tasks
                        UpdateTask(dateTask, load, quality, routeFlow, currentTask, selectedRowId);

                        UpdateTaskUnits(selectedRowId, unitIds);
                    }
                    else
                    {
                        EditTechnologistComent(selectedRowId, technologist, "Technologists");
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    // В случае ошибки откатить транзакцию
                    transaction.Rollback();
                    MessageBox.Show("Произошла ошибка при добавлении задачи: " + ex.Message);
                }


            }
            _connection.Close();
        }

        private void UpdateTask(string dateTask, string load, string quality, string routeFlow, string currentTask, long selectedRowId)
        {
            //ring dateUpdate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // Получить текущую дату и время
            string dateUpdate = DateTime.Now.ToString(formatDateTime, culture); // Получить текущую дату и время

            string updateQuery = "UPDATE Tasks SET DateTask = @DateTask, Load = @Load, Quality = @Quality, RouteFlow = @RouteFlow, " +
                "CurrentTask = @CurrentTask, DateUpdate =  @DateUpdate WHERE id = @SelectedRowId ";
            using (SQLiteCommand command = new SQLiteCommand(updateQuery, _connection))
            {
                command.Parameters.AddWithValue("@DateTask", dateTask);
                command.Parameters.AddWithValue("@Load", load);
                command.Parameters.AddWithValue("@Quality", quality);
                command.Parameters.AddWithValue("@RouteFlow", routeFlow);
                command.Parameters.AddWithValue("@CurrentTask", currentTask);
                command.Parameters.AddWithValue("@DateUpdate", dateUpdate);
                command.Parameters.AddWithValue("@SelectedRowId", selectedRowId);
                command.ExecuteNonQuery();
            }

        }

        private void UpdateTaskUnits(long taskId, List<long> unitIds)
        {
            // Удаляем все существующие связи для данной задачи
            string deleteQuery = "DELETE FROM TaskUnits WHERE TaskID = @TaskID;";
            using (SQLiteCommand deleteCommand = new SQLiteCommand(deleteQuery, _connection))
            {
                deleteCommand.Parameters.AddWithValue("@TaskID", taskId);
                deleteCommand.ExecuteNonQuery();
            }

            // Вставляем новые связи
            string insertQuery = "INSERT INTO TaskUnits (TaskID, UnitID) VALUES (@TaskID, @UnitID);";
            foreach (long unitId in unitIds)
            {
                using (SQLiteCommand insertCommand = new SQLiteCommand(insertQuery, _connection))
                {
                    insertCommand.Parameters.AddWithValue("@TaskID", taskId);
                    insertCommand.Parameters.AddWithValue("@UnitID", unitId);
                    insertCommand.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Добавляем запись в таблицу Tasks
        /// </summary>
        /// <param name="dateTask"></param>
        /// <param name="load"></param>
        /// <param name="quality"></param>
        /// <param name="routeFlow"></param>
        /// <param name="currentTask"></param>
        /// <param name="technologist"></param>
        /// <returns>Возвращает id дбавленной строки </returns>
        private long InsertTask(string dateTask, string load, string quality, string routeFlow, string currentTask)
        {
            //string dateCreate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"); // Получить текущую дату и время
            string dateCreate = DateTime.Now.ToString(formatDateTime, culture); // Получить текущую дату и время
            string creator = AppBaseConfig.User_Name; // Получить имя текущего пользователя


            //string insertQuery = "INSERT INTO Tasks (DateTask, Load, Quality, RouteFlow, CurrentTask, Technologist, DateCreate, Creator) " +
            //    "VALUES (@DateTask, @Load, @Quality, @RouteFlow, @CurrentTask, @Technologist, @DateCreate, @Creator);";

            string insertQuery = "INSERT INTO Tasks (DateTask, Load, Quality, RouteFlow, CurrentTask, DateCreate, Creator) " +
                "VALUES (@DateTask, @Load, @Quality, @RouteFlow, @CurrentTask, @DateCreate, @Creator);";
            using (SQLiteCommand command = new SQLiteCommand(insertQuery, _connection))
            {
                command.Parameters.AddWithValue("@DateTask", dateTask);
                command.Parameters.AddWithValue("@Load", load);
                command.Parameters.AddWithValue("@Quality", quality);
                command.Parameters.AddWithValue("@RouteFlow", routeFlow);
                command.Parameters.AddWithValue("@CurrentTask", currentTask);
                command.Parameters.AddWithValue("@DateCreate", dateCreate);
                command.Parameters.AddWithValue("@Creator", creator);
                command.ExecuteNonQuery();
            }

            return _connection.LastInsertRowId;
        }

        /// <summary>
        /// Добавление записей в таблицу TaskUnits для связывания записей Tasks и Units
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="unitId"></param>
        private void InsertTaskUnit(long taskId, long unitId)
        {
            string insertTaskUnitsQuery = "INSERT INTO TaskUnits (TaskID, UnitID) VALUES (@TaskID, @UnitID);";
            using (SQLiteCommand command = new SQLiteCommand(insertTaskUnitsQuery, _connection))
            {
                command.Parameters.AddWithValue("@TaskID", taskId);
                command.Parameters.AddWithValue("@UnitID", unitId);
                command.ExecuteNonQuery();
            }
        }


        /// <summary>
        /// Добавить коментарий в таблицу
        /// </summary>
        /// <param name="id_task"> id связанной с коментрием задачи</param>
        /// <param name="text">Текст коентария</param>
        /// <param name="TableName">Имя таблицы в которую надо добавить коментарий</param>
        public void AddComent(long id_task, string text, string TableName)
        {
            //string dateCreate = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"); // Получить текущую дату и время
            string dateCreate = DateTime.Now.ToString(formatDateTime, culture); // Получить текущую дату и время
            string author = AppBaseConfig.User_Name; // Получить имя текущего пользователя

            string insertQuery = $"INSERT INTO {TableName} (id_task, Text, Author, DateCreate) " +
                                 "VALUES (@id_task, @Text, @Author, @DateCreate);";

            _connection.Open();

            using (SQLiteCommand command = new SQLiteCommand(insertQuery, _connection))
            {
                try
                {
                    command.Parameters.AddWithValue("@id_task", id_task);
                    command.Parameters.AddWithValue("@Text", text);
                    command.Parameters.AddWithValue("@Author", author);
                    command.Parameters.AddWithValue("@DateCreate", dateCreate);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Произошла ошибка при добавлении анализа задачи: " + ex.Message);
                }

            }
            _connection.Close();
        }

        public void EditComent(long id_task, string text, string TableName)
        {
            string dateCreate = DateTime.Now.ToString(formatDateTime, culture); // Получить текущую дату и время
            string author = AppBaseConfig.User_Name; // Получить имя текущего пользователя
            string updateQuery = $"UPDATE {TableName} SET Text = @Text, Author = @Author, DateCreate = @DateCreate " +
                                 "WHERE id_task = @id_task";

            //string updateQuery = "UPDATE Tasks SET DateTask = @DateTask, Load = @Load, Quality = @Quality, RouteFlow = @RouteFlow, " +
            //    "CurrentTask = @CurrentTask, Technologist = @Technologist, DateUpdate =  @DateUpdate WHERE id = @SelectedRowId ";

            _connection.Open();
            using (SQLiteCommand command = new SQLiteCommand(updateQuery, _connection))
            {
                try
                {
                    command.Parameters.AddWithValue("@id_task", id_task);
                    command.Parameters.AddWithValue("@Text", text);
                    command.Parameters.AddWithValue("@Author", author);
                    command.Parameters.AddWithValue("@DateCreate", dateCreate);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Произошла ошибка при добавлении анализа задачи: " + ex.Message);
                }
            }
            _connection.Close();
        }


        public void InsertTechnologistComent(long id_task, string text, string TableName)
        {
            string dateCreate = DateTime.Now.ToString(formatDateTime, culture); // Получить текущую дату и время
            string author = AppBaseConfig.User_Name; // Получить имя текущего пользователя

            string insertQuery = $"INSERT INTO {TableName} (id_task, Text, Author, DateCreate) " +
                                 "VALUES (@id_task, @Text, @Author, @DateCreate);";

            using (SQLiteCommand command = new SQLiteCommand(insertQuery, _connection))
            {
                try
                {
                    command.Parameters.AddWithValue("@id_task", id_task);
                    command.Parameters.AddWithValue("@Text", text);
                    command.Parameters.AddWithValue("@Author", author);
                    command.Parameters.AddWithValue("@DateCreate", dateCreate);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Произошла ошибка при добавлении анализа задачи: " + ex.Message);
                }
            }
        }

        public void EditTechnologistComent(long id_task, string text, string TableName)
        {
            string dateCreate = DateTime.Now.ToString(formatDateTime, culture); // Получить текущую дату и время
            string author = AppBaseConfig.User_Name; // Получить имя текущего пользователя
            string updateQuery = $"UPDATE {TableName} SET Text = @Text, Author = @Author, DateCreate = @DateCreate " +
                                 "WHERE id_task = @id_task";

            using (SQLiteCommand command = new SQLiteCommand(updateQuery, _connection))
            {
                try
                {
                    command.Parameters.AddWithValue("@id_task", id_task);
                    command.Parameters.AddWithValue("@Text", text);
                    command.Parameters.AddWithValue("@Author", author);
                    command.Parameters.AddWithValue("@DateCreate", dateCreate);
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Произошла ошибка при добавлении анализа задачи: " + ex.Message);
                }
            }
        }

    }

}
