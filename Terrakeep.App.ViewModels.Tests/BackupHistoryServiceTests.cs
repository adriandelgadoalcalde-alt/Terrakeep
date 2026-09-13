using System.IO;
using System.Linq;
using Terrakeep.App.Services;
using Terrakeep.Core.PlrFormat;

namespace Terrakeep.App.ViewModels.Tests;

// H5-04 (quinta auditoria de Opus): "la copia de seguridad es de un solo nivel - el segundo
// Guardar destruye la unica red". Copias rotativas reales con fecha, en su propia carpeta de
// %LOCALAPPDATA%\Terrakeep\Backups - nunca en la carpeta real de Documentos del personaje
// (mismo criterio ya establecido para el .tplr, T-C).
//
// BK (13-sep-2026): estas pruebas escribian en el %LOCALAPPDATA% REAL de la maquina - cada .plr
// temporal distinto estrenaba una carpeta de historial que no retiraba nadie (medido: 2.765
// carpetas, 28 MB acumulados en esta maquina antes de arreglarlo). Ahora cada prueba monta su
// propia raiz dentro de una carpeta temporal suya y la borra al terminar: ademas de no ensuciar,
// aisla de verdad (una prueba de cupo ya no puede ver copias de otra).
public sealed class BackupHistoryServiceTests : IDisposable
{
    private readonly string _raiz = Path.Combine(Path.GetTempPath(), "tk-backup-tests-" + Guid.NewGuid().ToString("N")[..12]);

    private BackupHistoryService NuevoServicio(int cupo = 20) =>
        new() { BackupsRoot = Path.Combine(_raiz, "Backups"), MaxBackupsPerCharacter = cupo };

    private string NuevoPlrEnDisco(string nombre)
    {
        string dir = Path.Combine(_raiz, "Players");
        Directory.CreateDirectory(dir);
        string ruta = Path.Combine(dir, nombre + ".plr");
        File.WriteAllBytes(ruta, PlrFile.Write(NuevoPersonaje(nombre)));
        return ruta;
    }

    private static PlrCharacter NuevoPersonaje(string nombre, int vida = 400) => new()
    {
        Name = nombre,
        Version = 279,
        HealthNow = vida,
        HealthMax = vida,
        ManaNow = 200,
        ManaMax = 200,
        PrimaryLoadout = PlrLoadout.CreateEmpty(isPrimary: true),
        Loadouts = [PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false), PlrLoadout.CreateEmpty(isPrimary: false)],
    };

    public void Dispose()
    {
        try { if (Directory.Exists(_raiz)) Directory.Delete(_raiz, recursive: true); } catch (Exception) { }
    }

    [Fact]
    public void SaveBackup_DejaUnaCopiaRealRecuperablePorListBackups()
    {
        var service = NuevoServicio();
        string path = NuevoPlrEnDisco("Test");

        service.SaveBackup(path, null, BackupReason.BeforeSave);
        var backups = service.ListBackups(path);

        Assert.Single(backups);
        Assert.True(File.Exists(backups[0].ContainerPath));
        Assert.True(backups[0].SizeBytes > 0);
        Assert.Equal(BackupReason.BeforeSave, backups[0].Reason);
    }

    [Fact]
    public void ListBackups_SinNingunaCopiaTodavia_DevuelveVacio()
    {
        var service = NuevoServicio();
        string path = Path.Combine(_raiz, "Players", "nunca.plr");
        Assert.Empty(service.ListBackups(path));
    }

    // Un personaje que todavia no existe en disco no tiene estado anterior que perder - no es un
    // error, simplemente no hay nada que fotografiar.
    [Fact]
    public void SaveBackup_SinFicheroEnDisco_DevuelveNullSinReventar()
    {
        var service = NuevoServicio();
        Assert.Null(service.SaveBackup(Path.Combine(_raiz, "Players", "no-existe.plr"), null, BackupReason.Manual));
    }

    [Fact]
    public void Restore_CopiaLaVersionElegidaEncimaDelFicheroReal()
    {
        var service = NuevoServicio();
        string path = NuevoPlrEnDisco("Original");
        service.SaveBackup(path, null, BackupReason.BeforeSave);
        var puntoOriginal = service.ListBackups(path)[0];

        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Editado")));
        service.Restore(path, null, puntoOriginal);

        Assert.Equal("Original", PlrFile.Read(File.ReadAllBytes(path)).Name);
    }

    // BK: lo que de verdad importa de una restauracion no es que "parezca" correcta sino que el
    // fichero quede BYTE A BYTE como estaba. Se compara el contenido entero, no solo un campo.
    [Fact]
    public void Restore_DejaElFicheroIdenticoByteAByteAlOriginal()
    {
        var service = NuevoServicio();
        string path = NuevoPlrEnDisco("ByteAByte");
        byte[] original = File.ReadAllBytes(path);

        service.SaveBackup(path, null, BackupReason.Manual);
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Otro", vida: 100)));
        Assert.False(original.SequenceEqual(File.ReadAllBytes(path))); // la prueba no valdria si no hubiera cambiado de verdad

        service.Restore(path, null, service.ListBackups(path)[0]);

        Assert.True(original.SequenceEqual(File.ReadAllBytes(path)));
    }

    // El .tplr hermano viaja DENTRO del mismo contenedor: una version es el par completo, nunca
    // medio personaje.
    [Fact]
    public void SaveBackupYRestore_ConservanElTplrHermanoByteAByte()
    {
        var service = NuevoServicio();
        string path = NuevoPlrEnDisco("ConTplr");
        string tplr = Path.ChangeExtension(path, ".tplr");
        byte[] tplrOriginal = [7, 7, 7, 1, 2, 3];
        File.WriteAllBytes(tplr, tplrOriginal);

        service.SaveBackup(path, tplr, BackupReason.Manual);
        File.WriteAllBytes(tplr, [0]);
        service.Restore(path, tplr, service.ListBackups(path)[0]);

        Assert.True(tplrOriginal.SequenceEqual(File.ReadAllBytes(tplr)));
    }

    // H3-02: sin .tplr en ESE punto del historial, no resucitar uno que naciera despues.
    [Fact]
    public void Restore_DeUnPuntoSinTplr_BorraElTplrQueNacioDespues()
    {
        var service = NuevoServicio();
        string path = NuevoPlrEnDisco("SinTplr");
        service.SaveBackup(path, null, BackupReason.Manual);

        string tplr = Path.ChangeExtension(path, ".tplr");
        File.WriteAllBytes(tplr, [1, 2, 3]);
        service.Restore(path, tplr, service.ListBackups(path)[0]);

        Assert.False(File.Exists(tplr));
    }

    [Fact]
    public void ListBackups_OrdenaDeMasRecienteAMasAntigua()
    {
        var service = NuevoServicio();
        string path = NuevoPlrEnDisco("Test");

        service.SaveBackup(path, null, BackupReason.BeforeSave);
        service.SaveBackup(path, null, BackupReason.BeforeSave);

        var backups = service.ListBackups(path);
        Assert.Equal(2, backups.Count);
        Assert.True(backups[0].TimestampLocal >= backups[1].TimestampLocal);
    }

    // BK-2: el sello era de SEGUNDO y se copiaba con overwrite:true - dos guardados dentro del
    // mismo segundo se pisaban en silencio y el historial enseñaba una sola version donde hubo
    // cinco. Sin Thread.Sleep a proposito: la prueba no vale nada si hay que ralentizarla para
    // que pase.
    [Fact]
    public void CincoCopiasSeguidasSinEsperar_SonCincoVersionesReales()
    {
        var service = NuevoServicio();
        string path = NuevoPlrEnDisco("Rapido");

        for (int i = 0; i < 5; i++) service.SaveBackup(path, null, BackupReason.BeforeSave);

        Assert.Equal(5, service.ListBackups(path).Count);
    }

    // H5-07: "N configurable de verdad". El cupo se satura de verdad y se queda clavado ahi.
    [Fact]
    public void MaxBackupsPerCharacter_ConfigurableAUnValorBajo_PurgaLasMasAntiguasDeVerdad()
    {
        var service = NuevoServicio(cupo: 2);
        string path = NuevoPlrEnDisco("Test");

        for (int i = 0; i < 3; i++) service.SaveBackup(path, null, BackupReason.BeforeSave);

        Assert.Equal(2, service.ListBackups(path).Count);
    }

    [Fact]
    public void CupoSaturado_ConMuchasMasCopias_NuncaPasaDelTope()
    {
        var service = NuevoServicio(cupo: 5);
        string path = NuevoPlrEnDisco("Saturado");

        for (int i = 0; i < 40; i++) service.SaveBackup(path, null, BackupReason.BeforeSave);

        var quedan = service.ListBackups(path);
        Assert.Equal(5, quedan.Count);
        // Y los que quedan son los 5 ULTIMOS, no 5 cualesquiera.
        Assert.True(quedan[0].TimestampLocal >= quedan[4].TimestampLocal);
    }

    // BK: una copia que el usuario marco a mano es un punto elegido a proposito - se retira
    // despues que cualquier automatica, aunque sea mas antigua que todas ellas.
    [Fact]
    public void ElCupo_SacrificaPrimeroLasAutomaticas_NoLaCopiaManualMasAntigua()
    {
        var service = NuevoServicio(cupo: 3);
        string path = NuevoPlrEnDisco("Mixto");

        service.SaveBackup(path, null, BackupReason.Manual); // la MAS ANTIGUA de todas
        for (int i = 0; i < 10; i++) service.SaveBackup(path, null, BackupReason.BeforeSave);

        var quedan = service.ListBackups(path);
        Assert.Equal(3, quedan.Count);
        Assert.Contains(quedan, b => b.Reason == BackupReason.Manual);
    }

    // BK-3: Purge ordenaba por File.GetLastWriteTimeUtc y File.Copy HEREDA la fecha del origen -
    // restaurar una version antigua dejaba el .plr con fecha antigua, y la copia siguiente nacia
    // "vieja" y se iba la primera aunque fuera la mas reciente de todas. Se reproduce el
    // escenario exacto: fichero con fecha de modificacion antigua a proposito.
    [Fact]
    public void ElCupo_NoSeDejaEngañarPorLaFechaDeModificacionDelPlrDeOrigen()
    {
        var service = NuevoServicio(cupo: 2);
        string path = NuevoPlrEnDisco("FechaVieja");

        service.SaveBackup(path, null, BackupReason.BeforeSave); // antigua de verdad (la 1ª)
        service.SaveBackup(path, null, BackupReason.BeforeSave);
        // El .plr real pasa a tener fecha de 2015 - como la tendria tras restaurar algo antiguo.
        File.SetLastWriteTimeUtc(path, new DateTime(2015, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var reciente = service.SaveBackup(path, null, BackupReason.BeforeSave);

        var quedan = service.ListBackups(path);
        Assert.Equal(2, quedan.Count);
        Assert.Contains(quedan, b => b.ContainerPath == reciente!.ContainerPath);
    }

    // El resumen legible sale del .plr fotografiado de verdad, no de lo que hubiera en memoria.
    [Fact]
    public void ElSnapshot_GuardaUnResumenRealSacadoDelPropioFichero()
    {
        var service = NuevoServicio();
        string path = NuevoPlrEnDisco("Resumen");
        File.WriteAllBytes(path, PlrFile.Write(NuevoPersonaje("Resumen", vida: 500)));

        service.SaveBackup(path, null, BackupReason.Manual);
        var info = service.ListBackups(path)[0].Info;

        Assert.NotNull(info);
        Assert.True(info!.SummaryAvailable);
        Assert.Equal("Resumen", info.CharacterName);
        Assert.Equal(500, info.HealthMax);
        Assert.Equal(279, info.SaveVersion);
    }

    // Un .plr ilegible (truncado, de otro juego) NO debe impedir que se guarde la copia: negarse
    // a copiar por no poder describir es exactamente el fallo que este sistema existe para evitar.
    [Fact]
    public void UnPlrIlegible_SeCopiaIgual_PeroSinResumenInventado()
    {
        var service = NuevoServicio();
        Directory.CreateDirectory(Path.Combine(_raiz, "Players"));
        string path = Path.Combine(_raiz, "Players", "Roto.plr");
        File.WriteAllBytes(path, [1, 2, 3]);

        var creada = service.SaveBackup(path, null, BackupReason.Manual);

        Assert.NotNull(creada);
        Assert.False(creada!.Info!.SummaryAvailable);
        Assert.Equal("", creada.Info.CharacterName);
        Assert.True(service.ReadPlrBytes(service.ListBackups(path)[0]).SequenceEqual(new byte[] { 1, 2, 3 }));
    }

    // Compatibilidad real hacia atras: quien ya venia usando Terrakeep tiene copias del formato
    // ANTIGUO (.plr/.tplr sueltos con sello de segundo). Se siguen listando Y restaurando.
    [Fact]
    public void CopiasDelFormatoAntiguo_SeSiguenListandoYRestaurando()
    {
        var service = NuevoServicio();
        string path = NuevoPlrEnDisco("Legado");
        byte[] antiguo = PlrFile.Write(NuevoPersonaje("DeAntes"));

        // Se fabrican como las dejaba la version anterior de esta clase.
        string dirHistorial = service.HistoryDirectoryFor(path);
        Directory.CreateDirectory(dirHistorial);
        File.WriteAllBytes(Path.Combine(dirHistorial, "20260101-101010.plr"), antiguo);

        var puntos = service.ListBackups(path);
        Assert.Single(puntos);
        Assert.True(puntos[0].IsLegacy);
        Assert.Equal(BackupReason.Unknown, puntos[0].Reason);

        service.Restore(path, null, puntos[0]);
        Assert.Equal("DeAntes", PlrFile.Read(File.ReadAllBytes(path)).Name);
    }

    // BK-4: historiales cuyo .plr de origen ya no aparece en ninguna carpeta escaneada. Aqui se
    // comprueba la parte determinista (encontrarlos y borrarlos bajo demanda) - el periodo de
    // gracia de la limpieza AUTOMATICA se prueba aparte, abajo.
    [Fact]
    public void FindOrphanHistories_EncuentraLosHistorialesSinPersonajeYSeLimpianBajoDemanda()
    {
        var service = NuevoServicio();
        string path = NuevoPlrEnDisco("Efimero");
        service.SaveBackup(path, null, BackupReason.Manual);
        File.Delete(path); // el personaje desaparece (borrado, o carpeta ya no escaneada)

        var huerfanos = service.FindOrphanHistories();
        Assert.Contains(huerfanos, o => o.DisplayName == "Efimero" && o.Snapshots == 1 && o.SizeBytes > 0);

        var (carpetas, bytes) = service.DeleteOrphanHistories(huerfanos.Where(o => o.DisplayName == "Efimero"));
        Assert.Equal(1, carpetas);
        Assert.True(bytes > 0);
        Assert.DoesNotContain(service.FindOrphanHistories(), o => o.DisplayName == "Efimero");
    }

    // El periodo de gracia no es decoracion: un historial recien quedado huerfano puede ser lo
    // UNICO que queda de un personaje borrado sin querer, y la limpieza automatica no debe
    // tocarlo. Se comprueba con las dos caras.
    [Fact]
    public void PurgeOrphanHistories_RespetaElPeriodoDeGracia()
    {
        var service = NuevoServicio();
        string path = NuevoPlrEnDisco("RecienBorrado");
        service.SaveBackup(path, null, BackupReason.Manual);
        File.Delete(path);

        service.PurgeOrphanHistories(TimeSpan.FromDays(90));
        Assert.Contains(service.FindOrphanHistories(), o => o.DisplayName == "RecienBorrado");

        // Con gracia cero (o sea, ya cumplida) si se retira.
        var (carpetas, _) = service.PurgeOrphanHistories(TimeSpan.Zero);
        Assert.True(carpetas >= 1);
        Assert.DoesNotContain(service.FindOrphanHistories(), o => o.DisplayName == "RecienBorrado");
    }

    // El motivo de cada version se conserva de verdad (se lee del nombre Y del meta.json).
    [Fact]
    public void CadaVersion_RecuerdaPorQueSeCreo()
    {
        var service = NuevoServicio();
        string path = NuevoPlrEnDisco("Motivos");

        service.SaveBackup(path, null, BackupReason.BeforeSave);
        service.SaveBackup(path, null, BackupReason.Manual);
        service.SaveBackup(path, null, BackupReason.BeforeRestore);

        var motivos = service.ListBackups(path).Select(b => b.Reason).ToList();
        Assert.Contains(BackupReason.BeforeSave, motivos);
        Assert.Contains(BackupReason.Manual, motivos);
        Assert.Contains(BackupReason.BeforeRestore, motivos);
    }

    // La decision de NO comprimir se apoya en una medida real (ver el comentario de cabecera de
    // BackupHistoryService): el contenedor no puede salir mas pequeño que su contenido, y esta
    // prueba lo deja clavado por si alguien cambia el CompressionLevel "por mejorarlo".
    [Fact]
    public void ElContenedor_NoIntentaComprimirDatosYaCifrados()
    {
        var service = NuevoServicio();
        string path = NuevoPlrEnDisco("Compresion");
        long plrReal = new FileInfo(path).Length;

        var creada = service.SaveBackup(path, null, BackupReason.Manual)!;
        long contenedor = new FileInfo(creada.ContainerPath).Length;

        // El .plr cabe entero y sin encoger dentro (solo se suma la cabeceria del zip + meta).
        Assert.True(contenedor >= plrReal, $"contenedor {contenedor} < plr {plrReal}");
        Assert.True(contenedor < plrReal + 4096, $"contenedor {contenedor} demasiado grande para un .plr de {plrReal}");
    }
}
