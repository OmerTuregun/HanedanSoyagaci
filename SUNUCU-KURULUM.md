# Sunucuda Oluşturulacak Dosya Yapısı

## 📁 Dizin Yapısı

Sunucuda (194.146.50.83) aşağıdaki dizin yapısını oluşturun:

```
/root/familytree-mysql/
├── docker-compose.mysql.yml
├── docker-run-commands.sh
├── mysql-test-connection.sh
└── README-MYSQL-SETUP.md
```

## 🚀 Sunucuda Kurulum Adımları

### 1. Dizin Oluştur

```bash
mkdir -p /root/familytree-mysql
cd /root/familytree-mysql
```

### 2. Dosyaları Oluştur

Aşağıdaki dosyaları oluşturun ve içeriklerini yapıştırın.

---

## 📄 1. docker-compose.mysql.yml

```yaml
version: '3.8'

services:
  familytree-mysql:
    image: mysql:8.4
    container_name: familytree_db
    restart: unless-stopped
    environment:
      MYSQL_ROOT_PASSWORD: FamilyTree2024!
      MYSQL_DATABASE: familytree
      MYSQL_USER: familytree_user
      MYSQL_PASSWORD: FamilyTreeUser2024!
      TZ: Europe/Istanbul
    ports:
      - "3308:3306"
      - "33060:33060"
    volumes:
      - familytree_mysql_data:/var/lib/mysql
    command: 
      - --character-set-server=utf8mb4
      - --collation-server=utf8mb4_unicode_ci
      - --default-authentication-plugin=mysql_native_password
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost", "-u", "root", "-p$$MYSQL_ROOT_PASSWORD"]
      interval: 10s
      timeout: 5s
      retries: 5
    networks:
      - familytree_network

  familytree-adminer:
    image: adminer:latest
    container_name: familytree_adminer
    restart: unless-stopped
    ports:
      - "8083:8080"
    environment:
      ADMINER_DEFAULT_SERVER: familytree-mysql
    networks:
      - familytree_network
    depends_on:
      - familytree-mysql

volumes:
  familytree_mysql_data:
    driver: local

networks:
  familytree_network:
    driver: bridge
```

---

## 📄 2. docker-run-commands.sh

```bash
#!/bin/bash

# Hanedan Soy Ağacı MySQL Container Kurulum Scripti
# Sunucu: 194.146.50.83

echo "🚀 FamilyTree MySQL Container'ı oluşturuluyor..."

# Volume oluştur (eğer yoksa)
docker volume create familytree_mysql_data

# MySQL Container'ı başlat
docker run -d \
  --name familytree_db \
  --restart unless-stopped \
  -e MYSQL_ROOT_PASSWORD=FamilyTree2024! \
  -e MYSQL_DATABASE=familytree \
  -e MYSQL_USER=familytree_user \
  -e MYSQL_PASSWORD=FamilyTreeUser2024! \
  -e TZ=Europe/Istanbul \
  -p 3308:3306 \
  -p 33060:33060 \
  -v familytree_mysql_data:/var/lib/mysql \
  --health-cmd="mysqladmin ping -h localhost -u root -p$$MYSQL_ROOT_PASSWORD" \
  --health-interval=10s \
  --health-timeout=5s \
  --health-retries=5 \
  mysql:8.4 \
  --character-set-server=utf8mb4 \
  --collation-server=utf8mb4_unicode_ci \
  --default-authentication-plugin=mysql_native_password

echo "⏳ Container başlatılıyor, 10 saniye bekleniyor..."
sleep 10

# Container durumunu kontrol et
if docker ps | grep -q familytree_db; then
    echo "✅ Container başarıyla başlatıldı!"
    echo ""
    echo "📊 Container Bilgileri:"
    docker ps | grep familytree_db
    echo ""
    echo "📝 Bağlantı Bilgileri:"
    echo "   Host: 194.146.50.83"
    echo "   Port: 3308"
    echo "   Database: familytree"
    echo "   User: familytree_user"
    echo "   Password: FamilyTreeUser2024!"
    echo ""
    echo "🔍 Logları görmek için: docker logs familytree_db"
    echo "🛑 Durdurmak için: docker stop familytree_db"
    echo "▶️  Başlatmak için: docker start familytree_db"
else
    echo "❌ Container başlatılamadı! Logları kontrol edin:"
    docker logs familytree_db
fi
```

---

## 📄 3. mysql-test-connection.sh

```bash
#!/bin/bash

# MySQL Bağlantı Test Scripti

echo "🔌 MySQL bağlantısı test ediliyor..."

# Container içinden test
docker exec familytree_db mysql -u familytree_user -pFamilyTreeUser2024! -e "SELECT 'Bağlantı başarılı!' AS Status, DATABASE() AS CurrentDatabase, VERSION() AS MySQLVersion;" familytree

if [ $? -eq 0 ]; then
    echo "✅ Bağlantı başarılı!"
    echo ""
    echo "📊 Veritabanı bilgileri:"
    docker exec familytree_db mysql -u familytree_user -pFamilyTreeUser2024! -e "SHOW DATABASES;" familytree
    echo ""
    echo "📋 Tablolar:"
    docker exec familytree_db mysql -u familytree_user -pFamilyTreeUser2024! -e "SHOW TABLES;" familytree
else
    echo "❌ Bağlantı başarısız! Container loglarını kontrol edin:"
    docker logs familytree_db --tail 50
fi
```

---

## 📄 4. README-MYSQL-SETUP.md

```markdown
# MySQL Veritabanı Kurulum Rehberi

## 🐳 Docker ile MySQL Kurulumu

### 1. Docker Compose ile Başlatma

```bash
cd /root/familytree-mysql
docker-compose -f docker-compose.mysql.yml up -d
docker ps | grep familytree_db
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

### 5. Container Yönetimi

```bash
# Container'ı durdur
docker stop familytree_db

# Container'ı başlat
docker start familytree_db

# Container'ı yeniden başlat
docker restart familytree_db

# Logları görüntüle
docker logs familytree_db
```

### 6. Veritabanı Yedekleme

```bash
# Yedek alma
docker exec familytree_db mysqldump -u root -pFamilyTree2024! familytree > backup_$(date +%Y%m%d).sql

# Yedek geri yükleme
docker exec -i familytree_db mysql -u root -pFamilyTree2024! familytree < backup_20240101.sql
```
```

---

## ✅ Sunucuda Çalıştırılacak Komutlar

### Adım 1: Dizin Oluştur ve Dosyaları Oluştur

```bash
# Dizin oluştur
mkdir -p /root/familytree-mysql
cd /root/familytree-mysql

# docker-compose.mysql.yml dosyasını oluştur
nano docker-compose.mysql.yml
# (Yukarıdaki içeriği yapıştırın, Ctrl+X, Y, Enter ile kaydedin)

# docker-run-commands.sh dosyasını oluştur
nano docker-run-commands.sh
# (Yukarıdaki içeriği yapıştırın, Ctrl+X, Y, Enter ile kaydedin)

# mysql-test-connection.sh dosyasını oluştur
nano mysql-test-connection.sh
# (Yukarıdaki içeriği yapıştırın, Ctrl+X, Y, Enter ile kaydedin)

# Script'lere çalıştırma izni ver
chmod +x docker-run-commands.sh
chmod +x mysql-test-connection.sh
```

### Adım 2: Container'ı Başlat

**Seçenek A: Docker Compose ile (Önerilen)**

```bash
cd /root/familytree-mysql
docker-compose -f docker-compose.mysql.yml up -d
```

**Seçenek B: Script ile**

```bash
cd /root/familytree-mysql
./docker-run-commands.sh
```

### Adım 3: Bağlantıyı Test Et

```bash
cd /root/familytree-mysql
./mysql-test-connection.sh
```

### Adım 4: Durumu Kontrol Et

```bash
# Container'ları listele
docker ps | grep familytree

# Logları kontrol et
docker logs familytree_db

# Port'u kontrol et
netstat -tuln | grep 3308
```

---

## 🔗 Bağlantı Bilgileri Özeti

- **Host:** 194.146.50.83
- **Port:** 3308
- **Database:** familytree
- **User:** familytree_user
- **Password:** FamilyTreeUser2024!
- **Adminer:** http://194.146.50.83:8083

---

## 📝 Hızlı Başlangıç (Tek Komut)

Eğer dosyaları manuel oluşturmak istemiyorsanız, sunucuda şu komutu çalıştırabilirsiniz:

```bash
mkdir -p /root/familytree-mysql && cd /root/familytree-mysql && \
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

Bu komut container'ı başlatır. Adminer için ayrı bir container gerekir.

