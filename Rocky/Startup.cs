using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rocky.Data;
using Rocky.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rocky
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
            // section 2 - 4
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                        Configuration.GetConnectionString("DefaultConnection")
                    )
            );

            // adding session for the shopping cart - section 5-8
            services.AddHttpContextAccessor();
            services.AddSession(Options =>
            {
                Options.IdleTimeout = TimeSpan.FromMinutes(10); // let the session lasts 10 minutes
                Options.Cookie.HttpOnly = true;
                Options.Cookie.IsEssential = true;
            });
            // and then, below this METHOD, activate the session with app.UseSession();

            // Identity activation (section 6-1)
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddDefaultTokenProviders().AddDefaultUI()
                .AddEntityFrameworkStores<ApplicationDbContext>(); // this line will make change to our database tables, will add Identity tables
            // below the METHOD, active the Authentication

            // E-mail sender registration 
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddControllersWithViews();
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
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication(); // activation of Identity, make sure that this line will come before Authorization!!!

            app.UseAuthorization();

            app.UseSession(); // session 

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages(); // Because Identity uses Razor pages instead of MVC 
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
