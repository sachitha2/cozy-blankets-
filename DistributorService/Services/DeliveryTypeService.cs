using DistributorService.DTOs;
using DistributorService.Models;
using DistributorService.Repositories;

namespace DistributorService.Services;

/// <summary>
/// Service implementation for DeliveryType business logic
/// </summary>
public class DeliveryTypeService : IDeliveryTypeService
{
    private readonly IDeliveryTypeRepository _repository;
    private readonly ILogger<DeliveryTypeService> _logger;

    public DeliveryTypeService(
        IDeliveryTypeRepository repository,
        ILogger<DeliveryTypeService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<DeliveryTypeDto>> GetAllAsync()
    {
        try
        {
            var deliveryTypes = await _repository.GetAllAsync();
            return deliveryTypes.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery types");
            throw;
        }
    }

    public async Task<DeliveryTypeDto?> GetByIdAsync(int id)
    {
        try
        {
            var deliveryType = await _repository.GetByIdAsync(id);
            return deliveryType == null ? null : MapToDto(deliveryType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving delivery type with Id: {Id}", id);
            throw;
        }
    }

    public async Task<DeliveryTypeDto> CreateAsync(CreateDeliveryTypeDto dto)
    {
        try
        {
            var deliveryType = new DeliveryType
            {
                Name = dto.Name,
                Description = dto.Description,
                Cost = dto.Cost,
                EstimatedDays = dto.EstimatedDays,
                IsActive = true
            };

            var created = await _repository.AddAsync(deliveryType);
            _logger.LogInformation("Delivery type created: {Name} (Id: {Id})", created.Name, created.Id);
            return MapToDto(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating delivery type");
            throw;
        }
    }

    public async Task<DeliveryTypeDto> UpdateAsync(int id, UpdateDeliveryTypeDto dto)
    {
        try
        {
            var deliveryType = await _repository.GetByIdAsync(id);
            if (deliveryType == null)
            {
                throw new KeyNotFoundException($"Delivery type with Id {id} not found");
            }

            deliveryType.Name = dto.Name;
            deliveryType.Description = dto.Description;
            deliveryType.Cost = dto.Cost;
            deliveryType.EstimatedDays = dto.EstimatedDays;
            deliveryType.IsActive = dto.IsActive;

            var updated = await _repository.UpdateAsync(deliveryType);
            _logger.LogInformation("Delivery type updated: {Name} (Id: {Id})", updated.Name, updated.Id);
            return MapToDto(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating delivery type with Id: {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var result = await _repository.DeleteAsync(id);
            if (result)
            {
                _logger.LogInformation("Delivery type deleted (soft delete) with Id: {Id}", id);
            }
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting delivery type with Id: {Id}", id);
            throw;
        }
    }

    private static DeliveryTypeDto MapToDto(DeliveryType deliveryType)
    {
        return new DeliveryTypeDto
        {
            Id = deliveryType.Id,
            Name = deliveryType.Name,
            Description = deliveryType.Description,
            Cost = deliveryType.Cost,
            EstimatedDays = deliveryType.EstimatedDays,
            IsActive = deliveryType.IsActive
        };
    }
}
