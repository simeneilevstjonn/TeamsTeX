using CSharpMath.Atom.Atoms;
using CSharpMath.Rendering.BackEnd;
using Microsoft.Bot.Schema;
using Microsoft.VisualBasic;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using static System.Formats.Asn1.AsnWriter;
using Typography.OpenFont.Tables;
using System.Diagnostics;

namespace LaTeXForTeamsApp
{
    public class LatexRenderer
    {
        string WorkDir { get; set; }

        public LatexRenderer(string WorkDir) 
        {
            this.WorkDir = WorkDir;
        }

        public async Task<string> LatexToSvg(string latex) 
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
                FileName = "C:\\Windows\\System32\\wsl.exe",
                Arguments = "timeout 5 latex -no-shell-escape -interaction=nonstopmode -halt-on-error eqn.tex"
            };
            Process latexProcess = Process.Start(latexStartInfo);
            await latexProcess.WaitForExitAsync();

            // Convert to svg
            ProcessStartInfo dvisvgmStartInfo = new()
            {
                WorkingDirectory = string.Format("{0}/{1}", WorkDir, id),
                FileName = "C:\\Windows\\System32\\wsl.exe",
                Arguments = "timeout 5 dvisvgm --no-font --exact eqn.dvi"
            };
            Process dvisvgmProcess = Process.Start(dvisvgmStartInfo);
            await dvisvgmProcess.WaitForExitAsync();

            // Read svg
            string svg = File.ReadAllText(string.Format("{0}/{1}/eqn.svg", WorkDir, id));

            // Cleanup
           // Directory.Delete(string.Format("{0}/{1}", WorkDir, id), true);

            return svg.ReplaceLineEndings("");

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
        ", latex);
    }
}
