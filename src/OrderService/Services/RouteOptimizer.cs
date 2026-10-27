namespace OrderService.Services;

public enum StopType
{
    Pickup,
    Delivery
}

// Paragem de uma rota: recolha na origem ou entrega no destino de uma encomenda
public record RouteStop(Guid OrderId, StopType Type, string Name, double Latitude, double Longitude);

// Otimização da ordem das paragens de um estafeta (classe pura, sem base de dados nem HTTP).
// A matriz tem o ponto de partida no índice 0 e a paragem i no índice i + 1.
public static class RouteOptimizer
{
    // Calcula a rota: vizinho mais próximo seguido de melhorias 2-opt.
    // Se for dada uma rota alternativa válida (ex: a ordem ingénua), também a melhora e fica com a melhor.
    public static List<int> Optimize(double[,] matriz, IReadOnlyList<RouteStop> paragens, IReadOnlyList<int>? rotaAlternativa = null)
    {
        var rota = TwoOpt(matriz, paragens, NearestNeighbor(matriz, paragens));

        if (rotaAlternativa != null && RespeitaPrecedencia(paragens, rotaAlternativa))
        {
            var alternativa = TwoOpt(matriz, paragens, rotaAlternativa.ToList());
            if (Cost(matriz, alternativa) < Cost(matriz, rota)) rota = alternativa;
        }

        return rota;
    }

    // 1. Vizinho mais próximo: vai sempre para a paragem válida mais perto
    public static List<int> NearestNeighbor(double[,] matriz, IReadOnlyList<RouteStop> paragens)
    {
        var rota = new List<int>();
        var visitadas = new bool[paragens.Count];
        var atual = 0; // Ponto de partida

        while (rota.Count < paragens.Count)
        {
            var melhor = -1;
            for (var i = 0; i < paragens.Count; i++)
            {
                if (visitadas[i] || !PodeVisitar(paragens, visitadas, i)) continue;
                if (melhor == -1 || matriz[atual, i + 1] < matriz[atual, melhor + 1]) melhor = i;
            }

            rota.Add(melhor);
            visitadas[melhor] = true;
            atual = melhor + 1;
        }

        return rota;
    }

    // 2. Melhoria 2-opt: inverte troços da rota enquanto a distância total diminuir
    public static List<int> TwoOpt(double[,] matriz, IReadOnlyList<RouteStop> paragens, List<int> rotaInicial)
    {
        var rota = new List<int>(rotaInicial);
        var custo = Cost(matriz, rota);
        var melhorou = true;

        while (melhorou)
        {
            melhorou = false;
            for (var i = 0; i < rota.Count - 1; i++)
            {
                for (var k = i + 1; k < rota.Count; k++)
                {
                    var candidata = new List<int>(rota);
                    candidata.Reverse(i, k - i + 1);

                    // Ignorar trocas que ponham uma entrega antes da recolha
                    if (!RespeitaPrecedencia(paragens, candidata)) continue;

                    var custoCandidata = Cost(matriz, candidata);
                    if (custoCandidata < custo - 1e-9)
                    {
                        rota = candidata;
                        custo = custoCandidata;
                        melhorou = true;
                    }
                }
            }
        }

        return rota;
    }

    // Distância total da rota, a partir do ponto de partida (sem regresso)
    public static double Cost(double[,] matriz, IReadOnlyList<int> rota)
    {
        var total = 0.0;
        var atual = 0;
        foreach (var paragem in rota)
        {
            total += matriz[atual, paragem + 1];
            atual = paragem + 1;
        }
        return total;
    }

    // Verifica se, em toda a rota, cada recolha vem antes da entrega da mesma encomenda
    public static bool RespeitaPrecedencia(IReadOnlyList<RouteStop> paragens, IReadOnlyList<int> rota)
    {
        var recolhidas = new HashSet<Guid>();
        var temRecolha = paragens.Where(p => p.Type == StopType.Pickup).Select(p => p.OrderId).ToHashSet();

        foreach (var indice in rota)
        {
            var paragem = paragens[indice];
            if (paragem.Type == StopType.Pickup)
                recolhidas.Add(paragem.OrderId);
            else if (temRecolha.Contains(paragem.OrderId) && !recolhidas.Contains(paragem.OrderId))
                return false;
        }
        return true;
    }

    // Método auxiliar: uma entrega só pode ser visitada depois da recolha (se a recolha estiver na rota)
    private static bool PodeVisitar(IReadOnlyList<RouteStop> paragens, bool[] visitadas, int indice)
    {
        var paragem = paragens[indice];
        if (paragem.Type == StopType.Pickup) return true;

        for (var i = 0; i < paragens.Count; i++)
        {
            if (paragens[i].Type == StopType.Pickup && paragens[i].OrderId == paragem.OrderId)
                return visitadas[i];
        }
        return true; // Encomenda já recolhida (InTransit): só falta a entrega
    }
}
