#!/bin/bash

# MySQL 8.4 Hata Düzeltme Scripti
# Bu script'i sunucuda çalıştırın

echo "🔴 Sorun: MySQL 8.4'te default-authentication-plugin parametresi desteklenmiyor"
echo ""

cd /root/familytree-mysql

echo "1️⃣ Container'ları durduruyorum..."
docker compose -f docker-compose.mysql.yml down

echo ""
echo "2️⃣ Eski volume'u siliyorum (bozuk veriler temizleniyor)..."
docker volume rm familytree_mysql_data 2>/dev/null || echo "Volume zaten yok veya kullanılıyor"

echo ""
echo "3️⃣ docker-compose.mysql.yml dosyasını düzeltiyorum..."

# default-authentication-plugin satırını sil
sed -i '/default-authentication-plugin/d' docker-compose.mysql.yml

# version satırını sil (artık gerekli değil)
sed -i '/^version:/d' docker-compose.mysql.yml

# mysql-init satırını kaldır (opsiyonel, hata verebilir)
sed -i '/mysql-init/d' docker-compose.mysql.yml

echo "✅ Dosya düzeltildi!"
echo ""

echo "4️⃣ Container'ları yeniden başlatıyorum..."
docker compose -f docker-compose.mysql.yml up -d

echo ""
echo "⏳ Container'ın başlamasını bekliyorum (15 saniye)..."
sleep 15

echo ""
echo "5️⃣ Durumu kontrol ediyorum..."
if docker ps | grep -q familytree_db; then
    echo "✅ Container çalışıyor!"
    echo ""
    echo "📊 Container durumu:"
    docker ps | grep familytree_db
    echo ""
    echo "📝 Son loglar:"
    docker logs familytree_db --tail 15
    echo ""
    echo "🧪 Bağlantı testi yapılıyor..."
    sleep 5
    docker exec familytree_db mysql -u familytree_user -pFamilyTreeUser2024! -e "SELECT 'Bağlantı başarılı!' AS Status, VERSION() AS MySQLVersion;" familytree 2>/dev/null
    if [ $? -eq 0 ]; then
        echo ""
        echo "✅✅✅ VERİTABANI BAŞARIYLA ÇALIŞIYOR! ✅✅✅"
        echo ""
        echo "📝 Bağlantı Bilgileri:"
        echo "   Host: 194.146.50.83"
        echo "   Port: 3308"
        echo "   Database: familytree"
        echo "   User: familytree_user"
        echo "   Password: FamilyTreeUser2024!"
    else
        echo ""
        echo "⏳ Veritabanı henüz tam hazır değil, birkaç saniye daha bekleyin..."
        echo "   Logları kontrol edin: docker logs familytree_db"
    fi
else
    echo ""
    echo "❌ Container başlatılamadı!"
    echo ""
    echo "🔍 Hata logları:"
    docker logs familytree_db --tail 30
fi

