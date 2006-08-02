<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Ajax.aspx.cs" Inherits="Ajax" %>
<%@ Register TagPrefix="smap" Namespace="SharpMap.Web.UI.Ajax" Assembly="SharpMap.UI" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
   <title>AJAX map</title>
</head>

<body>
<form id="Form1" runat="server" action="Ajax.aspx">
<h2>AJAX Map</h2>

<div style="background-color: #fff; color:#000;">
   	<asp:RadioButtonList ID="rblMapTools" runat="server" RepeatDirection="Horizontal">
		<asp:ListItem Value="0" onClick="ajaxMapObj.disableClickEvent(); ajaxMapObj.zoomAmount = 3;"  Selected="True">Zoom in</asp:ListItem>
		<asp:ListItem Value="1" onClick="ajaxMapObj.disableClickEvent(); ajaxMapObj.zoomAmount = 0.33333;" >Zoom out</asp:ListItem>
		<asp:ListItem Value="2" onClick="ajaxMapObj.enableClickEvent();">Query map</asp:ListItem>
	</asp:RadioButtonList>
</div>
<div style="background-color:#f1f1f1; border:solid 1px #000;">
	<smap:AjaxMapControl width="100%" height="400px" id="ajaxMap" runat="server"
	OnClickEvent="MapClicked" OnViewChange="ViewChanged" OnViewChanging="ViewChanging" />
</div>
 <div id="dataContents"></div> 
</form>

<script type="text/javascript">
//Fired when query is selected and map is clicked
function MapClicked(event,obj)
{
	var mousePos = SharpMap_GetRelativePosition(event.clientX,event.clientY,obj.container);
	var pnt = SharpMap_PixelToMap(mousePos.x,mousePos.y,obj);
	var field = document.getElementById('dataContents');
	field.innerHTML = "You clicked map at: " + pnt.x + "," + pnt.y;
}
//Fired when a new map starts to load
function ViewChanging(obj)
{
	var field = document.getElementById('dataContents');
	field.innerHTML = "Loading...";
}
//Fired when a map has loaded
function ViewChanged(obj)
{
	var field = document.getElementById('dataContents');
	field.innerHTML = "Current map center: " + obj.GetCenter().x + "," + obj.GetCenter().y;	
}
</script>

</body>
</html>
