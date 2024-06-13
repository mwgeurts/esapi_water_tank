using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VMS.TPS;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using Microsoft.Win32;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Media;
using System.Collections.Generic;

namespace ProfileComparison
{

    /// <summary>
    /// The FileBrowser class contains the interaction methods for FileBrowser.xaml.
    /// </summary>
    /// <param name="context">context contains the API handle to the patient currently open in Eclipse</param>
    public partial class FileBrowser : UserControl
    {

        public ScriptContext context;

        // FileBrowser constructor
        public FileBrowser()
        {
            InitializeComponent();
        }

        public static double interp(double x, double x0, double x1, double y0, double y1)
        {
            if ((x1 - x0) == 0)
            {
                return (y0 + y1) / 2;
            }
            return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
        }

        // Browse is called when the Browse button is clicked, opens the file browser dialog. 
        // The resulting file locating is sent back to the UI.
        private void BrowseFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "SNCTXT Files (*.snctxt)|*.snctxt";
            fileDialog.Title = "Select the water tank profile...";
            fileDialog.Multiselect = false;

            var success = fileDialog.ShowDialog();
            if ((bool)success)
            {
                uiFile.Text = fileDialog.FileName;
            }
        }

        // ValidateSigma is called whenever the sigma input field is changed, to validate and reformat the input
        private void ValidateSigma(object sender, KeyboardFocusChangedEventArgs e)
        {
            // First try to parse the input text as a number (the regex automatically removes the units)
            Double.TryParse(Regex.Match(uiSigma.Text, @"\d+\.*\d*").Value, out double test);
            if (test > 0)
            {
                // If successful, store the parsed number to three decimal precision with mm units
                test = Math.Round(test * 1000) / 1000;
                uiSigma.Text = test.ToString() + " mm";
            }
            else
                // If not successful, revert the field to the default value
                uiSigma.Text = "0.000 mm";
        }

        // ValidateTruncation is called whenever the threshold input field is changed, to validate and reformat the input
        private void ValidateTruncation(object sender, KeyboardFocusChangedEventArgs e)
        {
            // First try to parse the input text as a number (the regex automatically removes the units)
            Double.TryParse(Regex.Match(uiTruncation.Text, @"\d+\.*\d*").Value, out double test);
            if (test > 0)
            {
                // If successful, store the parsed number to single decimal precision with the mm units
                test = Math.Round(test * 1000) / 1000;
                uiTruncation.Text = test.ToString() + " mm";
            }
            else
                // If not successful, revert the field to the default value
                uiTruncation.Text = "0.000 mm";
        }

        // ValidateTruncation is called whenever the threshold input field is changed, to validate and reformat the input
        private void ValidateDTA(object sender, KeyboardFocusChangedEventArgs e)
        {
            // First try to parse the input text as a number (the regex automatically removes the units)
            Double.TryParse(Regex.Match(uiDTA.Text, @"\d+\.*\d*").Value, out double test);
            if (test > 0)
            {
                // If successful, store the parsed number to single decimal precision with the mm units
                test = Math.Round(test * 10) / 10;
                uiDTA.Text = test.ToString() + " mm";
            }
            else
                // If not successful, revert the field to the default value
                uiDTA.Text = "1.0 mm";
        }

        // ValidateThreshold is called whenever the threshold input field is changed, to validate and reformat the input
        private void ValidateThreshold(object sender, KeyboardFocusChangedEventArgs e)
        {
            // First try to parse the input text as a number (the regex automatically removes the units)
            Double.TryParse(Regex.Match(uiThreshold.Text, @"\d+\.*\d*").Value, out double test);
            if (test > 0)
            {
                // If successful, store the parsed number to single decimal precision with a percent symbol
                test = Math.Round(test * 10) / 10;
                uiThreshold.Text = test.ToString() + "%";
            }
            else
                // If not successful, revert the field to the default value
                uiThreshold.Text = "10.0%";
        }

        // ValidatePercent is called whenever the Gamma percent input field is changed, to validate and reformat the input
        private void ValidatePercent(object sender, KeyboardFocusChangedEventArgs e)
        {
            // First try to parse the input text as a number (the regex automatically removes the % symbol)
            Double.TryParse(Regex.Match(uiPercent.Text, @"\d+\.*\d*").Value, out double test);
            if (test > 0)
            {
                // If successful, store the parsed number to single decimal precision with a percent symbol
                test = Math.Round(test * 10) / 10;
                uiPercent.Text = test.ToString() + "%";
            }
            else
                // If not successful, revert the field to the default value
                uiPercent.Text = "2.0%";
        }

        // CompareProfiles is called when the form button is clicked, and compares the dose volume of the current plan to
        // the selected text file. The results are reported back to the UI and a message box is displayed.
        private void CompareProfiles(object sender, RoutedEventArgs e)
        {

            // If the user has not selected a text file yet, 
            if (uiFile.Text == "")
            {
                MessageBox.Show("You must select a SNC TXT file first");
                return;
            }

            // Run ParseSNCTXT to extract the first profile from the selected file
            List<Profile> txt = Script.ParseSNCTXT(uiFile.Text);

            // Extract a line dose from the current planned dose using the coordinates from the SNC TXT profile, converting to
            // DICOM coordinates using the UserToDicom ESAPI function
            VVector start = context.Image.UserToDicom(txt.First().Position, context.PlanSetup);
            VVector end = context.Image.UserToDicom(txt.Last().Position, context.PlanSetup);
            
            // Set the TPS resolution equal to 10X the DTA
            Double.TryParse(Regex.Match(uiDTA.Text, @"\d+\.*\d*").Value, out double dta);
            DoseProfile tpsProfile = context.PlanSetup.Dose.GetDoseProfile(start, end, new double[(int) Math.Ceiling((end - start).Length / dta * 10)]);

            // Store the DoseProfile object as Profile list, converting profile coordinates back from DICOM
            List<Profile> tps = new List<Profile>();
            double maxval = 0;
            foreach (ProfilePoint profile in tpsProfile)
            {
                Profile nextRow = new Profile();
                nextRow.Position = context.Image.DicomToUser(profile.Position, context.PlanSetup);
                nextRow.Value = profile.Value;
                tps.Add(nextRow);

                if (profile.Value > maxval)
                {
                    maxval = profile.Value;
                }
            }

            foreach (Profile point in tps)
            {
                point.Value = point.Value / maxval * 100;
            }


            // Apply convolution

            //
            //
            //  TO DO
            //
            //


            double t = 0;

            // If the depth axis changes, assume this is a depth profile, calculate PDD or R50 (based on if it is an photon or electron)
            if (Math.Abs(txt.First().Position[1] - txt.Last().Position[1]) > 10)
            {
                double dmeas = 0;
                double dcalc = 0;
                

                if (context.PlanSetup.PhotonCalculationModel is null)
                {
                    //
                    //
                    // R50 TO DO
                    //
                    //
                }
                else
                {
                    for (int i = 1; i < txt.Count(); i++)
                    {
                        if (Math.Sign(txt[i - 1].Position[1] - 100) != Math.Sign(txt[i].Position[1] - 100))
                        {
                            dmeas = interp(100, txt[i - 1].Position[1], txt[i].Position[1], txt[i-1].Value, txt[i].Value);
                            t = Math.Round(dmeas * 100) / 100;
                            uiDmeas.Text = t.ToString() + "%";
                            break;
                        }
                    }

                    for (int i = 1; i < tps.Count(); i++)
                    {
                        if (Math.Sign(tps[i - 1].Position[1] - 100) != Math.Sign(tps[i].Position[1] - 100))
                        {
                            dcalc = (tps[i - 1].Position[1] + tps[i].Position[1]) / 2;
                            t = Math.Round(dcalc * 100) / 100;
                            uiDcalc.Text = t.ToString() + "%";
                            break;
                        }
                    }

                    if (dmeas != 0 && dcalc != 0)
                    {
                        t = Math.Round((dcalc - dmeas) * 100) / 100;
                        uiDdiff.Text = t.ToString() + "%";
                    }
                }
            }

            // Otherwise, calculate the FWHM of the profiles
            else
            {
                //
                //
                // TO DO
                //
                //

            }

            // Perform Gamma evaluation and update UI
            Double.TryParse(Regex.Match(uiPercent.Text, @"\d+\.*\d*").Value, out double percent);
            Double.TryParse(Regex.Match(uiThreshold.Text, @"\d+\.*\d*").Value, out double threshold);

            double[] gammaStats = Script.CalculateGamma(txt, tps, percent, dta, threshold);

            if (gammaStats != null)
            {
                t = Math.Round((gammaStats[0]) * 10) / 10;
                uiPass.Text = t.ToString() + "%";

                t = Math.Round((gammaStats[1]) * 100) / 100;
                uiAvg.Text = t.ToString();

                t = Math.Round((gammaStats[2]) * 100) / 100;
                uiMax.Text = t.ToString();
            }

        }

    }
}