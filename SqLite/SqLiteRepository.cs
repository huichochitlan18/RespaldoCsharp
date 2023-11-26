using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Resplado.Models;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Resplado.SqLite;
public class SqLiteRepository : ISqLiteRepository
{
    private readonly SqliteConnection _connection;
    public SqLiteRepository(IConfiguration _configuration)
    {
        var path = _configuration.GetSection("DataBase").Get<string>();
        SqliteConnection connection = new SqliteConnection(
           new SqliteConnectionStringBuilder { DataSource = @$"{path}" }.ConnectionString);
        _connection = connection;
    }
    public void CreateTableMonthlyLog()
    {
        _connection.Open();
        try
        {
            using (var createTableCmd = _connection.CreateCommand())
            {
                createTableCmd.CommandText = "CREATE TABLE IF NOT EXISTS registro_mensual (id INTEGER PRIMARY KEY AUTOINCREMENT, created_at TEXT, path TEXT)";
                createTableCmd.ExecuteNonQuery();
            }
        }
        finally
        {
            _connection.Close();
        }
    }
    public void CreateTableWeeklyLog()
    {
        _connection.Open();
        try
        {
            using (var createTableCmd = _connection.CreateCommand())
            {
                createTableCmd.CommandText = "CREATE TABLE IF NOT EXISTS registro_semanal (id INTEGER PRIMARY KEY AUTOINCREMENT, created_at TEXT, path TEXT)";
                createTableCmd.ExecuteNonQuery();
            }
        }
        finally
        {
            _connection.Close();
        }
    }
    public void CreateTableDaily()
    {
        _connection.Open();
        try
        {
            using (var createTableCmd = _connection.CreateCommand())
            {
                createTableCmd.CommandText = "CREATE TABLE IF NOT EXISTS registro_diario (id INTEGER PRIMARY KEY AUTOINCREMENT, created_at TEXT,path TEXT)";
                createTableCmd.ExecuteNonQuery();
            }
        }
        finally
        {
            _connection.Close();
        }
    }
    public bool ExistTable()
    {
        throw new NotImplementedException();
    }
    public void Insert(string tabla, string fecha, string path)
    {
        _connection.Open();
        try
        {
            using (var transaction = _connection.CreateCommand())
            {
                transaction.CommandText = $"INSERT INTO {tabla} (created_at, path) VALUES ('{fecha}','{path}')";
                transaction.ExecuteNonQuery();
            }
        }
        finally
        {
            _connection.Close();
        }
    }
    public Registro Select(string tabla, string path)
    {
        _connection.Open();
        string fecha = ""; 
        var tableExist = _connection.CreateCommand();
        tableExist.CommandText = $"select * from {tabla} where path = '{path}' order by created_at desc limit 1";
        Registro registro = new Registro();
        using (var reader = tableExist.ExecuteReader())
        {
            while (reader.Read())
            {
                registro = new Registro()
                {
                    Id = reader.GetInt32(0),
                    Created_at  = reader.GetString(1),
                    Path = reader.GetString(2)
                };
            }
        }
        return registro;
    }
}

