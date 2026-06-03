namespace FoodDrinkApp.Services;

public static class MockApiConfig
{
    // Activated the mockapi.io remote endpoint for final release
    public const string EndpointUrl = "https://660429112ca9478ea17ff729.mockapi.io/api/v1/foods";

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(EndpointUrl);
}