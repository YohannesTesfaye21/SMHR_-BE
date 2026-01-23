# Setting Up Let's Encrypt SSL Certificate for Production

This guide shows how to get a trusted SSL certificate from Let's Encrypt for your production server.

## Prerequisites
- Domain name pointing to your server (e.g., `api.yourdomain.com`)
- Port 80 open for HTTP-01 challenge
- Root or sudo access on the server

## Option 1: Using Certbot (Recommended)

### 1. Install Certbot
```bash
# On Ubuntu/Debian
sudo apt update
sudo apt install certbot

# On CentOS/RHEL
sudo yum install certbot
```

### 2. Get Certificate
```bash
# Replace api.yourdomain.com with your actual domain
sudo certbot certonly --standalone -d api.yourdomain.com
```

This will:
- Create certificates in `/etc/letsencrypt/live/api.yourdomain.com/`
- Generate `fullchain.pem` and `privkey.pem`

### 3. Convert to PKCS#12 format (.pfx)
```bash
# Navigate to certs directory
cd /opt/smhfr-be/certs

# Convert to .pfx (you'll be prompted for a password)
sudo openssl pkcs12 -export \
  -out cert.pfx \
  -inkey /etc/letsencrypt/live/api.yourdomain.com/privkey.pem \
  -in /etc/letsencrypt/live/api.yourdomain.com/fullchain.pem \
  -name "api.yourdomain.com"

# Copy certificate to certs directory with correct permissions
sudo cp cert.pfx /opt/smhfr-be/certs/
sudo chown root:root /opt/smhfr-be/certs/cert.pfx
sudo chmod 644 /opt/smhfr-be/certs/cert.pfx
```

### 4. Update .env file
```bash
cd /opt/smhfr-be
nano .env
```

Add:
```env
CERT_PASSWORD=your_certificate_password_here
```

### 5. Restart API
```bash
docker compose restart api
```

### 6. Auto-renewal Setup
Let's Encrypt certificates expire every 90 days. Set up auto-renewal:

```bash
# Create renewal script
sudo nano /etc/cron.monthly/renew-letsencrypt-cert.sh
```

Add:
```bash
#!/bin/bash
certbot renew --quiet

# Convert renewed cert to .pfx if renewal succeeded
if [ $? -eq 0 ]; then
  cd /opt/smhfr-be/certs
  sudo openssl pkcs12 -export \
    -out cert.pfx \
    -inkey /etc/letsencrypt/live/api.yourdomain.com/privkey.pem \
    -in /etc/letsencrypt/live/api.yourdomain.com/fullchain.pem \
    -name "api.yourdomain.com" \
    -passout pass:your_certificate_password_here
  
  sudo cp cert.pfx /opt/smhfr-be/certs/
  docker compose restart api
fi
```

```bash
sudo chmod +x /etc/cron.monthly/renew-letsencrypt-cert.sh
```

## Option 2: Using Docker with Certbot

If you prefer to run Certbot in a container:

```bash
# Get certificate using Docker
docker run -it --rm \
  -v /etc/letsencrypt:/etc/letsencrypt \
  -v /var/lib/letsencrypt:/var/lib/letsencrypt \
  -p 80:80 \
  certbot/certbot certonly --standalone -d api.yourdomain.com
```

Then follow steps 3-6 from Option 1.

## Verify Certificate

After setup, verify:
```bash
# Test HTTPS endpoint
curl -I https://api.yourdomain.com:8443/api/health

# Check certificate details
openssl s_client -connect api.yourdomain.com:8443 -showcerts
```

## Notes

- **Domain Required**: Let's Encrypt requires a domain name - it won't work with just an IP address
- **Auto-Renewal**: Certificates expire every 90 days, so auto-renewal is essential
- **Port 80**: You need port 80 open temporarily for the HTTP-01 challenge
- **Security**: Keep your certificate password secure and don't commit it to version control

## Troubleshooting

**Issue: Certbot can't bind to port 80**
- Stop any web server: `sudo systemctl stop nginx` (or apache)
- Run certbot again
- Restart web server after

**Issue: Domain not resolving**
- Make sure DNS A record points to your server IP
- Wait for DNS propagation (can take up to 48 hours)

**Issue: Certificate renewal fails**
- Check certificate expiration: `sudo certbot certificates`
- Manually renew: `sudo certbot renew --force-renewal`
