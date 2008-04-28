using System;
using System.Collections;
using System.Configuration;
using System.Data;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using System.IO;

namespace SharpMap.Presentation.AspNet.Demo
{
    public partial class Default : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
                BindRepeater();
        }

        private void BindRepeater()
        {
            DirectoryInfo di = new DirectoryInfo(Server.MapPath("~/Maps"));
            FileInfo[] files = di.GetFiles();
            
            this.repDemos.DataSource = files;
            this.repDemos.DataBind();
        }
    }
}
