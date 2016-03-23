using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Internal;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Mvc.Controllers;
using Microsoft.AspNet.Mvc.ViewComponents;
using Microsoft.Data.Entity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SimpleInjector;
using SimpleInjector.Integration.AspNet;
using SimpleInjectorInit.Interfaces;
using SimpleInjectorInit.Models;
using SimpleInjectorInit.Repository;
using SimpleInjectorInit.Services;

namespace SimpleInjectorInit
{
    public class Startup
    {
        private Container container = new Container();

        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddEntityFramework()
                .AddSqlServer()
                .AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(Configuration["Data:DefaultConnection:ConnectionString"]));

            services.AddIdentity<ApplicationUser, IdentityRole>(cfg =>
            {
                cfg.Password.RequireDigit = false;
                cfg.Password.RequireNonLetterOrDigit = false;
                cfg.Password.RequiredLength = 6;
                cfg.Password.RequireUppercase = false;
                cfg.Password.RequireLowercase = false;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddMvc();

            services.AddInstance<IControllerActivator>(
                new SimpleInjectorControllerActivator(container));

            services.AddInstance<IViewComponentInvokerFactory>(
                new SimpleInjectorViewComponentInvokerFactory(container));

            // Work around for a Identity Framework bug inside the SignInManager<T> class.
            services.Add(ServiceDescriptor.Instance<IHttpContextAccessor>(
                new NeverNullHttpContextAccessor()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            container.Options.DefaultScopedLifestyle = new AspNetRequestLifestyle();

            app.UseSimpleInjectorAspNetRequestScoping(container);

            InitializeContainer(app);

            container.RegisterAspNetControllers(app);
            container.RegisterAspNetViewComponents(app);

            container.Verify();

            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");

                // For more details on creating database during deployment see http://go.microsoft.com/fwlink/?LinkID=615859
                try
                {
                    using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>()
                        .CreateScope())
                    {
                        serviceScope.ServiceProvider.GetService<ApplicationDbContext>()
                             .Database.Migrate();
                    }
                }
                catch { }
            }

            app.UseIISPlatformHandler(options => options.AuthenticationDescriptions.Clear());

            app.UseStaticFiles();

            app.UseIdentity();

            // To configure external authentication please see http://go.microsoft.com/fwlink/?LinkID=532715

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private void InitializeContainer(IApplicationBuilder app)
        {
            container.Register<IEmailSender, AuthMessageSender>();
            container.Register<ISmsSender, AuthMessageSender>();

            // Add application services. For instance:
            container.Register<IUserRepository, SqlUserRepository>(Lifestyle.Scoped);

            // Cross-wire ASP.NET services (if any). For instance:
            container.CrossWire<UserManager<ApplicationUser>>(app);
            container.CrossWire<SignInManager<ApplicationUser>>(app);
            container.CrossWire<ILoggerFactory>(app);
        }

        // Entry point for the application.
        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }

    public class NeverNullHttpContextAccessor : IHttpContextAccessor
    {
        AsyncLocal<HttpContext> context = new AsyncLocal<HttpContext>();

        public HttpContext HttpContext
        {
            get { return this.context.Value ?? new DefaultHttpContext(); }
            set { this.context.Value = value; }
        }
    }
}
