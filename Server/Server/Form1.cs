using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Net.Sockets;


namespace Server
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    tb_filePath.Text = fbd.SelectedPath;
                }
            }
        }

        private void btn_startServer_Click(object sender, EventArgs e)
        {
            if (tb_filePath.Text == string.Empty)
            {
                MessageBox.Show("Please Select a directory to download file and then start server","Error");
            }
            else
            {
                TCPServer obj_server = new TCPServer(tb_filePath.Text);
                System.Threading.Thread obj_thread = new System.Threading.Thread(obj_server.StartServer);
                obj_thread.Start();
            }
        }
    }

    class TCPServer
    {
        TcpListener obj_server;
        string file_path = "";
        public TCPServer(string filepath)
        {
            obj_server = new TcpListener(IPAddress.Any, 6868);
            file_path = filepath;
        }

        public void StartServer()
        {
            obj_server.Start();
            while (true)
            {
                TcpClient tc = obj_server.AcceptTcpClient();
                SocketHandler obj_handler = new SocketHandler(tc , file_path);
                System.Threading.Thread obj_thread = new System.Threading.Thread(obj_handler.ProcessSocketRequest);
                obj_thread.Start();
            }
        }

        class SocketHandler
        {
            NetworkStream ns;
            string file_path = "";
            public SocketHandler(TcpClient tc , string filepath)
            {
                ns = tc.GetStream();
                file_path = filepath + '\\';
            }

            public void ProcessSocketRequest()
            {
                FileStream fs = null;
                long current_file_pointer = 0;
                Boolean loop_break = false;
                while (true)
                {
                    if (ns.ReadByte() == 2)
                    {
                        byte[] cmd_buff = new byte[3];
                        ns.Read(cmd_buff, 0, cmd_buff.Length);
                        byte[] recv_data = ReadStream();
                        switch (Convert.ToInt32(Encoding.UTF8.GetString(cmd_buff)))
                        {
                            case 125:
                                {
                                    fs = new FileStream( (file_path + Encoding.UTF8.GetString(recv_data)), FileMode.CreateNew);
                                    //fs = new FileStream(@"C:\tl\" + Encoding.UTF8.GetString(recv_data), FileMode.CreateNew);
                                    byte[] data_to_send = CreateDataPacket(Encoding.UTF8.GetBytes("126"), Encoding.UTF8.GetBytes(Convert.ToString(current_file_pointer)));
                                    ns.Write(data_to_send, 0, data_to_send.Length);
                                    ns.Flush();
                                }
                                break;
                            case 127:
                                {
                                    fs.Seek(current_file_pointer, SeekOrigin.Begin);
                                    fs.Write(recv_data, 0, recv_data.Length);
                                    current_file_pointer = fs.Position;
                                    byte[] data_to_send = CreateDataPacket(Encoding.UTF8.GetBytes("126"), Encoding.UTF8.GetBytes(Convert.ToString(current_file_pointer)));
                                    ns.Write(data_to_send, 0, data_to_send.Length);
                                    ns.Flush();
                                }
                                break;
                            case 128:
                                {
                                    fs.Close();
                                    loop_break = true;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    if (loop_break == true)
                    {
                        ns.Close();
                        break;
                    }
                }
            }

            public byte[] ReadStream()
            {
                byte[] data_buff = null;

                int b = 0;
                String buff_length = "";
                while ((b = ns.ReadByte()) != 4)
                {
                    buff_length += (char)b;
                }
                int data_length = Convert.ToInt32(buff_length);
                data_buff = new byte[data_length];
                int byte_read = 0;
                int byte_offset = 0;
                while (byte_offset < data_length)
                {
                    byte_read = ns.Read(data_buff, byte_offset, data_length - byte_offset);
                    byte_offset += byte_read;
                }

                return data_buff;
            }

            private byte[] CreateDataPacket(byte[] cmd, byte[] data)
            {
                byte[] initialize = new byte[1];
                initialize[0] = 2;
                byte[] separator = new byte[1];
                separator[0] = 4;
                byte[] datalength = Encoding.UTF8.GetBytes(Convert.ToString(data.Length));
                MemoryStream ms = new MemoryStream();
                ms.Write(initialize, 0, initialize.Length);
                ms.Write(cmd, 0, cmd.Length);
                ms.Write(datalength, 0, datalength.Length);
                ms.Write(separator, 0, separator.Length);
                ms.Write(data, 0, data.Length);
                return ms.ToArray();
            }
        }

    }
}
