using Microsoft.AspNetCore.Mvc;
using PeopleManager.DTOs;
using PeopleManager.Services;

namespace PeopleManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PeopleController : ControllerBase
    {
        private readonly IPersonService _personService;

        public PeopleController(IPersonService personService)
        {
            _personService = personService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var people = await _personService.GetAllAsync();
            return Ok(people);
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest("Search term cannot be empty");

            var results = await _personService.SearchByNameAsync(name);
            return Ok(results);
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Create([FromForm] CreatePersonDto dto, IFormFile? image)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _personService.CreateAsync(dto, image);
            return CreatedAtAction(nameof(GetAll), new { id = result.Id }, result);
        }

        [HttpGet("export/pdf")]
        public async Task<IActionResult> ExportPdf()
        {
            var pdfBytes = await _personService.ExportToPdfAsync();
            return File(pdfBytes, "application/pdf", "people.pdf");
        }
    }
}