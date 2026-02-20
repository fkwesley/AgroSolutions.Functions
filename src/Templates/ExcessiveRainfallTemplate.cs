namespace Common.Notifications.Function.Templates;

public class ExcessiveRainfallTemplate : EmailTemplateBase
{
    public override string GetSubjectTemplate()
    {
        return "⚠️ Alerta de Chuva Excessiva - Campo {fieldId}";
    }

    public override string GetBodyTemplate()
    {
        return @"
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
        .footer { background: #f8f9fa; padding: 20px; text-align: center; color: #6c757d; font-size: 12px; border-radius: 0 0 10px 10px; }
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""icon"">⚠️</div>
        <h1>Alerta de Chuva Excessiva</h1>
        <p>Campo {fieldId} - Detectado em {detectedAt}</p>
    </div>
    
    <div class=""content"">
        <div class=""alert-box"">
            <h2 style=""margin-top:0;"">🌧️ CHUVA EXCESSIVA DETECTADA</h2>
            <p>O sistema detectou níveis de precipitação acima do limite configurado para este campo.</p>
        </div>

        <div class=""metrics"">
            <h3 style=""margin-top:0; color:#495057;"">📊 Métricas Atuais</h3>
            <div class=""metric-row"">
                <span class=""metric-label"">Precipitação Atual:</span>
                <span class=""metric-value"">{precipitation} mm</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Limite Configurado:</span>
                <span class=""metric-value"">{threshold} mm</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Excesso:</span>
                <span class=""metric-value"" style=""color:#dc3545;"">{excess} mm ({percentAbove}% acima)</span>
            </div>
        </div>

        <div class=""section"">
            <div class=""section-title"">🔍 O Que Foi Avaliado</div>
            <p>O sistema monitora continuamente os níveis de precipitação em todos os campos. Quando uma nova medição é registrada, verifica se a precipitação excede o limite configurado.</p>
        </div>

        <div class=""section"">
            <div class=""section-title"">⚠️ Por Que Isso É Importante</div>
            <p>Chuvas excessivas podem causar:</p>
            <ul>
                <li>Erosão do solo e lixiviação de nutrientes</li>
                <li>Encharcamento e deficiência de oxigênio nas zonas radiculares</li>
                <li>Aumento do risco de doenças fúngicas</li>
                <li>Danos às culturas e perda de produtividade</li>
                <li>Atraso nas operações de campo</li>
            </ul>
        </div>

        <div class=""action-list"">
            <div class=""section-title"" style=""border-bottom:none; color:#0066cc;"">✅ Ações Recomendadas</div>
            <ol>
                <li>Inspecionar sistemas de drenagem para prevenir alagamento</li>
                <li>Monitorar níveis de umidade do solo nas próximas 24-48 horas</li>
                <li>Avaliar a saúde das culturas quanto a sinais de estresse ou doença</li>
                <li>Adiar irrigação e fertilização até que a umidade do solo normalize</li>
                <li>Considerar drenagem adicional se o alagamento persistir</li>
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

    protected override string GetHeaderGradient() => "linear-gradient(135deg, #667eea 0%, #764ba2 100%)";
    protected override string GetAlertBoxBackground() => "#fff3cd";
    protected override string GetAlertBoxBorder() => "#ffc107";
}
