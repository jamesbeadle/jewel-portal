namespace Jewel.JPMS.Api.Features.SiteAccess.Site;

/// <summary>The scan page's whole stylesheet, inline: high contrast for sunlight, tap targets a
/// gloved thumb can hit, and not one request beyond the page itself.</summary>
public static class SiteDrawingsPageStyles
{
    public const string Css =
        "*{box-sizing:border-box}" +
        "body{margin:0;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;background:#fff;color:#1a1e29;font-size:17px;line-height:1.4}" +
        "header{padding:20px 16px 12px;border-bottom:4px solid #ff8300}" +
        ".brand{margin:0 0 6px;font-size:12px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;color:#c09a51}" +
        "h1{margin:0;font-size:22px;line-height:1.2}" +
        ".label{margin:6px 0 0;font-size:17px;font-weight:600}" +
        ".stamp{margin:6px 0 0;font-size:14px;color:#606672}" +
        "main{padding:0 16px 32px}" +
        "h2{margin:22px 0 8px;font-size:14px;font-weight:700;letter-spacing:.06em;text-transform:uppercase;color:#606672}" +
        "ul{list-style:none;margin:0;padding:0}" +
        "li{border:1px solid #dddde1;border-radius:8px;margin:0 0 10px}" +
        "a{display:block;padding:14px 16px;min-height:60px;color:#1a1e29;text-decoration:none}" +
        "a:active{background:#f3f3f5}" +
        ".name{display:block;font-size:18px;font-weight:600}" +
        ".meta{display:block;margin-top:4px;font-size:14px;color:#606672}" +
        ".approved{color:#1f7a3a}" +
        ".unapproved{color:#c2410c}" +
        ".empty{margin:24px 0;color:#606672}";
}
