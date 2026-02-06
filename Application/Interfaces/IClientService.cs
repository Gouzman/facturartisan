using FacturArtisan.Api.DTOs;
using FacturArtisan.Api.Application.DTOs.Clients;
using ClientDto = FacturArtisan.Api.Application.DTOs.Clients.ClientDto;

namespace FacturArtisan.Api.Application.Interfaces;

public interface IClientService
{
    Task<PagedResult<ClientDto>> GetClients(int page, int pageSize);
    Task<ClientDto> CreateClient(CreateClientRequest request);
    Task<ClientDto?> UpdateClient(Guid id, UpdateClientRequest request);
}
