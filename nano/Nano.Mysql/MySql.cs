using System;
using System.Collections.Generic;
using System.Text;
using MySql.Data.MySqlClient;

namespace Nano.Data.MySql
{
	public static class MySqlKit
	{
		public static MySqlConnection Connect(string host, uint port, string user, string pass, string dbname)
		{
			MySqlConnectionStringBuilder connBuilder = new MySqlConnectionStringBuilder();
			connBuilder.Server = host;
			connBuilder.Port = port;
			connBuilder.UserID = user;
			connBuilder.Password = pass;
			connBuilder.Database = dbname;
			connBuilder.CharacterSet = "utf8";

			MySqlConnection connection = new MySqlConnection(connBuilder.ConnectionString);
			connection.Open();

			return connection;
		}

		public static int ExecuteCommand(MySqlConnection con, string sql)
		{
			MySqlCommand cmd = new MySqlCommand(sql, con);
			return cmd.ExecuteNonQuery();
		}

		public static MySqlDataReader ExecuteReader(MySqlConnection con, string sql)
		{
			MySqlCommand cmd = new MySqlCommand(sql, con);
			return cmd.ExecuteReader();
		}

		public static object ExecuteSingleValue(MySqlConnection con, string sql)
		{
			MySqlCommand cmd = new MySqlCommand(sql, con);
			return cmd.ExecuteScalar();
		}
	}

	public interface IMysqlConnectionFactory : IDisposable
	{
		MySqlConnection Retrieve();
		void Return(MySqlConnection conn);
	}

	public class SingletonMysqlConnectionFactory : IMysqlConnectionFactory
	{
		MySqlConnection m_conn;

		public SingletonMysqlConnectionFactory(MySqlConnection conn)
		{
			m_conn = conn;
		}

		public MySqlConnection Retrieve()
		{
			return m_conn;
		}

		public void Return(MySqlConnection conn)
		{
		}

		public void Dispose()
		{
			if (m_conn != null)
			{
				m_conn.Close();
				m_conn = null;
			}
		}
	}
}
