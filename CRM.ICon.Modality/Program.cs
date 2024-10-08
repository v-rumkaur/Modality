using CRM.ICon.Modality;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<AuthTokenClient>();

builder.Services.AddHttpClient<IVDMService, VDMService>().ConfigureServiceAuthHandler<VDMConfiguration>((options) => new ServiceAuthHandlerParams
{
    Resource = options.Resource,
    TenantId = options.TenantId,
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
