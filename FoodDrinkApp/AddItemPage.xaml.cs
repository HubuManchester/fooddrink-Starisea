using FoodDrinkApp.Models;
using FoodDrinkApp.Services;

namespace FoodDrinkApp;

public partial class AddItemPage : ContentPage
{
    private string _capturedPhotoPath = string.Empty;

    public AddItemPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AccessibilityService.ApplyFontScale(this);
    }

    public async void OnSaveClicked(object? sender, EventArgs e)
    {
        try
        {
            var validationMessage = ValidateForm(out var calories, out var protein, out var carbs, out var fat);
            if (validationMessage is not null)
            {
                ShowValidation(validationMessage);
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(250));
                return;
            }

            var item = new FoodItem
            {
                Name = NameEntry.Text!.Trim(),
                Category = CategoryPicker.SelectedItem?.ToString() ?? "Snack",
                Description = DescriptionEditor.Text!.Trim(),
                Calories = calories,
                Protein = protein,
                Carbs = carbs,
                Fat = fat,
                AllergyNote = string.IsNullOrWhiteSpace(AllergyEntry.Text)
                    ? "No allergy note provided."
                    : AllergyEntry.Text.Trim(),
                ImagePath = _capturedPhotoPath,
                LocationName = FormLocationEntry.Text ?? string.Empty,
                Tags = $"{NameEntry.Text} {CategoryPicker.SelectedItem} {DescriptionEditor.Text} {FormLocationEntry.Text}"
            };

            await FoodCatalogService.AddAsync(item);
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            SemanticScreenReader.Announce("Food record saved.");

            await DisplayAlert(
                "Saved",
                MockApiConfig.IsConfigured
                    ? "The record has been saved to mockapi.io."
                    : "The record has been saved to local fallback data.",
                "OK");

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            ShowValidation($"The record could not be saved: {ex.Message}");
        }
    }

    private string? ValidateForm(out int calories, out int protein, out int carbs, out int fat)
    {
        calories = protein = carbs = fat = 0;

        if (string.IsNullOrWhiteSpace(NameEntry.Text))
        {
            return "Please enter a food or drink name.";
        }

        if (CategoryPicker.SelectedIndex < 0)
        {
            return "Please choose a category.";
        }

        if (string.IsNullOrWhiteSpace(DescriptionEditor.Text))
        {
            return "Please add a short description.";
        }

        return TryReadNumber(CaloriesEntry.Text, "calories", out calories)
            ?? TryReadNumber(ProteinEntry.Text, "protein", out protein)
            ?? TryReadNumber(CarbsEntry.Text, "carbs", out carbs)
            ?? TryReadNumber(FatEntry.Text, "fat", out fat);
    }

    private static string? TryReadNumber(string? value, string fieldName, out int number)
    {
        if (int.TryParse(value, out number) && number >= 0)
        {
            return null;
        }

        return $"Please enter a valid non-negative number for {fieldName}.";
    }

    private void ShowValidation(string message)
    {
        ValidationLabel.Text = message;
        ValidationPanel.IsVisible = true;
        SemanticScreenReader.Announce(message);
    }

    
    public async void OnFormTakePhotoClicked(object? sender, EventArgs e)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                ShowValidation("Camera capture is not supported on this device.");
                return;
            }

            FileResult photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo != null)
            {
                string localFilePath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
                using Stream sourceStream = await photo.OpenReadAsync();
                using FileStream localFileStream = File.OpenWrite(localFilePath);
                await sourceStream.CopyToAsync(localFileStream);

                _capturedPhotoPath = localFilePath;
                FormImagePreview.Source = ImageSource.FromFile(localFilePath);
                FormImagePreview.IsVisible = true;

                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                SemanticScreenReader.Announce("Food photo attached successfully.");
            }
        }
        catch (PermissionException)
        {
            ShowValidation("Camera permission denied.");
        }
        catch (Exception ex)
        {
            ShowValidation($"Camera error: {ex.Message}");
        }
    }

    public void OnFormClearPhotoClicked(object? sender, EventArgs e)
    {
        _capturedPhotoPath = string.Empty;
        FormImagePreview.IsVisible = false;
        FormImagePreview.Source = null;
        SemanticScreenReader.Announce("Photo removed.");
    }

   
    public async void OnFormLocateClicked(object? sender, EventArgs e)
    {
        try
        {
            FormLocationEntry.Text = "Locating via satellites...";
            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location is null)
            {
                FormLocationEntry.Text = "GPS context unavailable.";
                return;
            }

            var placemarks = await Geocoding.Default.GetPlacemarksAsync(location);
            var placemark = placemarks?.FirstOrDefault();

            if (placemark is not null)
            {
                FormLocationEntry.Text = $"{placemark.Locality ?? placemark.AdminArea}, {placemark.Thoroughfare ?? "Nearby"}";
            }
            else
            {
                FormLocationEntry.Text = $"Lat: {location.Latitude:F3}, Lng: {location.Longitude:F3}";
            }

            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
            SemanticScreenReader.Announce("Location sync complete.");
        }
        catch (PermissionException)
        {
            FormLocationEntry.Text = string.Empty;
            ShowValidation("Location permission denied.");
        }
        catch (Exception ex)
        {
            FormLocationEntry.Text = string.Empty;
            ShowValidation($"GPS error: {ex.Message}");
        }
    }
}