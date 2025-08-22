using PCL.Core.ProgramSetup;

namespace PCL.Core.UI;

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

public partial class MotdRenderer {
    // Default Color for originalColorMap: #808080
    // Minecraft color code mapping
    private readonly Dictionary<string, Brush> _colorMapWithBlackBackground = new Dictionary<string, Brush> {
        { "0", Brushes.Black }, // Black
        { "1", new SolidColorBrush(Color.FromRgb(0, 0, 170)) }, // Dark Blue
        { "2", new SolidColorBrush(Color.FromRgb(0, 170, 0)) }, // Dark Green
        { "3", new SolidColorBrush(Color.FromRgb(0, 170, 170)) }, // Cyan
        { "4", new SolidColorBrush(Color.FromRgb(170, 0, 0)) }, // Dark Red
        { "5", new SolidColorBrush(Color.FromRgb(170, 0, 170)) }, // Purple
        { "6", new SolidColorBrush(Color.FromRgb(255, 170, 0)) }, // Gold
        { "7", Brushes.LightGray }, // Gray
        { "8", Brushes.DarkGray }, // Dark Gray
        { "9", Brushes.Blue }, // Blue
        { "a", Brushes.Lime }, // Green
        { "b", Brushes.Cyan }, // Cyan
        { "c", Brushes.Red }, // Red
        { "d", Brushes.Magenta }, // Magenta
        { "e", Brushes.Yellow }, // Yellow
        { "f", Brushes.White } // White
    };

    // Color code mapping optimized for white background (#f3f6fa)
    private readonly Dictionary<string, Brush> _colorMapWithWhiteBackground = new Dictionary<string, Brush> {
        { "0", new SolidColorBrush(Color.FromRgb(51, 51, 51)) }, // Deep Gray #333333
        { "1", new SolidColorBrush(Color.FromRgb(0, 48, 135)) }, // Navy Blue #003087
        { "2", new SolidColorBrush(Color.FromRgb(0, 128, 0)) }, // Forest Green #008000
        { "3", new SolidColorBrush(Color.FromRgb(0, 122, 122)) }, // Cyan #007A7A
        { "4", new SolidColorBrush(Color.FromRgb(161, 0, 0)) }, // Deep Red #A10000
        { "5", new SolidColorBrush(Color.FromRgb(128, 0, 128)) }, // Deep Purple #800080
        { "6", new SolidColorBrush(Color.FromRgb(204, 112, 0)) }, // Deep Orange #CC7000
        { "7", new SolidColorBrush(Color.FromRgb(102, 102, 102)) }, // Medium Gray #666666
        { "8", new SolidColorBrush(Color.FromRgb(68, 68, 68)) }, // Charcoal #444444
        { "9", new SolidColorBrush(Color.FromRgb(0, 68, 204)) }, // Royal Blue #0044CC
        { "a", new SolidColorBrush(Color.FromRgb(0, 153, 0)) }, // Green #009900
        { "b", new SolidColorBrush(Color.FromRgb(0, 161, 161)) }, // Cyan #00A1A1
        { "c", new SolidColorBrush(Color.FromRgb(204, 0, 0)) }, // Red #CC0000
        { "d", new SolidColorBrush(Color.FromRgb(194, 0, 194)) }, // Magenta #C200C2
        { "e", new SolidColorBrush(Color.FromRgb(179, 160, 0)) }, // Deep Yellow #B3A000
        { "f", new SolidColorBrush(Color.FromRgb(136, 136, 136)) } // White
    };

    // Format code mapping
    private readonly Dictionary<string, bool> _formatMap = new Dictionary<string, bool> {
        { "l", true }, // Bold
        { "o", true }, // Italic
        { "n", true }, // Underline
        { "m", true }, // Strikethrough
        { "k", true }, // Obfuscated (not supported)
        { "r", false } // Reset
    };

    // Store TextBlock and original text for §k obfuscated text
    private readonly List<(TextBlock TextBlock, string OriginalText)> _obfuscatedTextBlocks =
        new List<(TextBlock, string)>();

    private readonly Random _random = new Random();
    private readonly string _randomChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()";
    private readonly Color _backgroundColor = Color.FromRgb(243, 246, 250); // #f3f6fa

    public MotdRenderer() {
        InitializeComponent(); // 初始化 XAML 定义的控件
        // Start timer to update §k text
        var timer = new DispatcherTimer {
            Interval = TimeSpan.FromMilliseconds(20)
        };
        timer.Tick += _UpdateObfuscatedText;
        timer.Start();
    }

    public void Clear() {
        
    }

    private void _UpdateObfuscatedText(object? sender, EventArgs e) {
        foreach (var item in _obfuscatedTextBlocks) {
            var textBlock = item.TextBlock;
            var originalText = item.OriginalText;
            // Generate random characters of the same length as the original text
            var obfuscated = string.Join("",
                Enumerable.Range(0, originalText.Length).Select(_ => _randomChars[_random.Next(_randomChars.Length)]));
            textBlock.Text = obfuscated;
        }
    }

    private double _GetRelativeLuminance(Color color) {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;
        double rL = r <= 0.03928 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
        double gL = g <= 0.03928 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
        double bL = b <= 0.03928 ? b / 12.92 : Math.Pow((b + 0.055) / 1.055, 2.4);
        return 0.2126 * rL + 0.7152 * gL + 0.0722 * bL;
    }

    private double _GetContrastRatio(Color foreground, Color background) {
        double l1 = _GetRelativeLuminance(foreground);
        double l2 = _GetRelativeLuminance(background);
        return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
    }

    private Color _AdjustColorForContrast(Color inputColor) {
        double contrastRatio = _GetContrastRatio(inputColor, _backgroundColor);
        if (contrastRatio >= 4.5) return inputColor; // Contrast is sufficient

        // Convert RGB to HSL
        double r = inputColor.R / 255.0;
        double g = inputColor.G / 255.0;
        double b = inputColor.B / 255.0;
        double max = Math.Max(Math.Max(r, g), b);
        double min = Math.Min(Math.Min(r, g), b);
        double l = (max + min) / 2.0;
        double s;
        double h;

        if (Math.Abs(max - min) < double.Epsilon) {
            h = 0.0;
            s = 0.0;
        } else {
            double d = max - min;
            s = l > 0.5 ? d / (2.0 - max - min) : d / (max + min);
            h = max switch {
                _ when Math.Abs(max - r) < double.Epsilon => (g - b) / d + (g < b ? 6.0 : 0.0),
                _ when Math.Abs(max - g) < double.Epsilon => (b - r) / d + 2.0,
                _ => (r - g) / d + 4.0
            };
            h /= 6.0;
        }

        // Decrease lightness until contrast ratio ≥ 4.5:1
        double newL = l;
        Color adjustedColor = inputColor;
        while (newL > 0.1 && _GetContrastRatio(adjustedColor, _backgroundColor) < 4.5) {
            newL -= 0.05; // Gradually decrease lightness
            double newR, newG, newB;
            if (s == 0) {
                newR = newL;
                newG = newL;
                newB = newL;
            } else {
                double q = newL < 0.5 ? newL * (1.0 + s) : newL + s - newL * s;
                double p = 2.0 * newL - q;
                newR = _HueToRgb(p, q, h + 1.0 / 3.0);
                newG = _HueToRgb(p, q, h);
                newB = _HueToRgb(p, q, h - 1.0 / 3.0);
            }

            adjustedColor = Color.FromRgb((byte)(newR * 255), (byte)(newG * 255), (byte)(newB * 255));
        }

        // If contrast is still insufficient, use default color #555555
        if (_GetContrastRatio(adjustedColor, _backgroundColor) < 4.5) {
            return Color.FromRgb(85, 85, 85); // colorMap["f"]
        }

        return adjustedColor;
    }

    private double _HueToRgb(double p, double q, double t) {
        if (t < 0) t += 1.0;
        if (t > 1) t -= 1.0;
        if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
        if (t < 0.5) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
        return p;
    }

    public void RenderMotd(string motd, bool isWhiteBackground = true) {
        MotdCanvas.Children.Clear();
        _obfuscatedTextBlocks.Clear();

        var colorMap = isWhiteBackground ? _colorMapWithWhiteBackground : _colorMapWithBlackBackground;
        string font = Setup.Ui.Font; // Assuming Setup is a static class accessible in the project
        var fontFamily = new FontFamily(string.IsNullOrWhiteSpace(font)
            ? "./Resources/#PCL English, Segoe UI, Microsoft YaHei UI"
            : font);
        double fontSize = 12;
        double canvasWidth = MotdCanvas.ActualWidth > 0 ? MotdCanvas.ActualWidth : 300; // Prevent zero width
        double canvasHeight = MotdCanvas.ActualHeight > 0 ? MotdCanvas.ActualHeight : 34; // Prevent zero height
        double y = 10;

        // Regex to match § codes and RGB colors
        var regex = new Regex("(§[0-9a-fk-oAr]|#[0-9A-Fa-f]{6})");

        // Split multi-line MOTD
        motd = motd.Replace("\n", "\r\n");
        string[] lines = motd.Split("\r\n");
        Brush currentColor = colorMap["f"];
        bool isBold = false;
        bool isItalic = false;
        bool isUnderline = false;
        bool isStrikethrough = false;
        bool isObfuscated = false;

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++) {
            string line = lines[lineIndex].Trim();
            string[] parts = regex.Split(line);

            // Calculate line width
            double lineWidth = 0;
            double lineHeight = 0;
            double tempX = 0; // Temporary x-coordinate for width calculation
            var textBlocks = new List<TextBlock>(); // Store TextBlocks for the line
            var positions = new List<double>(); // Store x-coordinates for each TextBlock

            foreach (string part in parts) {
                if (string.IsNullOrEmpty(part)) continue;

                // Handle § color codes
                if (part.StartsWith("§") && part.Length == 2) {
                    string code = part.Substring(1).ToLower();
                    if (colorMap.ContainsKey(code)) {
                        currentColor = colorMap[code];
                        isBold = false;
                        isItalic = false;
                        isUnderline = false;
                        isStrikethrough = false;
                        isObfuscated = false;
                    } else if (_formatMap.ContainsKey(code)) {
                        if (code == "l") isBold = true;
                        if (code == "o") isItalic = true;
                        if (code == "n") isUnderline = true;
                        if (code == "m") isStrikethrough = true;
                        if (code == "k") isObfuscated = true;
                        if (code == "r") {
                            currentColor = colorMap["f"];
                            isBold = false;
                            isItalic = false;
                            isUnderline = false;
                            isStrikethrough = false;
                            isObfuscated = false;
                        }
                    }

                    continue;
                }

                // Handle RGB color codes
                if (Regex.IsMatch(part, "^#[0-9A-Fa-f]{6}$")) {
                    try {
                        string hex = part.Substring(1);
                        byte r = Convert.ToByte(hex.Substring(0, 2), 16);
                        byte g = Convert.ToByte(hex.Substring(2, 2), 16);
                        byte b = Convert.ToByte(hex.Substring(4, 2), 16);
                        Color inputColor = Color.FromRgb(r, g, b);
                        currentColor = new SolidColorBrush(_AdjustColorForContrast(inputColor));
                        isBold = false;
                        isItalic = false;
                        isUnderline = false;
                        isStrikethrough = false;
                        isObfuscated = false;
                    } catch {
                        // Invalid RGB color, keep current color
                    }

                    continue;
                }

                // Render text, always use original text for width calculation
                string displayText = part;
                TextBlock textBlock;
                if (isObfuscated) {
                    // Generate initial random characters for §k text
                    foreach (char singleChar in part) {
                        //Log(singleChar); // Assuming Log is a method accessible in the project
                        displayText = _randomChars[_random.Next(_randomChars.Length)].ToString();
                        textBlock = _RenderText(displayText, fontFamily, fontSize, currentColor, isBold, isItalic,
                            isUnderline, isStrikethrough, tempX, y, true,
                            _MeasureTextWidth(singleChar.ToString(), fontFamily, fontSize, isBold, isItalic));
                        _obfuscatedTextBlocks.Add((textBlock, singleChar.ToString()));
                        textBlocks.Add(textBlock);
                        positions.Add(tempX);
                        tempX += _MeasureTextWidth(singleChar.ToString(), fontFamily, fontSize, isBold, isItalic);
                    }
                } else {
                    textBlock = _RenderText(displayText, fontFamily, fontSize, currentColor, isBold, isItalic,
                        isUnderline, isStrikethrough, tempX, y);
                    textBlocks.Add(textBlock);
                    positions.Add(tempX);
                }

                // Update tempX coordinate using original text width
                if (!isObfuscated) {
                    tempX += _MeasureTextWidth(part, fontFamily, fontSize, isBold, isItalic);
                }

                double textHeight = _MeasureTextHeight(part, fontFamily, fontSize, isBold, isItalic);
                lineHeight = textHeight > lineHeight ? textHeight : lineHeight;
                lineWidth = tempX; // Update line width
            }

            // Center-align: Adjust x-coordinates for each TextBlock
            double offsetX = (canvasWidth - lineWidth) / 2;
            for (int i = 0; i < textBlocks.Count; i++) {
                Canvas.SetLeft(textBlocks[i], positions[i] + offsetX);
            }

            if (lines.Length == 1) {
                double offsetY = (canvasHeight - lineHeight) / 2;
                for (int i = 0; i < textBlocks.Count; i++) {
                    Canvas.SetTop(textBlocks[i], offsetY);
                }
            } else if (lines.Length == 2 && lineIndex == 0) {
                double offsetY = (canvasHeight - lineHeight * 2) / 2;
                for (int i = 0; i < textBlocks.Count; i++) {
                    Canvas.SetTop(textBlocks[i], offsetY);
                }

                y = lineHeight + offsetY;
            }
        }
    }

    private TextBlock _RenderText(string text, FontFamily fontFamily, double fontSize, Brush color,
        bool isBold, bool isItalic, bool isUnderline, bool isStrikethrough,
        double x, double y, bool withClip = false, double clipWidth = 15) {
        var textBlock = new TextBlock {
            Text = text,
            FontFamily = fontFamily,
            FontSize = fontSize,
            Foreground = color,
            FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
            FontStyle = isItalic ? FontStyles.Italic : FontStyles.Normal
        };

        if (isUnderline || isStrikethrough) {
            textBlock.TextDecorations = new TextDecorationCollection();
            if (isUnderline) textBlock.TextDecorations.Add(TextDecorations.Underline);
            if (isStrikethrough) textBlock.TextDecorations.Add(TextDecorations.Strikethrough);
        }

        if (withClip) {
            var clipRect = new RectangleGeometry {
                Rect = new Rect(0, 0, clipWidth, _MeasureTextHeight(text, fontFamily, fontSize, isBold, isItalic))
            };
            textBlock.Clip = clipRect;
        }

        Canvas.SetLeft(textBlock, x);
        Canvas.SetTop(textBlock, y);
        if (this.Content is Canvas canvas) {
            canvas.Children.Add(textBlock);
        }

        return textBlock;
    }

    private FormattedText _CreateFormattedText(string text, FontFamily fontFamily, double fontSize, bool isBold, bool isItalic)
    {
        return new FormattedText(
            text,
            System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(fontFamily, isItalic ? FontStyles.Italic : FontStyles.Normal,
                isBold ? FontWeights.Bold : FontWeights.Normal, FontStretches.Normal),
            fontSize,
            Brushes.White,
            96);
    }

    private double _MeasureTextWidth(string text, FontFamily fontFamily, double fontSize, bool isBold, bool isItalic) {
        return _CreateFormattedText(text, fontFamily, fontSize, isBold, isItalic).WidthIncludingTrailingWhitespace;
    }

    private double _MeasureTextHeight(string text, FontFamily fontFamily, double fontSize, bool isBold, bool isItalic) {
        return _CreateFormattedText(text, fontFamily, fontSize, isBold, isItalic).Height;
    }

    public void RenderCanvas() {
        // Ensure Canvas is rendered
        MotdCanvas.UpdateLayout();

        // Generate static random characters for §k text
        foreach (var item in _obfuscatedTextBlocks) {
            var textBlock = item.TextBlock;
            var originalText = item.OriginalText;
            textBlock.Text = string.Join("",
                Enumerable.Range(0, originalText.Length).Select(_ => _randomChars[_random.Next(_randomChars.Length)]));
        }

        // Capture Canvas using RenderTargetBitmap
        var rtb = new RenderTargetBitmap(
            (int)MotdCanvas.Width, (int)MotdCanvas.Height, 96, 96, PixelFormats.Pbgra32);
        rtb.Render(MotdCanvas);
    }
    
    public void ClearCanvas() {
        MotdCanvas.Children.Clear();
        _obfuscatedTextBlocks.Clear();
    }
}