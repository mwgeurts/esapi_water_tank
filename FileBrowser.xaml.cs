using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
    /// <param name="context">ScriptContext that contains the API handle to the patient currently open in Eclipse</param>
    public partial class FileBrowser : UserControl
    {
        public ScriptContext context;

        // chartHeight and chartWidth are used when drawing profiles and must match the size of the uiChartArea canvas 
        // in FileBrowser.xaml. In the future I would like the code to determine these values automatically and support
        // dynamic resizing. Alas, for another day.
        readonly double chartHeight = 370;
        readonly double chartWidth = 539;

        // FileBrowser constructor
        public FileBrowser()
        {
            InitializeComponent();

            // Initialize chart area with sine and cosine waves. The variable res specifies the resolution of the lines
            // that are used when drawing the waves (using a for loop from 1 to res).
            double res = 700;
            for (int i = 0; i < res; i++)
            {
                // Add a red line segment to the uiChartArea canvas representing the sine wave
                uiChartArea.Children.Add(new Line()
                {
                    X1 = i / res * chartWidth,
                    X2 = (i + 1) / res * chartWidth,
                    Y1 = (Math.Sin(i * 10 / res) + 1) * chartHeight / 2,
                    Y2 = (Math.Sin((i + 1) * 10 / res) + 1) * chartHeight / 2,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1
                });

                // Add a blue line segment to the uiChartArea canvas representing the cosine wave
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
        
        /// <summary>
        /// interp is a helper function called by CompareProfiles several times to perform linear interpolation
        /// between two points: (x0,y0) and (x1,y1) at the point x.
        /// </summary>
        /// <param name="x">double</param>
        /// <param name="x0">double</param>
        /// <param name="x1">double</param>
        /// <param name="y0">double</param>
        /// <param name="y1">double</param>
        /// <returns>the interpolated value of y as a double</returns>
        public static double interp(double x, double x0, double x1, double y0, double y1)
        {
            // Special case if x1 == x0
            if ((x1 - x0) == 0)
            {
                return (y0 + y1) / 2;
            }

            // Otherwise, return the standard equation for linear interpolation
            return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
        }

        // BrowseFile is called when the Browse button is clicked, opens the file browser dialog. 
        // The resulting file locating is sent back to the UI.
        private void BrowseFile(object sender, RoutedEventArgs e)
        {
            // Clear all results fields and the chart canvas
            ClearResults();
            uiChartArea.Children.Clear();

            // Open a new file selection dialog with the following parameters
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Filter = "SNCTXT Files (*.snctxt)|*.snctxt|ICP Text Files (*.txt)|*.txt";
            fileDialog.Title = "Select the water tank profile...";
            fileDialog.Multiselect = false;
            var success = fileDialog.ShowDialog();

            // If the user selected a file, update the file name input box (if not, leave the existing value).
            if ((bool)success)
            {
                uiFile.Text = fileDialog.FileName;

                // If a .txt was selected, enable the dropdown menu to select which profile
                if (uiFile.Text.EndsWith(".txt", StringComparison.CurrentCultureIgnoreCase))
                    uiICP.Visibility = Visibility.Visible;

                else
                    uiICP.Visibility = Visibility.Hidden;
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

            // Clear all results fields
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

            // Clear all results fields
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

            // Clear all results fields
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

            // Clear all results fields
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

            // Clear all results fields
            ClearResults();
        }

        // RadioChange is called whenever the normalization radio button is changed
        private void RadioChange(object sender, RoutedEventArgs e)
        {
            // Clear all results fields
            ClearResults();
        }

        // ClearResults is called whenever an input variable is changed and clears the results text boxes
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
            uiLC80.Text = "";
            uiGPass.Text = "";
            uiGAvg.Text = "";
            uiGMax.Text = "";
            uiGC80.Text = "";
        }

        // CompareProfiles is called when the form button is clicked and compares the dose volume of the current plan to
        // the selected text file. The results are reported back to the UI and the profiles are plotted.
        private void CompareProfiles(object sender, RoutedEventArgs e)
        {

            List<Profile> txt;

            // If the user has not selected a text file yet, inform them that is a required step 
            if (uiFile.Text == "")
            {
                MessageBox.Show("You must select a SNC TXT file first");
                return;
            }

            // Otherwise, if a .snctxt file was selected
            else if (uiFile.Text.EndsWith(".snctxt", StringComparison.CurrentCultureIgnoreCase))
            {
                // Run ParseSNCTXT to extract the first profile from the selected file
                txt = Script.ParseSNCTXT(uiFile.Text);
            }

            // Otherwise, if a .txt file was selected
            else if (uiFile.Text.EndsWith(".txt", StringComparison.CurrentCultureIgnoreCase))
            {
                // Run ParseICPTXT to extract the IC Profiler profile from the selected file
                txt = Script.ParseICPTXT(uiFile.Text, uiICP.Text);
            }

            // Otherwise, an unknown file was selected
            else
            {
                MessageBox.Show("An unknown file type was selected");
                return;
            }

            // Extract a line dose from the current planned dose using the coordinates from the SNC TXT profile, converting
            // to DICOM coordinates using the UserToDicom ESAPI function
            VVector start = context.Image.UserToDicom(txt.First().Position, context.PlanSetup);
            VVector end = context.Image.UserToDicom(txt.Last().Position, context.PlanSetup);

            // Set the TPS resolution equal to 10X the measured (this is a reasonable balance between gamma calculation accuracy
            // and computation speed; increasing this multiplier will increase gamma accuracy but with diminishing returns)
            Double.TryParse(Regex.Match(uiDTA.Text, @"\d+\.*\d*").Value, out double dta);
            DoseProfile tpsProfile = context.PlanSetup.Dose.GetDoseProfile(start, end, 
                new double[txt.Count() * 10]);

            // Store the DoseProfile object as Profile list, converting the coordinates back from DICOM and normalizing
            // to the maximum value along the profile
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

            // Initialize a new list to store the convolved TPS profile. Convolving the TPS makes the comparison to the
            // measurement more accurate, as it can account for measurement parameters such as detector size or scan speed 
            List<Profile> convtps = new List<Profile>();

            // Retrieve the convolution parameters from the UI
            Double.TryParse(Regex.Match(uiSigma.Text, @"\d+\.*\d*").Value, out double sigma);
            Double.TryParse(Regex.Match(uiTruncation.Text, @"\d+\.*\d*").Value, out double trunc);

            // If the convolution filter parameter is zero, skip convolution
            if (sigma == 0)
            {
                convtps = tps;
            }

            // Otherwise, apply a truncated Gaussian convolution to the TPS profile
            else
            {
                // Precalculate the denominator of filter to increase computation speed
                sigma = 2 * Math.Pow(sigma, 2);

                // Initialize flags to keep track of edge conditions
                bool firstEdge;
                bool lastEdge;

                // Loop through each point in the TPS profile
                foreach (Profile point in tps)
                {
                    // Initialize a new convoluted value at the same position with an initial value of zero
                    Profile nextRow = new Profile
                    {
                        Position = point.Position,
                        Value = 0
                    };

                    // If the point is too close to the start of the profile, set the startEdge flag
                    if ((point.Position - tps.First().Position).Length <= trunc)
                    {
                        firstEdge = true;
                    }
                    else
                    {
                        firstEdge = false;
                    }

                    // If the point is too close to the end of the profile, set the lastEdge flag
                    if ((point.Position - tps.Last().Position).Length <= trunc)
                    {
                        lastEdge = true;
                    }
                    else
                    {
                        lastEdge = false;
                    }

                    // Loop through the TPS profile again (yes, Fourier Transform would be faster, but this keeps it simple)
                    foreach (Profile filter in tps)
                    {
                        // If the distance between the filter position and the profile point exceeds the truncation distance,
                        // do not apply the filter
                        if ((point.Position - filter.Position).Length > trunc)
                            continue;

                        // Otherwise, apply the Gaussian convolution based on the distance along the filter and the sigma parameter
                        // provided by the user
                        else
                        {
                            // If the point and filter positions are the same, don't apply the filter (exp(0) = 1)
                            if (point.Position.Equals(filter.Position))
                            {
                                nextRow.Value += filter.Value;
                            }

                            // If the point's position is near the the starting edge of the profile such that the filter is cutoff, mirror
                            // it to prevent the edge effect (by mirror, double its value)
                            else if (firstEdge && (point.Position - filter.Position).Length > (point.Position - tps.First().Position).Length)
                            {
                                nextRow.Value += 2 * filter.Value * Math.Exp(-Math.Pow((point.Position - filter.Position).Length, 2) / sigma);
                            }

                            // If the point's position is near the the back edge of the profile such that the filter is cutoff
                            else if (lastEdge && (point.Position - filter.Position).Length > (point.Position - tps.Last().Position).Length)
                            {
                                nextRow.Value += 2 * filter.Value * Math.Exp(-Math.Pow((point.Position - filter.Position).Length, 2) / sigma);
                            }

                            // If neither condition, just apply the regular filter (with no edge conditions)
                            else
                            {
                                nextRow.Value += filter.Value * Math.Exp(-Math.Pow((point.Position - filter.Position).Length, 2) / sigma);
                            }                          
                        }

                    }

                    // Keep track of the convolved TPS profile max value so that it can be renormalized
                    if (nextRow.Value > maxval)
                    {
                        maxval = nextRow.Value;
                    }

                    // Add the convolved point to the Profile list
                    convtps.Add(nextRow);
                }
            }

            // Normalize the convolved profile back to 100% 
            foreach (Profile point in convtps)
            {
                point.Value = point.Value / maxval * 100;
            }

            // Initialize temporary variable to store rounded statistics
            double t;

            // Initialize temporary variablse to store FWHM and center (used for central 80% determination) using the full profile. If a valid
            // FWHM is found later, we will use that instead
            double fwhm = (txt.Last().Position - txt.First().Position).Length;
            VVector center = (txt.Last().Position + txt.First().Position) / 2;

            // Initialize temporary variable to store max depth (used for depth profiles)
            double maxdepth = 0;

            // If the depth axis changes, assume this is a depth profile, so calculate PDD or R50 (based on if it is an photon or electron)
            if (Math.Abs(txt.First().Position[1] - txt.Last().Position[1]) > 10)
            {
                // Initialize measured (SNC TXT) and calculated (TPS) depth metric variables
                double dmeas = 0;
                double dcalc = 0;

                // Loop through the profile to find max depth (use the measured profile)
                for (int i = 1; i < txt.Count(); i++)
                {
                    if (txt[i].Value == 100)
                    {
                        maxdepth = txt[i].Position[1];
                        break;
                    }
                }

                // If the treatment beam energy contains an "E", it is an Electron beam, so calculate R50 (note, this assumes that the first
                // beam is the one that was used to calculate dose; there may be a better way to determine this)
                if (context.PlanSetup.Beams.First().EnergyModeDisplayName.Contains('E'))
                {
                    // Loop through the SNC TXT profile using indices (necessary since [i] and [i-1] are looked at together)
                    for (int i = 1; i < txt.Count(); i++)
                    {
                        // If the values at [i] and [i-1] are above and below R50
                        if (Math.Sign(txt[i - 1].Value - 50) != Math.Sign(txt[i].Value - 50))
                        {
                            // Interpolate between the depths at [i] and [i-1] to determine R50
                            dmeas = interp(50, txt[i - 1].Value, txt[i].Value, txt[i - 1].Position[1], txt[i].Position[1]);
                            
                            // Round and update the UI with the SNC TXT R50 value
                            t = Math.Round(dmeas * 10) / 100;
                            uiDmeas.Text = t.ToString() + " cm";

                            // End the for loop, as R50 was found
                            break;
                        }
                    }

                    // Loop through the convolved TPS profile using indices (necessary since [i] and [i-1] are looked at together)
                    for (int i = 1; i < convtps.Count(); i++)
                    {
                        // If the values at [i] and [i-1] are above and below R50
                        if (Math.Sign(convtps[i - 1].Value - 50) != Math.Sign(convtps[i].Value - 50))
                        {
                            // Interpolate between the depths at [i] and [i-1] to determine R50
                            dcalc = interp(50, convtps[i - 1].Value, convtps[i].Value, convtps[i - 1].Position[1], convtps[i].Position[1]);

                            // Round and update the UI with the convolved TPS R50 value
                            t = Math.Round(dcalc * 10) / 100;
                            uiDcalc.Text = t.ToString() + " cm";

                            // End the for loop, as R50 was found
                            break;
                        }
                    }

                    // If both the SNC TXT and TPS R50 values were found
                    if (dmeas != 0 && dcalc != 0)
                    {
                        // Calculate and round the difference between the two, then report to the UI
                        t = Math.Round((dcalc - dmeas) * 10) / 100;
                        uiDdiff.Text = t.ToString() + " cm";
                    }
                }

                // Otherwise, if "E" is not present in the beam energy, this is a photon energy, so calculate PDD(10)
                else
                {
                    // Loop through the SNC TXT profile using indices (necessary since [i] and [i-1] are looked at together)
                    for (int i = 1; i < txt.Count(); i++)
                    {
                        // If the depth at [i] and [i-1] are above and below 10 cm
                        if (Math.Sign(txt[i - 1].Position[1] - 100) != Math.Sign(txt[i].Position[1] - 100))
                        {
                            // Interpolate between [i] and [i-1] to PDD(10)
                            dmeas = interp(100, txt[i - 1].Position[1], txt[i].Position[1], txt[i - 1].Value, txt[i].Value);

                            // Round and update the UI with the SNC TXT PDD(10) value
                            t = Math.Round(dmeas * 100) / 100;
                            uiDmeas.Text = t.ToString() + "%";

                            // End the for loop, as PDD(10) was found
                            break;
                        }
                    }

                    // Loop through the convolved TPS profile using indices (necessary since [i] and [i-1] are looked at together)
                    for (int i = 1; i < convtps.Count(); i++)
                    {
                        // If the depth at [i] and [i-1] are above and below 10 cm
                        if (Math.Sign(convtps[i - 1].Position[1] - 100) != Math.Sign(convtps[i].Position[1] - 100))
                        {
                            // Interpolate between [i] and [i-1] to PDD(10)
                            dcalc = interp(100, convtps[i - 1].Position[1], convtps[i].Position[1], 
                                convtps[i - 1].Value, convtps[i].Value);

                            // Round and update the UI with the convolved TPS PDD(10) value
                            t = Math.Round(dcalc * 100) / 100;
                            uiDcalc.Text = t.ToString() + "%";

                            // End the for loop, as PDD(10) was found
                            break;
                        }
                    }

                    // If both the SNC TXT and TPS PDD(10) values were found
                    if (dmeas != 0 && dcalc != 0)
                    {
                        // Calculate and round the difference between the two, then report to the UI
                        t = Math.Round((dcalc - dmeas) * 100) / 100;
                        uiDdiff.Text = t.ToString() + "%";
                    }
                }
            }

            // Otherwise, if the depth does not change, calculate the FWHM of the profiles
            else
            {
                // Run CalculateFWHM using the SNC TXT profile and store the resulting FWHM value
                (double fmeas, VVector cmeas) = Script.CalculateFWHM(txt);

                // If a valid FWHM was returned, round and report it to the UI
                if (fmeas != 0)
                {
                    fwhm = fmeas;
                    center = cmeas;
                    t = Math.Round(fmeas * 10) / 100;
                    uiFmeas.Text = t.ToString() + " cm";
                }


                // Run CalculateFWHM using the convolved TPS profile and store the resulting FWHM value
                (double fcalc, VVector ccalc) = Script.CalculateFWHM(convtps);

                // If a valid FWHM was returned, round and report it to the UI
                if (fcalc != 0)
                {
                    fwhm = fcalc;
                    center = ccalc;
                    t = Math.Round(fcalc * 10) / 100;
                    uiFcalc.Text = t.ToString() + " cm";
                }

                // If both a valid SNC TXT and convolved TPS FWHM were found, calculate and round the difference
                if (fmeas != 0 && fcalc != 0)
                {
                    t = Math.Round((fcalc - fmeas) * 10) / 100;
                    uiFdiff.Text = t.ToString() + " cm";
                }
            }

            // If field center/D10 normalization is selected
            if (uiCenter.IsChecked == true)
            {
                // Initialize temporary variables to store normalization factor
                double normtxt = 0;
                double normtps = 0;
                double count = 0;

                // If depth axis changes, normalize to D10
                if (Math.Abs(txt.First().Position[1] - txt.Last().Position[1]) > 10)
                {
                    // Loop through the SNC TXT profile using indices (necessary since [i] and [i-1] are looked at together)
                    for (int i = 1; i < txt.Count(); i++)
                    {
                        // If the depth at [i] and [i-1] are above and below 10 cm
                        if (Math.Sign(txt[i - 1].Position[1] - 100) != Math.Sign(txt[i].Position[1] - 100))
                        {
                            // Interpolate between [i] and [i-1] to determine normalization factor
                            normtxt = interp(100, txt[i - 1].Position[1], txt[i].Position[1], txt[i - 1].Value, txt[i].Value);

                            // End the for loop, as PDD(10) was found
                            break;
                        }
                    }

                    // Loop through the convolve TPS profile using indices (necessary since [i] and [i-1] are looked at together)
                    for (int i = 1; i < convtps.Count(); i++)
                    {
                        // If the depth at [i] and [i-1] are above and below 10 cm
                        if (Math.Sign(convtps[i - 1].Position[1] - 100) != Math.Sign(convtps[i].Position[1] - 100))
                        {
                            // Interpolate between [i] and [i-1] to determine normalization factor
                            normtps = interp(100, convtps[i - 1].Position[1], convtps[i].Position[1], convtps[i - 1].Value, convtps[i].Value);

                            // End the for loop, as PDD(10) was found
                            break;
                        }
                    }

                    // Loop through SNC TXT profile again, normalizing TPS to TXT D10
                    foreach (Profile point in convtps)
                    {
                        if (normtps > 0)
                        {
                            point.Value = point.Value * normtxt / normtps;
                        }
                    }
                }

                // Otherwise, normalize to field center (average over the central 10%)
                else
                {
                    // Loop through the SNC TXT profile
                    foreach (Profile point in txt)
                    {
                        // If point position is within 10% of the FWHM, add value and increment counter (to average)
                        if ((point.Position - center).Length <= fwhm * 0.1)
                        {
                            normtxt += point.Value;
                            count++;
                        }
                    }

                    // Calculate average value and reset counter
                    normtxt = normtxt / count;
                    count = 0;

                    // Loop through the SNC TXT profile
                    foreach (Profile point in convtps)
                    {
                        // If point position is within 10% of the FWHM, add value and increment counter (to average)
                        if ((point.Position - center).Length <= fwhm * 0.1)
                        {
                            normtps += point.Value;
                            count++;
                        }
                    }

                    // Calculate average value
                    normtps = normtps / count;

                    // Loop through convolved TPS profile again, normalizing by center
                    foreach (Profile point in convtps)
                    {
                        point.Value = point.Value * normtxt / normtps;
                    }
                }
            }

            // Retrieve the other two gamma criteria from the UI (DTA was retrieved earlier when parsing the TPS profile)
            Double.TryParse(Regex.Match(uiPercent.Text, @"\d+\.*\d*").Value, out double percent);
            Double.TryParse(Regex.Match(uiThreshold.Text, @"\d+\.*\d*").Value, out double threshold);

            // Execute CalculateGamma using the SNC TXT profile, convoluted TPS, and gamma criteria
            List<Profile> gamma = Script.CalculateGamma(txt, convtps, percent, dta, threshold);

            // Initialize the gamma statistics variables
            double localPass = 0;
            double globalPass = 0;
            double localAverage = 0;
            double globalAverage = 0;
            double localMax = 0;
            double globalMax = 0;
            double localCentral = 0;
            double globalCentral = 0;
            double countCentral = 0;

            // If a valid gamma Profile list was returned
            if (gamma.Count > 0)
            {
                // Loop through the Profile list (remember, point.Value = local, point.Value2 = global)
                foreach (Profile point in gamma)
                {
                    // If the local gamma value passed, increment the local pass rate
                    if (point.Value <= 1)
                    {
                        localPass++;       
                    }

                    // Update the local average statistic
                    localAverage += point.Value;

                    // Update the local maximum gamma statistic
                    if (localMax < point.Value)
                    {
                        localMax = point.Value;
                    }

                    // If the global gamma value passed, increment the global pass rate
                    if (point.Value2 <= 1)
                    {
                        globalPass++;
                    }

                    // Update the global average statistic
                    globalAverage += point.Value2;

                    // Update the local maximum gamma statistic
                    if (globalMax < point.Value2)
                    {
                        globalMax = point.Value2;
                    }

                    // If the profile point is within the central 80% of the FWHM or beyond the maximum depth
                    if (maxdepth > 0 && point.Position[1] > maxdepth)
                    {
                        // Count the total number of central 80% values
                        countCentral++;

                        // Count the number of central 80% local pass values
                        if (point.Value <= 1)
                        {
                            localCentral++;
                        }

                        // Count the number of central 80% global pass values
                        if (point.Value2 <= 1)
                        {
                            globalCentral++;
                        }
                    }
                    
                    else if (fwhm > 0 && (point.Position - center).Length < fwhm * 0.4)
                    {
                        // Count the total number of central 80% values
                        countCentral++;

                        // Count the number of central 80% local pass values
                        if (point.Value <= 1)
                        {
                            localCentral++;
                        }

                        // Count the number of central 80% global pass values
                        if (point.Value2 <= 1)
                        {
                            globalCentral++;
                        }
                    }
                }

                // Finish the pass rate statistics (pass rate = sum passed / total number of points * 100)
                localPass = localPass / gamma.Count * 100;
                globalPass = globalPass / gamma.Count * 100;

                // Finish the average statistics (average = sum / total number of points)
                localAverage /= gamma.Count;
                globalAverage /= gamma.Count;

                // Round and update the UI for all gamma statistics
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

                // Round and update the UI for central 80% values, if FWHM was calculated
                if (maxdepth > 0 || fwhm > 0)
                {
                    localCentral = localCentral / countCentral * 100;
                    globalCentral = globalCentral / countCentral * 100;

                    t = Math.Round((localCentral) * 10) / 10;
                    uiLC80.Text = t.ToString() + "%";

                    t = Math.Round((globalCentral) * 10) / 10;
                    uiGC80.Text = t.ToString() + "%";

                    // Set label based on value
                    if (maxdepth > 0)
                    {
                        uiLabel.Content = "Below Dmax";
                    }
                    else
                    {
                        uiLabel.Content = "Central 80";
                    }
                }
            }

            // Clear the Chart Area of all previous profiles
            uiChartArea.Children.Clear();

            // Draw the SNC TXT Profile using red lines
            double xscale = chartWidth / (txt.Last().Position - txt.First().Position).Length;
            for (int i = 1; i < txt.Count; i++)
            {
                uiChartArea.Children.Add(new Line()
                {
                    X1 = (txt[i - 1].Position - txt.First().Position).Length * xscale,
                    X2 = (txt[i].Position - txt.First().Position).Length * xscale,
                    Y1 = (chartHeight - 20) * (100 - txt[i - 1].Value) / 100 + 20,
                    Y2 = (chartHeight - 20) * (100 - txt[i].Value) / 100 + 20,
                    Stroke = Brushes.Red,
                    StrokeThickness = 1
                });
            }

            // Draw the convolved TPS Profile using blue lines (skip NaN values)
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
                    Y1 = (chartHeight - 20) * (100 - convtps[i - 1].Value) / 100 + 20,
                    Y2 = (chartHeight - 20) * (100 - convtps[i].Value) / 100 + 20,
                    Stroke = Brushes.Blue,
                    StrokeThickness = 1
                });
            }

            // Plot global gamma profile (note, the scale X axis is scaled based on the SNC TXT profile as the gamma
            // profile may not contain the same dimensions as the other two profiles due to thresholding. Also, the
            // Y axis scale is dynamically set to exither the maximum gamma value or a gamma value of 1)
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