using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.LinkLabel;

namespace ParticleDatapack
{
    public partial class Form1 : Form
    {
        public int amountprogress = 0;

        public int faceslength = 0;
        public int verticylength = 0;
        public int vnlength = 0;

        public Form1()
        {
            InitializeComponent();
            

            if (Properties.Settings.Default["FileOutput"] != null)
            {
                textBox4.Text = Properties.Settings.Default["FileOutput"].ToString();
            }

            if(Properties.Settings.Default["FileName"] != null)
            {
                textBox5.Text = Properties.Settings.Default["FileName"].ToString();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int size = -1;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            List<string> lines = new List<string>();

            if (result == DialogResult.OK) // Test result.
            {
                string file = openFileDialog1.FileName;
                textBox1.Text = file;

                try
                {
                    foreach (string line in File.ReadAllLines(file))
                    {
                        lines.Add(line);
                    }


                    size = File.ReadAllLines(file).Length;
                }
                catch (IOException)
                {
                }


                Console.WriteLine(size); // <-- Shows file size in debugging mode.
                Console.WriteLine(result); // <-- For debugging use.
                Console.WriteLine(lines[4]);

                if (File.Exists(GetDirNoFile(openFileDialog1.FileName) + "model.mtl"))
                {
                    textBox2.Text = GetDirNoFile(openFileDialog1.FileName) + "model.mtl";
                }
            }
            
            

            
        }


        private string CleanFileName(string input)
        {
            string[] slipraw = input.Split(Path.DirectorySeparatorChar);

            string ou = slipraw[slipraw.Length - 1];

            string[] nameraw = ou.Split('.');


            return nameraw[0];

        }

        private string GetDirNoFile(string file)
        {
            string[] slipraw = file.Split(Path.DirectorySeparatorChar);

            string news = "";

            for (int i = 0; i < slipraw.Length; i++)
            {
                //your code here
                if (i != slipraw.Length - 1)
                {
                    if (i == 0)
                    {
                        news += slipraw[i];
                    }
                    else if (i != 0)
                    {
                        news += (Path.DirectorySeparatorChar + slipraw[i]);
                    }

                }
                else
                {
                    news += Path.DirectorySeparatorChar;
                }
            }

            return news;


        }

        private void button3_Click(object sender, EventArgs e)
        {

            if(textBox1.Text == "")
            {
                MessageBox.Show("You Must select a model");
                return;
            }
            if(textBox5.Text == "")
            {
                MessageBox.Show("You Must select a file name");
            }
            amountprogress = 0;

            string pathout = Path.Combine(textBox4.Text, textBox5.Text + ".mcfunction");

            if (!File.Exists(pathout))
            {
                File.Create(pathout);
                MessageBox.Show("Please try again, This is a common error \n (already being written to)");
                return;
            }

            // Get the materials - using a Dictionary here because it maps keys to values
            // e.g. the name of the material is the key, and its colour is the value.
            // Makes it efficient and easy to look up a material name and get its colour.
            Dictionary<string, OBJMat> objmats = GetMaterialsFromMTL(textBox2.Text);

            //string s = "v -0.086489 1.865501 0.138342";
            //string stringout = ConvertToCommand(s, textBox2.Text);

            //string write = "line \n another line \n \n space";

            //File.WriteAllText(pathout, write);

            

            List<string> writelines = new List<string>();
            List<Vertex> vertices = new List<Vertex>();
            OBJMat currentMaterial = new OBJMat { r = 0, b = 0, g = 0 };

            // I changed the function of this loop so that it stores all the vertices for a part
            // of the object, and then when it sees the 'usemtl' line, it gets the colour for the
            // material and then writes all the vertices with that colour. Then it deletes the
            // vertices list (makes a new empty list) so that those vertices don't appear in the
            // next part of the object.



            int o = 0;
            foreach (string line in File.ReadLines(textBox1.Text))
            {
                if (line.Contains("v"))
                {


                    if (!line.Contains("vn"))
                    {
                        verticylength += 1;
                        vertices.Add(new Vertex { line = line, material = currentMaterial }); // store the vertices for the object
                        //writelines.Add(ConvertToCommand(line, textBox2.Text));
                    }
                    if (line.Contains("vn"))
                    {
                        vnlength += 1;
                    }

                }
                else if (line.Contains("f "))
                {
                    faceslength += 1;
                    string[] facesSplit = line.Split(' ');
                    //Console.WriteLine(facesSplit[1]);
                    int v1 = int.Parse(facesSplit[1].Split('/')[0]); // take the vertex before the '/' and convert to a number
                    int v2 = int.Parse(facesSplit[2].Split('/')[0]);
                    int v3 = int.Parse(facesSplit[3].Split('/')[0]);
                    vertices[v1].material = currentMaterial; // Set the colour on the vertex
                    vertices[v2].material = currentMaterial;
                    vertices[v3].material = currentMaterial;
                }
                else if (line.Contains("usemtl"))
                {
                    string[] mtlsplit = line.Split(' ');
                    try
                    {
                        currentMaterial = objmats[mtlsplit[1]]; // look up the material's colour
                    }
                    catch (KeyNotFoundException) // if it doesn't exist, make it black
                    {
                        currentMaterial = new OBJMat { r = 0, g = 0, b = 0 };
                    }
                }


                o++;
            }

            foreach(Vertex v in vertices)
            {
                writelines.Add(ConvertToCommand(v.line, v.material));
            }

            try
            {
                File.WriteAllLines(pathout, writelines);
            }
            catch(IOException)
            {
                progressBar1.Value = 100;
                MessageBox.Show("Please try again, This is a common error \n (the file is being used by anotehr application)");
                
                return;
            }
            

            Console.WriteLine("Faces: " + faceslength + " Verticies: " + verticylength + " VN: " + vnlength);
            progressBar1.Value = 100;




            //Console.WriteLine(stringout);

        }

        private string ConvertToCommand(string objline, OBJMat material)
        {
            string[] splitraw = objline.Split(' ');

            if (splitraw[0] == "v")
            {
                //float x = float.Parse(splitraw[1]);
                //float y = float.Parse(splitraw[2]);
                //float z = float.Parse(splitraw[3]);



                float colorx = material.r;
                float colory = material.g;
                float colorz = material.b;

                //"particle dust " + color1 + " " + color2 + " " + color3 + " 1 " + cline + " 0 0 0 0 1 force"
                // particle dust 0.8 0.009515 0.8 1 ~-0.530915 ~0.120492 ~0.930629 0 0 0 0 1 force

                //List<OBJMat> objmats = new List<OBJMat>();
                //objmats = GetMaterialsFromMTL(mtlline);
                Console.WriteLine("BOOP, amount progress: " + amountprogress + " max verticys: " + verticylength + " extimated: " + updateprogress(amountprogress, verticylength));
                amountprogress += 1;

                progressBar1.Value = (int)(updateprogress(amountprogress, verticylength) * 100);
                




                //string comm = "particle dust " + colorx + " " + colory + " " + colorz + " 1 ~" + x + " ~" + y + " ~" + z + " 0 0 0 0 1 force";
                string comm = "particle dust " + colorx + " " + colory + " " + colorz + " " + ParticleProperties.size + " ~" + splitraw[1] + " ~" + splitraw[2] + " ~" + splitraw[3] + " 0 0 0 0 1 force";

                return comm;
            }
            else
            {
                return "";
            }



        }



        private Dictionary<string, OBJMat> GetMaterialsFromMTL(string path)
        {
            List<string> AllLines = new List<string>();

            Dictionary<string, OBJMat> mats = new Dictionary<string, OBJMat>();



            foreach (string line in File.ReadAllLines(path))
            {
                AllLines.Add(line);


            }

            int i = 0;
            foreach (string l in AllLines)
            {
                if (l.Contains("newmtl"))
                {




                    string[] splitline = l.Split(' ');
                    string matnam = splitline[1];


                    string colorline = AllLines[i + 3];

                    string[] splitcolor = colorline.Split(' ');

                    float red = float.Parse(splitcolor[1]);
                    float green = float.Parse(splitcolor[2]);
                    float blue = float.Parse(splitcolor[3]);







                    mats.Add(matnam, new OBJMat() { r = red, g = green, b = blue });

                    Console.WriteLine("----------");
                    Console.WriteLine(matnam);
                    Console.WriteLine(red);
                    Console.WriteLine(green);
                    Console.WriteLine(blue);
                }

                i++;
            }

            return mats;




        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            int scrollpos = trackBar1.Value;

            ParticleProperties.size = float.Parse(scrollpos.ToString()) / 10;

            textBox3.Text = ParticleProperties.size.ToString();
            
        }

        private float updateprogress(int prog, int maxgrog)
        {
            float progfloat = float.Parse(prog.ToString());
            float maxprogfloat = float.Parse(maxgrog.ToString());

            float result = progfloat / maxprogfloat;

            return result;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    //string[] files = Directory.GetFiles(fbd.SelectedPath);

                    //System.Windows.Forms.MessageBox.Show("Files found: " + files.Length.ToString(), "Message");
                    textBox4.Text = fbd.SelectedPath;
                    Properties.Settings.Default["FileOutput"] = fbd.SelectedPath;
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            
            Properties.Settings.Default["FileName"] = textBox5.Text;
            Properties.Settings.Default.Save();
        }
    }

    public class OBJMat
    {
        public float r;
        public float b;
        public float g;
    }

    public class Vertex
    {
        public string line;
        public OBJMat material;
    }

    public static class ParticleProperties
    {
        public static float size = 1;
    }

}
