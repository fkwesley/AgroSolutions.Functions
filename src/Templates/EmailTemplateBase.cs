namespace Common.Notifications.Function.Templates;

/// <summary>
/// Base class for all email templates providing common functionality
/// </summary>
public abstract class EmailTemplateBase : IEmailTemplate
{
    public abstract string GetSubjectTemplate();
    public abstract string GetBodyTemplate();

    public string GetSubject(Dictionary<string, string> parameters)
    {
        return ReplaceParameters(GetSubjectTemplate(), parameters);
    }

    public string GetBody(Dictionary<string, string> parameters)
    {
        return ReplaceParameters(GetBodyTemplate(), parameters);
    }

    protected string ReplaceParameters(string content, Dictionary<string, string> parameters)
    {
        if (string.IsNullOrEmpty(content) || parameters == null || parameters.Count == 0)
            return content;

        var sb = new System.Text.StringBuilder(content);
        foreach (var param in parameters)
        {
            sb.Replace(param.Key, param.Value);
        }
        return sb.ToString();
    }

    protected string GetBaseHtmlTemplate(string icon, string title, string subtitle, string alertContent, string metricsContent, string evaluatedContent, string importanceContent, string actionsContent, string correlationId)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 800px; margin: 0 auto; }}
        .header {{ background: {GetHeaderGradient()}; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
        .header h1 {{ margin: 0; font-size: 28px; }}
        .header .icon {{ font-size: 48px; margin-bottom: 10px; }}
        .content {{ padding: 30px; background: #ffffff; }}
        .alert-box {{ background: {GetAlertBoxBackground()}; border-left: 5px solid {GetAlertBoxBorder()}; padding: 20px; margin: 20px 0; border-radius: 5px; }}
        .metrics {{ background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; }}
        .metric-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #dee2e6; }}
        .metric-label {{ font-weight: 600; color: #495057; }}
        .metric-value {{ color: #212529; font-weight: bold; }}
        .section {{ margin: 25px 0; }}
        .section-title {{ color: #495057; font-size: 18px; font-weight: 600; margin-bottom: 15px; padding-bottom: 10px; border-bottom: 2px solid #e9ecef; }}
        .action-list {{ background: {GetActionListBackground()}; padding: 20px; border-radius: 8px; border-left: 4px solid {GetActionListBorder()}; }}
        .action-list ol {{ margin: 0; padding-left: 20px; }}
        .action-list li {{ margin: 10px 0; }}
        .footer {{ background: #f8f9fa; padding: 20px; text-align: center; color: #6c757d; font-size: 12px; border-radius: 0 0 10px 10px; }}
        .urgent {{ background: #dc3545; color: white; padding: 3px 10px; border-radius: 3px; font-weight: bold; }}
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""icon"">{icon}</div>
        <h1>{title}</h1>
        <p>{subtitle}</p>
    </div>
    
    <div class=""content"">
        <div class=""alert-box"">
            {alertContent}
        </div>

        <div class=""metrics"">
            <h3 style=""margin-top:0; color:#495057;"">📊 Métricas Atuais</h3>
            {metricsContent}
        </div>

        <div class=""section"">
            <div class=""section-title"">🔍 O Que Foi Avaliado</div>
            {evaluatedContent}
        </div>

        <div class=""section"">
            <div class=""section-title"">⚠️ Por Que Isso É Importante</div>
            {importanceContent}
        </div>

        <div class=""action-list"">
            <div class=""section-title"" style=""border-bottom:none; color:{GetActionListBorder()};"">✅ Ações Recomendadas</div>
            <ol>
                {actionsContent}
            </ol>
        </div>
    </div>

    <div class=""footer"">
        <p>Este é um alerta automático do Sistema de Monitoramento Agrícola</p>
        <p>Correlation ID: {correlationId}</p>
    </div>
</body>
</html>";
    }

    protected virtual string GetHeaderGradient() => "linear-gradient(135deg, #667eea 0%, #764ba2 100%)";
    protected virtual string GetAlertBoxBackground() => "#fff3cd";
    protected virtual string GetAlertBoxBorder() => "#ffc107";
    protected virtual string GetActionListBackground() => "#e7f3ff";
    protected virtual string GetActionListBorder() => "#0066cc";
}
