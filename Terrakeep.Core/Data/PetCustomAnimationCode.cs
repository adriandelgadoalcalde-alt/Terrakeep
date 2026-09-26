namespace Terrakeep.Core.Data;

// PortSeleccion Encargo5 (26-sep-2026, investigacion propia sobre el hallazgo de PortSeleccion
// original, arquitecto-keep a07c2c8f): el offset/posicion BASE de cada mascota (OffsetX/OffsetY/
// SpriteDirection) ya se porto en el Encargo4 - esta clase cubre la capa que faltaba, los
// delegados de animacion CUSTOM que Terraria asigna con ".WithCode(...)" a algunos proyectiles de
// mascota en la tabla real (Terraria/ID/ProjectileID.cs, CharacterPreviewAnimations,
// Downloads\Keep\tModLoader-Decompiled\tModLoader\Terraria\ID\ProjectileID.cs:34-37) - codigo real
// que se ejecuta ADEMAS del ciclo de fotogramas (SelStart/SelCount/SelDelay), tomado literal de
// Terraria/DelegateMethods.cs (clase CharacterPreview,
// Downloads\Keep\tModLoader-Decompiled\tModLoader\Terraria\DelegateMethods.cs:13-156).
//
// Recuento real (no "~15 delegados" del hallazgo original - esa cifra SI describe el numero de
// mascotas catalogadas que usan el delegado "Float", una entre 5 delegados distintos realmente
// usados por las 63 mascotas ya catalogadas en pet_animations.json):
//   - Float                    (proj.position.Y bob continuo, SIN mirar "walking")  -> 15 mascotas
//   - FloatAndSpinWhenWalking  (Float + proj.rotation cuando camina)                ->  2 mascotas
//   - SlimePet                 (proj.position.Y bob distinto, SOLO si camina)       ->  3 mascotas
//   - BerniePet                (proj.position.X += 6 constante, SOLO si camina)     ->  1 mascota
//   - WormPet                  (cola de N segmentos rotados, ver limite mas abajo)  ->  3 mascotas
//   - CompanionCubePet/EtsyPet (bob+rotacion / rotacion orbital)                    ->  0 mascotas
//     catalogadas ahora mismo (proyectiles 653/1018/764 no tienen entrada en
//     pet_animations.json - el objeto que los dispara no esta en el catalogo de 63, asi que esta
//     clase NO necesita cubrirlos hoy; si el catalogo crece con uno de esos shoot, "code" quedaria
//     en null hasta que se añada aqui con la misma cita real de evidencia).
//
// LIMITE REAL de "walking"/hover en Terrakeep: mientras la tarjeta de Inicio esta en hover, el
// unico estado real es "animado" (equivalente a walking=true en el decompilado) - Terrakeep nunca
// simula el estado "no seleccionado" (walking=false) porque nunca dibuja el doll de forma animada
// fuera del hover (ver CharacterListEntryViewModel.SetHovering). Por eso "activo" (el bool que
// recibe Evaluate) sustituye directamente a "walking": true durante el hover, false en reposo -
// exactamente cuando Terraria real mostraria una mascota QUIETA por no estar en la ventana de
// personajes seleccionada.
//
// LIMITE REAL confirmado (WormPet): el delegado real recorre "proj.oldPos" (un rastro de N
// posiciones historicas reales del propio proyectil, T-1, T-2...) y calcula la rotacion de CADA
// segmento por separado a partir de un vector que rota -0.05235987901687622 rad por segmento
// (DelegateMethods.cs:87-116) - un gusano de N segmentos independientes, no una imagen estatica
// con un solo offset/rotacion. Terrakeep renderiza la mascota como una UNICA imagen recortada de
// una hoja de sprite (PetPreviewRenderer.RenderFrame) - reproducirlo con fidelidad exigiria
// dibujar y rotar N imagenes/segmentos por fotograma, una arquitectura de render distinta que
// esta fuera de alcance de este encargo. Devuelve siempre (0,0) para no fingir un movimiento que
// no es el real.
//
// PortSeleccion Encargo6 (26-sep-2026, cierra el pendiente real dejado por el Encargo5 de arriba):
// "FloatAndSpinWhenWalking" ya aplicaba el mismo bob que "Float", pero le faltaba el "spin"
// (proj.rotation = 2*PI*(tiempo%20/20) mientras camina, DelegateMethods.cs:119-130). Ahora
// EvaluateRotationDegrees calcula ese angulo (en GRADOS, ver el comentario real de esa funcion
// para la conversion) y Terrakeep.App/MainWindow.xaml lo bindea con un RotateTransform NUEVO
// dentro del mismo TransformGroup que ya tenia ScaleTransform+TranslateTransform (PortSeleccion
// Encargo4) - combinado, nunca sustituido. Para las otras 61 mascotas (sin este delegado) el
// angulo siempre es 0, RotateTransform identidad, sin cambio visual respecto a antes.
public static class PetCustomAnimationCode
{
    public const string Float = "Float";
    public const string FloatAndSpinWhenWalking = "FloatAndSpinWhenWalking";
    public const string SlimePet = "SlimePet";
    public const string BerniePet = "BerniePet";
    public const string WormPet = "WormPet";

    // "activo" == "walking" real del decompilado (ver comentario de clase). "elapsedTicksReal" es
    // tiempo TRANSCURRIDO desde que el hover empezo, en TICKS de juego reales (60/s, igual unidad
    // que "Main.timeForVisualEffects" del decompilado) - PetAnimationDriver.ElapsedTicksReal ya
    // hace esa conversion desde ms reales.
    public static (float DeltaX, float DeltaY) Evaluate(string? code, float elapsedTicksReal, bool activo)
    {
        if (!activo || code is null) return (0f, 0f);
        return code switch
        {
            Float => (0f, FloatBob(elapsedTicksReal)),
            // El bob es identico al de Float (DelegateMethods.cs:121, "Float(proj, walking)"
            // llamado SIEMPRE antes del if de spin) - el spin (rotacion) queda fuera, ver el
            // comentario real de clase mas arriba.
            FloatAndSpinWhenWalking => (0f, FloatBob(elapsedTicksReal)),
            SlimePet => (0f, -SlimeBob(elapsedTicksReal)),
            BerniePet => (6f, 0f),
            _ => (0f, 0f),
        };
    }

    // PortSeleccion Encargo6 (26-sep-2026): angulo de "spin" real, en GRADOS (RotateTransform.Angle
    // de WPF espera grados, no radianes como el decompilado). Solo "FloatAndSpinWhenWalking" gira -
    // las demas 61 mascotas catalogadas siempre devuelven 0 (RotateTransform identidad).
    //
    // Cita real, DelegateMethods.cs:119-130 (FloatAndSpinWhenWalking):
    //   public static void FloatAndSpinWhenWalking(Projectile proj, bool walking) {
    //     Float(proj, walking);
    //     if (walking) { proj.rotation = (float)Math.PI * 2f * ((float)Main.timeForVisualEffects % 20f / 20f); }
    //     else { proj.rotation = 0f; }
    //   }
    // "activo" hace de "walking" real (ver el comentario de clase, mismo criterio que en Evaluate).
    // 2*PI radianes == 360 grados exactos, asi que "percent * 360f" reproduce la MISMA formula sin
    // el redondeo extra de convertir por PI/180 - no es una aproximacion distinta, es la version en
    // grados de la misma cuenta.
    public static float EvaluateRotationDegrees(string? code, float elapsedTicksReal, bool activo)
    {
        if (code != FloatAndSpinWhenWalking || !activo) return 0f;
        float percent = Mod(elapsedTicksReal, 20f) / 20f;
        return percent * 360f;
    }

    // Cita real, DelegateMethods.cs:138-143 (Float):
    //   float num = 0.5f;
    //   float num2 = (float)Main.timeForVisualEffects % 60f / 60f;
    //   proj.position.Y += 0f - num + (float)(Math.Cos(num2 * (Math.PI*2f) * 2f) * (double)(num*2f));
    // Nota real: Float NUNCA mira "walking" - el bob corre siempre que el delegado se evalue (aqui,
    // siempre que "activo" sea true, ver Evaluate).
    private static float FloatBob(float elapsedTicksReal)
    {
        const float num = 0.5f;
        float num2 = Mod(elapsedTicksReal, 60f) / 60f;
        return -num + (float)(Math.Cos(num2 * (Math.PI * 2.0) * 2.0) * (num * 2f));
    }

    // Cita real, DelegateMethods.cs:54-61 (SlimePet):
    //   if (walking) {
    //     float percent = (float)Main.timeForVisualEffects % 30f / 30f;
    //     proj.position.Y -= Utils.MultiLerp(percent, 0f, 0f, 16f, 20f, 20f, 16f, 0f, 0f);
    //   }
    // Devuelve el MultiLerp (positivo); Evaluate le cambia el signo para reproducir el "-=" real.
    private static float SlimeBob(float elapsedTicksReal)
    {
        float percent = Mod(elapsedTicksReal, 30f) / 30f;
        return MultiLerp(percent, 0f, 0f, 16f, 20f, 20f, 16f, 0f, 0f);
    }

    // Cita real, Terraria/Utils.cs:216-227 (Utils.MultiLerp) - interpolacion lineal por tramos
    // entre "floats.Length-1" segmentos iguales, portada literal (misma formula, mismo orden de
    // operaciones) para no introducir ninguna diferencia de redondeo respecto al original.
    private static float MultiLerp(float percent, params float[] floats)
    {
        float step = 1f / (floats.Length - 1f);
        float bound = step;
        int index = 0;
        while (percent / bound > 1f && index < floats.Length - 2)
        {
            bound += step;
            index++;
        }
        float t = (percent - step * index) / step;
        return floats[index] + (floats[index + 1] - floats[index]) * t;
    }

    // (float)Main.timeForVisualEffects es un contador de ticks SIEMPRE >= 0 en el juego real, pero
    // "elapsedTicksReal" aqui es un float que solo crece (nunca negativo en la practica, viene de
    // un cronometro real) - Mod existe solo por seguridad (igual que el operador "%" de C# ya se
    // comporta como se espera con floats no negativos, este helper documenta la intencion real).
    private static float Mod(float value, float modulus) => value % modulus;
}
