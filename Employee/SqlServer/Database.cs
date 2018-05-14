using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;

namespace SqlServer
{
    public class Database : SqlServer
    {
        public string Name { get; private set; }
        protected override int Timeout { get; set; } = 300;

        public Database(string dataSource, string user, string password, string database) : base(dataSource, user, password)
        {
            Name = database;
        }

        public T[] Get<T>(string sql)
        {
            List<T> records = new List<T>();
            try {
                if (CheckConnetionMember() && !string.IsNullOrEmpty(Name)) {
                    string connectionString = CreateConnectionString(Name);
                    using (SqlConnection sqlConnection = new SqlConnection(connectionString)) {
                        sqlConnection.Open();
                        using (SqlCommand sqlCommand = new SqlCommand(sql, sqlConnection) { CommandTimeout = Timeout, }) {
                            using (SqlDataReader sqlDataReader = sqlCommand.ExecuteReader(CommandBehavior.CloseConnection)) {
                                records.AddRange(ParseReader<T>(sqlDataReader));
                                sqlDataReader.Close();
                            }
                        }
                    }
                }
            } catch　(Exception ex)  {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }

            return records.ToArray();
        }

        private T[] ParseReader<T>(SqlDataReader reader)
        {
            List<T> records = new List<T>();
            try {
                if (reader != null) {
                    while (reader.Read()) {
                        object o = (T)Activator.CreateInstance(typeof(T));
                        List<PropertyInfo> properties = new List<PropertyInfo>(o.GetType().GetProperties());
                        List<FieldInfo> fields = new List<FieldInfo>(o.GetType().GetFields());
                        for (int i = 0; i < reader.VisibleFieldCount; i++) {
                            string columnName = reader.GetName(i);
                            Type columnType = reader.GetFieldType(i);
                            object data = reader[i];
                            if (data != DBNull.Value) {
                                foreach (PropertyInfo property in properties) {
                                    if (property.Name.Equals(columnName, StringComparison.CurrentCultureIgnoreCase)) {
                                        if (property.PropertyType == columnType) {
                                            property.SetValue(o, data);
                                            properties.Remove(property);
                                            break;
                                        } else {
                                            if (property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                                                if (property.PropertyType.GetGenericArguments()[0] == columnType) {
                                                    property.SetValue(o, data);
                                                    properties.Remove(property);
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                                foreach (FieldInfo field in fields) {
                                    if (field.Name.Equals(columnName, StringComparison.CurrentCultureIgnoreCase)) {
                                        if (field.FieldType == columnType) {
                                            field.SetValue(o, data);
                                            fields.Remove(field);
                                            break;
                                        } else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(Nullable<>)) {
                                            if (field.FieldType.GetGenericArguments()[0] == columnType) {
                                                field.SetValue(o, data);
                                                fields.Remove(field);
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        records.Add((T)o);
                    }
                }
            } catch {
            }
            return records.ToArray();
        }
    }
}
