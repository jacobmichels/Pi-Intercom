using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using pi_intercom.State;

namespace pi_intercom.Hubs
{
    public class AudioUploadHub : Hub
    {
        private ILogger _logger;
        private IPlaybackState _state;
        public AudioUploadHub(ILogger<AudioUploadHub> logger, IPlaybackState state)
        {
            _logger = logger;
            _state = state;
        }

        public bool RequestAccess()
        {
            _logger.LogInformation("userid" + Context.ConnectionId);
            if (_state.InUse)
            {
                return false;
            }
            _state.InUse = true;
            _state.CurrentUser = Context.ConnectionId;
            return true;
        }

        public async Task AudioStream(IAsyncEnumerable<string> audioStream)
        {
            _logger.LogInformation("New audio stream started.");
            _logger.LogInformation("userid" + Context.ConnectionId);
            if (_state.InUse && _state.CurrentUser!=Context.ConnectionId)
            {
                _logger.LogInformation("Cannot play new stream, device already in use.");
                return;
            }
            try
            {
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
                        _logger.LogError("Node speaker process failed to start.");
                        return;
                    }
                    _logger.LogInformation("Node speaker process started. Receiving data.");
                    var stdin = speakerProc.StandardInput;
                    byte[] raw;
                    await foreach (var chunk in audioStream)
                    {
                        raw = Convert.FromBase64String(chunk);
                        stdin.BaseStream.Write(raw, 0, raw.Length);
                    }
                    _logger.LogInformation("Audio stream complete. Killing node speaker process.");
                    speakerProc.Kill();
                    _logger.LogInformation(speakerProc.StandardOutput.ReadToEnd());
                    _logger.LogInformation(speakerProc.StandardError.ReadToEnd());
                }
                _logger.LogInformation("Process disposed");
                _state.InUse = false;
                _state.CurrentUser = string.Empty;
            }
            catch (Exception e)
            {
                _logger.LogError("Exception caught in AudioUploadHub: " + e.Message);
                throw;
            }

        }
    }
}
