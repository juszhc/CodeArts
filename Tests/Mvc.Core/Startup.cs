using CodeArts.Db;
using CodeArts.Db.MySql;
using CodeArts.Mvc;
using CodeArts.Serialize.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace Mvc.Core
{
    /// <inheritdoc />
    public class Startup : JwtStartup
    {
        /// <inheritdoc />
        public Startup()
        {
        }
        /// <inheritdoc />
        public override void ConfigureServices(IServiceCollection services)
        {
            DbConnectionManager.RegisterAdapter(new MySqlAdapter());
            DbConnectionManager.RegisterProvider<CodeArtsProvider>();

            //services.AddGrpc();

            var x = JsonHelper.ToJson(new { x = 1 });

            ModelValidator.CustomValidate<RequiredAttribute>((attr, context) =>
            {
                return $"{context.DisplayName}Ϊ�����ֶ�!";
            });

            base.ConfigureServices(services);
        }

        /// <inheritdoc />
        public override void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            base.Configure(app.MapPost("/test", "/api/values/test"), env);

            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapGrpcService<PushService>();

            //    endpoints.MapGet("/", async context =>
            //    {
            //        await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            //    });
            //});
        }
    }
}
