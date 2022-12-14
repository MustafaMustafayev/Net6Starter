using API.ActionFilters;
using API.Containers;
using API.Graphql.Role;
using API.Hubs;
using API.Middlewares;
using API.Services;
using BLL.Mappers;
using BLL.MediatR;
using CORE.Config;
using CORE.Constants;
using DAL.DatabaseContext;
using DTO.Auth.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using GraphQL.Server.Ui.Voyager;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterNLogger();

var config = new ConfigSettings();

builder.Configuration.GetSection("Config").Bind(config);

builder.Services.AddSingleton(config);

builder.Services.AddControllers(opt => opt.Filters.Add(typeof(ModelValidatorActionFilter)));

builder.Services.AddFluentValidationAutoValidation()
    .AddValidatorsFromAssemblyContaining<LoginDtoValidator>();

if (config.SentrySettings.IsEnabled) builder.WebHost.UseSentry();

builder.Services.AddAutoMapper(Automapper.GetAutoMapperProfilesFromAllAssemblies().ToArray());

builder.Services.AddDbContext<DataContext>(options =>
    options.UseNpgsql(config.ConnectionStrings.AppDb));

builder.Services.AddHttpContextAccessor();

builder.Services.RegisterHttpClients(config);

if (config.RedisSettings.IsEnabled)
{
    builder.Services.AddHostedService<RedisIndexCreatorService>();
    builder.Services.RegisterRedis(config);
}

builder.Services.RegisterRepositories();
builder.Services.RegisterAntiForgeryToken();
builder.Services.RegisterUnitOfWork();

builder.Services.AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddProjections()
    .AddSorting()
    .AddFiltering();

builder.Services.AddMediatR(typeof(MediatrAssemblyContainer).Assembly);

builder.Services.AddHealthChecks();

builder.Services.RegisterAuthentication(config);

builder.Services.AddCors(o =>
    o.AddPolicy(Constants.EnableAllCorsName,
        b => b.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin()));

builder.Services.AddScoped<LogActionFilter>();

builder.Services.AddScoped<ModelValidatorActionFilter>();

builder.Services.AddEndpointsApiExplorer();

if (config.SwaggerSettings.IsEnabled) builder.Services.RegisterSwagger(config);

builder.Services.RegisterMiniProfiler();

builder.Services.AddSignalR();

var app = builder.Build();

// if (app.Environment.IsDevelopment())

if (config.SwaggerSettings.IsEnabled) app.UseSwagger();

if (config.SwaggerSettings.IsEnabled)
    app.UseSwaggerUI(c => c.InjectStylesheet($"/swagger_ui/{config.SwaggerSettings.Theme}.css"));

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

app.UseCors(Constants.EnableAllCorsName);

app.UseMiddleware<LocalizationMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

// anti forgery token implementation
app.UseMiddleware<AntiForgeryTokenValidator>();

//app.UseMiddleware<ValidateBlackListMiddleware>();

app.UseHttpsRedirection();

app.Use((context, next) =>
{
    context.Request.EnableBuffering();
    return next();
});

app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("Content-Security-Policy", "default-src 'self'");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("X-Frame-Options", "Deny");
    context.Response.Headers.Add("Referrer-Policy", "no-referrer");
    await next.Invoke();
});

if (config.SentrySettings.IsEnabled) app.UseSentryTracing();

app.UseStaticFiles();

app.UseAuthorization();

app.UseAuthentication();

// app.UseMiniProfiler();

app.UseHealthChecks("/hc");

app.MapControllers();

app.MapHub<UserHub>("/userHub");

app.MapGraphQL((PathString)"/graphql");

app.UseGraphQLVoyager("/graphql-voyager", new VoyagerOptions
{
    GraphQLEndPoint = "/graphql"
});

app.Run();