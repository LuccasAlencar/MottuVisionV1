using Microsoft.ML;
using Microsoft.ML.Data;

namespace MottuVision.Services;

/// <summary>
/// Dados de entrada para previsão
/// </summary>
public class MotoPrevisaoInput
{
    [LoadColumn(0)]
    public float ZonaId { get; set; }

    [LoadColumn(1)]
    public float PatioId { get; set; }

    [LoadColumn(2)]
    public float StatusId { get; set; }

    [LoadColumn(3)]
    public float DiaSemana { get; set; }

    [LoadColumn(4)]
    public float TempoPermanenciaReal { get; set; } // Para treino
}

/// <summary>
/// Saída da previsão
/// </summary>
public class MotoPrevisaoOutput
{
    [ColumnName("Score")]
    public float TempoPrevistoHoras { get; set; }
}

/// <summary>
/// Serviço de ML.NET para prever tempo de permanência de motos no pátio
/// </summary>
public class MotoPrevisaoService
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;

    public MotoPrevisaoService()
    {
        _mlContext = new MLContext(seed: 0);
        TreinarModelo();
    }

    /// <summary>
    /// Treina o modelo com dados sintéticos (em produção, usar dados reais)
    /// </summary>
    private void TreinarModelo()
    {
        // Dados de treinamento sintéticos (exemplo)
        var dadosTreinamento = new List<MotoPrevisaoInput>
        {
            new() { ZonaId = 1, PatioId = 1, StatusId = 1, DiaSemana = 1, TempoPermanenciaReal = 24 },
            new() { ZonaId = 1, PatioId = 1, StatusId = 1, DiaSemana = 2, TempoPermanenciaReal = 30 },
            new() { ZonaId = 1, PatioId = 2, StatusId = 1, DiaSemana = 3, TempoPermanenciaReal = 48 },
            new() { ZonaId = 2, PatioId = 1, StatusId = 2, DiaSemana = 4, TempoPermanenciaReal = 72 },
            new() { ZonaId = 2, PatioId = 2, StatusId = 2, DiaSemana = 5, TempoPermanenciaReal = 96 },
            new() { ZonaId = 1, PatioId = 1, StatusId = 3, DiaSemana = 6, TempoPermanenciaReal = 120 },
            new() { ZonaId = 3, PatioId = 3, StatusId = 1, DiaSemana = 0, TempoPermanenciaReal = 36 },
            new() { ZonaId = 3, PatioId = 2, StatusId = 2, DiaSemana = 1, TempoPermanenciaReal = 60 },
        };

        var dataView = _mlContext.Data.LoadFromEnumerable(dadosTreinamento);

        // Pipeline de treino
        var pipeline = _mlContext.Transforms.Concatenate("Features", 
                nameof(MotoPrevisaoInput.ZonaId),
                nameof(MotoPrevisaoInput.PatioId),
                nameof(MotoPrevisaoInput.StatusId),
                nameof(MotoPrevisaoInput.DiaSemana))
            .Append(_mlContext.Regression.Trainers.Sdca(
                labelColumnName: nameof(MotoPrevisaoInput.TempoPermanenciaReal),
                featureColumnName: "Features"));

        // Treinar modelo
        _model = pipeline.Fit(dataView);
    }

    /// <summary>
    /// Prevê o tempo de permanência de uma moto no pátio
    /// </summary>
    public float PreverTempoPermanencia(int zonaId, int patioId, int statusId, int diaSemana)
    {
        if (_model == null)
            throw new InvalidOperationException("Modelo não foi treinado");

        var predictor = _mlContext.Model.CreatePredictionEngine<MotoPrevisaoInput, MotoPrevisaoOutput>(_model);

        var input = new MotoPrevisaoInput
        {
            ZonaId = zonaId,
            PatioId = patioId,
            StatusId = statusId,
            DiaSemana = diaSemana
        };

        var resultado = predictor.Predict(input);
        return resultado.TempoPrevistoHoras;
    }
}

/// <summary>
/// DTO para requisição de previsão
/// </summary>
public record PrevisaoRequestDto(int ZonaId, int PatioId, int StatusId, int DiaSemana);

/// <summary>
/// DTO para resposta de previsão
/// </summary>
public record PrevisaoResponseDto(
    float TempoPrevistoHoras,
    int TempoPrevistoDias,
    string Mensagem,
    PrevisaoRequestDto DadosEntrada);
