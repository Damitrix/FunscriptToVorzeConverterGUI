using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace FunscriptToVorzeConverterGUI
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ResetSafezoneButton_Click(object sender, RoutedEventArgs e)
        {
            SafezoneTextBox.Text = "300";
        }

        private void ResetMultiButton_Click(object sender, RoutedEventArgs e)
        {
            MultiTextBox.Text = "2.4";
        }

        private void Convert(string inputFile, string outputFile)
        {
            float multi = float.Parse(MultiTextBox.Text, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            int savezone = System.Convert.ToInt32(SafezoneTextBox.Text);

            string inputScript = File.ReadAllText(inputFile);
            StreamWriter outputStream = File.CreateText(outputFile);

            //Writing all Actions from Json to Array
            Actions actions = JsonConvert.DeserializeObject<Actions>(inputScript);

            Console.WriteLine("Starting conversion of " + Path.GetFileName(inputFile));

            Direction lastDirection = Direction.Left;
            int lastDirectionChange = 0;

            int i = 0;
            foreach (var action in actions.actions)
            {
                if (i == actions.actions.Count - 1)
                {
                    //No more Commands, setting to 0 and stopping
                    outputStream.WriteLine(action.at / 100 + ",0,0");
                    break;
                }

                var thisAt = action.at;
                var thisPos = action.pos;

                var nextAt = actions.actions[i + 1].at;
                var nextPos = actions.actions[i + 1].pos;

                long posDif = 0;
                Direction direction = 0;

                if (thisPos - nextPos < 0)
                {
                    posDif = -(thisPos - nextPos);

                    //Make sure, the unit will not be damaged by applying a minimum Delay between direction changes
                    if (lastDirection != Direction.Right && thisAt - lastDirectionChange > savezone)
                    {
                        direction = Direction.Right;
                    }
                }
                else
                {
                    posDif = thisPos - nextPos;

                    //Make sure, the unit will not be damaged by applying a minimum Delay between direction changes
                    if (lastDirection != Direction.Left && thisAt - lastDirectionChange > savezone)
                    {
                        direction = Direction.Left;
                    }
                }

                lastDirection = direction;
                lastDirectionChange = System.Convert.ToInt32(thisAt);

                //Really bad calculation done at 5 in the morning
                double force = Math.Round(((posDif * 10) / ((nextAt - thisAt) / 10)) * multi);
                if (force > 100)
                {
                    force = 100;
                }
                outputStream.WriteLine(thisAt / 100 + "," + (int)direction + "," + force);

                i++;
            }
            outputStream.Close();
            Console.WriteLine("Finished conversion of " + Path.GetFileNameWithoutExtension(inputFile));
        }

        private enum Direction
        {
            Left,
            Right
        }

        private class Actions
        {
            public List<action> actions;

            public class action
            {
                public long at;
                public long pos;
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if((bool)ConvertAllCheckbox.IsChecked)
            {
                foreach(var file in Directory.GetFiles(Directory.GetCurrentDirectory()))
                {
                    try
                    {
                        Convert(file, file + ".csv");
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show("Oops, something went wrong." + Environment.NewLine + ex.Message);
                    }
                }
            }
            else
            {
                if(File.Exists(InputTextBox.Text))
                {
                    Convert(InputTextBox.Text, OutputTextBox.Text);
                    MessageBox.Show("Done!");
                }
            }
        }

        private void ChooseInputButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fDiag = new OpenFileDialog()
            {
                Filter = "Funscript Files (*.funscript)|*.funscript"
            };
            if ((bool)fDiag.ShowDialog())
            {
                InputTextBox.Text = fDiag.FileName;
            }
        }

        private void ChooseOutputButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog fDiag = new SaveFileDialog()
            {
                Filter = "Vorze Player Files (*.csv)|*.csv|All files (*.*)|*.*"
            };
            if ((bool)fDiag.ShowDialog())
            {
                OutputTextBox.Text = fDiag.FileName;
            }
        }
    }
}
