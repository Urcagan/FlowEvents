using FlowEvents.Models;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;

namespace FlowEvents
{
    public class DatabaseService 
    {
        private readonly string _connectionString;

        public DatabaseService(string connectionString)
        {
            _connectionString = $"Data Source={connectionString};Version=3;";

        }

        
        
    }
}
