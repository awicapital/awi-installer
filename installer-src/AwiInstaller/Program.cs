using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

class Program
{
    // Caminho onde vamos salvar o arquivo com a pasta do MT5
    static string LocalConfigPath = @"D:\awi-installer\config\mt5-path.txt";

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();

        Console.WriteLine("=======================================");
        Console.WriteLine("   AWI CAPITAL - INSTALADOR OFICIAL    ");
        Console.WriteLine("=======================================");

        // 1) Detectar pasta MQL5 automaticamente
        string mt5Path = DetectMT5Directory();

        if (string.IsNullOrEmpty(mt5Path))
        {
            Console.WriteLine("MT5 não encontrado automaticamente.");
            Console.WriteLine("Selecione manualmente a pasta *MQL5*.");

            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Selecione a pasta MQL5 do seu MetaTrader 5";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    mt5Path = dialog.SelectedPath;
                }
                else
                {
                    Console.WriteLine("Instalação cancelada.");
                    return;
                }
            }
        }

        Console.WriteLine($"Pasta MQL5 definida: {mt5Path}");
        Directory.CreateDirectory(Path.GetDirectoryName(LocalConfigPath)!);
        File.WriteAllText(LocalConfigPath, mt5Path);

        // 2) Copiar payload-template → MT5
        Console.WriteLine("Copiando arquivos AWI Capital para o MT5...");

        string payloadRoot = @"D:\awi-installer\payload-template\MQL5";
        CopyDirectory(payloadRoot, mt5Path);

        Console.WriteLine("Copiado com sucesso.");

        // 3) Rodar Updater para manter atualizações futuras
        RunUpdater();

        Console.WriteLine("Instalação finalizada!");
    }

    static string DetectMT5Directory()
    {
        string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string terminalPath = Path.Combine(roaming, "MetaQuotes", "Terminal");

        if (!Directory.Exists(terminalPath))
            return null;

        foreach (var folder in Directory.GetDirectories(terminalPath))
        {
            string mql5 = Path.Combine(folder, "MQL5");

            if (Directory.Exists(mql5))
                return mql5;
        }

        return null;
    }

    static void CopyDirectory(string source, string target)
    {
        foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            string newDir = dir.Replace(source, target);
            Directory.CreateDirectory(newDir);
        }

        foreach (string file in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
        {
            string newFile = file.Replace(source, target);
            Directory.CreateDirectory(Path.GetDirectoryName(newFile)!);
            File.Copy(file, newFile, true);
        }
    }

    static void RunUpdater()
    {
        string updaterPath = @"D:\awi-installer\updater-src\AwiUpdater\bin\Release\net8.0\AwiUpdater.exe";

        if (File.Exists(updaterPath))
        {
            Console.WriteLine("Executando updater...");
            Process.Start(updaterPath);
        }
        else
        {
            Console.WriteLine("Updater não encontrado. Instalação segue sem atualização.");
        }
    }
}
