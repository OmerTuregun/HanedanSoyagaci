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

