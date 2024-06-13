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
        window.Height = 400;

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
            const Int32 BufferSize = 128;
            
            Regex r = new Regex(@"\t([\d\.]+)\t([\d\.]+)\t([\d\.]+)\t([\d\.]+)");
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

            return parsedList;
        }

        public static double[] CalculateGamma(List<Profile> profile1, List<Profile> profile2, double abs, double dta, double threshold)
        {
            // Initialize results and return arrays
            double[] results = new double[profile1.Count()];
            double[] returnArray = new double[3];
            double gamma = 0;
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
                results[i] = 1000;

                // Loop through second array
                for (int j = 0; j < profile2.Count(); j++)
                {

                    // Calculate Gamma-squared
                    gamma = Math.Pow(profile1[i].Value - profile2[j].Value, 2) / abs + (profile1[i].Position - profile2[j].Position).LengthSquared / (dta * dta);

                    // Update minimum Gamma-squared
                    if (gamma < results[i])
                    {
                        results[i] = gamma;
                    }

                    // If a gamma of zero is found, skip ahead to next point (this is meant to speed things up)
                    if (gamma < 0.01)
                    {
                        break;
                    }
                }

                // Apply sqaure root to Gamma
                results[i] = Math.Sqrt(results[i]);
            }

            // Calculate gamma statistics
            returnArray[0] = 0;
            returnArray[1] = 0;
            returnArray[2] = 0;
            for (int i = 0; i < results.Length; i++)
            {
                if (profile1[i].Value < threshold)
                {
                    continue;
                }

                if (results[i] < 1) 
                {
                    returnArray[0]++;
                }
                returnArray[1] = returnArray[1] + results[i];
                if (returnArray[2] < results[i])
                {
                    returnArray[2] = results[i];
                }
            }
            returnArray[0] = returnArray[0] / (results.Length - excluded) * 100;
            returnArray[1] = returnArray[1] / (results.Length - excluded);

            return returnArray;
        }
  }
}
