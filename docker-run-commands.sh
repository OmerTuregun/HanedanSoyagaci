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

