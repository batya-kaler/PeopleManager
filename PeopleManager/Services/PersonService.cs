using Microsoft.EntityFrameworkCore;
using PeopleManager.Data;
using PeopleManager.DTOs;
using PeopleManager.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

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
            if (image != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                    throw new ArgumentException("Only image files are allowed (.jpg, .jpeg, .png, .gif)");

                if (image.Length > 5 * 1024 * 1024)
                    throw new ArgumentException("Image size cannot exceed 5MB");
            }

            var person = new Person
            {
                FullName = dto.FullName,
                Phone = dto.Phone,
                Email = dto.Email,
                CreatedAt = DateTime.UtcNow
            };

            if (image != null)
                person.ImagePath = await SaveImageAsync(image);

            _context.People.Add(person);
            await _context.SaveChangesAsync();

            return ToResponseDto(person);
        }

        public async Task<byte[]> ExportToPdfAsync()
        {
            var people = await _context.People
                .OrderBy(p => p.FullName)
                .ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    page.Header()
                        .Text("People List")
                        .FontSize(20)
                        .Bold()
                        .AlignCenter();

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Full Name").Bold();
                            header.Cell().Text("Phone").Bold();
                            header.Cell().Text("Email").Bold();
                        });

                        foreach (var person in people)
                        {
                            table.Cell().Text(person.FullName);
                            table.Cell().Text(person.Phone);
                            table.Cell().Text(person.Email);
                        }
                    });

                    page.Footer().Text(text =>
                    {
                        text.Span("Generated: ");
                        text.Span(DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"));
                    });
                });
            }).GeneratePdf();
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