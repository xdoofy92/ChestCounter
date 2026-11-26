namespace ChestCounter;

/// <summary>
/// Horas del tesoro (Cazatesoros, noviembre–diciembre de 2026): durante esas dos horas aumenta la probabilidad de los
/// tesoros (Acceso a la Tienda pirata ×1,5, Traje dorado ×7,5, vehículo ×10; trajes ×15 el 28 de noviembre).
/// Golden Week: https://communities.epicgames.com/thread/golden-week-starts-november-28/MuNA/
/// El anuncio actualiza multiplicadores, no franjas horarias. Horario en UTC, como lo publica
/// fortnite.bot/treasure: cada sábado de 19:00 a 21:00 y la madrugada del domingo de 02:00 a 04:00. Se pasa a la hora
/// local de Windows al mostrarlo. Sin conexión: el horario va dentro de la aplicación.
/// </summary>
public static class TreasureHours
{
    public sealed record Window(DateTime StartUtc, DateTime EndUtc)
    {
        public decimal GoldOutfitMultiplier { get; init; } = 7.5m;
        public DateTime StartLocal => StartUtc.ToLocalTime();
        public DateTime EndLocal => EndUtc.ToLocalTime();
    }

    public static readonly IReadOnlyList<Window> Schedule = new[]
    {
        W("2026-11-07T19:00"), W("2026-11-08T02:00"),
        new Window(Utc("2026-11-14T19:00"), Utc("2026-11-14T23:00")), W("2026-11-15T02:00"),
        W("2026-11-21T19:00"), W("2026-11-22T02:00"),
        // Ambas sesiones del sábado en ET: ×7,5 de Hora del tesoro ×2 de Golden Week = ×15.
        W("2026-11-28T19:00", 15m), W("2026-11-29T02:00", 15m),
    };

    /// <summary>Hora del tesoro en curso, o null.</summary>
    public static Window? Active(DateTime utcNow) => All.FirstOrDefault(w => utcNow >= w.StartUtc && utcNow < w.EndUtc);

    /// <summary>Próxima Hora del tesoro (aún sin empezar), o null si ya no quedan.</summary>
    public static Window? Next(DateTime utcNow) => All.Where(w => w.StartUtc > utcNow).OrderBy(w => w.StartUtc).FirstOrDefault();

    public static string Boosts(Window window) => L.F(
        "Tienda pirata ×1,5 · Traje dorado ×{0} · Vehículos/calcomanías ×10",
        "Pirate Shop ×1.5 · Gold outfit ×{0} · Vehicles/decals ×10",
        window.GoldOutfitMultiplier.ToString("0.#", L.Culture));

    public static readonly DateTime GoldenWeekStartUtc = Utc("2026-11-28T14:00");

    // Fechas del anuncio en ET (UTC−5 en noviembre/diciembre). La retirada del aviso al día siguiente
    // no representa un fin confirmado: el día 5 solo indicamos que termina con el evento, sin afirmar ×2 activo.
    public static string? GoldenWeekNotice(DateTime utcNow)
    {
        if (utcNow < Utc("2026-11-24T00:00") || utcNow >= Utc("2026-12-06T05:00")) return null;
        if (utcNow < GoldenWeekStartUtc)
            return L.F("Golden Week · Empieza {0} (hora local)", "Golden Week · Starts {0} (local time)",
                GoldenWeekStartUtc.ToLocalTime().ToString("dd/MM HH:mm", L.Culture));
        if (utcNow >= Utc("2026-12-05T05:00"))
            return L.T("Golden Week · Finaliza con el evento del 05/12", "Golden Week · Ends with the Dec 5 event");
        return L.T("Golden Week · En curso · Todos los tesoros · Trajes ×2",
            "Golden Week · Live · All treasures · Outfits ×2");
    }

    public static string GoldenWeekDetails => L.Big("goldenWeekDetails",
        "Desde el 28/11 a las 9:00 ET hasta el evento del 05/12: vuelven los tesoros de semanas anteriores. Probabilidad de cualquier tesoro: 1 entre 21 (excluye duplicados propios). Trajes dorados ×2 de base; ×15 total durante las Horas del tesoro del 28/11. Vehículos y calcomanías ×10 y Tienda pirata ×1,5 solo durante esas horas.",
        "From Nov 28 at 9 AM ET until the Dec 5 event: treasures from previous weeks return. Any treasure: 1 in 21 (excluding owned duplicates). Gold outfits: ×2 base odds; ×15 total during Nov 28 Treasure Hours. Vehicles and decals ×10 and Pirate Shop ×1.5 only during those hours.");

    /// <summary>
    /// Desarrollo: CHESTCOUNTER_TREASURE_PREVIEW=1 simula una Hora del tesoro en curso (modo dorado) y =2 una que
    /// empieza dentro de 3 horas (cuenta atrás del día).
    /// </summary>
    private static readonly IReadOnlyList<Window> All = Environment.GetEnvironmentVariable("CHESTCOUNTER_TREASURE_PREVIEW") switch
    {
        "1" => new[] { new Window(DateTime.UtcNow.AddMinutes(-15), DateTime.UtcNow.AddMinutes(105)) },
        "2" => new[] { new Window(DateTime.UtcNow.AddHours(3), DateTime.UtcNow.AddHours(5)) },
        _ => Schedule,
    };


    /// <summary>"sáb 21/11 16:00", en hora local.</summary>
    public static string When(Window w) => w.StartLocal.ToString("ddd dd/MM HH:mm", L.Culture);

    /// <summary>Cuenta atrás: "1 d 05:23:10", o "05:23:10" si queda menos de un día.</summary>
    public static string Countdown(TimeSpan left)
    {
        if (left < TimeSpan.Zero) left = TimeSpan.Zero;
        var clock = $"{left.Hours:00}:{left.Minutes:00}:{left.Seconds:00}";
        return left.Days > 0 ? $"{left.Days} d {clock}" : clock;
    }

    private static Window W(string startUtc, decimal goldOutfitMultiplier = 7.5m) =>
        new(Utc(startUtc), Utc(startUtc).AddHours(2)) { GoldOutfitMultiplier = goldOutfitMultiplier };

    private static DateTime Utc(string s) =>
        DateTime.SpecifyKind(DateTime.ParseExact(s, "yyyy-MM-dd'T'HH:mm", System.Globalization.CultureInfo.InvariantCulture), DateTimeKind.Utc);
}
