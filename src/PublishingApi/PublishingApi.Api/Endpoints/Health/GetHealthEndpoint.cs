public static class GetHealthEndpoint
{
    public static void MapGetHealth(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
           .WithName("GetHealth")
           .AllowAnonymous();
    }
}
