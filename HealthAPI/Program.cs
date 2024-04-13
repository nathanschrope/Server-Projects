using System.Xml.Serialization;

CommonLibrary.XML.Config config = new();

if (args.Length > 1)
    throw new ArgumentException("Invalid number of Arguments");
else if (args.Length == 1)
{
    using (var reader = new StreamReader(args[0]))
    {
        var deserializedFile = new XmlSerializer(typeof(CommonLibrary.XML.Config)).Deserialize(reader);
        if (deserializedFile != null)
            config = (CommonLibrary.XML.Config)deserializedFile;
    }
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton(typeof(CommonLibrary.StartStop.IConfig<CommonLibrary.StartStop.IApplication>), config);

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
