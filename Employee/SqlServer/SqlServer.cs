using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;

namespace SqlServer
{
    public abstract class SqlServer
    {
        public string DataSource { get; set; }
        public string User { get; }
        public string Password { get; }
        protected virtual int Timeout { get; set; } = 30;

        public SqlServer(string dataSource, string user, string password)
        {
            DataSource = dataSource;
            User = user;
            Password = password;
        }

        public bool IsAlive => TestAlive(out string error);

        public bool TestAlive(out string error)
        {
            bool b = false;
            error = string.Empty;
            string connectionString = CreateConnectionString();
            if (!string.IsNullOrEmpty(connectionString)) {
                SqlConnection connection = new SqlConnection(connectionString);
                try {
                    connection.Open();
                    b = true;
                    connection.Close();
                } catch (Exception ex) {
                    error = ex.Message;
                }
            }
            return b;
        }

        protected string CreateConnectionString()
        {
            return (CreateConnectionString(DataSource, User, Password, null, Timeout));
        }

        protected string CreateConnectionString(string database)
        {
            return (CreateConnectionString(DataSource, User, Password, database, Timeout));
        }

        protected string CreateConnectionString(string dataSource, string User, string password, string dataBase, int timeout)
        {
            string sql = null;
            if (!string.IsNullOrEmpty(dataSource) && !string.IsNullOrEmpty(User) && password != null) {
                SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder() {
                    DataSource = dataSource,
                    IntegratedSecurity = false,
                    UserID = User,
                    Password = password,
                    InitialCatalog = dataBase ?? string.Empty,
                    ConnectTimeout = timeout,
                };
                sql = scsb.ConnectionString;
            }
            return sql;
        }

        protected bool CheckConnetionMember()
        {
            return !string.IsNullOrEmpty(DataSource) && !string.IsNullOrEmpty(User) && Password != null;
        }

        public static string[] AvailableSQLServers {
            get {
                List<string> servers = new List<string>();

                // Retrieve the enumerator instance and then the data.
                SqlDataSourceEnumerator enumerator = SqlDataSourceEnumerator.Instance;
                DataTable sources = enumerator.GetDataSources();

                foreach (DataRow row in sources.Rows) {
                    string instanceName = row["InstanceName"] as string;
                    if (string.IsNullOrEmpty(instanceName)) {
                        servers.Add(row["ServerName"] as string);
                    } else {
                        servers.Add(string.Format("{0}\\{1}", row["ServerName"], row["InstanceName"]));
                    }
                }
                return servers.ToArray();
            }
        }

        public static SqlParameter CreateSqlParameter(string name, object data, Type type)
        {
            if (string.IsNullOrEmpty(name)) {
                return null;
            }
            string p = name;
            if (p.Length > 0 && p[0] != '@') {
                p = string.Format("@{0}", name);
            }
            if (data == null) {
                if (type == null) {
                    return new SqlParameter(p, DBNull.Value);
                } else {
                    if (type == typeof(byte[]) || type == typeof(MemoryStream)) {
                        SqlParameter sqlParameter = new SqlParameter(p, SqlDbType.Image);
                        sqlParameter.Value = DBNull.Value;
                        return sqlParameter;
                    } else {
                        return new SqlParameter(p, DBNull.Value);
                    }
                }
            } else if (data.GetType().IsGenericType && data.GetType() == typeof(Nullable<>)) {
                Type arg0 = data.GetType().GetGenericArguments()[0];
                if (arg0 == typeof(DateTime)) {
                    DateTime datetime = (DateTime)Convert.ChangeType(data, Nullable.GetUnderlyingType(arg0));
                    return new SqlParameter(p, datetime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                } else {
                    return new SqlParameter(p, Convert.ChangeType(data, Nullable.GetUnderlyingType(arg0)));
                }

            } else if (data.GetType() == typeof(MemoryStream)) {
                return new SqlParameter(p, (data as MemoryStream).ToArray());
            } else if (data.GetType() == typeof(string)) {
                return new SqlParameter(p, data == null ? DBNull.Value : data);
            } else if (data.GetType() == typeof(DateTime)) {
                DateTime clientDateTime = (DateTime)data;
                return new SqlParameter(p, clientDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
            return new SqlParameter(p, data);
        }

        public SqlDataReader ExecuteStoreProcedure(string procedureName, StoreProcedureParameter[] parameters, string dataBase)
        {
            try {
                SqlConnection connection = new SqlConnection(
                    CreateConnectionString(DataSource, User, Password, dataBase, Timeout));

                connection.Open();
                SqlCommand command = new SqlCommand(procedureName, connection) {
                    CommandType = CommandType.StoredProcedure,
                };

                foreach (StoreProcedureParameter p in parameters) {
                    command.Parameters.Add(CreateSqlParameter(p.Name, p.Value, p.Type));
                }

                return command.ExecuteReader(CommandBehavior.CloseConnection);
            } catch (Exception ex) {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
            return null;
        }
    }

    public class StoreProcedureParameter
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public Type Type { get; set; }
    }
}
