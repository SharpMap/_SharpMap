using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DelftTools.Utils.Reflection;
using log4net;

namespace DelftTools.TestUtils
{
    public partial class WindowsFormsTestHelper : Form
    {
        private static WindowsFormsTestHelper form;

        private static ILog log = LogManager.GetLogger(typeof(WindowsFormsTestHelper));

        private static Action<Form> formShown;
        private static Exception exception;

        static WindowsFormsTestHelper()
        {
            Application.EnableVisualStyles();
            Application.ThreadException += Application_ThreadException;
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            exception = e.Exception;
            log.Error("Windows.Forms exception occured: " + e.Exception.Message, e.Exception);
        }

        public WindowsFormsTestHelper()
        {
            InitializeComponent();
        }

        public static void Show(Control control, Action<Form> formShownAction, params object[] propertyObjects)
        {
            new WindowsFormsTestHelper().ShowControl(control, formShownAction, propertyObjects);
        }

        public static void Show(Control control, params object[] propertyObjects)
        {
            new WindowsFormsTestHelper().ShowControl(control, propertyObjects);
        }

        public static void ShowModal(Control control, Action<Form> formShownAction, params object[] propertyObjects)
        {
            new WindowsFormsTestHelper().ShowControlModal(control, formShownAction, propertyObjects);
        }

        /// <summary>
        /// Shows a control Modally by embedding it in a testform or calling .Show() for a toplevel control (Form).
        /// </summary>
        /// <param name="control"></param>
        /// <param name="propertyObjects"></param>
        public static void ShowModal(Control control, params object[] propertyObjects)
        {
            new WindowsFormsTestHelper().ShowControlModal(control, propertyObjects);
        }

        /// <summary>
        /// Embeds the controls into one container control, and shows this control modally.
        /// All controls will be using dockstyle top.
        /// </summary>
        /// <param name="controls">Controls to embed</param>
        /// <param name="propertyObjects">Objects to show as property</param>
        public static void ShowModal(IEnumerable<Control> controls, params object[] propertyObjects)
        {
            ShowModal(controls, true, propertyObjects);
        }
        
        /// <summary>
        /// Embeds the controls into one container control, and shows this control modally. 
        /// All controls will be using dockstyle top, exect the last control (if useFillForLastControl is true).
        /// </summary>
        /// <param name="controls">Controls to embed</param>
        /// <param name="useFillForLastControl">If true, the last control will be using dockstyle fill</param>
        /// <param name="propertyObjects">Objects to show as property</param>
        public static void ShowModal(IEnumerable<Control> controls, bool useFillForLastControl, params object[] propertyObjects)
        {
            var containerControl = new Control();
            var numberOfControls = 0;

            foreach (var control in controls)
            {
                control.Dock = DockStyle.Top;
                containerControl.Controls.Add(control);
                numberOfControls++;
            }

            if (useFillForLastControl)
            {
                containerControl.Controls[numberOfControls - 1].Dock = DockStyle.Fill;
            }

            ShowModal(containerControl, propertyObjects);
        }

        /// <summary>
        /// Checks build_number environment variable to determine whether we run on the build server.
        /// </summary>
        public static bool IsBuildServer
        {
            get
            {
                return File.Exists("C:\\build.server") 
                    || !String.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUILD_NUMBER"));
            }
        }

        public void ShowControl(Control control, Action<Form> formShownAction, params object[] propertyObjects)
        {
            exception = null;
            formShown = formShownAction;

            if (control.TopLevelControl == control)
            {
                ShowTopLevelControl(control, false);
            }
            else
            {
                ShowControlInTestForm(control, false, propertyObjects);
            }
        }

        public void ShowControl(Control control, params object[] propertyObjects)
        {
            exception = null;
            formShown = null;

            if (control.TopLevelControl == control)
            {
                ShowTopLevelControl(control,false);
            }
            else
            {
                ShowControlInTestForm(control, false, propertyObjects);
            }
        }

        public void ShowControlModal(Control control, Action<Form> formShownAction, params object[] propertyObjects)
        {
            exception = null;
            formShown = formShownAction;

            var modal = true;

            if (control.TopLevelControl == control)
            {
                ShowTopLevelControl(control, modal);
            }
            else
            {
                ShowControlInTestForm(control, modal, propertyObjects);
            }
        }

        public void ShowControlModal(Control control, params object[] propertyObjects)
        {
            exception = null;
            formShown = null;

            var modal = true;

            if (control.TopLevelControl == control)
            {
                ShowTopLevelControl(control, modal);
            }
            else
            {
                ShowControlInTestForm(control, modal, propertyObjects);
            }
        }

        private void ShowControlInTestForm(Control control, bool modal, object[] propertyObjects)
        {
            PropertyObjects = propertyObjects;

            propertyGrid1.SelectedObject = control;
            control.Dock = DockStyle.Fill;
            splitContainer1.Panel2.Controls.Add(control);

            InitializeTree(control);

            Show();

            while (!control.Visible)
            {
                Application.DoEvents();
            }

            try
            {
                if (formShown != null)
                {
                    formShown(this);
                }
            }
            catch (Exception e)
            {
                exception = e;
            }

            if (IsBuildServer)
            {
                //Application.DoEvents();
                
                if(exception != null)
                {
                    log.Error("Exception occured: " + exception.Message, exception);
                    log.Error("Stack trace: " + exception.StackTrace);
                    throw exception;
                }
            }
            else
            {
                log.DebugFormat("Not on buildserver so will show the following control modal: {0}", control);

                while (modal && Visible)
                {
                    Application.DoEvents();

                    if (exception != null)
                    {
                        throw exception;
                    }
                }
            }
        }

        private static void ShowTopLevelControl(Control control,bool modal)
        {
            bool wasShown = false;
            control.VisibleChanged += delegate
                                          {
                                              if (control.Visible)
                                              {
                                                  wasShown = true;
                                              }

                                              try
                                              {
                                                  if (formShown != null)
                                                  {
                                                      formShown(control is Form ? (Form)control : form);
                                                  }
                                              }
                                              catch (Exception e)
                                              {
                                                  exception = e;

                                              }

                                          };
            control.Show();

            if (IsBuildServer)
            {
                Application.DoEvents();

                if (exception != null)
                {
                    throw exception;
                }
            }
            else while (modal && control.Visible)
            {
                Application.DoEvents();

                if (exception != null)
                {
                    throw exception;
                }
            }

            while (!wasShown && !control.Visible) // wait until control is shown
            {
                Application.DoEvents();
            }
        }

        public static object[] PropertyObjects { get; set; }

        public PropertyGrid PropertyGrid
        {
            get { return propertyGrid1; }
        }

        public static object PropertyObject
        {
            get { return form.PropertyGrid.SelectedObject; }
            set { form.PropertyGrid.SelectedObject = value; }
        }

        private class RootNode : ArrayList
        {
            public override string ToString()
            {
                return "RootNode";
            }
        }

        private void InitializeTree(Control control)
        {
            IList itemsToShow = new RootNode {control};
            foreach (var o in PropertyObjects)
            {
                itemsToShow.Add(o);
            }

            treeView1.ImageList = new ImageList { ColorDepth = ColorDepth.Depth32Bit };
            treeView1.ImageList.Images.Add("Control", Resources.Control);
            treeView1.ImageList.Images.Add("Data", Resources.Data);
            
            
            AddAllNodes(treeView1.Nodes, itemsToShow);

            treeView1.NodeMouseClick += delegate { propertyGrid1.SelectedObject = treeView1.SelectedNode.Tag; };
        }

        private void AddAllNodes(TreeNodeCollection nodes, IEnumerable itemsToShow)
        {
            foreach (var item in itemsToShow.Cast<object>().Where(i => i != null))
            {
                var imageIndex = item is Control ? 0 : 1;
                var node = new TreeNode(item.ToString(), imageIndex, imageIndex) {Tag = item};
                nodes.Add(node);

                if(item is Control)
                {
                    var control = ((Control)item);
                    AddAllNodes(node.Nodes, control.Controls);

                    // hack, try to get Data or DataSource property
                    var dataProperty = control.GetType().GetProperty("Data");
                    if(dataProperty != null)
                    {
                        AddAllNodes(node.Nodes, new [] {dataProperty.GetValue(control, null)});
                    }

                    var dataSourceProperty = control.GetType().GetProperty("DataSource");
                    if (dataSourceProperty != null)
                    {
                        AddAllNodes(node.Nodes, new[] { dataSourceProperty.GetValue(control, null) });
                    }
                }

                if(item is IEnumerable)
                {
                    AddAllNodes(node.Nodes, (IEnumerable)item);
                }
            }
        }
    }
}