using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;

namespace Employee
{
    public class SqlServer
    {
        public class StoreProcedureParameter
        {
            public string Name { get; set; }
            public object Value { get; set; }
            public Type Type { get; set; }
        }

        private string m_dataSource;
        private string m_userID;
        private string m_password;

        public string DataSource
        {
            get { return m_dataSource; }
            set { m_dataSource = value; }
        }

        public string UserID
        {
            get { return m_userID; }
        }

        public string Password
        {
            get { return m_password; }
        }


        public SqlServer(string dataSource, string userID, string password)
        {
            this.m_dataSource = dataSource;
            this.m_userID = userID;
            this.m_password = password;
        }

        public bool IsAlive
        {
            get
            {
                string error = string.Empty;
                return TestAlive(out error);
            }
        }

        public bool TestAlive(out string error)
        {
            bool b = true;

            error = string.Empty;

            /* http://www.cnblogs.com/aflyfly/archive/2011/08/10/2133546.html */
            /* http://www.cnblogs.com/goody9807/archive/2009/02/24/1397330.html */
            string connectionString = getConnectionString(this.DataSource, this.UserID, this.Password, null);
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }

            /* http://wenku.baidu.com/link?url=GkYkdffW-7C9hj1UUxkSJVn0AzhbAQzseQk3ilbh02vRcF-giJlMR9Nr95NvdGGPNJn0ZI7wBwJNfJJuia-EPGRGtMtrpbnsI9I0e0w8nVm */
            /* SQL2005使用的是动态端口.那如果用程序连接SQL20005服务器的时候,程序如何知道SQL2005 */
            /* 服务器用的是什么端口呢?原来SQL2005提供了一个SQL BROWER服务,开启这个服务后, */
            /* 就可以通过查询SQL BROWER服务 就可以知道SQL2005现在正在使用哪个端口. */
            /* 从SQL Server 2000 开始，引入了“实例”的概念，在一台服务器上可以有多个SQL Server 安装。*/
            /* 而TCP1433 或命名管道\sql\query 只能被一个连接使用，一般分配给默认实例。为了解决端口冲突，*/
            /* SQL Server 2000 引入了SSRP 协议（SQL Server Resolution Protocol，即SQL Server解析协议），*/
            /* 使用UDP1434 端口进行侦听。该侦听器用已安装的实例的名称以及实例使用的端口或命名管道来响应客户端请求。*/
            /* ConnectionTimeout 在.net 1.x 可以设置 在.net 2.0后是只读属性，则需要在连接字符串设置 */
            /* 如：server=.;uid=sa;pwd=;database=PMIS;Integrated Security=SSPI; Connection Timeout=30 */
            SqlConnection connection = new SqlConnection(connectionString);

            try
            {
                /* 打开数据库 */
                connection.Open();
            }
            catch (Exception ex)
            {
                /* 打开不成功 则连接不成功 */
                error = ex.Message;
                b = false;
            }
            finally
            {
                /* 关闭数据库连接 */
                connection.Close();
            }

            return b;
        }

        private string getConnectionString(string dataSource, string userID, string password, string dataBase)
        {
            if (string.IsNullOrEmpty(dataSource) ||
                string.IsNullOrEmpty(userID) ||
                password == null)
            {
                return null;
            }

            SqlConnectionStringBuilder scsb = new SqlConnectionStringBuilder()
            {
                DataSource = dataSource,

                /* 当设置Integrated Security为 True 的时候，连接语句前面的 UserID, */
                /* PW 是不起作用的，即采用windows身份验证模式。只有设置为 False 或 */
                /* 省略该项的时候，才按照 UserID, PW 来连接。 */
                IntegratedSecurity = false,
                UserID = userID,
                Password = password,
                InitialCatalog = dataBase ?? string.Empty,

                /* 数据库连接超时 */
                ConnectTimeout = 1,
            };

            return scsb.ConnectionString;
        }

        /* http://msdn.microsoft.com/zh-CN/library/a6t1z9x2.aspx */
        /* http://www.csharpwin.com/csharpspace/1377.shtml */
        public static string[] AvailableSQLServers
        {
            get
            {
                List<string> servers = new List<string>();

                /* Retrieve the enumerator instance and then the data. */
                SqlDataSourceEnumerator enumerator = SqlDataSourceEnumerator.Instance;
                DataTable sources = enumerator.GetDataSources();

                foreach (DataRow row in sources.Rows)
                {
                    string instanceName = row["InstanceName"] as string;
                    if (string.IsNullOrEmpty(instanceName))
                    {
                        servers.Add(row["ServerName"] as string);
                    }
                    else
                    {
                        servers.Add(string.Format("{0}\\{1}", row["ServerName"], row["InstanceName"]));
                    }
                }
                return servers.ToArray();
            }
        }

        public static SqlParameter CreateSqlParameter(string name, object data, Type type)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            string p = name;
            if (p.Length > 0 && p[0] != '@')
            {
                p = string.Format("@{0}", name);
            }

            if (data == null)
            {
                if (type == null)
                {
                    return new SqlParameter(p, DBNull.Value);
                }
                else
                {
                    /* http://stackoverflow.com/questions/2420708/operand-type-clash-nvarchar-is-incompatible-with-image */
                    /* image类型时候要插入DBNull.Value必须要显示指定SqlDbType.Image类型 */
                    if (type == typeof(byte[]) || type == typeof(MemoryStream))
                    {
                        SqlParameter sqlParameter = new SqlParameter(p, SqlDbType.Image);
                        sqlParameter.Value = DBNull.Value;
                        return sqlParameter;
                    }
                    else
                    {
                        return new SqlParameter(p, DBNull.Value);
                    }
                }
            }
            else if (data.GetType().IsGenericType && data.GetType() == typeof(Nullable<>))
            {
                Type genericType = data.GetType().GetGenericArguments()[0];

                /* http://www.2cto.com/kf/201312/267290.html */
                /* nullable类型需要特别转换 */
                /* 需要判断是否是 DateTime? 类型，如果是这种类型，需要再次格式化数据 */
                if (genericType == typeof(DateTime))
                {
                    DateTime datetime = (DateTime)Convert.ChangeType(data, Nullable.GetUnderlyingType(genericType));
                    return new SqlParameter(p, datetime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                }
                else
                {
                    return new SqlParameter(p, Convert.ChangeType(data, Nullable.GetUnderlyingType(genericType)));
                }

            }
            else if (data.GetType() == typeof(MemoryStream))
            {
                return new SqlParameter(p, (data as MemoryStream).ToArray());
            }
            else if (data.GetType() == typeof(string))
            {
                return new SqlParameter(p, data == null ? DBNull.Value : data);
            }
            else if (data.GetType() == typeof(DateTime))
            {
                /*
                 * http://bbs.csdn.net/topics/110078191
                 * http://bbs.csdn.net/topics/320085658
                 * http://bbs.csdn.net/topics/390210124
                 * 日期必须重新格式化，数据库只接受yyyy-MM-dd这样的格式
                 */
                DateTime clientDateTime = (DateTime)data;
                return new SqlParameter(p, clientDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            }
            return new SqlParameter(p, data);
        }

        /* http://blog.csdn.net/zxcred/article/details/2767347 */
        public SqlDataReader ExecuteStoreProcedure(string procedureName, StoreProcedureParameter[] parameters, string dataBase)
        {
            try
            {
                SqlConnection connection = new SqlConnection(
                    getConnectionString(this.DataSource, this.UserID, this.Password, dataBase));

                connection.Open();
                SqlCommand command = new SqlCommand(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure,
                };

                foreach (StoreProcedureParameter p in parameters)
                {
                    command.Parameters.Add(SqlServer.CreateSqlParameter(p.Name, p.Value, p.Type));
                }

                /* http://www.cnblogs.com/sophist/archive/2011/05/20/2052158.html */
                return command.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }

            return null;
        }
    }
}
