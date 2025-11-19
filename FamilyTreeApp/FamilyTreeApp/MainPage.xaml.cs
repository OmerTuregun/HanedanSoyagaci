using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using FamilyTreeApp.Models;
using FamilyTreeApp.Drawers;
using FamilyTreeApp.Helpers;
using FamilyTreeApp.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui.ApplicationModel.DataTransfer;

namespace FamilyTreeApp;

public partial class MainPage : ContentPage
{
    // Veri Listeleri
    List<Person> people = new List<Person>();
    List<Relationship> relationships = new List<Relationship>();
    
    // DataService
    private DataService _dataService;

    // PAN & ZOOM: SKMatrix ile transform
    private SKMatrix _currentMatrix = SKMatrix.CreateIdentity();
    private SKPoint _lastPanPoint;
    private bool _isPanning = false;
    private const float MinZoom = 0.5f;
    private const float MaxZoom = 3.0f;
    private const float ZoomStep = 0.1f;

    public MainPage()
    {
        InitializeComponent();
        
        // DataService'i başlat (Connection string'i buradan veya appsettings'den alabilirsiniz)
        _dataService = new DataService(DataService.DefaultConnectionString);
        
        // Uygulama başlatılırken verileri yükle
        Loaded += MainPage_Loaded;
        
        // Windows için mouse wheel desteği
#if WINDOWS
        Loaded += MainPage_LoadedWindows;
#endif
    }

    private async void MainPage_Loaded(object sender, EventArgs e)
    {
        await LoadTreeFromDatabaseAsync();
    }

#if WINDOWS
    private void MainPage_LoadedWindows(object sender, EventArgs e)
    {
        // Windows'ta mouse wheel event'ini yakala
        if (this.canvasView.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement uiElement)
        {
            uiElement.PointerWheelChanged += OnWindowsPointerWheelChanged;
        }
    }
#endif

#if WINDOWS
    private void OnWindowsPointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(this.canvasView.Handler?.PlatformView as Microsoft.UI.Xaml.UIElement);
        SKPoint zoomCenter = new SKPoint((float)point.Position.X, (float)point.Position.Y);
        
        // Delta değeri: pozitif yukarı (zoom in), negatif aşağı (zoom out)
        float delta = (float)point.Properties.MouseWheelDelta;
        float zoomFactor = delta > 0 ? (1 + ZoomStep * 2) : (1 - ZoomStep * 2);
        
        ApplyZoom(zoomFactor, zoomCenter);
        e.Handled = true;
    }
#endif

    // --- 1. ÇİZİM DÖNGÜSÜ ---
    private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
    {
        SKCanvas canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        // PAN & ZOOM: Transform matrisini uygula
        canvas.SetMatrix(_currentMatrix);

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

                LineDrawer.DrawConnection(canvas, x1, y, x2, y, rel.IsUncertain);
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

                    // İlişkinin belirsiz olup olmadığını kontrol et
                    var parentRel = relationships.FirstOrDefault(r => 
                        (r.FromPersonId == p1.Id || r.FromPersonId == p2.Id) && 
                        r.ToPersonId == person.Id && 
                        r.Type == RelationshipType.ParentChild);
                    bool isUncertain = parentRel?.IsUncertain ?? false;

                    // 1. AŞAMA: KISA DİKEY İNİŞ (Evlilik çizgisi seviyesinden T-Kavşağı'na)
                    // DİKEY: X sabit.
                    LineDrawer.DrawConnection(canvas, parentMidX, marriageY, parentMidX, junctionY, isUncertain);

                    // 2. AŞAMA: YATAY TAŞIMA (T-Kavşağı'ndan Çocuğun merkezine yatay kayma)
                    // YATAY: Y sabit (junctionY). Bu, çizginin dik açıyla çocuğa ulaşmasını sağlar.
                    LineDrawer.DrawConnection(canvas, parentMidX, junctionY, person.ScreenRect.MidX, junctionY, isUncertain);

                    // 3. AŞAMA: ANA DİKEY İNİŞ (Yatay Kayma Noktasından Çocuğun kutusuna)
                    // DİKEY: X sabit.
                    LineDrawer.DrawConnection(canvas, person.ScreenRect.MidX, junctionY, person.ScreenRect.MidX, person.ScreenRect.Top, isUncertain);
                }
            }
        }

        // 3. KUTULARI ÇİZ
        foreach (var person in people)
        {
            BoxDrawer.Draw(canvas, person, person.ScreenRect.Left, person.ScreenRect.Top);
        }
    }

    // --- SİLİNMİŞ KISIM: Eski OnCanvasViewTouch metodunun yerine gelen yeni olaylar ---

    // MAUI Pointer Olayları (Pan ve Tıklama)
    private void OnPointerPressed(object sender, PointerEventArgs e)
    {
        // Tıklama başlangıç noktası ve Pan başlatma
        var position = e.GetPosition(this.canvasView);
        if (position.HasValue)
        {
            _lastPanPoint = new SKPoint((float)position.Value.X, (float)position.Value.Y);
            _isPanning = true;
        }
    }

    private void OnPointerReleased(object sender, PointerEventArgs e)
    {
        var position = e.GetPosition(this.canvasView);
        if (position.HasValue)
        {
            var currentPos = new SKPoint((float)position.Value.X, (float)position.Value.Y);
            
            // Tıklama (Click) olarak değerlendirilen olay
            if (Math.Abs(currentPos.X - _lastPanPoint.X) < 5 &&
                Math.Abs(currentPos.Y - _lastPanPoint.Y) < 5)
            {
                HandleTap(currentPos);
            }
        }
        
        _isPanning = false;
        this.canvasView.InvalidateSurface();
    }

    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        if (!_isPanning) return;

        // Kaydırma (PAN) mantığı
        var position = e.GetPosition(this.canvasView);
        if (!position.HasValue) return;
        
        var currentPoint = new SKPoint((float)position.Value.X, (float)position.Value.Y);

        float deltaX = currentPoint.X - _lastPanPoint.X;
        float deltaY = currentPoint.Y - _lastPanPoint.Y;

        // Pan matrisini güncelle
        SKMatrix translation = SKMatrix.CreateTranslation(deltaX, deltaY);
        _currentMatrix = _currentMatrix.PreConcat(translation);

        _lastPanPoint = currentPoint;
        this.canvasView.InvalidateSurface();
    }

    // --- TIKLAMA VE MENÜ YÖNETİMİ ---
    private async void HandleTap(SKPoint screenPoint)
    {
        // World Point: Ekrandaki tıklama noktasını, kaydırılmış tuval uzayına dönüştür
        SKPoint worldPoint = _currentMatrix.Invert().MapPoint(screenPoint);

        // Hangi kişiye tıklandı?
        var selectedPerson = people.FirstOrDefault(p => p.ScreenRect.Contains(worldPoint.X, worldPoint.Y));

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
                await EditPersonFlow(selectedPerson);
            }
        }
        this.canvasView.InvalidateSurface();
    }

    // --- ZOOM MANTIĞI ---
    private void ApplyZoom(float zoomFactor, SKPoint zoomCenter)
    {
        // Mevcut zoom seviyesini kontrol et
        float currentScale = _currentMatrix.ScaleX;
        float newScale = currentScale * zoomFactor;
        
        // Zoom limitleri kontrolü
        if (newScale < MinZoom || newScale > MaxZoom)
            return;

        // Zoom işlemi: Merkez noktadan zoom
        // 1. Zoom merkezini orijine taşı
        SKMatrix translateToOrigin = SKMatrix.CreateTranslation(-zoomCenter.X, -zoomCenter.Y);
        
        // 2. Zoom uygula
        SKMatrix scale = SKMatrix.CreateScale(zoomFactor, zoomFactor);
        
        // 3. Zoom merkezini geri taşı
        SKMatrix translateBack = SKMatrix.CreateTranslation(zoomCenter.X, zoomCenter.Y);
        
        // Matrisleri birleştir
        SKMatrix zoomMatrix = translateToOrigin
            .PostConcat(scale)
            .PostConcat(translateBack);
        
        _currentMatrix = _currentMatrix.PostConcat(zoomMatrix);
        
        this.canvasView.InvalidateSurface();
    }

    // --- 3. YENİ KİŞİ EKLEME AKIŞI ---
    private async Task AddRelatedPersonFlow(Person sourcePerson)
    {
        string action = await DisplayActionSheet("Ne Eklemek İstersin?", "İptal", null, "Eş", "Çocuk");
        if (action == "İptal" || action == null) return;

        // Eş veya çocuk ekleme işlemlerini ilgili metotlara yönlendir
        if (action == "Eş")
        {
            await AddSpouseViaFlow(sourcePerson);
        }
        else if (action == "Çocuk")
        {
            await AddChildViaFlow(sourcePerson);
        }
    }

    // --- 4. İLK KİŞİYİ EKLEME (SAĞ ALT BUTON) ---
    private async void OnAddIndependentPersonClicked(object sender, EventArgs e)
    {
        // Formu aç
        var newPerson = await PersonInputPage.Show(Navigation);

        if (newPerson != null)
        {
            // Ekranın ortasına yerleştir
            var info = this.canvasView.CanvasSize; // Tuval boyutunu al (Piksel cinsinden)
            // Not: CanvasSize bazen DPI'a göre büyük gelebilir, basitleştirilmiş orta nokta:
            newPerson.ScreenRect = LayoutHelper.CalculateCenterPosition(500, 500);

            people.Add(newPerson);
            
            try
            {
                await _dataService.SavePersonAsync(newPerson);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Kişi kaydetme hatası: {ex.Message}", "Tamam");
                people.Remove(newPerson);
            }
            
            this.canvasView.InvalidateSurface();
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
            var info = this.canvasView.CanvasSize;
            newPerson.ScreenRect = LayoutHelper.CalculateCenterPosition(info.Width, info.Height);

            people.Add(newPerson);
            
            try
            {
                await _dataService.SavePersonAsync(newPerson);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Kişi kaydetme hatası: {ex.Message}", "Tamam");
                people.Remove(newPerson);
            }
            
            this.canvasView.InvalidateSurface();
        }
        else if (choice == "İlişkili Kişi")
        {
            await AddRelatedPersonFlow();
        }
    }


    // --- YENİ İLİŞKİ EKLEME MANTIKLARI ---

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
        
        // Veritabanına kaydet
        try
        {
            await _dataService.SavePersonAsync(newChild);
            await _dataService.SaveRelationshipAsync(new Relationship { FromPersonId = sourcePerson.Id, ToPersonId = newChild.Id, Type = RelationshipType.ParentChild });
            await _dataService.SaveRelationshipAsync(new Relationship { FromPersonId = coParent.Id, ToPersonId = newChild.Id, Type = RelationshipType.ParentChild });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Veritabanı kayıt hatası: {ex.Message}", "Tamam");
        }
        
        this.canvasView.InvalidateSurface();
    }

    // --- VERİTABANI İŞLEMLERİ ---
    
    private async Task LoadTreeFromDatabaseAsync()
    {
        try
        {
            // Veritabanını başlat (tabloları oluştur)
            await _dataService.InitializeDatabaseAsync();
            
            // Verileri yükle
            var (loadedPeople, loadedRelationships) = await _dataService.LoadTreeAsync();
            
            people = loadedPeople;
            relationships = loadedRelationships;
            
            // Ekran pozisyonlarını hesapla (basit bir yerleşim)
            if (people.Count > 0)
            {
                var info = this.canvasView.CanvasSize;
                float startX = 100;
                float startY = 100;
                float spacing = 150;
                
                foreach (var person in people)
                {
                    if (person.ScreenRect.IsEmpty)
                    {
                        person.ScreenRect = new SKRect(startX, startY, startX + BoxDrawer.Width, startY + BoxDrawer.Height);
                        startX += spacing;
                    }
                }
            }
            
            this.canvasView.InvalidateSurface();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Veritabanı yükleme hatası: {ex.Message}\n\nLütfen MySQL bağlantı ayarlarını kontrol edin.", "Tamam");
        }
    }

    // --- KİŞİ DÜZENLEME AKIŞI ---
    
    private async Task EditPersonFlow(Person person)
    {
        var editedPerson = await PersonInputPage.ShowForEdit(Navigation, person);
        if (editedPerson == null) return;
        
        try
        {
            await _dataService.UpdatePersonAsync(editedPerson);
            this.canvasView.InvalidateSurface();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Kişi güncelleme hatası: {ex.Message}", "Tamam");
        }
    }

    // --- EŞ EKLEME AKIŞI (GÜNCELLENMİŞ: MEVCUT KİŞİ SEÇİMİ EKLENDİ) ---
    
    private async Task AddSpouseViaFlow(Person sourcePerson)
    {
        // Yeni eş mi, mevcut kişi mi?
        string choice = await DisplayActionSheet("Eş ekleme seçeneği:", "İptal", null, "Yeni Kişi Ekle", "Ağaçtan Mevcut Kişiyi Seç");
        
        if (choice == "İptal" || choice == null) return;
        
        Person newSpouse = null;
        
        if (choice == "Yeni Kişi Ekle")
        {
            newSpouse = await PersonInputPage.Show(Navigation);
            if (newSpouse == null) return;
            
            // Eşin konumunu hesapla
            newSpouse.ScreenRect = LayoutHelper.CalculatePosition(sourcePerson, RelationshipType.Spouse, people, relationships);
            people.Add(newSpouse);
            
            try
            {
                await _dataService.SavePersonAsync(newSpouse);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Hata", $"Kişi kaydetme hatası: {ex.Message}", "Tamam");
                people.Remove(newSpouse);
                return;
            }
        }
        else if (choice == "Ağaçtan Mevcut Kişiyi Seç")
        {
            // Mevcut eşleri ve kendisini hariç tut
            var spouseIds = relationships
                .Where(r => (r.FromPersonId == sourcePerson.Id || r.ToPersonId == sourcePerson.Id) && r.Type == RelationshipType.Spouse)
                .Select(r => r.FromPersonId == sourcePerson.Id ? r.ToPersonId : r.FromPersonId)
                .ToList();
            
            var availablePeople = people
                .Where(p => p.Id != sourcePerson.Id && !spouseIds.Contains(p.Id))
                .ToList();
            
            if (!availablePeople.Any())
            {
                await DisplayAlert("Bilgi", "Ağaçta seçilebilecek başka kişi yok.", "Tamam");
                return;
            }
            
            newSpouse = await ShowPersonSelectionAsync("Eş olarak seçilecek kişi:", availablePeople);
            if (newSpouse == null) return;
        }
        
        // İlişkiyi kaydet
        var relationship = new Relationship { FromPersonId = sourcePerson.Id, ToPersonId = newSpouse.Id, Type = RelationshipType.Spouse };
        relationships.Add(relationship);
        
        try
        {
            await _dataService.SaveRelationshipAsync(relationship);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"İlişki kaydetme hatası: {ex.Message}", "Tamam");
            relationships.Remove(relationship);
            return;
        }
        
        this.canvasView.InvalidateSurface();
    }

    // --- PNG DIŞA AKTARMA (EXPORT) ---
    
    private async void OnExportClicked(object sender, EventArgs e)
    {
        try
        {
            // Kullanıcıdan dosya konumu seçtir
            string fileName = $"FamilyTree_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            
            // MAUI FilePicker kullan (SaveAsync yok, PickAsync kullan ve sonra kaydet)
            var customFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".png" } },
                    { DevicePlatform.Android, new[] { "image/png" } },
                    { DevicePlatform.iOS, new[] { "public.png" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.png" } }
                });
            
            // Geçici olarak dosyayı oluştur, sonra kullanıcıya göster
            string tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await ExportToPngAsync(tempPath);
            
            // Kullanıcıya dosyayı paylaş veya kaydet seçeneği sun
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Ağaç şemasını kaydet",
                File = new ShareFile(tempPath)
            });
            
            // Alternatif: Doğrudan Documents klasörüne kaydet
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string finalPath = Path.Combine(documentsPath, fileName);
            await ExportToPngAsync(finalPath);
            await DisplayAlert("Başarılı", $"Ağaç şeması kaydedildi:\n{finalPath}", "Tamam");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Dışa aktarma hatası: {ex.Message}", "Tamam");
        }
    }
    
    private async Task ExportToPngAsync(string filePath)
    {
        // Canvas boyutunu al
        var info = this.canvasView.CanvasSize;
        int width = (int)info.Width;
        int height = (int)info.Height;
        
        // Yüksek çözünürlük için scale faktörü
        float scale = 2.0f; // 2x çözünürlük
        int exportWidth = (int)(width * scale);
        int exportHeight = (int)(height * scale);
        
        // Yeni surface oluştur
        using (var surface = SKSurface.Create(exportWidth, exportHeight, SKColorType.Rgba8888, SKAlphaType.Premul))
        {
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            
            // Transform matrisini uygula (scale ile)
            canvas.SetMatrix(_currentMatrix.PostConcat(SKMatrix.CreateScale(scale, scale)));
            
            // Çizim işlemlerini tekrarla
            DrawTreeToCanvas(canvas);
            
            // PNG olarak kaydet
            using (var image = surface.Snapshot())
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(filePath))
            {
                data.SaveTo(stream);
            }
        }
    }
    
    private Task DrawTreeToCanvas(SKCanvas canvas)
    {
        // Çizim mantığını buraya kopyala (OnCanvasViewPaintSurface'dan)
        // 1. Eş çizgileri
        foreach (var rel in relationships.Where(r => r.Type == RelationshipType.Spouse))
        {
            var p1 = people.FirstOrDefault(p => p.Id == rel.FromPersonId);
            var p2 = people.FirstOrDefault(p => p.Id == rel.ToPersonId);
            if (p1 != null && p2 != null)
            {
                float y = p1.ScreenRect.MidY;
                float x1, x2;
                
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
                
                LineDrawer.DrawConnection(canvas, x1, y, x2, y, rel.IsUncertain);
            }
        }
        
        // 2. Çocuk çizgileri
        foreach (var person in people)
        {
            var parentRelations = relationships.Where(r => r.ToPersonId == person.Id && r.Type == RelationshipType.ParentChild).ToList();
            if (parentRelations.Count > 0)
            {
                var parents = people.Where(p => parentRelations.Any(r => r.FromPersonId == p.Id)).ToList();
                if (parents.Count >= 2)
                {
                    var p1 = parents[0];
                    var p2 = parents[1];
                    float parentMidX = (p1.ScreenRect.MidX + p2.ScreenRect.MidX) / 2;
                    float marriageY = p1.ScreenRect.MidY;
                    float junctionY = marriageY + BoxDrawer.Height;
                    
                    var parentRel = relationships.FirstOrDefault(r => 
                        (r.FromPersonId == p1.Id || r.FromPersonId == p2.Id) && 
                        r.ToPersonId == person.Id && 
                        r.Type == RelationshipType.ParentChild);
                    bool isUncertain = parentRel?.IsUncertain ?? false;
                    
                    LineDrawer.DrawConnection(canvas, parentMidX, marriageY, parentMidX, junctionY, isUncertain);
                    LineDrawer.DrawConnection(canvas, parentMidX, junctionY, person.ScreenRect.MidX, junctionY, isUncertain);
                    LineDrawer.DrawConnection(canvas, person.ScreenRect.MidX, junctionY, person.ScreenRect.MidX, person.ScreenRect.Top, isUncertain);
                }
            }
        }
        
        // 3. Kutular
        foreach (var person in people)
        {
            BoxDrawer.Draw(canvas, person, person.ScreenRect.Left, person.ScreenRect.Top);
        }
        
        return Task.CompletedTask;
    }
}