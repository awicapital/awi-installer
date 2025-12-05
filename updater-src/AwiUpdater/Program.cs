using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    // Arquivo do instalador que contém o caminho real do MT5
    static string Mt5PathConfig = @"D:\awi-installer\config\mt5-path.txt";

    // URL pública do version.json (vamos trocar depois)
    static string VersionJsonUrl = "https://raw.githubusercontent.com/awicapital/awi-installer/main/version.json";

    static async Task<int> Main(string[] args)
    {
        try
        {
            Console.WriteLine("=======================================");
            Console.WriteLine("         AWI CAPITAL - UPDATER");
            Console.WriteLine("=======================================");

            if (!File.Exists(Mt5PathConfig))
            {
                Console.WriteLine("[ERRO] mt5-path.txt não encontrado.");
                return 1;
            }

            string mt5Path = File.ReadAllText(Mt5PathConfig).Trim();
            Console.WriteLine("MT5 Path: " + mt5Path);

            // Pasta AWI Capital dentro do MT5
            string awiFolder = Path.Combine(mt5Path, "AWI Capital");

            // 1) Ler versão remota
            Console.WriteLine("Baixando version.json...");
            using var http = new HttpClient();
            string json = await http.GetStringAsync(VersionJsonUrl);

            var remoteInfo = JsonSerializer.Deserialize<RemoteVersion>(json);
            if (remoteInfo == null)
            {
                Console.WriteLine("[ERRO] version.json inválido.");
                return 1;
            }

            Console.WriteLine("Versão remota: " + remoteInfo.version);

            // 2) Ler versão local
            string localVersionFile = Path.Combine(awiFolder, "version.txt");
            string localVersion = File.Exists(localVersionFile)
                ? File.ReadAllText(localVersionFile).Trim()
                : "0.0.0";

            Console.WriteLine("Versão local: " + localVersion);

            if (localVersion == remoteInfo.version)
            {
                Console.WriteLine("Já está na última versão. Nada a fazer.");
                return 0;
            }

            Console.WriteLine("Nova versão disponível! Baixando...");

            // 3) Baixar .zip da nova versão
            string tempDir = Path.Combine(Path.GetTempPath(), "AWI_UPDATE");
            string zipPath = Path.Combine(tempDir, "update.zip");
            string extractDir = Path.Combine(tempDir, "extract");

            Directory.CreateDirectory(tempDir);
            if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);

            using var stream = await http.GetStreamAsync(remoteInfo.downloadUrl);
            using var fs = new FileStream(zipPath, FileMode.Create);
            await stream.CopyToAsync(fs);

            Console.WriteLine("Download completo. Extraindo...");

            ZipFile.ExtractToDirectory(zipPath, extractDir);

            Console.WriteLine("Copiando atualização para o MT5...");

            // 4) Copiar somente a pasta AWI Capital
            string sourceAwi = Path.Combine(extractDir, "MQL5", "AWI Capital");
            if (!Directory.Exists(sourceAwi))
            {
                Console.WriteLine("[ERRO] Pasta AWI Capital não encontrada no pacote.");
                return 1;
            }

            CopyDirectory(sourceAwi, awiFolder);

            // 5) Salvar nova versão
            File.WriteAllText(localVersionFile, remoteInfo.version);

            Console.WriteLine("Atualização concluída com sucesso!");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ERRO] " + ex.Message);
            return 1;
        }
    }

    static void CopyDirectory(string source, string target)
    {
        foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(source, target));
        }

        foreach (string file in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
        {
            string newPath = file.Replace(source, target);
            Directory.CreateDirectory(Path.GetDirectoryName(newPath)!);
            File.Copy(file, newPath, true);
        }
    }

    class RemoteVersion
    {
        public string version { get; set; }
        public string downloadUrl { get; set; }
    }
}
