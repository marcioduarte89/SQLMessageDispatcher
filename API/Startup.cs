using Amazon.SQS;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SQLMessageDispatcher.Interfaces;
using System.Reflection;

namespace API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddAWSService<IAmazonSQS>(Configuration.GetAWSOptions());
        }


        /// <summary>
        /// Configure Container will be called after running ConfigureServices
        /// Any registration here will override registrations made in ConfigureServices
        /// Don't need to build the container as its done automatically
        /// </summary>
        /// <param name="builder">Container builder</param>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            var mediatrOpenTypes = new[] {
                typeof(IHandleMessage<>),
            };

            foreach (var mediatrOpenType in mediatrOpenTypes)
            {
                builder
                   .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                   .AsClosedTypesOf(mediatrOpenType)
                   .AsImplementedInterfaces()
                   .InstancePerLifetimeScope();
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
