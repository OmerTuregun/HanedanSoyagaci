# 🌳 Hanedan Soy Ağacı (Dynastic Family Tree)

Bu proje, karmaşık hanedan yapılarını (birden fazla eş, tahta çıkış tarihleri, akraba evlilikleri) görselleştirmek için tasarlanmış, .NET MAUI ve SkiaSharp tabanlı bir masaüstü uygulamasıdır.

## 🚀 Özellikler

* **Özelleştirilmiş Görselleştirme:**
    * Erkekler için hafif yuvarlatılmış, Kadınlar için tam oval (kapsül) kutu tasarımları.
    * Cinsiyete göre dinamik renklendirme.
    * Kişiye özel "Hüküm Süreleri" (Reigns) gösterimi.
* **İlişki Yönetimi:**
    * Ebeveyn-Çocuk ve Eş ilişkileri için özel bağlantı çizgileri.
    * Akraba evliliklerini (döngüsel bağlar) destekleyen yapı.
* **Yüksek Performans:** SkiaSharp grafik motoru ile oluşturulan, ölçeklenebilir çizim alanı.
* **Çapraz Platform:** Windows ve macOS üzerinde çalışır (Masaüstü odaklı).

## 🛠️ Teknolojiler

* **Framework:** .NET 8.0 (MAUI)
* **Dil:** C#
* **Grafik Motoru:** SkiaSharp & SkiaSharp.Views.Maui.Controls
* **Mimari:** MVVM (Model-View-ViewModel) prensiplerine uygun katmanlı yapı.

## 📸 Ekran Görüntüleri

*(Buraya uygulamanızın ekran görüntülerini ekleyebilirsiniz)*

## ⚙️ Kurulum ve Çalıştırma

1.  Projeyi klonlayın:
    ```bash
    git clone https://github.com/OmerTuregun/HanedanSoyagaci
    ```
2.  Visual Studio 2022 ile `FamilyTreeApp.sln` dosyasını açın.
3.  `.NET MAUI` iş yükünün yüklü olduğundan emin olun.
4.  Hedef platformu "Windows Machine" seçin ve projeyi derleyin (F5).

## 🗺️ Yol Haritası (Roadmap)

- [x] Temel proje yapısı ve SkiaSharp entegrasyonu
- [x] Özel kutu şekilleri ve ilişki çizgileri
- [ ] Kullanıcı arayüzü ile kişi ekleme/düzenleme (Interactive UI)
- [ ] Tuval üzerinde Pan & Zoom (Kaydırma ve Yakınlaştırma)
- [ ] SQLite veritabanı entegrasyonu
- [ ] PNG/PDF olarak dışa aktarma

---
Geliştirici: Ömer Faruk Türegün
