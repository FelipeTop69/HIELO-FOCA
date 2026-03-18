using Data.Repository.Interfaces.General;
using Data.Repository.Interfaces.Specific.System;
using Entity.Context;
using Entity.Models.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace Data.Repository.Implementations.Specific.System
{
    /// <summary>
    /// Repositorio para gestión de items con generación automática de códigos QR
    /// </summary>
    public class ItemData : GenericData<Item>, IItem
    {
        private readonly AppDbContext _context;
        private readonly ILogger _logger;
        private readonly IQrCodeService _qrService;

        public ItemData(AppDbContext context, ILogger<Item> logger, IQrCodeService qrService)
            : base(context, logger)
        {
            _context = context;
            _logger = logger;
            _qrService = qrService;
        }

        /// <summary>
        /// Obtiene todos los items activos con sus relaciones
        /// </summary>
        public override async Task<IEnumerable<Item>> GetAllAsync()
        {
            try
            {
                return await _context.Item
                    .Include(fm => fm.CategoryItem)
                    .Include(fm => fm.StateItem)
                    .Include(fm => fm.Zone)
                    .Where(fm => fm.Active)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "No se puedieron obtener los datos");
                throw;
            }
        }

        /// <summary>
        /// Obtiene un item por ID con sus relaciones
        /// </summary>
        /// <param name="id">ID del item</param>
        public override async Task<Item?> GetByIdAsync(int id)
        {
            try
            {
                return await _context.Item
                    .Include(fm => fm.CategoryItem)
                    .Include(fm => fm.StateItem)
                    .Include(fm => fm.Zone)
                    .FirstOrDefaultAsync(fm => fm.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "No se puedieron obtener los datos por id");
                throw;
            }
        }

        /// <summary>
        /// Crea un nuevo item y genera automáticamente su código QR
        /// </summary>
        /// <param name="entity">Item a crear</param>
        public override async Task<Item> CreateAsync(Item entity)
        {
            // Guardar primero para obtener ID
            await _context.Set<Item>().AddAsync(entity);
            await _context.SaveChangesAsync();

            string content = $"Code:{entity.Code}";

            // Version con jerarquía pero mismo formato
            var uploadResult = _qrService.GenerateAndSaveQrCodeWithHierarchy(content, entity.Id, entity.Code);

            entity.QrPath = uploadResult.SecureUrl;     // Guardamos la URL
            entity.QrPublicId = uploadResult.PublicId;  // Guardamos el PublicId

            _context.Set<Item>().Update(entity);
            await _context.SaveChangesAsync();

            return entity;
        }

        /// <summary>
        /// Elimina un item de la base de datos y su QR asociado en Cloudinary.
        /// </summary>
        /// <param name="id">ID del item a eliminar</param>
        public override async Task<bool> DeletePersistenceAsync(int id)
        {
            try
            {
                var itemToDelete = await _context.Item
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (itemToDelete == null)
                {
                    _logger.LogWarning("Intento de borrado fallido: Item con id {Id} no encontrado.", id);
                    return false;
                }

                // Borrar el QR de Cloudinary (si tiene uno)
                if (!string.IsNullOrEmpty(itemToDelete.QrPublicId))
                {
                    try
                    {
                        bool deleteSuccess = await _qrService.DeleteQrCodeAsync(itemToDelete.QrPublicId);
                        if (deleteSuccess)
                        {
                            _logger.LogInformation("QR de Cloudinary {PublicId} eliminado exitosamente.", itemToDelete.QrPublicId);
                        }
                        else
                        {
                            _logger.LogWarning("El borrado del QR {PublicId} en Cloudinary falló o no fue encontrado.", itemToDelete.QrPublicId);
                        }
                    }
                    catch (Exception cloudinaryEx)
                    {
                        // IMPORTANTE: Loguear el error de Cloudinary, pero NO el borrado de la BD. 
                        _logger.LogWarning(cloudinaryEx,
                            "Error al eliminar QR de Cloudinary {PublicId}. El item {Id} se eliminará de la BD de todos modos.",
                            itemToDelete.QrPublicId, id);
                    }
                }

                // 3. Borrar el item de la Base de Datos
                _context.Set<Item>().Remove(itemToDelete);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Item con id {Id} eliminado exitosamente de la BD.", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al eliminar la entidad con id {Id}", id);
                return false;
            }
        }


        // General

        /// <summary>
        /// Obtiene todos los items sin filtrar por estado
        /// </summary>
        public override async Task<IEnumerable<Item>> GetAllTotalAsync()
        {
            try
            {
                return await _context.Item
                    .Include(fm => fm.CategoryItem)
                    .Include(fm => fm.StateItem)
                    .Include(fm => fm.Zone)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, $"No se puedieron obtener todos los datos");
                throw;
            }
        }

        //Specific

        /// <summary>
        /// Obtiene items de una zona específica con sus relaciones
        /// </summary>
        /// <param name="zonaId">ID de la zona</param>
        public override async Task<IEnumerable<Item>> GetAllItemsSpecific(int zonaId)
        {
            try
            {
                return await _context.Item
                    .Include(i => i.CategoryItem)
                    .Include(i => i.StateItem)
                    .Include(i => i.Zone)
                    .Where(i => i.ZoneId == zonaId)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, $"No se pudieron obtener todos los datos");
                throw;
            }
        }

        /// <summary>
        /// Busca un item por su código único
        /// </summary>
        /// <param name="code">Código del item</param>
        public async Task<Item?> GetByCodeAsync(string code)
        {
            return await _context.Item
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Code == code);
        }

        /// <summary>
        /// Obtiene el último código generado para una categoría
        /// </summary>
        /// <param name="categoryId">ID de la categoría</param>
        public async Task<string> GetLastCodeByCategoryAsync(int categoryId)
        {
            return await _context.Item
                .Where(i => i.CategoryItemId == categoryId && i.Active)
                .OrderByDescending(i => i.Code)
                .Select(i => i.Code)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        /// <summary>
        /// Genera el siguiente código disponible basado en la categoría
        /// </summary>
        /// <param name="categoryName">Nombre de la categoría</param>
        public async Task<string> GenerateNextCodeAsync(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                throw new ArgumentException("Category name cannot be empty.", nameof(categoryName));

            //Normalizar y eliminar tildes
            string CleanString(string input)
            {
                var normalized = input.Normalize(NormalizationForm.FormD);
                var sb = new StringBuilder();

                foreach (var c in normalized)
                {
                    var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                    if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                        sb.Append(c);
                }

                return sb.ToString().Normalize(NormalizationForm.FormC);
            }

            // Quitar tildes y dejar solo letras/numeros
            string Sanitize(string input)
            {
                input = CleanString(input).ToUpper();

                var sb = new StringBuilder();
                foreach (var c in input)
                {
                    if (char.IsLetterOrDigit(c))
                        sb.Append(c);
                }
                return sb.ToString();
            }

            // Generar prefijo limpio (solo letras/numeros, sin acentos)
            var sanitized = Sanitize(categoryName);
            var prefix = sanitized.Length >= 3 ? sanitized[..3] : sanitized;

            // Buscar el ultimo codigo con este prefijo
            var lastCode = await _context.Item
                .Where(i => i.Code.StartsWith(prefix) && i.Active)
                .OrderByDescending(i => i.Code)
                .Select(i => i.Code)
                .FirstOrDefaultAsync();

            if (lastCode is null)
                return $"{prefix}001";

            // Extraer parte numerica despues del prefijo
            var numberPart = lastCode[prefix.Length..];

            if (int.TryParse(numberPart, out int lastNumber))
                return $"{prefix}{(lastNumber + 1):D3}";

            // Si el formato es incorrecto, reinicia numeracion
            return $"{prefix}001";
        }

        /// <summary>
        /// Valida códigos existentes para carga masiva
        /// </summary>
        /// <param name="codes">Lista de códigos a validar</param>
        public async Task<HashSet<string>> GetExistingCodesAsync(List<string> codes)
        {
            return await _context.Item
                .Where(i => codes.Contains(i.Code) && i.Active)
                .Select(i => i.Code)
                .ToHashSetAsync();
        }

        /// <summary>
        /// Obtiene todos los items sin filtrar (versión 2)
        /// </summary>
        public async Task<IEnumerable<Item>> GetAllTotalV2Async()
        {
            try
            {
                return await _context.Item
                    .Include(fm => fm.CategoryItem)
                    .Include(fm => fm.StateItem)
                    .Include(fm => fm.Zone)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, $"No se puedieron obtener todos los datos");
                throw;
            }
        }

        /// <summary>
        /// Obtiene un item por su código dentro de una sucursal específica,
        /// incluyendo sus relaciones de categoría, estado y zona.
        /// </summary>
        /// <param name="code">Código único del item</param>
        /// <param name="branchId">ID de la sucursal a la que pertenece</param>
        /// <returns>El item encontrado o null si no existe</returns>
        public async Task<Item?> GetByCodeAndBranchAsync(string code, int branchId)
        {
            try
            {
                return await _context.Item
                    .Include(i => i.CategoryItem)
                    .Include(i => i.StateItem)
                    .Include(i => i.Zone)
                    .Where(i => i.Code == code && i.Zone!.BranchId == branchId)
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo obtener el item con código {Code} en la sucursal {BranchId}", code, branchId);
                throw;
            }
        }

    }
}
