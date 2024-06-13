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

        

        public static double[,] ParseSNCTXT(string fileName)
        {
            if (fileName == null)
            {
                throw new ArgumentNullException("Provided file input must not be empty");
            }

            // Initialize list to store parsed lines
            List<double[]> parsedList = new List<double[]>();

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
                        double[] nextRow = new double[4];
                        nextRow[0] = x*10;
                        nextRow[1] = z*10;
                        nextRow[2] = y*10;
                        nextRow[3] = d;
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

            // Initialize return array
            double[,] returnArray = new double[4, matchCount];

            // Loop through list, coverting to array
            for (int i = 0; i < matchCount; i++)
            {
                returnArray[0, i] = parsedList[i][0];
                returnArray[1, i] = parsedList[i][1];
                returnArray[2, i] = parsedList[i][2];
                returnArray[3, i] = parsedList[i][3];
            }

            return returnArray;
        }

        public static double[] CalculateGamma(double[,] arr1, double[,] arr2, double abs, double dta, double threshold)
        {
            // Initialize results and return arrays
            double[] results = new double[arr1.GetLength(1)];
            double[] returnArray = new double[3];
            VVector distance = new VVector();
            double gamma = 0;
            double excluded = 0;

            // Loop through first array
            for (int i = 0; i < arr1.GetLength(1); i++)
            {
                // Exclude values below threshold
                if (arr1[3, i] < threshold)
                {
                    excluded++;
                    continue;
                }

                // Start at a default Gamma value of 1000 (arbitrary, used to determine min value)
                results[i] = 1000;

                // Loop through second array
                for (int j = 0; j < arr2.GetLength(1); j++)
                {
                    // Calculate distance vector
                    distance = new VVector(arr1[0, i], arr1[1, i], arr1[2, i]) - new VVector(arr2[0, j], arr2[1, j], arr2[2, j]);

                    // Calculate Gamma-squared
                    gamma = (arr1[3, i] - arr2[3, j]) * (arr1[3, i] - arr2[3, j]) / abs + distance.LengthSquared / (dta * dta);

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
                if (arr1[3, i] < threshold)
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
