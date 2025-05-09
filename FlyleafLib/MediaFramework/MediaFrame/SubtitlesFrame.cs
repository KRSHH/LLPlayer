﻿using System.Collections.Generic;
using System.Drawing;
using System.Globalization;

namespace FlyleafLib.MediaFramework.MediaFrame;

public class SubtitlesFrame : FrameBase
{
    public bool isBitmap;
    public uint duration;
    public string text;
    public List<SubStyle> subStyles;
    public AVSubtitle sub;
    public SubtitlesFrameBitmap bitmap;

    // for translation switch
    public bool isTranslated;
}

public class SubtitlesFrameBitmap
{
    // Buffer to hold decoded pixels
    public byte[] data;
    public int width;
    public int height;
    public int x;
    public int y;
}

public struct SubStyle
{
    public SubStyles style;
    public Color value;

    public int from;
    public int len;

    public SubStyle(int from, int len, Color value) : this(SubStyles.COLOR, from, len, value) { }
    public SubStyle(SubStyles style, int from = -1, int len = -1, Color? value = null)
    {
        this.style  = style;
        this.value  = value == null ? Color.White : (Color)value;
        this.from   = from;
        this.len    = len;
    }
}

public enum SubStyles
{
    BOLD,
    ITALIC,
    UNDERLINE,
    STRIKEOUT,
    FONTSIZE,
    FONTNAME,
    COLOR
}

/// <summary>
/// Default Subtitles Parser
/// </summary>
public static class ParseSubtitles
{
    public static void Parse(SubtitlesFrame sFrame)
    {
        sFrame.text = SSAtoSubStyles(sFrame.text, out var subStyles);
        sFrame.subStyles = subStyles;
    }
    public static string SSAtoSubStyles(string s, out List<SubStyle> styles)
    {
        int pos = 0;
        string sout = "";
        styles = new List<SubStyle>();

        SubStyle bold       = new(SubStyles.BOLD);
        SubStyle italic     = new(SubStyles.ITALIC);
        SubStyle underline  = new(SubStyles.UNDERLINE);
        SubStyle strikeout  = new(SubStyles.STRIKEOUT);
        SubStyle color      = new(SubStyles.COLOR);

        //SubStyle fontname      = new SubStyle(SubStyles.FONTNAME);
        //SubStyle fontsize      = new SubStyle(SubStyles.FONTSIZE);

        if (string.IsNullOrEmpty(s))
        {
            return sout;
        }

        s = s.LastIndexOf(",,") == -1 ? s : s[(s.LastIndexOf(",,") + 2)..].Replace("\\N", "\n").Trim();

        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == '{') continue;

            if (s[i] == '\\' && s[i - 1] == '{')
            {
                int codeLen = s.IndexOf('}', i) - i;
                if (codeLen == -1) continue;

                string code = s.Substring(i, codeLen).Trim();

                switch (code[1])
                {
                    case 'c':
                        if (code.Length == 2)
                        {
                            if (color.from == -1) break;

                            color.len = pos - color.from;
                            if (color.value != Color.Transparent)
                                styles.Add(color);

                            color = new SubStyle(SubStyles.COLOR);
                        }
                        else
                        {
                            color.from = pos;
                            color.value = Color.Transparent;
                            if (code.Length < 7) break;

                            int colorEnd = code.LastIndexOf('&');
                            if (colorEnd < 6) break;

                            string hexColor = code[4..colorEnd];

                            int red = int.Parse(hexColor.Substring(hexColor.Length - 2, 2), NumberStyles.HexNumber);
                            int green = 0;
                            int blue = 0;

                            if (hexColor.Length - 2 > 0)
                            {
                                hexColor = hexColor[..^2];
                                if (hexColor.Length == 1)
                                    hexColor = "0" + hexColor;

                                green = int.Parse(hexColor.Substring(hexColor.Length - 2, 2), NumberStyles.HexNumber);
                            }

                            if (hexColor.Length - 2 > 0)
                            {
                                hexColor = hexColor[..^2];
                                if (hexColor.Length == 1)
                                    hexColor = "0" + hexColor;

                                blue = int.Parse(hexColor.Substring(hexColor.Length - 2, 2), NumberStyles.HexNumber);
                            }

                            color.value = Color.FromArgb(255, red, green, blue);
                        }
                        break;

                    case 'b':
                        if (code[2] == '0')
                        {
                            if (bold.from == -1) break;

                            bold.len = pos - bold.from;
                            styles.Add(bold);
                            bold = new SubStyle(SubStyles.BOLD);
                        }
                        else
                        {
                            bold.from = pos;
                            //bold.value = code.Substring(2, code.Length-2);
                        }

                        break;

                    case 'u':
                        if (code[2] == '0')
                        {
                            if (underline.from == -1) break;

                            underline.len = pos - underline.from;
                            styles.Add(underline);
                            underline = new SubStyle(SubStyles.UNDERLINE);
                        }
                        else
                        {
                            underline.from = pos;
                        }

                        break;

                    case 's':
                        if (code[2] == '0')
                        {
                            if (strikeout.from == -1) break;

                            strikeout.len = pos - strikeout.from;
                            styles.Add(strikeout);
                            strikeout = new SubStyle(SubStyles.STRIKEOUT);
                        }
                        else
                        {
                            strikeout.from = pos;
                        }

                        break;

                    case 'i':
                        if (code.Length > 2 && code[2] == '0')
                        {
                            if (italic.from == -1) break;

                            italic.len = pos - italic.from;
                            styles.Add(italic);
                            italic = new SubStyle(SubStyles.ITALIC);
                        }
                        else
                        {
                            italic.from = pos;
                        }

                        break;
                }

                i += codeLen;
                continue;
            }

            sout += s[i];
            pos++;
        }

        // Non-Closing Codes
        int soutPostLast = sout.Length;
        if (bold.from != -1) { bold.len = soutPostLast - bold.from; styles.Add(bold); }
        if (italic.from != -1) { italic.len = soutPostLast - italic.from; styles.Add(italic); }
        if (strikeout.from != -1) { strikeout.len = soutPostLast - strikeout.from; styles.Add(strikeout); }
        if (underline.from != -1) { underline.len = soutPostLast - underline.from; styles.Add(underline); }
        if (color.from != -1 && color.value != Color.Transparent) { color.len = soutPostLast - color.from; styles.Add(color); }

        // Greek issue?
        sout = sout.Replace("ʼ", "Ά");

        return sout;
    }
}
