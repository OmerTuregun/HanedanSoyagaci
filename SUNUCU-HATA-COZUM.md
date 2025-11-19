# Sunucuda Hata Çözüm Rehberi

## 🔍 Yaygın Hatalar ve Çözümleri

### 1. "docker-compose: command not found" Hatası

**Çözüm A:** `docker compose` (boşluklu) kullanın:
```bash
docker compose -f docker-compose.mysql.yml up -d
```

**Çözüm B:** Docker Compose'u yükleyin:
```bash
# Docker Compose v2 yükleme
sudo apt-get update
sudo apt-get install docker-compose-plugin

# Veya eski versiyon
sudo apt-get install docker-compose
```

### 2. "Port 3308 is already allocated" Hatası

**Çözüm:** Port'u değiştirin veya mevcut container'ı kontrol edin:
```bash
# Hangi container port 3308'i kullanıyor?
docker ps | grep 3308

# Eğer başka bir container varsa, docker-compose.mysql.yml'de port'u değiştirin
# Örneğin: "3309:3306" yapın
```

### 3. "Container name 'familytree_db' is already in use" Hatası

**Çözüm:** Mevcut container'ı silin veya farklı isim kullanın:
```bash
# Mevcut container'ı durdur ve sil
docker stop familytree_db
docker rm familytree_db

# Sonra tekrar başlat
docker-compose -f docker-compose.mysql.yml up -d
```

### 4. "Permission denied" Hatası

**Çözüm:** Root kullanıcısı ile çalıştırın veya Docker grubuna ekleyin:
```bash
# Root olarak çalıştır
sudo docker-compose -f docker-compose.mysql.yml up -d

# VEYA kullanıcıyı docker grubuna ekle
sudo usermod -aG docker $USER
# Sonra logout/login yapın
```

### 5. "Network familytree_network already exists" Hatası

**Çözüm:** Network'ü silin veya farklı isim kullanın:
```bash
# Network'ü sil
docker network rm familytree_network

# VEYA docker-compose.mysql.yml'de network ismini değiştirin
```

---

## ✅ Alternatif Kurulum Yöntemleri

### Yöntem 1: Sadece MySQL Container (En Basit)

```bash
cd /root/familytree-mysql

# Mevcut container varsa sil
docker stop familytree_db 2>/dev/null
docker rm familytree_db 2>/dev/null

# Yeni container başlat
docker run -d \
  --name familytree_db \
  --restart unless-stopped \
  -e MYSQL_ROOT_PASSWORD=FamilyTree2024! \
  -e MYSQL_DATABASE=familytree \
  -e MYSQL_USER=familytree_user \
  -e MYSQL_PASSWORD=FamilyTreeUser2024! \
  -e TZ=Europe/Istanbul \
  -p 3308:3306 \
  -v familytree_mysql_data:/var/lib/mysql \
  mysql:8.4 \
  --character-set-server=utf8mb4 \
  --collation-server=utf8mb4_unicode_ci \
  --default-authentication-plugin=mysql_native_password

# Durumu kontrol et
docker ps | grep familytree_db
docker logs familytree_db
```

### Yöntem 2: Docker Compose (Boşluklu)

```bash
cd /root/familytree-mysql

# Docker Compose v2 kullan (boşluklu)
docker compose -f docker-compose.mysql.yml up -d

# Durumu kontrol et
docker compose -f docker-compose.mysql.yml ps
```

### Yöntem 3: Port Değiştirerek

Eğer port 3308 kullanılıyorsa, 3309 kullanın:

```bash
cd /root/familytree-mysql

# docker-compose.mysql.yml dosyasını düzenle
sed -i 's/3308:3306/3309:3306/g' docker-compose.mysql.yml

# Container'ı başlat
docker-compose -f docker-compose.mysql.yml up -d
# VEYA
docker compose -f docker-compose.mysql.yml up -d
```

---

## 🔧 Sorun Giderme Komutları

```bash
# Tüm container'ları listele
docker ps -a

# familytree ile ilgili container'ları bul
docker ps -a | grep familytree

# Port 3308'i kullanan process'i bul
netstat -tuln | grep 3308
# VEYA
ss -tuln | grep 3308

# Docker loglarını kontrol et
docker logs familytree_db

# Container'ın durumunu kontrol et
docker inspect familytree_db

# Volume'ları listele
docker volume ls | grep familytree

# Network'leri listele
docker network ls | grep familytree
```

---

## 🚀 Temiz Kurulum (Sıfırdan)

Eğer her şeyi temizleyip baştan başlamak istiyorsanız:

```bash
# 1. Mevcut container'ları durdur ve sil
docker stop familytree_db familytree_adminer 2>/dev/null
docker rm familytree_db familytree_adminer 2>/dev/null

# 2. Network'ü sil (eğer varsa)
docker network rm familytree_network 2>/dev/null

# 3. Volume'u sil (DİKKAT: Tüm veriler silinir!)
# docker volume rm familytree_mysql_data  # Bu satırı yorumdan çıkarın sadece verileri silmek istiyorsanız

# 4. Dizini temizle ve yeniden oluştur
cd /root
rm -rf familytree-mysql
mkdir -p familytree-mysql
cd familytree-mysql

# 5. Dosyaları yeniden oluştur (sunucu-hizli-kurulum.sh script'ini çalıştırın)
# VEYA manuel olarak dosyaları oluşturun

# 6. Container'ı başlat
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

# 7. Durumu kontrol et
sleep 5
docker ps | grep familytree_db
docker logs familytree_db --tail 20
```

---

## 📝 Hata Mesajını Paylaşın

Lütfen aldığınız tam hata mesajını paylaşın, böylece daha spesifik bir çözüm sunabilirim.

Hata mesajını görmek için:
```bash
docker-compose -f docker-compose.mysql.yml up -d 2>&1
# VEYA
docker compose -f docker-compose.mysql.yml up -d 2>&1
```

