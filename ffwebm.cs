using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

public class ffwebm
{
	string inFile, bitRate, aBitRate, startT, finishT, outFile, scaleRatio, qualityCoef;
    bool validInput, useSS, scaling, usingCRF;
	
	public ffwebm(string[] args) {
		// Initialize bools as false
		validInput = false;
		useSS = false;
        scaling = false;
        usingCRF = false;
        SetArgs(args);
	}
	
    static void Main(string[] args) {
        Console.WriteLine("\nFormat is: ffwebm inputfile bitrate audiobitrate [-vf scale=XPixels:-1] [-ss start finish] [outputfile]\n");
        ffwebm webmObj = new ffwebm(args);
        webmObj.Transcode();
    }
    
    private void SetArgs(string[] args) {
        List<string> argList = removeOptionalFlags(args);
        int argLen = argList.Count;
		if (3 <= argLen && argLen <= 4) { // 3 or 4
            try {
                inFile = argList[0].Replace("[", "`[").Replace("]", "`]");
                bitRate = argList[1];
                aBitRate = argList[2];
                    
                if (argLen == 3) {
                    validInput = true;
                    outFile = inFile + ".webm";
                }
                else if (argLen == 4) {
                    validInput = true;	
                    outFile = argList[3].Replace("[", "`[").Replace("]", "`]") + ".webm";
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine("There was an error binding the arguments as strings.");
            }
        } else {
            PrintArgError();
        }
	}

	
    private List<string> removeOptionalFlags(string[] args) {
        // Handle optional flags, removing related args
        List<string> argList = new List<string>();
        foreach(string element in args) {
            argList.Add(element);
        }

        int i = 0;
        while (i < argList.Count) {
            switch(argList[i]) {
                case "-ss":
                    useSS = true;
                    startT = argList[i+1];
                    finishT = argList[i+2];

                    argList.Remove(argList[i]);
                    argList.Remove(argList[i]);
                    argList.Remove(argList[i]);
                    break;
                case "-vf":
                    scaling = true;
                    scaleRatio = argList[i+1];
                    argList.Remove(argList[i]);
                    argList.Remove(argList[i]);
                    break;
                case "-crf":
                    usingCRF = true;
                    qualityCoef = argList[i+1];
                    argList.Remove(argList[i]);
                    argList.Remove(argList[i]);
                    break;
                default:
                    i++;
                    break;
            }
        }
        return argList;
    }

    private string setFFMPEGInput() {
        List<string> ffmpegArgsList = new List<string>();
        if (useSS) {
            ffmpegArgsList.Add("-ss " + startT + " -i \"" + inFile + "\" -to " + finishT + " -metadata title=\"encoded@" + bitRate + "," + aBitRate + ", " + startT + " to " + finishT + " from " + inFile + "\"");
        } else {
            ffmpegArgsList.Add("-i \"" + inFile + "\" -metadata title=\"encoded@" + bitRate + "," + aBitRate + " from " + inFile + "\"");
        }
        if (usingCRF) {
            ffmpegArgsList.Add("-crf " + qualityCoef);
        } else {
            ffmpegArgsList.Add("-crf 4 -qmin 0 -qmax 30");
        }

        ffmpegArgsList.Add("-c:v libvpx -b:v " + bitRate + " -c:a libopus -b:a " + aBitRate + " -preset ultrafast -copyts -start_at_zero -sn -y -threads 4");

        if (scaling) {
            ffmpegArgsList.Add("-vf " + scaleRatio);
        }

        ffmpegArgsList.Add("-f webm \"" + outFile + "\"");

        string retVal = "";
        foreach(string entry in ffmpegArgsList) {
            retVal+="" + entry + " ";
        }
        return retVal;
    }
    private void PrintArgError() {
		Console.WriteLine("ffwebm error - invalid arguments.");
		Console.WriteLine("Format is: ffwebm inputfile bitrate audiobitrate [-vf scale=XPixels:-1] [-ss start finish] [outputfile]");
	}

	public void Transcode() {
        if (!validInput) {
            Console.WriteLine("Input was invalid, exiting.");
            Environment.Exit(1);
        } else {
            string ffmpegInput = setFFMPEGInput();
            try
            {
                var p = new Process();
                Console.WriteLine("ffmpeg " + ffmpegInput);
                p.StartInfo = new ProcessStartInfo(@"ffmpeg.exe", ffmpegInput)
                {
                    UseShellExecute = false     //Used to run in same cmd prompt
                };
                p.Start();
                p.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine("ffmpeg process threw an exception: " + ex.Message);
            }
            
			if (useSS) {
				try {
					//TimestampFix();
				}
				catch (Exception ex) {
					Console.WriteLine("TimestampFix method has thrown an exception!\n"+ ex.Message);
				}
			}
        }
    }
	
	public void TimestampFix() {
		string ffmpegArgs;
        ffmpegArgs = "-i \"" + outFile + "\" -c copy -fflags +genpts -y \"/tmp/" + outFile + "\"";

        try
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo(@"ffmpeg.exe", ffmpegArgs)
            {
                UseShellExecute = true     //Used to run in same cmd prompt
            };
            p.Start();
            p.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine("TimestampFix ffmpeg process has thrown an exception: " + ex.Message);
        }

        string newPath = "\\tmp\\" + outFile;
		string originalPath = ".\\" + outFile;
	}
	
    private void EstimateFor8MB() {
        string[] sub1 = startT.Split(':');
        string[] sub2 = finishT.Split(':');
        // Trim .000's from seconds value
        sub1[sub1.Length - 1] = sub1[sub1.Length - 1].Split('.')[0];
        sub2[sub2.Length - 1] = sub2[sub2.Length - 1].Split('.')[0];

        double[] dbl1 = new double[sub1.Length];
        double[] dbl2 = new double[sub2.Length];

        for (int y = 0; y < sub1.Length; y++) {
            dbl1[y] = Convert.ToDouble(sub1[y]);
            dbl2[y] = Convert.ToDouble(sub2[y]);
        }

        double time1 = dbl1[0]*3600 + dbl1[1]*60.0 + dbl1[2];
        double time2 = dbl2[0]*3600 + dbl2[1]*60.0 + dbl2[2];
        double est = (8192*8) / (time2-time1) - Convert.ToDouble(aBitRate.Split('k')[0]) - 50;


        // do something for crf?
        Console.WriteLine("\nEstimated bitrate for 8MB is: " + est + "k\n");

        if (Convert.ToDouble(bitRate.Split('k')[0]) > est) {
            Console.WriteLine("Your file is estimated to be over 8MB, do you wish to continue? y/n");
            string inputVal = Console.ReadLine();
            while (!inputVal.Equals("y")) {
                if (inputVal.Equals("n")) {
                    Environment.Exit(1);
                }
                inputVal = Console.ReadLine();
            }
        }
    }
}