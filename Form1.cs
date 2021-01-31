﻿using System;
using System.Windows.Forms;
// using GMap.NET;
// using GMap.NET.MapProviders;
using SharpGL;
using SharpGL.WinForms;
using SharpGL.SceneGraph;
using System.Diagnostics;
using System.Threading;
using System.IO.Ports;
// using System.Windows.Forms.DataVisualization.Charting; // Chart
// using GMap.NET.WindowsForms; // GMapControl
using SocketIOClient;
using Newtonsoft.Json;
using System.IO;
using Assimp;
using System.Runtime.InteropServices;
using System.Globalization;




namespace CanSatGUI
{
  



    public partial class Form1 : Form
    {
        public SocketIO client = new SocketIO("http://77.55.213.87:3000");

        Stopwatch timer = new Stopwatch();
        string rxString;
        //string ComPort;
        StreamWriter writer;
        int VERTICES_LENGTH;





        public Form1()  //definiowanie ustawienia oraz port szeregowy
        {
            if (Debugger.IsAttached)
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-US");

            // sets double conversion separator to '.'
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            // init window
            InitializeComponent();

            // init timer
            timer.Start();

            // setup output file
            string date_time = Utils.GetTimestamp(DateTime.Now);
            System.IO.Directory.CreateDirectory("output");
            writer = new StreamWriter("output/output" + date_time + ".log");
        }

        // poczatek czesci wykonawczej serial port txt box v1.0
        private void myPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            rxString = SerialPort1.ReadLine();

            writer.Write(rxString);
            writer.Flush();

            //Console.WriteLine(rxString);
            try
            {
                this.Invoke(new EventHandler(UpdateWidgets));
            }
            catch (System.IndexOutOfRangeException)
            { }
        }

        //private void UpdateWidgets(object sender, EventArgs e)
        private async void UpdateWidgets(object sender, EventArgs e)
        {

            // RSSI; framenr; xaccel; yaccel; zaccel; xtilt; ytilt; ztilt; xmag; ymag; zmag; pressure; temp; lat; long; alt; speed; course; h:m:s:ms; hall
            DataStream.AppendText(rxString);
            string[] packetElems = Utils.ParsePacket(rxString);
            double time = Convert.ToInt32(timer.Elapsed.TotalSeconds);

            if (packetElems == null || packetElems.Length != 20)
            {
                Console.WriteLine("Skipped corrupted packet");
                return;
            }

            rssitxt.Text = packetElems[0];
            framenrtxt.Text = packetElems[1];
            xacceltxt.Text = packetElems[2];
            yacceltxt.Text = packetElems[3];
            zacceltxt.Text = packetElems[4];
            xtilttxt.Text = packetElems[5];
            ytilttxt.Text = packetElems[6];
            ztilttxt.Text = packetElems[7];
            xmagtxt.Text = packetElems[8];
            ymagtxt.Text = packetElems[9];
            zmagtxt.Text = packetElems[10];
            psrtxt.Text = packetElems[11];
            temptxt.Text = packetElems[12];
            lattxt.Text = packetElems[13];
            longtxt.Text = packetElems[14];
            alttxt.Text = packetElems[15];
            speedtxt.Text = packetElems[16];
            coursetxt.Text = packetElems[17];
            timetxt.Text = packetElems[18];
            halltxt.Text = packetElems[19];

            double temperature = Convert.ToDouble(packetElems[3]);
            Upd.UpdateChart(chart1, temperature, time);

            double pressure = Convert.ToDouble(packetElems[2]);
            Upd.UpdateChart(chart2, pressure, time);

            double Hall = Convert.ToDouble(packetElems[19]);
            double windSpeed = Utils.WindSpeed(Hall);
            Upd.UpdateChart(chart3, windSpeed, time);

            double Altitude = Convert.ToDouble(packetElems[15]);
            Upd.UpdateChart(chart4, Altitude, time);

            double signal = Convert.ToDouble(packetElems[0]);
            Upd.UpdateChart(chart5, signal, time);

            double speed = Convert.ToDouble(packetElems[16]);
            Upd.UpdateChart(chart6, speed, time);
            
            double latitude = Convert.ToDouble(packetElems[5]);
            double longtitude = Convert.ToDouble(packetElems[6]);
            Upd.UpdateMap(map, latitude, longtitude);

            // socketio
            string json = JsonConvert.SerializeObject(new { psrtxt = psrtxt.Text, tmptxt = temptxt.Text, txtLat = lattxt.Text, txtLong = longtxt.Text, hghttext = alttxt.Text });
            try
            {
                _ = client.EmitAsync("data", json);
            }
            catch { }
            // catch (SocketIOClient.Exceptions.InvalidSocketStateException) { }

            // opengl
            Upd.UpdateOpenGLControl(openGLControl1, VERTICES_LENGTH / 3);
        }

        private void psrtxt_TextChanged(object sender, EventArgs e)
        {

        }

        private void chart2_Click(object sender, EventArgs e)
        {

        }

        private void map_Load(object sender, EventArgs e)
        {

        }

        private void openGLControl1_Load(object sender, EventArgs e)
        {
            OpenGL gl = openGLControl1.OpenGL;
            // OpenGL.IsExtensionFunctionSupported("glGenVertexArrays");
            string glVersion = gl.GetString(OpenGL.GL_VERSION);
            Console.WriteLine(glVersion);

            string vertexshadersource = File.ReadAllText("Shaders/shader.vs");
            string fragmentshadersource = File.ReadAllText("Shaders/shader.fs");

            // load vertex shader
            var vertexShader = gl.CreateShader(OpenGL.GL_VERTEX_SHADER);
            gl.ShaderSource(vertexShader, vertexshadersource);
            gl.CompileShader(vertexShader);

            // load fragment shader
            var fragmentShader = gl.CreateShader(OpenGL.GL_FRAGMENT_SHADER);
            gl.ShaderSource(fragmentShader,fragmentshadersource );
            gl.CompileShader(fragmentShader);

            // compile shaders
            var shaderProgram = gl.CreateProgram();
            gl.AttachShader(shaderProgram, vertexShader);
            gl.AttachShader(shaderProgram, fragmentShader);

            // link shaders
            gl.LinkProgram(shaderProgram);
            gl.DetachShader(shaderProgram, vertexShader);
            gl.DetachShader(shaderProgram, fragmentShader);
            gl.DeleteShader(vertexShader);
            gl.DeleteShader(fragmentShader);

            // create VAO
            uint[] VAO = new uint[1]; // vertex array object
            uint[] VBO = new uint[1]; // vertex buffer object
            gl.GenVertexArrays(1, VAO);
            gl.GenBuffers(1, VBO);

            // use this for object context (VAO) so we can configure it
            gl.BindVertexArray(VAO[0]);

            // bind buffer and static vertices data to current VAO
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, VBO[0]);

            // setup OpenGL
            Assimp.Scene cansatModel = assimp_load_obj();

            // triangle vertices
            for (int j = 0; j < cansatModel.MeshCount; j++)
            {
                VERTICES_LENGTH += 3 * cansatModel.Meshes[j].VertexCount;
            }
            float[] vertices = new float[VERTICES_LENGTH];
            int i = 0;
            for (int m = 0; m < cansatModel.MeshCount; m++)
            {
                foreach (Assimp.Vector3D vertice in cansatModel.Meshes[m].Vertices)
                {
                    vertices[i] = vertice[0] / 3.0f;
                    i++;
                    vertices[i] = vertice[1] / 3.0f;
                    i++;
                    vertices[i] = vertice[2] / 3.0f;
                    i++;
                }
            }
            
            IntPtr verticesPtr = Marshal.AllocHGlobal(VERTICES_LENGTH * sizeof(float));
            //float[] vertices = { -0.5f, -0.5f, 1.0f, 0.5f, -0.5f, 1.0f, 0.0f, 0.5f, 1.0f };
            Marshal.Copy(vertices, 0, verticesPtr, VERTICES_LENGTH);

            // bind vetices data to current VAO
            //IntPtr verticesPtr = Utils.IntPtrFromFloatArray(vertices, VERTICES_SIZE);
            gl.BufferData(OpenGL.GL_ARRAY_BUFFER, VERTICES_LENGTH*sizeof(float), verticesPtr, OpenGL.GL_STATIC_DRAW);
            Marshal.FreeHGlobal(verticesPtr);

            // linking vertex attributes to current VAO
            gl.VertexAttribPointer(0, 3, OpenGL.GL_FLOAT, false, 3 * sizeof(float), IntPtr.Zero);
            gl.EnableVertexAttribArray(0);

            // now we can unbind VBO
            gl.BindBuffer(OpenGL.GL_ARRAY_BUFFER, 0);
            gl.BindVertexArray(0);

            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.UseProgram(shaderProgram);
            gl.BindVertexArray(VAO[0]);

        }


        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            client.OnConnected += async (sender_socket, e_socket) => {
                //await client.EmitAsync("data", ".net core");
            };
            try
            {
                await client.ConnectAsync();
            }
            catch (System.Net.WebSockets.WebSocketException)
            {

            }
        }

       

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string Dropdown;
            
            Dropdown = ComboBox1.GetItemText(ComboBox1.SelectedItem);
            //Console.WriteLine(Dropdown);

            // init COM
            SerialPort1 = Utils.InitSerialPort(Dropdown);     
            SerialPort1.DataReceived += myPort_DataReceived;

            // ComPort = Dropdown;



        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void DataStream_TextChanged(object sender, EventArgs e)
        {
            DataStream.ScrollToCaret();
          
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {

        }

        private void chart3_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void chart4_Click(object sender, EventArgs e)
        {

        }

        private void chart6_Click(object sender, EventArgs e)
        {

        }

        private Assimp.Scene assimp_load_obj()
        {
            Assimp.Scene model;
            Assimp.AssimpContext importer = new Assimp.AssimpContext();
            // importer.SetConfig(new Assimp.Configs.NormalSmoothingAngleConfig(66.0f));
            model = importer.ImportFile("Assets/cansat_bezspadochronu.obj"); // , Assimp.PostProcessPreset.TargetRealTimeMaximumQuality
            return model;
        }
        
    }
}


