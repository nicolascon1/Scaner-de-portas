using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        public static List<UsedPort> Portas = new List<UsedPort>();
        public static string diretorio = "";
        public static Dictionary<string, string> Informacoes;

        static void Main(string[] args)
        {
            BuscarDiretorio();

            BuscarInformacoes();

            string nome = Dns.GetHostName();
            IPAddress ip = Dns.GetHostAddresses(nome)[2];

            Portas = PegarPortas().ToList();

            StartScan(ip);

            ImprimirESalvarPortas();
            Console.ReadLine();
        }

        private static void BuscarInformacoes()
        {
            string Auxinformacoes = "";
            string[] informacoesEmArray = null;
            if (System.IO.File.Exists(diretorio))
            {
                Auxinformacoes = System.IO.File.ReadAllText(diretorio);
                Auxinformacoes = Auxinformacoes.Replace("\r\n", "|");
                informacoesEmArray = Auxinformacoes.Split('|');
            }

            Informacoes = new Dictionary<string, string>();

            for (int i = 0; i < informacoesEmArray.Length; i++)
            {
                int aux = i + 1;
                if (!Informacoes.ContainsKey(informacoesEmArray[i]))
                {
                    Informacoes.Add(informacoesEmArray[i], informacoesEmArray[aux]);
                }
                i = aux;
            }
        }

        private static void BuscarDiretorio()
        {
            diretorio = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory.ToString());
            diretorio = $@"{diretorio}\ArquivoDescricoes\Portas2.txt";
        }

        private static void ImprimirESalvarPortas()
        {
            Console.Clear();
            foreach (Porta porta in portasAbertas)
            {
                Console.WriteLine($"Porta: {porta.IdPorta}. Descrição breve: {porta.Descricao}.");
            }
        }

        public static List<Porta> portasAbertas = new List<Porta>();

        public static void StartScan(IPAddress ipAddress)
        {
            for (int i = 1; i <= 1500; i++)
            {
                Console.WriteLine($"Scaneando Porta: {i}");

                ScanearPorta(ipAddress, i, ProtocolType.Tcp);

                ScanearPorta(ipAddress, i, ProtocolType.Udp);
            }
        }

        public static void ScanearPorta(IPAddress ipAddress, int Porta, ProtocolType tipoDePorta)
        {
            try
            {
                Socket s = new Socket(AddressFamily.InterNetwork,
                                          SocketType.Stream, tipoDePorta);

                s.BeginConnect(new IPEndPoint(ipAddress, Porta), EndConnect, s);
            }
            catch { }
        }

        public static void EndConnect(IAsyncResult ar)
        {
            try
            {
                Socket s = ar.AsyncState as Socket;

                s.EndConnect(ar);

                if (s.Connected)
                {
                    int openPort = Convert.ToInt32(s.RemoteEndPoint.ToString().Split(':')[1]);

                    string descricaoPorta = "";

                    try
                    {
                        descricaoPorta = Portas.FirstOrDefault(p => p.Port == openPort).Process;

                        if (descricaoPorta.Contains("Não é possível obter informações de propriedade"))
                        {
                            descricaoPorta = null;
                        }
                    }
                    catch { }

                    if (string.IsNullOrEmpty(descricaoPorta))
                    {
                        descricaoPorta = "Porta aberta e não utilizada. Recomendasse fechar portas abertas e que não estão sendo utilizadas";
                    }

                    if (Informacoes.ContainsKey(openPort.ToString()))
                    {
                        descricaoPorta += $" .{Informacoes[openPort.ToString()]}";
                    }

                    portasAbertas.Add(new Porta(openPort, descricaoPorta));

                    Console.WriteLine("TCP conectado na porta: { 0}", openPort);

                    s.Disconnect(true);
                }

            }
            catch { }
        }

        public class UsedPort
        {
            public int Port { get; set; }
            public string Process { get; set; }
        }

        public static IEnumerable<UsedPort> PegarPortas()
        {
            var process = Process.Start(new ProcessStartInfo("netstat", "-anb")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false
            });
            string line = null;
            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                if (line.IndexOf("TCP") >= 0 || line.IndexOf("UDP") >= 0)
                {
                    var row = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    var localAddress = row[1];
                    var processName = process.StandardOutput.ReadLine();
                    if (!processName.Contains("Can not obtain ownership information"))
                    {
                        yield return new UsedPort
                        {
                            Port = int.Parse(localAddress.Split(':').Last()),
                            Process = processName
                        };
                    }
                }
            }
        }
    }

    internal class Porta
    {
        public long IdPorta { get; set; }
        public string Descricao { get; set; }

        public Porta(long IdPorta, string Descricao)
        {
            this.IdPorta = IdPorta;
            this.Descricao = Descricao;
        }
    }
}
