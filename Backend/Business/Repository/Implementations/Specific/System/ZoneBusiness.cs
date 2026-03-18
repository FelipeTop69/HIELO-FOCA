using AutoMapper;
using Business.Repository.Interfaces.Specific.System;
using Data.Factory;
using Data.Repository.Interfaces.General;
using Data.Repository.Interfaces.Specific.System;
using Data.Repository.Interfaces.Strategy.Delete;
using Entity.DTOs.System.Item;
using Entity.DTOs.System.Zone;
using Entity.Models.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Utilities.Exceptions;
using Utilities.Helpers;

namespace Business.Repository.Implementations.Specific.System
{
    /// <summary>
    /// Implementación de la lógica de negocio para la gestión de Zonas (áreas dentro de una Sucursal).
    /// </summary>
    public class ZoneBusiness :
        GenericBusinessDualDTO<Zone, ZoneConsultDTO, ZoneDTO>,
        IZoneBusiness
    {

        private readonly IGeneral<Zone> _general;
        private readonly IZone _zoneData;
        private readonly IDataFactoryGlobal _factory;
        public ZoneBusiness(
            IDataFactoryGlobal factory,
            IGeneral<Zone> general,
            IZone zoneData,
            IDeleteStrategyResolver<Zone> deleteStrategyResolver,
            ILogger<Zone> logger,
            IMapper mapper)
            : base(factory.CreateZoneData(), deleteStrategyResolver, logger, mapper)
        {
            _general = general;
            _zoneData = zoneData;
            _factory = factory;
        }

        // General 

        /// <summary>
        /// Obtiene todas las zonas registradas, incluyendo las inactivas.
        /// </summary>
        public async Task<IEnumerable<ZoneConsultDTO>> GetAllTotalAsync()
        {
            var active = await _general.GetAllTotalAsync();
            return _mapper.Map<IEnumerable<ZoneConsultDTO>>(active);
        }


        // Specific

        /// <summary>
        /// Obtiene una lista simple de zonas asociadas a una sucursal específica.
        /// </summary>
        public async Task<IEnumerable<ZoneSimpleDTO>> GetZonesByBranchAsync(int branchId)
        {
            ValidationHelper.EnsureValidId(branchId, "Branch ID");
            var branches = await _zoneData.GetZonesByBranchAsync(branchId);
            return _mapper.Map<IEnumerable<ZoneSimpleDTO>>(branches);
        }

        /// <summary>
        /// Obtiene una lista de zonas disponibles para asignación, según el usuario operativo actual.
        /// </summary>
        public async Task<IEnumerable<ZoneOperatingDTO>> GetAvailableZonesByUserAsync(int userId)
        {
            var operating = await _factory.CreateOperatingData()
                .GetQueryable()
                .Include(o => o.CreatedByUser)   // ← El encargado real
                .Where(o => o.UserId == userId)
                .FirstOrDefaultAsync();

            if (operating is null)
                throw new Exception("El usuario no está asignado a ningún Operating.");

            var encargado = operating.CreatedByUser;

            // 2️⃣ Obtener zonas del encargado
            var zonasDelEncargado = await _factory.CreateZoneData()
                .GetQueryable()
                .Include(z => z.Branch)
                    .ThenInclude(b => b.Company)
                .Where(z => z.UserId == encargado.Id)
                .ToListAsync();

            if (!zonasDelEncargado.Any())
                throw new Exception("El encargado no tiene zonas asignadas.");

            // 3️⃣ Tomamos la Branch desde la primera zona del encargado
            var branch = zonasDelEncargado.First().Branch;

            // 4️⃣ Recuperamos todas las zonas de esa Branch
            var zonas = await _zoneData.GetZonesByBranchAsync(branch.Id);

            // 5️⃣ Mapeo a DTO
            return zonas.Select(z => new ZoneOperatingDTO
            {
                Id = z.Id,
                Name = z.Name,
                Description = z.Description ?? "",
                BranchId = branch.Id,
                BranchName = branch.Name,
                CompanyName = branch.Company.Name,
                StateZone = z.StateZone.ToString()
            }).ToList();
        }

        /// <summary>
        /// Obtiene los detalles completos de una zona específica.
        /// </summary>
        public async Task<ZoneDetailsDTO?> GetZoneDetailsAsync(int zoneId)
        {
            var zone = await _zoneData.GetZoneDetailsAsync(zoneId);
            if (zone == null) return null;

            return _mapper.Map<ZoneDetailsDTO>(zone);
        }

        /// <summary>
        /// Obtiene una lista de los responsables de zona (InCharges) dentro de una sucursal.
        /// </summary>
        public async Task<IEnumerable<ZoneInChargeListDTO>> GetInChargesAsync(int branchId)
        {
            ValidationHelper.EnsureValidId(branchId, "Branch ID");
            var inCharges = await _zoneData.GetInChargesAsync(branchId);

            return _mapper.Map<IEnumerable<ZoneInChargeListDTO>>(inCharges);
        }

        /// <summary>
        /// Obtiene la zona asociada a un Area Manager (usuario) específico.
        /// </summary>
        public async Task<ZoneConsultDTO?> GetZoneByAreaManagerAsync(int userId)
        {
            ValidationHelper.EnsureValidId(userId, "User ID");

            var branch = await _zoneData.GetZoneByAreaManagerAsync(userId);

            if (branch == null)
                return null;

            return _mapper.Map<ZoneConsultDTO>(branch);
        }

        /// <summary>
        /// Realiza una actualización parcial de la zona (ej. Nombre, Descripción), aplicando validaciones de unicidad para el nombre.
        /// </summary>
        public async Task<ZoneConsultDTO> PartialUpdateAsync(ZonePartialUpdateDTO dto)
        {
            ValidationHelper.EnsureValidId(dto.Id, "ZoneId");

            var zone = await _data.GetByIdAsync(dto.Id);
            if (zone == null)
                throw new EntityNotFoundException(nameof(Zone), dto.Id);

            var allZones = await _data.GetAllAsync();

            // --- Name ---
            if (!string.IsNullOrWhiteSpace(dto.Name) &&
                !StringHelper.EqualsNormalized(zone.Name, dto.Name))
            {
                bool nameExists = allZones.Any(c =>
                c.Id != dto.Id &&
                    StringHelper.EqualsNormalized(c.Name, dto.Name));

                if (nameExists)
                    throw new ValidationException("Zone", $"El nombre '{dto.Name}' ya está en uso.");

                zone.Name = dto.Name;
            }

            zone.Description = dto.Description;


            await _data.UpdateAsync(zone);
            return _mapper.Map<ZoneConsultDTO>(zone);
        }

        /// <summary>
        /// Obtiene el inventario base de ítems para una zona específica.
        /// </summary>
        public async Task<IEnumerable<ItemInventorieBaseSimpleDTO>> GetZoneBaseInventoryAsync(int zoneId)
        {
            var items = await _zoneData.GetZoneBaseInventoryAsync(zoneId);

            return _mapper.Map<IEnumerable<ItemInventorieBaseSimpleDTO>>(items);
        }


        // Actions

        /// <summary>
        /// Hook para realizar validaciones de campos obligatorios/IDs y transformaciones de datos (ej. hashing de contraseñas) antes del mapeo y creación.
        /// </summary>
        protected override Task BeforeCreateMap(ZoneDTO dto, Zone entity)
        {
            ValidationHelper.ThrowIfEmpty(dto.Name, "Name");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Hook para realizar validaciones de campos obligatorios/IDs y transformaciones condicionales de datos antes del mapeo y actualización.
        /// </summary>
        protected override Task BeforeUpdateMap(ZoneDTO dto, Zone entity)
        {
            ValidationHelper.ThrowIfEmpty(dto.Name, "Name");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Realiza validaciones asíncronas de unicidad o reglas de negocio complejas antes de la creación de una entidad.
        /// </summary>
        protected override async Task ValidateBeforeCreateAsync(ZoneDTO dto)
        {
            var existing = await _data.GetAllAsync();
            if (existing.Any(e => StringHelper.EqualsNormalized(e.Name, dto.Name)))
                throw new ValidationException("Name", $"Ya existe un Branch con el Name '{dto.Name}'.");
        }

        /// <summary>
        /// Realiza validaciones asíncronas de unicidad o reglas de negocio complejas antes de la actualización de una entidad, excluyendo el registro actual.
        /// </summary>
        protected override async Task ValidateBeforeUpdateAsync(ZoneDTO dto, Zone existingEntity)
        {
            if (!StringHelper.EqualsNormalized(existingEntity.Name, dto.Name))
            {
                var others = await _data.GetAllAsync();
                if (others.Any(e => e.Id != dto.Id && StringHelper.EqualsNormalized(e.Name, dto.Name)))
                    throw new ValidationException("Name", $"Ya existe un Brach con el Name '{dto.Name}'.");
            }
        }
    }
}