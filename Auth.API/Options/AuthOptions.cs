namespace Auth.Api.Options;

public class AuthOptions
{
    public string PublicBackendUrl { get; set; } = default!;
    public string FrontendCallbackUrl { get; set; } = default!;
}
