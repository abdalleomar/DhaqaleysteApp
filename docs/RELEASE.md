# Releasing Dhaqaleyste

Two stages:

1. **Test on real phones** with an APK file and a temporary public address for the API on your PC.
2. **Publish on Google Play** once real hosting is ready.

App identity (already set in `DhaqaleysteApp.csproj`):

| Setting | Value | Notes |
|---|---|---|
| App ID (`ApplicationId`) | `com.dhaqaleyste.app` | Permanent once published on Google Play |
| Name (`ApplicationTitle`) | Dhaqaleyste | Shown under the icon |
| Version (`ApplicationDisplayVersion`) | 1.0 | What people see |
| Build number (`ApplicationVersion`) | 1 | Must go up by 1 for every Google Play upload |

---

## Stage 1 — Test on phones (APK)

### 1. Give the API a public https address (Visual Studio Dev Tunnel)

Phones cannot reach `localhost` on your PC, so the API needs a public address while you test.

1. In Visual Studio, select the **DhaqaleysteApi** project, then the arrow next to **Start** → **Dev Tunnels** → **Create a Tunnel…**
2. Sign in with your Microsoft or GitHub account. Name: `dhaqaleyste`, Tunnel type: **Persistent** (the address stays the same), Access: **Public**. Click **OK**.
3. Start the API with the **http** profile. Its window and **View → Output** show the tunnel address, something like
   `https://abcd1234-5092.euw.devtunnels.ms/`.
4. Because the API now sits behind the tunnel, turn on forwarded headers (so login limits see each phone's real address):

   ```bash
   cd DhaqaleysteApi
   dotnet user-secrets set "ForwardedHeaders:Enabled" "true"
   ```

The tunnel and the API only work while your PC is on and the API is running. Good for testing; not for customers.

### 2. Point the app at that address

Edit `DhaqaleysteApp/Resources/Raw/appsettings.json`:

```json
{ "ApiBaseUrl": "https://abcd1234-5092.euw.devtunnels.ms/" }
```

(Use your own tunnel address, with the `/` at the end. Release builds have no built-in address: without one here the app shows "not connected to a server". Android also only allows https addresses.)

### 3. Build and sign the APK

1. Toolbar: **Release**, and an Android device or emulator selected.
2. Right-click **DhaqaleysteApp** → **Publish…**. The Archive Manager builds the app.
3. Click **Distribute…** → **Ad Hoc**.
4. Signing identity: click **+** and create one:
   - Alias: `dhaqaleyste`
   - Password: a strong password you store safely
   - Name / organisation: your company details
5. **Save As** → choose a folder. You get `com.dhaqaleyste.app-Signed.apk`.

> **Keep the signing key safe.** Visual Studio stores it in
> `%LocalAppData%\Xamarin\Mono for Android\Keystore\dhaqaleyste\`.
> Copy that folder and the password somewhere safe (not in Git). Use the same key for Google Play uploads.
> If you lose it, you cannot update the app.

### 4. Install on phones

1. Send the `.apk` by WhatsApp, Google Drive or USB.
2. On the phone, open it. Android asks to **allow installing unknown apps** for WhatsApp/Files: allow it.
3. Open **Dhaqaleyste**.

### 5. Test checklist

- [ ] Home loads products, categories and the featured banner
- [ ] Search, filters, sort; Categories tab
- [ ] Product page: photos, video, **Call**, **Order on WhatsApp**, Share
- [ ] Favorites (heart), Recently viewed
- [ ] Contact tab: Call, WhatsApp, map, email; Somali / English; Light / Dark
- [ ] Staff: sign in, add product with photos, mark sold out, featured
- [ ] Staff: **Create your staff account** with the invite code; **Forgot password** (code appears in the API window until email is set up); **Change password**
- [ ] Admin: Staff accounts → share code, remove / restore someone
- [ ] Turn off Wi-Fi/data: the app shows "no internet" with Try again, and does not crash

---

## Stage 2 — Real hosting (before Google Play)

Customers need the API running all the time at a fixed address such as `https://api.yourcompany.com/`.

1. **Hosting.** For example Azure App Service + Azure SQL, or a Windows/Linux server (VPS) with SQL Server Express.
2. **Settings** (environment variables, never in files):

   | Variable | Value |
   |---|---|
   | `ConnectionStrings__DefaultConnection` | the hosting database |
   | `Jwt__SigningKey` | a new random value, 32+ characters |
   | `Bootstrap__AdminEmail`, `Bootstrap__AdminPassword` | first start only |
   | `Storage__RootPath` | a folder that survives restarts and is backed up (uploaded photos/videos) |
   | `Email__SmtpHost`, `Email__SmtpPort`, `Email__Username`, `Email__Password`, `Email__FromAddress` | mail server for forgot-password codes |
   | `ForwardedHeaders__Enabled` | `true` when behind the host's proxy/load balancer |
   | `AllowedHosts` | your API domain |

3. **Database.** Create the tables with `dotnet ef database update`, or generate a script to run on the server:

   ```bash
   dotnet ef migrations script --idempotent -o migrate.sql --project DhaqaleysteApi
   ```

4. **HTTPS** with a certificate for your domain (hosting providers usually include this).
5. **Backups** of the database and the uploads folder.
6. Put the real address in the app's `appsettings.json` and build again.
7. Check the privacy page opens: `https://api.yourcompany.com/privacy`.

---

## Stage 3 — Google Play

1. **Developer account** at play.google.com/console — one-off $25 fee and identity verification.
   New personal accounts may have to run a **closed test** (Google currently asks for a number of testers over about two weeks) before going public.
2. **Build an .aab** (Google Play format) signed with the same key:

   ```bash
   dotnet publish DhaqaleysteApp -f net9.0-android -c Release -p:AndroidPackageFormat=aab -p:AndroidKeyStore=true -p:AndroidSigningKeyStore=<path>\dhaqaleyste.keystore -p:AndroidSigningKeyAlias=dhaqaleyste -p:AndroidSigningKeyPass=env:DHAQ_KEY_PASS -p:AndroidSigningStorePass=env:DHAQ_KEY_PASS
   ```

   (Set the `DHAQ_KEY_PASS` environment variable to the key password first, so it is not typed into the command.)
   The `.aab` is in `DhaqaleysteApp/bin/Release/net9.0-android/publish/`.
3. **Create the app** in Play Console and fill in:
   - **Store listing:** name, short and full description (English and Somali), 512×512 icon, 1024×500 feature graphic, phone screenshots.
   - **Privacy policy:** `https://api.yourcompany.com/privacy`
   - **App access:** "Some functionality is restricted" — explain that the staff area needs a staff account and give a test staff login for Google's reviewers.
   - **Ads:** No.
   - **Content rating** questionnaire (a shopping catalogue; no violence etc.).
   - **Target audience:** adults / general.
   - **Data safety** (what this app actually does):
     - Collects **name, email address, phone number** — only from **staff accounts**, for **account management**; not shared with third parties.
     - Customers: no account, no personal data collected; favorites stay on the device.
     - Data is **encrypted in transit** (HTTPS). Users can ask for their data to be deleted (contact details on the privacy page).
     - No location, contacts, photos of the user's device, or advertising IDs collected.
4. Upload the `.aab` to **Internal testing** first, then **Closed testing**, then **Production**.
5. For every update: raise `ApplicationVersion` by 1 (and `ApplicationDisplayVersion`, e.g. 1.1), build a new `.aab`, upload.
