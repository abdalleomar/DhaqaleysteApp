using System.Text.Encodings.Web;
using DhaqaleysteApi.Models;

namespace DhaqaleysteApi.Services
{
    /// <summary>
    /// The app's privacy policy (English and Somali), served at /privacy. Google Play asks for this link.
    /// Company name and contact details come from Company details, so the page stays up to date.
    /// Have it reviewed before release; it describes what this app actually does today.
    /// </summary>
    public static class PrivacyPolicyPage
    {
        public const string LastUpdated = "11 November 2026";

        public static string Render(CompanySettings company)
        {
            var e = HtmlEncoder.Default;
            var name = e.Encode(string.IsNullOrWhiteSpace(company.CompanyName) ? "Dhaqaleyste" : company.CompanyName);
            var contactParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(company.Email)) contactParts.Add($"<a href=\"mailto:{e.Encode(company.Email)}\">{e.Encode(company.Email)}</a>");
            if (!string.IsNullOrWhiteSpace(company.Phone)) contactParts.Add(e.Encode(company.Phone));
            if (!string.IsNullOrWhiteSpace(company.Address) || !string.IsNullOrWhiteSpace(company.City))
                contactParts.Add(e.Encode(string.Join(", ", new[] { company.Address, company.City }.Where(s => !string.IsNullOrWhiteSpace(s)))));
            var contact = contactParts.Count > 0 ? string.Join(" · ", contactParts) : "the contact details shown in the app's Contact tab";
            var contactSo = contactParts.Count > 0 ? string.Join(" · ", contactParts) : "xogta xiriirka ee ku jirta qaybta \"Nala xiriir\" ee app-ka";

            return $$"""
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Privacy policy · {{name}}</title>
<style>
  :root { color-scheme: light dark; }
  body { margin: 0; font-family: system-ui, -apple-system, "Segoe UI", sans-serif; line-height: 1.6; background: #F5F6F4; color: #0E1726; }
  main { max-width: 760px; margin: 0 auto; padding: 32px 20px 64px; }
  h1 { font-size: 28px; margin: 0 0 4px; }
  h2 { font-size: 20px; margin: 32px 0 8px; }
  .muted { color: #5B6576; }
  .card { background: #FFFFFF; border: 1px solid #E4E7EC; border-radius: 18px; padding: 20px 24px; margin-top: 24px; }
  a { color: #0B6E5F; }
  nav a { margin-right: 16px; font-weight: 600; }
  @media (prefers-color-scheme: dark) {
    body { background: #0B0F14; color: #EEF1F4; } .muted { color: #9AA5B1; }
    .card { background: #151B23; border-color: #232B36; } a { color: #3CC4A8; }
  }
</style>
</head>
<body>
<main>
<h1>Privacy policy</h1>
<p class="muted">{{name}} app · Last updated {{LastUpdated}}</p>
<nav><a href="#en">English</a><a href="#so">Soomaali</a></nav>

<div class="card" id="en">
<h2>Who we are</h2>
<p>The {{name}} app shows the products sold by {{name}}. Questions about this policy: {{contact}}.</p>

<h2>Customers</h2>
<ul>
  <li><strong>No account is needed.</strong> We do not ask customers for their name, phone number, email or location.</li>
  <li><strong>Favorites and recently viewed products are saved only on your phone.</strong> They are not sent to us. Deleting the app deletes them.</li>
  <li>To show products, the app connects to our server. Like every website, the server receives your device's internet (IP) address. We use it only to keep the service working and secure (for example to stop abuse) and do not keep it longer than needed for that.</li>
  <li>When you open a product we add one to that product's view count. This count is not linked to you.</li>
  <li><strong>Call and WhatsApp buttons</strong> open your phone's dialer or WhatsApp. What you then share is handled by those apps and their own privacy policies.</li>
  <li>Your language and appearance choices are saved on your phone.</li>
</ul>

<h2>Staff accounts</h2>
<p>Company staff who sign in give their name, email and (optionally) phone number. We store these to let them sign in and manage products, and use the email only to send password-reset codes. Passwords are stored only in a protected (hashed) form that cannot be read back. Images and videos that staff upload are shown to customers as part of the products.</p>

<h2>What we do not do</h2>
<p>We do not show ads, sell or rent personal information, track you across other apps or websites, or use your location, contacts, camera or microphone.</p>

<h2>Security</h2>
<p>The app talks to our server over an encrypted connection (HTTPS). Access to staff functions is protected by sign-in.</p>

<h2>Your choices</h2>
<p>Customers can clear their favorites in the app or delete the app at any time. Staff can ask the company to remove their account and personal details using the contact details above.</p>

<h2>Children</h2>
<p>The app is a product catalogue for a general audience and does not knowingly collect personal information from children.</p>

<h2>Changes</h2>
<p>If this policy changes, we will update this page and the date at the top.</p>
</div>

<div class="card" id="so" lang="so">
<h2>Annaga</h2>
<p>App-ka {{name}} wuxuu muujiyaa alaabta ay iibiso {{name}}. Su'aalaha ku saabsan siyaasaddan: {{contactSo}}.</p>

<h2>Macaamiisha</h2>
<ul>
  <li><strong>Akoon looma baahna.</strong> Kama codsanno macaamiisha magacooda, lambarka taleefanka, iimaylka ama goobta ay joogaan.</li>
  <li><strong>Alaabta aad jeceshahay iyo kuwa aad dhowaan eegtay waxay ku kaydsan yihiin taleefankaaga oo keliya.</strong> Nalooma soo diro. Haddii aad tirtirto app-ka, way tirmayaan.</li>
  <li>Si loo muujiyo alaabta, app-ku wuxuu ku xirmaa server-kayaga. Sida website kasta, server-ku wuxuu helaa cinwaanka internetka (IP) ee qalabkaaga. Waxaan u isticmaalnaa oo keliya inaan adeegga shaqeysiinno oo aan ilaalinno, mana hayno wax ka badan inta loo baahan yahay.</li>
  <li>Marka aad furto alaab, waxaan hal ku darnaa tirada daawashada alaabtaas. Tiradaas laguma xiriirin adiga.</li>
  <li><strong>Badhamada Wac iyo WhatsApp</strong> waxay furaan taleefanka ama WhatsApp. Waxa aad markaas wadaagto waxaa maamula app-yadaas iyo siyaasadahooda.</li>
  <li>Luqadda iyo muuqaalka aad dooratay waxay ku kaydsan yihiin taleefankaaga.</li>
</ul>

<h2>Akoonnada shaqaalaha</h2>
<p>Shaqaalaha shirkadda ee soo gala waxay bixiyaan magacooda, iimaylkooda iyo (haddii ay doonaan) lambarka taleefanka. Waxaan u kaydinnaa si ay u soo galaan oo ay u maareeyaan alaabta, iimaylkana waxaan u isticmaalnaa oo keliya dirista koodhka dib-u-dejinta furaha. Furayaasha waxaa loo kaydiyaa qaab la ilaaliyay (hashed) oo aan dib loo akhrin karin. Sawirrada iyo muuqaallada ay shaqaaluhu soo geliyaan waxaa loo muujiyaa macaamiisha iyagoo qayb ka ah alaabta.</p>

<h2>Waxyaabaha aanan samaynin</h2>
<p>Ma muujinno xayaysiis, ma iibinno ama ma kireysanno macluumaadka shakhsiga ah, kuma raadinno app-yada ama websaydhada kale, mana isticmaalno goobtaada, xiriiradaada, kamaradaada ama makarafoonkaaga.</p>

<h2>Amniga</h2>
<p>App-ku wuxuu la xiriiraa server-kayaga isagoo isticmaalaya xiriir sir ah (HTTPS). Hawlaha shaqaalaha waxaa lagu ilaaliyaa soo gelid.</p>

<h2>Doorashooyinkaaga</h2>
<p>Macaamiishu waxay tirtiri karaan alaabta ay jecel yihiin ama app-ka mar kasta. Shaqaaluhu waxay shirkadda ka codsan karaan inay tirtirto akoonkooda iyo xogtooda, iyagoo isticmaalaya xogta xiriirka ee kor ku qoran.</p>

<h2>Carruurta</h2>
<p>App-ku waa liiska alaabta oo loogu talagalay dadweynaha guud, mana si ula kac ah u ururiyo macluumaad shakhsi ah oo carruur ah.</p>

<h2>Isbeddellada</h2>
<p>Haddii siyaasaddan isbeddesho, waxaan cusboonaysiin doonnaa boggan iyo taariikhda sare.</p>
</div>
</main>
</body>
</html>
""";
        }
    }
}
