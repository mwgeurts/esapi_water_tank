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
using ProfileComparison;

namespace VMS.TPS
{

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
    public void Execute(ScriptContext context, System.Windows.Window window/*, ScriptEnvironment environment*/)
    {
        // Launch a new user interface defined by FileBrowser.xaml
        var userInterface = new ProfileComparison.FileBrowser();
        window.Title = "Water Tank Profile Comparison Tool";
        window.Content = userInterface;
        window.Width = 1060;
        window.Height = 500;

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

        public static List<Profile> CalculateGamma(List<Profile> profile1, List<Profile> profile2, double abs, double dta, double threshold)
        {
            // Initialize results object and temporary variables
            List<Profile> gammaList = new List<Profile>();
            double local;
            double global;
            double maxlocal;
            double maxglobal;

            // Loop through first array
            for (int i = 0; i < profile1.Count(); i++)
            {
                // Exclude values below threshold
                if (profile1[i].Value < threshold)
                {
                    continue;
                }

                // Start at a default Gamma value of 1000 (arbitrary, used to determine min value)
                maxlocal = 1000;
                maxglobal = 1000;

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
                    if (local < maxlocal)
                    {
                        maxlocal = local;
                    }
                    if (global < maxglobal)
                    {
                        maxglobal = global;
                    }

                    // If a gamma of basically zero is found, skip ahead to next point (this is meant to speed things up)
                    if (local < 0.01)
                    {
                        break;
                    }
                }

                // Copy positions from first profile and apply sqaure root to return values
                Profile gamma = new Profile();
                gamma.Position = profile1[i].Position;
                gamma.Value = Math.Sqrt(maxlocal);
                gamma.Value2 = Math.Sqrt(maxglobal);

                gammaList.Add(gamma);
            }

            return gammaList;
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