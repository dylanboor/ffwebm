using System;
using System.Diagnostics;

public class ffwebm
{
	string inFile, bitRate, aBitRate, startT, finishT, outFile;
    bool validInput, useSS;
    int argLen;
	
	public ffwebm(string[] args) {
		// Initialize bools as false
		validInput = false;
		useSS = false;
		
        argLen = args.Length;
        SetArgs(args);
	}
	
    static void Main(string[] args) {
		/*  // Output the args
		foreach (string s in args) {
            System.Console.WriteLine(s);
        }
		*/
        ffwebm webmObj = new ffwebm(args);
        webmObj.Transcode();
    }
   
    private void SetArgs(string[] args) {
		if (argLen != 0) {
            try {
				if (args[0].Equals("-ss")) {
					useSS = true;
					
					inFile = args[1];
					bitRate = args[2];
					aBitRate = args[3];
					startT = args[4];
					finishT = args[5];
						
					if (argLen == 6) {
						validInput = true;
						outFile = args[1] + ".webm";
					}
					else if (argLen == 7) {
						validInput = true;	
						outFile = args[6] + ".webm";
					}
					else {
						PrintArgError();
					}
					
				}
				else {  // If no -ss and re-encoding whole file
					inFile = args[0];
					bitRate = args[1];
					aBitRate = args[2];
						
					if (argLen == 3) {
						validInput = true;
						outFile = args[0] + ".webm";
					}
					else if (argLen == 4) {
						validInput = true;	
						outFile = args[3] + ".webm";
					}
					else {
						PrintArgError();
					}
				}
            } catch (Exception e) {
                Console.WriteLine(e.Message);
                Console.WriteLine("There was an error binding the arguments as strings.");
            }
        } else {
            PrintArgError();
        }
	}

	private void PrintArgError() {
		Console.WriteLine("ffwebm error - invalid arguments.");
		Console.WriteLine("Format is: ffwebm -ss inputfile bitrate audiobitrate start finish [outputfile]");
        Console.WriteLine("       or: ffwebm inputfile bitrate audiobitrate [outputfile]");
	}

	public void Transcode() {
        if (!validInput) {
            Console.WriteLine("Input was invalid, exiting.");
            Environment.Exit(1);
        } else {
            string ffmpegArgs;

            if (!useSS) {	// If encoding entire file duration
                // Can't estimate because can't get file duration
                ffmpegArgs = "-i \"" + inFile + "\" -c:v libvpx-vp9 -crf 0 -b:v " + bitRate + " -c:a libopus -b:a " + aBitRate + " -preset ultrafast -copyts -sn -y -threads 4 -metadata title=\"encoded@" + bitRate + "," + aBitRate + ",crf0\" -f webm \"" + outFile + "\"";
            } else {
                //EstimateFor8MB();
				ffmpegArgs = "-ss " + startT + " -i \"" + inFile + "\" -to " + finishT + " -c:v libvpx -crf 0 -b:v " + bitRate + " -c:a libopus -b:a " + aBitRate + " -preset ultrafast -copyts -start_at_zero -sn -y -threads 4 -metadata title=\"encoded@" + bitRate + "," + aBitRate + ",crf0 " + startT + " to " + finishT + " from " + inFile + "\" -f webm \"" + outFile + "\"";
            }

            var p = new Process();
            p.StartInfo = new ProcessStartInfo(@"ffmpeg.exe", ffmpegArgs) {
                UseShellExecute = true     //Used to run in same cmd prompt
            };
            p.Start();
            p.WaitForExit();
			
			if (useSS) { TimestampFix(); }
        }
    }
	
	public void TimestampFix() {
		string ffmpegArgs;
		ffmpegArgs = "-i \"" + outFile + "\" -c copy -fflags +genpts -y \"/tmp/" + outFile + "\"";
		
		var p = new Process();
            p.StartInfo = new ProcessStartInfo(@"ffmpeg.exe", ffmpegArgs) {
                UseShellExecute = true     //Used to run in same cmd prompt
            };
        p.Start();
		p.WaitForExit();
		
		string newPath = "\\tmp\\" + outFile;
		string originalPath = ".\\" + outFile;

		if (System.IO.File.Exists(@originalPath)) {
			try {
                System.IO.File.Delete(@originalPath);
            }
            catch (System.IO.IOException e) {
                Console.WriteLine(e.Message);
                return;
            }
		}

		System.IO.File.Move(@newPath, @originalPath);
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