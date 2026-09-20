using GreenCrescent.Application.Common;
using GreenCrescent.Application.Features.Beneficiaries;
using GreenCrescent.Application.Features.FinancialEntries;
using GreenCrescent.Application.Features.Reports;
using GreenCrescent.Application.Features.ResponsibleSheikhs;
using GreenCrescent.Application.Features.Sponsors;
using GreenCrescent.Application.Features.SponsorshipActions;
using GreenCrescent.Application.Features.Sponsorships;
using GreenCrescent.Infrastructure.Identity;
using GreenCrescent.Infrastructure.Services;
using GreenCrescent.Web.Components;
using GreenCrescent.Web.Components.Account;
using GreenCrescent.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using GreenCrescent.Application.Features.OrphanApplications;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto;

    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddScoped<
    IOrphanApplicationService,
    OrphanApplicationService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddDataProtection()
    .SetApplicationName("GreenCrescent")
    .PersistKeysToDbContext<ApplicationDbContext>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme =
        IdentityConstants.ApplicationScheme;

    options.DefaultSignInScheme =
        IdentityConstants.ExternalScheme;
})
    .AddIdentityCookies();

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.Stores.SchemaVersion =
            IdentitySchemaVersions.Version3;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "GreenCrescent.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;
    options.Cookie.SameSite =
        SameSiteMode.Lax;

    options.SlidingExpiration = true;
    options.ExpireTimeSpan =
        TimeSpan.FromHours(8);

    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath =
        "/Account/AccessDenied";
});

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
builder.Services.AddScoped<ISponsorService, SponsorService>();
builder.Services.AddScoped<
    IBeneficiaryService,
    BeneficiaryService>();
builder.Services.AddScoped<
    ISponsorshipService,
    SponsorshipService>();
builder.Services.AddScoped<
    IFinancialEntryService,
    FinancialEntryService>();

builder.Services.AddScoped<
    ICurrentUserService,
    CurrentUserService>();

builder.Services.AddScoped<
    ISponsorshipActionService,
    SponsorshipActionService>();

builder.Services.AddScoped<
    IReportService,
    ReportService>();

builder.Services.AddSingleton<
    IExcelExportService,
    ExcelExportService>();

builder.Services.AddScoped<
    IResponsibleSheikhService,
    ResponsibleSheikhService>();

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseForwardedHeaders();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapHealthChecks("/health");
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

await IdentitySeeder.SeedAsync(
    app.Services,
    app.Configuration);

app.Run();
