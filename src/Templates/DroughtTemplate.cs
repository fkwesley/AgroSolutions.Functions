namespace Common.Notifications.Function.Templates;

public class DroughtTemplate : EmailTemplateBase
{
    public override string GetSubjectTemplate() => "🌵 Alerta de Condição de Seca - Campo {fieldId}";

    public override string GetBodyTemplate() => GetDroughtHtmlTemplate();

    protected override string GetHeaderGradient() => "linear-gradient(135deg, #667eea 0%, #764ba2 100%)";
    protected override string GetAlertBoxBackground() => "#fff3cd";
    protected override string GetAlertBoxBorder() => "#ffc107";

    private string GetDroughtHtmlTemplate() => @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <style>
        body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 800px; margin: 0 auto; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
        .header h1 { margin: 0; font-size: 28px; }
        .header .icon { font-size: 48px; margin-bottom: 10px; }
        .content { padding: 30px; background: #ffffff; }
        .alert-box { background: #fff3cd; border-left: 5px solid #ffc107; padding: 20px; margin: 20px 0; border-radius: 5px; }
        .metrics { background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0; }
        .metric-row { display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #dee2e6; }
        .metric-label { font-weight: 600; color: #495057; }
        .metric-value { color: #212529; font-weight: bold; }
        .section { margin: 25px 0; }
        .section-title { color: #495057; font-size: 18px; font-weight: 600; margin-bottom: 15px; padding-bottom: 10px; border-bottom: 2px solid #e9ecef; }
        .action-list { background: #e7f3ff; padding: 20px; border-radius: 8px; border-left: 4px solid #0066cc; }
        .action-list ol { margin: 0; padding-left: 20px; }
        .action-list li { margin: 10px 0; }
        .urgent { background: #dc3545; color: white; padding: 3px 10px; border-radius: 3px; font-weight: bold; }
        .footer { background: #f8f9fa; padding: 20px; text-align: center; color: #6c757d; font-size: 12px; border-radius: 0 0 10px 10px; }
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""icon"">🌵</div>
        <h1>Alerta de Condição de Seca</h1>
        <p>Campo {fieldId} - Detectado em {detectedAt}</p>
    </div>

    <div class=""content"">
        <div class=""alert-box"">
            <h2 style=""margin-top:0;"">💧 CONDIÇÃO DE SECA DETECTADA</h2>
            <p>O sistema identificou um período prolongado de baixa umidade do solo que caracteriza condição de seca.</p>
        </div>

        <div class=""metrics"">
            <h3 style=""margin-top:0; color:#495057;"">📊 Métricas Atuais</h3>
            <div class=""metric-row"">
                <span class=""metric-label"">Umidade do Solo Atual:</span>
                <span class=""metric-value"">{soilMoisture}%</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Limite de Seca:</span>
                <span class=""metric-value"">{threshold}%</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Déficit de Umidade:</span>
                <span class=""metric-value"" style=""color:#dc3545;"">{moistureDeficit}%</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Duração da Seca:</span>
                <span class=""metric-value"">{durationHours} horas ({durationDays} dias)</span>
            </div>
        </div>

        <div class=""section"">
            <div class=""section-title"">🔍 O Que Foi Avaliado</div>
            <p>O sistema analisa dados de umidade do solo dos últimos {historyDays} dias para detectar períodos prolongados de baixos níveis de umidade.</p>
        </div>

        <div class=""section"">
            <div class=""section-title"">⚠️ Por Que Isso É Importante</div>
            <p>Condições de seca prolongada podem causar:</p>
            <ul>
                <li>Estresse hídrico severo afetando crescimento e desenvolvimento das culturas</li>
                <li>Redução da fotossíntese e absorção de nutrientes</li>
                <li>Murchamento permanente e potencial perda da cultura</li>
                <li>Diminuição da qualidade e quantidade da produção</li>
            </ul>
        </div>

        <div class=""action-list"">
            <div class=""section-title"" style=""border-bottom:none; color:#0066cc;"">✅ Ações Recomendadas</div>
            <ol>
                <li><span class=""urgent"">URGENTE</span> Programar irrigação imediata para restaurar umidade do solo</li>
                <li>Calcular necessidades de água baseado no tipo de solo e necessidades da cultura</li>
                <li>Monitorar indicadores de estresse da cultura</li>
                <li>Ajustar programação de irrigação para prevenir recorrência</li>
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
