using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using System.Windows.Shapes;
using System.CodeDom;
using System.IO;
using System.Text.RegularExpressions;

namespace VMS.TPS
{

    public class Profile
    {
        public VVector Position;
        public double Value;
    }

    public class Script
  {

        

        public Script()
    {
    }

        

        [MethodImpl(MethodImplOptions.NoInlining)]
    public void Execute(ScriptContext context, System.Windows.Window window/*, ScriptEnvironment environment*/)
    {
        // Launch a new user interface defined by FileBrowser.xaml
        var userInterface = new ProfileComparison.FileBrowser();
        window.Title = "Water Tank Profile Comparison Tool";
        window.Content = userInterface;
        window.Width = 650;
        window.Height = 450;

        // Pass the current patient context to the UI
        userInterface.context = context;
    }
       


        public static List<Profile> ParseSNCTXT(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("Provided file input must not be empty");
            }

            // Initialize list to store parsed lines
            List<Profile> parsedList = new List<Profile>();

            int matchCount = 0;
            double maxval = 0;
            const Int32 BufferSize = 128;
            
            Regex r = new Regex(@"\t([-\d\.]+)\t([-\d\.]+)\t([-\d\.]+)\t([\d\.]+)");
            using (var fileStream = File.OpenRead(fileName))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                String line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    Match m = r.Match(line);
                    if (m.Success)
                    {
                        matchCount++;

                        Double.TryParse(m.Groups[1].Value, out double x);
                        Double.TryParse(m.Groups[2].Value, out double y);
                        Double.TryParse(m.Groups[3].Value, out double z);
                        Double.TryParse(m.Groups[4].Value, out double d);

                        // Keep track of profile maximum
                        if (d > maxval)
                        {
                            maxval = d;
                        }

                        // Flip Y and Z axes to match Eclipse coordinate system
                        Profile nextRow = new Profile();
                        nextRow.Position = new VVector(x * 10, z * 10, y * 10);
                        nextRow.Value = d;
                        parsedList.Add(nextRow);
                    }

                    // Otherwise, if a dose table has already been parsed, stop (only parse one profile
                    // from a multi-profile text file)
                    else if (matchCount > 0)
                    {
                        break;
                    }
                }
            }

            // Normalize profile to 100%
            foreach (Profile point in parsedList)
            {
                point.Value = point.Value / maxval * 100;
            }

            return parsedList;
        }

        public static double[,] CalculateGamma(List<Profile> profile1, List<Profile> profile2, double abs, double dta, double threshold)
        {
            // Initialize results and return arrays
            double[,] results = new double[2,profile1.Count()];
            double[,] returnArray = new double[2,3];
            double local = 0;
            double global = 0;
            double excluded = 0;

            // Loop through first array
            for (int i = 0; i < profile1.Count(); i++)
            {
                // Exclude values below threshold
                if (profile1[i].Value < threshold)
                {
                    excluded++;
                    continue;
                }

                // Start at a default Gamma value of 1000 (arbitrary, used to determine min value)
                results[0,i] = 1000;
                results[1, i] = 1000;

                // Loop through second array
                for (int j = 0; j < profile2.Count(); j++)
                {

                    // Calculate local Gamma-squared
                    local = Math.Pow((profile1[i].Value - profile2[j].Value) / (profile1[i].Value * abs / 100), 2) + 
                        (profile1[i].Position - profile2[j].Position).LengthSquared / Math.Pow(dta, 2);

                    // Calculate global Gamma-squared
                    global = Math.Pow((profile1[i].Value - profile2[j].Value) / abs, 2) +
                        (profile1[i].Position - profile2[j].Position).LengthSquared / Math.Pow(dta, 2);

                    // Update minimum Gamma-squared
                    if (local < results[0, i])
                    {
                        results[0,i] = local;
                    }
                    if (global < results[1, i])
                    {
                        results[1,i] = global;
                    }

                    // If a gamma of zero is found, skip ahead to next point (this is meant to speed things up)
                    if (local < 0.01)
                    {
                        break;
                    }
                }

                // Apply sqaure root to Gamma
                results[0, i] = Math.Sqrt(results[0, i]);
                results[1, i] = Math.Sqrt(results[1, i]);
            }

            // Calculate gamma statistics
            returnArray[0, 0] = 0;
            returnArray[0, 1] = 0;
            returnArray[0, 2] = 0;
            returnArray[1, 0] = 0;
            returnArray[1, 1] = 0;
            returnArray[1, 2] = 0;
            for (int i = 0; i < results.GetLength(1); i++)
            {
                if (profile1[i].Value < threshold)
                {
                    continue;
                }

                if (results[0, i] <= 1) 
                {
                    returnArray[0, 0]++;
                }
                if (results[1, i] <= 1)
                {
                    returnArray[1, 0]++;
                }
                returnArray[0, 1] = returnArray[0, 1] + results[0, i];
                returnArray[1, 1] = returnArray[1, 1] + results[1, i];
                if (returnArray[0, 2] < results[0, i])
                {
                    returnArray[0, 2] = results[0, i];
                }
                if (returnArray[1, 2] < results[1, i])
                {
                    returnArray[1, 2] = results[1, i];
                }
            }
            returnArray[0, 0] = returnArray[0, 0] / (results.GetLength(1) - excluded) * 100;
            returnArray[1, 0] = returnArray[1, 0] / (results.GetLength(1) - excluded) * 100;
            returnArray[0, 1] = returnArray[0, 1] / (results.GetLength(1) - excluded);
            returnArray[1, 1] = returnArray[1, 1] / (results.GetLength(1) - excluded);

            return returnArray;
        }

        public static double CalculateFWHM(List<Profile> profile)
        {
            // Initialize temporary and return variables
            double thresh = 0;
            double fwhm = 0;

            foreach (Profile point in profile)
            {
                if (point.Value > thresh)
                {
                    thresh = point.Value;
                }
            }

            // Set threshold to half max
            thresh /= 2;

            for (int i = 1; i < profile.Count; i++)
            {
                if (Math.Sign(profile[i - 1].Value - thresh) != Math.Sign(profile[i].Value - thresh))
                {
                    for (int j = i + 2; j < profile.Count - 1; j++) 
                    {
                        if (Math.Sign(profile[j].Value - thresh) != Math.Sign(profile[j + 1].Value - thresh))
                        {
                            fwhm = (profile[j].Position - profile[i].Position).Length;
                            break;
                        }

                    }
                    break;
                }
            }
            return fwhm;
        }
  }
}