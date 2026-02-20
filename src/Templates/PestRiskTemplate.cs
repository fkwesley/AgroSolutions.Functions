namespace Common.Notifications.Function.Templates;

public class PestRiskTemplate : EmailTemplateBase
{
    public override string GetSubjectTemplate() => "🐛 Alerta de Risco de Pragas - Campo {fieldId} (Risco {riskLevel})";

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
        .risk-factors { background: #fff3e6; padding: 20px; border-radius: 8px; border-left: 4px solid #ff9800; margin: 20px 0; }
        .action-list { background: #e7f3ff; padding: 20px; border-radius: 8px; border-left: 4px solid #0066cc; }
        .action-list ol { margin: 0; padding-left: 20px; }
        .action-list li { margin: 10px 0; }
        .footer { background: #f8f9fa; padding: 20px; text-align: center; color: #6c757d; font-size: 12px; border-radius: 0 0 10px 10px; }
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""icon"">🐛</div>
        <h1>Alerta de Risco de Pragas</h1>
        <p>Campo {fieldId} - Risco: {riskLevel}</p>
    </div>

    <div class=""content"">
        <div class=""alert-box"">
            <h2 style=""margin-top:0;"">⚠️ CONDIÇÕES FAVORÁVEIS PARA PRAGAS</h2>
            <p>O sistema detectou condições climáticas favoráveis à proliferação de pragas agrícolas.</p>
        </div>

        <div class=""metrics"">
            <h3 style=""margin-top:0; color:#495057;"">📊 Métricas Atuais</h3>
            <div class=""metric-row"">
                <span class=""metric-label"">Dias Consecutivos Favoráveis:</span>
                <span class=""metric-value"">{favorableDaysCount}</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Temperatura Média:</span>
                <span class=""metric-value"">{averageTemperature}°C</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Umidade Média:</span>
                <span class=""metric-value"">{averageMoisture}%</span>
            </div>
        </div>

        <div class=""section"">
            <div class=""section-title"">🔍 O Que Foi Avaliado</div>
            <p>O sistema monitora condições climáticas (temperatura e umidade) que favorecem a proliferação de pragas agrícolas, analisando padrões ao longo de vários dias consecutivos.</p>
        </div>

        <div class=""risk-factors"">
            <div class=""section-title"" style=""border-bottom:none; color:#ff9800;"">🎯 Fatores de Risco Identificados</div>
            {riskFactors}
        </div>

        <div class=""section"">
            <div class=""section-title"">⚠️ Por Que Isso É Importante</div>
            <p>Condições favoráveis para pragas podem resultar em:</p>
            <ul>
                <li>Rápida multiplicação de populações de pragas</li>
                <li>Danos significativos às culturas</li>
                <li>Redução da produtividade e qualidade</li>
                <li>Aumento dos custos com controle de pragas</li>
                <li>Necessidade de intervenção urgente</li>
            </ul>
        </div>

        <div class=""action-list"">
            <div class=""section-title"" style=""border-bottom:none; color:#0066cc;"">✅ Ações Recomendadas</div>
            <ol>
                <li>Realizar vistoria imediata de campo para detectar presença de pragas</li>
                <li>Configurar armadilhas de monitoramento</li>
                <li>Coordenar com agrônomo para plano de controle preventivo</li>
                <li>Preparar produtos de controle apropriados</li>
                <li>Intensificar monitoramento nas próximas semanas</li>
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
