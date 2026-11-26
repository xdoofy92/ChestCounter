using System.Drawing;
using System.Runtime.Versioning;
using System.Windows.Forms;

namespace ChestCounter;

/// <summary>
/// Bienvenida y novedades, mostradas una vez por versión, también en el primer uso.
/// Mismo aspecto que el resto de la aplicación (fondo oscuro, tarjetas redondeadas, botones píldora) y el código
/// de creador DOOFY destacado, con botón para copiarlo y enlace a X.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ThanksDialog : Form
{
    public const string CreatorCode = "DOOFY";

    private const int W = 470, Pad = 20, TextX = 72;

    private ThanksDialog()
    {
        Text = "ChestCounter";
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new SizeF(96, 96);
        Font = Theme.Text();
        BackColor = Theme.Bg;
        ForeColor = Theme.Ink;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Icon = AppIcon.Load();

        // Cabecera: corazón en un círculo, título y versión.
        var icon = new Card { Location = new Point(Pad, Pad), Size = new Size(40, 40), Radius = 20, BackColor = Theme.Panel };
        icon.Controls.Add(new Label
        {
            Text = "♥",
            Font = Theme.Text(15f, FontStyle.Bold),
            ForeColor = Theme.Rose,
            BackColor = Color.Transparent,
            Location = new Point(0, 0),
            Size = new Size(40, 40),
            TextAlign = ContentAlignment.MiddleCenter,
        });
        Controls.Add(icon);

        var y = Pad - 1;
        y = AddText(this, L.T("¡Bienvenido a ChestCounter!", "Welcome to ChestCounter!"), Theme.Text(12f, FontStyle.Bold), Theme.Ink, TextX, y, W - TextX - Pad) + 4;
        y = AddText(this, L.F("Versión {0} · Un mensaje de Doofy", "Version {0} · A message from Doofy", AppInfo.VersionText), Theme.Text(9f), Theme.Muted, TextX, y, W - TextX - Pad);
        y = Math.Max(y, Pad + 40) + 16;

        // El mensaje, en una tarjeta.
        var cardW = W - 2 * Pad;
        var message = new Card { Location = new Point(Pad, y), Radius = 14, BackColor = Theme.Card };
        var my = 14;
        my = AddText(message, L.Big("releaseIntro",
            "¡Última semana de tesoros! Gracias por compartir esta locura de cofres conmigo. Ojalá Golden Week nos dé esa alegría que falta.",
            "One last week of treasure! Thanks for sharing this chest-opening adventure with me. I hope Golden Week brings us that lucky moment."),
            Theme.Text(9f), Theme.Ink, 16, my, cardW - 32) + 14;
        my = AddText(message, L.T("Novedades de esta versión", "What's new in this version"), Theme.Text(9f, FontStyle.Bold), Theme.Cyan, 16, my, cardW - 32) + 8;
        foreach (var change in new[]
        {
            L.T("• Los cofres reabastecidos cuentan al volver a abrirlos.", "• Replenished chests count each time you reopen them."),
            L.T("• Golden Week: todos los tesoros del 28/11 al 05/12.", "• Golden Week: all treasures from Nov 28 to Dec 5."),
            L.T("• Trajes ×2 en la semana; ×15 en las horas del 28/11.", "• Outfits ×2 all week; ×15 in Nov 28 Treasure Hours."),
            L.T("• Inicio en hora local y avisos según la fase del evento.", "• Local start time and notices for each event phase."),
            L.T("• Información de Golden Week en los diez idiomas.", "• Golden Week information in all ten languages."),
        })
        {
            my = AddText(message, change, Theme.Text(9f), Theme.Ink, 16, my, cardW - 32) + 7;
        }
        my += 4;
        my = AddText(message, L.T("Seguiré creando cosas para ustedes. Se les quiere. ♥", "I'll keep creating things for you. Much love. ♥"), Theme.Text(9f, FontStyle.Bold), Theme.Rose, 16, my, cardW - 32);
        message.Size = new Size(cardW, my + 14);
        Controls.Add(message);
        y += message.Height + 12;

        // Código de creador en una sola fila: el código grande en ámbar, al lado qué es y dónde usarlo, y el botón de copiar.
        var code = new Card { Location = new Point(Pad, y), Radius = 14, BackColor = Theme.Card };
        const int copyW = 84, rowPad = 12;
        var codeFont = Theme.Text(20f, FontStyle.Bold);
        var codeW = (int)Math.Ceiling(TextRenderer.MeasureText(CreatorCode, codeFont).Width / (DeviceDpi / 96f)) + 4;
        var textX = 14 + codeW + 10;
        var textW = cardW - textX - 12 - copyW - 14;
        var ty = AddText(code, L.T("Mi código de creador", "My creator code"), Theme.Text(8.5f, FontStyle.Bold), Theme.Ink, textX, rowPad, textW);
        ty = AddText(code, L.T("Si quieren apoyarme, úsalo en la tienda de Fortnite. ♥", "To support me, use it in the Fortnite Item Shop. ♥"), Theme.Text(8f), Theme.Muted, textX, ty, textW);
        var rowH = Math.Max(ty - rowPad, 38);
        code.Size = new Size(cardW, rowH + 2 * rowPad);
        code.Controls.Add(new Label
        {
            Text = CreatorCode,
            Font = codeFont,
            ForeColor = Theme.Amber,
            BackColor = Theme.Card,
            Location = new Point(12, rowPad),
            Size = new Size(codeW + 2, rowH),
            TextAlign = ContentAlignment.MiddleLeft,
        });
        var copy = new PillButton
        {
            Text = L.T("Copiar", "Copy"),
            Size = new Size(copyW, 32),
            Location = new Point(cardW - 14 - copyW, (code.Height - 32) / 2),
            Radius = 11,
        };
        copy.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(CreatorCode);
                copy.Text = L.T("¡Copiado!", "Copied!");
                copy.ForeColor = Theme.Mint;
            }
            catch
            {
                // Portapapeles ocupado por otra aplicación: el código sigue a la vista.
            }
        };
        code.Controls.Add(copy);
        Controls.Add(code);
        y += code.Height + 18;

        // Pie: "Sígueme" y los logos de las redes (los mismos que en Ajustes) a la izquierda, botón principal a la derecha.
        var followText = L.T("Sígueme", "Follow me");
        var followFont = Theme.Text(9f, FontStyle.Bold);
        var followW = TextRenderer.MeasureText(followText, followFont).Width + 4;
        Controls.Add(new Label
        {
            Text = followText,
            Font = followFont,
            ForeColor = Theme.Muted,
            BackColor = Theme.Bg,
            Location = new Point(Pad, y),
            Size = new Size(followW, 36),
            TextAlign = ContentAlignment.MiddleLeft,
        });
        var tips = new ToolTip();
        var x = Pad + followW + 8;
        foreach (var network in SettingsForm.Networks)
        {
            var url = network.Url;
            var logo = new LogoButton
            {
                Logo = SettingsForm.LoadLogo(network.Logo),
                Location = new Point(x, y + 3),
                Size = new Size(30, 30),
                AccessibleName = network.Tip,
            };
            logo.Click += (_, _) => SettingsForm.OpenUrl(url);
            tips.SetToolTip(logo, network.Tip);
            Controls.Add(logo);
            x += 30 + 6;
        }

        var ok = new PillButton
        {
            Text = L.T("¡Gracias!", "Thanks!"),
            Size = new Size(120, 36),
            Location = new Point(W - Pad - 120, y),
            Radius = 11,
            BackColor = Theme.Accent,
            HoverColor = Theme.AccentHover,
            ForeColor = Theme.AccentInk,
            DialogResult = DialogResult.OK,
        };
        Controls.Add(ok);
        AcceptButton = ok;
        CancelButton = ok;
        ActiveControl = ok;
        ClientSize = new Size(W, y + 36 + Pad);
    }

    /// <summary>Muestra el mensaje sobre la ventana dueña (tras actualizar, o con el corazón de la ventana principal).</summary>
    public static void Open(IWin32Window owner)
    {
        using var dialog = new ThanksDialog();
        dialog.ShowDialog(owner);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Theme.Chrome(Handle);
    }

    /// <summary>Texto con salto de línea, del alto justo. Devuelve la coordenada de debajo.</summary>
    private int AddText(Control parent, string text, Font font, Color color, int x, int y, int width)
    {
        // Se mide a 96 ppp: el escalado automático de la ventana ya agranda después todo junto.
        var scale = DeviceDpi / 96f;
        var measured = TextRenderer.MeasureText(text, font, new Size((int)(width * scale), int.MaxValue), TextFormatFlags.WordBreak | TextFormatFlags.NoPadding);
        var height = (int)Math.Ceiling(measured.Height / scale) + 2;
        parent.Controls.Add(new Label
        {
            Text = text,
            Font = font,
            ForeColor = color,
            BackColor = parent.BackColor,
            Location = new Point(x, y),
            Size = new Size(width, height),
        });
        return y + height;
    }
}
