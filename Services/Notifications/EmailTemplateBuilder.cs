using System.Text;
using Microsoft.Extensions.Localization;
using UnifiWatch.Models;

namespace UnifiWatch.Services.Notifications;

/// <summary>
/// Builds HTML and plain text email templates for stock notifications
/// Uses localization for all user-facing strings
/// </summary>
public class EmailTemplateBuilder
{
    private readonly IStringLocalizer _localizer;

    public EmailTemplateBuilder(IStringLocalizer localizer)
    {
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    /// <summary>
    /// Builds an HTML email template for stock alert notifications
    /// </summary>
    /// <param name="products">List of products in stock</param>
    /// <param name="metadata">Additional metadata (store name, check timestamp, etc.)</param>
    /// <returns>HTML email body</returns>
    public string BuildStockAlertHtml(List<UnifiProduct> products, Dictionary<string, string>? metadata = null)
    {
        metadata ??= new Dictionary<string, string>();
        var storeName = metadata.ContainsKey("store") ? metadata["store"] : "UniFi";
        var checkTime = metadata.ContainsKey("checkTime") ? metadata["checkTime"] : DateTime.UtcNow.ToString("O");

        var html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html>");
        html.AppendLine("<head>");
        html.AppendLine("<meta charset=\"utf-8\">");
        html.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine($"<title>{System.Web.HttpUtility.HtmlEncode(_localizer["Email.Subject"])}</title>");
        html.AppendLine("<style>");
        html.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; color: #333; line-height: 1.6; }");
        html.AppendLine(".container { max-width: 600px; margin: 0 auto; padding: 20px; }");
        html.AppendLine(".header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 20px; border-radius: 8px; margin-bottom: 20px; }");
        html.AppendLine(".header h1 { margin: 0; font-size: 24px; }");
        html.AppendLine(".products-table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
        html.AppendLine(".products-table th { background: #f5f5f5; padding: 12px; text-align: left; border-bottom: 2px solid #ddd; font-weight: 600; }");
        html.AppendLine(".products-table td { padding: 12px; border-bottom: 1px solid #eee; }");
        html.AppendLine(".products-table tr:hover { background: #f9f9f9; }");
        html.AppendLine(".price { font-weight: 600; color: #667eea; }");
        html.AppendLine(".in-stock { color: #27ae60; font-weight: 600; }");
        html.AppendLine(".footer { color: #666; font-size: 12px; margin-top: 20px; padding-top: 20px; border-top: 1px solid #eee; text-align: center; }");
        html.AppendLine("</style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("<div class=\"container\">");
        html.AppendLine("<div class=\"header\">");
        html.AppendLine($"<h1>🎉 {System.Web.HttpUtility.HtmlEncode(_localizer["Email.Body.Header"])}</h1>");
        html.AppendLine($"<p>{System.Web.HttpUtility.HtmlEncode(string.Format(_localizer["ProductInStock.Message"], storeName))}</p>");
        html.AppendLine("</div>");

        if (products.Count > 0)
        {
            html.AppendLine("<table class=\"products-table\">");
            html.AppendLine("<thead>");
            html.AppendLine($"<tr><th>{_localizer["Email.Body.ProductName"]}</th><th>{_localizer["Email.Body.SKU"]}</th><th>{_localizer["Email.Body.Price"]}</th><th>{_localizer["Email.Body.Store"]}</th></tr>");
            html.AppendLine("</thead>");
            html.AppendLine("<tbody>");

            foreach (var product in products.Where(p => p.Available))
            {
                html.AppendLine("<tr>");
                html.AppendLine("<td>" + System.Web.HttpUtility.HtmlEncode(product.Name) + "</td>");
                html.AppendLine("<td>" + System.Web.HttpUtility.HtmlEncode(product.SKU) + "</td>");
                html.AppendLine("<td class=\"price\">" + (product.Price.HasValue ? "$" + product.Price.Value.ToString("F2") : "N/A") + "</td>");
                html.AppendLine("<td><span class=\"in-stock\">✓ In Stock</span></td>");
                html.AppendLine("</tr>");
            }

            html.AppendLine("</tbody>");
            html.AppendLine("</table>");
        }
        else
        {
            html.AppendLine("<p>No products currently in stock.</p>");
        }

        html.AppendLine("<div class=\"footer\">");
        html.AppendLine($"<p>Check time: {System.Web.HttpUtility.HtmlEncode(checkTime)}</p>");
        html.AppendLine($"<p><small>{_localizer["Email.Body.Footer"]}</small></p>");
        html.AppendLine("</div>");
        html.AppendLine("</div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    /// <summary>
    /// Builds a plain text email body for stock alerts
    /// </summary>
    /// <param name="products">List of products in stock</param>
    /// <param name="metadata">Additional metadata</param>
    /// <returns>Plain text email body</returns>
    public string BuildStockAlertText(List<UnifiProduct> products, Dictionary<string, string>? metadata = null)
    {
        metadata ??= new Dictionary<string, string>();
        var storeName = metadata.ContainsKey("store") ? metadata["store"] : "UniFi";
        var checkTime = metadata.ContainsKey("checkTime") ? metadata["checkTime"] : DateTime.UtcNow.ToString("O");

        var text = new StringBuilder();

        text.AppendLine($"🎉 {_localizer["Email.Body.Header"]}");
        text.AppendLine(new string('=', 50));
        text.AppendLine();
        text.AppendLine(string.Format(_localizer["ProductInStock.Message"], storeName));
        text.AppendLine();

        if (products.Count > 0)
        {
            foreach (var product in products.Where(p => p.Available))
            {
                text.AppendLine($"• {product.Name}");
                text.AppendLine($"  {_localizer["Email.Body.SKU"]}: {product.SKU}");
                text.AppendLine($"  {_localizer["Email.Body.Price"]}: {(product.Price.HasValue ? "$" + product.Price.Value.ToString("F2") : "N/A")}");
                text.AppendLine($"  ✓ In Stock");
                text.AppendLine();
            }
        }
        else
        {
            text.AppendLine("No products currently in stock.");
            text.AppendLine();
        }

        text.AppendLine(new string('-', 50));
        text.AppendLine($"Check time: {checkTime}");
        text.AppendLine(_localizer["Email.Body.Footer"]);

        return text.ToString();
    }
}
