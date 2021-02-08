using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Logging;

using Server.Controllers;
using Server.Data;
using Server.Services;

namespace Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            this.Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        public IHttpContextAccessor HttpContextAccessor;

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            IdentityModelEventSource.ShowPII = true;
            services.AddRazorPages();
            services.AddServerSideBlazor();

            services.AddSingleton<IAuthManager>(new AuthManager(Configuration));
            services.AddTransient<ISqlDataAccess, SqlDataAccess>();
            services.AddTransient<IUserClaims, UserClaims>();

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(option => 
            {
                
                option.SaveToken = true;
                option.TokenValidationParameters = new TokenValidationParameters {
                        
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = Configuration["Jwt:Issuer"],
                    ValidAudience = Configuration["Jwt:Issuer"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
                        
                };
        
            });

            services.AddCors(options =>
            {
                options.AddPolicy(name: "Access-Control-Allow-Origin", builder => builder.WithOrigins("http://localhost:5000"));
                options.AddPolicy(name: "Access-Control-Allow-Credentials", builder => builder.AllowCredentials());
                options.AddPolicy(name: "Access-Control-Allow-Methods", builder => builder.AllowAnyMethod());
                options.AddPolicy(name: "Access-Control-Allow-Headers", builder => builder.AllowAnyHeader());

                options.AddPolicy(name: "Access-Control-Allow-Headers", 
                    builder => builder.WithHeaders("X-Requested-With, Content-Type, Content-Length, Accept, Origin, Authorization"));
                
            });
            
            services.AddHttpContextAccessor();
            services.AddAuthorization(options => {

                options.AddPolicy("AdminRolePolicy",
                    policy => policy.RequireRole("admin"));

                options.AddPolicy("OrdinaryRolePolicy",
                    policy => policy.RequireRole("ordinary"));

            });

            services.AddControllers()
            .AddNewtonsoftJson(
                options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
            )
            .AddNewtonsoftJson(
                options => options.SerializerSettings.DateParseHandling = Newtonsoft.Json.DateParseHandling.DateTime
            );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBlazorHub();
                endpoints.MapDefaultControllerRoute();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}