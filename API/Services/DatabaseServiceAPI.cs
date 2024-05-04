using API.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using System.Reflection.PortableExecutable;

namespace KoleoPL.Services
{
    public class DatabaseServiceAPI : IDatabaseServiceAPI
    {
        public IConfiguration Configuration { get; }
        public DatabaseServiceAPI(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        public async Task<(List<string[]>, bool)> ExecuteSQL(string sql) // moze tu dodac wyjatki zeby sie zwracal bool tez?
        {
            var results = new List<string[]>();

            await using var conn = new SqliteConnection(Configuration.GetConnectionString("DefaultConnection"));

            await conn.OpenAsync();

            await using var cmd = new SqliteCommand(sql, conn);

            await using var dataReader = await cmd.ExecuteReaderAsync();

            while(await dataReader.ReadAsync())
            {
                //Console.WriteLine(dataReader["Name"]);
                //Console.WriteLine(dataReader["Surname"]);

                var values = new string[dataReader.FieldCount];
                dataReader.GetValues(values);
                results.Add(values);
            }
              
            return (results, true); // TO DO: change true
        }

        public async Task<(List<object[]>, bool)> ExecuteSQLLastRow(string sql) // moze tu dodac wyjatki zeby sie zwracal bool tez?
        {
            var results = new List<object[]>();

            await using var conn = new SqliteConnection(Configuration.GetConnectionString("DefaultConnection"));

            await conn.OpenAsync();

            await using var cmd = new SqliteCommand(sql, conn);

            await using var dataReader = await cmd.ExecuteReaderAsync();

            while (await dataReader.ReadAsync())
            {
                //Console.WriteLine(dataReader["Name"]);
                //Console.WriteLine(dataReader["Surname"]);

                var values = new object[dataReader.FieldCount];
                dataReader.GetValues(values);
                results.Add(values);
            }

            return (results, true); // TO DO: change true
        }

        //public async Task<(List<string[]>, bool)> ExecuteSQLNonQuery(string sql) // moze tu dodac wyjatki zeby sie zwracal bool tez?
        //{
        //    await using var conn = new SqliteConnection(Configuration.GetConnectionString("DefaultConnection"));

        //    await conn.OpenAsync();

        //    await using var cmd = new SqliteCommand(sql, conn);

        //    await cmd.ExecuteNonQueryAsync();

        //    return (new List<string[]>(), true); // TO DO: change true
        //}
        public async void Backup()
        {
            string backupDb = "KoleoBackup.db";

            var backup = new SqliteConnection($"Data Source={backupDb}");

            await using var conn = new SqliteConnection(Configuration.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            conn.BackupDatabase(backup);
        }
    }
}