using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkyBuilding;
using SkyBuilding.Cache;
using SkyBuilding.Config;
using SkyBuilding.Log;
using SkyBuilding.Mvc;
using SkyBuilding.Mvc.Cache;
using SkyBuilding.Mvc.Config;
using SkyBuilding.Mvc.Converters;
using SkyBuilding.Mvc.Log;
using SkyBuilding.Mvc.Serialize.Json;
using SkyBuilding.Serialize.Json;
using Swashbuckle.AspNetCore.Swagger;

namespace Mvc.Core
{
    //public class Startup
    //{
    //    public Startup(IConfiguration configuration)
    //    {
    //        Configuration = configuration;
    //    }

    //    public IConfiguration Configuration { get; }

    //    // This method gets called by the runtime. Use this method to add services to the container.
    //    public void ConfigureServices(IServiceCollection services)
    //    {
    //        services.AddControllers();
    //    }

    //    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    //    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    //    {
    //        if (env.IsDevelopment())
    //        {
    //            app.UseDeveloperExceptionPage();
    //        }

    //        app.UseRouting();

    //        app.UseAuthorization();

    //        app.UseEndpoints(endpoints =>
    //        {
    //            endpoints.MapControllers();
    //        });
    //    }
    //}

    /// <inheritdoc />
    public class Startup
    {
        /// <inheritdoc />
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("Allow",
                    builder =>
                    {
                        builder.SetIsOriginAllowed(origin => true)
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials();
                    });
            });
            //? MVC
            services
                .AddMvc(options =>
                {
                    //�Զ����쳣����
                    options.Filters.Add<DExceptionFilter>();
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new SkyJsonConverter());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            RuntimeServManager.TryAddSingleton<IJsonHelper, DefaultJsonHelper>();
            RuntimeServManager.TryAddSingleton<IConfigHelper, DefaultConfigHelper>();

            //? ��־����
            LogManager.AddAdapter(new Log4NetAdapter());

            //? �������
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.First);
            CacheManager.TryAddProvider(new RuntimeCacheProvider(), CacheLevel.Second);

            //����XML�ĵ�����
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "API�ӿ�", Version = "v3" });
                var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    c.IncludeXmlComments(file);
                }
            });

            services.AddAutofac(container => IocRegisters(container));
        }
        /// <inheritdoc />
        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            //app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }

        /// <summary>
        /// ����ע��
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        private static IContainer IocRegisters(ContainerBuilder builder)
        {
            var path = AppDomain.CurrentDomain.RelativeSearchPath;
            if (!Directory.Exists(path))
                path = AppDomain.CurrentDomain.BaseDirectory;

            var assemblys =
                Directory.GetFiles(path, "*.dll")
                    .Select(Assembly.LoadFrom)
                    .ToArray();

            builder.RegisterAssemblyTypes(assemblys)
                .Where(type => !type.IsAbstract)
                .AsSelf() //�����������û�нӿڵ���
                .AsImplementedInterfaces() //�ӿڷ���
                .PropertiesAutowired()//����ע��
                .InstancePerLifetimeScope(); //��֤�������ڻ�������

            builder.RegisterAssemblyTypes(assemblys)
                .Where(type => !type.IsAbstract)
                .AsSelf() //�����������û�нӿڵ���
                .AsImplementedInterfaces() //�ӿڷ���
                .PropertiesAutowired(); //����ע��

            builder.RegisterAssemblyTypes(assemblys)
                .Where(type => !type.IsAbstract)
                .AsSelf() //�����������û�нӿڵ���
                .AsImplementedInterfaces() //�ӿڷ���
                .PropertiesAutowired()//����ע��
                .SingleInstance(); //��֤����ע��

            return builder.Build();
        }
    }
}
