using Game.Api.Dtos;
using Game.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace Game.Api.Controllers;

[ApiController]
[Route("/steam")]
public class GamesController : ControllerBase
{
    private readonly ILogger<GamesController> _logger;
    private readonly SteamGameApiService _steamGameApiService;
    private readonly SteamStoreApiService _steamStoreApiService;

    public GamesController(ILogger<GamesController> logger, SteamGameApiService steamGameApiService,
        SteamStoreApiService steamStoreApiService)
    {
        _logger = logger;
        _steamGameApiService = steamGameApiService;
        _steamStoreApiService = steamStoreApiService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id, [FromQuery] string? lang)
    {
        if (string.IsNullOrWhiteSpace(id))
            return BadRequest(new ApiErrorResponse
            {
                IsSuccess = false,
                Message = "Game id is required.",
                Error = "InvalidGameId"
            });

        var language = string.IsNullOrWhiteSpace(lang)
            ? "english"
            : lang.Trim().ToLowerInvariant();

        try
        {
            var game = await _steamGameApiService.GetGameByIdAsync(id, language, HttpContext.RequestAborted);

            if (game is null)
            {
                _logger.LogWarning("Game not found or Steam API returned invalid data for id {GameId}", id);

                return NotFound(new ApiErrorResponse
                {
                    IsSuccess = false,
                    Message = "Game not found.",
                    Error = "GameNotFound"
                });
            }

            _logger.LogInformation("Game details fetched successfully for id {GameId}", id);

            return Ok(new ApiSuccessResponse<SteamAppDetails>
            {
                IsSuccess = true,
                Message = "Game details fetched successfully.",
                Data = game
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching game {GameId}", id);

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                IsSuccess = false,
                Message = "An unexpected error occurred while fetching game details.",
                Error = "InternalServerError"
            });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string term,
        [FromQuery] string? lang,
        [FromQuery] string? cc)
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(new ApiErrorResponse
            {
                IsSuccess = false,
                Message = "Search term is required.",
                Error = "InvalidTerm"
            });

        var language = string.IsNullOrWhiteSpace(lang) ? "english" : lang!;
        var countryCode = string.IsNullOrWhiteSpace(cc) ? "UA" : cc!;

        var result = await _steamStoreApiService.SearchAsync(
            term,
            language,
            countryCode);

        if (result is null || result.Total == 0 || result.Items is null || result.Items.Count == 0)
            return Ok(new ApiSuccessResponse<SteamStoreSearchResponse>
            {
                IsSuccess = true,
                Message = "No results.",
                Data = new SteamStoreSearchResponse
                {
                    Items = new List<SteamStoreSearchItem>(),
                    Total = 0
                }
            });

        return Ok(new ApiSuccessResponse<SteamStoreSearchResponse>
        {
            IsSuccess = true,
            Message = "Search results.",
            Data = new SteamStoreSearchResponse
            {
                Items = result.Items,
                Total = result.Total
            }
        });
    }
}