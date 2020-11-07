using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace pi_intercom.Hubs
{
    public class AudioUploadHub : Hub
    {
        public async Task AudioStream(IAsyncEnumerable<string> audioStream)
        {
            try
            {
                Console.WriteLine("Client stream started");
                using (Process speakerProc = new Process())
                {
                    speakerProc.StartInfo.FileName = "node";
                    speakerProc.StartInfo.UseShellExecute = false;
                    speakerProc.StartInfo.RedirectStandardInput = true;
                    speakerProc.StartInfo.RedirectStandardOutput = true;
                    speakerProc.StartInfo.RedirectStandardError = true;
                    speakerProc.StartInfo.Arguments = "node-speaker/index.js";
                    bool started = speakerProc.Start();
                    if (!started)
                    {
                        Console.WriteLine("PROCESS NOT STARTED");
                    }
                    var stdin = speakerProc.StandardInput;
                    byte[] raw;
                    await foreach (var chunk in audioStream)
                    {
                        raw = Convert.FromBase64String(chunk);
                        stdin.BaseStream.Write(raw, 0, raw.Length);
                        //stdin.BaseStream.Flush();
                        //Console.WriteLine("chunk received");
                    }
                    Console.WriteLine("Done");
                    speakerProc.Kill();
                    Console.WriteLine(speakerProc.StandardOutput.ReadToEnd());
                    Console.WriteLine(speakerProc.StandardError.ReadToEnd());
                }
                Console.WriteLine("Client stream ended");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception caught in AudioUploadHub: "+e.Message);
                throw;
            }

        }
    }
}
