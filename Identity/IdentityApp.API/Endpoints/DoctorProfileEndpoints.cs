using System.Security.Claims;
using IdentityApp.Domain.DTOs;
using IdentityApp.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApp.API.Endpoints;

public static class DoctorProfileEndpoints
{
    public static void MapDoctorProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/doctor-profiles")
            .WithTags("Doctor Profiles");

        // Public endpoints
        group.MapGet("/", GetAllProfilesAsync)
            .WithName("GetAllProfiles")
            .WithOpenApi()
            .Produces<IEnumerable<DoctorProfileDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id}", GetProfileByIdAsync)
            .WithName("GetProfileById")
            .WithOpenApi()
            .Produces<DoctorProfileDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/user/{userId}", GetProfileByUserIdAsync)
            .WithName("GetProfileByUserId")
            .WithOpenApi()
            .Produces<DoctorProfileDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/specialty/{specialty}", GetProfilesBySpecialtyAsync)
            .WithName("GetProfilesBySpecialty")
            .WithOpenApi()
            .Produces<IEnumerable<DoctorProfileDto>>(StatusCodes.Status200OK);

        group.MapGet("/search", SearchProfilesAsync)
            .WithName("SearchProfiles")
            .WithOpenApi()
            .Produces<IEnumerable<DoctorProfileDto>>(StatusCodes.Status200OK);

        // Authenticated endpoints
        group.MapGet("/my-profile", GetMyProfileAsync)
            .WithName("GetMyProfile")
            .WithOpenApi()
            .Produces<DoctorProfileDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateProfileAsync)
            .WithName("CreateProfile")
            .RequireAuthorization()
            .WithOpenApi()
            .Produces<DoctorProfileDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapPut("/", UpdateProfileAsync)
            .WithName("UpdateProfile")
            .RequireAuthorization()
            .WithOpenApi()
            .Produces<DoctorProfileDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest);

        group.MapDelete("/", DeleteProfileAsync)
            .WithName("DeleteProfile")
            .RequireAuthorization()
            .WithOpenApi()
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetAllProfilesAsync(IDoctorProfileService profileService)
    {
        var (success, message, profiles) = await profileService.GetAllProfilesAsync();
        return Results.Ok(profiles);
    }

    private static async Task<IResult> GetProfileByIdAsync(
        Guid id,
        IDoctorProfileService profileService)
    {
        var (success, message, profile) = await profileService.GetProfileByIdAsync(id);

        if (!success)
        {
            return Results.NotFound(new { message });
        }

        return Results.Ok(profile);
    }

    private static async Task<IResult> GetProfileByUserIdAsync(
        Guid userId,
        IDoctorProfileService profileService)
    {
        var (success, message, profile) = await profileService.GetProfileByUserIdAsync(userId);

        if (!success)
        {
            return Results.NotFound(new { message });
        }

        return Results.Ok(profile);
    }

    private static async Task<IResult> GetProfilesBySpecialtyAsync(
        string specialty,
        IDoctorProfileService profileService)
    {
        var (success, message, profiles) = await profileService.GetProfilesBySpecialtyAsync(specialty);
        return Results.Ok(profiles);
    }

    private static async Task<IResult> SearchProfilesAsync(
        [FromQuery] string? query,
        IDoctorProfileService profileService)
    {
        var (success, message, profiles) = await profileService.SearchProfilesAsync(query ?? string.Empty);
        return Results.Ok(profiles);
    }

    private static async Task<IResult> GetMyProfileAsync(
        HttpContext httpContext,
        IDoctorProfileService profileService)
    {
        var userIdGuid = TryGetUserId(httpContext.User);

        if (!userIdGuid.HasValue)
        {
            return Results.Ok(null);
        }

        var (success, message, profile) = await profileService.GetProfileByUserIdAsync(userIdGuid.Value);

        if (!success)
        {
            // Missing profile is a normal case for newly registered doctors.
            // Return 200 with null to avoid browser 404 noise on initial profile setup.
            return Results.Ok(null);
        }

        return Results.Ok(profile);
    }

    private static async Task<IResult> CreateProfileAsync(
        [FromBody] CreateDoctorProfileDto model,
        HttpContext httpContext,
        IDoctorProfileService profileService)
    {
        var userIdGuid = TryGetUserId(httpContext.User);

        if (!userIdGuid.HasValue)
        {
            return Results.Unauthorized();
        }

        var (success, message, profile) = await profileService.CreateProfileAsync(userIdGuid.Value, model);

        if (!success)
        {
            return Results.BadRequest(new { message });
        }

        return Results.Ok(profile);
    }

    private static async Task<IResult> UpdateProfileAsync(
        [FromBody] UpdateDoctorProfileDto model,
        HttpContext httpContext,
        IDoctorProfileService profileService)
    {
        var userIdGuid = TryGetUserId(httpContext.User);

        if (!userIdGuid.HasValue)
        {
            return Results.Unauthorized();
        }

        var (success, message, profile) = await profileService.UpdateProfileAsync(userIdGuid.Value, model);

        if (!success)
        {
            return Results.BadRequest(new { message });
        }

        return Results.Ok(profile);
    }

    private static async Task<IResult> DeleteProfileAsync(
        HttpContext httpContext,
        IDoctorProfileService profileService)
    {
        var userIdGuid = TryGetUserId(httpContext.User);

        if (!userIdGuid.HasValue)
        {
            return Results.Unauthorized();
        }

        var (success, message) = await profileService.DeleteProfileAsync(userIdGuid.Value);

        if (!success)
        {
            return Results.NotFound(new { message });
        }

        return Results.Ok(new { message });
    }

    private static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("nameid");

        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}
