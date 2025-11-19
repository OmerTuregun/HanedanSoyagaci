#!/bin/bash

# Sunucuda Hızlı Kurulum Scripti
# Bu script'i sunucuda çalıştırarak tüm dosyaları otomatik oluşturur

echo "📁 Dizin oluşturuluyor..."
mkdir -p /root/familytree-mysql
cd /root/familytree-mysql

echo "📄 docker-compose.mysql.yml oluşturuluyor..."
cat > docker-compose.mysql.yml << 'EOF'
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
EOF

echo "📄 docker-run-commands.sh oluşturuluyor..."
cat > docker-run-commands.sh << 'EOF'
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
EOF

echo "📄 mysql-test-connection.sh oluşturuluyor..."
cat > mysql-test-connection.sh << 'EOF'
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
EOF

echo "🔐 Script'lere çalıştırma izni veriliyor..."
chmod +x docker-run-commands.sh
chmod +x mysql-test-connection.sh

echo ""
echo "✅ Tüm dosyalar oluşturuldu!"
echo ""
echo "📁 Oluşturulan dosyalar:"
ls -lah /root/familytree-mysql/
echo ""
echo "🚀 Container'ı başlatmak için:"
echo "   cd /root/familytree-mysql"
echo "   docker-compose -f docker-compose.mysql.yml up -d"
echo ""
echo "   VEYA"
echo ""
echo "   ./docker-run-commands.sh"

