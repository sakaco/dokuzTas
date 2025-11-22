#!/bin/bash

echo "🚀 Dokuz Taş Online Turnuva Deployment Script"
echo "=============================================="

# Renklendirme
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Değişkenler
APP_NAME="dokuztasonline"
APP_PATH="/var/www/dokuztasonline"
SERVICE_NAME="dokuztasonline.service"
NGINX_CONF="/etc/nginx/sites-available/dokuztasonline"

# Hata kontrolü
set -e

echo -e "${YELLOW}1. Uygulamayı durduruyoruz...${NC}"
sudo systemctl stop $SERVICE_NAME 2>/dev/null || true

echo -e "${YELLOW}2. Veritabanı ve Redis kontrolü...${NC}"
sudo systemctl status postgresql --no-pager || echo "PostgreSQL durumu kontrol edilemedi"
sudo systemctl status redis-server --no-pager || echo "Redis durumu kontrol edilemedi"

echo -e "${YELLOW}3. Uygulama klasörünü hazırlıyoruz...${NC}"
sudo mkdir -p $APP_PATH
sudo chown -R $USER:$USER $APP_PATH

echo -e "${YELLOW}4. Uygulamayı publish ediyoruz...${NC}"
dotnet publish -c Release -o $APP_PATH

echo -e "${YELLOW}5. İzinleri ayarlıyoruz...${NC}"
sudo chown -R www-data:www-data $APP_PATH
sudo chmod -R 755 $APP_PATH

echo -e "${YELLOW}6. Systemd service'i kuruyoruz...${NC}"
sudo cp dokuztasonline.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable $SERVICE_NAME

echo -e "${YELLOW}7. Nginx yapılandırmasını kuruyoruz...${NC}"
sudo cp nginx.conf $NGINX_CONF
sudo ln -sf $NGINX_CONF /etc/nginx/sites-enabled/dokuztasonline
sudo nginx -t
sudo systemctl reload nginx

echo -e "${YELLOW}8. Veritabanı migration...${NC}"
cd $APP_PATH
sudo -u www-data dotnet ef database update 2>/dev/null || echo "Migration çalıştırılamadı, ilk çalıştırmada otomatik olacak"

echo -e "${YELLOW}9. Uygulamayı başlatıyoruz...${NC}"
sudo systemctl start $SERVICE_NAME

echo -e "${YELLOW}10. Durum kontrolü...${NC}"
sleep 3
sudo systemctl status $SERVICE_NAME --no-pager

echo -e "${GREEN}✅ Deployment tamamlandı!${NC}"
echo -e "${GREEN}📍 Uygulama adresi: http://farukzerdali.com.tr/dokuzTasOnlineTurnuva${NC}"
echo ""
echo "Log kontrolü için: sudo journalctl -u $SERVICE_NAME -f"
echo "Servis durumu: sudo systemctl status $SERVICE_NAME"
