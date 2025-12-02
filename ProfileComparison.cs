using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace VMS.TPS
{
    /// <summary>
    /// Profile is a simple class used to store profile point objects as combinations of positions and values.
    /// </summary>
    /// <value name="Position">VMS.TPS.Common.Model.Types.VVector containing the three-dimensional position of the profile point</value>
    /// <value name="Value">double contianing the value at the profile point position</value>
    /// <value name="Value2">double containing an optional secondary value at the same position. This value is used to store the local and global Gamma values</value>
    public class Profile
    {
        public VVector Position;
        public double Value;
        public double Value2;
    }

    public class Script
    {

        public Script()
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]

        // Execute() is called by Eclipse when this script is run
        public void Execute(ScriptContext context, System.Windows.Window window/*, ScriptEnvironment environment*/)
        {
            // Launch a new user interface defined by FileBrowser.xaml
            var userInterface = new ProfileComparison.FileBrowser();
            window.Title = "Water Tank Profile Comparison Tool";
            window.Content = userInterface;
            window.Width = 1170;
            window.Height = 500;
            userInterface.uiCenter.IsChecked = true;

            // Pass the current patient context to the UI
            userInterface.context = context;
        }

        /// <summary>
        /// ParseSNXTXT is called by FileBrowser when the user selects a SNC TXT file to load, and is responsible for parsing the profile data 
        /// from it. The function will find the first group of profile points in the tab-delimited TXT file (identified by a series of four 
        /// numbers separated by tabs with no other text on the line), store them in a list of Profile objects (using the Profile class defined 
        /// above), and stop. If the SNC TXT file contains multiple profiles, only the first profile is returned.
        /// </summary>
        /// <param name="fileName">string containing the full path of the SNX TXT file to read</param>
        /// <returns>a list of Profile objects containing the positions and values of the first profile in the SNC TXT file</returns>
        /// <exception cref="ArgumentNullException">will be thrown if the filename parameter is empty</exception>
        public static List<Profile> ParseSNCTXT(string fileName)
        {
            // Verify the input parameter is valid
            if (fileName == null)
            {
                throw new ArgumentNullException("Provided file input must not be empty");
            }

            // Initialize list to store parsed lines
            List<Profile> parsedList = new List<Profile>();

            // Initialize counter and stream buffer variables
            int matchCount = 0;
            double maxval = 0;
            const Int32 BufferSize = 128;

            // Define regular expression to detect lines that contain profile points (0.000\t0.000\t0.000\t0.000)
            Regex r = new Regex(@"\t([-\d\.]+)\t([-\d\.]+)\t([-\d\.]+)\t([\d\.]+)");

            // Open the file in read-only mode using stream reader
            using (var fileStream = File.OpenRead(fileName))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                // Read the next line from the file into a string, until the end of the file
                String line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    // Try to match the line to the profile point pattern above
                    Match m = r.Match(line);
                    if (m.Success)
                    {
                        // Keep track of the number of matched lines
                        matchCount++;

                        // Retrieve each of the four matched numbers, parsing out as signed doubles
                        Double.TryParse(m.Groups[1].Value, out double x);
                        Double.TryParse(m.Groups[2].Value, out double y);
                        Double.TryParse(m.Groups[3].Value, out double z);
                        Double.TryParse(m.Groups[4].Value, out double d);

                        // Keep track of profile maximum (to normalize the profile at the end)
                        if (d > maxval)
                        {
                            maxval = d;
                        }

                        // Store the point as a new Profile object, flipping the Y and Z axes to match to the
                        // Eclipse coordinate system.
                        Profile nextRow = new Profile
                        {
                            Position = new VVector(x * 10, z * 10, y * 10),
                            Value = d
                        };

                        // Only add the profile point if it's position is greater than 0.05 mm from the last one (some profiles
                        // have duplicates or too high of resolution for Gamma calculation to function correctly).
                        if (matchCount < 2 || (parsedList.Last().Position - nextRow.Position).Length > 0.05)
                        {

                            // Add the Profile point to the list
                            parsedList.Add(nextRow);
                        }
                    }

                    // If the line did not match the profile point format, but previous points did, assume this means that the end
                    // of the profile has been reached, so stop parsing the file. This prevents parsing other profiles in case there 
                    // were multiple profiles stored in the SNC TXT file.
                    else if (matchCount > 0)
                    {
                        break;
                    }
                }
            }

            // Normalize the profile to 100%
            foreach (Profile point in parsedList)
            {
                point.Value = point.Value / maxval * 100;
            }

            // Return the Profile list
            return parsedList;
        }

        /// <summary>
        /// ParseICPTXT is called by FileBrowser when the user selects a ICP TXT file to load, and is responsible for parsing the profile data 
        /// from it. The function will first ask the user which profile to extract (X, Y, or diagonals) then find the first group of profile 
        /// points in the tab-delimited TXT file (identified by a series of two numbers separated by tabs with no other text on the line), 
        /// determine the corresponding 3D coordinates, store them in a list of Profile objects (using the Profile class defined above), and 
        /// stop. If the SNC TXT file contains multiple profiles, only the first profile is returned.
        /// </summary>
        /// <param name="fileName">string containing the full path of the SNX TXT file to read</param>
        /// <param name="type">string containing the profile to extract (X Axis, Y Axis, Positive Diagonal, or Negative Diagonal)</param>
        /// <returns>a list of Profile objects containing the positions and values of the first profile in the SNC TXT file</returns>
        /// <exception cref="ArgumentNullException">will be thrown if the filename parameter is empty</exception>
        public static List<Profile> ParseICPTXT(string fileName, string type)
        {

            // Defines the effective depth (in mm) of the IC Profiler detectors relative to device SSD. This is accounted for during analysis.
            double effDepth = 9.4;

            // Verify the input parameters are valid
            if (fileName == null)
            {
                throw new ArgumentNullException("Provided file input must not be empty");
            }
            if (type == null)
            {
                throw new ArgumentNullException("Provided profile type");
            }

            // Initialize list to store parsed lines
            List<Profile> parsedList = new List<Profile>();

            // Initialize counter and stream buffer variables
            int matchCount = 0;
            double maxval = 0;
            const Int32 BufferSize = 128;

            // Define regular expression to detect lines that contain profile points (0.000\t0.000)
            Regex r = new Regex(@"\t([-\d\.]+)\t([-\d\.]+)");

            // Initialize flag to know when correct profile has been found
            bool ready = false;

            // Open the file in read-only mode using stream reader
            using (var fileStream = File.OpenRead(fileName))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                // Read the next line from the file into a string, until the end of the file
                String line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    // Check the line to see if 
                    if (line.StartsWith("Detector ID\t" + type + " Position(cm)"))
                        ready = true;

                    // Try to match the line to the profile point pattern above
                    Match m = r.Match(line);
                    if (ready && m.Success)
                    {
                        // Keep track of the number of matched lines
                        matchCount++;

                        // Retrieve each of the four matched numbers, parsing out as signed doubles
                        Double.TryParse(m.Groups[1].Value, out double x);
                        Double.TryParse(m.Groups[2].Value, out double d);

                        // Keep track of profile maximum (to normalize the profile at the end)
                        if (d > maxval)
                        {
                            maxval = d;
                        }


                        // Store the point as a new Profile object based on the profile direction
                        if (type == "X Axis")
                        {
                            Profile nextRow = new Profile
                            {
                                Position = new VVector(x * 10, effDepth, 0),
                                Value = d
                            };

                            // Add the Profile point to the list
                            parsedList.Add(nextRow);
                        }
                        else if (type == "Y Axis")
                        {
                            Profile nextRow = new Profile
                            {
                                Position = new VVector(0, effDepth, x * 10),
                                Value = d
                            };

                            // Add the Profile point to the list
                            parsedList.Add(nextRow);
                        }
                        else if (type == "Positive Diagonal")
                        {
                            Profile nextRow = new Profile
                            {
                                Position = new VVector(x / Math.Sqrt(2) * 10, effDepth, x / Math.Sqrt(2) * 10),
                                Value = d
                            };

                            // Add the Profile point to the list
                            parsedList.Add(nextRow);
                        }
                        else
                        {
                            Profile nextRow = new Profile
                            {
                                Position = new VVector(x / Math.Sqrt(2) * 10, effDepth, x / Math.Sqrt(2) * -10),
                                Value = d
                            };

                            // Add the Profile point to the list
                            parsedList.Add(nextRow);
                        }
                    }

                    // If the line did not match the profile point format, but previous points did, assume this means that the end
                    // of the profile has been reached, so stop parsing the file. This prevents parsing other profiles in case there 
                    // were multiple profiles stored in the SNC TXT file.
                    else if (matchCount > 0)
                    {
                        break;
                    }
                }
            }

            // Normalize the profile to 100%
            foreach (Profile point in parsedList)
            {
                point.Value = point.Value / maxval * 100;
            }

            // Return the Profile list
            return parsedList;
        }

        /// <summary>
        /// CalculateGamma is called by FileBrowser after parsing the SNC TXT file and extracting the corresponding TPS profile to calculate
        /// the local and global gamma metric for each SNC TXT profile point based on the method of Low DA, Harms WB, Mutic S, Purdy JA. "A 
        /// technique for the quantitative evaluation of dose distributions. Med Phys. 1998 May;25(5):656-61. doi: 10.1118/1.598248."
        /// </summary>
        /// <param name="profile1">List of Profile point objects (see class above) for which gamma will be computed</param>
        /// <param name="profile2">List of Profile point objects that profile1 will be compared to. Note that profile2 is not interpolated 
        /// as part of this function, so must be passed to this function at a sufficiently high resolution to provide an accurate calculation. 
        /// For 1D profiles, this is recommended to be at least 10x the Distance to Agreement parameter</param>
        /// <param name="abs">Double containing the absolute value criterion as a percentage. For the local evaluation, each point will be 
        /// evaluated using an absolute criterion that is a percentage of that point. For the global evaluation, profile1 is assumed to be 
        /// normalizd to 100, so each point is evaluated to this percentage as an invariant value.</param>
        /// <param name="dta">Double containing the Distance to Agreement criterion in the same units as the Profile input list VVector 
        /// positions</param>
        /// <param name="threshold">Double containing the threshold parameter. All profile1 points with a value less than this value are 
        /// excluded from the gamma evaluation.</param>
        /// <returns>List of Profile objects containing the same Position VVectors as profile1 (but only the Profile points that exceeded the 
        /// threshold), and two values: the local gamma in value, and the global gamma in value2.</returns>
        public static List<Profile> CalculateGamma(List<Profile> profile1, List<Profile> profile2, double abs, double dta, double threshold)
        {
            // Initialize results object and temporary variables
            List<Profile> gammaList = new List<Profile>();
            double local;
            double global;
            double maxlocal;
            double maxglobal;

            // Loop through first list of Profile objects
            foreach (Profile point1 in profile1)
            {
                // Exclude values below threshold
                if (point1.Value < threshold)
                {
                    continue;
                }

                // Start at a default Gamma value of 1000 (arbitrary, used to determine min value)
                maxlocal = 1000;
                maxglobal = 1000;

                // Loop through second list of Profile objects
                foreach (Profile point2 in profile2)
                {

                    // Calculate local Gamma-squared
                    local = Math.Pow((point1.Value - point2.Value) / (point1.Value * abs / 100), 2) +
                        (point1.Position - point2.Position).LengthSquared / Math.Pow(dta, 2);

                    // Calculate global Gamma-squared
                    global = Math.Pow((point1.Value - point2.Value) / abs, 2) +
                        (point1.Position - point2.Position).LengthSquared / Math.Pow(dta, 2);

                    // Update minimum Gamma-squared
                    if (local < maxlocal)
                    {
                        maxlocal = local;
                    }
                    if (global < maxglobal)
                    {
                        maxglobal = global;
                    }

                    // If a gamma of basically zero is found, skip ahead to next point (this is meant to speed things up, so that if the 
                    // two profiles are very similar the function will not spend too much time recalculating very low values of Gamma). 
                    // You can reduce or eliminate this code to slightly increase the accuracy of this function.
                    if (local < 0.01)
                    {
                        break;
                    }
                }

                // Copy positions from first profile and apply sqaure root to return values (applying the square root here is meant to save 
                // a computation step in the for loop above, as it does not affect the determination of the maximum value).
                Profile gamma = new Profile
                {
                    Position = point1.Position,
                    Value = Math.Sqrt(maxlocal),
                    Value2 = Math.Sqrt(maxglobal)
                };
                gammaList.Add(gamma);
            }

            // Return the List<Profile> of calculated local and global gamma values
            return gammaList;
        }

        /// <summary>
        /// CalculateFWHM calculates and returns the Full Width at Half Maximum (FWHM) of the provided profile.
        /// </summary>
        /// <param name="profile">List of Profile objects (see class above) for which the FWHM will be calculated</param>
        /// <returns>tupledouble containing the FWHM value, or zero if it was not found</returns>
        /// <returns>VVector containing the position halfway between the left and right FWHM</returns>
        public static (double, VVector) CalculateFWHM(List<Profile> profile)
        {
            // Initialize temporary and return variables
            double thresh = 0;
            double fwhm = 0;
            VVector center = new VVector(0, 0, 0);

            // Loop through and find the maximum value in the profile (in case the profile was not normalized)
            foreach (Profile point in profile)
            {
                if (point.Value > thresh)
                {
                    thresh = point.Value;
                }
            }

            // Set threshold to half of the maximum value
            thresh /= 2;

            // Loop through the profile again, this time using the indexes since we use neighboring points. Note that we 
            // start at i = 1 since we use the points [i] and [i-1] in each loop 
            for (int i = 1; i < profile.Count; i++)
            {
                // If the points [i] and [i-1] are valid and on either side of the threshold, we've found the "left" side of the profile
                if (profile[i - 1].Value == profile[i - 1].Value && profile[i].Value == profile[i].Value && 
                    Math.Sign(profile[i - 1].Value - thresh) != Math.Sign(profile[i].Value - thresh))
                {
                    // Now start looking for the other side of the profile, starting one over from where we found the left side
                    for (int j = i + 2; j < profile.Count - 1; j++)
                    {
                        // If the points [j] and [j+1] are on either side of the threshold, we've found the "right" side
                        if (profile[i + 1].Value == profile[i + 1].Value && profile[i].Value == profile[i].Value && 
                            Math.Sign(profile[j].Value - thresh) != Math.Sign(profile[j + 1].Value - thresh))
                        {
                            // Calculate the FWHM as the difference between the points [i] and [j], adding the interpolated distance 
                            // from each point to the threshold (using similar triangles) on each side
                            fwhm = (profile[j].Position - profile[i].Position).Length + 
                                (profile[i - 1].Position - profile[i].Position).Length * (1 - Math.Abs((thresh - 
                                Math.Min(profile[i - 1].Value, profile[i].Value)) / (profile[i - 1].Value - profile[i].Value))) +
                                (profile[j].Position - profile[j + 1].Position).Length * (1 - Math.Abs((thresh -
                                Math.Min(profile[j].Value, profile[j + 1].Value)) / (profile[j].Value - profile[j + 1].Value)));

                            // Calculate the center as the average between points [i] and [j] (this is used for the central 80% metric)
                            center = (profile[j].Position + profile[i].Position) / 2;

                            // Exit the [j] loop, as the FWHM was found
                            break;
                        }
                    }

                    // Exit the [i] loop, as the FWHM was found
                    break;
                }
            }

            // Return the calculated FWHM and center
            return ( fwhm, center );
        }
    }
}