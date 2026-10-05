using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;

class ReleaseBuilder
{
    static string Root = AppDomain.CurrentDomain.BaseDirectory;
    static string Build = Path.Combine(Root, "_build");
    static string Stage = Path.Combine(Build, "payload");
    static string Dist = Path.Combine(Root, "dist");

    static void Main()
    {
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("PDF Compressor 1.0 — Integrated Setup Builder");
            Console.WriteLine("================================================");
            Console.WriteLine();

            string csc = FindCsc();
            if (csc == null)
                throw new Exception("Windows .NET Framework C# compiler csc.exe was not found.");

            string gsRoot = FindGhostscriptRoot();
            if (gsRoot == null)
                throw new Exception(
                    "Ghostscript runtime was not found.\r\n" +
                    "Set GHOSTSCRIPT_ROOT or install Ghostscript in a common Program Files location.");

            Console.WriteLine("Ghostscript runtime:");
            Console.WriteLine("  " + gsRoot);
            Console.WriteLine();

            ResetBuild();

            Console.WriteLine("[1/6] Compiling PDF Compressor...");
            Run(csc,
                "/nologo /target:winexe /optimize+ " +
                "/r:System.Windows.Forms.dll /r:System.Drawing.dll " +
                "/out:\"" + Path.Combine(Stage, "PDF_Compressor.exe") + "\" " +
                "\"" + Path.Combine(Root, "src", "PDF_Compressor.cs") + "\" " +
                "\"" + Path.Combine(Root, "src", "AssemblyInfo.cs") + "\"");

            Console.WriteLine("[2/6] Compiling uninstaller...");
            Run(csc,
                "/nologo /target:winexe /optimize+ " +
                "/win32manifest:\"" + Path.Combine(Root, "installer", "uninstall.manifest") + "\" " +
                "/r:System.Windows.Forms.dll " +
                "/out:\"" + Path.Combine(Stage, "Uninstall.exe") + "\" " +
                "\"" + Path.Combine(Root, "installer", "Uninstall.cs") + "\" " +
                "\"" + Path.Combine(Root, "installer", "UninstallAssemblyInfo.cs") + "\"");

            Console.WriteLine("[3/6] Copying Ghostscript runtime...");
            CopyDirectory(gsRoot, Path.Combine(Stage, "Ghostscript"));

            Console.WriteLine("[4/6] Adding source/license notices...");
            CopyFile(Path.Combine(Root, "LICENSE"), Path.Combine(Stage, "LICENSE"));
            CopyFile(Path.Combine(Root, "THIRD_PARTY_NOTICES.md"), Path.Combine(Stage, "THIRD_PARTY_NOTICES.md"));
            CopyFile(Path.Combine(Root, "README.md"), Path.Combine(Stage, "README.md"));
            CopyFile(Path.Combine(Root, "src", "PDF_Compressor.cs"),
                     Path.Combine(Stage, "PDF_Compressor_SOURCE.cs"));

            File.WriteAllText(Path.Combine(Stage, "GHOSTSCRIPT_SOURCE_INFO.txt"),
                "Bundled Ghostscript runtime source location:\r\n" +
                "https://github.com/ArtifexSoftware/ghostpdl-downloads/releases/tag/gs10080\r\n\r\n" +
                "Pinned source archive: ghostscript-10.08.0.tar.xz\r\n" +
                "SHA-256: c20492bc8ebb96c87fa2e52a0926e1cda8cde95d66145e018ac713fed5da38cf\r\n",
                System.Text.Encoding.UTF8);

            Console.WriteLine("[5/6] Creating payload...");
            string payloadZip = Path.Combine(Build, "Payload.zip");
            if (File.Exists(payloadZip)) File.Delete(payloadZip);
            ZipFile.CreateFromDirectory(Stage, payloadZip, CompressionLevel.Optimal, false);

            Console.WriteLine("[6/6] Compiling Setup...");
            string setupOut = Path.Combine(Dist, "PDF_Compressor_Setup_1.0.0.exe");

            Run(csc,
                "/nologo /target:winexe /optimize+ " +
                "/win32manifest:\"" + Path.Combine(Root, "installer", "setup.manifest") + "\" " +
                "/r:System.Windows.Forms.dll /r:System.Drawing.dll " +
                "/r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll " +
                "/resource:\"" + payloadZip + "\",Payload.zip " +
                "/out:\"" + setupOut + "\" " +
                "\"" + Path.Combine(Root, "installer", "SetupInstaller.cs") + "\" " +
                "\"" + Path.Combine(Root, "installer", "SetupAssemblyInfo.cs") + "\"");

            Console.WriteLine();
            Console.WriteLine("Build successful:");
            Console.WriteLine("  " + setupOut);
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("[ERROR]");
            Console.WriteLine(ex.Message);
            Environment.ExitCode = 1;
        }
    }

    static string FindCsc()
    {
        string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string[] paths = new string[]
        {
            Path.Combine(windir, @"Microsoft.NET\Framework64\v4.0.30319\csc.exe"),
            Path.Combine(windir, @"Microsoft.NET\Framework\v4.0.30319\csc.exe")
        };
        return paths.FirstOrDefault(File.Exists);
    }

    static string FindGhostscriptRoot()
    {
        string env = Environment.GetEnvironmentVariable("GHOSTSCRIPT_ROOT");
        if (!String.IsNullOrWhiteSpace(env))
        {
            string normalized = Path.GetFullPath(env);
            if (File.Exists(Path.Combine(normalized, "bin", "gswin64c.exe")))
                return normalized;
        }

        string[] roots = new string[]
        {
            @"D:\Program Files\gs",
            @"C:\Program Files\gs",
            @"C:\Program Files (x86)\gs"
        };

        List<string> found = new List<string>();
        foreach (string root in roots)
        {
            try
            {
                if (!Directory.Exists(root)) continue;
                foreach (string exe in Directory.GetFiles(root, "gswin64c.exe", SearchOption.AllDirectories))
                {
                    DirectoryInfo bin = Directory.GetParent(exe);
                    if (bin != null && bin.Parent != null)
                        found.Add(bin.Parent.FullName);
                }
            }
            catch { }
        }

        return found.OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
    }

    static void ResetBuild()
    {
        if (Directory.Exists(Build)) Directory.Delete(Build, true);
        if (Directory.Exists(Dist)) Directory.Delete(Dist, true);
        Directory.CreateDirectory(Stage);
        Directory.CreateDirectory(Dist);
    }

    static void Run(string exe, string args)
    {
        ProcessStartInfo psi = new ProcessStartInfo(exe, args);
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = true;
        psi.RedirectStandardError = true;

        using (Process p = Process.Start(psi))
        {
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();

            if (!String.IsNullOrWhiteSpace(stdout)) Console.WriteLine(stdout);
            if (p.ExitCode != 0)
                throw new Exception(stderr + "\r\n" + stdout);
        }
    }

    static void CopyFile(string source, string dest)
    {
        string parent = Path.GetDirectoryName(dest);
        if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);
        File.Copy(source, dest, true);
    }

    static void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);

        foreach (string dir in Directory.GetDirectories(sourceDir))
            CopyDirectory(dir, Path.Combine(destDir, Path.GetFileName(dir)));
    }
}
