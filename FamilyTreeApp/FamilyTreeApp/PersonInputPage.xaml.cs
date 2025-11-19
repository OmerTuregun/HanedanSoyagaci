using FamilyTreeApp.Models;
using SkiaSharp;

namespace FamilyTreeApp;

public partial class PersonInputPage : ContentPage
{
    private TaskCompletionSource<Person> _tcs;
    private Person _editingPerson;

    public PersonInputPage()
    {
        InitializeComponent();
    }

    // Yeni kişi ekleme için
    public static async Task<Person> Show(INavigation navigation)
    {
        var page = new PersonInputPage();
        page._tcs = new TaskCompletionSource<Person>();
        page.TitleLabel.Text = "Yeni Kişi Ekle";
        page.SaveButton.Text = "Kaydet";

        await navigation.PushModalAsync(page);

        return await page._tcs.Task;
    }

    // Mevcut kişiyi düzenleme için
    public static async Task<Person> ShowForEdit(INavigation navigation, Person person)
    {
        var page = new PersonInputPage();
        page._tcs = new TaskCompletionSource<Person>();
        page._editingPerson = person;
        
        // Mevcut bilgileri doldur
        page.TitleLabel.Text = "Kişi Düzenle";
        page.NameEntry.Text = person.Name;
        page.GenderPicker.SelectedIndex = person.Gender == Gender.Male ? 0 : 1;
        page.SaveButton.Text = "Güncelle";

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

        Person person;
        if (_editingPerson != null)
        {
            // Düzenleme modu: Mevcut kişiyi güncelle
            person = _editingPerson;
            person.Name = name;
            person.Gender = GenderPicker.SelectedIndex == 0 ? Gender.Male : Gender.Female;
        }
        else
        {
            // Yeni kişi oluştur
            person = new Person
            {
                Name = name,
                Gender = GenderPicker.SelectedIndex == 0 ? Gender.Male : Gender.Female,
                FillColor = GenderPicker.SelectedIndex == 0 ? SKColors.LightBlue : SKColors.Pink
            };
        }

        // Pencereyi kapat ve kişiyi gönder
        await Navigation.PopModalAsync();
        _tcs.SetResult(person);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        // İptal edilirse null döndür
        await Navigation.PopModalAsync();
        _tcs.SetResult(null);
    }
}
