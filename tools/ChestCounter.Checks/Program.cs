using System.Drawing;
using System.Reflection;
using System.Text.Json;
using ChestCounter;
using System.Windows.Forms;

internal static class Program
{
    private static int _checks;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); _checks++; }
    [STAThread]
    private static void Main(string[] args)
    {
        var output = Path.GetFullPath(args[0]);
        Directory.CreateDirectory(output);
        var analyzer = new ReplayAnalyzer();
        var export = typeof(ReplayAnalyzer).GetMethod("OnExportRead", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var searches = (System.Collections.IList)typeof(ReplayAnalyzer).GetField("_searches", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(analyzer)!;
        void Send(bool? searched = null, uint? pawn = null, bool destroyed = false) => export.Invoke(analyzer,
            new object[] { 1u, new FortniteReplayReader.Models.NetFieldExports.Items.GenericContainer { bAlreadySearched = searched, SearchingPawn = pawn, bDestroyed = destroyed } });
        Send(false);
        Send(true, 10);
        Send(true, 10);
        Send(pawn: 10);
        Send(true, 10);
        Check(searches.Count == 1, "Repeated or sparse exports duplicate an opening");
        Send(false);
        Send();
        Send(true, 20);
        Check(searches.Count == 2, "Replenished chest opening missing");
        Check((uint?)searches[1]!.GetType().GetProperty("SearcherPawn")!.GetValue(searches[1]) == 20, "Replenished chest attributed to previous player");
        Send(false);
        Send(true);
        Check(searches.Count == 3 && searches[2]!.GetType().GetProperty("SearcherPawn")!.GetValue(searches[2]) == null, "Reset retained stale searcher");
        Send(false);
        Send(pawn: 30, destroyed: true);
        Send(pawn: 30, destroyed: true);
        Send(true, 30);
        Check(searches.Count == 4, "Destroyed replenished chest missing or duplicated");
        var goldenHours = TreasureHours.Schedule.Where(w => w.GoldOutfitMultiplier == 15m).ToArray();
        Check(goldenHours.Length == 2, "Golden Week must boost both November 28 ET sessions");
        foreach (var hour in goldenHours)
        {
            Check(hour.StartUtc.Kind == DateTimeKind.Utc && hour.EndUtc - hour.StartUtc == TimeSpan.FromHours(2), "Treasure Hour UTC duration changed");
            Check(TreasureHours.Active(hour.StartUtc) == hour, "Treasure Hour start excluded");
            Check(TreasureHours.Active(hour.EndUtc) == null, "Treasure Hour end included");
            Check(TreasureHours.Boosts(hour).Contains("×15"), "Golden Week boost missing from UI text");
        }
        Check(TreasureHours.Schedule.Where(w => !goldenHours.Contains(w)).All(w => w.GoldOutfitMultiplier == 7.5m), "Historical boosts changed");
        Check(TreasureHours.GoldenWeekNotice(new DateTime(2026, 12, 2, 18, 0, 0, DateTimeKind.Utc)) != null, "Midweek Golden Week notice missing");
        Check(TreasureHours.Active(new DateTime(2026, 12, 2, 18, 0, 0, DateTimeKind.Utc)) == null, "Golden Week treated as continuous Treasure Hours");
        var originalLanguage = L.Code;
        var start = TreasureHours.GoldenWeekStartUtc;
        var finalDay = new DateTime(2026, 12, 5, 5, 0, 0, DateTimeKind.Utc);
        var retired = new DateTime(2026, 12, 6, 5, 0, 0, DateTimeKind.Utc);
        L.Use("es");
        Check(TreasureHours.GoldenWeekNotice(start.AddTicks(-1))!.Contains("Empieza"), "Boost advertised before official start");
        Check(TreasureHours.GoldenWeekNotice(start)!.Contains("En curso"), "Golden Week not live at official start");
        Check(TreasureHours.GoldenWeekNotice(finalDay)!.Contains("evento"), "Final day needs event wording instead of an invented end time");
        Check(!TreasureHours.GoldenWeekNotice(finalDay)!.Contains("×2"), "Final day claims boost remains active after unknown event time");
        Check(TreasureHours.GoldenWeekNotice(retired.AddTicks(-1)) != null && TreasureHours.GoldenWeekNotice(retired) == null, "Notice retirement must use the announcement's ET calendar date");
        L.Use("en");
        var english = new[] { TreasureHours.GoldenWeekNotice(start.AddTicks(-1)), TreasureHours.GoldenWeekNotice(start), TreasureHours.GoldenWeekNotice(finalDay), TreasureHours.GoldenWeekDetails, TreasureHours.Boosts(goldenHours[0]) };
        foreach (var (code, _) in L.Languages.Where(l => l.Code is not "es" and not "en"))
        {
            L.Use(code);
            var texts = new[] { TreasureHours.GoldenWeekNotice(start.AddTicks(-1)), TreasureHours.GoldenWeekNotice(start), TreasureHours.GoldenWeekNotice(finalDay), TreasureHours.GoldenWeekDetails, TreasureHours.Boosts(goldenHours[0]) };
            Check(texts.Where((text, i) => string.IsNullOrWhiteSpace(text) || text == english[i]).Count() == 0, "Golden Week translation missing: " + code);
            Check(texts.All(text => !text!.Contains("{0}")), "Unformatted translation: " + code);
        }
        L.Use(originalLanguage);
        Check(AppInfo.Version == new Version(2, 0, 29, 0), "Release version was not updated");
        foreach (var p in new[] { "Playlist_DefaultSolo", "Playlist_DefaultSquad", "Playlist_NoBuildBR_Trio", "Playlist_Trios", "Playlist_HabaneroBR_Solo" })
            Check(Profiles.IsBattleRoyalePlaylist(p), "BR rejected: " + p);
        foreach (var p in new string?[] { null, "", "Playlist_Creative", "Playlist_BlastBerry_Squad", "Playlist_Reload_Solo", "Playlist_DefaultSquad_LTM", "Playlist_Delulu", "Playlist_Festival" })
            Check(!Profiles.IsBattleRoyalePlaylist(p), "Other mode accepted: " + p);
        var own = new TreasureFind { Item = "Own reward", RawItem = "own" };
        var team = new TreasureFind { Item = "Team reward", Scope = "team", Finder = "Mate", RawItem = "team" };
        var nearby = new TreasureFind { Item = "Pirate Shop Access", Scope = "nearby", Finder = "Opponent", RawItem = "PirateShopAccess", Evidence = "recipient-handler" };
        var match = new MatchRecord { Playlist = "Playlist_NoBuildBR_Squad", GameVersion = "++Fortnite+Release-43.10", PlayerName = "Test", EpicId = "test", ChestsOpened = 3,
            TreasureFinds = new() { own }, NearbyTreasureFinds = new() { team, nearby }, Teammates = new() { new() { Name = "Mate", ChestsSeen = 4 } } };
        var config = new Config();
        Check(config.NeedsWelcome, "First-time user must see the welcome/changelog");
        config.ThanksShown = true;
        Check(config.NeedsWelcome, "Legacy thank-you flag hides the new changelog");
        config.WelcomeVersion = "2.0.26";
        Check(config.NeedsWelcome, "Upgrade must show the new changelog");
        config.WelcomeVersion = AppInfo.VersionText;
        Check(!config.NeedsWelcome, "Welcome repeats on the same version");
        var restoredConfig = JsonSerializer.Deserialize<Config>(JsonSerializer.Serialize(config))!;
        Check(!restoredConfig.NeedsWelcome, "Welcome acknowledgement does not persist");
        Check(Profiles.Adjust(match, config)!.TreasureFinds.Count == 1, "Team treasures default off");
        config.CountTeammateTreasures = true;
        var adjusted = Profiles.Adjust(match, config)!;
        Check(adjusted.TreasureFinds.Count == 2, "Shared treasures missing");
        Check(adjusted.ChestsOpened == 3, "Independent chest setting changed");
        Check(adjusted.AllTreasureFinds.Count() == 3, "Report duplicates shared treasures");
        Check(adjusted.NearbyTreasureFinds.Count == 1, "Nearby enemy included in own total");
        Check(match.TreasureFinds.Count == 1 && match.NearbyTreasureFinds.Count == 2, "Stored data changed");
        config.CountTeammateChests = true;
        Check(Profiles.Adjust(match, config)!.ChestsOpened == 7, "Squad chest regression");
        config.CountTeammateTreasures = false;
        Check(Profiles.Adjust(match, config)!.TreasureFinds.Count == 1, "Toggle not reversible");
        Check(!new TreasureFind { RawItem = "WID_PirateShopAccess", Evidence = "handler-export" }.IsPirateToken, "Class export mistaken for an obtained token");
        Check(new TreasureFind { RawItem = "WID_PirateShopAccess", Evidence = "recipient-handler" }.IsPirateToken, "Confirmed token receipt ignored");
        Check(match.PirateTokenSeen, "Enemy token not noticed");
        Check(!TreasureNames.IsPirateToken("DelilahPrize:dpid_item16"), "Guessed numeric prize");
        Check(!TreasureNames.IsPirateToken("WID_SmugglerExtractionKey"), "Extraction key mistaken for Pirate token");
        var json = JsonSerializer.Serialize(match);
        var loaded = JsonSerializer.Deserialize<MatchRecord>(json)!;
        Check(loaded.PirateTokenSeen && loaded.AllTreasureFinds.Count() == 3, "Persistence lost rewards");
        var other = new MatchRecord { Playlist = "Playlist_Creative", GameVersion = match.GameVersion, ChestsOpened = 999, EpicId = "test" };
        var data = new TrackerData { Matches = new() { match, other } };
        Check(Profiles.View(data, config).Matches.Count == 1, "Legacy other mode leaks into stats");
        Check(!ReportWriter.Csv(data).Contains("999"), "Other mode leaks into CSV");
        Check(ChestKinds.Classify("B_SpritePosters_Booty_Chest_C") == ChestKinds.PosterChest, "Poster chest not recognized");
        Check(!ChestKinds.IsChest(ChestKinds.Classify("B_SpritePosters_Poster_C")), "Map collection counted as a chest");
        Check(ChestKinds.Classify("Tiered_Ammo_Athena_C") == ChestKinds.AmmoBox, "Ammo classification changed");
        Check(ChestKinds.Classify("FoodBox_Large_C") == ChestKinds.FoodBox, "Food classification changed");
        config.CountTeammateTreasures = true;
        var html = ReportWriter.Report(config, Profiles.View(data, config), Profiles.List(data, config));
        Check(html.Contains("Opponent") && html.Contains("Mate") && html.Contains("Pirate Shop Access"), "Treasure report lost scopes");
        Check(html.Contains("TOKEN", StringComparison.OrdinalIgnoreCase), "Token alert missing in report");
        File.WriteAllText(Path.Combine(output, "report-test.html"), html);
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        var welcomeLanguage = L.Code;
        var tipType = typeof(MainForm).Assembly.GetType("ChestCounter.ThemedToolTip")!;
        var measureTip = tipType.GetMethod("Measure", BindingFlags.Static | BindingFlags.NonPublic)!;
        var paintTip = tipType.GetMethod("PaintTip", BindingFlags.Static | BindingFlags.NonPublic)!;
        var placeTip = tipType.GetMethod("Place", BindingFlags.Static | BindingFlags.NonPublic)!;
        var workArea = new Rectangle(-1920, 40, 1920, 1040);
        var popupSize = new Size(360, 160);
        foreach (var cursor in new[] { new Point(-1000, 400), new Point(-1900, 50), new Point(-10, 50), new Point(-10, 1070), new Point(-1900, 1070) })
        {
            var position = (Point)placeTip.Invoke(null, new object[] { cursor, popupSize, workArea, 16 })!;
            Check(workArea.Contains(new Rectangle(position, popupSize)), "Tooltip escaped monitor working area");
            Check(Math.Abs(position.X - cursor.X) <= popupSize.Width + 16 && Math.Abs(position.Y - cursor.Y) <= popupSize.Height + 16, "Tooltip detached from cursor");
            if (cursor == new Point(-1000, 400)) Check(position == new Point(-984, 416), "Tooltip is not anchored beside the pointer");
        }
        foreach (var (code, _) in L.Languages)
        {
            L.Use(code);
            foreach (var dpi in new[] { 96, 144, 192 })
            {
                var scale = dpi / 96f;
                using var font = new Font("Segoe UI", 9f * scale);
                var padding = (int)Math.Round(12 * scale);
                foreach (var screenWidth in new[] { 320, 1920 })
                {
                    var maxWidth = Math.Min((int)(360 * scale), screenWidth - 2 * padding);
                    var tipSize = (Size)measureTip.Invoke(null, new object[] { TreasureHours.GoldenWeekDetails, font, padding, maxWidth })!;
                    Check(tipSize.Width <= maxWidth && tipSize.Width > padding * 2, "Tooltip exceeds available width: " + code);
                    Check(tipSize.Height > font.Height * 2 + padding * 2, "Golden Week tooltip did not wrap: " + code);
                    using var tip = new Bitmap(tipSize.Width, tipSize.Height);
                    using var graphics = Graphics.FromImage(tip);
                    paintTip.Invoke(null, new object[] { graphics, new Rectangle(Point.Empty, tipSize), TreasureHours.GoldenWeekDetails, font, padding });
                    if (dpi == 96 && screenWidth == 1920) tip.Save(Path.Combine(output, "tooltip-" + code + ".png"));
                }
            }
            using var welcome = (Form)Activator.CreateInstance(typeof(ThanksDialog), nonPublic: true)!;
            welcome.StartPosition = FormStartPosition.Manual;
            welcome.Location = new Point(-30000, -30000);
            welcome.Show();
            var creator = welcome.Controls.Cast<Control>().Single(c => c.Controls.OfType<Label>().Any(l => l.Text == ThanksDialog.CreatorCode));
            var changes = welcome.Controls.Cast<Control>().Single(c => c.Controls.OfType<Label>().Any(l => l.Text.StartsWith("•")));
            Check(creator.Top >= changes.Bottom, "Creator code must follow the changelog: " + code);
            Check(changes.Controls.OfType<Label>().Count(l => l.Text.StartsWith("•")) == 5, "Release notes missing: " + code);
            Check(welcome.Controls.Cast<Control>().All(c => c.Right <= welcome.ClientSize.Width && c.Bottom <= welcome.ClientSize.Height), "Welcome layout overflow: " + code);
            foreach (var label in changes.Controls.OfType<Label>())
            {
                var size = TextRenderer.MeasureText(label.Text, label.Font, new Size(label.Width, int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
                Check(size.Height <= label.Height, "Welcome text clipped: " + code);
            }
            using var preview = new Bitmap(welcome.Width, welcome.Height);
            welcome.DrawToBitmap(preview, new Rectangle(Point.Empty, preview.Size));
            preview.Save(Path.Combine(output, "welcome-" + code + ".png"));
        }
        L.Use(welcomeLanguage);
        var store = new Store(Path.Combine(output, "isolated-data"));
        var tracker = new Tracker(store, config, data);
        var monitor = new ChestCounter.Monitor(store, config, data, tracker, store.Folder);
        using (var settings = new SettingsForm(monitor, store))
        {
            settings.StartPosition = FormStartPosition.Manual;
            settings.Location = new Point(-30000, -30000);
            settings.Show();
            Check(settings.FormBorderStyle == FormBorderStyle.Sizable && settings.MaximizeBox, "Settings cannot resize");
            var baseSize = settings.ClientSize;
            settings.ClientSize = new Size(baseSize.Width * 3 / 2, baseSize.Height * 3 / 2);
            Check(settings.Controls.Cast<Control>().Where(c => c.Visible).All(c => c.Right <= settings.ClientSize.Width + 2 && c.Bottom <= settings.ClientSize.Height + 2), "Resized settings overflow");
            using var bitmap = new Bitmap(settings.Width, settings.Height);
            settings.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(output, "settings-resized.png"));
            settings.ClientSize = baseSize;
            using var normal = new Bitmap(settings.Width, settings.Height);
            settings.DrawToBitmap(normal, new Rectangle(Point.Empty, normal.Size));
            normal.Save(Path.Combine(output, "settings-normal.png"));
        }
        using (var main = new MainForm(monitor, store))
        {
            main.ShowInTaskbar = false;
            main.StartPosition = FormStartPosition.Manual;
            main.Location = new Point(-30000, -30000);
            main.Show();
            var stats = ChestStats.Empty with { LastMatch = new(3, 0, 1, 1, 80, 2, true) };
            var state = new MonitorState(OverlayModel.Build(Profiles.View(data, config), null, false), Profiles.List(data, config), "test", null, null, false, stats);
            typeof(MainForm).GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(main, new object[] { state });
            var buttons = main.Controls.Cast<Control>().Where(c => c is PillButton && (c.Text == "Ajustes" || c.Text == "Settings" || c.Text == "Informe" || c.Text == "Report")).ToList();
            Check(buttons.Count == 2 && buttons.All(c => c.Visible), "Token banner hides action buttons");
            using var bitmap = new Bitmap(main.Width, main.Height);
            main.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
            bitmap.Save(Path.Combine(output, "main-token.png"));
            var teamState = state with { Team = new TeamSummary(false, 3, new[] { new TeamMember("Mate", 4) }, false) };
            typeof(MainForm).GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(main, new object[] { teamState });
            Check(main.ClientSize.Height > 430 && buttons.All(c => c.Visible), "Team and token panels overlap actions");
            using var combined = new Bitmap(main.Width, main.Height);
            main.DrawToBitmap(combined, new Rectangle(Point.Empty, combined.Size));
            combined.Save(Path.Combine(output, "main-token-team.png"));
            var noToken = state with { Stats = ChestStats.Empty };
            typeof(MainForm).GetMethod("Render", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(main, new object[] { noToken });
            Check(main.ClientSize.Height == 382, "Persistent token status is missing");
            using var emptyToken = new Bitmap(main.Width, main.Height);
            main.DrawToBitmap(emptyToken, new Rectangle(Point.Empty, emptyToken.Size));
            emptyToken.Save(Path.Combine(output, "main-token-empty.png"));
        }
        Console.WriteLine($"PASS: {_checks} regression checks; UI images and report saved in {output}");
    }
}
