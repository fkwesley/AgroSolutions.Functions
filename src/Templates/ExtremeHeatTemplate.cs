namespace Common.Notifications.Function.Templates;

public class ExtremeHeatTemplate : EmailTemplateBase
{
    public override string GetSubjectTemplate() => "🔥 Alerta de Calor Extremo - Campo {fieldId}";

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
        .footer { background: #f8f9fa; padding: 20px; text-align: center; color: #6c757d; font-size: 12px; border-radius: 0 0 10px 10px; }
    </style>
</head>
<body>
    <div class=""header"">
        <div class=""icon"">🔥</div>
        <h1>Alerta de Calor Extremo</h1>
        <p>Campo {fieldId}</p>
    </div>

    <div class=""content"">
        <div class=""alert-box"">
            <h2 style=""margin-top:0;"">🌡️ CALOR EXTREMO DETECTADO</h2>
            <p>Temperatura do ar excedeu o limite de calor extremo configurado para este campo.</p>
        </div>

        <div class=""metrics"">
            <h3 style=""margin-top:0; color:#495057;"">📊 Métricas Atuais</h3>
            <div class=""metric-row"">
                <span class=""metric-label"">Temperatura do Ar:</span>
                <span class=""metric-value"" style=""color:#dc3545;"">{airTemperature}°C</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Limite Configurado:</span>
                <span class=""metric-value"">{threshold}°C</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Excesso:</span>
                <span class=""metric-value"">+{temperatureExcess}°C</span>
            </div>
        </div>

        <div class=""section"">
            <div class=""section-title"">🔍 O Que Foi Avaliado</div>
            <p>O sistema monitora continuamente a temperatura do ar em todos os campos. Quando uma nova medição é registrada, verifica se a temperatura excede o limite de calor extremo configurado.</p>
        </div>

        <div class=""section"">
            <div class=""section-title"">⚠️ Por Que Isso É Importante</div>
            <p>Temperaturas extremas podem causar:</p>
            <ul>
                <li>Estresse térmico severo reduzindo a fotossíntese</li>
                <li>Perda acelerada de água por transpiração</li>
                <li>Danos celulares nas plantas</li>
                <li>Redução da qualidade e produtividade das culturas</li>
                <li>Aumento do risco de desidratação das plantas</li>
            </ul>
        </div>

        <div class=""action-list"">
            <div class=""section-title"" style=""border-bottom:none; color:#0066cc;"">✅ Ações Recomendadas</div>
            <ol>
                <li>Aumentar frequência de irrigação para compensar perda de água</li>
                <li>Monitorar umidade do solo de perto</li>
                <li>Reagendar trabalho de campo para horários mais frescos</li>
                <li>Considerar sombreamento temporário para culturas sensíveis</li>
                <li>Avaliar saúde das plantas quanto a sinais de estresse térmico</li>
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
