using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowEvents
{
    public class RoleService
    {
        private readonly string _connectionString;

        public RoleService(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void AddRole(string roleName, string description = null)
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var command = new SQLiteCommand(
                    "INSERT INTO Roles (RoleName, description) VALUES (@name, @desc)",
                    connection);

                command.Parameters.AddWithValue("@name", roleName);
                command.Parameters.AddWithValue("@desc", description ?? (object)DBNull.Value);

                command.ExecuteNonQuery();
            }
        }

        public DataTable GetAllRoles()
        {
            var table = new DataTable();
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();
                var adapter = new SQLiteDataAdapter("SELECT * FROM Roles", connection);
                adapter.Fill(table);
            }
            return table;
        }
    }
}
