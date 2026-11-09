Dhaqaleyste is a cross-platform product catalogue app for a single company, built with .NET MAUI and an ASP.NET Core Web API.

Customers open the app and browse the company's products without signing in. Company staff sign in to add and manage products.

## ✨ Features

**Customers (no account needed)**
- Product listing with search
- Product details with image and video

**Company staff (sign-in required)**
- Secure staff sign-in (email or phone)
- Add, edit and delete products
- Image and video upload
- Dashboard
- Admin and Staff roles; admins manage staff accounts through the API

## 🛠 Technologies

C# · .NET 9 · .NET MAUI · ASP.NET Core Web API · Entity Framework Core · SQL Server · XAML

## Projects

| Folder | What it is |
|---|---|
| `DhaqaleysteApi/` | ASP.NET Core Web API (JWT authentication, SQL Server) |
| `DhaqaleysteApi.Tests/` | Integration tests for the API (run against a real SQL Server) |
| `DhaqaleysteApp/` | .NET MAUI app (Android, iOS, macOS, Windows) |

## Running the API locally

Secrets are never committed. In development they live in .NET user-secrets on your machine.

1. **Database.** `appsettings.Development.json` uses your local SQL Server Express (`.\SQLEXPRESS`, database `DhaqaleysteDB`). To use another server:

   ```bash
   cd DhaqaleysteApi
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=DhaqaleysteDB;..."
   ```

2. **Signing key** for staff sign-in tokens (any random text of at least 32 characters). If you skip this in development, a temporary key is used and staff must sign in again after every restart.

   ```bash
   dotnet user-secrets set "Jwt:SigningKey" "<a long random value>"
   ```

3. **First administrator.** There is no built-in admin password. Set one, start the API once, and the account is created:

   ```bash
   dotnet user-secrets set "Bootstrap:AdminEmail" "you@yourcompany.com"
   dotnet user-secrets set "Bootstrap:AdminPassword" "<at least 8 characters, letters and numbers>"
   ```

   The admin is only created when no active admin exists. After that you can remove these two secrets.

4. **Update the database schema**:

   ```bash
   dotnet tool install --global dotnet-ef
   dotnet ef database update
   ```

   Upgrading from the first version: existing user accounts are kept but deactivated, because their passwords were stored in plain text. Old customer accounts become `Legacy` (no permissions). Products are not changed.

5. **Run**: `dotnet run --launch-profile http` (listens on `http://localhost:5092`).

### Production configuration

Set these as environment variables on the server (never in `appsettings.json`):

| Variable | Purpose |
|---|---|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Jwt__SigningKey` | Random secret, 32+ characters (the API refuses to start without it) |
| `Bootstrap__AdminEmail`, `Bootstrap__AdminPassword` | Only for the first start |
| `Storage__RootPath` | Folder for uploaded media (default `uploads` next to the API) |
| `AllowedHosts` | Your API domain, e.g. `api.example.com` |

## API overview

| Endpoint | Access |
|---|---|
| `GET /api/products`, `GET /api/products/{id}` | Public |
| `POST/PUT/DELETE /api/products...` | Staff |
| `POST /api/upload/image`, `POST /api/upload/video` | Staff |
| `POST /api/auth/login`, `/refresh`, `/logout` | Public (rate limited) |
| `GET /api/auth/me`, `POST /api/auth/change-password` | Staff |
| `GET/POST/PUT /api/staff`, `POST /api/staff/{id}/reset-password` | Admin |
| `GET /health` | Public |

## Running the tests

The tests create and delete their own database. Point them at any SQL Server you can use for testing:

```bash
export DHAQ_TEST_SQLSERVER="Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=True"
dotnet test DhaqaleysteApi.Tests
```

## Mobile app configuration

The API address is read from `DhaqaleysteApp/Resources/Raw/appsettings.json`:

```json
{ "ApiBaseUrl": "https://api.yourcompany.com/" }
```

Leave it empty during development: Debug builds then use `http://10.0.2.2:5092/` on the Android emulator and `http://localhost:5092/` elsewhere. Release builds require a real HTTPS address.
