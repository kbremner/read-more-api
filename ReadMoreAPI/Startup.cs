using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ReadMoreData;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using ReadMoreAPI.Services;
using PocketLib;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;

namespace ReadMoreAPI
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", false, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddJsonFile("appsettings.local.json", true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            CurrentEnvironment = env;
        }

        private IConfiguration Configuration { get; }
        private IHostingEnvironment CurrentEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            
            services.Configure<EnvVariableSettings>(Configuration);
            services.AddSingleton(CurrentEnvironment);

            if (!CurrentEnvironment.IsDevelopment())
            {
                // Require HTTPS when not running in dev
                services.Configure<MvcOptions>(options =>
                {
                    options.Filters.Add(new RequireHttpsAttribute());
                });
            }

            services.AddScoped<IPocketService, PocketService>();

            services.AddScoped<HttpClient>();
            services.AddScoped<IHttpRequestHandler, JsonHttpRequestHandler>();
            services.AddScoped<IPocketClient>(provider => new HttpPocketClient(
                provider.GetService<IHttpRequestHandler>(),
                provider.GetService<IOptions<EnvVariableSettings>>().Value.POCKET_CONSUMER_KEY));

            // Add a factory method that creates a new postgres DB connection
            services.AddTransient<IDbConnection>(provider => new NpgsqlConnection(
                provider.GetService<IOptions<EnvVariableSettings>>().Value.NpgsqlConnectionString));
            services.AddSingleton<Func<IDbConnection>>(provider => provider.GetService<IDbConnection>);

            services.AddScoped<IPocketAccountsRepository, DbPocketAccountsRepository>();

            services.AddScoped<IXmlKeysRepository, DbXmlKeysRepository>();
            services.AddSingleton<IXmlRepository, SqlXmlRepository>();

            // Add a startup filter to handle migrations here, so that the required
            // tables are present before the KeyManager attempts to retrieve data
            // protection keys via the SqlXmlRepository
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IStartupFilter, DbMigrationsStartupFilter>());

            var sp = services.BuildServiceProvider();
            services.AddDataProtection()
                .SetApplicationName("readmore-api")
                .AddKeyManagementOptions(options => options.XmlRepository = sp.GetService<IXmlRepository>());

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // We want to redirect if the original request is HTTP instead of HTTPS.
                // However, when deployed to Heroku requests received by this app are
                // always HTTP due to SSL termination by Heroku. However, Heroku provides
                // the X-Forwarded-Proto header that provides the scheme used in the
                // original request. So, we enable support for updating the request
                // Scheme property if this header is present.
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedProto
                });

                // Redirect to HTTPS for HTTP requests
                var options = new RewriteOptions()
                    .AddRedirectToHttps();
                app.UseRewriter(options);
            }

            app.UseMvc();
        }
    }
}
