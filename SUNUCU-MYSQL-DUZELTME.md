# MySQL 8.4 Hata Düzeltme

## 🔴 Sorun
MySQL 8.4'te `default-authentication-plugin=mysql_native_password` parametresi artık desteklenmiyor ve volume'da eski/bozuk veriler var.

## ✅ Çözüm Adımları

### 1. Container'ları Durdur ve Sil

```bash
cd /root/familytree-mysql

# Container'ları durdur ve sil
docker compose -f docker-compose.mysql.yml down

# VEYA manuel olarak
docker stop familytree_db familytree_adminer
docker rm familytree_db familytree_adminer
```

### 2. Eski Volume'u Sil (ÖNEMLİ: Tüm veriler silinir!)

```bash
# Volume'u sil
docker volume rm familytree_mysql_data
```

### 3. docker-compose.mysql.yml Dosyasını Düzelt

```bash
cd /root/familytree-mysql
nano docker-compose.mysql.yml
```

**Değiştirilecek kısım:**
```yaml
# ESKİ (HATALI):
command: 
  - --character-set-server=utf8mb4
  - --collation-server=utf8mb4_unicode_ci
  - --default-authentication-plugin=mysql_native_password

# YENİ (DOĞRU):
command: 
  - --character-set-server=utf8mb4
  - --collation-server=utf8mb4_unicode_ci
```

**VEYA** `version: '3.8'` satırını silin (artık gerekli değil).

### 4. Container'ları Yeniden Başlat

```bash
docker compose -f docker-compose.mysql.yml up -d
```

### 5. Durumu Kontrol Et

```bash
# Container'ın çalıştığını kontrol et
docker ps | grep familytree_db

# Logları kontrol et (hata olmamalı)
docker logs familytree_db --tail 20

# Bağlantı testi
./mysql-test-connection.sh
```

---

## 🚀 Tek Komut Çözümü

Tüm adımları tek seferde yapmak için:

```bash
cd /root/familytree-mysql

# 1. Container'ları durdur ve sil
docker compose -f docker-compose.mysql.yml down

# 2. Volume'u sil
docker volume rm familytree_mysql_data

# 3. docker-compose.mysql.yml'i düzelt (default-authentication-plugin satırını sil)
sed -i '/default-authentication-plugin/d' docker-compose.mysql.yml

# 4. version satırını sil (opsiyonel)
sed -i '/^version:/d' docker-compose.mysql.yml

# 5. Yeniden başlat
docker compose -f docker-compose.mysql.yml up -d

# 6. Bekle ve kontrol et
sleep 10
docker ps | grep familytree_db
docker logs familytree_db --tail 20
```

---

## 🔄 Alternatif: MySQL 8.0 Kullan (Daha Stabil)

Eğer MySQL 8.4 ile sorun yaşamaya devam ederseniz, MySQL 8.0 kullanabilirsiniz:

```bash
cd /root/familytree-mysql

# docker-compose.mysql.yml dosyasında image'i değiştir
sed -i 's/mysql:8.4/mysql:8.0/g' docker-compose.mysql.yml

# Container'ları yeniden başlat
docker compose -f docker-compose.mysql.yml down
docker volume rm familytree_mysql_data
docker compose -f docker-compose.mysql.yml up -d
```

---

## 📝 Güncellenmiş docker-compose.mysql.yml

```yaml
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

