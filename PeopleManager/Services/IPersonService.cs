using PeopleManager.DTOs;

namespace PeopleManager.Services
{
    public interface IPersonService
    {
        Task<PagedResponseDto<PersonResponseDto>> GetAllAsync(PersonFilterDto filter);
        Task<PersonResponseDto?> GetByIdAsync(int id);
        Task<IEnumerable<PersonResponseDto>> SearchByNameAsync(string searchTerm);
        Task<PersonResponseDto> CreateAsync(CreatePersonDto dto, IFormFile? image);
        Task<PersonResponseDto?> UpdateStatusAsync(int id, bool isActive);
        Task<byte[]> ExportToPdfAsync();
    }
}