using Terrakeep.Core.Data;
using Xunit;

namespace Terrakeep.Core.Tests.Data;

// PortSeleccion Encargo5 (26-sep-2026): canario real que cierra el hueco de cobertura de los
// delegados de animacion custom de mascotas (Terraria/DelegateMethods.cs, clase
// CharacterPreview) - sin esta prueba, un futuro retoque de PetCustomAnimationCode podria romper
// en silencio la formula EXACTA del decompilado (Float/SlimePet/BerniePet) sin que ningun otro
// test lo notara, igual que ya pasaba con PetAnimationCatalogTests para OffsetX/OffsetY/
// SpriteDirection (PortSeleccion Encargo4).
//
// Valores esperados calculados a mano con la formula real citada en PetCustomAnimationCode
// (misma orden de operaciones que el decompilado, sin redondeos intermedios distintos).
public class PetCustomAnimationCodeTests
{
    [Fact]
    public void Evaluate_SinCodigo_DevuelveCero()
    {
        var (dx, dy) = PetCustomAnimationCode.Evaluate(null, elapsedTicksReal: 15f, activo: true);
        Assert.Equal(0f, dx);
        Assert.Equal(0f, dy);
    }

    [Fact]
    public void Evaluate_Inactivo_DevuelveCeroAunqueHayaCodigoYTiempo()
    {
        // "activo=false" == "walking=false" del decompilado (sin hover en Terrakeep, ver el
        // comentario de clase real) - ninguno de los 5 delegados reales deja el offset fuera de
        // (0,0) en ese estado (Float TAMBIEN vuelve a (0,0) fuera de hover porque Terrakeep solo
        // evalua el delegado durante el hover, ver RefreshPetOffset en CharacterListEntryViewModel).
        var (dx, dy) = PetCustomAnimationCode.Evaluate(PetCustomAnimationCode.Float, elapsedTicksReal: 30f, activo: false);
        Assert.Equal(0f, dx);
        Assert.Equal(0f, dy);

        var (dx2, dy2) = PetCustomAnimationCode.Evaluate(PetCustomAnimationCode.BerniePet, elapsedTicksReal: 0f, activo: false);
        Assert.Equal(0f, dx2);
        Assert.Equal(0f, dy2);
    }

    // DelegateMethods.cs:138-143 (Float):
    //   float num = 0.5f;
    //   float num2 = (float)Main.timeForVisualEffects % 60f / 60f;
    //   proj.position.Y += -num + cos(num2 * 2*PI * 2) * (num*2)
    // En ticks=0: num2=0, cos(0)=1 -> -0.5 + 1*1 = 0.5
    [Fact]
    public void Evaluate_Float_EnTickCero_DevuelveMedioPixelHaciaAbajo()
    {
        var (dx, dy) = PetCustomAnimationCode.Evaluate(PetCustomAnimationCode.Float, elapsedTicksReal: 0f, activo: true);
        Assert.Equal(0f, dx);
        Assert.Equal(0.5f, dy, precision: 4);
    }

    // En ticks=15 (num2=0.25): cos(0.25 * 4*PI) = cos(PI) = -1 -> -0.5 + (-1)*1 = -1.5
    [Fact]
    public void Evaluate_Float_ACuartoDePeriodo_DevuelveElMinimoReal()
    {
        var (_, dy) = PetCustomAnimationCode.Evaluate(PetCustomAnimationCode.Float, elapsedTicksReal: 15f, activo: true);
        Assert.Equal(-1.5f, dy, precision: 3);
    }

    // El periodo real de Float es 30 ticks (num2 recorre 0..1 en 60 ticks, multiplicado por 2
    // dentro del coseno) - a los 60 ticks (una vuelta completa de num2) debe repetirse el mismo
    // valor que en ticks=0.
    [Fact]
    public void Evaluate_Float_EsPeriodicoCada60Ticks()
    {
        var (_, dyInicial) = PetCustomAnimationCode.Evaluate(PetCustomAnimationCode.Float, elapsedTicksReal: 3f, activo: true);
        var (_, dyUnaVueltaDespues) = PetCustomAnimationCode.Evaluate(PetCustomAnimationCode.Float, elapsedTicksReal: 63f, activo: true);
        Assert.Equal(dyInicial, dyUnaVueltaDespues, precision: 4);
    }

    // FloatAndSpinWhenWalking llama a Float(proj, walking) SIEMPRE antes del if de spin
    // (DelegateMethods.cs:121) - el bob real tiene que ser IDENTICO al de Float puro. El spin
    // (rotacion) se reproduce aparte en EvaluateRotationDegrees (PortSeleccion Encargo6, ver los
    // tests de esa funcion mas abajo) - Evaluate (offset X/Y) sigue devolviendo solo el bob.
    [Fact]
    public void Evaluate_FloatAndSpinWhenWalking_ReproduceElMismoBobQueFloat()
    {
        var (_, dyFloat) = PetCustomAnimationCode.Evaluate(PetCustomAnimationCode.Float, elapsedTicksReal: 22f, activo: true);
        var (dxSpin, dySpin) = PetCustomAnimationCode.Evaluate(PetCustomAnimationCode.FloatAndSpinWhenWalking, elapsedTicksReal: 22f, activo: true);
        Assert.Equal(0f, dxSpin);
        Assert.Equal(dyFloat, dySpin, precision: 5);
    }

    // PortSeleccion Encargo6 (26-sep-2026): cierra el pendiente real del Encargo5 (RotateTransform
    // en MainWindow.xaml). Cita real, DelegateMethods.cs:119-130 (FloatAndSpinWhenWalking):
    //   if (walking) proj.rotation = (float)Math.PI * 2f * ((float)Main.timeForVisualEffects % 20f / 20f);
    //   else proj.rotation = 0f;
    // proj.rotation esta en RADIANES; EvaluateRotationDegrees devuelve GRADOS para bindear directo
    // a RotateTransform.Angle (2*PI rad == 360 grados) - 3 instantes reales del periodo de 20 ticks:
    //   ticks=0  -> percent=0    -> 0 grados
    //   ticks=5  -> percent=0.25 -> 90 grados
    //   ticks=15 -> percent=0.75 -> 270 grados
    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(5f, 90f)]
    [InlineData(15f, 270f)]
    public void EvaluateRotationDegrees_FloatAndSpinWhenWalking_ReproduceElAnguloRealEnGrados(float elapsedTicksReal, float gradosEsperados)
    {
        float grados = PetCustomAnimationCode.EvaluateRotationDegrees(PetCustomAnimationCode.FloatAndSpinWhenWalking, elapsedTicksReal, activo: true);
        Assert.Equal(gradosEsperados, grados, precision: 3);
    }

    // Periodo real de 20 ticks (DelegateMethods.cs:124, "% 20f / 20f") - a los 20 ticks debe volver
    // exactamente al angulo de ticks=0 (0 grados, no 360 - "Mod" normaliza al mismo rango que el
    // decompilado real).
    [Fact]
    public void EvaluateRotationDegrees_FloatAndSpinWhenWalking_EsPeriodicoCada20Ticks()
    {
        float gradosInicial = PetCustomAnimationCode.EvaluateRotationDegrees(PetCustomAnimationCode.FloatAndSpinWhenWalking, elapsedTicksReal: 0f, activo: true);
        float gradosUnaVueltaDespues = PetCustomAnimationCode.EvaluateRotationDegrees(PetCustomAnimationCode.FloatAndSpinWhenWalking, elapsedTicksReal: 20f, activo: true);
        Assert.Equal(gradosInicial, gradosUnaVueltaDespues, precision: 3);
    }

    // Igual que Evaluate: sin hover ("activo=false" == "walking=false" del decompilado) el angulo
    // real vuelve a 0 (DelegateMethods.cs:128, "else proj.rotation = 0f;").
    [Fact]
    public void EvaluateRotationDegrees_Inactivo_DevuelveCeroAunqueHayaTiempo()
    {
        float grados = PetCustomAnimationCode.EvaluateRotationDegrees(PetCustomAnimationCode.FloatAndSpinWhenWalking, elapsedTicksReal: 15f, activo: false);
        Assert.Equal(0f, grados);
    }

    // Las otras 4 codigos reales (Float/SlimePet/BerniePet/WormPet) nunca giran - solo
    // FloatAndSpinWhenWalking tiene "spin" en el decompilado real.
    [Theory]
    [InlineData(PetCustomAnimationCode.Float)]
    [InlineData(PetCustomAnimationCode.SlimePet)]
    [InlineData(PetCustomAnimationCode.BerniePet)]
    [InlineData(PetCustomAnimationCode.WormPet)]
    public void EvaluateRotationDegrees_OtrosDelegados_SiempreCero(string code)
    {
        float grados = PetCustomAnimationCode.EvaluateRotationDegrees(code, elapsedTicksReal: 15f, activo: true);
        Assert.Equal(0f, grados);
    }

    // DelegateMethods.cs:54-61 (SlimePet):
    //   float percent = (float)Main.timeForVisualEffects % 30f / 30f;
    //   proj.position.Y -= Utils.MultiLerp(percent, 0,0,16,20,20,16,0,0);
    // En ticks=0: percent=0 -> MultiLerp devuelve el primer valor real, 0 -> dy=0.
    [Fact]
    public void Evaluate_SlimePet_EnTickCero_NoDesplazaAunNoHayBob()
    {
        var (dx, dy) = PetCustomAnimationCode.Evaluate(PetCustomAnimationCode.SlimePet, elapsedTicksReal: 0f, activo: true);
        Assert.Equal(0f, dx);
        Assert.Equal(0f, dy);
    }

    // En ticks=15 (mitad real del periodo de 30 ticks, percent=0.5): MultiLerp con 8 valores
    // reales (0,0,16,20,20,16,0,0) evalua exactamente en el punto medio real de la tabla,
    // percent/step = 0.5/(1/7) = 3.5, cae en el segmento [3]->[4] = 20->20 (tramo plano real) -
    // MultiLerp = 20. proj.position.Y -= 20 -> dy = -20 (hacia arriba en pantalla).
    [Fact]
    public void Evaluate_SlimePet_AMitadDePeriodo_SubeElPicoRealDeLaTabla()
    {
        var (_, dy) = PetCustomAnimationCode.Evaluate(PetCustomAnimationCode.SlimePet, elapsedTicksReal: 15f, activo: true);
        Assert.Equal(-20f, dy, precision: 3);
    }

    // DelegateMethods.cs:46-52 (BerniePet): "if (walking) proj.position.X += 6f;" - constante, sin
    // dependencia de tiempo (a diferencia de Float/SlimePet).
    [Theory]
    [InlineData(0f)]
    [InlineData(500f)]
    public void Evaluate_BerniePet_SumaSeisPixelesConstantesMientrasEstaActivo(float elapsedTicksReal)
    {
        var (dx, dy) = PetCustomAnimationCode.Evaluate(PetCustomAnimationCode.BerniePet, elapsedTicksReal, activo: true);
        Assert.Equal(6f, dx);
        Assert.Equal(0f, dy);
    }

    // WormPet es el limite real confirmado (cola de N segmentos independientes rotados, no un
    // unico offset/rotacion - ver el comentario de clase completo con la cita exacta del
    // decompilado) - Evaluate nunca debe fingir un desplazamiento.
    [Fact]
    public void Evaluate_WormPet_EsElLimiteRealConfirmado_SiempreCero()
    {
        var (dx, dy) = PetCustomAnimationCode.Evaluate(PetCustomAnimationCode.WormPet, elapsedTicksReal: 100f, activo: true);
        Assert.Equal(0f, dx);
        Assert.Equal(0f, dy);
    }

    [Fact]
    public void Evaluate_CodigoDesconocido_DevuelveCeroSinLanzar()
    {
        var (dx, dy) = PetCustomAnimationCode.Evaluate("NoExisteEnElDecompilado", elapsedTicksReal: 10f, activo: true);
        Assert.Equal(0f, dx);
        Assert.Equal(0f, dy);
    }
}
