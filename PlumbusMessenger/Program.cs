using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace PlumbusMessenger
{
    class Program
    {
        static void Main(string[] args)
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("192.168.1.249"), 1024);
            //TcpListener listener = new TcpListener(IPAddress.Parse("159.224.233.249"), 1024);
            listener.Start(10);
            listener.BeginAcceptTcpClient(MyCallback, listener);
            Console.WriteLine("Server started");
            


            Console.ReadKey();
        }

        private static void MyCallback(IAsyncResult ar)
        {
            string clientAdrr = "";
            try
            {
                TcpListener listener = ar.AsyncState as TcpListener;
                TcpClient client = listener.EndAcceptTcpClient(ar);
                listener.BeginAcceptTcpClient(MyCallback, listener);

                StreamReader reader = new StreamReader(client.GetStream(), Encoding.GetEncoding(866));
                StreamWriter writer = new StreamWriter(client.GetStream(), Encoding.GetEncoding(866));
                IPAddress remoteAddr = (client.Client.RemoteEndPoint as IPEndPoint).Address;
                clientAdrr = remoteAddr.ToString();

                Console.WriteLine($"User connect from IP: {remoteAddr}");

                writer.WriteLine($"Welcome to Plumbus! =^_^=");
                bool isLogin = false;
                string currName = null;
                var controller = new PlumbusDbController();
                while (true)
                {
                    if(isLogin)
                        writer.Write($"<{currName}> Enter command => ");
                    else
                        writer.Write("Enter command => ");
                    writer.Flush();
                    string command = reader.ReadLine().Trim().ToUpper();
                    switch (command)
                    {
                        case "HELP":
                            writer.WriteLine("CHECK IN - регистрация нового пользователя(Имя, Логин, Пароль)");
                            writer.WriteLine("LOGIN - вход под своим логином и паролем");
                            writer.WriteLine("LOG OUT - выход с текущего пользователя");
                            writer.WriteLine("LIST USERS - получить список Имен зарегистрированых пользователей");
                            writer.WriteLine("SEND MESSAGE - отправить сообщение(указать само сообщение и Имя получателя)");
                            writer.WriteLine("MESSAGES - перейти в меню просмотра сообщений");
                            writer.WriteLine("QUIT - выход");
                            break;

                        case "CHECK IN":
                            if(isLogin) break;
                            writer.Write("Enter your name =>");
                            writer.Flush();
                            string regName = reader.ReadLine();
                            writer.Write("Enter login =>");
                            writer.Flush();
                            string regLogin = reader.ReadLine();
                            writer.Write("Enter password =>");
                            writer.Flush();
                            string regPwd = reader.ReadLine();
                            
                            var list = controller.GetUsers();
                            if (list.Count(x => x.Name == regName) > 0)
                            {
                                writer.WriteLine("This name is already taken!");
                            }
                            else if (list.Count(x => x.Login == regLogin) > 0)
                            {
                                writer.WriteLine("This login is already taken!");
                            }
                            else
                            {
                                controller.AddUser(regName, regLogin, regPwd);
                                writer.WriteLine("User added! Now you can Login");
                            }
                            writer.Flush();
                            break;

                        case "LIST USERS":

                            writer.WriteLine("Users:\n");
                            foreach (var user in controller.GetUsers())
                            {
                                writer.WriteLine(user.Name);
                            }

                            writer.WriteLine();
                            writer.Flush();
                            break;

                        case "SEND MESSAGE":
                            if(!isLogin)break;
                            writer.Write("Enter name Recipient =>");
                            writer.Flush();
                            string nameRcpt = reader.ReadLine();
                            if (!controller.GetUsers().Exists(x => x.Name == nameRcpt))
                            {
                                writer.Write("Incorrect name Recipient");
                                break;
                            }
                            writer.Write("Enter message =>");
                            writer.Flush();
                            string messg = reader.ReadLine();
                            writer.Flush();

                            var dateIn = DateTime.Now.ToString("d");
                            controller.AddMessage(messg,dateIn,
                                controller.GetUsers().First(x => x.Name==currName).Id,
                                controller.GetUsers().First(x=>x.Name==nameRcpt).Id);
                            writer.WriteLine("Message was sent!");
                            break;

                        case "MESSAGES":
                            if(!isLogin) break;
                            var messages = controller.GetMessages();
                            var users = controller.GetUsers();
                            writer.WriteLine();
                            while (true)
                            {
                                writer.WriteLine("Commands: UNREAD - Show new messages; ALL - show all messages; BACK - to main menu");
                                writer.Write("Enter command => ");
                                writer.Flush();
                                command = reader.ReadLine().Trim().ToUpper();
                                switch (command)
                                {
                                    case "UNREAD":
                                        foreach (var mess in messages)
                                        {
                                            if (mess.RcptId == users.Find(x => x.Name == currName).Id &&
                                                mess.DateOut == "--:--:--")
                                            {
                                                writer.WriteLine(
                                                    $"From <{users.Find(x => x.Id == mess.SenderId).Name}> at {mess.DateIn}: {mess.Messg}");
                                                controller.UpdateMessage(mess.Id, DateTime.Now.ToString("d"));
                                            }
                                        }
                                        writer.WriteLine();
                                        writer.Flush();
                                        break;
                                    case "ALL":
                                        foreach (var mess in messages)
                                        {
                                            if (mess.RcptId == users.Find(x => x.Name == currName).Id )
                                            {
                                                writer.WriteLine(
                                                    $"From <{users.Find(x => x.Id == mess.SenderId).Name}> at {mess.DateIn}: {mess.Messg}");
                                                writer.WriteLine();
                                            }
                                        }
                                        writer.WriteLine();
                                        writer.Flush();
                                        break;
                                    case "BACK":
                                        goto MESS;
                                }
                            }

                            MESS:;
                            break;

                        case "LOGIN":
                            writer.Write("Enter login =>");
                            writer.Flush();
                            string login = reader.ReadLine();
                            writer.Write("Enter password =>");
                            writer.Flush();
                            string passwd = reader.ReadLine();
                            if (isLogin = CheckUser(login, passwd, out string name))
                            {
                                writer.WriteLine();
                                writer.WriteLine($"Login successed! Hello {name}");
                                currName = name;
                            }
                            else
                            {
                                writer.WriteLine("Login failed");
                            }
                            writer.Flush();
                            break;

                        case "LOG OUT":
                            if (isLogin)
                            {
                                writer.WriteLine($"\nYou are log out");
                                isLogin = false;
                                currName = null;
                            }
                            else
                            {
                                writer.WriteLine("Login failed");
                            }
                            writer.Flush();
                            break;

                        case "QUIT":
                            writer.WriteLine("Good buy!!!");
                            writer.Flush();
                            goto END;

                    }
                }

                END:;
                Console.WriteLine($"Disonnect from IP: {remoteAddr}");
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Disonnect from IP: {clientAdrr}");
            }
        }

        private static bool CheckUser(string login, string passwd, out string name)
        {
            var controller = new PlumbusDbController();
            var list = controller.GetUsers();
            var users = list.Where( x => x.Login == login && x.Pwd==passwd );
            if (users.Count()!=0)
            {
                name = users.First().Name;
                return true;
            }

            name = null;
            return false;
        }
    }
}
