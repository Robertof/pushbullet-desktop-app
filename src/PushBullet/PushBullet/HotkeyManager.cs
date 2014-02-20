using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace PushBullet
{
    // Inspired from https://github.com/curtisrutland/WpfHotkeys/.
    // Thank you!
    class HotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public delegate void HotkeyPressed();
        private IntPtr _hWnd;
        private Dictionary<int, DataContainer> keys = new Dictionary<int, DataContainer>();
        private static HotkeyManager instance;
        public static HotkeyManager Instance
        {
            get {
                if (instance == null)
                    instance = new HotkeyManager();
                return instance;
            }
        }

        private HotkeyManager() { }

        public int RegisterHotkey(Modifiers modifier, Keys key, Window window, HotkeyPressed evt)
        {
            if (_hWnd == null)
                _hWnd = new WindowInteropHelper(window).Handle;
            var src = PresentationSource.FromVisual(window) as HwndSource;
            if (src == null) throw new Exception("Can't create hWnd source from window");
            if (_hWnd == null) throw new Exception("hwnd null");
            src.AddHook(WndProc);
            int id = ((int) modifier ^ (int) key ^ _hWnd.ToInt32()) * 555;
            if (!RegisterHotKey(_hWnd, id, (int) modifier, (int) key))
                throw new Exception("Can't register hotkey >:(");
            keys.Add(id, new DataContainer(modifier, key, evt));
            return id;
        }

        public void UnregisterHotkey(int id) { UnregisterHotkey(id, false); }

        public void UnregisterHotkey(int id, bool dontRemove)
        {
            if (!UnregisterHotKey(_hWnd, id))
            {
                var wex = new Win32Exception();
                if (wex.NativeErrorCode != 0)
                    throw new Exception("Hotkey " + id + " failed to unregister. Please see InnerException for details.", wex);
            }
            if (!dontRemove)
                keys.Remove(id);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312)
            {
                MessageBox.Show("Got msg");
                var lpInt = (int) lParam;
                int key = (lpInt >> 16) & 0xFFFF;
                int modifier = lpInt & 0xFFFF;
                foreach (DataContainer container in keys.Values)
                {
                    if ((int) container.key == key && (int) container.modifier == modifier)
                        container.evt();
                }
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            MessageBox.Show("Dispose called");
            foreach (int key in keys.Keys)
                UnregisterHotkey(key, true);
            GC.SuppressFinalize(this);
        }

        ~HotkeyManager() { foreach (int key in keys.Keys) UnregisterHotkey(key, true); }

        // could have used an object[] for this but I'm lazy
        private sealed class DataContainer
        {
            public readonly Modifiers modifier;
            public readonly Keys key;
            public readonly HotkeyPressed evt;
            public DataContainer(Modifiers m, Keys k, HotkeyPressed e)
            {
                modifier = m; key = k; evt = e;
            }
        }

        // This is a copy-paste from
        // https://raw.github.com/curtisrutland/WpfHotkeys/master/Com.CurtisRutland.WpfHotkeys/Constants.cs
        // Thank you once again!
        [Flags]
        public enum Modifiers
        {
            NoMod = 0x0000,
            Alt = 0x0001,
            Ctrl = 0x0002,
            Shift = 0x0004,
            Win = 0x0008
        }

        /// <summary>
        /// Copy of System.Windows.Forms.Keys, remove the requirement to include a reference to System.Windows.Forms
        /// </summary>
        [Flags]
        public enum Keys
        {
            Modifiers = -65536,
            None = 0,
            LButton = 1,
            RButton = 2,
            Cancel = RButton | LButton,
            MButton = 4,
            XButton1 = MButton | LButton,
            XButton2 = MButton | RButton,
            Back = 8,
            Tab = Back | LButton,
            LineFeed = Back | RButton,
            Clear = Back | MButton,
            Enter = Clear | Tab,
            Return = Enter,
            ShiftKey = 16,
            ControlKey = ShiftKey | LButton,
            Menu = ShiftKey | RButton,
            Pause = Menu | ControlKey,
            Capital = ShiftKey | MButton,
            CapsLock = Capital,
            HanguelMode = CapsLock | ControlKey,
            HangulMode = HanguelMode,
            KanaMode = HangulMode,
            JunjaMode = KanaMode | Pause,
            FinalMode = ShiftKey | Back,
            HanjaMode = FinalMode | ControlKey,
            KanjiMode = HanjaMode,
            Escape = KanjiMode | Pause,
            IMEConvert = FinalMode | CapsLock,
            IMENonconvert = IMEConvert | KanjiMode,
            IMEAccept = IMEConvert | Menu,
            IMEAceept = IMEAccept,
            IMEModeChange = IMEAceept | IMENonconvert,
            Space = 32,
            PageUp = Space | LButton,
            Prior = PageUp,
            Next = Space | RButton,
            PageDown = Next,
            End = PageDown | Prior,
            Home = Space | MButton,
            Left = Home | Prior,
            Up = Home | PageDown,
            Right = Up | Left,
            Down = Space | Back,
            Select = Down | Prior,
            Print = Down | PageDown,
            Execute = Print | Select,
            PrintScreen = Down | Home,
            Snapshot = PrintScreen,
            Insert = Snapshot | Select,
            Delete = Snapshot | Print,
            Help = Delete | Insert,
            D0 = Space | ShiftKey,
            D1 = D0 | Prior,
            D2 = D0 | PageDown,
            D3 = D2 | D1,
            D4 = D0 | Home,
            D5 = D4 | D1,
            D6 = D4 | D2,
            D7 = D6 | D5,
            D8 = D0 | Down,
            D9 = D8 | D1,
            A = 65,
            B = 66,
            C = B | A,
            D = 68,
            E = D | A,
            F = D | B,
            G = F | E,
            H = 72,
            I = H | A,
            J = H | B,
            K = J | I,
            L = H | D,
            M = L | I,
            N = L | J,
            O = N | M,
            P = 80,
            Q = P | A,
            R = P | B,
            S = R | Q,
            T = P | D,
            U = T | Q,
            V = T | R,
            W = V | U,
            X = P | H,
            Y = X | Q,
            Z = X | R,
            LWin = Z | Y,
            RWin = X | T,
            Apps = RWin | Y,
            Sleep = Apps | LWin,
            NumPad0 = 96,
            NumPad1 = NumPad0 | A,
            NumPad2 = NumPad0 | B,
            NumPad3 = NumPad2 | NumPad1,
            NumPad4 = NumPad0 | D,
            NumPad5 = NumPad4 | NumPad1,
            NumPad6 = NumPad4 | NumPad2,
            NumPad7 = NumPad6 | NumPad5,
            NumPad8 = NumPad0 | H,
            NumPad9 = NumPad8 | NumPad1,
            Multiply = NumPad8 | NumPad2,
            Add = Multiply | NumPad9,
            Separator = NumPad8 | NumPad4,
            Subtract = Separator | NumPad9,
            Decimal = Separator | Multiply,
            Divide = Decimal | Subtract,
            F1 = NumPad0 | P,
            F2 = F1 | NumPad1,
            F3 = F1 | NumPad2,
            F4 = F3 | F2,
            F5 = F1 | NumPad4,
            F6 = F5 | F2,
            F7 = F5 | F3,
            F8 = F7 | F6,
            F9 = F1 | NumPad8,
            F10 = F9 | F2,
            F11 = F9 | F3,
            F12 = F11 | F10,
            F13 = F9 | F5,
            F14 = F13 | F10,
            F15 = F13 | F11,
            F16 = F15 | F14,
            F17 = 128,
            F18 = F17 | LButton,
            F19 = F17 | RButton,
            F20 = F19 | F18,
            F21 = F17 | MButton,
            F22 = F21 | F18,
            F23 = F21 | F19,
            F24 = F23 | F22,
            NumLock = F17 | ShiftKey,
            Scroll = NumLock | F18,
            LShiftKey = F17 | Space,
            RShiftKey = LShiftKey | F18,
            LControlKey = LShiftKey | F19,
            RControlKey = LControlKey | RShiftKey,
            LMenu = LShiftKey | F21,
            RMenu = LMenu | RShiftKey,
            BrowserBack = LMenu | LControlKey,
            BrowserForward = BrowserBack | RMenu,
            BrowserRefresh = LShiftKey | Down,
            BrowserStop = BrowserRefresh | RShiftKey,
            BrowserSearch = BrowserRefresh | LControlKey,
            BrowserFavorites = BrowserSearch | BrowserStop,
            BrowserHome = BrowserRefresh | LMenu,
            VolumeMute = BrowserHome | BrowserStop,
            VolumeDown = BrowserHome | BrowserSearch,
            VolumeUp = VolumeDown | VolumeMute,
            MediaNextTrack = LShiftKey | NumLock,
            MediaPreviousTrack = MediaNextTrack | RShiftKey,
            MediaStop = MediaNextTrack | LControlKey,
            MediaPlayPause = MediaStop | MediaPreviousTrack,
            LaunchMail = MediaNextTrack | LMenu,
            SelectMedia = LaunchMail | MediaPreviousTrack,
            LaunchApplication1 = LaunchMail | MediaStop,
            LaunchApplication2 = LaunchApplication1 | SelectMedia,
            Oem1 = MediaStop | BrowserSearch,
            OemSemicolon = Oem1,
            Oemplus = OemSemicolon | MediaPlayPause,
            Oemcomma = LaunchMail | BrowserHome,
            OemMinus = Oemcomma | SelectMedia,
            OemPeriod = Oemcomma | OemSemicolon,
            Oem2 = OemPeriod | OemMinus,
            OemQuestion = Oem2,
            Oem3 = 192,
            Oemtilde = Oem3,
            Oem4 = Oemtilde | Scroll | F20 | LWin,
            OemOpenBrackets = Oem4,
            Oem5 = Oemtilde | NumLock | F21 | RWin,
            OemPipe = Oem5,
            Oem6 = OemPipe | Scroll,
            OemCloseBrackets = Oem6,
            Oem7 = OemPipe | F23,
            OemQuotes = Oem7,
            Oem8 = OemQuotes | OemCloseBrackets,
            Oem102 = Oemtilde | LControlKey,
            OemBackslash = Oem102,
            ProcessKey = Oemtilde | RMenu,
            Packet = ProcessKey | OemBackslash,
            Attn = OemBackslash | LaunchApplication1,
            Crsel = Attn | Packet,
            Exsel = Oemtilde | MediaNextTrack | BrowserRefresh,
            EraseEof = Exsel | MediaPreviousTrack,
            Play = Exsel | OemBackslash,
            Zoom = Play | EraseEof,
            NoName = Exsel | OemPipe,
            Pa1 = NoName | EraseEof,
            OemClear = NoName | Play,
            KeyCode = 65535,
            Shift = 65536,
            Control = 131072,
            Alt = 262144,
        }
    }
}
