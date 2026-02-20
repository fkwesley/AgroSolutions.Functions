namespace Common.Notifications.Function.Templates;

public class FreezingTemperatureTemplate : EmailTemplateBase
{
    public override string GetSubjectTemplate() => "❄️ Alerta de Temperatura de Congelamento - Campo {fieldId}";

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
        <div class=""icon"">❄️</div>
        <h1>Alerta de Temperatura de Congelamento</h1>
        <p>Campo {fieldId} - RISCO DE GEADA</p>
    </div>

    <div class=""content"">
        <div class=""alert-box"">
            <h2 style=""margin-top:0;"">🧊 TEMPERATURA DE CONGELAMENTO DETECTADA</h2>
            <p>A temperatura do ar está abaixo do ponto de congelamento, criando risco significativo de geada.</p>
        </div>

        <div class=""metrics"">
            <h3 style=""margin-top:0; color:#495057;"">📊 Métricas Atuais</h3>
            <div class=""metric-row"">
                <span class=""metric-label"">Temperatura do Ar:</span>
                <span class=""metric-value"" style=""color:#2196f3;"">{airTemperature}°C</span>
            </div>
            <div class=""metric-row"">
                <span class=""metric-label"">Limite de Congelamento:</span>
                <span class=""metric-value"">{threshold}°C</span>
            </div>
        </div>

        <div class=""section"">
            <div class=""section-title"">🔍 O Que Foi Avaliado</div>
            <p>O sistema monitora continuamente a temperatura do ar. Quando a temperatura cai abaixo do ponto de congelamento, um alerta é emitido para proteger as culturas contra danos por geada.</p>
        </div>

        <div class=""section"">
            <div class=""section-title"">⚠️ Por Que Isso É Importante</div>
            <p>Temperaturas de congelamento podem causar:</p>
            <ul>
                <li>Formação de cristais de gelo dentro das células vegetais</li>
                <li>Danos irreversíveis aos tecidos das plantas</li>
                <li>Morte de mudas e plantas jovens</li>
                <li>Perda total ou parcial da safra</li>
                <li>Redução significativa da produtividade</li>
            </ul>
        </div>

        <div class=""action-list"">
            <div class=""section-title"" style=""border-bottom:none; color:#0066cc;"">✅ Ações Recomendadas</div>
            <ol>
                <li><span class=""urgent"">URGENTE</span> Ativar sistemas de proteção contra geada imediatamente</li>
                <li>Cobrir culturas sensíveis com materiais de proteção</li>
                <li>Monitorar temperatura continuamente durante a noite</li>
                <li>Considerar irrigação por aspersão como proteção térmica</li>
                <li>Avaliar danos nas plantas após o evento de congelamento</li>
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
