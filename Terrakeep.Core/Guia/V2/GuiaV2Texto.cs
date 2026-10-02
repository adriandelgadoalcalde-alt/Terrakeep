using System.Text;

namespace Terrakeep.Core.Guia.V2;

// Marcado en linea de los textos de la guia v2. Deliberadamente minimo, para que las dos UIs
// (WPF y la UI nativa de Terraria) lo pinten igual sin un motor HTML:
//   **negrita**
//   {o:Terraria/SlimeCrown}      objeto  -> sprite + nombre en español (clicable: como conseguirlo)
//   {n:CalamityMod/DesertScourgeHead} NPC/jefe -> icono + nombre en español
//   {z:sunken_sea}               zona del mundo -> nombre (clicable: mapa)
//   {p:desert}                   enlace a otra parada
//   {a:mapa}                     enlace a un articulo del manual
// Un token puede llevar texto propio tras '|' ({o:Terraria/Gel|gel}) para concordancias
// gramaticales; si no, la UI usa el nombre de la tabla de referencias.
public enum TipoSegmento
{
    Texto = 0,
    Negrita,
    Objeto,
    Npc,
    Zona,
    Parada,
    Articulo,
}

public sealed record SegmentoTexto(TipoSegmento Tipo, string Valor, string? TextoPropio = null);

public static class GuiaV2Texto
{
    public static List<SegmentoTexto> Analizar(string? texto)
    {
        var salida = new List<SegmentoTexto>();
        if (string.IsNullOrEmpty(texto)) return salida;

        var buf = new StringBuilder();
        bool negrita = false;
        int i = 0;
        while (i < texto.Length)
        {
            char c = texto[i];
            if (c == '*' && i + 1 < texto.Length && texto[i + 1] == '*')
            {
                Vaciar(salida, buf, negrita);
                negrita = !negrita;
                i += 2;
                continue;
            }
            if (c == '{' && i + 2 < texto.Length && texto[i + 2] == ':')
            {
                int cierre = texto.IndexOf('}', i);
                TipoSegmento? tipo = texto[i + 1] switch
                {
                    'o' => TipoSegmento.Objeto,
                    'n' => TipoSegmento.Npc,
                    'z' => TipoSegmento.Zona,
                    'p' => TipoSegmento.Parada,
                    'a' => TipoSegmento.Articulo,
                    _ => null,
                };
                if (cierre > i && tipo.HasValue)
                {
                    Vaciar(salida, buf, negrita);
                    string cuerpo = texto[(i + 3)..cierre];
                    int barra = cuerpo.IndexOf('|');
                    salida.Add(barra >= 0
                        ? new SegmentoTexto(tipo.Value, cuerpo[..barra], cuerpo[(barra + 1)..])
                        : new SegmentoTexto(tipo.Value, cuerpo));
                    i = cierre + 1;
                    continue;
                }
            }
            buf.Append(c);
            i++;
        }
        Vaciar(salida, buf, negrita);
        return salida;
    }

    private static void Vaciar(List<SegmentoTexto> salida, StringBuilder buf, bool negrita)
    {
        if (buf.Length == 0) return;
        salida.Add(new SegmentoTexto(negrita ? TipoSegmento.Negrita : TipoSegmento.Texto, buf.ToString()));
        buf.Clear();
    }

    /// <summary>Texto plano (busqueda, lectores de pantalla, recuento de palabras), con los
    /// tokens ya sustituidos por su nombre en español.</summary>
    public static string Plano(string? texto, GuiaV2Referencias? refs, Func<string, string?>? nombreZona = null,
        Func<string, string?>? tituloParada = null, Func<string, string?>? tituloArticulo = null)
    {
        var sb = new StringBuilder();
        foreach (var s in Analizar(texto))
        {
            sb.Append(s.Tipo switch
            {
                TipoSegmento.Texto or TipoSegmento.Negrita => s.Valor,
                _ when s.TextoPropio != null => s.TextoPropio,
                TipoSegmento.Objeto => refs != null && refs.Objetos.TryGetValue(s.Valor, out var o) ? o.Es : s.Valor,
                TipoSegmento.Npc => refs != null && refs.Npcs.TryGetValue(s.Valor, out var n) ? n.Es : s.Valor,
                TipoSegmento.Zona => nombreZona?.Invoke(s.Valor) ?? s.Valor,
                TipoSegmento.Parada => tituloParada?.Invoke(s.Valor) ?? s.Valor,
                TipoSegmento.Articulo => tituloArticulo?.Invoke(s.Valor) ?? s.Valor,
                _ => s.Valor,
            });
        }
        return sb.ToString();
    }
}
