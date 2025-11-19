# MySQL Veritabanı Kurulum Rehberi

## 🐳 Docker ile MySQL Kurulumu

### 1. Docker Compose ile Başlatma

Sunucuda (194.146.50.83) aşağıdaki komutları çalıştırın:

```bash
# Proje klasörüne git
cd /path/to/HanedanSoyagaci

# Docker Compose ile container'ı başlat
docker-compose -f docker-compose.mysql.yml up -d

# Container'ın çalıştığını kontrol et
docker ps | grep familytree_db

# Logları kontrol et
docker logs familytree_db
```

### 2. Veritabanı Bağlantı Bilgileri

**Sunucu IP:** 194.146.50.83  
**Port:** 3308 (external)  
**Veritabanı Adı:** familytree  
**Kullanıcı Adı:** familytree_user  
**Şifre:** FamilyTreeUser2024!  
**Root Şifre:** FamilyTree2024!

### 3. Connection String

Uygulama içinde kullanılacak connection string:

```
Server=194.146.50.83;Database=familytree;User=familytree_user;Password=FamilyTreeUser2024!;Port=3308;CharSet=utf8mb4;
```

### 4. Adminer ile Veritabanı Yönetimi

Adminer web arayüzü: `http://194.146.50.83:8083`

**Giriş Bilgileri:**
- Sistem: MySQL
- Sunucu: `familytree-mysql` (veya `194.146.50.83:3308`)
- Kullanıcı adı: `familytree_user`
- Şifre: `FamilyTreeUser2024!`
- Veritabanı: `familytree`

### 5. Manuel Docker Run (Alternatif)

Eğer docker-compose kullanmak istemiyorsanız:

```bash
docker run -d \
  --name familytree_db \
  --restart unless-stopped \
  -e MYSQL_ROOT_PASSWORD=FamilyTree2024! \
  -e MYSQL_DATABASE=familytree \
  -e MYSQL_USER=familytree_user \
  -e MYSQL_PASSWORD=FamilyTreeUser2024! \
  -p 3308:3306 \
  -v familytree_mysql_data:/var/lib/mysql \
  mysql:8.4 \
  --character-set-server=utf8mb4 \
  --collation-server=utf8mb4_unicode_ci \
  --default-authentication-plugin=mysql_native_password
```

### 6. Güvenlik Notları

⚠️ **ÖNEMLİ:** 
- Production ortamında şifreleri değiştirin!
- Firewall kurallarını kontrol edin (sadece gerekli IP'lerden erişim)
- SSL/TLS bağlantısı için sertifika yapılandırması yapın

### 7. Uygulama Ayarları

`DataService.cs` dosyasında connection string'i güncelleyin:

```csharp
public static string DefaultConnectionString => 
    "Server=194.146.50.83;Database=familytree;User=familytree_user;Password=FamilyTreeUser2024!;Port=3308;CharSet=utf8mb4;";
```

### 8. Veritabanı Yedekleme

```bash
# Yedek alma
docker exec familytree_db mysqldump -u root -pFamilyTree2024! familytree > backup_$(date +%Y%m%d).sql

# Yedek geri yükleme
docker exec -i familytree_db mysql -u root -pFamilyTree2024! familytree < backup_20240101.sql
```

### 9. Container Yönetimi

```bash
# Container'ı durdur
docker stop familytree_db

# Container'ı başlat
docker start familytree_db

# Container'ı yeniden başlat
docker restart familytree_db

# Container'ı sil (dikkatli!)
docker rm -f familytree_db

# Volume'u sil (tüm veriler silinir!)
docker volume rm familytree_mysql_data
```

### 10. Sorun Giderme

**Bağlantı hatası alıyorsanız:**
```bash
# Container loglarını kontrol et
docker logs familytree_db

# Container'ın çalıştığını kontrol et
docker ps | grep familytree_db

# Port'un açık olduğunu kontrol et
netstat -tuln | grep 3308

# Firewall kurallarını kontrol et
ufw status
```

**Veritabanı oluşturulmadıysa:**
```bash
# Container içine gir
docker exec -it familytree_db mysql -u root -pFamilyTree2024!

# Manuel olarak veritabanı oluştur
CREATE DATABASE IF NOT EXISTS familytree CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

