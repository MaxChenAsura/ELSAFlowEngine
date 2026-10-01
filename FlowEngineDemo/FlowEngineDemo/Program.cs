using FlowEngineDemo.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

builder.Services.AddCustomizations();

builder.Services.AddFlowEngineDatabase(
    builder.Configuration);

builder.Services.AddElsaFlowEngine(
    builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.RoutePrefix = "swagger";
        options.SwaggerEndpoint(
            "v1/swagger.json",
            "FlowEngineDemo API V1");
    });
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();