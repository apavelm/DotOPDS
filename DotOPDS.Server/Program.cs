using DotOPDS;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
var app = builder.Build();
app.Run();
