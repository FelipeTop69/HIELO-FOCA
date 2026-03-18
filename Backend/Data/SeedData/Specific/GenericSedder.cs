using Data.SeedData.Interface;
using Entity.Context;
using Entity.Models.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.Json.Serialization;
using Utilities.Enums.Models;
using Utilities.Helpers.Interface;

namespace Data.SeedData.Specific
{
    /// <summary>
    /// Sembrador genérico que lee datos desde archivos JSON y los inserta en la base de datos
    /// </summary>
    /// <typeparam name="T">Tipo de entidad a sembrar</typeparam>
    public class GenericSeeder<T> : IDataSeeder where T : class
    {
        private readonly string _folderName;
        private readonly string _fileName;
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Crea una instancia del sembrador genérico
        /// </summary>
        /// <param name="folderName">Carpeta donde se encuentra el archivo JSON</param>
        /// <param name="fileName">Nombre del archivo JSON con los datos</param>
        /// <param name="configuration">Configuración de la aplicación</param>
        public GenericSeeder(string folderName, string fileName, IConfiguration configuration)
        {
            _folderName = folderName;
            _fileName = fileName;
            _configuration = configuration;
        }

        /// <summary>
        /// Lee el archivo JSON y siembra los datos si la tabla está vacía
        /// </summary>
        /// <param name="context">Contexto de base de datos</param>
        public async Task SeedAsync(AppDbContext context)
        {
            // Obtener ruta configurada, o default interna
            var configuredPath = _configuration["SeedData"];
            var basePath = !string.IsNullOrEmpty(configuredPath)
                ? configuredPath
                : Path.Combine(AppContext.BaseDirectory, "SeedData", "JSONs");

            // Ruta completa del archivo
            var filePath = Path.Combine(basePath, _folderName, _fileName);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[Seeder] Archivo no encontrado: {filePath}");
                return;
            }

            // Leer archivo JSON
            var json = await File.ReadAllTextAsync(filePath);

            // Settings para enums como string
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };

            // Deserializar
            var data = JsonSerializer.Deserialize<List<T>>(json, options);
            if (data is not { Count: > 0 })
                return;

            // Procesar entidades
            foreach (var item in data)
            {
                // Default STATUS si aplica → MUY IMPORTANTE
                if (item is Inventary inv)
                {
                    inv.Status ??= InventaryStatus.InProgress;
                }

                // Si la entidad requiere hasheo de contraseña
                if (item is IRequiresPasswordHashing hashable)
                {
                    hashable.HashPassword();
                }
            }

            var dbSet = context.Set<T>();

            // Evitar duplicados: solo insertar si la tabla está vacía
            if (!await dbSet.AnyAsync())
            {
                await dbSet.AddRangeAsync(data);
                await context.SaveChangesAsync();
                Console.WriteLine($"[Seeder] Datos insertados para: {typeof(T).Name}");
            }
        }
    }
}
