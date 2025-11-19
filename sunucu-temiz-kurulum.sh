#!/bin/bash

# Temiz Kurulum Scripti - Tüm eski container'ları temizler ve yeniden kurar

echo "🧹 Eski container'lar temizleniyor..."

# Mevcut container'ları durdur ve sil
docker stop familytree_db familytree_adminer 2>/dev/null
docker rm familytree_db familytree_adminer 2>/dev/null

# Network'ü sil (eğer varsa)
docker network rm familytree_network 2>/dev/null

echo "📁 Dizin hazırlanıyor..."
mkdir -p /root/familytree-mysql
cd /root/familytree-mysql

echo "🐳 MySQL Container başlatılıyor..."

# Volume oluştur (eğer yoksa)
docker volume create familytree_mysql_data 2>/dev/null

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
  -v familytree_mysql_data:/var/lib/mysql \
  mysql:8.4 \
  --character-set-server=utf8mb4 \
  --collation-server=utf8mb4_unicode_ci \
  --default-authentication-plugin=mysql_native_password

echo "⏳ Container başlatılıyor, 15 saniye bekleniyor..."
sleep 15

# Durumu kontrol et
if docker ps | grep -q familytree_db; then
    echo ""
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
    echo "🔍 Son loglar:"
    docker logs familytree_db --tail 10
    echo ""
    echo "🧪 Bağlantı testi yapılıyor..."
    sleep 5
    docker exec familytree_db mysql -u familytree_user -pFamilyTreeUser2024! -e "SELECT 'Bağlantı başarılı!' AS Status;" familytree 2>/dev/null
    if [ $? -eq 0 ]; then
        echo "✅ Veritabanı bağlantısı başarılı!"
    else
        echo "⏳ Veritabanı henüz hazır değil, birkaç saniye daha bekleyin..."
    fi
else
    echo ""
    echo "❌ Container başlatılamadı!"
    echo ""
    echo "🔍 Hata logları:"
    docker logs familytree_db --tail 30
    echo ""
    echo "💡 Çözüm önerileri:"
    echo "   1. Port 3308'in kullanılıp kullanılmadığını kontrol edin: netstat -tuln | grep 3308"
    echo "   2. Docker'ın çalıştığını kontrol edin: docker ps"
    echo "   3. Yeterli disk alanı olduğunu kontrol edin: df -h"
fi

