using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using FamilyTreeApp.Models;
using FamilyTreeApp.Drawers;
using FamilyTreeApp.Helpers; // Yeni helper'ı ekledik

namespace FamilyTreeApp;

public partial class MainPage : ContentPage
{
    // Veri Listeleri
    List<Person> people = new List<Person>();
    List<Relationship> relationships = new List<Relationship>();

    public MainPage()
    {
        InitializeComponent();
        // InitializeData(); // ARTIK BOŞ BAŞLIYORUZ, BU SATIRI SİLDİK/YORUM YAPTIK
    }

    // --- 1. ÇİZİM DÖNGÜSÜ ---
    private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        SKCanvas canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        // 1. ÖNCE YATAY (EŞ) ÇİZGİLERİNİ ÇİZ
        foreach (var rel in relationships.Where(r => r.Type == RelationshipType.Spouse))
        {
            var p1 = people.FirstOrDefault(p => p.Id == rel.FromPersonId);
            var p2 = people.FirstOrDefault(p => p.Id == rel.ToPersonId);
            if (p1 != null && p2 != null)
            {
                // Eşler arası çizgi: Sağ kenar -> Sol kenar
                float y = p1.ScreenRect.MidY;

                // X1 ve X2'yi ayarla: Sol taraftaki kutunun sağı ile sağ taraftaki kutunun solu arasında çiz
                float x1 = Math.Min(p1.ScreenRect.Right, p2.ScreenRect.Right);
                float x2 = Math.Max(p1.ScreenRect.Right, p2.ScreenRect.Right);

                if (p1.ScreenRect.Left < p2.ScreenRect.Left)
                {
                    x1 = p1.ScreenRect.Right;
                    x2 = p2.ScreenRect.Left;
                }
                else
                {
                    x1 = p2.ScreenRect.Right;
                    x2 = p1.ScreenRect.Left;
                }

                LineDrawer.DrawConnection(canvas, x1, y, x2, y);
            }
        }

        // 2. SONRA DİKEY (ÇOCUK) ÇİZGİLERİNİ ÇİZ
        foreach (var person in people)
        {
            // Bu kişinin (çocuğun) ebeveyn ilişkilerini bul
            var parentRelations = relationships.Where(r => r.ToPersonId == person.Id && r.Type == RelationshipType.ParentChild).ToList();

            // Eğer ebeveyn ilişkisi varsa, ilgili kişileri (parents) bul
            if (parentRelations.Count > 0)
            {
                // **parents** listesi burada tanımlanır.
                var parents = people.Where(p => parentRelations.Any(r => r.FromPersonId == p.Id)).ToList();

                // Sadece iki ebeveynli çocukları (Evlilik çocuklarını) çiz
                if (parents.Count >= 2)
                {
                    var p1 = parents[0];
                    var p2 = parents[1];

                    // X-Midpoint: İki ebeveynin yatay orta noktası
                    float parentMidX = (p1.ScreenRect.MidX + p2.ScreenRect.MidX) / 2;
                    // Y-Midpoint: Ebeveynlerin dikey orta noktası (Evlilik çizgisi seviyesi)
                    float marriageY = p1.ScreenRect.MidY;

                    // --- T-KAVŞAĞI MANTIK NOKTALARI ---

                    // T-Kavşağının Y seviyesi (Evlilik çizgisinden 60px aşağı)
                    float junctionY = marriageY + BoxDrawer.Height;

                    // 1. AŞAMA: KISA DİKEY İNİŞ (Evlilik çizgisi seviyesinden T-Kavşağı'na)
                    // DİKEY: X sabit.
                    LineDrawer.DrawConnection(canvas, parentMidX, marriageY, parentMidX, junctionY);

                    // 2. AŞAMA: YATAY TAŞIMA (T-Kavşağı'ndan Çocuğun merkezine yatay kayma)
                    // YATAY: Y sabit (junctionY). Bu, çizginin dik açıyla çocuğa ulaşmasını sağlar.
                    LineDrawer.DrawConnection(canvas, parentMidX, junctionY, person.ScreenRect.MidX, junctionY);

                    // 3. AŞAMA: ANA DİKEY İNİŞ (Yatay Kayma Noktasından Çocuğun kutusuna)
                    // DİKEY: X sabit.
                    LineDrawer.DrawConnection(canvas, person.ScreenRect.MidX, junctionY, person.ScreenRect.MidX, person.ScreenRect.Top);
                }
            }
        }

        // 3. KUTULARI ÇİZ
        foreach (var person in people)
        {
            BoxDrawer.Draw(canvas, person, person.ScreenRect.Left, person.ScreenRect.Top);
        }
    }

    // --- 2. DOKUNMA ETKİLEŞİMİ (MENÜLÜ SİSTEM) ---
    private async void OnCanvasViewTouch(object sender, SKTouchEventArgs e)
    {
        if (e.ActionType == SKTouchAction.Pressed)
        {
            float touchX = e.Location.X;
            float touchY = e.Location.Y;

            // Hangi kişiye tıklandı?
            var selectedPerson = people.FirstOrDefault(p => p.ScreenRect.Contains(touchX, touchY));

            if (selectedPerson != null)
            {
                // KİŞİYE TIKLANDI: Seçenekleri Göster
                string action = await DisplayActionSheet($"{selectedPerson.Name} için işlem seçin:", "İptal", null, "Düzenle", "İlişkili Kişi Ekle");

                if (action == "İlişkili Kişi Ekle")
                {
                    await AddRelatedPersonFlow(selectedPerson);
                }
                else if (action == "Düzenle")
                {
                    await DisplayAlert("Bilgi", "Düzenleme sayfası yakında eklenecek.", "Tamam");
                }
            }

        }
        e.Handled = true;
    }

    // --- 3. YENİ KİŞİ EKLEME AKIŞI ---
    private async Task AddRelatedPersonFlow(Person sourcePerson)
    {
        string action = await DisplayActionSheet("Ne Eklemek İstersin?", "İptal", null, "Eş", "Çocuk");
        if (action == "İptal" || action == null) return;

        // --- EŞ EKLEME ---
        if (action == "Eş")
        {
            var newSpouse = await PersonInputPage.Show(Navigation);
            if (newSpouse == null) return;

            // Eşin konumunu hesapla (LayoutHelper sağa veya sola koyacak)
            newSpouse.ScreenRect = LayoutHelper.CalculatePosition(sourcePerson, RelationshipType.Spouse, people, relationships);

            people.Add(newSpouse);
            relationships.Add(new Relationship { FromPersonId = sourcePerson.Id, ToPersonId = newSpouse.Id, Type = RelationshipType.Spouse });
        }
        // --- ÇOCUK EKLEME (ZOR KISIM) ---
        else if (action == "Çocuk")
        {
            // 1. Önce bu kişinin eşi var mı kontrol et
            var spouseIds = relationships
                .Where(r => (r.FromPersonId == sourcePerson.Id || r.ToPersonId == sourcePerson.Id) && r.Type == RelationshipType.Spouse)
                .Select(r => r.FromPersonId == sourcePerson.Id ? r.ToPersonId : r.FromPersonId)
                .ToList();

            if (spouseIds.Count == 0)
            {
                await DisplayAlert("Hata", "Çocuk eklemek için önce bir eş eklemelisiniz.", "Tamam");
                return;
            }

            // 2. Diğer ebeveyni seçtir
            Person otherParent = null;
            if (spouseIds.Count == 1)
            {
                // Tek eş varsa direkt onu seç
                otherParent = people.FirstOrDefault(p => p.Id == spouseIds[0]);
            }
            else
            {
                // Birden fazla eş varsa sor (İsimleri listele)
                var spouseNames = people.Where(p => spouseIds.Contains(p.Id)).Select(p => p.Name).ToArray();
                string selectedSpouseName = await DisplayActionSheet("Diğer Ebeveyn Kim?", "İptal", null, spouseNames);
                if (selectedSpouseName == "İptal" || selectedSpouseName == null) return;

                otherParent = people.FirstOrDefault(p => p.Name == selectedSpouseName);
            }

            if (otherParent == null) return;

            // 3. Çocuğun bilgilerini gir
            var newChild = await PersonInputPage.Show(Navigation);
            if (newChild == null) return;

            // 4. Konum Hesapla (Babanın altına koy şimdilik)
            // Not: Burada 'ParentChild' tipiyle hesaplatıyoruz.
            newChild.ScreenRect = LayoutHelper.CalculatePosition(sourcePerson, RelationshipType.ParentChild, people, relationships);

            // 5. Veritabanına Ekle (Çocuk HER İKİSİYLE DE İLİŞKİLENDİRİLİR)
            people.Add(newChild);

            // İlişki 1: Kaynak Kişi -> Çocuk
            relationships.Add(new Relationship { FromPersonId = sourcePerson.Id, ToPersonId = newChild.Id, Type = RelationshipType.ParentChild });
            // İlişki 2: Diğer Ebeveyn -> Çocuk
            relationships.Add(new Relationship { FromPersonId = otherParent.Id, ToPersonId = newChild.Id, Type = RelationshipType.ParentChild });
        }

        canvasView.InvalidateSurface();
    }

    // --- 4. İLK KİŞİYİ EKLEME (SAĞ ALT BUTON) ---
    private async void OnAddIndependentPersonClicked(object sender, EventArgs e)
    {
        // Formu aç
        var newPerson = await PersonInputPage.Show(Navigation);

        if (newPerson != null)
        {
            // Ekranın ortasına yerleştir
            var info = canvasView.CanvasSize; // Tuval boyutunu al (Piksel cinsinden)
            // Not: CanvasSize bazen DPI'a göre büyük gelebilir, basitleştirilmiş orta nokta:
            newPerson.ScreenRect = LayoutHelper.CalculateCenterPosition(500, 500);

            people.Add(newPerson);
            canvasView.InvalidateSurface();
        }
    }

    // --- YENİ HELPER METOTLARI ---

    // Mevcut kişilerden seçim yapılmasını sağlayan yardımcı
    private async Task<Person?> ShowPersonSelectionAsync(string title, IEnumerable<Person> candidates)
    {
        var names = candidates.Select(p => p.Name).ToArray();
        string selectedName = await DisplayActionSheet(title, "İptal", null, names);

        if (selectedName == "İptal" || selectedName == null) return null;
        return candidates.FirstOrDefault(p => p.Name == selectedName);
    }

    // İlişkili Ekleme Akışını Yöneten
    private async Task AddRelatedPersonFlow()
    {
        // 1. Kime bağlı olacağını sor (Tüm mevcut kişileri listele)
        var candidates = people.ToList();
        if (!candidates.Any())
        {
            await DisplayAlert("Hata", "Ağaçta henüz kimse yok. Lütfen önce Bağımsız Kişi ekleyiniz.", "Tamam");
            return;
        }

        var sourcePerson = await ShowPersonSelectionAsync("Kiminle ilişkilendirilecek?", candidates);
        if (sourcePerson == null) return;

        // 2. Ne tür bir ilişki olacağını sor
        string action = await DisplayActionSheet($"{sourcePerson.Name} için ne ekleyelim?", "İptal", null, "Eş", "Çocuk");
        if (action == "İptal" || action == null) return;

        // 3. Eş Veya Çocuk Ekleme İşlemini Başlat
        if (action == "Eş")
        {
            await AddSpouseViaFlow(sourcePerson);
        }
        else if (action == "Çocuk")
        {
            await AddChildViaFlow(sourcePerson);
        }
    }


    // --- YENİ BUTON İŞLEMLERİ (BAĞIMSIZ VEYA İLİŞKİLİ) ---

    // Sağ alttaki (+) butonu burayı tetikler
    private async void OnAddPersonClicked(object sender, EventArgs e)
    {
        string choice = await DisplayActionSheet("Ne eklemek istersiniz?", "İptal", null, "Bağımsız Kişi (Kök)", "İlişkili Kişi");

        if (choice == "Bağımsız Kişi (Kök)")
        {
            var newPerson = await PersonInputPage.Show(Navigation);
            if (newPerson == null) return;

            // Ekranın ortasına yerleştir
            var info = canvasView.CanvasSize;
            newPerson.ScreenRect = LayoutHelper.CalculateCenterPosition(info.Width, info.Height);

            people.Add(newPerson);
            canvasView.InvalidateSurface();
        }
        else if (choice == "İlişkili Kişi")
        {
            await AddRelatedPersonFlow();
        }
    }


    // --- YENİ İLİŞKİ EKLEME MANTIKLARI ---

    private async Task AddSpouseViaFlow(Person sourcePerson)
    {
        var newSpouse = await PersonInputPage.Show(Navigation);
        if (newSpouse == null) return;

        // Konumu Hesapla (LayoutHelper)
        newSpouse.ScreenRect = LayoutHelper.CalculatePosition(sourcePerson, RelationshipType.Spouse, people, relationships);

        people.Add(newSpouse);
        relationships.Add(new Relationship { FromPersonId = sourcePerson.Id, ToPersonId = newSpouse.Id, Type = RelationshipType.Spouse });
        canvasView.InvalidateSurface();
    }

    private async Task AddChildViaFlow(Person sourcePerson)
    {
        // 1. Eşleri bul
        var spouseIds = relationships
            .Where(r => (r.FromPersonId == sourcePerson.Id || r.ToPersonId == sourcePerson.Id) && r.Type == RelationshipType.Spouse)
            .Select(r => r.FromPersonId == sourcePerson.Id ? r.ToPersonId : r.FromPersonId)
            .ToList();

        var existingSpouses = people.Where(p => spouseIds.Contains(p.Id)).ToList();

        if (!existingSpouses.Any())
        {
            await DisplayAlert("Kural Hatası", $"{sourcePerson.Name}'ın çocuğu olması için önce bir eşi olmalı.", "Tamam");
            return;
        }

        // 2. Diğer ebeveyni seçtir
        Person coParent = null;
        if (existingSpouses.Count == 1)
        {
            coParent = existingSpouses[0];
        }
        else
        {
            coParent = await ShowPersonSelectionAsync("Çocuğun diğer ebeveyni kim?", existingSpouses);
            if (coParent == null) return;
        }

        // 3. Çocuğun bilgilerini gir
        var newChild = await PersonInputPage.Show(Navigation);
        if (newChild == null) return;

        // 4. Konumu Hesapla
        newChild.ScreenRect = LayoutHelper.CalculatePosition(sourcePerson, RelationshipType.ParentChild, people, relationships);

        // 5. Veritabanına Ekle (Çocuk HER İKİSİYLE DE İLİŞKİLENDİRİLİR)
        people.Add(newChild);
        relationships.Add(new Relationship { FromPersonId = sourcePerson.Id, ToPersonId = newChild.Id, Type = RelationshipType.ParentChild });
        relationships.Add(new Relationship { FromPersonId = coParent.Id, ToPersonId = newChild.Id, Type = RelationshipType.ParentChild });
        canvasView.InvalidateSurface();
    }
}