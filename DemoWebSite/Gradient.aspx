<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Gradient.aspx.cs" Inherits="Gradient" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.1//EN" "http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
   <title>Gradient Theming demo</title>
</head>
<body>
	<h2>Gradient theming demo</h2>
	<form runat="server">
		<div>   
    		<asp:RadioButtonList ID="rblMapTools" runat="server" RepeatDirection="Horizontal">
				<asp:ListItem Value="0">Zoom in</asp:ListItem>
				<asp:ListItem Value="1">Zoom out</asp:ListItem>
				<asp:ListItem Value="2" Selected="True">Pan</asp:ListItem>
			</asp:RadioButtonList>
			<asp:ImageButton Width="600" Height="300" ID="imgMap" runat="server" OnClick="imgMap_Click" style="border: 1px solid #000;" />
		</div>
	</form>
	This demo uses the following thematics styles:<br />
	- Country color by population density density<br />
	- Country labelsize by population density<br />
	- City Symbol size by city popultion
</body>
</html>
