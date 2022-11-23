
using Microsoft.Bot.Schema;
using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using Svg;
using System.Collections;
using System.Drawing.Imaging;
using System.Drawing;
using LaTeXForTeamsApp.Exceptions;

namespace LaTeXForTeamsApp
{
    public class LatexRenderer
    {
        string WorkDir { get; set; }

        public LatexRenderer(string WorkDir) 
        {
            this.WorkDir = WorkDir;
        }

        public async Task<SvgDocument> LatexToSvg(string latex) 
        {
            // Make work directory
            string id = MakeId();

            Directory.CreateDirectory(string.Format("{0}/{1}", WorkDir, id));

            // Make tex file
            using FileStream tex = File.Create(string.Format("{0}/{1}/eqn.tex", WorkDir, id));
            
            byte[] bytes = Encoding.UTF8.GetBytes(WrapLatex(latex));

            await tex.WriteAsync(bytes);
            tex.Close();
            tex.Dispose();


            // Compile latex
            // Set working directory
            ProcessStartInfo latexStartInfo = new()
            {
                WorkingDirectory = string.Format("{0}/{1}", WorkDir, id),
                FileName = "/usr/bin/timeout",
                Arguments = "5 latex -no-shell-escape -interaction=nonstopmode -halt-on-error eqn.tex"
            };
            
            try
            {
                Process latexProcess = Process.Start(latexStartInfo);
                await latexProcess.WaitForExitAsync();
            }
            catch 
            {
                // Cleanup
                Process delp = Process.Start("/bin/rm", string.Format("-rf {0}/{1}", WorkDir, id));
                await delp.WaitForExitAsync();

                throw new LatexException();
            }
            

            // Convert to svg
            ProcessStartInfo dvisvgmStartInfo = new()
            {
                WorkingDirectory = string.Format("{0}/{1}", WorkDir, id),
                FileName = "/usr/bin/timeout",
                Arguments = "5 dvisvgm --no-font --exact eqn.dvi"
            };
            
            try
            {
                Process dvisvgmProcess = Process.Start(dvisvgmStartInfo);
                await dvisvgmProcess.WaitForExitAsync();
            }
            catch
            {
                // Cleanup
                Process delp = Process.Start("/bin/rm", string.Format("-rf {0}/{1}", WorkDir, id));
                await delp.WaitForExitAsync();

                throw new LatexException();
            }

            SvgDocument document = null;

            try
            {
                document = SvgDocument.Open(string.Format("{0}/{1}/eqn.svg", WorkDir, id));
            }
            catch
            {
                // Cleanup
                Process delp = Process.Start("/bin/rm", string.Format("-rf {0}/{1}", WorkDir, id));
                await delp.WaitForExitAsync();

                throw new LatexException();
            }

            // Cleanup
            Process delProcess = Process.Start("/bin/rm", string.Format("-rf {0}/{1}", WorkDir, id));
            await delProcess.WaitForExitAsync();
                        

            return document;

        }

        public async Task<(string, bool)> LatexToPngString(string latex) 
        {
            SvgDocument svg = await LatexToSvg(latex);

            svg.Fill = new SvgColourServer(Color.White);

            Bitmap bitmap = svg.Draw(1024, 0);
            if (bitmap.Height > 1024) bitmap = svg.Draw(0, 1024);

            MemoryStream ms = new();
            bitmap.Save(ms, ImageFormat.Png);
            byte[] byteImage = ms.ToArray();

            return (Convert.ToBase64String(byteImage), bitmap.Width < 500 && bitmap.Height > 700);
        }

        string MakeId()
        {
            string id = "";

            Random rnd = new();

            for (int i = 0; i < 10; i++) 
            {
                id += "0123456789abcdef"[rnd.Next(16)];
            }

            return id;
        }

        string WrapLatex(string latex) => string.Format(@"
\documentclass[12pt]{{article}}
\usepackage{{amsmath}}
\usepackage{{amssymb}}
\usepackage{{amsfonts}}
\usepackage{{xcolor}}
\usepackage{{siunitx}}
\usepackage[utf8]{{inputenc}}
\thispagestyle{{empty}}
\begin{{document}}
{0}
\end{{document}}
        ", Escape(latex));

        string Escape(string input)
        {
            string[] scary = {"input", "include", "newread", "file", "openin", "read", "write", "line", "closein", "text", "loop", "unless", "if", "repeat", "fileline", "else", "usepackage", "catcode", "immediate", "write18", "url", "href", "newwrite", "outfile", "openout", "closeout", "def"};

            foreach (string s in scary) 
            {
                input = input.Replace("\\" + s, s);
            }

            return input;
        }
    }
}
