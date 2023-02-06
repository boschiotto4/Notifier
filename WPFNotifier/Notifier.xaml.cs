//
//      Notifier.cs
// 
//      This project was born in the 2009 for a University application.
//      Then it was resurrected in the 2015 to be part of an old system that cannot handle the
//      3.5 framework and grows up to include more features. This is not a professional work, so
//      the code quality it's something like "let's do something quickly".
//      If you are looking for something professional, you can do it by yourself and of course share it!
//    
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFNotify
{
    public enum BackDialogStyle
    {
        None,                                                                       // Dialog style of black background
        FadedScreen,
        FadedApplication
    }

    public partial class Notifier : Window
    {
#region ImageSourceForBitmap
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public ImageSource ImageSourceForBitmap(System.Drawing.Bitmap bmp)          // Create an Image from Bitmap
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally 
            { 
                DeleteObject(handle); 
            }
        }
#endregion

#region GLOBALS
        public enum Type { INFO, WARNING, ERROR, OK }                   // Set the type of the Notifier

        class NoteLocation                                              // Helper class to handle Note position
        {
            internal double X;
            internal double Y;

            internal Point initialLocation;                             // Mouse bar drag helpers
            internal bool mouseIsDown = false;

            public NoteLocation(double x, double y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        static List<Notifier> notes = new List<Notifier>();             // Keep a list of the opened Notifiers

        private NoteLocation noteLocation;                              // Note position
        private short ID = 0;                                           // Note ID
        private string description = "";                                // Note default Description
        private string title = "Notifier";                              // Note default Title
        private Type type = Type.INFO;                                  // Note default Type

        private bool isDialog = false;                                  // Note is Dialog
        private BackDialogStyle backDialogStyle = BackDialogStyle.None; // DialogNote default background
        private Window myCallerApp;                                     // Base Application for Dialog Note

        private Color Hover = Color.FromArgb(0, 0, 0, 0);                // Default Color for over
        private Color Leave = Color.FromArgb(0, 0, 0, 0);               // Default Color for leave

        private int timeout_ms = 0;                                     // Temporary note: timeout
        private AutoResetEvent timerResetEvent = null;                       // Temporary note: reset event

        private Window inApp = null;                                    // In App Notifier: the note is binded to the specified container
#endregion

#region CONSTRUCTOR & DISPLAY
        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Default constructor
        //-------------------------------------------------------------------------------------------------------------------------------
        private Notifier(string dsc,
                         Type type,
                         string tit,
                         bool isDialog = false,
                         int timeout_ms = 0,
                         Window inApp = null)
        {
            this.isDialog       = isDialog;
            this.description    = dsc;
            this.type           = type;
            this.title          = tit;
            this.timeout_ms     = timeout_ms;
            this.inApp          = inApp;

            if (notes.Count > 0)                                            // Use the latest available ID from the note list
                ID = notes.Max(x => x.ID);
            ID++;

            if (inApp != null && !inAppNoteExists())                        // Register the drag and resize events only for the first note (if inApp note)
            {
                inApp.LocationChanged    += inApp_LocationChanged;
                inApp.SizeChanged        += inApp_LocationChanged;
                inApp.StateChanged       += inApp_LocationChanged;
            }
            InitializeComponent();
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Handle the drag  drop and resize location of the notes
        //-------------------------------------------------------------------------------------------------------------------------------
        void inApp_LocationChanged(object sender, EventArgs e)
        {
            foreach (var note in notes)
            {
                if (note.inApp != null)
                {
                    NoteLocation ln = adjustLocation(note);
                    note.Left       = ln.X;
                    note.Top        = ln.Y;
                }
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  On load form operations
        //-------------------------------------------------------------------------------------------------------------------------------
        private void OnLoad(object sender, RoutedEventArgs e)
        {
            ImageBrush imgClose  = new ImageBrush();
            ImageBrush imgMenu   = new ImageBrush();
            
            this.Tag             = "__Notifier|" + ID.ToString("X4");           // Save the note identification in the Tag field
            imgClose.ImageSource = ImageSourceForBitmap(
                                    global::WPFNotify.Properties.Resources.close);
            imgClose.Stretch     = Stretch.None;
            
            imgMenu.ImageSource  = ImageSourceForBitmap(
                                    global::WPFNotify.Properties.Resources.menu);
            imgMenu.Stretch      = Stretch.None;

            buttonClose.Background = imgClose;                                  // Set the Close button image
            buttonMenu.Background  = imgMenu;                                   // Set the Menu button image

            setNotifier(description, type, title);
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Create the Note and handle its location
        //-------------------------------------------------------------------------------------------------------------------------------
        private void setNotifier(string description, Type noteType, string title, bool isUpdate = false)
        {
            this.title = title;
            this.description = description;
            this.type = noteType;

            TextBlock contentText    = new TextBlock();                     // Wrap the content text
            contentText.TextWrapping = TextWrapping.Wrap;
            contentText.Text         = description;

            titleText.Content        = title;                               // Fill the Notifier data title
            noteContent.Content      = contentText;                         // Fill the Notifier data description
            noteDate.Content         = DateTime.Now + "";                   // Fill the Notifier data Timestamp
            noteIdText.Content       = ID.ToString("0000");                 // Fill the Notifier Id

#region ADJUST COLORS
            switch (noteType)
            {
                case Type.ERROR:
                    icon.Source = ImageSourceForBitmap(global::WPFNotify.Properties.Resources.ko);
                    Leave = Color.FromRgb(200, 60, 70);
                    Hover = Color.FromRgb(240, 80, 90);
                    break;
                case Type.INFO:
                    icon.Source = ImageSourceForBitmap(global::WPFNotify.Properties.Resources.info);
                    Leave = Color.FromRgb(90, 140, 230);
                    Hover = Color.FromRgb(110, 160, 250);
                    break;
                case Type.WARNING:
                    icon.Source = ImageSourceForBitmap(global::WPFNotify.Properties.Resources.warning);
                    Leave = Color.FromRgb(200, 200, 80);
                    Hover = Color.FromRgb(220, 220, 80);
                    break;
                case Type.OK:
                    icon.Source = ImageSourceForBitmap(global::WPFNotify.Properties.Resources.ok);
                    Leave = Color.FromRgb(80, 200, 130);
                    Hover = Color.FromRgb(80, 240, 130);
                    break;
            }


            titleText.Background = new SolidColorBrush(Leave);                 // Init colos                 
            BorderBrush          = new LinearGradientBrush(Leave, 
                                    Color.FromRgb(200, 200, 200), 90);
#endregion

#region DIALOG NOTE
            if (isDialog)
            {
                Button ok_button    = new Button();                               // Dialog note comes with a simple Ok button
                ok_button.Width     = 120;
                ok_button.Height    = 40;
                Height              = Height + 50;                                // Resize the note to contain the button
                contentGrid.Margin  = new Thickness(0,-50,0,0);

                int bLeft           = (int) (Width / 2 - ok_button.Width / 2);
                int bTop            = (int) (Height - 50);
                int shift_down      = 78;
                ok_button.Margin    = new Thickness(ok_button.Margin.Left, 
                                                 ok_button.Margin.Top + shift_down,
                                                 ok_button.Margin.Right, 
                                                 ok_button.Margin.Bottom - shift_down);

                ok_button.Content   = "OK";
                ok_button.Click     += onOkButtonClick;
                
                contentGrid.Children.Add(ok_button);                                 // Add the button to the grid

                noteDate.Margin     = new Thickness(noteDate.Margin.Left,            // Shift down the date location
                                                    noteDate.Margin.Top + 44, 
                                                    noteDate.Margin.Right, 
                                                    noteDate.Margin.Bottom - 44);

                switch (backDialogStyle)                                            // Positioning
                {
                    case BackDialogStyle.FadedScreen:
                    case BackDialogStyle.None:
                        Rectangle rec   = new Rectangle();                          // Working area
                        rec.Width       = System.Windows.SystemParameters.WorkArea.Width;
                        rec.Height      = System.Windows.SystemParameters.WorkArea.Height;

                        double X           = (rec.Width  - Width)  / 2;             // Center Screen position
                        double Y           = (rec.Height - Height) / 2;
                        noteLocation    = new NoteLocation(X, Y);
                        break;
                    case BackDialogStyle.FadedApplication:
                        double px = (myCallerApp.Left + myCallerApp.Width / 2) -    // Center of myCallerApp
                                    this.Width / 2;     
                        double py = (myCallerApp.Top + myCallerApp.Height / 2) - 
                                    this.Height / 2;
                        noteLocation = new NoteLocation(px, py);
                        break;
                }
            }
#endregion

#region NOTE LOCATION
            if (!isDialog && !isUpdate)
            {
                NoteLocation local = adjustLocation(this);                  // Set the note location

                Left = local.X;                                             // Notifier position X  
                Top  = local.Y;                                             // Notifier position Y 
            }
#endregion
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Find a valid position for the note into the note area:
        //                                  1. Inside the Screen (support multiple screens)
        //                                  2. Inside the father application (if specified)
        //-------------------------------------------------------------------------------------------------------------------------------
        private NoteLocation adjustLocation(Notifier note)
        {
            Rectangle notesArea     = new Rectangle();
            int       nMaxRows      = 0,
                      nColumn       = 0,
                      nMaxColumns   = 0,
                      xShift        = 25;                                             // Custom note overlay
            bool      add           = false;

            if (note.inApp != null &&                                                 // Get the available notes area,
                note.inApp.WindowState == System.Windows.WindowState.Normal)          // based on the type of note location
            {  
                notesArea.Width   = note.inApp.Width;
                notesArea.Height  = note.inApp.Height;
                notesArea.RadiusX = note.inApp.Left;
                notesArea.RadiusY = note.inApp.Top;
            }
            else
            {
                notesArea.Width =   Screens.getWorkingArea().Width;
                notesArea.Height =  Screens.getWorkingArea().Height;
                notesArea.RadiusX = Screens.getWorkingArea().Left;
                notesArea.RadiusY = Screens.getWorkingArea().Top;
            }

            nMaxRows     = (int) (notesArea.Height / Height);                         // Max number of rows in the available space
            nMaxColumns  = (int) (notesArea.Width  / xShift);                         // Max number of columns in the available space

            noteLocation = new NoteLocation(notesArea.Width -                         // Initial Position X
                                            Width +
                                            notesArea.RadiusX,
                                            notesArea.Height -                        // Initial Position Y
                                            Height +
                                            notesArea.RadiusY);

            while (nMaxRows > 0 && !add)                                              // Check the latest available position (no overlap)
            {
                for (int nRow = 1; nRow <= nMaxRows; nRow++)
                {
                    noteLocation.Y      = notesArea.Height  -
                                          Height * nRow     +
                                          notesArea.RadiusY;

                    if (!isLocationAlreadyUsed(noteLocation, note))
                    {
                        add = true; break;
                    }

                    if (nRow == nMaxRows)                                            // X shift if no more column space
                    {
                        nColumn++;
                        nRow = 0;

                        noteLocation.X  = notesArea.Width           -
                                          Width - xShift * nColumn  +
                                          notesArea.RadiusX;
                    }

                    if (nColumn >= nMaxColumns)                                      // Last exit condition: the screen is full of note
                    {
                        add = true; break;
                    }
                }
            }

            noteLocation.initialLocation = new Point(noteLocation.X,                  // Init the initial Location, for drag & drop
                                                     noteLocation.Y);
            return noteLocation;
        }
#endregion

#region EVENTS
        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Close event for the note
        //-------------------------------------------------------------------------------------------------------------------------------        
        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            closeMe();
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Show the menu (for the menu button) event
        //-------------------------------------------------------------------------------------------------------------------------------
        private void onMenuClick(object sender, RoutedEventArgs e)
        {
            buttonMenu.ContextMenu.PlacementTarget = this;
            buttonMenu.ContextMenu.IsOpen = true; 
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Close all the notes event
        //-------------------------------------------------------------------------------------------------------------------------------
        private void onMenuCloseAllClick(object sender, RoutedEventArgs e)
        {
            CloseAll();
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Dialog note Only (Ok button click event)
        //-------------------------------------------------------------------------------------------------------------------------------
        private void onOkButtonClick(object sender, RoutedEventArgs e)
        {
            closeMe();
        }
#endregion

#region HELPERS
        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Close the note event
        //-------------------------------------------------------------------------------------------------------------------------------
        private void closeMe()
        {
            notes.Remove(this);
            this.Close();

            if (notes.Count == 0)
                ID = 0;                                                 // Reset the ID counter if no notes is displayed
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  get the Specified note by the specified content
        //-------------------------------------------------------------------------------------------------------------------------------
        private Notifier getNote(string _title, string _desc, Type _type)
        {
            foreach (var note in notes)
            {
                if (note.description == _desc &&
                    note.title       == _title &&
                    note.type        == _type)
                {
                    return note;
                }
            }
            return null;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Check if a note with an inApp capabilities is setted
        //-------------------------------------------------------------------------------------------------------------------------------
        private bool inAppNoteExists()
        {
            foreach (var note in notes)
            {
                if (note.inApp != null)
                    return true;
            }
            return false;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  check if the specified location (X, Y) is already used by another note
        //-------------------------------------------------------------------------------------------------------------------------------
        private bool isLocationAlreadyUsed(NoteLocation location, Notifier note)
        {
            foreach (var p in notes)
                if (p.Left == location.X &&
                    p.Top  == location.Y)
                {
                    if (note.inApp != null &&
                        p.ID       == note.ID)
                        return false;
                    return true;
                }
            return false;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Close all the notes
        //-------------------------------------------------------------------------------------------------------------------------------
        public static void CloseAll()
        {
            for (int i = notes.Count - 1; i >= 0; i--)
            {
                notes[i].closeMe();
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Handle the close of a Dialog note
        //-------------------------------------------------------------------------------------------------------------------------------
        private void onClosed(object sender, EventArgs e)
        {
            switch (backDialogStyle)
            {
                case BackDialogStyle.FadedScreen:
                case BackDialogStyle.FadedApplication:
                    if (Owner != null) 
                        Owner.Close();
                    break;
            }
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Adjust the title bar color on mouse hover
        //-------------------------------------------------------------------------------------------------------------------------------
        private void onMouseEnter(object sender, MouseEventArgs e)
        {
            titleText.Background = new SolidColorBrush(Hover);
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Adjust the title bar color on mouse leave
        //-------------------------------------------------------------------------------------------------------------------------------
        private void onMouseLeave(object sender, MouseEventArgs e)
        {
            titleText.Background = new SolidColorBrush(Leave);
        }
#endregion

#region NOTE CREATION AND MODIFY
        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Show the note: it is the startup of the creation process of the note
        //-------------------------------------------------------------------------------------------------------------------------------
        public static short Show(string desc, 
                                 Type type      = Type.INFO, 
                                 string tit     = "Notifier", 
                                 bool isDialog  = false, 
                                 int timeout    = 0, 
                                 Window inApp   = null)
        {
            short updated_note_id = 0,                                       // If there is already a note with the same content
                  updated_note_occurency = 0;                                       // update it and do not create a new one

            if (NotifierAlreadyPresent(desc,
                                       type,
                                       tit,
                                       isDialog,
                                       out updated_note_id,
                                       out updated_note_occurency))
            {
                Update(updated_note_id, desc, type, "[" + ++updated_note_occurency + "] " + tit);
            }
            else
            {
                Notifier not = new Notifier(desc,                                   // Instantiate the Note
                                            type,
                                            tit,
                                            isDialog,
                                            timeout,
                                            inApp);
                not.Show();                                                         // Show the note

                if (not.timeout_ms >= 500)                                          // Start autoclose timer (if any)
                {
                    not.timerResetEvent      = new AutoResetEvent(false);
                    
                    BackgroundWorker timer   = new BackgroundWorker();
                    timer.DoWork             += timer_DoWork;
                    timer.RunWorkerCompleted += timer_RunWorkerCompleted;
                    timer.RunWorkerAsync(not);                                      // Timer (temporary notes)
                }

                notes.Add(not);                                                     // Add to our collection of Notifiers
                updated_note_id = not.ID;
            }

            return updated_note_id;                                                 // Return the current ID of the created/updated Note
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Check if the note is already present
        //                                  Point out the ID and the occurency of the already present note
        //-------------------------------------------------------------------------------------------------------------------------------
        private static bool NotifierAlreadyPresent(string desc,
                                                   Type type,
                                                   string tit,
                                                   bool isDiag,
                                                   out short updated_note_id,
                                                   out short updated_note_occurency)
        {
            updated_note_id = 0;
            updated_note_occurency = 0;

            foreach (var note in notes)
            {
                short occurency      = 0;
                string filteredTitle = note.title;
                int indx             = filteredTitle.IndexOf(']');

                if (indx > 0)
                {
                    string numberOccurency = filteredTitle.Substring(0, indx);              // Get occurrency from title
                    numberOccurency        = numberOccurency.Trim(' ', ']', '[');
                    Int16.TryParse(numberOccurency, out occurency);

                    if (occurency > 1)                                                      // This will fix the note counter due to the
                        --occurency;                                                        // displayed note number that starts from "[2]"

                    filteredTitle = filteredTitle.Substring(indx + 1).Trim();
                }

                if (note.Tag         != null &&                                             // Get the node
                    note.description == desc &&
                    note.isDialog    == isDiag &&
                    filteredTitle    == tit &&
                    note.type        == type)
                {
                    string hex_id          = note.Tag.ToString().Split('|')[1];             // Get Notifier ID
                    short id               = Convert.ToInt16(hex_id, 16);
                    updated_note_id        = id;
                    updated_note_occurency = ++occurency;
                    return true;
                }
            }
            return false;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Update the note with the new content. Reset the timeout if any
        //-------------------------------------------------------------------------------------------------------------------------------
        public static void Update(short ID,
                                  string desc,
                                  Type noteType,
                                  string title)
        {
            foreach (var note in notes)
            {
                if (note.Tag != null &&                                     // Get the node
                    note.Tag.Equals("__Notifier|" + ID.ToString("X4")))
                {
                    if (note.timerResetEvent != null)                            // Reset the timeout timer (if any)
                        note.timerResetEvent.Set();

                    Notifier myNote = (Notifier)note;
                    myNote.setNotifier(desc, noteType, title, true);        // Set the new note content
                }
            }
        }
#endregion

#region TIMER
        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Background Worker to handle the timeout of the note
        //-------------------------------------------------------------------------------------------------------------------------------
        private static void timer_DoWork(object sender, DoWorkEventArgs e)
        {
            Notifier not = (Notifier)e.Argument;
            bool timedOut = false;
            while (!timedOut)
            {
                if (!not.timerResetEvent.WaitOne(not.timeout_ms))
                    timedOut = true;                                        // Time is out
            }
            e.Result = e.Argument;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Background Worker to handle the timeout event
        //-------------------------------------------------------------------------------------------------------------------------------
        private static void timer_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Notifier not = (Notifier)e.Result;
            not.closeMe();                                                  // Close the note
        }
#endregion

#region DIALOG NOTE CREATION
        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Show a Dialog note: with faded background if specified
        //-------------------------------------------------------------------------------------------------------------------------------
        public static bool ShowDialog(string content, 
                                      Type type                       = Type.INFO, 
                                      string title                    = "Notifier",
                                      BackDialogStyle backDialogStyle = BackDialogStyle.FadedScreen,
                                      Window application              = null)
        {
            Window back             = null;
            int back_border         = 200;
            bool orgTopMostSettings = false;

            if (backDialogStyle == BackDialogStyle.FadedApplication && application == null)
                backDialogStyle = BackDialogStyle.FadedScreen;

            if(backDialogStyle != BackDialogStyle.None)
            {
                back                    = new Window();                             // Create the fade background
                back.ResizeMode         = ResizeMode.NoResize;
                back.Owner              = application;
                back.Background         = new SolidColorBrush(Color.FromArgb(153, 0, 0, 0));
                back.AllowsTransparency = true;
                back.WindowStyle        = WindowStyle.None;
                back.ShowInTaskbar      = false;
            }

            Notifier note        = new Notifier(content, type, title, true);          // Instantiate the Notifier form
            note.backDialogStyle = backDialogStyle;

            switch (note.backDialogStyle)
            {
                case BackDialogStyle.None:
                    if (application != null)                                       // Set the startup position
                    {
                        note.Owner                 = application;
                        note.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    }
                    else
                    {
                        note.WindowStartupLocation = WindowStartupLocation.CenterScreen; 
                    }
                    break;
                case BackDialogStyle.FadedScreen:
                    back.Left    = 0;
                    back.Top     = 0;
                                 
                    back.Width   = System.Windows.SystemParameters.WorkArea.Width  + back_border;
                    back.Height  = System.Windows.SystemParameters.WorkArea.Height + back_border;

                    back.Topmost = true;
                    
                    if (application != null)
                    {
                        back.Show();
                        note.Owner = back;
                    } 

                    note.WindowStartupLocation = WindowStartupLocation.CenterScreen; // Set the startup position
                    break;
                case BackDialogStyle.FadedApplication:
                    note.myCallerApp    = application;
                    orgTopMostSettings  = application.Topmost;
                    application.Topmost = true;

                    back.Width          = application.Width;
                    back.Height         = application.Height;
                    back.Left           = application.Left;
                    back.Top            = application.Top;
                    back.Topmost        = true;

                    if (application != null)
                    {
                        back.Show();
                        note.Owner = back;
                    }

                    note.WindowStartupLocation = WindowStartupLocation.CenterOwner; // Set the startup position
                    break;
            }

            notes.Add(note);                                                        // Add to our collection of Notifiers 

            note.ShowInTaskbar = false;
            note.ShowDialog();

            if (back != null)                                                       // Close the back
                back.Close();                                                       

            if (application != null)                                                // restore app window top most property
                application.Topmost = orgTopMostSettings;

            return true;
        }

        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Show a Dialog note: fast creation
        //-------------------------------------------------------------------------------------------------------------------------------
        public static void ShowDialog(string content, string title = "Notifier", Type type = Type.INFO)
        {
            ShowDialog(content, type, title);
        }
#endregion

#region DRAG NOTE
        //-------------------------------------------------------------------------------------------------------------------------------
        //                                  Handle the dragging event: change the position of the note
        //-------------------------------------------------------------------------------------------------------------------------------
        private void onMouseDownDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();

                noteLocation.X = Left;                      // Update the stored location
                noteLocation.Y = Top;
            }
        }
#endregion

    }   // Close Class
}       // Close NS
