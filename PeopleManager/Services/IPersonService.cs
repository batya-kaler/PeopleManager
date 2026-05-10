using PeopleManager.DTOs;

namespace PeopleManager.Services
{
    /// <summary>
    /// Defines the contract for all person-related business logic operations.
    /// </summary>
    public interface IPersonService
    {
        Task<IEnumerable<PersonResponseDto>> GetAllAsync();
        Task<IEnumerable<PersonResponseDto>> SearchByNameAsync(string name);
        Task<PersonResponseDto> CreateAsync(CreatePersonDto dto, IFormFile? image);
        Task<byte[]> ExportToPdfAsync();
    }
}