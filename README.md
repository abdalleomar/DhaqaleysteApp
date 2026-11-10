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

   Upgrading from the first version:
   - Existing user accounts are kept but deactivated, because their passwords were stored in plain text. Old customer accounts become `Legacy` (no permissions).
   - Each product's free-text category becomes a real category (same names), and its image and video move to the new media table. Old `https://localhost:7192/...` links are converted so they work from any server. No product data is lost.

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
| `GET /api/products?search=&categoryId=&minPrice=&maxPrice=&inStock=&featured=&sort=&page=&pageSize=` | Public. Paged (max 50 per page). `sort`: `newest`, `price_asc`, `price_desc`, `name`, `popular` |
| `GET /api/products/{id}` | Public. Full details with all images and videos |
| `POST /api/products/{id}/view` | Public. Counts a view (used for "popular") |
| `POST /api/products`, `PUT /api/products/{id}` | Staff |
| `PATCH /api/products/{id}/status` | Staff. Quick sold-out / featured / quantity change |
| `DELETE /api/products/{id}` | Staff. Hides the product; it stays in the database |
| `GET /api/categories` | Public (active categories) |
| `GET /api/categories/all`, `POST/PUT/DELETE /api/categories...` | Staff. Only empty categories can be deleted |
| `GET /api/company` | Public. Company name, phone, WhatsApp, address, links |
| `PUT /api/company` | Admin |
| `GET /api/stats` | Staff. Dashboard numbers |
| `POST /api/upload/image`, `POST /api/upload/video` | Staff |
| `POST /api/auth/login`, `/refresh`, `/logout` | Public (rate limited) |
| `GET /api/auth/me`, `POST /api/auth/change-password` | Staff |
| `GET/POST/PUT /api/staff`, `POST /api/staff/{id}/reset-password` | Admin |
| `GET /health` | Public |

A product has up to 10 images and 3 videos. The first image is the main photo.

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
