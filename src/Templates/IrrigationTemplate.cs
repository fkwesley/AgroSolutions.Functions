namespace Common.Notifications.Function.Templates;

public class IrrigationTemplate : EmailTemplateBase
{
    public override string GetSubjectTemplate() => "💧 Recomendação de Irrigação - Campo {fieldId} (Urgência {urgency})";

    public override string GetBodyTemplate() => @"
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
        <div class=""icon"">💧</div>
        <h1>Recomendação de Irrigação</h1>
        <p>Campo {fieldId} - Urgência: {urgency}</p>
    </div>

    <div class=""content"">
        <div class=""alert-box"">
            <h2 style=""margin-top:0;"">🌱 IRRIGAÇÃO RECOMENDADA</h2>
            <p>Com base nos níveis atuais de umidade do solo, o sistema recomenda irrigação para manter condições ótimas para as culturas.</p>
        </div>

        <div class=""metrics"">
            <h3 style=""margin-top:0; color:#495057;"">📊 Métricas Atuais</h3>
            <div class=""metric-row"">
                <span class=""metric-label"">Umidade Atual do Solo:</span>
                <span class=""metric-value"">{currentMoisture}%</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Umidade Alvo:</span>
                <span class=""metric-value"">{optimalMoisture}%</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Água Necessária:</span>
                <span class=""metric-value"" style=""color:#0066cc;"">{waterAmountMM} mm</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Duração Estimada:</span>
                <span class=""metric-value"">{estimatedDurationMinutes} minutos</span>
            </div>
        </div>

        <div class=""section"">
            <div class=""section-title"">🔍 O Que Foi Avaliado</div>
            <p>O sistema monitora continuamente os níveis de umidade do solo e compara com os níveis ótimos para as culturas. Quando a umidade cai abaixo do ideal, uma recomendação de irrigação é gerada com cálculo preciso da quantidade de água necessária.</p>
        </div>

        <div class=""section"">
            <div class=""section-title"">⚠️ Por Que Isso É Importante</div>
            <p>Manter níveis adequados de umidade do solo é essencial para:</p>
            <ul>
                <li>Crescimento saudável e desenvolvimento das culturas</li>
                <li>Absorção eficiente de nutrientes</li>
                <li>Prevenção de estresse hídrico</li>
                <li>Maximização da produtividade</li>
                <li>Qualidade superior dos produtos agrícolas</li>
            </ul>
        </div>

        <div class=""action-list"">
            <div class=""section-title"" style=""border-bottom:none; color:#0066cc;"">✅ Ações Recomendadas</div>
            <ol>
                <li>{urgencyAction}</li>
                <li>Monitorar umidade do solo durante e após a irrigação</li>
                <li>Verificar sistemas de irrigação antes de iniciar</li>
                <li>Ajustar volume de água conforme tipo de solo e cultura</li>
                <li>Registrar data e volume aplicado para histórico</li>
            </ol>
        </div>
    </div>

    <div class=""footer"">
        <p>Este é um alerta automático do Sistema de Monitoramento Agrícola</p>
        <p>Correlation ID: {correlationId}</p>
    </div>
</body>
</html>";

    protected override string GetHeaderGradient() => "linear-gradient(135deg, #667eea 0%, #764ba2 100%)";
    protected override string GetAlertBoxBackground() => "#fff3cd";
    protected override string GetAlertBoxBorder() => "#ffc107";
}
