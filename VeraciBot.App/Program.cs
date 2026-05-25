using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.Globalization;
using System.Security.Claims;
using VeraciBot.App.Auth;
using VeraciBot.App.Components.Layout;
using VeraciBot.App.Components.Account;
using VeraciBot.App.Data;
using VeraciBot.Core.Entities;
using VeraciBot.Infrastructure.Storage;
using VeraciBot.Core.Shared;
using VeraciBot.Application.External;
using VeraciBot.Application.Services;
using VeraciBot.Core.Interfaces;
using VeraciBot.Core.Enums;

namespace VeraciBot.App
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            EncryptTool.Configure(builder.Configuration["Encryption:Key"]);

            // Add services to the container.
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();

            builder.Services.AddCascadingAuthenticationState();
            builder.Services.AddScoped<IdentityRedirectManager>();
            builder.Services.AddScoped<
                AuthenticationStateProvider,
                IdentityRevalidatingAuthenticationStateProvider
            >();

            builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedEmail = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
            })
               .AddRoles<ApplicationRole>()
               .AddEntityFrameworkStores<ApplicationDbContext>()
               .AddDefaultTokenProviders();
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });
            builder.Services.AddScoped<SignInManager<ApplicationUser>, ApplicationSignInManager>();

            var connectionString =
                builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' not found."
                );
            builder.Services.AddDbContext<ApplicationDbContext>((options) =>
            {
                options.UseSqlServer(connectionString, sqlOptions => {
                    sqlOptions.MigrationsAssembly("VeraciBot.Infrastructure");
                });
            });
            
            AddConfiguredExternalAuthentication(builder.Services, builder.Configuration, connectionString);
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddScoped<IEmailSender<ApplicationUser>, IdentitySmtpEmailSender>();

            builder.Services.AddHealthChecks();
            builder.Services.AddAuthorization();
            builder.Services.AddSingleton<IAuthorizationPolicyProvider, RoleRequirementPolicyProvider>();
            builder.Services.AddSingleton<IAuthorizationHandler, RoleRequirementHandler>();
            builder.Services.AddMemoryCache();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddHttpClient();
            builder.Services.AddScoped<IBlobStorageService, LocalBlobStorageService>();
            builder.Services.AddScoped<ApplicationSettingsService>();
            builder.Services.AddScoped<TwitterBotAuthenticationService>();
            builder.Services.AddScoped<TwitterUserAuthorizationService>();
            builder.Services.AddScoped<ThemeState>();
            builder.Services.AddScoped<TwitterAPI>();
            builder.Services.AddSingleton<TwitterMentionsRuntimeStore>();
            builder.Services.AddScoped<VeraciBotAgentTools>();
            builder.Services.AddHostedService<TwitterMentionsBackgroundWorker>();
            builder.Services.AddHostedService<TwitterMentionsProcessorWorker>();

            var app = builder.Build();


            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();
                await db.Database.ExecuteSqlRawAsync(
                    "UPDATE [AspNetUserRoles] " +
                    "SET [Discriminator] = N'IdentityUserRole<long>' " +
                    "WHERE [Discriminator] IS NULL " +
                    "OR [Discriminator] NOT IN (N'IdentityUserRole<long>', N'ApplicationUserRoles')");

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

                var roles = Enum.GetValues<EApplicationRoles>();
                foreach (var role in roles)
                {
                    var roleName = RoleName(role);
                    var normalizedRoleName = roleManager.NormalizeKey(roleName);
                    var existingRole = await roleManager.FindByNameAsync(roleName)
                        ?? await db.Roles.FirstOrDefaultAsync(x =>
                            x.Role == role || EF.Property<string>(x, "Name") == roleName);

                    if (existingRole is null)
                    {
                        var identityResult = await roleManager.CreateAsync(new ApplicationRole { Role = role });
                        ThrowIfIdentityFailed(identityResult, $"criar role {roleName}");
                    }
                    else if (existingRole.Role != role ||
                             !string.Equals(existingRole.NormalizedName, normalizedRoleName, StringComparison.Ordinal))
                    {
                        existingRole.Role = role;
                        var identityResult = await roleManager.UpdateAsync(existingRole);
                        ThrowIfIdentityFailed(identityResult, $"atualizar role {roleName}");
                    }
                }

                var adminRoleName = RoleName(EApplicationRoles.Admin);
                var normalizedAdminName = userManager.NormalizeName("admin@admin.com");
                var admin = await userManager.FindByNameAsync("admin@admin.com")
                    ?? await userManager.Users.FirstOrDefaultAsync(x => x.UserName == "admin@admin.com");

                if (admin is null)
                {
                    admin = new ApplicationUser
                    {
                        UserName = "admin@admin.com",
                        Email = "admin@admin.com",
                        EmailConfirmed = true,
                        Enabled = true
                    };

                    var identityResult = await userManager.CreateAsync(admin, "Senha123456");
                    ThrowIfIdentityFailed(identityResult, "criar usuario admin");
                }
                else
                {
                    var adminChanged = !string.Equals(admin.NormalizedUserName, normalizedAdminName, StringComparison.Ordinal);
                    if (admin.UserName != "admin@admin.com")
                    {
                        admin.UserName = "admin@admin.com";
                        adminChanged = true;
                    }

                    if (string.IsNullOrWhiteSpace(admin.Email))
                    {
                        admin.Email = "admin@admin.com";
                        adminChanged = true;
                    }

                    if (!admin.EmailConfirmed)
                    {
                        admin.EmailConfirmed = true;
                        adminChanged = true;
                    }

                    if (adminChanged)
                    {
                        var identityResult = await userManager.UpdateAsync(admin);
                        ThrowIfIdentityFailed(identityResult, "atualizar usuario admin");
                    }
                }

                if (!await userManager.IsInRoleAsync(admin, adminRoleName))
                {
                    var identityResult = await userManager.AddToRoleAsync(admin, adminRoleName);
                    ThrowIfIdentityFailed(identityResult, "atribuir role Admin ao usuario admin");
                    await userManager.UpdateSecurityStampAsync(admin);
                }

              
                await db.SaveChangesAsync();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            UseConfiguredLocalBlobStaticFiles(app);

            app.UseAntiforgery();

            MapTwitterBotAuthenticationEndpoints(app);

            app.MapGet("/Identity/Account/Login", (HttpContext context) =>
            {
                var returnUrl = context.Request.Query["ReturnUrl"].ToString();
                if (string.IsNullOrWhiteSpace(returnUrl))
                {
                    return Results.Redirect("/Account/Login");
                }

                return Results.Redirect($"/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}");
            });

            app.MapStaticAssets();
            app.MapRazorComponents<Components.App>().AddInteractiveServerRenderMode();

            // Add additional endpoints required by the Identity /Account Razor components.
            app.MapAdditionalIdentityEndpoints();

            app.Run();
        }

        private static void MapTwitterBotAuthenticationEndpoints(WebApplication app)
        {
            var adminPolicy = RolePolicies.For(EApplicationRoles.Admin);

            app.MapGet("/admin/settings/twitter/bot/start",
                async (HttpContext context, TwitterBotAuthenticationService authService) =>
                {
                    try
                    {
                        var callbackUrl = BuildAbsoluteUrl(
                            context.Request,
                            "/admin/settings/twitter/bot/callback");
                        var authorizationUrl = await authService.BeginAuthorizationAsync(callbackUrl);

                        return Results.Redirect(authorizationUrl);
                    }
                    catch (Exception ex)
                    {
                        var message = Uri.EscapeDataString(ex.Message);
                        return Results.Redirect(BuildSettingsPath(context.Request, $"?twitterBotAuth=error&message={message}"));
                    }
                })
                .RequireAuthorization(adminPolicy);

            app.MapGet("/admin/settings/twitter/bot/callback",
                async (HttpContext context, TwitterBotAuthenticationService authService) =>
                {
                    if (context.Request.Query.ContainsKey("error"))
                    {
                        var error = Uri.EscapeDataString(context.Request.Query["error"].ToString());
                        return Results.Redirect(BuildSettingsPath(context.Request, $"?twitterBotAuth=denied&message={error}"));
                    }

                    try
                    {
                        var code = context.Request.Query["code"].ToString();
                        var state = context.Request.Query["state"].ToString();
                        var callbackUrl = BuildAbsoluteUrl(
                            context.Request,
                            "/admin/settings/twitter/bot/callback");
                        var authorizedById = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                        var result = await authService.CompleteAuthorizationAsync(
                            code,
                            state,
                            callbackUrl,
                            authorizedById);
                        var username = Uri.EscapeDataString(result.Username);

                        return Results.Redirect(BuildSettingsPath(context.Request, $"?twitterBotAuth=success&twitterUser={username}"));
                    }
                    catch (Exception ex)
                    {
                        var message = Uri.EscapeDataString(ex.Message);
                        return Results.Redirect(BuildSettingsPath(context.Request, $"?twitterBotAuth=error&message={message}"));
                    }
                })
                .RequireAuthorization(adminPolicy);
        }

        private static string RoleName(EApplicationRoles role)
        {
            return ((int)role).ToString(CultureInfo.InvariantCulture);
        }

        private static void ThrowIfIdentityFailed(IdentityResult result, string operation)
        {
            if (result.Succeeded)
                return;

            var errors = string.Join("; ", result.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Falha ao {operation}: {errors}");
        }

        private static string BuildAbsoluteUrl(HttpRequest request, string path)
        {
            var pathBase = request.PathBase.HasValue ? request.PathBase.Value : string.Empty;
            return $"{request.Scheme}://{request.Host}{pathBase}{path}";
        }

        private static string BuildSettingsPath(HttpRequest request, string query)
        {
            var pathBase = request.PathBase.HasValue ? request.PathBase.Value : string.Empty;
            return $"{pathBase}/admin/settings{query}";
        }

        private static void UseConfiguredLocalBlobStaticFiles(WebApplication app)
        {
            var rootPath = LocalBlobStoragePathResolver.ResolveRootPath(app.Configuration);
            var publicPath = LocalBlobStoragePathResolver.ResolvePublicPath(app.Configuration);
            if (string.IsNullOrWhiteSpace(publicPath))
                return;

            Directory.CreateDirectory(rootPath);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(rootPath),
                RequestPath = "/" + publicPath
            });
        }

        private static void AddConfiguredExternalAuthentication(IServiceCollection services, IConfiguration configuration, string connectionString)
        {
            var twitterClientId = ResolveTwitterSetting(configuration, connectionString,
                ApplicationParameter.TWITTER_CLIENT_ID,
                "TwitterApi:ApiKey",
                "Authentication:Twitter:ClientId");
            var twitterClientSecret = ResolveTwitterSetting(configuration, connectionString,
                ApplicationParameter.TWITTER_CLIENT_SECRET,
                "TwitterApi:ApiSecret",
                "Authentication:Twitter:ClientSecret");

            if (string.IsNullOrWhiteSpace(twitterClientId) || string.IsNullOrWhiteSpace(twitterClientSecret))
                return;

            services.AddAuthentication()
                .AddTwitter("Twitter", "X / Twitter", options =>
                {
                    options.ClientId = twitterClientId;
                    options.ClientSecret = twitterClientSecret;
                    options.SaveTokens = true;
                });
        }

        private static string ResolveTwitterSetting(
            IConfiguration configuration,
            string connectionString,
            ApplicationParameter parameter,
            string appSettingsKey,
            string legacyKey
        )
        {
            var fromDb = ResolveTwitterSettingFromDatabase(connectionString, parameter);
            if (!string.IsNullOrWhiteSpace(fromDb))
                return fromDb;

            var fromAppSettings = configuration[appSettingsKey];
            if (!string.IsNullOrWhiteSpace(fromAppSettings))
                return fromAppSettings;

            return configuration[legacyKey] ?? string.Empty;
        }

        private static string ResolveTwitterSettingFromDatabase(string connectionString, ApplicationParameter parameter)
        {
            try
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseSqlServer(connectionString)
                    .Options;

                using var db = new ApplicationDbContext(options);
                var setting = db.ApplicationSettings.FirstOrDefault(x => x.Id == parameter.Value);
                return setting?.Value ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
