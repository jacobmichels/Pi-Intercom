import * as React from 'react';
import { connect } from 'react-redux';
import { Button } from 'reactstrap';
import * as signalR from "@microsoft/signalr";

let connection: signalR.HubConnection;
let subject: signalR.Subject<string>;
let audioCtx: AudioContext;
let globalStream: MediaStream;

async function InitializeSignalr() {
    connection = new signalR.HubConnectionBuilder().withUrl("/streaminghub").configureLogging(signalR.LogLevel.Debug).build();
    await connection.start().then(function () {
        console.log("connected");
    }).catch(function () {
        console.log("problem with signalr connection");
    });
};

InitializeSignalr();

function StartRecording() {
    console.log("beginning recording");
    subject = new signalR.Subject();
    connection.send("AudioStream", subject);
    navigator.mediaDevices.getUserMedia({ audio: true })
        .then(stream => {
            audioCtx = new AudioContext();
            audioCtx.createMediaStreamSource(stream);
            audioCtx.onstatechange = (state) => { console.log(state); }

            var scriptNode = audioCtx.createScriptProcessor(4096, 1, 1);
            scriptNode.onaudioprocess = (audioProcessingEvent) => {
                //console.log("processing audio");
                // The input buffer is the song we loaded earlier
                var inputBuffer = audioProcessingEvent.inputBuffer;
                //console.log(inputBuffer.sampleRate);
                // Loop through the output channels (in this case there is only one)
                for (var channel = 0; channel < inputBuffer.numberOfChannels; channel++) {

                    // console.log("inputBuffer:" + audioProcessingEvent.inputBuffer.getChannelData(channel));
                    var chunk = audioProcessingEvent.inputBuffer.getChannelData(channel);
                    //because  endianness does matter
                    // console.log();
                    let arrayBuffer = convertFloat32ToInt16(chunk);
                    let base64 = arrayBufferToBase64(arrayBuffer);
                    // console.log(typeof(base64));



                     subject.next(base64);
                    // connection.invoke("UploadChunk", base64).catch(err => console.error(err));
                    // return this.convertFloat32ToInt16(chunk);
                }
            }
            var source = audioCtx.createMediaStreamSource(stream);
            source.connect(scriptNode);
            scriptNode.connect(audioCtx.destination);


            globalStream = stream;
        })
        .catch(function (e) {
            console.error('getUserMedia() error: ' + e.message);
        });
}

function StopRecording() {
    console.log("stopping recording");
    globalStream.getAudioTracks().forEach(track => track.stop());
    globalStream.getVideoTracks().forEach(track => track.stop());
    audioCtx.close();
    subject.complete();

}

function convertFloat32ToInt16(buffer: Float32Array) {
    let l = buffer.length;
    let buf = new Int16Array(l);
    while (l--) {
        buf[l] = Math.min(1, buffer[l]) * 0x7FFF;
    }
    return buf.buffer;
}

function arrayBufferToBase64(buffer: ArrayBuffer) {
    var binary = '';
    var bytes = new Uint8Array(buffer);
    var len = bytes.byteLength;
    for (var i = 0; i < len; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    return window.btoa(binary);
}

const Home = () => (
    <div>
        <h1>Hello, world!</h1>
        <p>Welcome to your new single-page application, built with:</p>
        <ul>
            <li><a href='https://get.asp.net/'>ASP.NET Core</a> and <a href='https://msdn.microsoft.com/en-us/library/67ef8sbd.aspx'>C#</a> for cross-platform server-side code</li>
            <li><a href='https://facebook.github.io/react/'>React</a> and <a href='https://redux.js.org/'>Redux</a> for client-side code</li>
            <li><a href='http://getbootstrap.com/'>Bootstrap</a> for layout and styling</li>
        </ul>
        <p>To help you get started, we've also set up:</p>
        <ul>
            <li><strong>Client-side navigation</strong>. For example, click <em>Counter</em> then <em>Back</em> to return here.</li>
            <li><strong>Development server integration</strong>. In development mode, the development server from <code>create-react-app</code> runs in the background automatically, so your client-side resources are dynamically built on demand and the page refreshes when you modify any file.</li>
            <li><strong>Efficient production builds</strong>. In production mode, development-time features are disabled, and your <code>dotnet publish</code> configuration produces minified, efficiently bundled JavaScript files.</li>
        </ul>
        <p>The <code>ClientApp</code> subdirectory is a standard React application based on the <code>create-react-app</code> template. If you open a command prompt in that directory, you can run <code>npm</code> commands such as <code>npm test</code> or <code>npm install</code>.</p>
        <Button onClick={StartRecording}>Start Recording</Button>
        <Button onClick={StopRecording}>Stop Recording</Button>
    </div>
);

export default connect()(Home);
