#!/bin/bash
# Generate a self-signed certificate for development/testing

CERT_DIR="./certs"
CERT_NAME="cert"
PASSWORD="dev-cert-password"

echo "🔐 Generating self-signed certificate for development..."

# Create certs directory if it doesn't exist
mkdir -p "$CERT_DIR"

# Generate private key and certificate
openssl req -x509 -newkey rsa:4096 \
    -keyout "$CERT_DIR/$CERT_NAME.key" \
    -out "$CERT_DIR/$CERT_NAME.crt" \
    -days 365 \
    -nodes \
    -subj "/CN=localhost/O=SMHFR Development/C=US"

# Convert to PKCS#12 format (.pfx) for .NET
openssl pkcs12 -export \
    -out "$CERT_DIR/$CERT_NAME.pfx" \
    -inkey "$CERT_DIR/$CERT_NAME.key" \
    -in "$CERT_DIR/$CERT_NAME.crt" \
    -passout "pass:$PASSWORD"

# Clean up intermediate files
rm "$CERT_DIR/$CERT_NAME.key" "$CERT_DIR/$CERT_NAME.crt"

echo "✅ Certificate generated successfully!"
echo ""
echo "📋 Certificate details:"
echo "   File: $CERT_DIR/$CERT_NAME.pfx"
echo "   Password: $PASSWORD"
echo ""
echo "⚠️  Add to your .env file or docker-compose.yml:"
echo "   CERT_PASSWORD=$PASSWORD"
echo ""
echo "🔒 This is a self-signed certificate for development only."
echo "   For production, use a certificate from a trusted CA."
