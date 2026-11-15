using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;
[assembly: AssemblyTitle("ScreenMirror")]
[assembly: AssemblyDescription("AirPlay screen mirroring receiver for Windows")]
[assembly: AssemblyProduct("ScreenMirror")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

sealed class SoftButton : Control {
    float hover; bool over;
    readonly System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer();
    readonly Stopwatch time = new Stopwatch();
    public bool Accent;
    public int Glyph;
    public SoftButton() {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable | ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent; Font = new Font("Segoe UI Semibold",10.5f); ForeColor=Color.White; Cursor=Cursors.Hand; TabStop=true; AccessibleRole=AccessibleRole.PushButton;
        clock.Interval=15;
        clock.Tick+=delegate { float dt=(float)time.Elapsed.TotalSeconds;time.Restart();float target=over?1:0;hover+=(target-hover)*(1-(float)Math.Exp(-dt*18));Invalidate();if(Math.Abs(hover-target)<.005f){hover=target;clock.Stop();} };
    }
    protected override void OnResize(EventArgs e) {
        base.OnResize(e); if(Width<3||Height<3)return;
        if(Glyph!=0){Region old=Region;Region=null;if(old!=null)old.Dispose();return;}
        using(var path=Mirror.Round(new RectangleF(0,0,Width,Height),Glyph==0?12:9)){Region old=Region;Region=new Region(path);if(old!=null)old.Dispose();}
    }
    protected override void OnMouseEnter(EventArgs e){over=true;time.Restart();clock.Start();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){over=false;time.Restart();clock.Start();base.OnMouseLeave(e);}
    protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Enter||e.KeyCode==Keys.Space){OnClick(EventArgs.Empty);e.Handled=true;}base.OnKeyDown(e);}
    protected override void OnPaintBackground(PaintEventArgs e){
        if(Glyph==0)base.OnPaintBackground(e);else e.Graphics.Clear(BackColor);
    }
    void PaintGlyph(Graphics target){
        const int scale=4;
        using(var bitmap=new Bitmap(Math.Max(1,Width*scale),Math.Max(1,Height*scale))){
            using(var g=Graphics.FromImage(bitmap)){
                g.Clear(BackColor);g.ScaleTransform(scale,scale);
                g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                var box=new RectangleF(1,1,Width-2,Height-2);
                Color fill=Glyph==3&&over?Color.FromArgb(159,47,70):Color.FromArgb(25+(int)(hover*12),30+(int)(hover*12),43+(int)(hover*14));
                using(var path=Mirror.Round(box,8)){
                    using(var brush=new SolidBrush(fill))g.FillPath(brush,path);
                    if(Focused&&ShowFocusCues)using(var brush=new SolidBrush(Color.FromArgb(18,255,255,255)))g.FillPath(brush,path);
                }
                float x=Width/2f,y=Height/2f;
                using(var pen=new Pen(Color.FromArgb(225,231,245),1.15f)){
                    pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;pen.LineJoin=LineJoin.Round;
                    if(Glyph==1)g.DrawLine(pen,x-4,y+1,x+4,y+1);
                    else if(Glyph==2)g.DrawRectangle(pen,x-4,y-4,8,8);
                    else{g.DrawLine(pen,x-3.5f,y-3.5f,x+3.5f,y+3.5f);g.DrawLine(pen,x+3.5f,y-3.5f,x-3.5f,y+3.5f);}
                }
            }
            target.InterpolationMode=InterpolationMode.HighQualityBicubic;
            target.PixelOffsetMode=PixelOffsetMode.HighQuality;
            target.DrawImage(bitmap,ClientRectangle,0,0,bitmap.Width,bitmap.Height,GraphicsUnit.Pixel);
        }
    }

    protected override void OnPaint(PaintEventArgs e){
        if(Glyph!=0){PaintGlyph(e.Graphics);return;}
        Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        RectangleF box=new RectangleF(.5f,.5f,Width-1,Height-1);
        using(var path=Mirror.Round(box,Glyph==0?12:9)){
            Color a=Accent?Color.FromArgb(94+(int)(hover*20),83+(int)(hover*20),230):Color.FromArgb(29+(int)(hover*14),34+(int)(hover*14),49+(int)(hover*17));
            if(Glyph==3&&over)a=Color.FromArgb(169,53,77);
            using(var fill=new SolidBrush(a))g.FillPath(fill,path);
            using(var pen=new Pen(Focused?Color.FromArgb(126,142,216):Color.FromArgb(65,83,103,143)))g.DrawPath(pen,path);
        }
        if(Glyph!=0){
            float x=Width/2f,y=Height/2f;
            using(var pen=new Pen(Color.FromArgb(225,232,247),1.25f)){
                pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;
                if(Glyph==1)g.DrawLine(pen,x-4,y+1,x+4,y+1);
                else if(Glyph==2)g.DrawRectangle(pen,x-4,y-4,8,8);
                else{g.DrawLine(pen,x-3.5f,y-3.5f,x+3.5f,y+3.5f);g.DrawLine(pen,x+3.5f,y-3.5f,x-3.5f,y+3.5f);}
            }
        }else using(var fill=new SolidBrush(Enabled?ForeColor:Color.Gray))using(var fmt=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})g.DrawString(Text,Font,fill,ClientRectangle,fmt);
    }
    protected override void Dispose(bool disposing){if(disposing)clock.Dispose();base.Dispose(disposing);}
}
sealed class ResizeGuide : Form {
    public ResizeGuide(){
        FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;
        BackColor=Color.FromArgb(156,139,255);StartPosition=FormStartPosition.Manual;
    }
    protected override bool ShowWithoutActivation{get{return true;}}
    protected override CreateParams CreateParams{get{var p=base.CreateParams;p.ExStyle|=0x08000000|0x00000080|0x00000020;return p;}}
    protected override void OnSizeChanged(EventArgs e){
        base.OnSizeChanged(e);if(Width<5||Height<5)return;
        Region border=new Region(new Rectangle(0,0,Width,Height));border.Exclude(new Rectangle(2,2,Width-4,Height-4));
        Region old=Region;Region=border;if(old!=null)old.Dispose();
    }
    protected override void WndProc(ref Message m){if(m.Msg==0x0084){m.Result=(IntPtr)(-1);return;}base.WndProc(ref m);}
}

public static class WindowGeometry {
    public static Rectangle ResizeOutline(Rectangle start,Point anchor,Point cursor,int edge,Size minimum){
        int dx=cursor.X-anchor.X,dy=cursor.Y-anchor.Y;
        int left=start.Left,top=start.Top,right=start.Right,bottom=start.Bottom;
        int w=Math.Max(1,minimum.Width),h=Math.Max(1,minimum.Height);
        if(edge==1||edge==4||edge==7)left=Math.Min(right-w,left+dx);
        if(edge==2||edge==5||edge==8)right=Math.Max(left+w,right+dx);
        if(edge==3||edge==4||edge==5)top=Math.Min(bottom-h,top+dy);
        if(edge==6||edge==7||edge==8)bottom=Math.Max(top+h,bottom+dy);
        return Rectangle.FromLTRB(left,top,right,bottom);
    }

    public static Rectangle RelativeWorkArea(Rectangle monitor,Rectangle work){
        return new Rectangle(work.Left-monitor.Left,work.Top-monitor.Top,work.Width,work.Height);
    }
}

sealed class Mirror : Form {
    Bitmap hubCache, phoneCache;
    string hubKey="";
    bool playerMode, playerExpanded;
    bool liveSizing;
    ResizeGuide resizeGuide;
    Rectangle resizeStart,resizeTarget;
    Point resizeAnchor;
    int resizeEdge;
    bool BeginOutlineResize(int edge){
        if(!playerMode||fullscreen||WindowState!=FormWindowState.Normal||edge<1||edge>8)return false;
        FinishOutlineResize(false);
        resizeEdge=edge;resizeStart=Bounds;resizeTarget=Bounds;resizeAnchor=Cursor.Position;
        resizeGuide=new ResizeGuide();resizeGuide.Bounds=Bounds;resizeGuide.Show(this);
        Capture=true;Focus();return true;
    }
    void TrackOutlineResize(){
        if(resizeGuide==null)return;
        if((GetAsyncKeyState(0x1B)&0x8000)!=0){FinishOutlineResize(false);return;}
        resizeTarget=WindowGeometry.ResizeOutline(resizeStart,resizeAnchor,Cursor.Position,resizeEdge,MinimumSize);
        if(resizeGuide.Bounds!=resizeTarget)resizeGuide.Bounds=resizeTarget;
        if((GetAsyncKeyState(0x01)&0x8000)==0)FinishOutlineResize(true);
    }
    void FinishOutlineResize(bool apply){
        ResizeGuide guide=resizeGuide;if(guide==null)return;
        resizeGuide=null;Capture=false;
        try{if(apply&&!closing&&playerMode){Bounds=resizeTarget;LayoutUI();RepaintPlayer();}}
        finally{guide.Dispose();}
    }
    protected override void OnMouseUp(MouseEventArgs e){
        if(e.Button==MouseButtons.Left&&resizeGuide!=null){
            resizeTarget=WindowGeometry.ResizeOutline(resizeStart,resizeAnchor,Cursor.Position,resizeEdge,MinimumSize);
            FinishOutlineResize(true);
        }
        base.OnMouseUp(e);
    }
    protected override void OnMouseCaptureChanged(EventArgs e){
        base.OnMouseCaptureChanged(e);
        if(resizeGuide!=null&&!Capture){
            bool released=(GetAsyncKeyState(0x01)&0x8000)==0&&(GetAsyncKeyState(0x1B)&0x8000)==0;
            if(released)resizeTarget=WindowGeometry.ResizeOutline(resizeStart,resizeAnchor,Cursor.Position,resizeEdge,MinimumSize);
            FinishOutlineResize(released);
        }
    }

    [DllImport("user32.dll")] static extern bool RedrawWindow(IntPtr window,IntPtr area,IntPtr region,uint flags);
    bool paintingPlayer;
    void RepaintPlayer(){
        if(!playerMode||paintingPlayer||!IsHandleCreated||WindowState==FormWindowState.Minimized)return;
        paintingPlayer=true;
        try{RedrawWindow(Handle,IntPtr.Zero,IntPtr.Zero,0x0001|0x0080|0x0100);}
        finally{paintingPlayer=false;}
    }
    IntPtr fittedChild;
    Size fittedSize;
    Rectangle hubBounds;
    FormWindowState hubState;
    volatile int sourceWidth=500,sourceHeight=1080;
    int aspectRevision, appliedAspect;
    DateTime controlsUntil;
    [DllImport("user32.dll")] static extern IntPtr SendMessage(IntPtr w,int msg,IntPtr a,IntPtr b);
    [DllImport("winmm.dll")] static extern uint timeBeginPeriod(uint period);
    [DllImport("winmm.dll")] static extern uint timeEndPeriod(uint period);
    bool preciseClock;
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern IntPtr DefWindowProc(IntPtr w,int message,IntPtr wp,IntPtr lp);
    protected override CreateParams CreateParams {
        get {var cp=base.CreateParams;cp.Style|=0x00C00000|0x00080000|0x00040000|0x00020000|0x00010000|0x02000000|0x04000000;return cp;}
    }
    void RoundedWindow(){
        if(Width<5||Height<5||liveSizing)return;
        Region old=Region;
        if(WindowState==FormWindowState.Maximized||fullscreen)Region=null;
        else using(var p=Round(new RectangleF(0,0,Width,Height),playerMode?12:17))Region=new Region(p);
        if(old!=null)old.Dispose();
    }
    void PlayerLayout(){
        Color caption=playerMode?Color.FromArgb(28,31,39):Color.FromArgb(12,15,24);
        foreach(var b in new[]{min,max,close}){b.BackColor=caption;b.Visible=true;b.BringToFront();}
    }
    void SizePlayer(bool large){
        Rectangle area=Screen.FromControl(this).WorkingArea;
        double aspect=(double)Math.Max(1,sourceWidth)/Math.Max(1,sourceHeight);
        int availableW=Math.Max(160,area.Width-88), availableH=Math.Max(200,area.Height-120);
        int h=large?availableH:(int)(availableH*.88);
        int w=(int)Math.Round(h*aspect);
        if(w>availableW){w=availableW;h=(int)(w/aspect);}
        w+=24;h+=56;
        int x=Left+(Width-w)/2,y=Top+(Height-h)/2;
        x=Math.Max(area.Left,Math.Min(x,area.Right-w));y=Math.Max(area.Top,Math.Min(y,area.Bottom-h));
        Bounds=new Rectangle(x,y,w,h);
    }
    void EnterPlayer(){
        if(playerMode)return;
        hubBounds=WindowState==FormWindowState.Normal?Bounds:RestoreBounds;hubState=WindowState;
        playerMode=true;DoubleBuffered=false;MinimumSize=new Size(180,260);WindowState=FormWindowState.Normal;
        SizePlayer(false);controlsUntil=DateTime.Now.AddSeconds(2);appliedAspect=aspectRevision;LayoutUI();
    }
    void LeavePlayer(){
        if(!playerMode)return;
        video.Visible=false;if(fullscreen)ToggleFullscreen();playerMode=false;DoubleBuffered=true;playerExpanded=false;
        WindowState=FormWindowState.Normal;MinimumSize=new Size(920,610);Bounds=hubBounds;WindowState=hubState;
        hubKey="";LayoutUI();Invalidate();
    }
    void ReturnToHub(){StopReceiver();StartReceiver();}
    void Enlarge(){
        if(playerMode){ToggleFullscreen();return;}
        WindowState=WindowState==FormWindowState.Maximized?FormWindowState.Normal:FormWindowState.Maximized;
    }
    protected override void OnPaintBackground(PaintEventArgs e) { }
    protected override void OnActivated(EventArgs e){base.OnActivated(e);Invalidate();}
    protected override void OnDeactivate(EventArgs e){base.OnDeactivate(e);Invalidate();}
    void Outline(Graphics g){
        g.SmoothingMode=SmoothingMode.AntiAlias;
        RectangleF box=new RectangleF(.5f,.5f,Width-1,Height-1);
        using(var pen=new Pen(Color.FromArgb(61,67,83))){
            if(WindowState==FormWindowState.Maximized||fullscreen||liveSizing)g.DrawRectangle(pen,.5f,.5f,Width-1,Height-1);
            else using(var path=Round(box,playerMode?12:17))g.DrawPath(pen,path);
        }
    }
    void ReadGeometry(string line){
        if(line==null||line.IndexOf("geometry is",StringComparison.Ordinal)<0)return;
        var m=System.Text.RegularExpressions.Regex.Match(line,@"\(w,h\)=\((\d+),(\d+)\)\s*\(source\)");
        int w,h;
        if(m.Success&&int.TryParse(m.Groups[1].Value,out w)&&int.TryParse(m.Groups[2].Value,out h)&&w>0&&h>0){sourceWidth=w;sourceHeight=h;Interlocked.Increment(ref aspectRevision);}
    }
    protected override void OnPaint(PaintEventArgs e){
        Graphics g=e.Graphics;
        if(playerMode){
            g.Clear(Color.FromArgb(28,31,39));g.SmoothingMode=SmoothingMode.AntiAlias;
            Outline(g);
            return;
        }
        string key=ClientSize.ToString()+"|"+pin+"|"+(receiver!=null)+"|"+notice;
        if(hubCache==null||hubKey!=key){
            Bitmap old=hubCache;hubCache=new Bitmap(Math.Max(1,Width),Math.Max(1,Height));
            using(var c=Graphics.FromImage(hubCache)){c.Clear(BackColor);PaintHub(c);}
            hubKey=key;if(old!=null)old.Dispose();
        }
        g.DrawImageUnscaled(hubCache,0,0);
        if(Width>=860){
            if(phoneCache==null){phoneCache=new Bitmap(160,290);using(var c=Graphics.FromImage(phoneCache)){c.SmoothingMode=SmoothingMode.AntiAlias;DrawPhone(c,2,8,0);}}
            float t=(float)elapsed.Elapsed.TotalSeconds;
            float top=Math.Max(113,Height*.20f)+20+(float)Math.Sin(t*1.15)*7;
            g.DrawImage(phoneCache,Width-254,top,phoneCache.Width,phoneCache.Height);
        }
        Outline(g);
    }

    static readonly string RuntimeDir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"runtime");
    static readonly string Engine=Path.Combine(RuntimeDir,"bin","uxplay.exe");
    static readonly string HomeDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ScreenMirror");
    readonly string receiverName = "ScreenMirror " + Environment.MachineName;
    readonly string logFile;
    readonly object logGate = new object();
    readonly Panel video = new Panel();
    readonly SoftButton stop = new SoftButton(), logs = new SoftButton(), full = new SoftButton();
    readonly SoftButton close = new SoftButton(), min = new SoftButton(), max = new SoftButton();
    readonly System.Windows.Forms.Timer clock = new System.Windows.Forms.Timer();
    readonly Stopwatch elapsed = Stopwatch.StartNew();
    readonly Bitmap brand;
    Process receiver;
    IntPtr child;
    bool closing, fullscreen, embedFailed;
    Rectangle savedBounds;
    FormWindowState savedState;
    int pin, tick;
    DateTime attachedAt;
    string notice = "";
    float reveal = 1;

    delegate bool EnumProc(IntPtr window, IntPtr param);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc cb, IntPtr p);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr w, out uint pid);
    [DllImport("user32.dll")] static extern bool IsWindow(IntPtr w);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr w);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr w, System.Text.StringBuilder s, int n);
    [DllImport("user32.dll", SetLastError=true)] static extern IntPtr SetParent(IntPtr w, IntPtr parent);
    [DllImport("user32.dll", EntryPoint="GetWindowLongW")] static extern int GetStyle(IntPtr w, int index);
    [DllImport("user32.dll", EntryPoint="SetWindowLongW")] static extern int SetStyle(IntPtr w, int index, int value);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr w, IntPtr after, int x,int y,int width,int height,uint flags);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr w, int cmd);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int key);
    [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr w,int attr,ref int value,int size);

    [STAThread] static void Main(string[] args) {
        Directory.CreateDirectory(HomeDir);
        if(args.Length > 0 && args[0] == "--icon") { SaveIcon(args.Length>1?args[1]:Path.Combine(HomeDir,"ScreenMirror.ico")); return; }
        bool first;
        using(var mutex = new Mutex(true,@"Local\ScreenMirrorDesktop",out first)) {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            if(!first) { MessageBox.Show("ScreenMirror уже открыт.","ScreenMirror"); return; }
            Mirror window=new Mirror();
            bool smoke=args.Length>0&&args[0]=="--smoke-test";
            if(smoke){
                var gate=new System.Windows.Forms.Timer();gate.Interval=2500;
                window.Shown+=delegate{gate.Start();};
                gate.Tick+=delegate{
                    gate.Stop();Environment.ExitCode=window.receiver!=null&&!window.receiver.HasExited?0:1;
                    window.Close();gate.Dispose();
                };
            }
            Application.Run(window);
        }
    }
    public static GraphicsPath Round(RectangleF r, float radius) {
        float d = Math.Min(radius*2,Math.Min(r.Width,r.Height)); var p = new GraphicsPath();
        p.AddArc(r.Left,r.Top,d,d,180,90); p.AddArc(r.Right-d,r.Top,d,d,270,90);
        p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90); p.AddArc(r.Left,r.Bottom-d,d,d,90,90); p.CloseFigure(); return p;
    }
    static Bitmap Logo(int size) {
        Bitmap b = new Bitmap(size,size);
        using(Graphics g = Graphics.FromImage(b)) {
            g.SmoothingMode = SmoothingMode.AntiAlias; g.ScaleTransform(size/128f,size/128f);
            using(var p=Round(new RectangleF(2,2,124,124),29))
            using(var fill=new LinearGradientBrush(new Rectangle(0,0,128,128),Color.FromArgb(39,44,78),Color.FromArgb(15,18,30),55)) g.FillPath(fill,p);
            using(var pen=new Pen(Color.FromArgb(126,145,255),5))
            using(var p=Round(new RectangleF(25,32,67,47),9)) g.DrawPath(pen,p);
            using(var pen=new Pen(Color.FromArgb(94,110,180),4)) { g.DrawLine(pen,45,88,72,88); g.DrawLine(pen,58,80,58,87); }
            using(var p=Round(new RectangleF(67,48,34,58),8))
            using(var fill=new LinearGradientBrush(new Rectangle(67,48,34,58),Color.FromArgb(105,139,255),Color.FromArgb(165,90,243),60)) g.FillPath(fill,p);
            using(var pen=new Pen(Color.FromArgb(225,232,255),3)) g.DrawLine(pen,79,97,89,97);
            using(var pen=new Pen(Color.FromArgb(199,213,255),3)) { g.DrawLine(pen,100,21,100,35); g.DrawLine(pen,93,28,107,28); }
        } return b;
    }
    static void SaveIcon(string path) {
        using(Bitmap bitmap=Logo(256)) using(var png=new MemoryStream()) {
            bitmap.Save(png,System.Drawing.Imaging.ImageFormat.Png); byte[] bytes=png.ToArray();
            using(var writer=new BinaryWriter(File.Create(path))) {
                writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)1);
                writer.Write((byte)0); writer.Write((byte)0); writer.Write((byte)0); writer.Write((byte)0);
                writer.Write((ushort)1); writer.Write((ushort)32); writer.Write(bytes.Length); writer.Write(22); writer.Write(bytes);
            }
        }
    }
    Mirror() {
        logFile=Path.Combine(HomeDir,"session-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+".log");
        Text="ScreenMirror"; BackColor=Color.FromArgb(12,15,24); ForeColor=Color.FromArgb(234,239,255);
        Font=new Font("Segoe UI",10); FormBorderStyle=FormBorderStyle.None;
        ClientSize=new Size(960,650); MinimumSize=new Size(920,610); StartPosition=FormStartPosition.CenterScreen;
        SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
        brand=Logo(96);
        string ico=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"ScreenMirror.ico"); if(File.Exists(ico)) Icon=new Icon(ico);
        video.BackColor=Color.Black; video.Visible=false; Controls.Add(video);
        close.Glyph=3; min.Glyph=1; max.Glyph=2;
        close.AccessibleName="В хаб / закрыть";min.AccessibleName="Свернуть";max.AccessibleName="Развернуть / полный экран";
        foreach(var b in new[]{close,min,max}) { b.Font=new Font("Segoe UI",13); Controls.Add(b); }
        close.Click+=delegate{if(playerMode)ReturnToHub();else Close();}; min.Click+=delegate{SendMessage(Handle,0x0112,(IntPtr)0xF020,IntPtr.Zero);};
        max.Click+=delegate{Enlarge();};
        stop.Accent=true; stop.Text="Остановить"; logs.Text="Журнал"; full.Text="На весь экран  ↗";
        foreach(var b in new[]{stop,logs,full}) Controls.Add(b);
        stop.Click+=delegate{if(receiver==null) StartReceiver(); else StopReceiver();};
        logs.Click+=delegate{Process.Start("explorer.exe","\""+HomeDir+"\"");};
        full.Click+=delegate{ToggleFullscreen();};
        Shown+=delegate{StartReceiver();};
        FormClosing+=delegate(object sender,FormClosingEventArgs e){if(playerMode&&e.CloseReason==CloseReason.UserClosing){e.Cancel=true;ReturnToHub();return;}closing=true;clock.Stop();StopReceiver();};
        preciseClock=timeBeginPeriod(1)==0;clock.Interval=16;clock.Tick+=Frame;clock.Start();
        LayoutUI();
    }
    protected override void OnHandleCreated(EventArgs e) {
        base.OnHandleCreated(e);
        try{int policy=1;DwmSetWindowAttribute(Handle,2,ref policy,4);}catch{}
    }
    void LayoutUI() {
        if(close==null)return;
        int top=playerMode?8:17,right=playerMode?12:20;
        close.SetBounds(ClientSize.Width-right-30,top,30,28);max.SetBounds(ClientSize.Width-right-65,top,30,28);min.SetBounds(ClientSize.Width-right-100,top,30,28);
        stop.Visible=!playerMode;logs.Visible=!playerMode;full.Visible=false;
        stop.SetBounds(26,ClientSize.Height-65,178,42);logs.SetBounds(216,ClientSize.Height-65,115,42);
        if(playerMode)video.SetBounds(12,44,Math.Max(1,ClientSize.Width-24),Math.Max(1,ClientSize.Height-56));
        else video.SetBounds(26,94,Math.Max(1,ClientSize.Width-52),Math.Max(1,ClientSize.Height-180));
        FitVideo();RoundedWindow();PlayerLayout();
    }
    protected override void OnResize(EventArgs e) {
        base.OnResize(e); if(video!=null)LayoutUI();
        RepaintPlayer();

    }
    protected override void OnResizeBegin(EventArgs e){
        liveSizing=true;
        Region old=Region;Region=null;if(old!=null)old.Dispose();
        base.OnResizeBegin(e);
        LayoutUI();RepaintPlayer();
    }
    protected override void OnResizeEnd(EventArgs e){
        liveSizing=false;
        base.OnResizeEnd(e);
        LayoutUI();RepaintPlayer();Invalidate();
    }
    void FitVideo(){SyncVideo();}
    void SyncVideo(){
        if(child==IntPtr.Zero||!IsWindow(child)){fittedChild=IntPtr.Zero;return;}
        Size target=video.ClientSize;
        if(target.Width<1||target.Height<1)return;
        if(fittedChild==child&&fittedSize==target)return;
        if(SetWindowPos(child,IntPtr.Zero,0,0,target.Width,target.Height,0x0010|0x0004)){
            fittedChild=child;fittedSize=target;
        }
    }
    [StructLayout(LayoutKind.Sequential)] struct WinPoint {
        public int X,Y;
        public WinPoint(int x,int y){X=x;Y=y;}
    }
    [StructLayout(LayoutKind.Sequential)] struct WinRect {public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)] struct MonitorData {
        public int Size;public WinRect Monitor,Work;public uint Flags;
    }
    [StructLayout(LayoutKind.Sequential)] struct MinMaxData {
        public WinPoint Reserved,MaxSize,MaxPosition,MinTrackSize,MaxTrackSize;
    }
    [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr window,uint flags);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern bool GetMonitorInfo(IntPtr monitor,ref MonitorData data);
    static void ApplyMonitorBounds(IntPtr window,IntPtr parameter){
        if(parameter==IntPtr.Zero)return;
        IntPtr monitor=MonitorFromWindow(window,2);
        MonitorData info=new MonitorData();info.Size=Marshal.SizeOf(typeof(MonitorData));
        if(monitor==IntPtr.Zero||!GetMonitorInfo(monitor,ref info))return;
        Rectangle relative=WindowGeometry.RelativeWorkArea(
            Rectangle.FromLTRB(info.Monitor.Left,info.Monitor.Top,info.Monitor.Right,info.Monitor.Bottom),
            Rectangle.FromLTRB(info.Work.Left,info.Work.Top,info.Work.Right,info.Work.Bottom));
        MinMaxData limits=(MinMaxData)Marshal.PtrToStructure(parameter,typeof(MinMaxData));
        limits.MaxPosition=new WinPoint(relative.X,relative.Y);
        limits.MaxSize=new WinPoint(relative.Width,relative.Height);
        Marshal.StructureToPtr(limits,parameter,false);
    }

    protected override void WndProc(ref Message m) {
        if(m.Msg==0x0112&&(m.WParam.ToInt64()&0xFFF0)==0xF000&&BeginOutlineResize((int)(m.WParam.ToInt64()&15))){m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x0024){base.WndProc(ref m);ApplyMonitorBounds(m.HWnd,m.LParam);return;}
        // Suppress native non-client painting on both active and inactive windows.
        if(m.Msg==0x0083||m.Msg==0x0085||m.Msg==0x00AE||m.Msg==0x00AF){m.Result=IntPtr.Zero;return;}
        if(m.Msg==0x0086){m.Result=DefWindowProc(m.HWnd,m.Msg,m.WParam,(IntPtr)(-1));if(IsHandleCreated)Invalidate();return;}
        if(m.Msg==0x0014){m.Result=(IntPtr)1;return;}
        base.WndProc(ref m);
        if(m.Msg==0x0084 && m.Result.ToInt32()==1 && !fullscreen) {
            long xy=m.LParam.ToInt64();Point p=PointToClient(new Point((short)(xy&65535),(short)((xy>>16)&65535)));
            if(WindowState==FormWindowState.Normal){
                bool l=p.X<7,r=p.X>=ClientSize.Width-7,t=p.Y<7,b=p.Y>=ClientSize.Height-7;
                int hit=t?(l?13:r?14:12):b?(l?16:r?17:15):l?10:r?11:0;
                if(hit!=0){m.Result=(IntPtr)hit;return;}
            }
            if(p.Y<(playerMode?44:71))m.Result=(IntPtr)2;
        }
    }
    void ToggleFullscreen() {
        if(!fullscreen){savedBounds=Bounds;savedState=WindowState;WindowState=FormWindowState.Normal;fullscreen=true;Bounds=Screen.FromControl(this).Bounds;}
        else{fullscreen=false;WindowState=FormWindowState.Normal;Bounds=savedBounds;WindowState=savedState;}
        full.Text=fullscreen?"Выйти из полного экрана":"На весь экран  ↗";
    }
    void Frame(object sender,EventArgs args) {
        if(closing)return;
        TrackOutlineResize();
        if(child!=IntPtr.Zero)SyncVideo();
        if(liveSizing&&tick%2==0)RepaintPlayer();
        if(playerMode&&appliedAspect!=aspectRevision){appliedAspect=aspectRevision;if(!fullscreen&&WindowState==FormWindowState.Normal)SizePlayer(playerExpanded);}
        if(++tick%8==0){
            if(receiver!=null && receiver.HasExited){int code=receiver.ExitCode; StopReceiver();notice="Движок завершился ("+code+"). Открой журнал.";}
            if(child!=IntPtr.Zero&&!IsWindow(child)){child=IntPtr.Zero;LeavePlayer();reveal=0;}
            if(receiver!=null&&child==IntPtr.Zero&&!embedFailed) FindVideo();

        }
        reveal=Math.Min(1,reveal+.045f);
        if(WindowState!=FormWindowState.Minimized&&!playerMode)Invalidate();
    }
    void FindVideo() {
        IntPtr found=IntPtr.Zero;uint wanted=(uint)receiver.Id;
        EnumWindows(delegate(IntPtr w,IntPtr unused){
            uint pid;GetWindowThreadProcessId(w,out pid);
            if(pid!=wanted||!IsWindowVisible(w))return true;
            var title=new System.Text.StringBuilder(256);GetWindowText(w,title,256);
            if(title.ToString().IndexOf("Direct3D11",StringComparison.OrdinalIgnoreCase)>=0){found=w;return false;}return true;
        },IntPtr.Zero);
        if(found==IntPtr.Zero)return;
        int oldStyle=GetStyle(found,-16),oldExStyle=GetStyle(found,-20);
        try {
            ShowWindow(found,0);
            int style=GetStyle(found,-16);
            style=(style&~unchecked((int)0x80CF0000))|0x40000000|0x02000000|0x04000000;
            SetStyle(found,-16,style);
            SetStyle(found,-20,GetStyle(found,-20)&~0x00040000);
            SetStyle(video.Handle,-16,GetStyle(video.Handle,-16)|0x02000000|0x04000000);
            SetParent(found,video.Handle);
            // Verify actual parenting, rather than reporting a successful embed blindly.
            if(GetParent(found)!=video.Handle)throw new Exception("Windows не разрешила встроить окно видео.");
            fittedChild=IntPtr.Zero;child=found;EnterPlayer();video.Visible=true;video.BringToFront();foreach(var b in new[]{min,max,close})b.BringToFront();PlayerLayout();FitVideo();SetWindowPos(found,IntPtr.Zero,0,0,video.Width,video.Height,0x0010|0x0004|0x0020);ShowWindow(found,4);
            attachedAt=DateTime.Now;notice="";Invalidate();
        }catch(Exception e){embedFailed=true;Log(e.Message);SetParent(found,IntPtr.Zero);SetStyle(found,-16,oldStyle);SetStyle(found,-20,oldExStyle);SetWindowPos(found,IntPtr.Zero,0,0,0,0,0x0037);ShowWindow(found,4);notice="Не удалось встроить видео. Подробности в журнале.";}
    }
    [DllImport("user32.dll")] static extern IntPtr GetParent(IntPtr w);
    void Log(string line){if(line==null)return;ReadGeometry(line);lock(logGate){try{if(!File.Exists(logFile)||new FileInfo(logFile).Length<8*1024*1024)File.AppendAllText(logFile,DateTime.Now.ToString("HH:mm:ss ")+line+Environment.NewLine);}catch{}}}
    void StartReceiver() {
        if(closing||receiver!=null)return;
        try {
            if(!File.Exists(Engine))throw new FileNotFoundException("Не найден установленный движок: "+Engine);
            using(var rng=System.Security.Cryptography.RandomNumberGenerator.Create()){byte[] b=new byte[4];rng.GetBytes(b);pin=1000+(int)(BitConverter.ToUInt32(b,0)%9000);}
            var info=new ProcessStartInfo(Engine);
            info.Arguments="-n \""+receiverName+"\" -nh -pin "+pin+" -nc no -vs d3d11videosink";
            info.WorkingDirectory=HomeDir;info.UseShellExecute=false;info.CreateNoWindow=true;
            info.RedirectStandardOutput=true;info.RedirectStandardError=true;
            info.EnvironmentVariables["PATH"]=Path.GetDirectoryName(Engine)+";"+Environment.GetEnvironmentVariable("PATH");
            info.EnvironmentVariables["GST_PLUGIN_PATH"]=Path.Combine(RuntimeDir,"lib","gstreamer-1.0");
            info.EnvironmentVariables["GST_PLUGIN_SYSTEM_PATH_1_0"]=Path.Combine(RuntimeDir,"lib","gstreamer-1.0");
            info.EnvironmentVariables["GST_PLUGIN_SCANNER"]=Path.Combine(RuntimeDir,"libexec","gstreamer-1.0","gst-plugin-scanner.exe");
            info.EnvironmentVariables["GST_REGISTRY_1_0"]=Path.Combine(HomeDir,"gst-registry.bin");
            var p=new Process();p.StartInfo=info;
            p.OutputDataReceived+=delegate(object s,DataReceivedEventArgs e){Log(e.Data);};p.ErrorDataReceived+=delegate(object s,DataReceivedEventArgs e){Log(e.Data);};
            try{p.Start();receiver=p;p.BeginOutputReadLine();p.BeginErrorReadLine();}catch{if(receiver!=null)StopReceiver();else p.Dispose();throw;}
            stop.Text="Остановить";notice="";embedFailed=false;reveal=0;
        }catch(Exception e){Log(e.ToString());notice="Не удалось запустить приёмник. Открой журнал.";MessageBox.Show(this,e.Message,"ScreenMirror");}
        Invalidate();
    }
    void StopReceiver() {
        FinishOutlineResize(false);
        Process p=receiver;receiver=null;
        if(p!=null){try{if(!p.HasExited){p.Kill();p.WaitForExit(2500);}}catch(Exception e){Log(e.Message);}finally{p.Dispose();}}
        child=IntPtr.Zero;video.Visible=false;LeavePlayer();stop.Text="Запустить";reveal=0;Invalidate();
    }
    void TextAt(Graphics g,string text,float size,Color color,RectangleF box,bool bold,string family="Segoe UI") {
        using(var f=new Font(family,size,bold?FontStyle.Bold:FontStyle.Regular))
        using(var b=new SolidBrush(color))
        using(var fmt=new StringFormat()){fmt.Trimming=StringTrimming.EllipsisCharacter;g.DrawString(text,f,b,box,fmt);}
    }
    void Glow(Graphics g,float x,float y,int size,Color color) {
        using(var path=new GraphicsPath()){path.AddEllipse(x-size/2,y-size/2,size,size);
            using(var brush=new PathGradientBrush(path)){brush.CenterColor=color;brush.SurroundColors=new[]{Color.FromArgb(0,color)};g.FillPath(brush,path);}}
    }
    void PaintHub(Graphics g) {
        g.SmoothingMode=SmoothingMode.AntiAlias;
        float time=0;
        bool playing=child!=IntPtr.Zero;
        if(!playing){
            Glow(g,ClientSize.Width*.78f+(float)Math.Sin(time*.17)*45,Height*.45f+(float)Math.Cos(time*.2)*30,650,Color.FromArgb(62,95,77,240));
            Glow(g,Width*.2f+(float)Math.Cos(time*.13)*40,Height*.85f,550,Color.FromArgb(25,40,130,220));
            for(int i=0;i<18;i++){float x=(i*173+37)%Math.Max(1,Width);float y=(float)((i*67+time*(3+i%3))%Math.Max(1,Height));using(var b=new SolidBrush(Color.FromArgb(18+(i%4)*8,150,170,255)))g.FillEllipse(b,x,y,2,2);}
        }

        using(var header=new SolidBrush(BackColor))g.FillRectangle(header,1,1,Width-2,74);
        g.DrawImage(brand,new Rectangle(24,18,37,37));
        TextAt(g,"ScreenMirror",13,ForeColor,new RectangleF(72,20,200,25),true);
        TextAt(g,"Дублирование экрана AirPlay",8,Color.FromArgb(126,139,171),new RectangleF(73,45,200,18),false);
        using(var pen=new Pen(Color.FromArgb(31,38,55)))g.DrawLine(pen,26,76,Width-26,76);
        if(playing){
            TextAt(g,"●  Трансляция",10,Color.FromArgb(112,224,186),new RectangleF(260,28,180,24),false);
            TimeSpan duration=DateTime.Now-attachedAt;
            TextAt(g,duration.ToString(@"hh\:mm\:ss"),10,Color.FromArgb(155,168,195),new RectangleF(Width/2-30,Height-54,120,25),false);
        }else{
            int left=48;float top=Math.Max(113,Height*.20f);
            Color secondary=Color.FromArgb(152,164,192);
            TextAt(g,receiver==null?"ПРИЁМНИК ОСТАНОВЛЕН":"ОЖИДАЕМ ПОДКЛЮЧЕНИЯ",8,Color.FromArgb(139,153,247),new RectangleF(left,top,440,23),true);
            TextAt(g,"Твой iPhone.\nНа большом экране.",27,ForeColor,new RectangleF(left,top+36,Width*.6f,106),true);
            TextAt(g,"1. Подключи iPhone к той же сети, что и компьютер.\n2. На iPhone открой «Повтор экрана».\n3. Выбери компьютер с именем ниже.",11,secondary,new RectangleF(left,top+157,Math.Min(570,Width-96),85),false);
            float cardY=top+264;
            using(var p=Round(new RectangleF(left,cardY,Math.Min(470,Width-310),104),18)){
                using(var b=new SolidBrush(Color.FromArgb(165,25,31,48)))g.FillPath(b,p);
                using(var pen=new Pen(Color.FromArgb(55,115,131,191)))g.DrawPath(pen,p);
            }
            TextAt(g,receiverName,11,ForeColor,new RectangleF(left+20,cardY+16,Math.Min(430,Width-350),27),true);
            TextAt(g,receiver==null?"Нажми «Запустить»":"Код подключения",10,secondary,new RectangleF(left+20,cardY+58,180,24),false,"Segoe UI Semibold");
            if(receiver!=null)TextAt(g,pin.ToString(),21,Color.FromArgb(183,194,255),new RectangleF(left+Math.Min(305,Width-460),cardY+44,137,42),true,"Consolas");

            if(notice!="")TextAt(g,notice,9,secondary,new RectangleF(left,Height-111,Width-96,30),false);
        }
        if(playing&&notice!="")TextAt(g,notice,9,Color.Orange,new RectangleF(350,Height-54,Width-590,35),false);
    }
    void DrawPhone(Graphics g,float x,float y,float time) {
        y+=(float)Math.Sin(time*.9)*5;
        using(var p=Round(new RectangleF(x,y,155,273),27)){
            using(var b=new LinearGradientBrush(new RectangleF(x,y,155,273),Color.FromArgb(38,46,76),Color.FromArgb(13,17,29),65))g.FillPath(b,p);
            using(var pen=new Pen(Color.FromArgb(110,139,159,229),1.5f))g.DrawPath(pen,p);
        }
        using(var p=Round(new RectangleF(x+10,y+11,135,250),20))
        using(var b=new LinearGradientBrush(new RectangleF(x+10,y+11,135,250),Color.FromArgb(66,66,158),Color.FromArgb(26,28,58),50))g.FillPath(b,p);
        using(var p=Round(new RectangleF(x+53,y+19,49,12),6))using(var b=new SolidBrush(Color.FromArgb(12,15,24)))g.FillPath(b,p);
        g.DrawImage(brand,new Rectangle((int)x+48,(int)y+95,60,60));
        using(var pen=new Pen(Color.FromArgb(190,209,220,255),2)){
            pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;
            g.DrawArc(pen,x+55,y+179,45,32,220,100);g.DrawArc(pen,x+64,y+187,27,20,220,100);
        }
        using(var dot=new SolidBrush(Color.FromArgb(214,223,255)))g.FillEllipse(dot,x+75,y+200,5,5);
        using(var pen=new Pen(Color.FromArgb(160,204,214,255),3)){pen.StartCap=LineCap.Round;pen.EndCap=LineCap.Round;g.DrawLine(pen,x+56,y+248,x+99,y+248);}
    }
    protected override void Dispose(bool disposing){if(disposing){FinishOutlineResize(false);clock.Dispose();if(preciseClock){timeEndPeriod(1);preciseClock=false;}if(hubCache!=null)hubCache.Dispose();if(phoneCache!=null)phoneCache.Dispose();brand.Dispose();if(Icon!=null)Icon.Dispose();}base.Dispose(disposing);}
}
