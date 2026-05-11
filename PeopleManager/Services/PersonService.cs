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

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private static readonly string[] AllowedMimeTypes = { "image/jpeg", "image/png", "image/gif" };

        private static readonly Dictionary<char, char> EnglishToHebrew = new()
        {
            {'a','ש'},{'b','נ'},{'c','ב'},{'d','ג'},{'e','ק'},{'f','כ'},{'g','ע'},
            {'h','י'},{'i','ן'},{'j','ח'},{'k','ל'},{'l','ך'},{'m','צ'},{'n','מ'},
            {'o','ם'},{'p','פ'},{'q','/'},{'r','ר'},{'s','ד'},{'t','א'},{'u','ו'},
            {'v','ה'},{'w','\''},{'x','ס'},{'y','ט'},{'z','ז'}
        };

        public PersonService(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<PagedResponseDto<PersonResponseDto>> GetAllAsync(PersonFilterDto filter)
        {
            var query = _context.People.AsNoTracking().AsQueryable();

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                query = ApplySearchFilter(query, filter.SearchTerm);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(p => ToResponseDto(p))
                .ToListAsync();

            return new PagedResponseDto<PersonResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<PersonResponseDto?> GetByIdAsync(int id)
        {
            var person = await _context.People
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            return person == null ? null : ToResponseDto(person);
        }

        public async Task<IEnumerable<PersonResponseDto>> SearchByNameAsync(string searchTerm)
        {
            var query = ApplySearchFilter(
                _context.People.AsNoTracking().AsQueryable(),
                searchTerm);

            return await query
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .Select(p => ToResponseDto(p))
                .ToListAsync();
        }

        public async Task<PersonResponseDto> CreateAsync(CreatePersonDto dto, IFormFile? image)
        {
            if (image != null)
                ValidateImage(image);

            var person = new Person
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Email = dto.Email,
                IsActive = true
            };

            if (image != null)
                person.ImagePath = await SaveImageAsync(image);

            _context.People.Add(person);
            await _context.SaveChangesAsync();

            return ToResponseDto(person);
        }

        public async Task<PersonResponseDto?> UpdateStatusAsync(int id, bool isActive)
        {
            var person = await _context.People.FindAsync(id);
            if (person == null) return null;

            person.IsActive = isActive;
            await _context.SaveChangesAsync();

            return ToResponseDto(person);
        }

        public async Task<byte[]> ExportToPdfAsync()
        {
            var people = await _context.People
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .ToListAsync();

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
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("First Name").Bold();
                            header.Cell().Text("Last Name").Bold();
                            header.Cell().Text("Phone").Bold();
                            header.Cell().Text("Email").Bold();
                        });

                        foreach (var person in people)
                        {
                            table.Cell().Text(person.FirstName);
                            table.Cell().Text(person.LastName);
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

        private static IQueryable<Person> ApplySearchFilter(IQueryable<Person> query, string searchTerm)
        {
            var hebrewTerm = TranslateKeyboard(searchTerm.ToLower());
            return query.Where(p =>
                EF.Functions.Like(p.FirstName, $"%{searchTerm}%") ||
                EF.Functions.Like(p.LastName, $"%{searchTerm}%") ||
                EF.Functions.Like(p.FirstName, $"%{hebrewTerm}%") ||
                EF.Functions.Like(p.LastName, $"%{hebrewTerm}%"));
        }

        private static void ValidateImage(IFormFile image)
        {
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
                throw new ArgumentException("Only image files are allowed (.jpg, .jpeg, .png, .gif)");

            if (image.Length > 5 * 1024 * 1024)
                throw new ArgumentException("Image size cannot exceed 5MB");

            if (!AllowedMimeTypes.Contains(image.ContentType.ToLower()))
                throw new ArgumentException("Invalid image file type");
        }

        private static string TranslateKeyboard(string input)
        {
            return new string(input.Select(c =>
                EnglishToHebrew.TryGetValue(c, out var h) ? h : c).ToArray());
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
            FirstName = p.FirstName,
            LastName = p.LastName,
            Phone = p.Phone,
            Email = p.Email,
            ImageUrl = p.ImagePath,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        };
    }
}