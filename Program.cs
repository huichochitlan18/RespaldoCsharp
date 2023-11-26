
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Resplado.Models;
using Resplado.SqLite;
using Resplado.zip;

namespace Resplado;
public class Program
{
    private static IConfiguration? _configuration;
    static async Task Main(string[] args)
    {
        _configuration = LoadConfiguration();
        //await Simulacion(new DateTime(2020, 1, 1, 0, 0, 0));
        await Proceso(DateTime.Now);
    }
     private static IConfiguration LoadConfiguration()
    {
        return new ConfigurationBuilder()
            //.SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();
    }
    public static async Task Proceso(DateTime fechaProceso)
    {
        var rutas = _configuration.GetSection("rutas").Get<List<Ruta>>();
        var sqLite = new SqLiteRepository(_configuration);

        sqLite.CreateTableMonthlyLog();
        sqLite.CreateTableWeeklyLog();
        sqLite.CreateTableDaily();

        DateTime fecha = new DateTime(fechaProceso.Year, fechaProceso.Month, fechaProceso.Day, 0, 0, 0);

        var hoy = new DateTime(fecha.Year, fecha.Month, fecha.Day);

        foreach (var ruta in rutas)
        {
            ruta.Destino = CleanPath(ruta.Destino);
            ruta.Origen  = CleanPath(ruta.Origen);

            if (ruta.Tipo == 2)
            {
                await HandleMonthlyRuta(sqLite, ruta, fecha, hoy);
            }

            if (ruta.Tipo == 1)
            {
                await HandleDailyRuta(sqLite, ruta, fecha, hoy);
            }
        }
    }
    private static DateTime TruncateTime(DateTime date)
    {
        return new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
    }
    public static async Task Simulacion(DateTime fechaProceso)
    {
        DateTime fecha = new DateTime(fechaProceso.Year, fechaProceso.Month, fechaProceso.Day, 0, 0, 0);
        for (int i = 0; i < 100000; i++)
        {
            await Proceso(fecha);
            fecha = fecha.AddHours(1);
        }
    }
    public static async Task Respaldo(Ruta ruta)
    {
        try
        {
            using (var currentProcess = Process.GetCurrentProcess())
            {
                var uuid = Guid.NewGuid().ToString();
                var procesadores = _configuration.GetSection("nucleos").Get<int>();
                ValidateAndCreateDierctory(ruta.Destino);
                currentProcess.ProcessorAffinity = (System.IntPtr)procesadores;
                await ZipFileWithProgress
                   .CreateFromDirectoryAsync(
                    $@"{ruta.Origen}", 
                    $@"{ruta.Destino}\\{uuid}.zip", 
                    new BasicProgress<double>(p =>
                    {
                       Console.SetCursorPosition(0, Console.CursorTop);
                       Console.Write($"{p:P2} archiving complete");
                    }));
                currentProcess.ProcessorAffinity = (IntPtr)((1 << Environment.ProcessorCount) - 1);
            }
        }
        catch (Exception e)
        {
            Log(e.ToString());
        }
        
    }
    public static void ValidateAndCreateDierctory(string path)
    {
        try
        {
            if (!ValidatePath(path))
            {
                CreateDirectory(path);
            }
            else
            {
                Log("Invalid path: " + path);
            }
        }
        catch (UnauthorizedAccessException e)
        {
            Log("Unauthorized access to directory: " + e.Message);
        }
        catch (PathTooLongException e)
        {
            Log("Path too long: " + e.Message);
        }
        catch (DirectoryNotFoundException e)
        {
            Log("Directory not found: " + e.Message);
        }
        catch (IOException e)
        {
            Log("IO error while creating directory: " + e.Message);
        }
        catch (Exception e)
        {
            Log("An unexpected error occurred: " + e.ToString());
        }
    }
    public static bool ValidatePath(string path)
    {
        var exists = Directory.Exists(path);
        return exists;
    }
    public static void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }
    public static string CleanPath(string path)
    {
        path = path.Trim();
        if (path.EndsWith("\\"))
        {
            path = path.Substring(0, path.Length - 1);
            return path;
        }
        return path;
    }
    private static async Task HandleMonthlyRuta(SqLiteRepository sqLite, Ruta ruta, DateTime fecha, DateTime hoy)
    {
        DateTime primerDiaDelMes = new DateTime(fecha.Year, fecha.Month, 1, 0, 0, 0);

        var registro = UltimaFecha("registro_mensual", ruta.Origen);

        if (primerDiaDelMes.Equals(hoy))
        {
            if (registro.Id == 0)
            {
                await Respaldo(ruta);
                sqLite.Insert("registro_mensual", fecha.ToString("yyyy-MM-dd HH-mm-ss"), ruta.Origen);

            }
            if (registro.Id != 0)
            {
                var ultimaFechaDiaria = StringToDate(registro.Created_at);
                if (hoy != ultimaFechaDiaria && hoy > ultimaFechaDiaria)
                {
                    await Respaldo(ruta);
                    sqLite.Insert("registro_mensual", fecha.ToString("yyyy-MM-dd HH-mm-ss"), ruta.Origen);
                }
            }
        }
    }

    private static async Task HandleDailyRuta(SqLiteRepository sqLite, Ruta ruta, DateTime fecha, DateTime hoy)
    {
        var registro = UltimaFecha("registro_diario", ruta.Origen);
        if (registro.Id == 0)
        {
            await Respaldo(ruta);
            sqLite.Insert("registro_diario", fecha.ToString("yyyy-MM-dd HH-mm-ss"), ruta.Origen);

        }
        if (registro.Id != 0)
        {
            var ultimaFechaDiaria = StringToDate(registro.Created_at);
            if (hoy != ultimaFechaDiaria && hoy > ultimaFechaDiaria)
            {
                await Respaldo(ruta);
                sqLite.Insert("registro_diario", fecha.ToString("yyyy-MM-dd HH-mm-ss"), ruta.Origen);
            }
        }
    }
    public static Registro UltimaFecha(string tabla, string ruta)
    {
        var sql = new SqLiteRepository(_configuration);
        var fecha = sql.Select(tabla, ruta);
        return fecha;
    }
    public static DateTime StringToDate(string date)
    {
        var fecha = date.Split(' ');
        var split = fecha[0].Split('-');
        var fecha1 = new DateTime(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
        return fecha1;
    }
    public static void Log(string contents)
    {
        using (FileStream fs = new FileStream(@"log.txt", FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
        {
            using (StreamWriter writer = new StreamWriter(fs))
            {
                writer.Write($"{contents}\r\n");
            }
        }
    }
}
