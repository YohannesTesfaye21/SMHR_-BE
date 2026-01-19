# SSL Certificates Directory

Place your SSL certificate files here for HTTPS support.

## Required Files

- `cert.pfx` - PKCS#12 certificate file (password-protected)

## Certificate Password

Set the certificate password in your `.env` file or `docker-compose.yml`:

```env
CERT_PASSWORD=your_certificate_password
```

## Generating a Self-Signed Certificate (Development)

For development/testing purposes, you can generate a self-signed certificate:

```bash
# Generate a self-signed certificate
openssl req -x509 -newkey rsa:4096 -keyout cert.key -out cert.crt -days 365 -nodes -subj "/CN=localhost"

# Convert to PKCS#12 format (.pfx)
openssl pkcs12 -export -out cert.pfx -inkey cert.key -in cert.crt -passout pass:your_password
```

## Production

For production, use a certificate from a trusted Certificate Authority (CA) such as:
- Let's Encrypt (free)
- Commercial CA providers

## Security Note

⚠️ **Never commit certificate files to version control!** This directory is already in `.gitignore`.
