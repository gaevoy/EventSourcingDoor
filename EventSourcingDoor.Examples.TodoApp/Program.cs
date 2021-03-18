using System;
using EventSourcingDoor.NEventStore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NEventStore;
using NEventStore.Persistence.Sql.SqlDialects;
using NEventStore.Serialization.Json;
using Npgsql;

namespace EventSourcingDoor.Examples.TodoApp
{
    public class Program
    {
        public static void Main(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(web => { web.UseStartup<Startup>(); })
                .Build()
                .Run();
    }

    public class Startup
    {
        public IConfiguration Configuration { get; }
        private string ConnectionString => Configuration["ConnectionStrings:PostgreSqlConnection"];

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHostedService<OutboxListener>();
            services.AddSingleton<IStoreEvents>(_ =>
                Wireup.Init()
                    .UsingSqlPersistence(NpgsqlFactory.Instance, ConnectionString)
                    .WithDialect(new PostgreSqlDialect())
                    .UsingJsonSerialization()
                    .Build());
            services.AddSingleton<IOutbox>(container =>
                new NEventStoreOutbox(container.GetService<IStoreEvents>(), TimeSpan.FromSeconds(1)));
            services.AddDbContext<TodoDbContext>(options => options.UseNpgsql(ConnectionString));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
            using var scope = app.ApplicationServices.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<TodoDbContext>();
            dbContext.Database.EnsureCreated();
            scope.ServiceProvider.GetRequiredService<IStoreEvents>().Advanced.Initialize();
        }
    }
}