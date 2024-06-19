using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using VMS.TPS;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace ProfileComparison
{

    /// <summary>
    /// The FileBrowser class contains the interaction methods for FileBrowser.xaml. It is called from ProfileComparison.cs
    /// </summary>
    /// <param name="context">context contains the API handle to the patient currently open in Eclipse</param>
    public partial class FileBrowser : UserControl
    {

        public ScriptContext context;

        readonly double chartHeight = 370;
        readonly double chartWidth = 437;

        // FileBrowser constructor
        public FileBrowser()
        {
            InitializeComponent();

            // Initialize chart area with sine and cosine waves
            double res = 700;
            for (int i = 0; i < res; i++)
            {
                uiChartArea.Children.Add(new Line()
                {
                    X1 = i / res * chartWidth,
                    X2 = (i + 1) / res * chartWidth,
                    Y1 = (Math.Sin(i * 10 / res) + 1) * chartHeight / 2,
                    Y2 = (Math.Sin((i + 1) * 10 / res) + 1) * chartHeight / 2,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1
                });
                uiChartArea.Children.Add(new Line()
                {
                    X1 = i * chartWidth / res,
                    X2 = (i + 1) / res * chartWidth,
                    Y1 = (Math.Cos(i * 10 / res) + 1) * chartHeight / 2,
                    Y2 = (Math.Cos((i + 1) * 10 / res) + 1) * chartHeight / 2,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 1
                });
            }
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
            ClearResults();
            uiChartArea.Children.Clear();

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
            Double.TryParse(Regex.Match(uiSigma.Text, @"\d+\.*\d*").Value, out double t);
            if (t > 0)
            {
                // If successful, store the parsed number to three decimal precision with mm units
                t = Math.Round(t * 1000) / 1000;
                uiSigma.Text = t.ToString() + " mm";
            }
            else
                // If not successful, revert the field to the default value
                uiSigma.Text = "0.000 mm";

            ClearResults();
        }

        // ValidateTruncation is called whenever the threshold input field is changed, to validate and reformat the input
        private void ValidateTruncation(object sender, KeyboardFocusChangedEventArgs e)
        {
            // First try to parse the input text as a number (the regex automatically removes the units)
            Double.TryParse(Regex.Match(uiTruncation.Text, @"\d+\.*\d*").Value, out double t);
            if (t > 0)
            {
                // If successful, store the parsed number to single decimal precision with the mm units
                t = Math.Round(t * 1000) / 1000;
                uiTruncation.Text = t.ToString() + " mm";
            }
            else
                // If not successful, revert the field to the default value
                uiTruncation.Text = "0.000 mm";

            ClearResults();
        }

        // ValidateTruncation is called whenever the threshold input field is changed, to validate and reformat the input
        private void ValidateDTA(object sender, KeyboardFocusChangedEventArgs e)
        {
            // First try to parse the input text as a number (the regex automatically removes the units)
            Double.TryParse(Regex.Match(uiDTA.Text, @"\d+\.*\d*").Value, out double t);
            if (t > 0)
            {
                // If successful, store the parsed number to single decimal precision with the mm units
                t = Math.Round(t * 10) / 10;
                uiDTA.Text = t.ToString() + " mm";
            }
            else
                // If not successful, revert the field to the default value
                uiDTA.Text = "1.0 mm";

            ClearResults();
        }

        // ValidateThreshold is called whenever the threshold input field is changed, to validate and reformat the input
        private void ValidateThreshold(object sender, KeyboardFocusChangedEventArgs e)
        {
            // First try to parse the input text as a number (the regex automatically removes the units)
            Double.TryParse(Regex.Match(uiThreshold.Text, @"\d+\.*\d*").Value, out double t);
            if (t >= 0)
            {
                // If successful, store the parsed number to single decimal precision with a percent symbol
                t = Math.Round(t * 10) / 10;
                uiThreshold.Text = t.ToString() + "%";
            }
            else
                // If not successful, revert the field to the default value
                uiThreshold.Text = "20.0%";

            ClearResults();
        }

        // ValidatePercent is called whenever the Gamma percent input field is changed, to validate and reformat the input
        private void ValidatePercent(object sender, KeyboardFocusChangedEventArgs e)
        {
            // First try to parse the input text as a number (the regex automatically removes the % symbol)
            Double.TryParse(Regex.Match(uiPercent.Text, @"\d+\.*\d*").Value, out double t);
            if (t > 0)
            {
                // If successful, store the parsed number to single decimal precision with a percent symbol
                t = Math.Round(t * 10) / 10;
                uiPercent.Text = t.ToString() + "%";
            }
            else
                // If not successful, revert the field to the default value
                uiPercent.Text = "1.0%";

            ClearResults();
        }

        // ClearResults is called whenever an input variable is changed, and clears the results fields
        private void ClearResults()
        {
            uiDmeas.Text = "";
            uiDcalc.Text = "";
            uiDdiff.Text = "";
            uiFmeas.Text = "";
            uiFcalc.Text = "";
            uiFdiff.Text = "";
            uiLPass.Text = "";
            uiLAvg.Text = "";
            uiLMax.Text = "";
            uiGPass.Text = "";
            uiGAvg.Text = "";
            uiGMax.Text = "";
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
            DoseProfile tpsProfile = context.PlanSetup.Dose.GetDoseProfile(start, end, new double[(int)Math.Ceiling((end - start).Length / dta * 10)]);

            // Store the DoseProfile object as Profile list, converting profile coordinates back from DICOM and normalizing to the maximum dose in the plan
            List<Profile> tps = new List<Profile>();
            double maxval = 0;
            foreach (ProfilePoint point in tpsProfile)
            {
                Profile nextRow = new Profile();
                nextRow.Position = context.Image.DicomToUser(point.Position, context.PlanSetup);
                nextRow.Value = point.Value;
                tps.Add(nextRow);

                if (point.Value > maxval)
                {
                    maxval = point.Value;
                }
            }


            List<Profile> convtps = new List<Profile>();
            Double.TryParse(Regex.Match(uiSigma.Text, @"\d+\.*\d*").Value, out double sigma);
            Double.TryParse(Regex.Match(uiTruncation.Text, @"\d+\.*\d*").Value, out double trunc);

            // If the convolution filter is zero, skip convolution
            if (sigma == 0)
            {
                convtps = tps;
            }
            else
            {

                // Precalculate denominator of filter
                sigma = 2 * Math.Pow(sigma, 2);

                // Apply convolution to calculated dose
                for (int i = 0; i < tps.Count; i++)
                {
                    Profile nextRow = new Profile();
                    nextRow.Position = tps[i].Position;
                    nextRow.Value = 0;

                    for (int j = 0; j < tps.Count; j++)
                    {
                        if ((tps[i].Position - tps[j].Position).Length > trunc)
                            continue;

                        else
                        {
                            nextRow.Value += tps[j].Value * Math.Exp(-Math.Pow((tps[i].Position - tps[j].Position).Length, 2) / sigma);
                        }

                    }

                    // Update maxval for normalization
                    if (nextRow.Value > maxval)
                    {
                        maxval = nextRow.Value;
                    }

                    convtps.Add(nextRow);
                }
            }

            // Normalize profile to 100%
            foreach (Profile point in convtps)
            {
                point.Value = point.Value / maxval * 100;
            }

            double t = 0;

            // If the depth axis changes, assume this is a depth profile, calculate PDD or R50 (based on if it is an photon or electron)
            if (Math.Abs(txt.First().Position[1] - txt.Last().Position[1]) > 10)
            {
                double dmeas = 0;
                double dcalc = 0;

                if (context.PlanSetup.Beams.First().EnergyModeDisplayName.Contains('E'))
                {
                    for (int i = 1; i < txt.Count(); i++)
                    {
                        if (Math.Sign(txt[i - 1].Value - 50) != Math.Sign(txt[i].Value - 50))
                        {
                            dmeas = interp(50, txt[i - 1].Value, txt[i].Value, txt[i - 1].Position[1], txt[i].Position[1]);
                            t = Math.Round(dmeas * 10) / 100;
                            uiDmeas.Text = t.ToString() + " cm";
                            break;
                        }
                    }

                    for (int i = 1; i < convtps.Count(); i++)
                    {
                        if (Math.Sign(convtps[i - 1].Value - 50) != Math.Sign(convtps[i].Value - 50))
                        {
                            dcalc = interp(50, convtps[i - 1].Value, convtps[i].Value, convtps[i - 1].Position[1], convtps[i].Position[1]);
                            t = Math.Round(dcalc * 10) / 100;
                            uiDcalc.Text = t.ToString() + " cm";
                            break;
                        }
                    }

                    if (dmeas != 0 && dcalc != 0)
                    {
                        t = Math.Round((dcalc - dmeas) * 10) / 100;
                        uiDdiff.Text = t.ToString() + " cm";
                    }

                }
                else
                {
                    for (int i = 1; i < txt.Count(); i++)
                    {
                        if (Math.Sign(txt[i - 1].Position[1] - 100) != Math.Sign(txt[i].Position[1] - 100))
                        {
                            dmeas = interp(100, txt[i - 1].Position[1], txt[i].Position[1], txt[i - 1].Value, txt[i].Value);
                            t = Math.Round(dmeas * 100) / 100;
                            uiDmeas.Text = t.ToString() + "%";
                            break;
                        }
                    }

                    for (int i = 1; i < convtps.Count(); i++)
                    {
                        if (Math.Sign(convtps[i - 1].Position[1] - 100) != Math.Sign(convtps[i].Position[1] - 100))
                        {
                            dcalc = interp(100, convtps[i - 1].Position[1], convtps[i].Position[1], convtps[i - 1].Value, convtps[i].Value);
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
                double fmeas = Script.CalculateFWHM(txt);
                if (fmeas != 0)
                {
                    t = Math.Round(fmeas * 10) / 100;
                    uiFmeas.Text = t.ToString() + " cm";
                }

                double fcalc = Script.CalculateFWHM(convtps);
                if (fcalc != 0)
                {
                    t = Math.Round(fcalc * 10) / 100;
                    uiFcalc.Text = t.ToString() + " cm";
                }
                if (fmeas != 0 && fcalc != 0)
                {
                    t = Math.Round((fcalc - fmeas) * 10) / 100;
                    uiFdiff.Text = t.ToString() + " cm";
                }

            }

            // Perform Gamma evaluation and update UI
            Double.TryParse(Regex.Match(uiPercent.Text, @"\d+\.*\d*").Value, out double percent);
            Double.TryParse(Regex.Match(uiThreshold.Text, @"\d+\.*\d*").Value, out double threshold);

            List<Profile> gamma = Script.CalculateGamma(txt, convtps, percent, dta, threshold);

            // Calculate gamma statistics
            double localPass = 0;
            double globalPass = 0;
            double localAverage = 0;
            double globalAverage = 0;
            double localMax = 0;
            double globalMax = 0;

            if (gamma.Count > 0)
            {
                foreach (Profile point in gamma)
                {
                    if (point.Value <= 1)
                    {
                        localPass = localPass + 1;
                    }
                    localAverage = localAverage + point.Value;

                    if (localMax < point.Value)
                    {
                        localMax = point.Value;
                    }

                    if (point.Value2 <= 1)
                    {
                        globalPass = globalPass + 1;
                    }
                    globalAverage = globalAverage + point.Value2;

                    if (globalMax < point.Value2)
                    {
                        globalMax = point.Value2;
                    }
                }

                localPass = localPass / gamma.Count * 100;
                globalPass = globalPass / gamma.Count * 100;

                localAverage = localAverage / gamma.Count;
                globalAverage = globalAverage / gamma.Count;

                t = Math.Round((localPass) * 10) / 10;
                uiLPass.Text = t.ToString() + "%";

                t = Math.Round((localAverage) * 100) / 100;
                uiLAvg.Text = t.ToString();

                t = Math.Round((localMax) * 100) / 100;
                uiLMax.Text = t.ToString();

                t = Math.Round((globalPass) * 10) / 10;
                uiGPass.Text = t.ToString() + "%";

                t = Math.Round((globalAverage) * 100) / 100;
                uiGAvg.Text = t.ToString();

                t = Math.Round((globalMax) * 100) / 100;
                uiGMax.Text = t.ToString();
            }

            // Clear Chart Area
            uiChartArea.Children.Clear();

            // Draw TXT Profile
            double xscale = chartWidth / (txt.Last().Position - txt.First().Position).Length;
            for (int i = 1; i < txt.Count; i++)
            {
                uiChartArea.Children.Add(new Line()
                {
                    X1 = (txt[i - 1].Position - txt.First().Position).Length * xscale,
                    X2 = (txt[i].Position - txt.First().Position).Length * xscale,
                    Y1 = chartHeight * (100 - txt[i - 1].Value) / 100,
                    Y2 = chartHeight * (100 - txt[i].Value) / 100,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1
                });
            }

            // Draw TPS Profile
            xscale = chartWidth / (convtps.Last().Position - convtps.First().Position).Length;
            for (int i = 1; i < convtps.Count; i++)
            {
                if (Double.IsNaN(convtps[i - 1].Value) || Double.IsNaN(convtps[i].Value))
                {
                    continue;
                }

                uiChartArea.Children.Add(new Line()
                {
                    X1 = (convtps[i - 1].Position - convtps.First().Position).Length * xscale,
                    X2 = (convtps[i].Position - convtps.First().Position).Length * xscale,
                    Y1 = chartHeight * (100 - convtps[i - 1].Value) / 100,
                    Y2 = chartHeight * (100 - convtps[i].Value) / 100,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 1
                });
            }

            // Plot Local Gamma (scale X position based on txt file, as gamma profile does not contain all positions)
            xscale = chartWidth / (txt.Last().Position - txt.First().Position).Length;
            for (int i = 1; i < gamma.Count; i++)
            {
                uiChartArea.Children.Add(new Line()
                {
                    X1 = (gamma[i - 1].Position - txt.First().Position).Length * xscale,
                    X2 = (gamma[i].Position - txt.First().Position).Length * xscale,
                    Y1 = chartHeight * (Math.Max(1, globalMax) - gamma[i - 1].Value2) / Math.Max(1, globalMax),
                    Y2 = chartHeight * (Math.Max(1, globalMax) - gamma[i].Value2) / Math.Max(1, globalMax),
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 1
                });
            }



        }
    }
}