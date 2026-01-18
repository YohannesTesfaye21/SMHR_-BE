# GitHub Actions Secrets and Variables Guide

This document lists all the secrets and variables you should configure in GitHub Actions for your SMHFR Backend API.

## 🔐 Repository Secrets (Required)

Go to: **Repository Settings → Secrets and variables → Actions → Secrets tab**

### **Database Configuration (Production)**

| Secret Name | Description | Example Value | Required |
|------------|-------------|---------------|----------|
| `DB_HOST` | PostgreSQL hostname | `prod-db.example.com` | ✅ Production |
| `DB_PORT` | PostgreSQL port | `5432` | ⚠️ If not default |
| `DB_NAME` | Database name | `smhfr_db` | ✅ Production |
| `DB_USERNAME` | Database username | `smhfr_user` | ✅ Production |
| `DB_PASSWORD` | Database password | `YourSecurePassword123!` | ✅ Production |
| `DB_CONNECTION_STRING` | Full connection string (alternative to above) | `Host=prod-db.example.com;Port=5432;Database=smhfr_db;Username=smhfr_user;Password=YourSecurePassword123!` | ⚠️ Alternative |

**Note:** Use either individual DB_* secrets OR `DB_CONNECTION_STRING`, not both.

### **JWT Authentication**

| Secret Name | Description | Example Value | Required |
|------------|-------------|---------------|----------|
| `JWT_SECRET_KEY` | JWT signing secret (minimum 32 characters) | `YourSuperSecretKeyForJWTTokenGenerationMustBeAtLeast32CharactersLong!` | ✅ Production |
| `JWT_ISSUER` | JWT token issuer | `SMHFR_API` | ⚠️ If different from default |
| `JWT_AUDIENCE` | JWT token audience | `SMHFR_Client` | ⚠️ If different from default |
| `JWT_EXPIRATION_MINUTES` | Token expiration time | `1440` | ⚠️ Optional |

### **Admin User Settings (Production)**

| Secret Name | Description | Example Value | Required |
|------------|-------------|---------------|----------|
| `ADMIN_EMAIL` | Admin user email | `admin@smhfr.com` | ✅ Production |
| `ADMIN_PASSWORD` | Admin user password (must meet password policy) | `Admin@SecurePassword123!` | ✅ Production |
| `ADMIN_FIRST_NAME` | Admin first name | `Admin` | ⚠️ Optional |
| `ADMIN_LAST_NAME` | Admin last name | `User` | ⚠️ Optional |

### **Docker Registry (If Deploying Images)**

| Secret Name | Description | Example Value | Required |
|------------|-------------|---------------|----------|
| `DOCKER_USERNAME` | Docker Hub / GHCR username | `yourusername` | ⚠️ If deploying to registry |
| `DOCKER_PASSWORD` | Docker Hub / GHCR password/token | `dckr_pat_xxxxx` | ⚠️ If deploying to registry |
| `DOCKER_REGISTRY` | Registry URL | `docker.io` or `ghcr.io` | ⚠️ If deploying to registry |

### **GitHub Container Registry (GHCR) - Recommended**

| Secret Name | Description | Example Value | Required |
|------------|-------------|---------------|----------|
| `GITHUB_TOKEN` | Auto-provided by GitHub Actions | `${{ secrets.GITHUB_TOKEN }}` | ✅ Auto-available |

**Note:** `GITHUB_TOKEN` is automatically available in workflows, no need to add it manually.

---

## 📋 Repository Variables (Optional - Non-Sensitive)

Go to: **Repository Settings → Secrets and variables → Actions → Variables tab**

| Variable Name | Description | Example Value | Purpose |
|--------------|-------------|---------------|---------|
| `DOCKER_IMAGE_NAME` | Docker image name | `smhfr-api` or `smhr-be` | Deployment |
| `DOCKER_REGISTRY_URL` | Full registry URL | `ghcr.io/YohannesTesfaye21/smhr-be` | Deployment |
| `DEPLOYMENT_ENV` | Default deployment environment | `staging` or `production` | Deployment |
| `DB_PORT` | Database port (if not secret) | `5432` | Configuration |
| `JWT_EXPIRATION_MINUTES` | Token expiration (if not secret) | `1440` | Configuration |
| `API_PORT` | API port | `8080` | Configuration |

---

## 🎯 Environment-Specific Secrets

For different environments (staging, production), you can use:

### **Staging Environment Secrets**
- `STAGING_DB_PASSWORD`
- `STAGING_JWT_SECRET_KEY`
- `STAGING_ADMIN_PASSWORD`

### **Production Environment Secrets**
- `PROD_DB_PASSWORD` or `DB_PASSWORD`
- `PROD_JWT_SECRET_KEY` or `JWT_SECRET_KEY`
- `PROD_ADMIN_PASSWORD` or `ADMIN_PASSWORD`

---

## 📝 Quick Setup Checklist

### ✅ Minimum Required for Local Development (CI Only)
- **None** - CI workflow runs locally and doesn't need secrets

### ✅ Minimum Required for Production Deployment

1. **Database:**
   - `DB_PASSWORD` or `DB_CONNECTION_STRING`

2. **JWT:**
   - `JWT_SECRET_KEY` (if different from appsettings.json)

3. **Admin:**
   - `ADMIN_PASSWORD` (if different from appsettings.json)

### ✅ Recommended for Full CI/CD Pipeline

1. All Database secrets (`DB_HOST`, `DB_NAME`, `DB_USERNAME`, `DB_PASSWORD`)
2. `JWT_SECRET_KEY`
3. `ADMIN_EMAIL` and `ADMIN_PASSWORD`
4. Docker registry credentials (if deploying images)

---

## 🔒 Security Best Practices

1. **Never commit secrets to git** - Use GitHub Secrets only
2. **Use strong passwords** - Minimum 16 characters with mixed case, numbers, and symbols
3. **Rotate secrets regularly** - Update JWT secrets and passwords periodically
4. **Use different secrets per environment** - Don't reuse production secrets in staging
5. **Limit access** - Only grant repository access to trusted team members
6. **Use environment-specific secrets** - Separate staging and production values
7. **Generate secure JWT keys** - Use a cryptographically secure random generator
   ```bash
   # Generate a secure JWT secret (64 characters)
   openssl rand -base64 64
   ```

---

## 📋 Current Values from appsettings.json

**Reference values** (DO NOT use these exact values in production):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=smhfr_db;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyForJWTTokenGenerationMustBeAtLeast32CharactersLong!",
    "Issuer": "SMHFR_API",
    "Audience": "SMHFR_Client",
    "ExpirationInMinutes": 1440
  },
  "AdminSettings": {
    "Email": "admin@smhfr.com",
    "Password": "Admin@123",
    "FirstName": "Admin",
    "LastName": "User"
  }
}
```

⚠️ **IMPORTANT:** Change all these default values before production deployment!

---

## 🚀 How to Add Secrets in GitHub

1. Go to your repository: `https://github.com/YohannesTesfaye21/SMHR_-BE`
2. Click **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Enter the **Name** and **Value**
5. Click **Add secret**

---

## 📚 Additional Resources

- [GitHub Secrets Documentation](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [GitHub Variables Documentation](https://docs.github.com/en/actions/learn-github-actions/variables)
- [Docker Hub Authentication](https://docs.docker.com/docker-hub/access-tokens/)
- [GitHub Container Registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)
