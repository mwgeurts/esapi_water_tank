# ESAPI Water Tank Comparison plugin

by Mark Geurts <mark.w.geurts@gmail.com>
<br>Copyright &copy; 2024, Aspirus Health

## Description

`ProfileComparison.easpi.dll` is a standalone ESAPI plugin that allows users to load Sun Nuclear water tank (.snctxt) profiles and compare to the calculated dose of the selected plan using a gamma evaluation. By calculating the dose to a water phantom in the treatment planning system and comparing the results to measured water tank profiles, this tool allows users to quickly evaluate the accuracy of their treatment planning system without the need to export each dose volume to DICOM. This type of evaluation is recommended as part of the validation of treatment planning system algorithms during commissioning in AAPM MPPG 5: 

Geurts MW, Jacqmin DJ, Jones LE, Kry SF, Mihailidis DN, Ohrt JD, Ritter T, Smilowitz JB, Wingreen NE. [AAPM MEDICAL PHYSICS PRACTICE GUIDELINE 5.b: Commissioning and QA of treatment planning dose calculations-Megavoltage photon and electron beams](https://doi.org/10.1002/acm2.13641). J Appl Clin Med Phys. 2022 Sep;23(9):e13641. doi: 10.1002/acm2.13641. Epub 2022 Aug 10. PMID: 35950259; PMCID: PMC9512346.

## Installation

To install this plugin, download a release and copy the .dll into the `PublishedScripts` folder of the Varian file server, then if required, register the script under Script Approvals in Eclipse. Alternatively, download the code from this repository and compile it yourself using Visual Studio.

## Usage and Documentation

1. Open a non-clinical patient plan in External Beam Planning that contains dose calculated on a water phantom.
2. Set the User Origin of the plan to the origin of the water tank coordinate system (where the home position of the water tank was set).
3. Select Tools > Scripts, then choose ProfileComparison.easpi.dll from the list
4. On the UI window that appears, click Browse and select the water tank .snctxt file to load. Note that the current version of this tool only supports files that contain one profile.
5. You may optionally choose to apply a convolution filter to the TPS data to account for the detector size. The tool will apply a truncated Gaussian filter based on the parameters you specify.
6. Click Calculate. The results will be displayed on the user interface, and the profiles will be plotted (water tank in red, TPS in blue, global gamma in gray).
 
## License

Released under the GNU GPL v3.0 License for evaluating and testing purposes only. This tool should NOT be used to evaluate clinical plans or make decisions that impact patient care. See the [LICENSE](LICENSE) file for further details.
