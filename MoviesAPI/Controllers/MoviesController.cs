using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MoviesAPI.Dtos;
using MoviesAPI.Models;

namespace MoviesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        private List<string> _allowedExtenstions = new List<string> { ".jpg", ".png" };
        private long _maxAllowedPosterSize = 1048576;

        public MoviesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var movies = await _context.Movies
                .OrderByDescending(x => x.Rate)
                .Include(m => m.Genre)
                .Select(m => new MovieDetailsDto
                {
                    Id = m.Id,
                    GenreId = m.GenreId,
                    GenreName = m.Genre.Name,
                    Poster = m.Poster,
                    Rate = m.Rate,
                    Storeline = m.Storeline,
                    Title = m.Title,
                    Year = m.Year
                })
                .ToListAsync();

            return Ok(movies);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(int id)
        {
            var movie = await _context.Movies.Include(m => m.Genre).SingleOrDefaultAsync(m => m.Id == id);

            if (movie == null)
                return NotFound();

            var dto = new MovieDetailsDto
            {
                Id = movie.Id,
                GenreId = movie.GenreId,
                GenreName = movie.Genre.Name,
                Poster = movie.Poster,
                Rate = movie.Rate,
                Storeline = movie.Storeline,
                Title = movie.Title,
                Year = movie.Year
            };

            return Ok(dto);
        }

        [HttpGet("GetByGenreId")]
        public async Task<IActionResult> GetByGenreIdAsync(byte genreId)
        {
            var movies = await _context.Movies
                .Where(m => m.GenreId == genreId)
                .OrderByDescending(x => x.Rate)
                .Include(m => m.Genre)
                .Select(m => new MovieDetailsDto
                {
                    Id = m.Id,
                    GenreId = m.GenreId,
                    GenreName = m.Genre.Name,
                    Poster = m.Poster,
                    Rate = m.Rate,
                    Storeline = m.Storeline,
                    Title = m.Title,
                    Year = m.Year
                })
                .ToListAsync();

            return Ok(movies);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromForm] MovieDto dto)
        {
            if (!_allowedExtenstions.Contains(Path.GetExtension(dto.Poster.FileName).ToLower()))
                return BadRequest("Only .jpg and .png images are allowed!");

            if (dto.Poster.Length > _maxAllowedPosterSize)
                return BadRequest("Max allowed size for poster is 1MB!");

            var isValidGenre = await _context.Genres.AnyAsync(g => g.Id == dto.GenreId);

            if (!isValidGenre)
                return BadRequest("Invalid genere ID!");

            using var dataStream = new MemoryStream();

            await dto.Poster.CopyToAsync(dataStream);

            var movie = new Movie
            {
                Title = dto.Title,
                Year = dto.Year,
                Rate = dto.Rate,
                Storeline = dto.Storeline,
                Poster = dataStream.ToArray(),
                GenreId = dto.GenreId
            };

            await _context.AddAsync(movie);
            _context.SaveChanges();

            return Ok(movie);
        }
    }
}
