<%@ Page Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true" CodeFile="Ajax3.aspx.cs" Inherits="Ajax" Title="AJAX map" %>
<%@ Register TagPrefix="smap" Namespace="SharpMap.Web.UI.Ajax" Assembly="SharpMap.UI" %>
<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" Runat="Server">

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

</asp:Content>

