using Friends.Api.Dtos;
using Friends.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;

namespace Friends.Api.Controllers;

[ApiController]
[Route("/steam")]
public class FriendsController : ControllerBase
{
    private readonly SteamApiClient _friendApiClient;
    private readonly ILogger<FriendsController> _logger;
    private readonly FriendsSnapshotService _snapshotService;

    public FriendsController(FriendsSnapshotService snapshotService, ILogger<FriendsController> logger,
        SteamApiClient friendApiClient)
    {
        _friendApiClient = friendApiClient;
        _snapshotService = snapshotService;
        _logger = logger;
    }

    [HttpGet("friends")]
    public async Task<ActionResult<FriendsSnapshotDto>> GetMyFriendsAsync(CancellationToken ct)
    {
        var steamId = Request.Headers["X-User-Id"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(steamId))
        {
            _logger.LogWarning("No X-User-Id header found for current request");

            return StatusCode(StatusCodes.Status401Unauthorized, new ApiErrorResponse
            {
                IsSuccess = false,
                Message = "SteamId is missing in token.",
                Error = "Unauthorized"
            });
        }

        try
        {
            var snapshot = await _snapshotService.GetSnapshotAsync(steamId, ct);

            if (snapshot == null || snapshot.Friends == null || snapshot.Friends.Count == 0)
            {
                _logger.LogInformation($"No friends data found in Steam for user {steamId}");

                return NotFound(new ApiErrorResponse
                {
                    IsSuccess = false,
                    Message = "No friends found or no data returned from Steam.",
                    Error = "FriendNotFound"
                });
            }
            
            return Ok(new ApiSuccessResponse<FriendsSnapshotDto>
            {
                IsSuccess = true,
                Message = "Friends list fetched successfully.",
                Data = snapshot
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error while fetching friends for user {steamId}");

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                IsSuccess = false,
                Message = $"An unexpected error occurred while fetching friends for user {steamId}.",
                Error = "InternalServerError"
            });
        }
    }

    [HttpGet("friends/{friendId}")]
    public async Task<ActionResult<FriendDetailsDto>> GetFriendSummaryByIdAsync(string friendId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(friendId))
            return BadRequest(new ApiErrorResponse
            {
                IsSuccess = false,
                Message = "Friend id is required.",
                Error = "InvalidFriendId"
            });

        try
        {
            var friend = await _friendApiClient.GetFriendSummaryById(friendId, ct);

            if (friend is null)
            {
                _logger.LogInformation($"Friend {friendId} not found in Steam or no data returned");

                return NotFound(new ApiErrorResponse
                {
                    IsSuccess = false,
                    Message = "Friend not found or no data returned from Steam.",
                    Error = "FriendNotFound"
                });
            }

            return Ok(new ApiSuccessResponse<FriendDetailsDto>
            {
                IsSuccess = true,
                Message = "Friend details fetched successfully.",
                Data = friend
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error while fetching friend {friendId}");

            return StatusCode(StatusCodes.Status500InternalServerError, new ApiErrorResponse
            {
                IsSuccess = false,
                Message = "An unexpected error occurred while fetching friend details.",
                Error = "InternalServerError"
            });
        }
    }
}