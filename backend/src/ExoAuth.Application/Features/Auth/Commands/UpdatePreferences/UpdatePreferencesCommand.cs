using ExoAuth.Application.Features.Auth.Models;
using Mediator;

namespace ExoAuth.Application.Features.Auth.Commands.UpdatePreferences;

public sealed record UpdatePreferencesCommand(
    string Language
) : ICommand<UpdatePreferencesResponse>;
