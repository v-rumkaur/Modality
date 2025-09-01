using CRM.ICon.Modality;
using CRM.ICon.Modality.Services.VDM;
using Microsoft.Extensions.Options;

public class Program
{
    /// <summary>
    /// Entry method
    /// </summary>
    /// <param name="args">Input arguments</param>
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    /// <summary>
    /// CreateHostBuilder
    /// </summary>
    /// <param name="args">args</param>
    /// <returns>Webhost builder</returns>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
       
        return Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
    }
}
