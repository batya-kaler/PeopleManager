using Microsoft.EntityFrameworkCore;
using PeopleManager.Data;
using PeopleManager.DTOs;
using PeopleManager.Models;

namespace PeopleManager.Services
{
    public class PersonService : IPersonService
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public PersonService(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IEnumerable<PersonResponseDto>> GetAllAsync()
        {
            return await _context.People
                .OrderBy(p => p.FullName)
                .Select(p => ToResponseDto(p))
                .ToListAsync();
        }

        public async Task<IEnumerable<PersonResponseDto>> SearchByNameAsync(string name)
        {
            return await _context.People
                .Where(p => p.FullName.Contains(name))
                .OrderBy(p => p.FullName)
                .Select(p => ToResponseDto(p))
                .ToListAsync();
        }

        public async Task<PersonResponseDto> CreateAsync(CreatePersonDto dto, IFormFile? image)
        {
            var person = new Person
            {
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                CreatedAt = DateTime.UtcNow
            };

            if (image != null)
            {
                person.ImagePath = await SaveImageAsync(image);
            }

            _context.People.Add(person);
            await _context.SaveChangesAsync();

            return ToResponseDto(person);
        }

        public Task<byte[]> ExportToPdfAsync()
        {
            throw new NotImplementedException();
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);

            return $"/uploads/{fileName}";
        }

        private static PersonResponseDto ToResponseDto(Person p) => new()
        {
            Id = p.Id,
            FullName = p.FullName,
            Phone = p.Phone,
            Email = p.Email,
            ImageUrl = p.ImagePath,
            CreatedAt = p.CreatedAt
        };
    }
}