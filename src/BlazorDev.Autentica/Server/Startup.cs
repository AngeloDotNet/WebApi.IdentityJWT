using BlazorDev.Autentica.Server.Models.Services.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Text;

namespace BlazorDev.Autentica.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddCors(options =>
            {
                options.AddPolicy("MyPolicy", builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });

            services.AddDbContextPool<ApplicationDbContext>(optionsBuilder => {
                string connectionString = Configuration.GetSection("ConnectionStrings").GetValue<string>("Default");
                optionsBuilder.UseSqlite(connectionString, options =>
                {
                    // Abilito il connection resiliency (tuttavia non è supportato dal provider di Sqlite perché non è soggetto a errori transienti)
                    // Info su: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency
                    // options.EnableRetryOnFailure(3);
                });
            });

            //* * * * * SOLUZIONE 1 * * * * *
            //services.AddIdentity<IdentityUser, IdentityRole>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>()
            //    .AddDefaultTokenProviders();

            //* * * * * SOLUZIONE 2 * * * * *
            //services.AddDefaultIdentity<IdentityUser>()
            //    .AddRoles<IdentityRole>()
            //    .AddEntityFrameworkStores<ApplicationDbContext>()
            //    .AddDefaultTokenProviders();

            //* * * * * SOLUZIONE 3 * * * * *
            //services.AddIdentity<IdentityUser, IdentityRole>(options =>
            //{
            //    options.User.RequireUniqueEmail = true;
            //    options.Password.RequiredLength = 6;
            //    options.Password.RequireNonAlphanumeric = true;
            //    options.Password.RequireDigit = true;
            //    options.Password.RequireLowercase = true;
            //    options.Password.RequireUppercase = true;
            //})
            //.AddEntityFrameworkStores<ApplicationDbContext>()
            //.AddDefaultTokenProviders();

            //* * * * * SOLUZIONE 4 * * * * *
            var identityBuilder = services.AddDefaultIdentity<IdentityUser>(options => {

                // Criteri di validazione della password
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredUniqueChars = 4;

                // Conferma dell'account
                options.SignIn.RequireConfirmedAccount = false;

                // Blocco dell'account
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            });

            identityBuilder.AddRoles<IdentityRole>();
            identityBuilder.AddEntityFrameworkStores<ApplicationDbContext>();
            identityBuilder.AddDefaultTokenProviders();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
                {
                    options.SaveToken = true;
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = true,
                        ValidIssuer = Configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = Configuration["Jwt:Audience"],
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Jwt:SecurityKey"])),
                        RequireExpirationTime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });

            services.AddSwaggerGen(swagger =>
            {
                //This is to generate the Default UI of Swagger Documentation    
                swagger.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "ASP.NET 5 Web API",
                    Description = "Authentication and Authorization in ASP.NET 5 with JWT and Swagger"
                });

                // To Enable authorization using Swagger (JWT)    
                swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter 'Bearer' [space] and then your valid token in the text input below."
                    //Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\"",
                });

                swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();

                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ASP.NET 5 Web API v1"));
            }
            //else
            //{
            //    app.UseExceptionHandler("/Error");
            //    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            //    app.UseHsts();
            //}

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseCors("MyPolicy");

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(routeBuilder =>
            {
                routeBuilder.MapRazorPages();
                routeBuilder.MapControllers();
                routeBuilder.MapFallbackToFile("index.html");
            });
        }
    }
}