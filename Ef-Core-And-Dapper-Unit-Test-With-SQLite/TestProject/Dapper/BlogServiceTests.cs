using System.Data;
using System.Linq;
using BusinessLogic;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestProject.Dapper
{
    [TestClass]
    public class BlogServiceTests
    {
        [TestMethod]
        public void Add_writes_to_database_with_ef_core_and_query_with_dapper()
        {
            // In-memory database only exists while the connection is open
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            try
            {
                var options = new DbContextOptionsBuilder<BloggingContext>()
                    .UseSqlite(connection)
                    .UseLoggerFactory(GetLoggerFactory())
                    .Options;

                // Create the schema in the database
                using var context = new BloggingContext(options);
                context.Database.EnsureCreated();

                IDbConnection dbConnection = context.Database.GetDbConnection();

                context.Add(new Blog
                {
                    BlogId = 1,
                    Url = "http://sample.com"
                });

                context.SaveChanges();

                var url = dbConnection
                    .Query<Blog>("select * from Blogs where BlogId = @Id", new { Id = 1 })
                    .Select(x => x.Url)
                    .Single();

                Assert.AreEqual("http://sample.com", url);
            }
            finally
            {
                connection.Close();
            }
        }

        private static ILoggerFactory GetLoggerFactory()
        {
            return new ServiceCollection()
                .AddLogging(builder =>
                   builder.AddDebug()
                          .AddFilter(DbLoggerCategory.Database.Command.Name,
                                     LogLevel.Debug))
                    .BuildServiceProvider()
                    .GetService<ILoggerFactory>();
        }
    }
}
