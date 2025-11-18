using FamilyTreeApp.Models;
using SkiaSharp; // Renkler için

namespace FamilyTreeApp;

public partial class PersonInputPage : ContentPage
{
    // Veriyi geri döndürmek için bir "Task" (Görev) yapýsý kullanýyoruz
    private TaskCompletionSource<Person> _tcs;

    public PersonInputPage()
    {
        InitializeComponent();
    }

    // Sayfayý dýþarýdan çaðýrmak ve sonucu beklemek için bu metodu kullanacaðýz
    public static async Task<Person> Show(INavigation navigation)
    {
        var page = new PersonInputPage();
        page._tcs = new TaskCompletionSource<Person>();

        await navigation.PushModalAsync(page);

        return await page._tcs.Task;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        string name = NameEntry.Text;
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlert("Hata", "Lütfen bir isim giriniz.", "Tamam");
            return;
        }

        // Yeni kiþiyi oluþtur
        var person = new Person
        {
            Name = name,
            Gender = GenderPicker.SelectedIndex == 0 ? Gender.Male : Gender.Female,
            // Renkleri þimdilik burada basit atýyoruz
            FillColor = GenderPicker.SelectedIndex == 0 ? SKColors.LightBlue : SKColors.Pink
        };

        // Pencereyi kapat ve kiþiyi gönder
        await Navigation.PopModalAsync();
        _tcs.SetResult(person);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // Ýptal edilirse null döndür
        await Navigation.PopModalAsync();
        _tcs.SetResult(null);
    }
}