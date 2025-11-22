# Dokuz Taş Online Turnuva

ASP.NET Core MVC tabanlı, SignalR ile gerçek zamanlı multiplayer Dokuz Taş (Nine Men's Morris) turnuva sistemi.

## Özellikler

### Oyun Mekanikleri
- ✅ Gerçek zamanlı multiplayer oyun (SignalR)
- ✅ Soru-cevap sistemi entegrasyonu
- ✅ Günlük maç limiti (admin tarafından ayarlanabilir)
- ✅ Oyundan çıkma cezalandırma sistemi
- ✅ Zaman kısıtlamaları (soru: 20sn, hamle: 30sn)
- ✅ Otomatik inaktivite kontrolü (5 dakika)

### Turnuva Sistemi
- ✅ Haftalık turnuva yapısı:
  - Pazartesi-Perşembe: Lig maçları
  - Cuma: Çeyrek final
  - Cumartesi: Yarı final
  - Pazar: Final
- ✅ Eleme maçları için saat aralığı ayarlama
- ✅ Otomatik eşleştirme sistemi
- ✅ Günlük aynı rakiple tek maç sınırı

### Puanlama & Averaj
- ✅ Kazanılan maç başına +3 puan
- ✅ Kompleks averaj sistemi:
  - Günlük bonus (5 maç tamamlama)
  - Ardışık gün bonusu (artan averaj)
  - Oyundan çıkma cezası (-9 averaj)
  - Kaybedilen taş başına eksi averaj

### Güvenlik & Kullanıcı Yönetimi
- ✅ ASP.NET Core Identity entegrasyonu
- ✅ Kullanıcı adı/şifre hatırlama (localStorage)
- ✅ Tek cihaz politikası (aynı hesap birden fazla cihazda açılamaz)
- ✅ Admin paneli ile oyuncu yönetimi
- ✅ Kara liste sistemi

### Raporlama
- ✅ Gerçek zamanlı sıralama tablosu
- ✅ Haftalık raporlar
- ✅ Oyuncu detay sayfaları
- ✅ Maç geçmişi takibi
- ✅ Günlük ve haftalık istatistikler

## Teknoloji Stack

- **Backend:** ASP.NET Core 8.0 MVC
- **Database:** PostgreSQL
- **Cache/Game State:** Redis
- **Real-time:** SignalR
- **Frontend:** Tailwind CSS, Vanilla JavaScript
- **Web Server:** Nginx (reverse proxy)
- **Identity:** ASP.NET Core Identity

## Sunucu Gereksinimleri

- Ubuntu 24.04 LTS
- .NET 8 SDK/Runtime
- PostgreSQL 12+
- Redis 6+
- Nginx
- 1 CPU, 1GB RAM minimum

## Kurulum

### 1. Gerekli Paketleri Yükleyin

```bash
# .NET 8 SDK
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt update
sudo apt install -y dotnet-sdk-8.0

# PostgreSQL
sudo apt install -y postgresql postgresql-contrib

# Redis
sudo apt install -y redis-server

# Nginx
sudo apt install -y nginx
```

### 2. Veritabanını Hazırlayın

```bash
sudo -u postgres psql
CREATE DATABASE dokuztasonline;
CREATE USER dokuztasuser WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE dokuztasonline TO dokuztasuser;
\q
```

### 3. Redis'i Yapılandırın

```bash
sudo systemctl enable redis-server
sudo systemctl start redis-server
```

### 4. Uygulamayı Klonlayın

```bash
cd /var/www
git clone [your-repo-url] dokuztasonline
cd dokuztasonline
```

### 5. Bağlantı Dizelerini Güncelleyin

`appsettings.Production.json` dosyasını düzenleyin:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=dokuztasonline;Username=dokuztasuser;Password=your_password",
    "Redis": "localhost:6379"
  }
}
```

### 6. Deploy Script'ini Çalıştırın

```bash
chmod +x deploy.sh
./deploy.sh
```

## Manuel Deployment

Eğer deploy script'i çalışmazsa:

```bash
# 1. Publish
dotnet publish -c Release -o /var/www/dokuztasonline

# 2. İzinler
sudo chown -R www-data:www-data /var/www/dokuztasonline
sudo chmod -R 755 /var/www/dokuztasonline

# 3. Systemd Service
sudo cp dokuztasonline.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable dokuztasonline
sudo systemctl start dokuztasonline

# 4. Nginx
sudo cp nginx.conf /etc/nginx/sites-available/dokuztasonline
sudo ln -s /etc/nginx/sites-available/dokuztasonline /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

## Kullanım

### İlk Giriş

1. Tarayıcıda `http://farukzerdali.com.tr/dokuzTasOnlineTurnuva` adresine gidin
2. İlk admin hesabı:
   - Kullanıcı Adı: `admin`
   - Şifre: `admin123`

### Admin Paneli

Admin panelinden:
- Oyuncu ekle/sil/düzenle
- Sistem ayarlarını değiştir (günlük maç limiti, süre ayarları, vb.)
- Eleme maçları saat aralıklarını belirle
- Soru bankası yönetimi
- Haftalık raporlar görüntüle
- Oyuncu detayları incele

### Oyuncu Kullanımı

1. Hesap oluştur veya giriş yap
2. Ana sayfada "Maç Bul" butonuna tıkla
3. Rakip bulunana kadar bekle
4. Sırayla soruları cevapla ve taş yerleştir/oyna
5. Değirmen yap ve rakip taşlarını al
6. Rakibin 2 taşa düşürüp kazanmaya çalış

## Bakım

### Logları İzleme

```bash
# Uygulama logları
sudo journalctl -u dokuztasonline -f

# Nginx logları
sudo tail -f /var/log/nginx/error.log
sudo tail -f /var/log/nginx/access.log

# PostgreSQL logları
sudo tail -f /var/log/postgresql/postgresql-*.log
```

### Servisleri Yönetme

```bash
# Uygulama
sudo systemctl restart dokuztasonline
sudo systemctl status dokuztasonline

# PostgreSQL
sudo systemctl restart postgresql
sudo systemctl status postgresql

# Redis
sudo systemctl restart redis-server
sudo systemctl status redis-server

# Nginx
sudo systemctl restart nginx
sudo systemctl status nginx
```

### Yedekleme

```bash
# PostgreSQL Backup
sudo -u postgres pg_dump dokuztasonline > backup_$(date +%Y%m%d).sql

# Restore
sudo -u postgres psql dokuztasonline < backup_20250101.sql
```

## Güncelleme

```bash
cd /var/www/dokuztasonline
git pull
./deploy.sh
```

## Sorun Giderme

### Port 5000 Kullanımda

```bash
sudo lsof -i :5000
sudo kill -9 [PID]
sudo systemctl restart dokuztasonline
```

### PostgreSQL Bağlantı Hatası

```bash
# Connection string'i kontrol et
cat appsettings.Production.json

# PostgreSQL çalışıyor mu?
sudo systemctl status postgresql

# PostgreSQL loglarına bak
sudo tail -f /var/log/postgresql/postgresql-*.log
```

### SignalR Bağlantı Problemi

```bash
# Nginx WebSocket yapılandırmasını kontrol et
sudo nginx -t
sudo systemctl reload nginx

# Uygulama loglarını kontrol et
sudo journalctl -u dokuztasonline -n 100
```

## Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

## Destek

Sorularınız için: [faruk@example.com]
