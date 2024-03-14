using System.Diagnostics;

internal class Program
{
    private static bool onlog;
    private const string logName = "Application";
    private const string sourceName = "AutoTmpDel";
    private readonly static Guid guid = Guid.NewGuid();
    private const string targetProcessName = "VB6";
    private const int timeoutSeconds = 60; 
    private static int elapsedTime = 1;

    private static void Main(string[] args)
    {
        onlog = args.Length > 0 && (args[0] == "onlog" ? true : throw new ArgumentException("O argumento passado para o programa é inválido"));
        Log($"{guid}A0 - O serviço foi iniciado! Aguardando o início do VB6.", EventLogEntryType.Information, 1);


        while (true)
        {
            Process[] processes = Process.GetProcessesByName(targetProcessName);
            if (processes.Length > 0)
            {
                Log($"{guid}B0 - O programa {targetProcessName} foi iniciado! Aguardando fechamento.", EventLogEntryType.Information, 1);

                while (true)
                {
                    Process[] processes2 = Process.GetProcessesByName(targetProcessName);

                    if (!(processes2.Length > 0))
                    {
                        Log($"{guid}C0 - O programa {targetProcessName} foi fechado! Iniciando processo de exclusão!", EventLogEntryType.Information, 1);
                        break;
                    }

                    Thread.Sleep(1000);
                }
                break;
            }
            else
            {
                Thread.Sleep(1000);
                elapsedTime += 1;

                if (elapsedTime > timeoutSeconds)
                {
                    Log($"{guid}B1 - O programa {targetProcessName} não foi iniciado! O tempo limite de {timeoutSeconds} segundos foi atingido. O processo  de exclusão não foi iniciado.", EventLogEntryType.Error, 2);
                    Environment.Exit(0);
                }
            }
        }

        try
        {
            //Obtém todos os arquivos temp vazios da raiz C:/
            string[] tmpFiles = Directory.GetFiles(@"C:\", "*.tmp")
            .Where(file => new FileInfo(file).Length == 0)
            .ToArray();

            Log($"{guid}D0 - Arquivos encontrados: {tmpFiles.Length}.", EventLogEntryType.Information, 1);

            //Deleta e registra
            var successCount = 0;
            foreach (var filePath in tmpFiles)
            {
                try
                {
                    File.Delete(filePath);
                    successCount += 1;
                }
                catch (Exception e)
                {
                    Log($"{guid}E{successCount} - Erro ao deletar {filePath}: {e.Message}.", EventLogEntryType.Error, 2);
                }
            };

            //Resultado do processamento
            if (tmpFiles.Length == successCount)
            {
                Log($"{guid}F0 - Arquivos excluídos com sucesso!", EventLogEntryType.SuccessAudit, 1);
            }
            else
            {
                Log($"{guid}F1 - Alguns ou todos os arquivos obtidos não foram excluídos com sucesso! Total de arquivos excluídos: {successCount}.", EventLogEntryType.Error, 2);
            }
        }
        catch (Exception e)
        {
            Log($"{guid}G0 - Erro durante a execução. O programa será interrompido: {e.Message}.", EventLogEntryType.Error, 2);
            Environment.Exit(0);
        }

        Log($"{guid}H0 - Fim do processo de exclusão!", EventLogEntryType.Information, 1);
    }

    private static void Log(string message, EventLogEntryType logType, int logId)
    {
        if (onlog)
        {
            // Cria o log caso não exista
            if (!EventLog.SourceExists(sourceName))
            {
                EventLog.CreateEventSource(sourceName, logName);
            }

            //Cria a instância do log
            EventLog eventLog = new(logName)
            {
                Source = sourceName,
            };

            eventLog.WriteEntry(message, logType, logId);
        }
    }
}