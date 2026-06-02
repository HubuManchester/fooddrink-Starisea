using FoodDrinkApp.Models;
using FoodDrinkApp.Services;

namespace FoodDrinkApp;

public partial class MainPage : ContentPage
{
    private const double ShakeThreshold = 2.5;
    private bool _isShakingActive = false;

    public MainPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        AccessibilityService.ApplyFontScale(this);
        await LoadFoodItemsAsync(SearchFoodBar.Text);

        ToggleAccelerometer(true);
    }

    protected override void OnDisappearing()
    {
        ToggleAccelerometer(false);
        base.OnDisappearing();
    }

    private async Task LoadFoodItemsAsync(string? query = null)
    {
        FoodCollection.ItemsSource = await FoodCatalogService.SearchAsync(query);
    }

    private async void OnAddClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(AddItemPage));
    }

    private async void OnDetailsClicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is string id)
        {
            await Shell.Current.GoToAsync($"{nameof(FoodDetailPage)}?id={Uri.EscapeDataString(id)}");
        }
    }

    private async void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        await LoadFoodItemsAsync(e.NewTextValue);
    }

    private async void OnSearchButtonPressed(object? sender, EventArgs e)
    {
        await LoadFoodItemsAsync(SearchFoodBar.Text);
    }

    private async void OnRefreshing(object? sender, EventArgs e)
    {
        await LoadFoodItemsAsync(SearchFoodBar.Text);
        FoodRefreshView.IsRefreshing = false;
        var source = FoodCatalogService.LastLoadUsedMockApi ? "mockapi.io" : "local fallback data";
        SemanticScreenReader.Announce($"Food and drink list refreshed. Current source: {source}.");
    }

    private void ToggleAccelerometer(bool start)
    {
        try
        {
            if (Accelerometer.Default.IsSupported)
            {
                if (start && !Accelerometer.Default.IsMonitoring)
                {
                    Accelerometer.Default.ReadingChanged += OnAccelerometerReadingChanged;
                    Accelerometer.Default.Start(SensorSpeed.UI);
                }
                else if (!start && Accelerometer.Default.IsMonitoring)
                {
                    Accelerometer.Default.Stop();
                    Accelerometer.Default.ReadingChanged -= OnAccelerometerReadingChanged;
                }
            }
        }
        catch (Exception) { }
    }

    private async void OnAccelerometerReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        var data = e.Reading;
        double gForce = Math.Sqrt(data.Acceleration.X * data.Acceleration.X +
                                  data.Acceleration.Y * data.Acceleration.Y +
                                  data.Acceleration.Z * data.Acceleration.Z);

        if (gForce > ShakeThreshold && !_isShakingActive)
        {
            _isShakingActive = true;
            await HandleFoodShakeBoxAsync();
            _isShakingActive = false;
        }
    }

    private async Task HandleFoodShakeBoxAsync()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
            Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(300));

            if (FoodCollection.ItemsSource is not IReadOnlyList<FoodItem> items || items.Count == 0)
            {
                return;
            }

            var random = new Random();
            var luckyFood = items[random.Next(items.Count)];

            SemanticScreenReader.Announce($"Shake roulette selected: {luckyFood.Name}");

            bool viewDetails = await DisplayAlert(
                "🎲 Shake Roulette!",
                $"NutriBite picked a meal for you:\n\n✨ {luckyFood.Name} ({luckyFood.CaloriesLabel})",
                "View Details",
                "Close");

            if (viewDetails)
            {
                await Shell.Current.GoToAsync($"{nameof(FoodDetailPage)}?id={Uri.EscapeDataString(luckyFood.Id)}");
            }
        }
        catch (Exception) { }
    }
}